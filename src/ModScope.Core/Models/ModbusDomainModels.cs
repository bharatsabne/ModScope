namespace ModScope.Core.Models;

/// <summary>
/// Modbus table type.
/// </summary>
public enum ModbusTableType
{
    Coil,
    DiscreteInput,
    HoldingRegister,
    InputRegister
}

/// <summary>
/// Display / data decoding format for registers.
/// </summary>
public enum DataDisplayFormat
{
    /// <summary>Raw 16-bit Unsigned Decimal (0 - 65535)</summary>
    UInt16,

    /// <summary>16-bit Signed Decimal (-32768 - 32767)</summary>
    Int16,

    /// <summary>Hexadecimal (0x0000 - 0xFFFF)</summary>
    Hexadecimal,

    /// <summary>Binary (16 bits: 00000000 00000000)</summary>
    Binary,

    /// <summary>32-bit Float (Single IEEE-754, 2 consecutive registers)</summary>
    Float32,

    /// <summary>64-bit Double precision IEEE-754 (4 consecutive registers, first 8 bytes)</summary>
    Double64,

    /// <summary>32-bit Signed Integer (Int32, 2 registers)</summary>
    Int32,

    /// <summary>32-bit Unsigned Integer (UInt32, 2 registers)</summary>
    UInt32,

    /// <summary>64-bit Signed Integer (Int64, 4 registers, first 8 bytes)</summary>
    Int64,

    /// <summary>ASCII Text String</summary>
    Ascii
}

/// <summary>
/// Configuration for Modbus connection mode.
/// </summary>
public enum ConnectionType
{
    Tcp,
    Rtu,
    Ascii
}

/// <summary>
/// Address formatting style: Zero-based raw protocol offset vs 1-based PLC Modicon standard (00001, 10001, 30001, 40001).
/// </summary>
public enum AddressFormatMode
{
    /// <summary>Standard 0-based protocol offset (e.g., 0, 1, 2, ...)</summary>
    RawZeroBased,

    /// <summary>Standard 5-digit PLC Modicon notation: 00001 (Coils), 10001 (Discrete Inputs), 30001 (Input Registers), 40001 (Holding Registers)</summary>
    Plc5Digit,

    /// <summary>Standard 6-digit PLC Modicon notation for extended registers: 000001, 100001, 300001, 400001</summary>
    Plc6Digit
}

/// <summary>
/// Modbus connection parameters model.
/// </summary>
public class ConnectionConfig
{
    public ConnectionType ConnectionType { get; set; } = ConnectionType.Tcp;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 502;
    public string SerialPortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public System.IO.Ports.Parity Parity { get; set; } = System.IO.Ports.Parity.None;
    public System.IO.Ports.StopBits StopBits { get; set; } = System.IO.Ports.StopBits.One;
    public int TimeoutMs { get; set; } = 1500;
    public int PollIntervalMs { get; set; } = 1000;
}
