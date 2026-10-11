namespace ModScope.ModbusMaster;

/// <summary>
/// High-reliability Modbus ASCII master implementing serial ASCII framing (: ... LRC \r\n), LRC validation, and transaction handling.
/// </summary>
public class ModbusAsciiMaster : IModbusMaster
{
    private readonly IModbusTransport _transport;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<ModbusRawFrame>? FrameCaptured;

    /// <inheritdoc/>
    public bool IsConnected => _transport.IsConnected;

    /// <summary>
    /// Creates a new instance of <see cref="ModbusAsciiMaster"/>.
    /// </summary>
    /// <param name="transport">Underlying serial transport channel.</param>
    public ModbusAsciiMaster(IModbusTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <inheritdoc/>
    public Task ConnectAsync(CancellationToken cancellationToken = default) => _transport.ConnectAsync(cancellationToken);

    /// <inheritdoc/>
    public Task DisconnectAsync() => _transport.DisconnectAsync();

    /// <inheritdoc/>
    public async Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 2000)
            throw new ArgumentOutOfRangeException(nameof(count), "Modbus FC01 supports 1 to 2000 coils.");

        byte[] requestPdu =
        [
            (byte)ModbusFunctionCode.ReadCoils,
            (byte)((startAddress >> 8) & 0xFF),
            (byte)(startAddress & 0xFF),
            (byte)((count >> 8) & 0xFF),
            (byte)(count & 0xFF)
        ];

        byte[] responseBinary = await ExecuteAsciiTransactionAsync(slaveId, requestPdu, "Read Coils (FC01 ASCII)", cancellationToken).ConfigureAwait(false);

        // Binary response is: [SlaveId, FC, ByteCount, Bytes...]
        int byteCount = responseBinary[2];
        bool[] result = new bool[count];
        for (int i = 0; i < count; i++)
        {
            byte b = responseBinary[3 + (i / 8)];
            result[i] = ((b >> (i % 8)) & 0x01) == 1;
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 2000)
            throw new ArgumentOutOfRangeException(nameof(count), "Modbus FC02 supports 1 to 2000 inputs.");

        byte[] requestPdu =
        [
            (byte)ModbusFunctionCode.ReadDiscreteInputs,
            (byte)((startAddress >> 8) & 0xFF),
            (byte)(startAddress & 0xFF),
            (byte)((count >> 8) & 0xFF),
            (byte)(count & 0xFF)
        ];

        byte[] responseBinary = await ExecuteAsciiTransactionAsync(slaveId, requestPdu, "Read Discrete Inputs (FC02 ASCII)", cancellationToken).ConfigureAwait(false);

        bool[] result = new bool[count];
        for (int i = 0; i < count; i++)
        {
            byte b = responseBinary[3 + (i / 8)];
            result[i] = ((b >> (i % 8)) & 0x01) == 1;
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 125)
            throw new ArgumentOutOfRangeException(nameof(count), "Modbus FC03 supports 1 to 125 registers.");

        byte[] requestPdu =
        [
            (byte)ModbusFunctionCode.ReadHoldingRegisters,
            (byte)((startAddress >> 8) & 0xFF),
            (byte)(startAddress & 0xFF),
            (byte)((count >> 8) & 0xFF),
            (byte)(count & 0xFF)
        ];

        byte[] responseBinary = await ExecuteAsciiTransactionAsync(slaveId, requestPdu, "Read Holding Registers (FC03 ASCII)", cancellationToken).ConfigureAwait(false);

        ushort[] registers = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            int offset = 3 + (i * 2);
            registers[i] = (ushort)((responseBinary[offset] << 8) | responseBinary[offset + 1]);
        }
        return registers;
    }

    /// <inheritdoc/>
    public async Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 125)
            throw new ArgumentOutOfRangeException(nameof(count), "Modbus FC04 supports 1 to 125 registers.");

        byte[] requestPdu =
        [
            (byte)ModbusFunctionCode.ReadInputRegisters,
            (byte)((startAddress >> 8) & 0xFF),
            (byte)(startAddress & 0xFF),
            (byte)((count >> 8) & 0xFF),
            (byte)(count & 0xFF)
        ];

        byte[] responseBinary = await ExecuteAsciiTransactionAsync(slaveId, requestPdu, "Read Input Registers (FC04 ASCII)", cancellationToken).ConfigureAwait(false);

        ushort[] registers = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            int offset = 3 + (i * 2);
            registers[i] = (ushort)((responseBinary[offset] << 8) | responseBinary[offset + 1]);
        }
        return registers;
    }

    /// <inheritdoc/>
    public async Task WriteSingleCoilAsync(byte slaveId, ushort coilAddress, bool value, CancellationToken cancellationToken = default)
    {
        ushort val = value ? (ushort)0xFF00 : (ushort)0x0000;
        byte[] requestPdu =
        [
            (byte)ModbusFunctionCode.WriteSingleCoil,
            (byte)((coilAddress >> 8) & 0xFF),
            (byte)(coilAddress & 0xFF),
            (byte)((val >> 8) & 0xFF),
            (byte)(val & 0xFF)
        ];

        await ExecuteAsciiTransactionAsync(slaveId, requestPdu, $"Write Single Coil (FC05 ASCII: Addr {coilAddress}={(value ? "ON" : "OFF")})", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteSingleRegisterAsync(byte slaveId, ushort registerAddress, ushort value, CancellationToken cancellationToken = default)
    {
        byte[] requestPdu =
        [
            (byte)ModbusFunctionCode.WriteSingleRegister,
            (byte)((registerAddress >> 8) & 0xFF),
            (byte)(registerAddress & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF)
        ];

        await ExecuteAsciiTransactionAsync(slaveId, requestPdu, $"Write Single Register (FC06 ASCII: Addr {registerAddress}=0x{value:X4})", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteMultipleCoilsAsync(byte slaveId, ushort startAddress, bool[] values, CancellationToken cancellationToken = default)
    {
        if (values == null || values.Length == 0 || values.Length > 1968)
            throw new ArgumentOutOfRangeException(nameof(values), "Modbus FC15 supports 1 to 1968 coils.");

        byte byteCount = (byte)((values.Length + 7) / 8);
        byte[] pdu = new byte[6 + byteCount];
        pdu[0] = (byte)ModbusFunctionCode.WriteMultipleCoils;
        pdu[1] = (byte)((startAddress >> 8) & 0xFF);
        pdu[2] = (byte)(startAddress & 0xFF);
        pdu[3] = (byte)((values.Length >> 8) & 0xFF);
        pdu[4] = (byte)(values.Length & 0xFF);
        pdu[5] = byteCount;

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i])
            {
                pdu[6 + (i / 8)] |= (byte)(1 << (i % 8));
            }
        }

        await ExecuteAsciiTransactionAsync(slaveId, pdu, $"Write Multiple Coils (FC15 ASCII: {values.Length} coils)", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default)
    {
        if (values == null || values.Length == 0 || values.Length > 123)
            throw new ArgumentOutOfRangeException(nameof(values), "Modbus FC16 supports 1 to 123 registers.");

        byte byteCount = (byte)(values.Length * 2);
        byte[] pdu = new byte[6 + byteCount];
        pdu[0] = (byte)ModbusFunctionCode.WriteMultipleRegisters;
        pdu[1] = (byte)((startAddress >> 8) & 0xFF);
        pdu[2] = (byte)(startAddress & 0xFF);
        pdu[3] = (byte)((values.Length >> 8) & 0xFF);
        pdu[4] = (byte)(values.Length & 0xFF);
        pdu[5] = byteCount;

        for (int i = 0; i < values.Length; i++)
        {
            pdu[6 + (i * 2)] = (byte)((values[i] >> 8) & 0xFF);
            pdu[6 + (i * 2) + 1] = (byte)(values[i] & 0xFF);
        }

        await ExecuteAsciiTransactionAsync(slaveId, pdu, $"Write Multiple Registers (FC16 ASCII: {values.Length} regs)", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MaskWriteRegisterAsync(byte slaveId, ushort address, ushort andMask, ushort orMask, CancellationToken cancellationToken = default)
    {
        byte[] pdu =
        [
            (byte)ModbusFunctionCode.MaskWriteRegister,
            (byte)((address >> 8) & 0xFF),
            (byte)(address & 0xFF),
            (byte)((andMask >> 8) & 0xFF),
            (byte)(andMask & 0xFF),
            (byte)((orMask >> 8) & 0xFF),
            (byte)(orMask & 0xFF)
        ];

        await ExecuteAsciiTransactionAsync(slaveId, pdu, $"Mask Write Register (FC22 ASCII: Addr {address})", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ushort[]> ReadWriteMultipleRegistersAsync(
        byte slaveId,
        ushort readStart,
        ushort readCount,
        ushort writeStart,
        ushort[] writeValues,
        CancellationToken cancellationToken = default)
    {
        if (readCount < 1 || readCount > 125)
            throw new ArgumentOutOfRangeException(nameof(readCount), "Modbus FC23 read count must be 1 to 125.");
        if (writeValues == null || writeValues.Length < 1 || writeValues.Length > 121)
            throw new ArgumentOutOfRangeException(nameof(writeValues), "Modbus FC23 write count must be 1 to 121.");

        byte byteCount = (byte)(writeValues.Length * 2);
        byte[] pdu = new byte[10 + byteCount];
        pdu[0] = (byte)ModbusFunctionCode.ReadWriteMultipleRegisters;
        pdu[1] = (byte)((readStart >> 8) & 0xFF);
        pdu[2] = (byte)(readStart & 0xFF);
        pdu[3] = (byte)((readCount >> 8) & 0xFF);
        pdu[4] = (byte)(readCount & 0xFF);
        pdu[5] = (byte)((writeStart >> 8) & 0xFF);
        pdu[6] = (byte)(writeStart & 0xFF);
        pdu[7] = (byte)((writeValues.Length >> 8) & 0xFF);
        pdu[8] = (byte)(writeValues.Length & 0xFF);
        pdu[9] = byteCount;

        for (int i = 0; i < writeValues.Length; i++)
        {
            pdu[10 + (i * 2)] = (byte)((writeValues[i] >> 8) & 0xFF);
            pdu[10 + (i * 2) + 1] = (byte)(writeValues[i] & 0xFF);
        }

        byte[] responseBinary = await ExecuteAsciiTransactionAsync(slaveId, pdu, "Read/Write Multiple Registers (FC23 ASCII)", cancellationToken).ConfigureAwait(false);
        ushort[] registers = new ushort[readCount];
        for (int i = 0; i < readCount; i++)
        {
            int offset = 3 + (i * 2);
            registers[i] = (ushort)((responseBinary[offset] << 8) | responseBinary[offset + 1]);
        }
        return registers;
    }

    private async Task<byte[]> ExecuteAsciiTransactionAsync(
        byte slaveId,
        byte[] pdu,
        string description,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_transport.IsConnected)
            {
                await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);
            }

            _transport.DiscardInBuffer();

            // Construct binary packet: [SlaveId] + [PDU]
            byte[] binaryRequest = new byte[1 + pdu.Length];
            binaryRequest[0] = slaveId;
            Buffer.BlockCopy(pdu, 0, binaryRequest, 1, pdu.Length);

            // Encode to ASCII frame with ':' and LRC and CRLF
            byte[] asciiTxFrame = ModbusLrc.EncodeAsciiFrame(binaryRequest);

            FrameCaptured?.Invoke(this, new ModbusRawFrame(
                DateTime.UtcNow,
                ModbusFrameDirection.Tx,
                asciiTxFrame,
                description));

            await _transport.WriteAsync(asciiTxFrame, 0, asciiTxFrame.Length, cancellationToken).ConfigureAwait(false);

            // Read ASCII response until '\n' (0x0A)
            List<byte> responseBuffer = new();
            byte[] single = new byte[1];
            bool foundColon = false;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_transport.TimeoutMs);

            while (!cts.IsCancellationRequested)
            {
                int r = await _transport.ReadAsync(single, 0, 1, cts.Token).ConfigureAwait(false);
                if (r == 0) break;

                byte b = single[0];
                if (!foundColon)
                {
                    if (b == (byte)':')
                    {
                        foundColon = true;
                        responseBuffer.Add(b);
                    }
                }
                else
                {
                    responseBuffer.Add(b);
                    if (b == 0x0A) // CRLF reached
                    {
                        break;
                    }
                }
            }

            if (responseBuffer.Count < 7)
            {
                throw new ModbusTimeoutException($"Modbus ASCII timeout waiting for response from Slave {slaveId}.");
            }

            byte[] fullRxAscii = responseBuffer.ToArray();

            // Decode and validate LRC
            byte[] binaryResponse = ModbusLrc.DecodeAsciiFrame(fullRxAscii);

            // Check for Exception response (FC with MSB set)
            if ((binaryResponse[1] & 0x80) != 0)
            {
                FrameCaptured?.Invoke(this, new ModbusRawFrame(
                    DateTime.UtcNow,
                    ModbusFrameDirection.Rx,
                    fullRxAscii,
                    $"ASCII Exception FC=0x{binaryResponse[1]:X2}",
                    IsError: true));

                throw new ModbusProtocolException((byte)(binaryResponse[1] & 0x7F), (ModbusExceptionCode)binaryResponse[2]);
            }

            FrameCaptured?.Invoke(this, new ModbusRawFrame(
                DateTime.UtcNow,
                ModbusFrameDirection.Rx,
                fullRxAscii,
                description));

            return binaryResponse;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _lock.Dispose();
            _transport.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
