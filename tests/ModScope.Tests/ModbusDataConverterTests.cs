using ModScope.ModbusMaster;
using Xunit;

namespace ModScope.Tests;

public class ModbusDataConverterTests
{
    [Fact]
    public void ToInt16_NormalAndSwapped_ReturnsExpected()
    {
        ushort val = 0x1234;
        short normal = ModbusDataConverter.ToInt16(val, byteSwap: false);
        short swapped = ModbusDataConverter.ToInt16(val, byteSwap: true);

        Assert.Equal(0x1234, normal);
        Assert.Equal(0x3412, swapped);
    }

    [Fact]
    public void SingleFloatConversion_RoundTrip_MatchesExpected()
    {
        float original = 123.45f;
        ushort[] registers = ModbusDataConverter.FromSingle(original, EndiannessOrder.BigEndian);
        float decoded = ModbusDataConverter.ToSingle(registers[0], registers[1], EndiannessOrder.BigEndian);

        Assert.Equal(original, decoded, precision: 2);
    }

    [Fact]
    public void Int32Conversion_WordSwap_MatchesExpected()
    {
        int original = 65538; // 0x00010002
        ushort[] registers = ModbusDataConverter.FromInt32(original, EndiannessOrder.WordSwap);
        int decoded = ModbusDataConverter.ToInt32(registers[0], registers[1], EndiannessOrder.WordSwap);

        Assert.Equal(original, decoded);
    }

    [Theory]
    [InlineData(EndiannessOrder.BigEndian)]
    [InlineData(EndiannessOrder.LittleEndian)]
    [InlineData(EndiannessOrder.WordSwap)]
    [InlineData(EndiannessOrder.ByteSwap)]
    public void SingleFloatConversion_AllEndianness_RoundTrip(EndiannessOrder order)
    {
        float original = 9876.543f;
        ushort[] registers = ModbusDataConverter.FromSingle(original, order);
        Assert.Equal(2, registers.Length);

        float decoded = ModbusDataConverter.ToSingle(registers[0], registers[1], order);
        Assert.Equal(original, decoded, precision: 3);
    }

    [Theory]
    [InlineData(EndiannessOrder.BigEndian)]
    [InlineData(EndiannessOrder.LittleEndian)]
    [InlineData(EndiannessOrder.WordSwap)]
    [InlineData(EndiannessOrder.ByteSwap)]
    public void Double64Conversion_AllEndianness_RoundTrip(EndiannessOrder order)
    {
        double original = 1234567.89012345;
        ushort[] registers = ModbusDataConverter.FromDouble(original, order);
        Assert.Equal(4, registers.Length);

        double decoded = ModbusDataConverter.ToDouble(registers[0], registers[1], registers[2], registers[3], order);
        Assert.Equal(original, decoded, precision: 6);
    }

    [Theory]
    [InlineData(EndiannessOrder.BigEndian)]
    [InlineData(EndiannessOrder.LittleEndian)]
    [InlineData(EndiannessOrder.WordSwap)]
    [InlineData(EndiannessOrder.ByteSwap)]
    public void Int64Conversion_AllEndianness_RoundTrip(EndiannessOrder order)
    {
        long original = 0x0123456789ABCDEF;
        ushort[] registers = ModbusDataConverter.FromInt64(original, order);
        Assert.Equal(4, registers.Length);

        long decoded = ModbusDataConverter.ToInt64(registers[0], registers[1], registers[2], registers[3], order);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void AsciiConversion_RoundTrip_MatchesExpected()
    {
        string text = "MODBUS";
        ushort[] registers = ModbusDataConverter.FromAsciiString(text, 3);
        string decoded = ModbusDataConverter.ToAsciiString(registers);

        Assert.Equal(text, decoded);
    }
}
