using ModScope.Diagnostics.Simulator;
using ModScope.ModbusMaster;
using Xunit;

namespace ModScope.Tests;

public class ModbusIntegrationTests : IDisposable
{
    private readonly ModbusSimulatorServer _simulator;
    private readonly int _testPort = 50299;

    public ModbusIntegrationTests()
    {
        _simulator = new ModbusSimulatorServer();
        _simulator.Start(_testPort);
    }

    [Fact]
    public async Task ModbusTcpMaster_ReadHoldingRegisters_ReturnsValues()
    {
        using var transport = new ModbusTcpTransport("127.0.0.1", _testPort, timeoutMs: 2000);
        using var master = new ModbusTcpMaster(transport);

        var regs = await master.ReadHoldingRegistersAsync(slaveId: 1, startAddress: 0, count: 4);

        Assert.NotNull(regs);
        Assert.Equal(4, regs.Length);
        Assert.Equal(100, regs[0]);
        Assert.Equal(0x1234, regs[1]);
    }

    [Fact]
    public async Task ModbusTcpMaster_WriteSingleRegister_UpdatesAndReadsBack()
    {
        using var transport = new ModbusTcpTransport("127.0.0.1", _testPort, timeoutMs: 2000);
        using var master = new ModbusTcpMaster(transport);

        ushort writeVal = 0xABCD;
        ushort targetAddr = 50;

        await master.WriteSingleRegisterAsync(slaveId: 1, registerAddress: targetAddr, value: writeVal);

        var readBack = await master.ReadHoldingRegistersAsync(slaveId: 1, startAddress: targetAddr, count: 1);

        Assert.Single(readBack);
        Assert.Equal(writeVal, readBack[0]);
    }

    [Fact]
    public async Task ModbusTcpMaster_ReadCoils_ReturnsValues()
    {
        using var transport = new ModbusTcpTransport("127.0.0.1", _testPort, timeoutMs: 2000);
        using var master = new ModbusTcpMaster(transport);

        var coils = await master.ReadCoilsAsync(slaveId: 1, startAddress: 0, count: 3);

        Assert.Equal(3, coils.Length);
        Assert.True(coils[0]);  // Reg 0 initialized true
        Assert.False(coils[1]); // Reg 1 initialized false
        Assert.True(coils[2]);  // Reg 2 initialized true
    }

    [Fact]
    public async Task ModbusTcpMaster_FrameCapturedEvent_FiresOnTxAndRx()
    {
        using var transport = new ModbusTcpTransport("127.0.0.1", _testPort, timeoutMs: 2000);
        using var master = new ModbusTcpMaster(transport);

        var frames = new List<ModbusRawFrame>();
        master.FrameCaptured += (sender, frame) => frames.Add(frame);

        await master.ReadHoldingRegistersAsync(slaveId: 1, startAddress: 0, count: 2);

        Assert.True(frames.Count >= 2);
        Assert.Contains(frames, f => f.Direction == ModbusFrameDirection.Tx);
        Assert.Contains(frames, f => f.Direction == ModbusFrameDirection.Rx);
    }

    public void Dispose()
    {
        _simulator.Dispose();
    }
}
