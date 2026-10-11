namespace ModScope.ModbusMaster;

/// <summary>
/// Modbus ASCII Longitudinal Redundancy Check (LRC) computation and validation.
/// Standard Modbus ASCII LRC: Two's complement of the 8-bit sum of all bytes in the message excluding starting colon and ending CRLF.
/// </summary>
public static class ModbusLrc
{
    /// <summary>
    /// Computes the 1-byte LRC for a span of binary bytes.
    /// </summary>
    public static byte Compute(ReadOnlySpan<byte> data)
    {
        byte sum = 0;
        for (int i = 0; i < data.Length; i++)
        {
            sum += data[i];
        }
        return (byte)(unchecked((byte)(-sum)));
    }

    /// <summary>
    /// Encodes a binary payload into a standard Modbus ASCII frame string (e.g., ":010300000001F8\r\n").
    /// </summary>
    public static byte[] EncodeAsciiFrame(byte[] binaryPayload)
    {
        if (binaryPayload == null) throw new ArgumentNullException(nameof(binaryPayload));
        byte lrc = Compute(binaryPayload);

        // Frame structure: ':' (1 byte) + 2 hex chars per payload byte + 2 hex chars for LRC + '\r\n' (2 bytes)
        int asciiLength = 1 + (binaryPayload.Length * 2) + 2 + 2;
        byte[] frame = new byte[asciiLength];
        frame[0] = (byte)':';

        int pos = 1;
        for (int i = 0; i < binaryPayload.Length; i++)
        {
            string hex = binaryPayload[i].ToString("X2");
            frame[pos++] = (byte)hex[0];
            frame[pos++] = (byte)hex[1];
        }

        string lrcHex = lrc.ToString("X2");
        frame[pos++] = (byte)lrcHex[0];
        frame[pos++] = (byte)lrcHex[1];

        frame[pos++] = 0x0D; // '\r'
        frame[pos++] = 0x0A; // '\n'

        return frame;
    }

    /// <summary>
    /// Decodes an ASCII frame string (e.g., ":010302006496\r\n") into binary payload, validating the starting colon, ending CRLF, and LRC.
    /// </summary>
    public static byte[] DecodeAsciiFrame(ReadOnlySpan<byte> asciiFrame)
    {
        // Must start with ':' and end with '\r\n' and have even number of hex characters
        if (asciiFrame.Length < 7) // minimum: ':' + 1 byte slave + 1 byte FC + 1 byte LRC + '\r\n' = 1 + 6 + 2 = 9
            throw new FormatException("Modbus ASCII frame is too short.");

        int start = 0;
        while (start < asciiFrame.Length && asciiFrame[start] != (byte)':')
        {
            start++;
        }

        if (start >= asciiFrame.Length)
            throw new FormatException("Modbus ASCII frame missing starting colon ':'.");

        // Find CRLF or end
        int end = asciiFrame.Length;
        if (asciiFrame[^2] == 0x0D && asciiFrame[^1] == 0x0A)
        {
            end -= 2;
        }

        ReadOnlySpan<byte> hexSpan = asciiFrame.Slice(start + 1, end - (start + 1));
        if (hexSpan.Length % 2 != 0)
            throw new FormatException("Modbus ASCII frame contains an odd number of hex characters.");

        int byteCount = hexSpan.Length / 2;
        byte[] binaryData = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            string hexPair = $"{(char)hexSpan[i * 2]}{(char)hexSpan[i * 2 + 1]}";
            binaryData[i] = Convert.ToByte(hexPair, 16);
        }

        // Validate LRC (last byte is LRC)
        byte receivedLrc = binaryData[^1];
        byte calculatedLrc = Compute(binaryData.AsSpan(0, binaryData.Length - 1));

        if (receivedLrc != calculatedLrc)
        {
            throw new ModbusChecksumException(calculatedLrc, receivedLrc);
        }

        // Return binary payload excluding LRC
        byte[] payload = new byte[binaryData.Length - 1];
        Buffer.BlockCopy(binaryData, 0, payload, 0, payload.Length);
        return payload;
    }
}
