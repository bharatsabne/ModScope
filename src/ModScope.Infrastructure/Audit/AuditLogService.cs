using System.Globalization;
using System.Text.Json;
using CsvHelper;
using ModScope.Core.Interfaces;
using ModScope.Core.Models;

namespace ModScope.Infrastructure.Audit;

/// <summary>
/// Audit logging implementation for recording safety-critical write actions, confirmations, and parameter modifications.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly List<AuditLogEntry> _entries = new();
    private readonly object _lock = new();

    public IReadOnlyList<AuditLogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList();
            }
        }
    }

    public event EventHandler<AuditLogEntry>? EntryAdded;

    public void Log(AuditLogEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }
        EntryAdded?.Invoke(this, entry);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }

    /// <summary>
    /// Exports audit trail entries to a CSV file.
    /// </summary>
    public void ExportToCsv(string filePath)
    {
        lock (_lock)
        {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(_entries);
        }
    }

    /// <summary>
    /// Exports audit trail entries to formatted JSON.
    /// </summary>
    public void ExportToJson(string filePath)
    {
        lock (_lock)
        {
            string json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
