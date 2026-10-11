using ModScope.Core.Models;
using ModScope.ModbusMaster;

namespace ModScope.Core.Interfaces;

/// <summary>
/// Service abstraction for Modbus session management, polling, and writes.
/// </summary>
public interface IModbusSessionService
{
    event EventHandler<ModbusRawFrame>? FrameCaptured;
    event EventHandler<bool>? ConnectionStatusChanged;
    event EventHandler<string>? ErrorOccurred;

    bool IsConnected { get; }
    bool IsPolling { get; }
    ConnectionConfig Config { get; set; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    
    Task<ushort[]> ReadRegistersAsync(ModbusTableType tableType, byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default);
    Task<bool[]> ReadBitsAsync(ModbusTableType tableType, byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    Task WriteRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default);
    Task WriteMultipleRegistersAsync(byte slaveId, ushort address, ushort[] values, CancellationToken cancellationToken = default);
    Task WriteCoilAsync(byte slaveId, ushort address, bool value, CancellationToken cancellationToken = default);
    Task WriteMultipleCoilsAsync(byte slaveId, ushort address, bool[] values, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service abstraction for recording audit operations.
/// </summary>
public interface IAuditLogService
{
    IReadOnlyList<AuditLogEntry> Entries { get; }
    event EventHandler<AuditLogEntry>? EntryAdded;
    void Log(AuditLogEntry entry);
    void Clear();
}
