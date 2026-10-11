# ModScope.ModbusMaster

**ModScope.ModbusMaster** is a high-performance, platform-independent .NET library for Modbus communication supporting both **Modbus RTU** (Serial RS-232 / RS-485) and **Modbus TCP** (Ethernet).

Designed cleanly with zero UI dependencies, asynchronous I/O (`Task`, `CancellationToken`), robust CRC-16 computation, and microsecond-level raw frame capturing.

## Features

- **Protocol Coverage:** Standard Modbus Function Codes:
  - `FC01` Read Coils
  - `FC02` Read Discrete Inputs
  - `FC03` Read Holding Registers
  - `FC04` Read Input Registers
  - `FC05` Write Single Coil
  - `FC06` Write Single Register
  - `FC15` Write Multiple Coils
  - `FC16` Write Multiple Registers
  - `FC22` Mask Write Register
  - `FC23` Read/Write Multiple Registers
- **Cross-Platform:** Runs seamlessly on Windows x64, Linux x64, and Linux ARM64.
- **Diagnostics:** Subscribed `FrameCaptured` events expose raw byte payloads, transmission direction (`Tx`/`Rx`), descriptions, and error states.
- **Data Conversion:** Endianness support for IEEE-754 floats, signed/unsigned 32-bit integers, and ASCII strings (BigEndian ABCD, LittleEndian DCBA, ByteSwap BADC, WordSwap CDAB).

## Installation

```bash
dotnet add package ModScope.ModbusMaster
```

## Quick Start: Modbus TCP

```csharp
using ModScope.ModbusMaster;

using var transport = new ModbusTcpTransport("192.168.1.100", 502, timeoutMs: 2000);
using var master = new ModbusTcpMaster(transport);

await master.ConnectAsync();

// Read 10 holding registers starting at address 0 from Slave ID 1
ushort[] registers = await master.ReadHoldingRegistersAsync(slaveId: 1, startAddress: 0, count: 10);

// Convert registers 2 & 3 into a float
float temperature = ModbusDataConverter.ToSingle(registers[2], registers[3], EndiannessOrder.WordSwap);

Console.WriteLine($"Temperature: {temperature:F2} °C");
```

## Quick Start: Modbus RTU (Serial RS-485)

```csharp
using ModScope.ModbusMaster;
using System.IO.Ports;

// Linux: "/dev/ttyUSB0" | Windows: "COM3"
using var transport = new ModbusSerialTransport("COM3", baudRate: 9600, parity: Parity.None, timeoutMs: 2000);
using var master = new ModbusRtuMaster(transport);

// Listen to raw traffic
master.FrameCaptured += (sender, frame) =>
{
    Console.WriteLine($"[{frame.Timestamp:HH:mm:ss.fff}] {frame.Direction}: {frame.HexString} ({frame.Description})");
};

await master.ConnectAsync();
ushort[] data = await master.ReadHoldingRegistersAsync(slaveId: 1, startAddress: 100, count: 2);
```

## License

MIT License.
