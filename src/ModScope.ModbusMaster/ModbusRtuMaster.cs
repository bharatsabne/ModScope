namespace ModScope.ModbusMaster;

/// <summary>
/// High-reliability Modbus RTU master implementing binary framing, CRC-16 validation, inter-frame timeouts, and error handling.
/// </summary>
public class ModbusRtuMaster : IModbusMaster
{
    private readonly IModbusTransport _transport;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<ModbusRawFrame>? FrameCaptured;

    /// <inheritdoc/>
    public bool IsConnected => _transport.IsConnected;

    /// <summary>
    /// Creates a new instance of <see cref="ModbusRtuMaster"/>.
    /// </summary>
    /// <param name="transport">Underlying transport channel (e.g. <see cref="ModbusSerialTransport"/>).</param>
    public ModbusRtuMaster(IModbusTransport transport)
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

        int expectedByteCount = (count + 7) / 8;
        byte[] responsePayload = await ExecuteTransactionAsync(
            slaveId,
            requestPdu,
            expectedResponseLength: 1 + 1 + 1 + expectedByteCount + 2, // slave + FC + byteCount + bytes + CRC
            "Read Coils (FC01)",
            cancellationToken).ConfigureAwait(false);

        byte responseByteCount = responsePayload[2];
        bool[] result = new bool[count];
        for (int i = 0; i < count; i++)
        {
            byte b = responsePayload[3 + (i / 8)];
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

        int expectedByteCount = (count + 7) / 8;
        byte[] responsePayload = await ExecuteTransactionAsync(
            slaveId,
            requestPdu,
            expectedResponseLength: 1 + 1 + 1 + expectedByteCount + 2,
            "Read Discrete Inputs (FC02)",
            cancellationToken).ConfigureAwait(false);

        bool[] result = new bool[count];
        for (int i = 0; i < count; i++)
        {
            byte b = responsePayload[3 + (i / 8)];
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

        int expectedBytes = count * 2;
        byte[] responsePayload = await ExecuteTransactionAsync(
            slaveId,
            requestPdu,
            expectedResponseLength: 1 + 1 + 1 + expectedBytes + 2,
            "Read Holding Registers (FC03)",
            cancellationToken).ConfigureAwait(false);

        ushort[] registers = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            int offset = 3 + (i * 2);
            registers[i] = (ushort)((responsePayload[offset] << 8) | responsePayload[offset + 1]);
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

        int expectedBytes = count * 2;
        byte[] responsePayload = await ExecuteTransactionAsync(
            slaveId,
            requestPdu,
            expectedResponseLength: 1 + 1 + 1 + expectedBytes + 2,
            "Read Input Registers (FC04)",
            cancellationToken).ConfigureAwait(false);

        ushort[] registers = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            int offset = 3 + (i * 2);
            registers[i] = (ushort)((responsePayload[offset] << 8) | responsePayload[offset + 1]);
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

        await ExecuteTransactionAsync(
            slaveId,
            requestPdu,
            expectedResponseLength: 8, // Slave + FC + Addr(2) + Value(2) + CRC(2)
            $"Write Single Coil (FC05: Addr {coilAddress}={(value ? "ON" : "OFF")})",
            cancellationToken).ConfigureAwait(false);
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

        await ExecuteTransactionAsync(
            slaveId,
            requestPdu,
            expectedResponseLength: 8,
            $"Write Single Register (FC06: Addr {registerAddress}=0x{value:X4})",
            cancellationToken).ConfigureAwait(false);
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

        await ExecuteTransactionAsync(
            slaveId,
            pdu,
            expectedResponseLength: 8, // Slave + FC + Addr(2) + Count(2) + CRC(2)
            $"Write Multiple Coils (FC15: {values.Length} coils)",
            cancellationToken).ConfigureAwait(false);
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

        await ExecuteTransactionAsync(
            slaveId,
            pdu,
            expectedResponseLength: 8, // Slave + FC + Addr(2) + Count(2) + CRC(2)
            $"Write Multiple Registers (FC16: {values.Length} regs)",
            cancellationToken).ConfigureAwait(false);
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

        await ExecuteTransactionAsync(
            slaveId,
            pdu,
            expectedResponseLength: 10,
            $"Mask Write Register (FC22: Addr {address})",
            cancellationToken).ConfigureAwait(false);
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

        int expectedBytes = readCount * 2;
        byte[] responsePayload = await ExecuteTransactionAsync(
            slaveId,
            pdu,
            expectedResponseLength: 1 + 1 + 1 + expectedBytes + 2,
            $"Read/Write Multiple Registers (FC23)",
            cancellationToken).ConfigureAwait(false);

        ushort[] registers = new ushort[readCount];
        for (int i = 0; i < readCount; i++)
        {
            int offset = 3 + (i * 2);
            registers[i] = (ushort)((responsePayload[offset] << 8) | responsePayload[offset + 1]);
        }
        return registers;
    }

    private async Task<byte[]> ExecuteTransactionAsync(
        byte slaveId,
        byte[] pdu,
        int expectedResponseLength,
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

            // Assemble frame: [SlaveId] + [PDU] + [CRC16 Low, High]
            byte[] rawRequest = new byte[1 + pdu.Length];
            rawRequest[0] = slaveId;
            Buffer.BlockCopy(pdu, 0, rawRequest, 1, pdu.Length);
            byte[] fullTxFrame = ModbusCrc.AppendCrc(rawRequest);

            FrameCaptured?.Invoke(this, new ModbusRawFrame(
                DateTime.UtcNow,
                ModbusFrameDirection.Tx,
                fullTxFrame,
                description));

            await _transport.WriteAsync(fullTxFrame, 0, fullTxFrame.Length, cancellationToken).ConfigureAwait(false);

            // Read response: first 3 bytes (SlaveId, FunctionCode, ByteCount or first data byte)
            byte[] header = new byte[3];
            int readHdr = await _transport.ReadAsync(header, 0, 3, cancellationToken).ConfigureAwait(false);
            if (readHdr < 3)
            {
                throw new ModbusTimeoutException($"RTU timeout waiting for header from Slave {slaveId}.");
            }

            // Check for Exception response (FC with MSB set: FC >= 0x80)
            if ((header[1] & 0x80) != 0)
            {
                // Modbus exception response is 5 bytes total: [SlaveId, FC+0x80, ExceptionCode, CRC-Lo, CRC-Hi]
                byte[] excRest = new byte[2]; // Remaining CRC bytes
                await _transport.ReadAsync(excRest, 0, 2, cancellationToken).ConfigureAwait(false);

                byte[] excFrame = [header[0], header[1], header[2], excRest[0], excRest[1]];

                FrameCaptured?.Invoke(this, new ModbusRawFrame(
                    DateTime.UtcNow,
                    ModbusFrameDirection.Rx,
                    excFrame,
                    $"Exception Response FC=0x{header[1]:X2}",
                    IsError: true));

                if (!ModbusCrc.Validate(excFrame))
                {
                    throw new ModbusChecksumException(0, 0);
                }

                throw new ModbusProtocolException((byte)(header[1] & 0x7F), (ModbusExceptionCode)header[2]);
            }

            // Normal response: read the remainder of the expected frame
            int remaining = expectedResponseLength - 3;
            byte[] remainder = new byte[remaining];
            await _transport.ReadAsync(remainder, 0, remaining, cancellationToken).ConfigureAwait(false);

            byte[] fullRxFrame = new byte[expectedResponseLength];
            Buffer.BlockCopy(header, 0, fullRxFrame, 0, 3);
            Buffer.BlockCopy(remainder, 0, fullRxFrame, 3, remaining);

            FrameCaptured?.Invoke(this, new ModbusRawFrame(
                DateTime.UtcNow,
                ModbusFrameDirection.Rx,
                fullRxFrame,
                description));

            // Validate CRC
            if (!ModbusCrc.Validate(fullRxFrame))
            {
                ushort calculated = ModbusCrc.Compute(fullRxFrame.AsSpan(0, fullRxFrame.Length - 2));
                ushort received = (ushort)(fullRxFrame[^2] | (fullRxFrame[^1] << 8));
                throw new ModbusChecksumException(calculated, received);
            }

            // Validate Slave ID and Function Code
            if (fullRxFrame[0] != slaveId)
            {
                throw new InvalidOperationException($"Slave ID mismatch: requested {slaveId}, received {fullRxFrame[0]}.");
            }

            if (fullRxFrame[1] != pdu[0])
            {
                throw new InvalidOperationException($"Function Code mismatch: expected 0x{pdu[0]:X2}, received 0x{fullRxFrame[1]:X2}.");
            }

            return fullRxFrame;
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
