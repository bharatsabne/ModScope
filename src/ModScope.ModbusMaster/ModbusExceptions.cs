namespace ModScope.ModbusMaster;

/// <summary>
/// Exception thrown when a Modbus slave returns an error response (FC + 0x80).
/// </summary>
public class ModbusProtocolException : Exception
{
    /// <summary>
    /// The Modbus function code of the failed request.
    /// </summary>
    public byte FunctionCode { get; }

    /// <summary>
    /// The Modbus exception code returned by the slave.
    /// </summary>
    public ModbusExceptionCode ExceptionCode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ModbusProtocolException"/>.
    /// </summary>
    public ModbusProtocolException(byte functionCode, ModbusExceptionCode exceptionCode)
        : base($"Modbus exception response: FC=0x{functionCode:X2}, Error=0x{(byte)exceptionCode:X2} ({exceptionCode})")
    {
        FunctionCode = functionCode;
        ExceptionCode = exceptionCode;
    }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    public ModbusProtocolException(byte functionCode, ModbusExceptionCode exceptionCode, string message)
        : base(message)
    {
        FunctionCode = functionCode;
        ExceptionCode = exceptionCode;
    }
}

/// <summary>
/// Exception thrown when Modbus frame checksum (CRC-16) validation fails.
/// </summary>
public class ModbusChecksumException : Exception
{
    /// <summary>Expected CRC value calculated from frame payload.</summary>
    public ushort ExpectedCrc { get; }

    /// <summary>Actual CRC value read from the frame.</summary>
    public ushort ActualCrc { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ModbusChecksumException"/>.
    /// </summary>
    public ModbusChecksumException(ushort expectedCrc, ushort actualCrc)
        : base($"Modbus CRC-16 mismatch: expected 0x{expectedCrc:X4}, got 0x{actualCrc:X4}")
    {
        ExpectedCrc = expectedCrc;
        ActualCrc = actualCrc;
    }
}

/// <summary>
/// Exception thrown when Modbus transaction times out.
/// </summary>
public class ModbusTimeoutException : TimeoutException
{
    /// <summary>
    /// Initializes a new instance of <see cref="ModbusTimeoutException"/>.
    /// </summary>
    public ModbusTimeoutException(string message) : base(message) { }
}
