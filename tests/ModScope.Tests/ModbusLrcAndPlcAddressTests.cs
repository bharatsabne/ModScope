using ModScope.Core.Models;
using ModScope.ModbusMaster;
using Xunit;

namespace ModScope.Tests;

public class ModbusLrcAndPlcAddressTests
{
    [Fact]
    public void ModbusLrc_Compute_ReturnsValidTwoComplement()
    {
        // Example: Slave 0x01, FC 0x03, Start 0x0000, Count 0x0001
        // Bytes: 0x01 + 0x03 + 0x00 + 0x00 + 0x00 + 0x01 = 0x05
        // Two's complement (-5): 0xFB
        byte[] payload = [0x01, 0x03, 0x00, 0x00, 0x00, 0x01];
        byte lrc = ModbusLrc.Compute(payload);

        Assert.Equal(0xFB, lrc);
    }

    [Fact]
    public void ModbusLrc_EncodeAndDecodeAsciiFrame_RoundTripSucceeds()
    {
        byte[] payload = [0x01, 0x03, 0x00, 0x00, 0x00, 0x02];
        byte[] asciiFrame = ModbusLrc.EncodeAsciiFrame(payload);

        // First char must be ':' and ends with '\r\n'
        Assert.Equal((byte)':', asciiFrame[0]);
        Assert.Equal(0x0D, asciiFrame[^2]);
        Assert.Equal(0x0A, asciiFrame[^1]);

        byte[] decoded = ModbusLrc.DecodeAsciiFrame(asciiFrame);
        Assert.Equal(payload, decoded);
    }

    [Theory]
    [InlineData(0, ModbusTableType.HoldingRegister, "40001")]
    [InlineData(9, ModbusTableType.HoldingRegister, "40010")]
    [InlineData(0, ModbusTableType.InputRegister, "30001")]
    [InlineData(0, ModbusTableType.DiscreteInput, "10001")]
    [InlineData(0, ModbusTableType.Coil, "00001")]
    public void ModbusPlcAddressFormatter_FormatAddress_ReturnsExpectedPlc5Digit(ushort raw, ModbusTableType table, string expected)
    {
        string formatted = ModbusPlcAddressFormatter.FormatAddress(raw, table, AddressFormatMode.Plc5Digit);
        Assert.Equal(expected, formatted);
    }

    [Theory]
    [InlineData("40001", 0, ModbusTableType.HoldingRegister)]
    [InlineData("40010", 9, ModbusTableType.HoldingRegister)]
    [InlineData("30005", 4, ModbusTableType.InputRegister)]
    [InlineData("10002", 1, ModbusTableType.DiscreteInput)]
    [InlineData("00001", 0, ModbusTableType.Coil)]
    [InlineData("100", 100, null)]
    public void ModbusPlcAddressFormatter_ParseAddress_ParsesCorrectly(string input, ushort expectedRaw, ModbusTableType? expectedTable)
    {
        var (raw, table) = ModbusPlcAddressFormatter.ParseAddress(input);
        Assert.Equal(expectedRaw, raw);
        Assert.Equal(expectedTable, table);
    }
}
