using ModScope.Core.Models;
using ModScope.ModbusMaster;

namespace ModScope.Diagnostics.Discovery;

/// <summary>
/// Result of a scanned slave device.
/// </summary>
public record DiscoveredDevice(
    byte SlaveId,
    bool Responded,
    string DeviceType,
    TimeSpan ResponseTime,
    string? Error = null
);

/// <summary>
/// Scans Modbus buses (Serial RTU slave IDs 1-247, or IP ranges) to discover online devices.
/// </summary>
public class ModbusDeviceScanner
{
    private readonly IModbusMaster _master;

    public ModbusDeviceScanner(IModbusMaster master)
    {
        _master = master ?? throw new ArgumentNullException(nameof(master));
    }

    /// <summary>
    /// Scans a range of slave IDs (e.g. 1 to 32, or 1 to 247) by sending a non-destructive read request.
    /// </summary>
    public async Task<List<DiscoveredDevice>> ScanSlaveIdsAsync(
        byte startId,
        byte endId,
        IProgress<(byte currentId, int percent)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var discovered = new List<DiscoveredDevice>();
        int total = endId - startId + 1;
        int count = 0;

        for (byte id = startId; id <= endId; id++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool responded = false;
            string devType = "Unknown";
            string? error = null;

            try
            {
                // Non-destructive read: 1 holding register at address 0
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(400); // Quick scan timeout

                var regs = await _master.ReadHoldingRegistersAsync(id, 0, 1, cts.Token).ConfigureAwait(false);
                sw.Stop();
                responded = true;
                devType = $"Modbus Device (Reg0=0x{regs[0]:X4})";
            }
            catch (ModbusProtocolException ex)
            {
                // If the device returned an exception, IT IS ALIVE and responding!
                sw.Stop();
                responded = true;
                devType = $"Modbus Device (Exc: {ex.ExceptionCode})";
            }
            catch (Exception ex)
            {
                sw.Stop();
                error = ex.Message;
            }

            count++;
            progress?.Report((id, (int)((count / (double)total) * 100)));

            if (responded)
            {
                discovered.Add(new DiscoveredDevice(id, true, devType, sw.Elapsed));
            }

            // Brief pause between requests to respect RS-485 line turnaround
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }

        return discovered;
    }
}
