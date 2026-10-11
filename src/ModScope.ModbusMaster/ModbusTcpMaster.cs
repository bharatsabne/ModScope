namespace ModScope.ModbusMaster;

/// <summary>
/// High-performance Modbus TCP Master implementation adhering to RFC / Modbus Messaging on TCP/IP Implementation Guide.
/// Supports MBAP header management, transaction IDs, unit IDs, and raw frame diagnostics.
/// </summary>
public class ModbusTcpMaster : IModbusMaster
{
    private readonly IModbusTransport _transport;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ushort _transactionId;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<ModbusRawFrame>? FrameCaptured;

    /// <inheritdoc/>
    public bool IsConnected => _transport.IsConnected;

    /// <summary>
    /// Creates a new instance of <see cref="ModbusTcpMaster"/>.
    /// </summary>
    /// <param name="transport">Underlying transport channel (e.g. <see cref="ModbusTcpTransport"/>).</param>
    public ModbusTcpMaster(IModbusTransport transport)
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

        byte[] pdu =
        [
            (byte)ModbusFunctionCode.ReadCoils,
            (byte)((startAddress >> 8) & 0xFF),
            (byte)(startAddress & 0xFF),
            (byte)((count >> 8) & 0xFF),
            (byte)(count & 0xFF)
        ];

        byte[] responsePdu = await ExecuteMbapTransactionAsync(slaveId, pdu, "Read Coils (FC01)", cancellationToken).ConfigureAwait(false);
        int expectedBytes = (count + 7) / 8;
        if (responsePdu.Length < 2 + expectedBytes)
            throw new InvalidOperationException("Modbus TCP response PDU is shorter than expected.");

        bool[] result = new bool[count];
        for (int i = 0; i < count; i++)
        {
            byte b = responsePdu[2 + (i / 8)];
            result[i] = ((b >> (i % 8)) & 0x01) == 1;
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 2000)
            throw new ArgumentOutOfRangeException(nameof(count), "Modbus FC02 supports 1 to 2000 inputs.");

        byte[] pdu =
        [
            (byte)ModbusFunctionCode.ReadDiscreteInputs,
            (byte)((startAddress >> 8) & 0xFF),
            (byte)(startAddress & 0xFF),
            (byte)((count >> 8) & 0xFF),
            (byte)(count & 0xFF)
        ];

        byte[] responsePdu = await ExecuteMbapTransactionAsync(slaveId, pdu, "Read Discrete Inputs (FC02)", cancellationToken).ConfigureAwait(false);
        int expectedBytes = (count + 7) / 8;
        if (responsePdu.Length < 2 + expectedBytes)
            throw new InvalidOperationException("Modbus TCP response PDU is shorter than expected.");

        bool[] result = new bool[count];
        for (int i = 0; i < count; i++)
        {
            byte b = responsePdu[2 + (i / 8)];
            result[i] = ((b >> (i % 8)) & 0x01) == 1;
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 125)
            throw new ArgumentOutOfRangeException(nameof(count), "Modbus FC03 supports 1 to 125 registers.");

        byte[] pdu =
        [
            (byte)ModbusFunctionCode.ReadHoldingRegisters,
            (byte)((startAddress >> 8) & 0xFF),
            (byte)(startAddress & 0xFF),
            (byte)((count >> 8) & 0xFF),
            (byte)(count & 0xFF)
        ];

        byte[] responsePdu = await ExecuteMbapTransactionAsync(slaveId, pdu, "Read Holding Registers (FC03)", cancellationToken).ConfigureAwait(false);
        ushort[] registers = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            int offset = 2 + (i * 2);
            registers[i] = (ushort)((responsePdu[offset] << 8) | responsePdu[offset + 1]);
        }
        return registers;
    }

    /// <inheritdoc/>
    public async Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 125)
            throw new ArgumentOutOfRangeException(nameof(count), "Modbus FC04 supports 1 to 125 registers.");

        byte[] pdu =
        [
            (byte)ModbusFunctionCode.ReadInputRegisters,
            (byte)((startAddress >> 8) & 0xFF),
            (byte)(startAddress & 0xFF),
            (byte)((count >> 8) & 0xFF),
            (byte)(count & 0xFF)
        ];

        byte[] responsePdu = await ExecuteMbapTransactionAsync(slaveId, pdu, "Read Input Registers (FC04)", cancellationToken).ConfigureAwait(false);
        ushort[] registers = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            int offset = 2 + (i * 2);
            registers[i] = (ushort)((responsePdu[offset] << 8) | responsePdu[offset + 1]);
        }
        return registers;
    }

    /// <inheritdoc/>
    public async Task WriteSingleCoilAsync(byte slaveId, ushort coilAddress, bool value, CancellationToken cancellationToken = default)
    {
        ushort val = value ? (ushort)0xFF00 : (ushort)0x0000;
        byte[] pdu =
        [
            (byte)ModbusFunctionCode.WriteSingleCoil,
            (byte)((coilAddress >> 8) & 0xFF),
            (byte)(coilAddress & 0xFF),
            (byte)((val >> 8) & 0xFF),
            (byte)(val & 0xFF)
        ];

        await ExecuteMbapTransactionAsync(slaveId, pdu, $"Write Single Coil (FC05: Addr {coilAddress}={(value ? "ON" : "OFF")})", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteSingleRegisterAsync(byte slaveId, ushort registerAddress, ushort value, CancellationToken cancellationToken = default)
    {
        byte[] pdu =
        [
            (byte)ModbusFunctionCode.WriteSingleRegister,
            (byte)((registerAddress >> 8) & 0xFF),
            (byte)(registerAddress & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF)
        ];

        await ExecuteMbapTransactionAsync(slaveId, pdu, $"Write Single Register (FC06: Addr {registerAddress}=0x{value:X4})", cancellationToken).ConfigureAwait(false);
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

        await ExecuteMbapTransactionAsync(slaveId, pdu, $"Write Multiple Coils (FC15: {values.Length} coils)", cancellationToken).ConfigureAwait(false);
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

        await ExecuteMbapTransactionAsync(slaveId, pdu, $"Write Multiple Registers (FC16: {values.Length} regs)", cancellationToken).ConfigureAwait(false);
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

        await ExecuteMbapTransactionAsync(slaveId, pdu, $"Mask Write Register (FC22: Addr {address})", cancellationToken).ConfigureAwait(false);
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

        byte[] responsePdu = await ExecuteMbapTransactionAsync(slaveId, pdu, "Read/Write Multiple Registers (FC23)", cancellationToken).ConfigureAwait(false);
        ushort[] registers = new ushort[readCount];
        for (int i = 0; i < readCount; i++)
        {
            int offset = 2 + (i * 2);
            registers[i] = (ushort)((responsePdu[offset] << 8) | responsePdu[offset + 1]);
        }
        return registers;
    }

    private async Task<byte[]> ExecuteMbapTransactionAsync(
        byte unitId,
        byte[] requestPdu,
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

            ushort txId = unchecked(++_transactionId);
            ushort protocolId = 0; // Modbus protocol identifier = 0
            ushort lengthField = (ushort)(1 + requestPdu.Length); // UnitId + PDU length

            byte[] mbapFrame = new byte[6 + lengthField];
            mbapFrame[0] = (byte)((txId >> 8) & 0xFF);
            mbapFrame[1] = (byte)(txId & 0xFF);
            mbapFrame[2] = (byte)((protocolId >> 8) & 0xFF);
            mbapFrame[3] = (byte)(protocolId & 0xFF);
            mbapFrame[4] = (byte)((lengthField >> 8) & 0xFF);
            mbapFrame[5] = (byte)(lengthField & 0xFF);
            mbapFrame[6] = unitId;
            Buffer.BlockCopy(requestPdu, 0, mbapFrame, 7, requestPdu.Length);

            FrameCaptured?.Invoke(this, new ModbusRawFrame(
                DateTime.UtcNow,
                ModbusFrameDirection.Tx,
                mbapFrame,
                $"[TxId {txId}] {description}"));

            await _transport.WriteAsync(mbapFrame, 0, mbapFrame.Length, cancellationToken).ConfigureAwait(false);

            // Read MBAP Header (first 7 bytes: TxId(2), Proto(2), Length(2), UnitId(1))
            byte[] mbapHeader = new byte[7];
            int readHdr = await _transport.ReadAsync(mbapHeader, 0, 7, cancellationToken).ConfigureAwait(false);
            if (readHdr < 7)
            {
                throw new ModbusTimeoutException($"Modbus TCP timeout reading MBAP header from Unit {unitId}.");
            }

            ushort respTxId = (ushort)((mbapHeader[0] << 8) | mbapHeader[1]);
            ushort respProto = (ushort)((mbapHeader[2] << 8) | mbapHeader[3]);
            ushort respLength = (ushort)((mbapHeader[4] << 8) | mbapHeader[5]);
            byte respUnitId = mbapHeader[6];

            if (respTxId != txId)
            {
                throw new InvalidOperationException($"Modbus TCP transaction ID mismatch: requested {txId}, received {respTxId}.");
            }

            if (respLength < 2)
            {
                throw new InvalidOperationException($"Invalid Modbus TCP length field: {respLength}.");
            }

            // Read PDU: Length field includes UnitId (which we already read in byte 6), so PDU size is (respLength - 1)
            int pduLength = respLength - 1;
            byte[] pduBuffer = new byte[pduLength];
            await _transport.ReadAsync(pduBuffer, 0, pduLength, cancellationToken).ConfigureAwait(false);

            byte[] fullRxFrame = new byte[7 + pduLength];
            Buffer.BlockCopy(mbapHeader, 0, fullRxFrame, 0, 7);
            Buffer.BlockCopy(pduBuffer, 0, fullRxFrame, 7, pduLength);

            // Check if response is Modbus Exception
            if ((pduBuffer[0] & 0x80) != 0)
            {
                FrameCaptured?.Invoke(this, new ModbusRawFrame(
                    DateTime.UtcNow,
                    ModbusFrameDirection.Rx,
                    fullRxFrame,
                    $"[TxId {txId}] Exception FC=0x{pduBuffer[0]:X2}",
                    IsError: true));

                throw new ModbusProtocolException((byte)(pduBuffer[0] & 0x7F), (ModbusExceptionCode)pduBuffer[1]);
            }

            FrameCaptured?.Invoke(this, new ModbusRawFrame(
                DateTime.UtcNow,
                ModbusFrameDirection.Rx,
                fullRxFrame,
                $"[TxId {txId}] {description}"));

            return pduBuffer;
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
