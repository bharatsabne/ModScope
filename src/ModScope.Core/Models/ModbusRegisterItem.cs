using System.ComponentModel;
using System.Runtime.CompilerServices;
using ModScope.ModbusMaster;

namespace ModScope.Core.Models;

/// <summary>
/// Represents a monitored Modbus register or coil cell with customizable user tag, description,
/// and live formatted decoding (UInt16, Int16, Float32, Double64, Int32, UInt32, Ascii, Binary, Hex).
/// </summary>
public class ModbusRegisterItem : INotifyPropertyChanged
{
    private ushort _address;
    private ushort _rawValue;
    private bool _booleanValue;
    private string _formattedValue = string.Empty;
    private string _tagName = string.Empty;
    private string _description = string.Empty;
    private ModbusTableType _tableType;
    private DateTime _lastUpdated = DateTime.UtcNow;
    private AddressFormatMode _addressFormat = AddressFormatMode.Plc5Digit;
    private DataDisplayFormat _displayFormat = DataDisplayFormat.UInt16;
    private EndiannessOrder _endianness = EndiannessOrder.BigEndian;

    // References to adjacent 3 registers for 32-bit (2 regs) and 64-bit (4 regs / first 8 bytes) decoding
    private ushort _nextWord1;
    private ushort _nextWord2;
    private ushort _nextWord3;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ushort Address
    {
        get => _address;
        set
        {
            if (_address != value)
            {
                _address = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlcAddress));
            }
        }
    }

    /// <summary>
    /// PLC formatted address string (e.g. 40001, 30001, 00001, 10001) or raw 0-based address.
    /// </summary>
    public string PlcAddress => ModbusPlcAddressFormatter.FormatAddress(Address, TableType, AddressFormat);

    public AddressFormatMode AddressFormat
    {
        get => _addressFormat;
        set
        {
            if (_addressFormat != value)
            {
                _addressFormat = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlcAddress));
            }
        }
    }

    public ushort RawValue
    {
        get => _rawValue;
        set
        {
            if (_rawValue != value)
            {
                _rawValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HexValue));
                OnPropertyChanged(nameof(BinaryValue));
                UpdateFormattedValue();
            }
        }
    }

    public ushort NextWord1
    {
        get => _nextWord1;
        set
        {
            if (_nextWord1 != value)
            {
                _nextWord1 = value;
                UpdateFormattedValue();
            }
        }
    }

    public ushort NextWord2
    {
        get => _nextWord2;
        set
        {
            if (_nextWord2 != value)
            {
                _nextWord2 = value;
                UpdateFormattedValue();
            }
        }
    }

    public ushort NextWord3
    {
        get => _nextWord3;
        set
        {
            if (_nextWord3 != value)
            {
                _nextWord3 = value;
                UpdateFormattedValue();
            }
        }
    }

    public bool BooleanValue
    {
        get => _booleanValue;
        set
        {
            if (_booleanValue != value)
            {
                _booleanValue = value;
                OnPropertyChanged();
                UpdateFormattedValue();
            }
        }
    }

    public DataDisplayFormat DisplayFormat
    {
        get => _displayFormat;
        set
        {
            if (_displayFormat != value)
            {
                _displayFormat = value;
                OnPropertyChanged();
                UpdateFormattedValue();
            }
        }
    }

    public EndiannessOrder Endianness
    {
        get => _endianness;
        set
        {
            if (_endianness != value)
            {
                _endianness = value;
                OnPropertyChanged();
                UpdateFormattedValue();
            }
        }
    }

    public string FormattedValue
    {
        get => _formattedValue;
        set
        {
            if (_formattedValue != value)
            {
                _formattedValue = value;
                OnPropertyChanged();
            }
        }
    }

    public string HexValue => $"0x{RawValue:X4}";

    public string BinaryValue => Convert.ToString(RawValue, 2).PadLeft(16, '0');

    public DateTime LastUpdated
    {
        get => _lastUpdated;
        set
        {
            if (_lastUpdated != value)
            {
                _lastUpdated = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// User-defined tag name (e.g. "Tank_Pressure", "Motor_Speed").
    /// </summary>
    public string TagName
    {
        get => _tagName;
        set
        {
            if (_tagName != value)
            {
                _tagName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// User-defined tag description.
    /// </summary>
    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged();
            }
        }
    }

    public ModbusTableType TableType
    {
        get => _tableType;
        set
        {
            if (_tableType != value)
            {
                _tableType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlcAddress));
                UpdateFormattedValue();
            }
        }
    }

    public void UpdateAdjacentRegisters(ushort w1, ushort w2, ushort w3)
    {
        _nextWord1 = w1;
        _nextWord2 = w2;
        _nextWord3 = w3;
        UpdateFormattedValue();
    }

    public void UpdateFormattedValue()
    {
        if (TableType == ModbusTableType.Coil || TableType == ModbusTableType.DiscreteInput)
        {
            FormattedValue = BooleanValue ? "ON" : "OFF";
            return;
        }

        try
        {
            FormattedValue = DisplayFormat switch
            {
                DataDisplayFormat.UInt16 => ModbusDataConverter.ToUInt16(RawValue, Endianness == EndiannessOrder.ByteSwap).ToString(),
                DataDisplayFormat.Int16 => ModbusDataConverter.ToInt16(RawValue, Endianness == EndiannessOrder.ByteSwap).ToString(),
                DataDisplayFormat.Hexadecimal => $"0x{RawValue:X4}",
                DataDisplayFormat.Binary => Convert.ToString(RawValue, 2).PadLeft(16, '0'),
                DataDisplayFormat.Float32 => FormatFloat(ModbusDataConverter.ToSingle(RawValue, NextWord1, Endianness)),
                DataDisplayFormat.Double64 => FormatDouble(ModbusDataConverter.ToDouble(RawValue, NextWord1, NextWord2, NextWord3, Endianness)),
                DataDisplayFormat.Int32 => ModbusDataConverter.ToInt32(RawValue, NextWord1, Endianness).ToString(),
                DataDisplayFormat.UInt32 => ModbusDataConverter.ToUInt32(RawValue, NextWord1, Endianness).ToString(),
                DataDisplayFormat.Int64 => ModbusDataConverter.ToInt64(RawValue, NextWord1, NextWord2, NextWord3, Endianness).ToString(),
                DataDisplayFormat.Ascii => ModbusDataConverter.ToAsciiString([RawValue, NextWord1, NextWord2, NextWord3], Endianness == EndiannessOrder.ByteSwap),
                _ => RawValue.ToString()
            };
        }
        catch
        {
            FormattedValue = RawValue.ToString();
        }
    }

    private static string FormatFloat(float val)
    {
        if (float.IsNaN(val)) return "NaN";
        if (float.IsInfinity(val)) return float.IsPositiveInfinity(val) ? "+Infinity" : "-Infinity";
        if (Math.Abs(val) < 1e-15f && val != 0f) return "0.0";
        if (Math.Abs(val) >= 0.0001f && Math.Abs(val) < 1000000f)
            return val.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        return val.ToString("G7", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatDouble(double val)
    {
        if (double.IsNaN(val)) return "NaN";
        if (double.IsInfinity(val)) return double.IsPositiveInfinity(val) ? "+Infinity" : "-Infinity";
        if (Math.Abs(val) < 1e-30 && val != 0.0) return "0.0";
        if (Math.Abs(val) >= 0.000001 && Math.Abs(val) < 10000000000.0)
            return val.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        return val.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Audit trail entry for tracking all write operations, overrides, and parameter updates.
/// </summary>
public record AuditLogEntry(
    DateTime Timestamp,
    string Operation,
    byte SlaveId,
    ushort Address,
    string OldValue,
    string NewValue,
    bool Success,
    string OperatorName = "Engineer",
    string? Notes = null
);
