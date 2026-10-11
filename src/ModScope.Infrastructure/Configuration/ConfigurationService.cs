using System.Text.Json;
using ModScope.Core.Models;

namespace ModScope.Infrastructure.Configuration;

/// <summary>
/// User-defined tag definition entry saved with workspace settings.
/// </summary>
public class TagDefinition
{
    public ushort Address { get; set; }
    public ModbusTableType TableType { get; set; } = ModbusTableType.HoldingRegister;
    public string TagName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Persisted user application settings and workspace configuration.
/// </summary>
public class AppSettings
{
    public ConnectionConfig Connection { get; set; } = new();
    public string Theme { get; set; } = "Dark";
    public bool ReadOnlyMode { get; set; } = true;
    public bool AutoScrollRawFrames { get; set; } = true;
    public int MaxCapturedFrames { get; set; } = 1000;
    public string OperatorName { get; set; } = "Automation Engineer";
    public string DefaultExportDirectory { get; set; } = string.Empty;
    public AddressFormatMode AddressFormat { get; set; } = AddressFormatMode.Plc5Digit;
    public byte DefaultSlaveId { get; set; } = 1;
    public ushort DefaultStartAddress { get; set; } = 0;
    public ushort DefaultRegisterCount { get; set; } = 10;
    public string DefaultTableType { get; set; } = "HoldingRegister";
    public DataDisplayFormat DefaultDisplayFormat { get; set; } = DataDisplayFormat.UInt16;
    public ModScope.ModbusMaster.EndiannessOrder DefaultEndianness { get; set; } = ModScope.ModbusMaster.EndiannessOrder.BigEndian;

    /// <summary>
    /// User-defined custom tags mapped by "TableType:Address" or stored in list.
    /// </summary>
    public List<TagDefinition> CustomTags { get; set; } = new();
}

/// <summary>
/// Manages loading and saving configuration JSON files across Windows and Linux.
/// </summary>
public class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _configFilePath;

    public string ConfigFilePath => _configFilePath;

    public ConfigurationService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _configFilePath = customPath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "ModScope");
            Directory.CreateDirectory(folder);
            _configFilePath = Path.Combine(folder, "modscope_settings.json");
        }
    }

    public AppSettings LoadSettings(string? path = null)
    {
        string target = path ?? _configFilePath;
        try
        {
            if (File.Exists(target))
            {
                string json = File.ReadAllText(target);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // Fallback to default on error
        }

        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings, string? path = null)
    {
        string target = path ?? _configFilePath;
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            string? dir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(target, json);
        }
        catch
        {
            // Ignore persist error
        }
    }
}
