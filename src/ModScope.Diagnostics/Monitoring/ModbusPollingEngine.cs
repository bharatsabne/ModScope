using ModScope.Core.Models;
using ModScope.ModbusMaster;

namespace ModScope.Diagnostics.Monitoring;

/// <summary>
/// Communication quality metrics and health statistics.
/// </summary>
public class CommunicationHealthMetrics
{
    public long TotalTransactions { get; set; }
    public long SuccessfulTransactions { get; set; }
    public long TimeoutErrors { get; set; }
    public long CrcErrors { get; set; }
    public long ProtocolExceptions { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double LastResponseTimeMs { get; set; }
    public double PacketSuccessRate => TotalTransactions > 0 
        ? Math.Round((SuccessfulTransactions / (double)TotalTransactions) * 100.0, 2) 
        : 100.0;

    public void Reset()
    {
        TotalTransactions = 0;
        SuccessfulTransactions = 0;
        TimeoutErrors = 0;
        CrcErrors = 0;
        ProtocolExceptions = 0;
        AverageResponseTimeMs = 0;
        LastResponseTimeMs = 0;
    }
}

/// <summary>
/// Continuous polling engine for Modbus register maps with health monitoring and real-time event updates.
/// </summary>
public class ModbusPollingEngine : IDisposable
{
    private readonly IModbusMaster _master;
    private CancellationTokenSource? _cts;
    private Task? _pollTask;
    private readonly List<ModbusRegisterItem> _monitoredItems = new();
    private readonly object _lock = new();

    public CommunicationHealthMetrics HealthMetrics { get; } = new();
    public bool IsPolling { get; private set; }
    public int IntervalMs { get; set; } = 1000;
    public byte SlaveId { get; set; } = 1;
    public ushort StartAddress { get; set; } = 0;
    public ushort Count { get; set; } = 10;
    public ModbusTableType TableType { get; set; } = ModbusTableType.HoldingRegister;

    public event EventHandler<IReadOnlyList<ModbusRegisterItem>>? DataPolled;
    public event EventHandler<CommunicationHealthMetrics>? MetricsUpdated;
    public event EventHandler<Exception>? PollErrorOccurred;

    public ModbusPollingEngine(IModbusMaster master)
    {
        _master = master ?? throw new ArgumentNullException(nameof(master));
    }

    public AddressFormatMode AddressFormat { get; set; } = AddressFormatMode.Plc5Digit;

    public void SetMonitoredRange(
        byte slaveId,
        ModbusTableType tableType,
        ushort startAddress,
        ushort count,
        IDictionary<string, (string TagName, string Description)>? customTags = null,
        AddressFormatMode addressFormat = AddressFormatMode.Plc5Digit)
    {
        lock (_lock)
        {
            SlaveId = slaveId;
            TableType = tableType;
            StartAddress = startAddress;
            Count = count;
            AddressFormat = addressFormat;

            _monitoredItems.Clear();
            for (ushort i = 0; i < count; i++)
            {
                ushort addr = (ushort)(startAddress + i);
                string key = $"{tableType}:{addr}";
                string tagName = $"Tag_{addr:D4}";
                string description = $"{tableType} at address {addr}";

                if (customTags != null && customTags.TryGetValue(key, out var custom))
                {
                    if (!string.IsNullOrEmpty(custom.TagName)) tagName = custom.TagName;
                    if (!string.IsNullOrEmpty(custom.Description)) description = custom.Description;
                }

                _monitoredItems.Add(new ModbusRegisterItem
                {
                    Address = addr,
                    TableType = tableType,
                    AddressFormat = addressFormat,
                    TagName = tagName,
                    Description = description
                });
            }
        }
    }

    public void StartPolling()
    {
        if (IsPolling) return;
        _cts = new CancellationTokenSource();
        IsPolling = true;
        _pollTask = Task.Run(() => PollLoopAsync(_cts.Token));
    }

    public void StopPolling()
    {
        if (!IsPolling) return;
        _cts?.Cancel();
        IsPolling = false;
    }

    private async Task PollLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            HealthMetrics.TotalTransactions++;

            try
            {
                // Read Count + 3 registers (up to max 125 registers per Modbus specification) 
                // to provide lookahead words for 32-bit (2 words) and 64-bit (4 words) float/double decoding
                ushort readCount = (ushort)Math.Min(Count + 3, 125);

                if (TableType == ModbusTableType.HoldingRegister)
                {
                    var data = await _master.ReadHoldingRegistersAsync(SlaveId, StartAddress, readCount, token).ConfigureAwait(false);
                    UpdateRegisterValues(data);
                }
                else if (TableType == ModbusTableType.InputRegister)
                {
                    var data = await _master.ReadInputRegistersAsync(SlaveId, StartAddress, readCount, token).ConfigureAwait(false);
                    UpdateRegisterValues(data);
                }
                else if (TableType == ModbusTableType.Coil)
                {
                    var data = await _master.ReadCoilsAsync(SlaveId, StartAddress, Count, token).ConfigureAwait(false);
                    UpdateBitValues(data);
                }
                else if (TableType == ModbusTableType.DiscreteInput)
                {
                    var data = await _master.ReadDiscreteInputsAsync(SlaveId, StartAddress, Count, token).ConfigureAwait(false);
                    UpdateBitValues(data);
                }

                sw.Stop();
                HealthMetrics.SuccessfulTransactions++;
                HealthMetrics.LastResponseTimeMs = sw.Elapsed.TotalMilliseconds;
                HealthMetrics.AverageResponseTimeMs = HealthMetrics.AverageResponseTimeMs == 0
                    ? HealthMetrics.LastResponseTimeMs
                    : (HealthMetrics.AverageResponseTimeMs * 0.9) + (HealthMetrics.LastResponseTimeMs * 0.1);

                MetricsUpdated?.Invoke(this, HealthMetrics);
            }
            catch (ModbusChecksumException ex)
            {
                HealthMetrics.CrcErrors++;
                MetricsUpdated?.Invoke(this, HealthMetrics);
                PollErrorOccurred?.Invoke(this, ex);
            }
            catch (ModbusTimeoutException ex)
            {
                HealthMetrics.TimeoutErrors++;
                MetricsUpdated?.Invoke(this, HealthMetrics);
                PollErrorOccurred?.Invoke(this, ex);
            }
            catch (ModbusProtocolException ex)
            {
                HealthMetrics.ProtocolExceptions++;
                MetricsUpdated?.Invoke(this, HealthMetrics);
                PollErrorOccurred?.Invoke(this, ex);
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                PollErrorOccurred?.Invoke(this, ex);
            }

            try
            {
                await Task.Delay(IntervalMs, token).ConfigureAwait(false);
            }
            catch when (token.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void UpdateRegisterValues(ushort[] data)
    {
        lock (_lock)
        {
            for (int i = 0; i < data.Length && i < _monitoredItems.Count; i++)
            {
                ushort w1 = (i + 1 < data.Length) ? data[i + 1] : (ushort)0;
                ushort w2 = (i + 2 < data.Length) ? data[i + 2] : (ushort)0;
                ushort w3 = (i + 3 < data.Length) ? data[i + 3] : (ushort)0;

                _monitoredItems[i].RawValue = data[i];
                _monitoredItems[i].UpdateAdjacentRegisters(w1, w2, w3);
                _monitoredItems[i].LastUpdated = DateTime.UtcNow;
            }
            DataPolled?.Invoke(this, _monitoredItems.ToList());
        }
    }

    private void UpdateBitValues(bool[] data)
    {
        lock (_lock)
        {
            for (int i = 0; i < data.Length && i < _monitoredItems.Count; i++)
            {
                _monitoredItems[i].BooleanValue = data[i];
                _monitoredItems[i].RawValue = data[i] ? (ushort)1 : (ushort)0;
                _monitoredItems[i].FormattedValue = data[i] ? "ON" : "OFF";
                _monitoredItems[i].LastUpdated = DateTime.UtcNow;
            }
            DataPolled?.Invoke(this, _monitoredItems.ToList());
        }
    }

    public void Dispose()
    {
        StopPolling();
        _cts?.Dispose();
    }
}
