namespace ModScope.ModbusMaster;

/// <summary>
/// High-performance CRC-16 computation for Modbus RTU frames (polynomial 0xA001, initial value 0xFFFF).
/// </summary>
public static class ModbusCrc
{
    private static readonly ushort[] Table = new ushort[256];

    static ModbusCrc()
    {
        for (ushort i = 0; i < 256; i++)
        {
            ushort value = i;
            for (byte j = 0; j < 8; j++)
            {
                if ((value & 0x0001) != 0)
                {
                    value = (ushort)((value >> 1) ^ 0xA001);
                }
                else
                {
                    value = (ushort)(value >> 1);
                }
            }
            Table[i] = value;
        }
    }

    /// <summary>
    /// Computes the Modbus CRC-16 for the given buffer.
    /// </summary>
    /// <param name="buffer">Byte buffer.</param>
    /// <param name="offset">Starting index.</param>
    /// <param name="count">Number of bytes to process.</param>
    /// <returns>CRC-16 value (Low byte first in standard transmission).</returns>
    public static ushort Compute(byte[] buffer, int offset, int count)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || count < 0 || offset + count > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        return Compute(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// Computes the Modbus CRC-16 for a span of bytes.
    /// </summary>
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        for (int i = 0; i < data.Length; i++)
        {
            byte index = (byte)(crc ^ data[i]);
            crc = (ushort)((crc >> 8) ^ Table[index]);
        }
        return crc;
    }

    /// <summary>
    /// Validates whether the Modbus RTU frame ends with the correct 2-byte CRC.
    /// </summary>
    /// <param name="frame">Full frame including 2 CRC bytes at the end.</param>
    /// <returns>True if CRC matches; false otherwise.</returns>
    public static bool Validate(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 3) return false;
        ushort calculated = Compute(frame[..^2]);
        ushort received = (ushort)(frame[^2] | (frame[^1] << 8));
        return calculated == received;
    }

    /// <summary>
    /// Appends the 2-byte CRC (Low byte first, then High byte) to the end of a payload.
    /// </summary>
    public static byte[] AppendCrc(byte[] payload)
    {
        if (payload == null) throw new ArgumentNullException(nameof(payload));
        ushort crc = Compute(payload);
        byte[] frame = new byte[payload.Length + 2];
        Buffer.BlockCopy(payload, 0, frame, 0, payload.Length);
        frame[^2] = (byte)(crc & 0xFF);         // CRC Low
        frame[^1] = (byte)((crc >> 8) & 0xFF);  // CRC High
        return frame;
    }
}
