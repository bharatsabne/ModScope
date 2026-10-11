namespace ModScope.ModbusMaster;

/// <summary>
/// Endianness and word arrangement for 16-bit, 32-bit, and 64-bit Modbus data types.
/// </summary>
public enum EndiannessOrder
{
    /// <summary>
    /// Big-Endian (ABCD) - High Byte First (MSB), High Word First. Standard Modbus specification order.
    /// </summary>
    BigEndian,

    /// <summary>
    /// Little-Endian (DCBA) - Low Byte First (LSB), Low Word First. Common in x86/ARM systems.
    /// </summary>
    LittleEndian,

    /// <summary>
    /// Byte Swap / Mid-Little Endian (BADC) - Low Byte First within words, High Word First.
    /// </summary>
    ByteSwap,

    /// <summary>
    /// Word Swap / Mid-Big Endian (CDAB) - High Byte First within words, Low Word First. Very common in PLCs (Schneider, Modicon, Omron).
    /// </summary>
    WordSwap
}

/// <summary>
/// Utilities for converting Modbus registers (16-bit words) to various industrial data formats:
/// UInt16, Int16, UInt32, Int32, Float32 (Single), Double64, UInt64, Int64, and ASCII/UTF8 strings,
/// with configurable byte order (MSB-LSB vs LSB-MSB) and word order.
/// </summary>
public static class ModbusDataConverter
{
    /// <summary>
    /// Converts a single 16-bit register to signed short (Int16) based on byte order.
    /// </summary>
    public static short ToInt16(ushort register, bool byteSwap = false)
    {
        if (!byteSwap) return unchecked((short)register);
        ushort swapped = (ushort)(((register & 0xFF) << 8) | ((register >> 8) & 0xFF));
        return unchecked((short)swapped);
    }

    /// <summary>
    /// Converts a single 16-bit register to unsigned short (UInt16) based on byte order.
    /// </summary>
    public static ushort ToUInt16(ushort register, bool byteSwap = false)
    {
        if (!byteSwap) return register;
        return (ushort)(((register & 0xFF) << 8) | ((register >> 8) & 0xFF));
    }

    /// <summary>
    /// Converts two 16-bit registers to a 32-bit signed integer (Int32) using the specified byte/word order.
    /// </summary>
    public static int ToInt32(ushort reg1, ushort reg2, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = ExtractBytes(reg1, reg2, order);
        return BitConverter.ToInt32(bytes, 0);
    }

    /// <summary>
    /// Converts two 16-bit registers to a 32-bit unsigned integer (UInt32) using the specified byte/word order.
    /// </summary>
    public static uint ToUInt32(ushort reg1, ushort reg2, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = ExtractBytes(reg1, reg2, order);
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// Converts two 16-bit registers to an IEEE-754 32-bit floating point number (Float / Single).
    /// </summary>
    public static float ToSingle(ushort reg1, ushort reg2, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = ExtractBytes(reg1, reg2, order);
        return BitConverter.ToSingle(bytes, 0);
    }

    /// <summary>
    /// Converts four 16-bit registers (64 bits, 8 bytes) to an IEEE-754 64-bit double precision float (Double).
    /// </summary>
    public static double ToDouble(ushort r1, ushort r2, ushort r3, ushort r4, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = Extract8Bytes(r1, r2, r3, r4, order);
        return BitConverter.ToDouble(bytes, 0);
    }

    /// <summary>
    /// Converts four 16-bit registers (64 bits, 8 bytes) to a 64-bit signed integer (Int64).
    /// </summary>
    public static long ToInt64(ushort r1, ushort r2, ushort r3, ushort r4, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = Extract8Bytes(r1, r2, r3, r4, order);
        return BitConverter.ToInt64(bytes, 0);
    }

    /// <summary>
    /// Converts four 16-bit registers (64 bits, 8 bytes) to a 64-bit unsigned integer (UInt64).
    /// </summary>
    public static ulong ToUInt64(ushort r1, ushort r2, ushort r3, ushort r4, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = Extract8Bytes(r1, r2, r3, r4, order);
        return BitConverter.ToUInt64(bytes, 0);
    }

    /// <summary>
    /// Converts a 64-bit IEEE-754 double precision float (Double) into four 16-bit Modbus registers (8 bytes) according to the specified endianness.
    /// </summary>
    public static ushort[] FromDouble(double value, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return PackBytesToRegisters8(bytes, order);
    }

    /// <summary>
    /// Converts a 64-bit signed integer (Int64) into four 16-bit Modbus registers (8 bytes) according to the specified endianness.
    /// </summary>
    public static ushort[] FromInt64(long value, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return PackBytesToRegisters8(bytes, order);
    }

    /// <summary>
    /// Converts a 64-bit unsigned integer (UInt64) into four 16-bit Modbus registers (8 bytes) according to the specified endianness.
    /// </summary>
    public static ushort[] FromUInt64(ulong value, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return PackBytesToRegisters8(bytes, order);
    }

    /// <summary>
    /// Converts an IEEE 754 float into two 16-bit Modbus registers according to the specified endianness.
    /// </summary>
    public static ushort[] FromSingle(float value, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return PackBytesToRegisters(bytes, order);
    }

    /// <summary>
    /// Converts an Int32 into two 16-bit Modbus registers according to the specified endianness.
    /// </summary>
    public static ushort[] FromInt32(int value, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return PackBytesToRegisters(bytes, order);
    }

    /// <summary>
    /// Converts a UInt32 into two 16-bit Modbus registers according to the specified endianness.
    /// </summary>
    public static ushort[] FromUInt32(uint value, EndiannessOrder order = EndiannessOrder.BigEndian)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return PackBytesToRegisters(bytes, order);
    }

    /// <summary>
    /// Converts registers into an ASCII string.
    /// </summary>
    public static string ToAsciiString(ReadOnlySpan<ushort> registers, bool byteSwap = false)
    {
        if (registers.IsEmpty) return string.Empty;
        byte[] bytes = new byte[registers.Length * 2];
        for (int i = 0; i < registers.Length; i++)
        {
            byte bHigh = (byte)((registers[i] >> 8) & 0xFF);
            byte bLow = (byte)(registers[i] & 0xFF);
            if (byteSwap)
            {
                bytes[i * 2] = bLow;
                bytes[i * 2 + 1] = bHigh;
            }
            else
            {
                bytes[i * 2] = bHigh;
                bytes[i * 2 + 1] = bLow;
            }
        }
        int nullIdx = Array.IndexOf(bytes, (byte)0);
        int len = nullIdx >= 0 ? nullIdx : bytes.Length;
        return System.Text.Encoding.ASCII.GetString(bytes, 0, len);
    }

    /// <summary>
    /// Converts an ASCII string into an array of 16-bit registers.
    /// </summary>
    public static ushort[] FromAsciiString(string text, int registerCount, bool byteSwap = false)
    {
        if (text == null) text = string.Empty;
        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(text);
        byte[] buffer = new byte[registerCount * 2];
        Array.Copy(asciiBytes, buffer, Math.Min(asciiBytes.Length, buffer.Length));

        ushort[] result = new ushort[registerCount];
        for (int i = 0; i < registerCount; i++)
        {
            byte b1 = buffer[i * 2];
            byte b2 = buffer[i * 2 + 1];
            if (byteSwap)
            {
                result[i] = (ushort)(b1 | (b2 << 8));
            }
            else
            {
                result[i] = (ushort)((b1 << 8) | b2);
            }
        }
        return result;
    }

    /// <summary>
    /// Extracts 4 bytes from two 16-bit registers according to EndiannessOrder.
    /// </summary>
    private static byte[] ExtractBytes(ushort r1, ushort r2, EndiannessOrder order)
    {
        byte r1High = (byte)((r1 >> 8) & 0xFF);
        byte r1Low = (byte)(r1 & 0xFF);
        byte r2High = (byte)((r2 >> 8) & 0xFF);
        byte r2Low = (byte)(r2 & 0xFF);

        // A=r1High, B=r1Low, C=r2High, D=r2Low
        byte[] orderedBytes = order switch
        {
            EndiannessOrder.BigEndian => [r1High, r1Low, r2High, r2Low],      // ABCD (MSB first)
            EndiannessOrder.LittleEndian => [r2Low, r2High, r1Low, r1High],   // DCBA (LSB first)
            EndiannessOrder.ByteSwap => [r1Low, r1High, r2Low, r2High],       // BADC (LSB-MSB per word)
            EndiannessOrder.WordSwap => [r2High, r2Low, r1High, r1Low],       // CDAB (Low Word First)
            _ => [r1High, r1Low, r2High, r2Low]
        };

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(orderedBytes);
        }

        return orderedBytes;
    }

    /// <summary>
    /// Extracts 8 bytes (first 8 bytes for 64-bit Int64, UInt64, Double) from four 16-bit registers.
    /// </summary>
    private static byte[] Extract8Bytes(ushort r1, ushort r2, ushort r3, ushort r4, EndiannessOrder order)
    {
        byte b0 = (byte)((r1 >> 8) & 0xFF);
        byte b1 = (byte)(r1 & 0xFF);
        byte b2 = (byte)((r2 >> 8) & 0xFF);
        byte b3 = (byte)(r2 & 0xFF);
        byte b4 = (byte)((r3 >> 8) & 0xFF);
        byte b5 = (byte)(r3 & 0xFF);
        byte b6 = (byte)((r4 >> 8) & 0xFF);
        byte b7 = (byte)(r4 & 0xFF);

        // 8 bytes in big-endian high-to-low order: [b0..b7]
        byte[] ordered = order switch
        {
            EndiannessOrder.BigEndian => [b0, b1, b2, b3, b4, b5, b6, b7],
            EndiannessOrder.LittleEndian => [b7, b6, b5, b4, b3, b2, b1, b0],
            EndiannessOrder.ByteSwap => [b1, b0, b3, b2, b5, b4, b7, b6],
            EndiannessOrder.WordSwap => [b6, b7, b4, b5, b2, b3, b0, b1],
            _ => [b0, b1, b2, b3, b4, b5, b6, b7]
        };

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(ordered);
        }

        return ordered;
    }

    private static ushort[] PackBytesToRegisters(byte[] sysBytes, EndiannessOrder order)
    {
        byte[] abcd = (byte[])sysBytes.Clone();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(abcd);
        }

        byte a = abcd[0], b = abcd[1], c = abcd[2], d = abcd[3];
        byte r1High, r1Low, r2High, r2Low;

        switch (order)
        {
            case EndiannessOrder.BigEndian:
                r1High = a; r1Low = b; r2High = c; r2Low = d;
                break;
            case EndiannessOrder.LittleEndian:
                r1High = d; r1Low = c; r2High = b; r2Low = a;
                break;
            case EndiannessOrder.ByteSwap:
                r1High = b; r1Low = a; r2High = d; r2Low = c;
                break;
            case EndiannessOrder.WordSwap:
                r1High = c; r1Low = d; r2High = a; r2Low = b;
                break;
            default:
                r1High = a; r1Low = b; r2High = c; r2Low = d;
                break;
        }

        return
        [
            (ushort)((r1High << 8) | r1Low),
            (ushort)((r2High << 8) | r2Low)
        ];
    }

    private static ushort[] PackBytesToRegisters8(byte[] sysBytes, EndiannessOrder order)
    {
        byte[] b = (byte[])sysBytes.Clone();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(b);
        }

        byte[] ordered = order switch
        {
            EndiannessOrder.BigEndian => [b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7]],
            EndiannessOrder.LittleEndian => [b[7], b[6], b[5], b[4], b[3], b[2], b[1], b[0]],
            EndiannessOrder.ByteSwap => [b[1], b[0], b[3], b[2], b[5], b[4], b[7], b[6]],
            EndiannessOrder.WordSwap => [b[6], b[7], b[4], b[5], b[2], b[3], b[0], b[1]],
            _ => [b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7]]
        };

        return
        [
            (ushort)((ordered[0] << 8) | ordered[1]),
            (ushort)((ordered[2] << 8) | ordered[3]),
            (ushort)((ordered[4] << 8) | ordered[5]),
            (ushort)((ordered[6] << 8) | ordered[7])
        ];
    }
}
