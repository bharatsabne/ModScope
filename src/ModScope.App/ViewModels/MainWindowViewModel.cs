using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModScope.App.Services;
using ModScope.Core.Models;
using ModScope.Diagnostics.Discovery;
using ModScope.Diagnostics.Monitoring;
using ModScope.Diagnostics.Sequences;
using ModScope.Diagnostics.Simulator;
using ModScope.Infrastructure.Audit;
using ModScope.Infrastructure.Configuration;
using ModScope.Infrastructure.Reporting;
using ModScope.ModbusMaster;

namespace ModScope.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private IModbusMaster? _master;
    private IModbusTransport? _transport;
    private ModbusPollingEngine? _pollingEngine;
    private ModbusSimulatorServer? _simulatorServer;
    private readonly ConfigurationService _configService = new();
    private readonly AuditLogService _auditService = new();
    private readonly IStorageDialogService _dialogService;

    // Map of user-defined tags keyed by "TableType:Address"
    private readonly Dictionary<string, (string TagName, string Description)> _userTags = new();

    [ObservableProperty]
    private string _windowTitle = "ModScope — Industrial Modbus Diagnostic Studio";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectionButtonText))]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PollingButtonText))]
    [NotifyPropertyChangedFor(nameof(PollingButtonBackground))]
    private bool _isPolling;

    public string PollingButtonText => IsPolling ? "Stop Polling" : "Start Polling";
    public string PollingButtonBackground => IsPolling ? "#DC2626" : "#0284C7";

    [ObservableProperty]
    private bool _isSimulatorRunning;

    [ObservableProperty]
    private bool _readOnlyMode = true;

    [ObservableProperty]
    private string _connectionStatusText = "Disconnected";

    public string ConnectionButtonText => IsConnected ? "Disconnect" : "Connect";

    // Active Config file path
    [ObservableProperty]
    private string _currentSettingsFilePath = string.Empty;

    // Connection settings: "TCP", "RTU", or "ASCII"
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTcpMode))]
    [NotifyPropertyChangedFor(nameof(IsSerialMode))]
    private string _connectionType = "TCP";

    public bool IsTcpMode => ConnectionType == "TCP";
    public bool IsSerialMode => ConnectionType == "RTU" || ConnectionType == "ASCII";

    [ObservableProperty]
    private string _tcpHost = "127.0.0.1";

    [ObservableProperty]
    private int _tcpPort = 5020;

    [ObservableProperty]
    private string _serialPort = "COM1";

    [ObservableProperty]
    private int _baudRate = 9600;

    // Address Formatting Mode: "PLC 5-Digit (40001)", "PLC 6-Digit (400001)", "0-Based Raw (0)"
    [ObservableProperty]
    private string _selectedAddressFormat = "PLC 5-Digit (40001)";

    // Scan / Polling settings
    [ObservableProperty]
    private byte _slaveId = 1;

    [ObservableProperty]
    private string _startAddressText = "40001"; // Supports PLC (40001) or raw (0)

    [ObservableProperty]
    private ushort _registerCount = 10;

    [ObservableProperty]
    private string _selectedTableType = "HoldingRegister";

    // Display Format: "UInt16", "Int16", "Float32", "Double64", "Int32", "UInt32", "Int64", "ASCII", "Hex", "Binary"
    [ObservableProperty]
    private string _selectedDisplayFormat = "UInt16";

    // Byte / Word Order: "Big-Endian (ABCD / MSB-LSB)", "Little-Endian (DCBA / LSB-MSB)", "Word-Swap (CDAB)", "Byte-Swap (BADC)"
    [ObservableProperty]
    private string _selectedEndianness = "Big-Endian (ABCD / MSB-LSB)";

    partial void OnSelectedDisplayFormatChanged(string value)
    {
        var fmt = GetDisplayFormat();
        foreach (var item in RegisterItems)
        {
            item.DisplayFormat = fmt;
        }
    }

    partial void OnSelectedEndiannessChanged(string value)
    {
        var end = GetEndiannessOrder();
        foreach (var item in RegisterItems)
        {
            item.Endianness = end;
        }
    }

    partial void OnSelectedAddressFormatChanged(string value)
    {
        var af = GetAddressFormatMode();
        foreach (var item in RegisterItems)
        {
            item.AddressFormat = af;
        }
    }

    // Write parameters
    [ObservableProperty]
    private string _writeAddressText = "40001"; // Supports 40001 or raw

    [ObservableProperty]
    private string _writeValueText = "0";

    [ObservableProperty]
    private string _writeStatusMessage = string.Empty;

    // Diagnostics & Discovery
    [ObservableProperty]
    private string _discoveryStatus = "Ready to scan";

    [ObservableProperty]
    private int _discoveryProgress;

    // Workspace & Settings Status
    [ObservableProperty]
    private string _settingsStatusMessage = "Settings loaded.";

    // Telemetry
    [ObservableProperty]
    private long _totalFrames;

    [ObservableProperty]
    private double _successRate = 100.0;

    [ObservableProperty]
    private double _latencyMs;

    [ObservableProperty]
    private long _crcErrors;

    [ObservableProperty]
    private long _timeoutErrors;

    public ObservableCollection<ModbusRegisterItem> RegisterItems { get; } = new();
    public ObservableCollection<ModbusRawFrame> RawFrames { get; } = new();
    public ObservableCollection<DiscoveredDevice> DiscoveredDevices { get; } = new();
    public ObservableCollection<SequenceStep> SequenceSteps { get; } = new();
    public ObservableCollection<AuditLogEntry> AuditLogs { get; } = new();
    public ObservableCollection<string> AvailableSerialPorts { get; } = new();

    public MainWindowViewModel(IStorageDialogService? dialogService = null)
    {
        _dialogService = dialogService ?? new AvaloniaStorageDialogService();
        RefreshSerialPorts();
        LoadSettings();
    }

    [RelayCommand]
    public void RefreshSerialPorts()
    {
        AvailableSerialPorts.Clear();
        foreach (var p in ModbusSerialTransport.GetPortNames())
        {
            AvailableSerialPorts.Add(p);
        }
        if (AvailableSerialPorts.Count > 0 && !AvailableSerialPorts.Contains(SerialPort))
        {
            SerialPort = AvailableSerialPorts[0];
        }
    }

    [RelayCommand]
    public async Task ToggleConnectionAsync()
    {
        if (IsConnected)
        {
            await DisconnectMasterAsync();
        }
        else
        {
            await ConnectMasterAsync();
        }
    }

    private async Task ConnectMasterAsync()
    {
        try
        {
            ConnectionStatusText = "Connecting...";
            if (ConnectionType == "TCP")
            {
                _transport = new ModbusTcpTransport(TcpHost, TcpPort, timeoutMs: 2500);
                _master = new ModbusTcpMaster(_transport);
            }
            else if (ConnectionType == "ASCII")
            {
                // Modbus ASCII over Serial Port
                _transport = new ModbusSerialTransport(SerialPort, BaudRate, timeoutMs: 2500);
                _master = new ModbusAsciiMaster(_transport);
            }
            else
            {
                // Modbus RTU over Serial Port
                _transport = new ModbusSerialTransport(SerialPort, BaudRate, timeoutMs: 2500);
                _master = new ModbusRtuMaster(_transport);
            }

            _master.FrameCaptured += Master_FrameCaptured;
            await _master.ConnectAsync();

            _pollingEngine = new ModbusPollingEngine(_master);
            _pollingEngine.DataPolled += PollingEngine_DataPolled;
            _pollingEngine.MetricsUpdated += PollingEngine_MetricsUpdated;

            IsConnected = true;
            ConnectionStatusText = $"Connected ({ConnectionType}: {(ConnectionType == "TCP" ? $"{TcpHost}:{TcpPort}" : SerialPort)})";
        }
        catch (Exception ex)
        {
            ConnectionStatusText = $"Connection Failed: {ex.Message}";
            IsConnected = false;
        }
    }

    private async Task DisconnectMasterAsync()
    {
        try
        {
            if (_pollingEngine != null)
            {
                _pollingEngine.StopPolling();
                IsPolling = false;
            }

            if (_master != null)
            {
                _master.FrameCaptured -= Master_FrameCaptured;
                await _master.DisconnectAsync();
                _master.Dispose();
                _master = null;
            }

            _transport?.Dispose();
            _transport = null;

            IsConnected = false;
            ConnectionStatusText = "Disconnected";
        }
        catch (Exception ex)
        {
            ConnectionStatusText = $"Disconnect error: {ex.Message}";
        }
    }

    private AddressFormatMode GetAddressFormatMode()
    {
        return SelectedAddressFormat switch
        {
            "0-Based Raw (0)" => AddressFormatMode.RawZeroBased,
            "PLC 6-Digit (400001)" => AddressFormatMode.Plc6Digit,
            _ => AddressFormatMode.Plc5Digit
        };
    }

    private DataDisplayFormat GetDisplayFormat()
    {
        return SelectedDisplayFormat switch
        {
            "Int16" => DataDisplayFormat.Int16,
            "Float32" => DataDisplayFormat.Float32,
            "Double64" => DataDisplayFormat.Double64,
            "Int32" => DataDisplayFormat.Int32,
            "UInt32" => DataDisplayFormat.UInt32,
            "Int64" => DataDisplayFormat.Int64,
            "ASCII" => DataDisplayFormat.Ascii,
            "Hex" => DataDisplayFormat.Hexadecimal,
            "Binary" => DataDisplayFormat.Binary,
            _ => DataDisplayFormat.UInt16
        };
    }

    private EndiannessOrder GetEndiannessOrder()
    {
        if (SelectedEndianness.Contains("Little-Endian", StringComparison.OrdinalIgnoreCase))
            return EndiannessOrder.LittleEndian;
        if (SelectedEndianness.Contains("Word-Swap", StringComparison.OrdinalIgnoreCase))
            return EndiannessOrder.WordSwap;
        if (SelectedEndianness.Contains("Byte-Swap", StringComparison.OrdinalIgnoreCase))
            return EndiannessOrder.ByteSwap;
        return EndiannessOrder.BigEndian;
    }

    [RelayCommand]
    public void TogglePolling()
    {
        if (!IsConnected || _pollingEngine == null) return;

        if (IsPolling)
        {
            _pollingEngine.StopPolling();
            IsPolling = false;
        }
        else
        {
            var parsed = ModbusPlcAddressFormatter.ParseAddress(StartAddressText);
            ushort rawStart = parsed.RawAddress;

            var tableType = SelectedTableType switch
            {
                "Coil" => ModbusTableType.Coil,
                "DiscreteInput" => ModbusTableType.DiscreteInput,
                "InputRegister" => ModbusTableType.InputRegister,
                _ => ModbusTableType.HoldingRegister
            };

            // If user typed a PLC format like 30001 or 10001, honor the inferred table type if compatible
            if (parsed.InferredTable.HasValue)
            {
                tableType = parsed.InferredTable.Value;
                SelectedTableType = tableType.ToString();
            }

            var addrFormat = GetAddressFormatMode();

            // Store current custom tags into dictionary before polling
            CaptureCurrentTags();

            _pollingEngine.SetMonitoredRange(SlaveId, tableType, rawStart, RegisterCount, _userTags, addrFormat);
            _pollingEngine.StartPolling();
            IsPolling = true;
        }
    }

    [RelayCommand]
    public void SaveWorkspaceSettings()
    {
        string? targetPath = string.IsNullOrWhiteSpace(CurrentSettingsFilePath) ? null : CurrentSettingsFilePath;
        SaveSettingsToPath(targetPath);
    }

    [RelayCommand]
    public async Task SaveWorkspaceSettingsAsAsync()
    {
        string defaultName = string.IsNullOrWhiteSpace(CurrentSettingsFilePath)
            ? "modscope_settings.json"
            : System.IO.Path.GetFileName(CurrentSettingsFilePath);

        var pickedPath = await _dialogService.SaveSettingsFileDialogAsync(defaultName);
        if (!string.IsNullOrWhiteSpace(pickedPath))
        {
            CurrentSettingsFilePath = pickedPath;
            SaveSettingsToPath(pickedPath);
        }
    }

    [RelayCommand]
    public async Task LoadWorkspaceSettingsFromFileAsync()
    {
        var pickedPath = await _dialogService.OpenSettingsFileDialogAsync();
        if (!string.IsNullOrWhiteSpace(pickedPath))
        {
            CurrentSettingsFilePath = pickedPath;
            LoadSettings(pickedPath);
        }
    }

    private void SaveSettingsToPath(string? path)
    {
        CaptureCurrentTags();

        var settings = new AppSettings
        {
            ReadOnlyMode = ReadOnlyMode,
            AddressFormat = GetAddressFormatMode(),
            DefaultSlaveId = SlaveId,
            DefaultStartAddress = ModbusPlcAddressFormatter.ParseAddress(StartAddressText).RawAddress,
            DefaultRegisterCount = RegisterCount,
            DefaultTableType = SelectedTableType,
            DefaultDisplayFormat = GetDisplayFormat(),
            DefaultEndianness = GetEndiannessOrder(),
            Connection = new ConnectionConfig
            {
                ConnectionType = ConnectionType switch
                {
                    "ASCII" => Core.Models.ConnectionType.Ascii,
                    "RTU" => Core.Models.ConnectionType.Rtu,
                    _ => Core.Models.ConnectionType.Tcp
                },
                Host = TcpHost,
                Port = TcpPort,
                SerialPortName = SerialPort,
                BaudRate = BaudRate
            },
            CustomTags = _userTags.Select(kv =>
            {
                var parts = kv.Key.Split(':');
                Enum.TryParse<ModbusTableType>(parts[0], out var tt);
                ushort.TryParse(parts[1], out var addr);
                return new TagDefinition
                {
                    TableType = tt,
                    Address = addr,
                    TagName = kv.Value.TagName,
                    Description = kv.Value.Description
                };
            }).ToList()
        };

        _configService.SaveSettings(settings, path);
        string displayPath = path ?? _configService.ConfigFilePath;
        SettingsStatusMessage = $"Saved: {System.IO.Path.GetFileName(displayPath)} ({_userTags.Count} tags) at {DateTime.Now:HH:mm:ss}";
    }

    [RelayCommand]
    public void ToggleSimulator()
    {
        if (IsSimulatorRunning)
        {
            _simulatorServer?.Stop();
            _simulatorServer?.Dispose();
            _simulatorServer = null;
            IsSimulatorRunning = false;
        }
        else
        {
            _simulatorServer = new ModbusSimulatorServer();
            _simulatorServer.Start(5020);
            IsSimulatorRunning = true;
            TcpPort = 5020;
            TcpHost = "127.0.0.1";
        }
    }

    [RelayCommand]
    public async Task ExecuteWriteAsync()
    {
        if (!IsConnected || _master == null)
        {
            WriteStatusMessage = "Cannot write: not connected.";
            return;
        }

        if (ReadOnlyMode)
        {
            WriteStatusMessage = "Write Blocked: Read-Only Mode is active. Disable Read-Only Mode to authorize writes.";
            return;
        }

        try
        {
            WriteStatusMessage = "Writing...";

            var parsed = ModbusPlcAddressFormatter.ParseAddress(WriteAddressText);
            ushort rawAddr = parsed.RawAddress;

            var table = parsed.InferredTable ?? (SelectedTableType == "Coil" ? ModbusTableType.Coil : ModbusTableType.HoldingRegister);

            if (table == ModbusTableType.Coil)
            {
                bool val = WriteValueText == "1" || WriteValueText.Equals("true", StringComparison.OrdinalIgnoreCase) || WriteValueText.Equals("on", StringComparison.OrdinalIgnoreCase);
                await _master.WriteSingleCoilAsync(SlaveId, rawAddr, val);

                _auditService.Log(new AuditLogEntry(
                    DateTime.UtcNow,
                    "WriteSingleCoil",
                    SlaveId,
                    rawAddr,
                    "Unknown",
                    val.ToString(),
                    true,
                    "Engineer",
                    $"Manual UI Write (PLC {WriteAddressText})"));

                WriteStatusMessage = $"Successfully wrote Coil {WriteAddressText} (Raw {rawAddr}) = {val}";
            }
            else
            {
                ushort val = ushort.Parse(WriteValueText.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToUInt16(WriteValueText, 16).ToString()
                    : WriteValueText);

                await _master.WriteSingleRegisterAsync(SlaveId, rawAddr, val);

                // Read-Back Verification
                var verify = await _master.ReadHoldingRegistersAsync(SlaveId, rawAddr, 1);
                bool verified = verify.Length > 0 && verify[0] == val;

                _auditService.Log(new AuditLogEntry(
                    DateTime.UtcNow,
                    "WriteSingleRegister",
                    SlaveId,
                    rawAddr,
                    "Unknown",
                    $"0x{val:X4}",
                    verified,
                    "Engineer",
                    verified ? "Read-back verified" : "Read-back mismatch"));

                WriteStatusMessage = verified
                    ? $"Success: Wrote & Verified Reg {WriteAddressText} (Raw {rawAddr}) = 0x{val:X4} ({val})"
                    : $"Warning: Write sent but read-back returned 0x{verify.FirstOrDefault():X4}";
            }

            SyncAuditLogs();
        }
        catch (Exception ex)
        {
            WriteStatusMessage = $"Write Failed: {ex.Message}";
            _auditService.Log(new AuditLogEntry(
                DateTime.UtcNow,
                "WriteError",
                SlaveId,
                0,
                "-",
                WriteValueText,
                false,
                "Engineer",
                ex.Message));
            SyncAuditLogs();
        }
    }

    [RelayCommand]
    public async Task StartDeviceDiscoveryAsync()
    {
        if (!IsConnected || _master == null)
        {
            DiscoveryStatus = "Connect to device/bus first to perform discovery.";
            return;
        }

        DiscoveredDevices.Clear();
        DiscoveryStatus = "Scanning slave IDs 1 to 16...";
        var scanner = new ModbusDeviceScanner(_master);
        var progress = new Progress<(byte currentId, int percent)>(p =>
        {
            DiscoveryProgress = p.percent;
            DiscoveryStatus = $"Scanning Slave ID {p.currentId} ({p.percent}%)...";
        });

        var results = await scanner.ScanSlaveIdsAsync(1, 16, progress);
        foreach (var dev in results)
        {
            DiscoveredDevices.Add(dev);
        }

        DiscoveryStatus = $"Discovery complete: {results.Count} device(s) found.";
    }

    [RelayCommand]
    public async Task RunCommissioningSequenceAsync()
    {
        if (!IsConnected || _master == null) return;

        var runner = new DiagnosticSequenceRunner(_master);
        var steps = runner.CreateStandardCommissioningSequence(SlaveId);
        SequenceSteps.Clear();
        foreach (var s in steps) SequenceSteps.Add(s);

        var progress = new Progress<SequenceStep>(step =>
        {
            int idx = SequenceSteps.IndexOf(step);
            if (idx >= 0)
            {
                SequenceSteps[idx] = step;
            }
        });

        await runner.RunSequenceAsync(steps, progress);
    }

    [RelayCommand]
    public void ClearRawFrames()
    {
        RawFrames.Clear();
    }

    private void Master_FrameCaptured(object? sender, ModbusRawFrame frame)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            RawFrames.Insert(0, frame);
            if (RawFrames.Count > 500)
            {
                RawFrames.RemoveAt(RawFrames.Count - 1);
            }
            TotalFrames++;
        });
    }

    private void PollingEngine_DataPolled(object? sender, IReadOnlyList<ModbusRegisterItem> items)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var fmt = GetDisplayFormat();
            var end = GetEndiannessOrder();
            var af = GetAddressFormatMode();

            // If collection is empty or count changed, populate from scratch
            if (RegisterItems.Count != items.Count)
            {
                RegisterItems.Clear();
                foreach (var item in items)
                {
                    item.DisplayFormat = fmt;
                    item.Endianness = end;
                    item.AddressFormat = af;

                    // Restore user tag if previously customized
                    string key = $"{item.TableType}:{item.Address}";
                    if (_userTags.TryGetValue(key, out var tag))
                    {
                        item.TagName = tag.TagName;
                        item.Description = tag.Description;
                    }
                    item.UpdateFormattedValue();
                    RegisterItems.Add(item);
                }
            }
            else
            {
                // In-place value update to preserve user-edited text in cells
                for (int i = 0; i < items.Count; i++)
                {
                    RegisterItems[i].DisplayFormat = fmt;
                    RegisterItems[i].Endianness = end;
                    RegisterItems[i].AddressFormat = af;
                    RegisterItems[i].BooleanValue = items[i].BooleanValue;
                    RegisterItems[i].RawValue = items[i].RawValue;
                    RegisterItems[i].UpdateAdjacentRegisters(items[i].NextWord1, items[i].NextWord2, items[i].NextWord3);
                    RegisterItems[i].LastUpdated = items[i].LastUpdated;
                }
            }
        });
    }

    private void CaptureCurrentTags()
    {
        foreach (var item in RegisterItems)
        {
            if (!string.IsNullOrWhiteSpace(item.TagName) || !string.IsNullOrWhiteSpace(item.Description))
            {
                string key = $"{item.TableType}:{item.Address}";
                _userTags[key] = (item.TagName, item.Description);
            }
        }
    }

    private void PollingEngine_MetricsUpdated(object? sender, CommunicationHealthMetrics metrics)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            SuccessRate = metrics.PacketSuccessRate;
            LatencyMs = Math.Round(metrics.LastResponseTimeMs, 1);
            CrcErrors = metrics.CrcErrors;
            TimeoutErrors = metrics.TimeoutErrors;
        });
    }

    private void SyncAuditLogs()
    {
        AuditLogs.Clear();
        foreach (var a in _auditService.Entries)
        {
            AuditLogs.Add(a);
        }
    }

    private void LoadSettings(string? path = null)
    {
        var settings = _configService.LoadSettings(path);
        ReadOnlyMode = settings.ReadOnlyMode;

        ConnectionType = settings.Connection.ConnectionType switch
        {
            Core.Models.ConnectionType.Ascii => "ASCII",
            Core.Models.ConnectionType.Rtu => "RTU",
            _ => "TCP"
        };

        TcpHost = settings.Connection.Host;
        TcpPort = settings.Connection.Port;
        BaudRate = settings.Connection.BaudRate;
        SerialPort = settings.Connection.SerialPortName;

        SelectedAddressFormat = settings.AddressFormat switch
        {
            AddressFormatMode.RawZeroBased => "0-Based Raw (0)",
            AddressFormatMode.Plc6Digit => "PLC 6-Digit (400001)",
            _ => "PLC 5-Digit (40001)"
        };

        SelectedDisplayFormat = settings.DefaultDisplayFormat switch
        {
            DataDisplayFormat.Int16 => "Int16",
            DataDisplayFormat.Float32 => "Float32",
            DataDisplayFormat.Double64 => "Double64",
            DataDisplayFormat.Int32 => "Int32",
            DataDisplayFormat.UInt32 => "UInt32",
            DataDisplayFormat.Int64 => "Int64",
            DataDisplayFormat.Ascii => "ASCII",
            DataDisplayFormat.Hexadecimal => "Hex",
            DataDisplayFormat.Binary => "Binary",
            _ => "UInt16"
        };

        SelectedEndianness = settings.DefaultEndianness switch
        {
            EndiannessOrder.LittleEndian => "Little-Endian (DCBA / LSB-MSB)",
            EndiannessOrder.WordSwap => "Word-Swap (CDAB)",
            EndiannessOrder.ByteSwap => "Byte-Swap (BADC)",
            _ => "Big-Endian (ABCD / MSB-LSB)"
        };

        SlaveId = settings.DefaultSlaveId;
        SelectedTableType = string.IsNullOrEmpty(settings.DefaultTableType) ? "HoldingRegister" : settings.DefaultTableType;
        RegisterCount = settings.DefaultRegisterCount > 0 ? settings.DefaultRegisterCount : (ushort)10;

        StartAddressText = ModbusPlcAddressFormatter.FormatAddress(
            settings.DefaultStartAddress,
            Enum.TryParse<ModbusTableType>(SelectedTableType, out var tt) ? tt : ModbusTableType.HoldingRegister,
            settings.AddressFormat);

        WriteAddressText = StartAddressText;

        // Load custom tags
        _userTags.Clear();
        if (settings.CustomTags != null)
        {
            foreach (var t in settings.CustomTags)
            {
                _userTags[$"{t.TableType}:{t.Address}"] = (t.TagName, t.Description);
            }
        }

        string sourceName = !string.IsNullOrWhiteSpace(path) 
            ? System.IO.Path.GetFileName(path) 
            : "Default";
        SettingsStatusMessage = $"Active Config: {sourceName} ({_userTags.Count} tags loaded)";
    }
}
