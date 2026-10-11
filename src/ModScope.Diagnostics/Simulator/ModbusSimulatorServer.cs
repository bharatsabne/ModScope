using System.Net;
using System.Net.Sockets;
using ModScope.ModbusMaster;

namespace ModScope.Diagnostics.Simulator;

/// <summary>
/// Built-in in-memory Modbus TCP/RTU server and device simulator for offline testing,
/// commissioning, and integration without requiring physical hardware.
/// </summary>
public class ModbusSimulatorServer : IDisposable
{
    private readonly ushort[] _holdingRegisters = new ushort[10000];
    private readonly ushort[] _inputRegisters = new ushort[10000];
    private readonly bool[] _coils = new bool[10000];
    private readonly bool[] _discreteInputs = new bool[10000];

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private readonly object _syncLock = new();

    public int Port { get; private set; }
    public bool IsRunning { get; private set; }

    public ModbusSimulatorServer()
    {
        InitializeDefaultData();
    }

    private void InitializeDefaultData()
    {
        // Populate standard sample test registers
        // Reg 0: Counter (simulated dynamic value)
        // Reg 1: 0x1234
        // Reg 2-3: IEEE float 123.45f
        _holdingRegisters[0] = 100;
        _holdingRegisters[1] = 0x1234;

        ushort[] floatRegs = ModbusDataConverter.FromSingle(123.45f, EndiannessOrder.BigEndian);
        _holdingRegisters[2] = floatRegs[0];
        _holdingRegisters[3] = floatRegs[1];

        // Reg 4-7: ASCII string "MODBUS"
        ushort[] asciiRegs = ModbusDataConverter.FromAsciiString("MODBUS", 4);
        Array.Copy(asciiRegs, 0, _holdingRegisters, 4, asciiRegs.Length);

        // Input registers
        _inputRegisters[0] = 500;
        _inputRegisters[1] = 750;

        // Discrete Coils & Inputs
        _coils[0] = true;
        _coils[1] = false;
        _coils[2] = true;
        _discreteInputs[0] = true;
        _discreteInputs[1] = true;
    }

    /// <summary>
    /// Starts the simulated Modbus TCP server on the specified port.
    /// </summary>
    public void Start(int port = 5020)
    {
        if (IsRunning) return;

        Port = port;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _cts = new CancellationTokenSource();
        IsRunning = true;

        _listenerTask = Task.Run(() => AcceptClientsAsync(_cts.Token));
    }

    /// <summary>
    /// Stops the simulator server.
    /// </summary>
    public void Stop()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        _listener?.Stop();
        IsRunning = false;
    }

    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(token).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, token), token);
            }
            catch when (token.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Accept error or listener closed
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        using (var stream = client.GetStream())
        {
            byte[] header = new byte[7];
            while (!token.IsCancellationRequested && client.Connected)
            {
                int readHdr = 0;
                while (readHdr < 7)
                {
                    int r = await stream.ReadAsync(header.AsMemory(readHdr, 7 - readHdr), token).ConfigureAwait(false);
                    if (r == 0) return;
                    readHdr += r;
                }

                ushort txId = (ushort)((header[0] << 8) | header[1]);
                ushort proto = (ushort)((header[2] << 8) | header[3]);
                ushort length = (ushort)((header[4] << 8) | header[5]);
                byte unitId = header[6];

                int pduLen = length - 1;
                byte[] pdu = new byte[pduLen];
                int readPdu = 0;
                while (readPdu < pduLen)
                {
                    int r = await stream.ReadAsync(pdu.AsMemory(readPdu, pduLen - readPdu), token).ConfigureAwait(false);
                    if (r == 0) return;
                    readPdu += r;
                }

                byte[] responsePdu = ProcessPdu(pdu);

                // Build MBAP response
                ushort respLen = (ushort)(1 + responsePdu.Length);
                byte[] response = new byte[7 + responsePdu.Length];
                response[0] = (byte)((txId >> 8) & 0xFF);
                response[1] = (byte)(txId & 0xFF);
                response[2] = 0;
                response[3] = 0;
                response[4] = (byte)((respLen >> 8) & 0xFF);
                response[5] = (byte)(respLen & 0xFF);
                response[6] = unitId;
                Buffer.BlockCopy(responsePdu, 0, response, 7, responsePdu.Length);

                await stream.WriteAsync(response, token).ConfigureAwait(false);
            }
        }
    }

    private byte[] ProcessPdu(byte[] pdu)
    {
        byte fc = pdu[0];
        lock (_syncLock)
        {
            switch ((ModbusFunctionCode)fc)
            {
                case ModbusFunctionCode.ReadCoils:
                {
                    ushort start = (ushort)((pdu[1] << 8) | pdu[2]);
                    ushort count = (ushort)((pdu[3] << 8) | pdu[4]);
                    if (count < 1 || count > 2000 || start + count > _coils.Length)
                        return [unchecked((byte)(fc | 0x80)), (byte)ModbusExceptionCode.IllegalDataAddress];

                    byte byteCount = (byte)((count + 7) / 8);
                    byte[] resp = new byte[2 + byteCount];
                    resp[0] = fc;
                    resp[1] = byteCount;
                    for (int i = 0; i < count; i++)
                    {
                        if (_coils[start + i])
                            resp[2 + (i / 8)] |= (byte)(1 << (i % 8));
                    }
                    return resp;
                }

                case ModbusFunctionCode.ReadDiscreteInputs:
                {
                    ushort start = (ushort)((pdu[1] << 8) | pdu[2]);
                    ushort count = (ushort)((pdu[3] << 8) | pdu[4]);
                    if (count < 1 || count > 2000 || start + count > _discreteInputs.Length)
                        return [unchecked((byte)(fc | 0x80)), (byte)ModbusExceptionCode.IllegalDataAddress];

                    byte byteCount = (byte)((count + 7) / 8);
                    byte[] resp = new byte[2 + byteCount];
                    resp[0] = fc;
                    resp[1] = byteCount;
                    for (int i = 0; i < count; i++)
                    {
                        if (_discreteInputs[start + i])
                            resp[2 + (i / 8)] |= (byte)(1 << (i % 8));
                    }
                    return resp;
                }

                case ModbusFunctionCode.ReadHoldingRegisters:
                {
                    ushort start = (ushort)((pdu[1] << 8) | pdu[2]);
                    ushort count = (ushort)((pdu[3] << 8) | pdu[4]);
                    if (count < 1 || count > 125 || start + count > _holdingRegisters.Length)
                        return [unchecked((byte)(fc | 0x80)), (byte)ModbusExceptionCode.IllegalDataAddress];

                    byte byteCount = (byte)(count * 2);
                    byte[] resp = new byte[2 + byteCount];
                    resp[0] = fc;
                    resp[1] = byteCount;
                    for (int i = 0; i < count; i++)
                    {
                        ushort val = _holdingRegisters[start + i];
                        resp[2 + (i * 2)] = (byte)((val >> 8) & 0xFF);
                        resp[2 + (i * 2) + 1] = (byte)(val & 0xFF);
                    }
                    return resp;
                }

                case ModbusFunctionCode.ReadInputRegisters:
                {
                    ushort start = (ushort)((pdu[1] << 8) | pdu[2]);
                    ushort count = (ushort)((pdu[3] << 8) | pdu[4]);
                    if (count < 1 || count > 125 || start + count > _inputRegisters.Length)
                        return [unchecked((byte)(fc | 0x80)), (byte)ModbusExceptionCode.IllegalDataAddress];

                    byte byteCount = (byte)(count * 2);
                    byte[] resp = new byte[2 + byteCount];
                    resp[0] = fc;
                    resp[1] = byteCount;
                    for (int i = 0; i < count; i++)
                    {
                        ushort val = _inputRegisters[start + i];
                        resp[2 + (i * 2)] = (byte)((val >> 8) & 0xFF);
                        resp[2 + (i * 2) + 1] = (byte)(val & 0xFF);
                    }
                    return resp;
                }

                case ModbusFunctionCode.WriteSingleCoil:
                {
                    ushort addr = (ushort)((pdu[1] << 8) | pdu[2]);
                    ushort val = (ushort)((pdu[3] << 8) | pdu[4]);
                    if (addr >= _coils.Length)
                        return [unchecked((byte)(fc | 0x80)), (byte)ModbusExceptionCode.IllegalDataAddress];

                    _coils[addr] = val == 0xFF00;
                    return pdu; // Echo request
                }

                case ModbusFunctionCode.WriteSingleRegister:
                {
                    ushort addr = (ushort)((pdu[1] << 8) | pdu[2]);
                    ushort val = (ushort)((pdu[3] << 8) | pdu[4]);
                    if (addr >= _holdingRegisters.Length)
                        return [unchecked((byte)(fc | 0x80)), (byte)ModbusExceptionCode.IllegalDataAddress];

                    _holdingRegisters[addr] = val;
                    return pdu; // Echo request
                }

                case ModbusFunctionCode.WriteMultipleRegisters:
                {
                    ushort start = (ushort)((pdu[1] << 8) | pdu[2]);
                    ushort count = (ushort)((pdu[3] << 8) | pdu[4]);
                    byte byteCount = pdu[5];
                    if (count < 1 || count > 123 || start + count > _holdingRegisters.Length)
                        return [unchecked((byte)(fc | 0x80)), (byte)ModbusExceptionCode.IllegalDataAddress];

                    for (int i = 0; i < count; i++)
                    {
                        _holdingRegisters[start + i] = (ushort)((pdu[6 + (i * 2)] << 8) | pdu[6 + (i * 2) + 1]);
                    }
                    return [fc, pdu[1], pdu[2], pdu[3], pdu[4]];
                }

                default:
                    return [unchecked((byte)(fc | 0x80)), (byte)ModbusExceptionCode.IllegalFunction];
            }
        }
    }

    public ushort GetHoldingRegister(ushort address) => _holdingRegisters[address];
    public void SetHoldingRegister(ushort address, ushort val) => _holdingRegisters[address] = val;
    public bool GetCoil(ushort address) => _coils[address];
    public void SetCoil(ushort address, bool val) => _coils[address] = val;

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
