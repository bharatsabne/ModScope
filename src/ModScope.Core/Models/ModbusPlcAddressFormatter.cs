namespace ModScope.Core.Models;

/// <summary>
/// Helper for converting between zero-based raw Modbus protocol addresses and standard PLC notation
/// (e.g., 00001 for Coils, 10001 for Discrete Inputs, 30001 for Input Registers, 40001 for Holding Registers).
/// </summary>
public static class ModbusPlcAddressFormatter
{
    /// <summary>
    /// Formats a raw zero-based Modbus address into PLC display string based on table type and formatting mode.
    /// </summary>
    public static string FormatAddress(ushort rawAddress, ModbusTableType tableType, AddressFormatMode mode = AddressFormatMode.Plc5Digit)
    {
        if (mode == AddressFormatMode.RawZeroBased)
        {
            return rawAddress.ToString();
        }

        int oneBased = rawAddress + 1;

        if (mode == AddressFormatMode.Plc5Digit)
        {
            return tableType switch
            {
                ModbusTableType.Coil => $"0{oneBased:D4}",             // 00001 - 09999
                ModbusTableType.DiscreteInput => $"1{oneBased:D4}",    // 10001 - 19999
                ModbusTableType.InputRegister => $"3{oneBased:D4}",    // 30001 - 39999
                ModbusTableType.HoldingRegister => $"4{oneBased:D4}",  // 40001 - 49999
                _ => rawAddress.ToString()
            };
        }

        // 6-digit extended notation
        return tableType switch
        {
            ModbusTableType.Coil => $"0{oneBased:D5}",             // 000001 - 065536
            ModbusTableType.DiscreteInput => $"1{oneBased:D5}",    // 100001 - 165536
            ModbusTableType.InputRegister => $"3{oneBased:D5}",    // 300001 - 365536
            ModbusTableType.HoldingRegister => $"4{oneBased:D5}",  // 400001 - 465536
            _ => rawAddress.ToString()
        };
    }

    /// <summary>
    /// Parses an address input (either raw number like "0", "100" or PLC format like "40001", "30001", "00001") into raw zero-based address and optional inferred table type.
    /// </summary>
    public static (ushort RawAddress, ModbusTableType? InferredTable) ParseAddress(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return (0, null);
        string s = input.Trim();

        // Check if hex: "0x..."
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            ushort hexVal = Convert.ToUInt16(s[2..], 16);
            return (hexVal, null);
        }

        if (!int.TryParse(s, out int val))
        {
            return (0, null);
        }

        // 5-digit PLC notation:
        // 40001 - 49999 -> Holding Register
        // 30001 - 39999 -> Input Register
        // 10001 - 19999 -> Discrete Input
        // 00001 - 09999 (or starts with leading 0) -> Coil
        if (s.Length == 5)
        {
            if (s[0] == '4' && val >= 40001)
                return ((ushort)(val - 40001), ModbusTableType.HoldingRegister);
            if (s[0] == '3' && val >= 30001)
                return ((ushort)(val - 30001), ModbusTableType.InputRegister);
            if (s[0] == '1' && val >= 10001)
                return ((ushort)(val - 10001), ModbusTableType.DiscreteInput);
            if (s[0] == '0' && val >= 1)
                return ((ushort)(val - 1), ModbusTableType.Coil);
        }

        // 6-digit PLC notation:
        if (s.Length == 6)
        {
            if (s[0] == '4' && val >= 400001)
                return ((ushort)(val - 400001), ModbusTableType.HoldingRegister);
            if (s[0] == '3' && val >= 300001)
                return ((ushort)(val - 300001), ModbusTableType.InputRegister);
            if (s[0] == '1' && val >= 100001)
                return ((ushort)(val - 100001), ModbusTableType.DiscreteInput);
            if (s[0] == '0' && val >= 1)
                return ((ushort)(val - 1), ModbusTableType.Coil);
        }

        // Default: zero-based address
        return ((ushort)Math.Max(0, Math.Min(val, 65535)), null);
    }
}
