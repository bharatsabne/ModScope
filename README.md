# ModScope — Cross-Platform Modbus Diagnostic Studio

[![Build and Test](https://github.com/bharatsabne/ModScope/actions/workflows/ci.yml/badge.svg)](https://github.com/bharatsabne/ModScope/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/badge/nuget-v1.0.0-blue)](https://www.nuget.org/packages/ModScope.ModbusMaster)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey)](#cross-platform-support)

**ModScope** is a professional, open-source industrial Modbus diagnostic studio and protocol analyzer built with C#, .NET 8 LTS, Avalonia UI, and MVVM.

It provides commissioning engineers, automation technicians, and embedded systems developers with an intuitive suite to monitor, analyze, test, and troubleshoot Modbus RTU and TCP communication networks.

---

## Key Highlights

- **Multi-Protocol Support:** Serial RTU (RS-232, RS-422, RS-485), Serial ASCII (with LRC validation), and Ethernet Modbus TCP.
- **Independent Master Library:** `ModScope.ModbusMaster` is a zero-UI dependency, high-performance library publishable directly to NuGet.
- **Safety First:** Default **Read-Only Mode** prevents inadvertent equipment actuation; write operations require explicit operator authorization and offer read-back verification.
- **Rich Industrial Decoders:** 16-bit UInt/Int, 32-bit UInt/Int, **32-bit Float32 (2 registers)**, **64-bit Double64 (4 registers / 8 bytes)**, 64-bit Int64, Hex, Binary, and ASCII strings with configurable byte/word ordering (ABCD, DCBA, CDAB, BADC).
- **PLC Notation & Tag Customization:** Supports standard PLC addressing (`00001` Coils, `10001` Discrete Inputs, `30001` Input Registers, `40001` Holding Registers) or 0-based raw protocol addressing. Customizable tag names and descriptions with persistent JSON workspace configuration.
- **Industrial IPC / Low-Res Screen Ergonomics:** Starts maximized by default with responsive, scrollable toolbars designed for 1366x768 industrial touchscreen panels up to 4K displays.
- **Real-Time Frame Analyzer:** Microsecond-accurate TX/RX packet sniffer displaying raw hex bytes, directional flows, and CRC/LRC checks.
- **Built-in Device Simulator & Example Slave:** Integrated Modbus TCP server (port 5020) and bundled `examples/Modbus-Slave-Example` with dynamic float, double, integer, and string register feeds.
- **Automated Commissioning Sequences:** One-click health validation, endurance loopback, and read-back diagnostics.
- **Device Bus Discovery:** Automated address scan across slave IDs 1–247.
- **Audit History & Reporting:** Complete tamper-evident audit trail with export to CSV, JSON, and professional Markdown/HTML diagnostic reports.
- **Industrial Dark Theme:** High-contrast ergonomic UI designed for factory and control-room environments.

---

## Solution Architecture

```
ModScope.sln
├── src/
│   ├── ModScope.ModbusMaster/     # Reusable, standalone Modbus RTU/TCP/ASCII master library (NuGet ready)
│   ├── ModScope.Core/             # Shared domain entities, models, and contracts
│   ├── ModScope.Diagnostics/      # Polling engine, device discovery, simulator, test sequences
│   ├── ModScope.Infrastructure/   # Settings persistence, CSV/JSON reporting, audit logs
│   └── ModScope.App/              # Cross-platform Avalonia UI desktop application
├── examples/
│   └── Modbus-Slave-Example/      # Sample Modbus TCP Slave application with float/double/string registers
└── tests/
    └── ModScope.Tests/            # Unit tests (CRC, conversions) & integration tests (simulator)
```

---

## Installation & Compilation Guide (Per Operating System)

### 🪟 Windows (Windows 10, Windows 11, Windows Server)

#### 1. System Requirements & Installation
- Install [.NET 8.0 SDK (x64)](https://dotnet.microsoft.com/download/dotnet/8.0) or run via winget:
  ```powershell
  winget install Microsoft.DotNet.SDK.8
  ```
- **Serial Ports (RS-485 / RS-232):** Automatically enumerated as `COM1`, `COM2`, etc. FTDI, CP2102, CH340, and PL2303 USB-to-RS485 adapters work immediately with manufacturer drivers. No special user permission is needed.

#### 2. Build & Run
```powershell
# Clone and enter directory
git clone https://github.com/bharatsabne/ModScope.git
cd ModScope

# Restore packages & compile
dotnet restore ModScope.sln
dotnet build ModScope.sln -c Release

# Run automated tests
dotnet test ModScope.sln

# Launch desktop app
dotnet run --project src/ModScope.App/ModScope.App.csproj -c Release
```

#### 3. Publish Single-File Standalone `.exe` (No .NET installation required on client PC)
```powershell
dotnet publish src/ModScope.App/ModScope.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./artifacts/publish/win-x64
```
> Executable generated at: `./artifacts/publish/win-x64/ModScope.App.exe`

---

### 🐧 Linux (Ubuntu, Debian, Fedora, RHEL, Arch Linux, Alpine)

#### 1. System Requirements & Installation
- **Install .NET 8.0 SDK:**
  - **Ubuntu / Debian:**
    ```bash
    sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
    ```
  - **Fedora / RHEL:**
    ```bash
    sudo dnf install -y dotnet-sdk-8.0
    ```
  - **Arch Linux:**
    ```bash
    sudo pacman -S dotnet-sdk
    ```

- **GUI & Graphics Dependencies:**
  Avalonia uses Skia and X11/Wayland. Ensure the following standard libraries are installed:
  ```bash
  # Ubuntu / Debian
  sudo apt-get install -y libx11-6 libx11-xcb1 libice6 libsm6 libfontconfig1 libice6
  ```

- **Serial Port Permissions (RS-485 / USB-to-UART):**
  Serial ports map to `/dev/ttyUSB*` (USB converters) or `/dev/ttyS*` (hardware UART).
  To read/write serial ports without `sudo`, grant group permissions to your user:
  ```bash
  # Debian/Ubuntu (dialout group)
  sudo usermod -a -G dialout $USER

  # Arch/Fedora/RHEL (uucp group)
  sudo usermod -a -G uucp $USER
  ```
  *(Log out and log back in for group changes to activate).*

#### 2. Build & Run
```bash
git clone https://github.com/modscope/modscope.git
cd modscope

dotnet restore ModScope.sln
dotnet build ModScope.sln -c Release
dotnet test ModScope.sln
dotnet run --project src/ModScope.App/ModScope.App.csproj -c Release
```

#### 3. Publish Single-File Executable
- **Linux x64 (Standard PCs / IPCs):**
  ```bash
  dotnet publish src/ModScope.App/ModScope.App.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o ./artifacts/publish/linux-x64
  ```
  Run with:
  ```bash
  chmod +x ./artifacts/publish/linux-x64/ModScope.App
  ./artifacts/publish/linux-x64/ModScope.App
  ```

- **Linux ARM64 (Raspberry Pi 4/5, Advantech/Moxa Industrial Gateways):**
  ```bash
  dotnet publish src/ModScope.App/ModScope.App.csproj \
    -c Release \
    -r linux-arm64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o ./artifacts/publish/linux-arm64
  ```

---

### 🍎 macOS (Apple Silicon M1/M2/M3/M4 & Intel x64)

#### 1. System Requirements & Installation
- Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or use Homebrew:
  ```bash
  brew install --cask dotnet-sdk
  ```
- **Serial Ports:** USB-to-RS485 adapters enumerate as `/dev/cu.usbserial-*` or `/dev/tty.usbserial-*`. Standard FTDI driver permissions apply.

#### 2. Build & Run
```bash
git clone https://github.com/modscope/modscope.git
cd modscope

dotnet restore ModScope.sln
dotnet build ModScope.sln -c Release
dotnet test ModScope.sln
dotnet run --project src/ModScope.App/ModScope.App.csproj -c Release
```

#### 3. Publish Standalone macOS App Bundle
- **Apple Silicon (ARM64):**
  ```bash
  dotnet publish src/ModScope.App/ModScope.App.csproj \
    -c Release \
    -r osx-arm64 \
    --self-contained true \
    -o ./artifacts/publish/osx-arm64
  ```
- **Intel Mac (x64):**
  ```bash
  dotnet publish src/ModScope.App/ModScope.App.csproj \
    -c Release \
    -r osx-x64 \
    --self-contained true \
    -o ./artifacts/publish/osx-x64
  ```

---

## NuGet Package Generation (`ModScope.ModbusMaster`)

To compile, version, and produce the standalone, UI-independent `ModScope.ModbusMaster` NuGet package:

```bash
dotnet pack src/ModScope.ModbusMaster/ModScope.ModbusMaster.csproj \
  -c Release \
  -o ./artifacts/packages
```

To consume the compiled package in your own application:
```bash
dotnet add package ModScope.ModbusMaster --source ./artifacts/packages
```

---

## Testing with the Bundled Modbus Slave Example

ModScope includes a complete reference Modbus TCP Slave application located in [`examples/Modbus-Slave-Example/`](examples/Modbus-Slave-Example/README.md) for live end-to-end commissioning without physical hardware.

### Register Mapping

| PLC Address | Protocol Address | Modbus Table | Data Type | Registers | Example Values |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`30001`** | `0` | Input Register | **UInt16** | 1 | Dynamic Transmit Counter (`TransmitCount()`) |
| **`30003`**–**`30005`** | `2`–`4` | Input Register | **ASCII** | 3 | `"Bharat"` / `"Pooja"` (alternating) |
| **`30008`**–**`30010`** | `7`–`9` | Input Register | **ASCII** | 3 | `"INDIA"` / `"MST01"` (alternating) |
| **`30013`**–**`30016`** | `12`–`15` | Input Register | **Double64** | 4 (8 bytes) | `100.890` (64-bit IEEE 754) |
| **`30018`**–**`30019`** | `17`–`18` | Input Register | **Float32** | 2 (4 bytes) | `3.14159` / `9.81` (32-bit IEEE 754) |

### Step-by-Step Testing Procedure

1. **Start the Example Slave:**
   Run the precompiled binary or build from source:
   ```powershell
   & "examples/Modbus-Slave-Example/Modbus Example/bin/Debug/Modbus Example.exe"
   ```

2. **Connect in ModScope Studio:**
   - **Connection Type:** Select `TCP`
   - **Host:** `127.0.0.1`
   - **Port:** `502`
   - Click **Connect** (Status updates to `Connected (TCP: 127.0.0.1:502)`)

3. **Monitor Live Registers:**
   - **Table:** Select `InputRegister`
   - **Start Addr:** Enter `30001` (PLC notation) or `0` (Raw)
   - **Count:** Enter `20`
   - Click **Start Polling** (Button dynamically turns red to display **Stop Polling**)

4. **Verify Industrial Decoding:**
   - Select **Type: `Float32`** to observe the 2-register float at address `30018` (`3.14159` / `9.81`).
   - Select **Type: `Double64`** to observe the 4-register double at address `30013` (`100.890`).
   - Select **Type: `ASCII`** to observe string decoding at addresses `30003` and `30008`.

---

## Contributing & License

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

Licensed under the permissive [MIT License](LICENSE).
