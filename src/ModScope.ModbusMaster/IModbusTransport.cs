namespace ModScope.ModbusMaster;

/// <summary>
/// Abstraction for the underlying Modbus communication channel (Serial Port, TCP Client, Virtual Channel).
/// </summary>
public interface IModbusTransport : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the transport is currently connected and open.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Opens the communication transport asynchronously.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the transport.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Writes raw bytes to the transport.
    /// </summary>
    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads exact number of bytes or until timeout/cancellation.
    /// </summary>
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears any unread or pending data in the input buffer.
    /// </summary>
    void DiscardInBuffer();

    /// <summary>
    /// Timeout in milliseconds for read and write operations.
    /// </summary>
    int TimeoutMs { get; set; }
}
