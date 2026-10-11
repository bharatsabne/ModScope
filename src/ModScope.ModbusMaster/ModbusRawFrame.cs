namespace ModScope.ModbusMaster;

/// <summary>
/// Direction of the raw Modbus frame.
/// </summary>
public enum ModbusFrameDirection
{
    /// <summary>Transmitted from master to slave.</summary>
    Tx,

    /// <summary>Received by master from slave.</summary>
    Rx
}

/// <summary>
/// Captured raw transmission or reception frame with microsecond/millisecond accurate timestamp.
/// </summary>
public record ModbusRawFrame(
    DateTime Timestamp,
    ModbusFrameDirection Direction,
    byte[] RawBytes,
    string Description,
    bool IsError = false
)
{
    /// <summary>
    /// Hexadecimal string representation formatted for engineering diagnostics (e.g., "01 03 00 00 00 0A C5 CD").
    /// </summary>
    public string HexString => BitConverter.ToString(RawBytes).Replace('-', ' ');

    /// <summary>
    /// Length of the raw frame in bytes.
    /// </summary>
    public int Length => RawBytes.Length;
}
