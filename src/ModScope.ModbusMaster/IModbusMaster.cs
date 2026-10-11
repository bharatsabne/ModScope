namespace ModScope.ModbusMaster;

/// <summary>
/// Public interface for Modbus Master communication operations.
/// </summary>
public interface IModbusMaster : IDisposable
{
    /// <summary>
    /// Event triggered whenever a raw TX (transmitted) or RX (received) frame is processed.
    /// </summary>
    event EventHandler<ModbusRawFrame>? FrameCaptured;

    /// <summary>
    /// Connects to the remote Modbus device or gateway.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the remote device.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Gets whether the master is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// FC01: Read Discrete Coils (1-2000 coils).
    /// </summary>
    Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    /// <summary>
    /// FC02: Read Discrete Inputs (1-2000 inputs).
    /// </summary>
    Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    /// <summary>
    /// FC03: Read Holding Registers (1-125 registers).
    /// </summary>
    Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    /// <summary>
    /// FC04: Read Input Registers (1-125 registers).
    /// </summary>
    Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    /// <summary>
    /// FC05: Write Single Coil.
    /// </summary>
    Task WriteSingleCoilAsync(byte slaveId, ushort coilAddress, bool value, CancellationToken cancellationToken = default);

    /// <summary>
    /// FC06: Write Single Holding Register.
    /// </summary>
    Task WriteSingleRegisterAsync(byte slaveId, ushort registerAddress, ushort value, CancellationToken cancellationToken = default);

    /// <summary>
    /// FC15: Write Multiple Coils.
    /// </summary>
    Task WriteMultipleCoilsAsync(byte slaveId, ushort startAddress, bool[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// FC16: Write Multiple Holding Registers.
    /// </summary>
    Task WriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// FC22: Mask Write Register.
    /// </summary>
    Task MaskWriteRegisterAsync(byte slaveId, ushort address, ushort andMask, ushort orMask, CancellationToken cancellationToken = default);

    /// <summary>
    /// FC23: Read/Write Multiple Registers in a single atomic transaction.
    /// </summary>
    Task<ushort[]> ReadWriteMultipleRegistersAsync(
        byte slaveId,
        ushort readStart,
        ushort readCount,
        ushort writeStart,
        ushort[] writeValues,
        CancellationToken cancellationToken = default);
}
