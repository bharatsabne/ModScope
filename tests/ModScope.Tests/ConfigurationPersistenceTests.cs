using System.IO;
using ModScope.Core.Models;
using ModScope.Infrastructure.Configuration;
using Xunit;

namespace ModScope.Tests;

public class ConfigurationPersistenceTests
{
    [Fact]
    public void ConfigurationService_SaveAndLoad_CustomFile_PreservesAllSettingsAndTags()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"modscope_test_cfg_{Guid.NewGuid():N}.json");
        try
        {
            var configService = new ConfigurationService();

            var settings = new AppSettings
            {
                ReadOnlyMode = false,
                AddressFormat = AddressFormatMode.Plc5Digit,
                DefaultSlaveId = 3,
                DefaultStartAddress = 100,
                DefaultRegisterCount = 20,
                DefaultTableType = "InputRegister",
                DefaultDisplayFormat = DataDisplayFormat.Float32,
                DefaultEndianness = ModScope.ModbusMaster.EndiannessOrder.WordSwap,
                Connection = new ConnectionConfig
                {
                    ConnectionType = ConnectionType.Ascii,
                    Host = "192.168.1.50",
                    Port = 502,
                    SerialPortName = "COM4",
                    BaudRate = 19200
                },
                CustomTags = new List<TagDefinition>
                {
                    new() { TableType = ModbusTableType.HoldingRegister, Address = 0, TagName = "Zone1_Temp", Description = "Zone 1 temperature C" },
                    new() { TableType = ModbusTableType.Coil, Address = 5, TagName = "Pump_Run_Cmd", Description = "Main pump relay run trigger" }
                }
            };

            // Save to arbitrary custom file location
            configService.SaveSettings(settings, tempFile);
            Assert.True(File.Exists(tempFile));

            // Load from that arbitrary file location
            var loaded = configService.LoadSettings(tempFile);

            Assert.False(loaded.ReadOnlyMode);
            Assert.Equal(AddressFormatMode.Plc5Digit, loaded.AddressFormat);
            Assert.Equal(3, loaded.DefaultSlaveId);
            Assert.Equal(100, loaded.DefaultStartAddress);
            Assert.Equal(20, loaded.DefaultRegisterCount);
            Assert.Equal("InputRegister", loaded.DefaultTableType);
            Assert.Equal(DataDisplayFormat.Float32, loaded.DefaultDisplayFormat);
            Assert.Equal(ModScope.ModbusMaster.EndiannessOrder.WordSwap, loaded.DefaultEndianness);

            Assert.Equal(ConnectionType.Ascii, loaded.Connection.ConnectionType);
            Assert.Equal("192.168.1.50", loaded.Connection.Host);
            Assert.Equal(502, loaded.Connection.Port);
            Assert.Equal("COM4", loaded.Connection.SerialPortName);
            Assert.Equal(19200, loaded.Connection.BaudRate);

            Assert.Equal(2, loaded.CustomTags.Count);
            Assert.Equal("Zone1_Temp", loaded.CustomTags[0].TagName);
            Assert.Equal("Pump_Run_Cmd", loaded.CustomTags[1].TagName);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
