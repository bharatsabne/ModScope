using System.IO.Ports;

namespace ModScope.ModbusMaster;

/// <summary>
/// Cross-platform Serial Port transport implementation for Modbus RTU (Windows and Linux).
/// </summary>
public class ModbusSerialTransport : IModbusTransport
{
    private readonly SerialPort _serialPort;
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

    /// <inheritdoc/>
    public int TimeoutMs
    {
        get => _serialPort.ReadTimeout;
        set
        {
            _serialPort.ReadTimeout = value;
            _serialPort.WriteTimeout = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ModbusSerialTransport"/>.
    /// </summary>
    public ModbusSerialTransport(
        string portName,
        int baudRate = 9600,
        Parity parity = Parity.None,
        int dataBits = 8,
        StopBits stopBits = StopBits.One,
        int timeoutMs = 2000)
    {
        _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
        {
            ReadTimeout = timeoutMs,
            WriteTimeout = timeoutMs,
            Handshake = Handshake.None,
            DtrEnable = true,
            RtsEnable = true
        };
    }

    /// <summary>
    /// Returns available system serial port names (e.g. COM1-COM9 on Windows, /dev/ttyUSB* or /dev/ttyS* on Linux).
    /// </summary>
    public static string[] GetPortNames()
    {
        try
        {
            return SerialPort.GetPortNames();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc/>
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected)
        {
            _serialPort.Open();
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DisconnectAsync()
    {
        if (IsConnected)
        {
            try
            {
                _serialPort.Close();
            }
            catch
            {
                // Ignore closing exceptions
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected) throw new InvalidOperationException("Serial port is not open.");

        using var reg = cancellationToken.Register(() =>
        {
            try { _serialPort.DiscardOutBuffer(); } catch { }
        });

        await _serialPort.BaseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        await _serialPort.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected) throw new InvalidOperationException("Serial port is not open.");

        int totalRead = 0;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeoutMs);

        while (totalRead < count)
        {
            int read = await _serialPort.BaseStream.ReadAsync(
                buffer.AsMemory(offset + totalRead, count - totalRead),
                cts.Token).ConfigureAwait(false);

            if (read == 0)
            {
                if (totalRead > 0) return totalRead;
                throw new EndOfStreamException("Serial stream closed while reading frame.");
            }
            totalRead += read;
        }

        return totalRead;
    }

    /// <inheritdoc/>
    public void DiscardInBuffer()
    {
        if (IsConnected)
        {
            try { _serialPort.DiscardInBuffer(); } catch { }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
            }
            catch
            {
                // Suppress disposal errors
            }
        }
        GC.SuppressFinalize(this);
    }
}
