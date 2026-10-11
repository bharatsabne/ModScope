using ModScope.ModbusMaster;
using Xunit;

namespace ModScope.Tests;

public class ModbusCrcTests
{
    [Fact]
    public void Compute_StandardModbusExample_ReturnsCorrectCrc()
    {
        // Standard Modbus RTU test frame: Slave 0x01, FC 0x03, Start 0x0000, Count 0x000A
        // In RTU transmission order: Low byte first (0xC5), then High byte (0xCD) -> 16-bit word is 0xCDC5
        byte[] frame = [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A];
        ushort crc = ModbusCrc.Compute(frame);

        Assert.Equal(0xCDC5, crc);
    }

    [Fact]
    public void AppendCrc_AddsCorrectTwoBytes()
    {
        byte[] payload = [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A];
        byte[] fullFrame = ModbusCrc.AppendCrc(payload);

        Assert.Equal(8, fullFrame.Length);
        Assert.Equal(0xC5, fullFrame[6]); // Low byte
        Assert.Equal(0xCD, fullFrame[7]); // High byte
    }

    [Fact]
    public void Validate_ValidFrame_ReturnsTrue()
    {
        byte[] validFrame = [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD];
        bool isValid = ModbusCrc.Validate(validFrame);

        Assert.True(isValid);
    }

    [Fact]
    public void Validate_CorruptedFrame_ReturnsFalse()
    {
        byte[] corruptedFrame = [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00];
        bool isValid = ModbusCrc.Validate(corruptedFrame);

        Assert.False(isValid);
    }
}
