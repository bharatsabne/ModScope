using System.Net.Sockets;

namespace ModScope.ModbusMaster;

/// <summary>
/// Ethernet TCP transport implementation for Modbus TCP.
/// </summary>
public class ModbusTcpTransport : IModbusTransport
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

    /// <inheritdoc/>
    public int TimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Initializes a new instance of <see cref="ModbusTcpTransport"/>.
    /// </summary>
    /// <param name="host">Target IP address or hostname.</param>
    /// <param name="port">Target TCP port (standard default is 502).</param>
    /// <param name="timeoutMs">Connection and socket timeout in milliseconds.</param>
    public ModbusTcpTransport(string host, int port = 502, int timeoutMs = 3000)
    {
        _host = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host;
        _port = port > 0 ? port : 502;
        TimeoutMs = timeoutMs;
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsConnected)
            return;

        _tcpClient?.Dispose();
        _tcpClient = new TcpClient
        {
            NoDelay = true,
            SendTimeout = TimeoutMs,
            ReceiveTimeout = TimeoutMs
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeoutMs);

        await _tcpClient.ConnectAsync(_host, _port, cts.Token).ConfigureAwait(false);
        _stream = _tcpClient.GetStream();
        _stream.ReadTimeout = TimeoutMs;
        _stream.WriteTimeout = TimeoutMs;
    }

    /// <inheritdoc/>
    public Task DisconnectAsync()
    {
        try
        {
            _stream?.Dispose();
            _tcpClient?.Close();
            _tcpClient?.Dispose();
        }
        finally
        {
            _stream = null;
            _tcpClient = null;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_stream == null || !IsConnected)
            throw new InvalidOperationException("TCP transport is not connected.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeoutMs);

        await _stream.WriteAsync(buffer.AsMemory(offset, count), cts.Token).ConfigureAwait(false);
        await _stream.FlushAsync(cts.Token).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_stream == null || !IsConnected)
            throw new InvalidOperationException("TCP transport is not connected.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeoutMs);

        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await _stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), cts.Token).ConfigureAwait(false);
            if (read == 0)
            {
                if (totalRead > 0) return totalRead;
                throw new EndOfStreamException("Modbus TCP stream reached end-of-stream before requested bytes were read.");
            }
            totalRead += read;
        }

        return totalRead;
    }

    /// <inheritdoc/>
    public void DiscardInBuffer()
    {
        if (_stream != null && _stream.DataAvailable)
        {
            byte[] drain = new byte[1024];
            while (_stream.DataAvailable)
            {
                _stream.Read(drain, 0, drain.Length);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
