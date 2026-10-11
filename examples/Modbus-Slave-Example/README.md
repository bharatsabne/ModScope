# Modbus Slave Example

This folder contains a complete reference Modbus TCP Slave (Server) application and library for testing and commissioning with **ModScope**.

---

## Overview

- **JModbus**: Lightweight in-memory Modbus TCP Slave library implementing function codes `01` (Read Coils), `02` (Read Discrete Inputs), `03` (Read Holding Registers), `04` (Read Input Registers), `05` (Write Single Coil), and `16` (Write Multiple Registers).
- **Modbus Example**: Windows desktop test application (`Strtup.cs`) that updates dynamic sensor data:
  - **Address 0 (PLC 30001)**: Dynamic Transmission Counter (`modbus.TransmitCount()`).
  - **Address 2–4 (PLC 30003–30005)**: ASCII String (`"Bharat"` / `"Pooja"`).
  - **Address 7–9 (PLC 30008–30010)**: ASCII String (`"INDIA"` / `"MST01"`).
  - **Address 12–15 (PLC 30013–30016)**: 64-bit Double (`100.890d`, 4 registers).
  - **Address 17–18 (PLC 30018–30019)**: 32-bit Float (`3.14159f` / `9.81f`, 2 registers).

---

## How to Run and Test with ModScope

1. **Build and Launch Example Slave**:
   ```bash
   dotnet build "examples/Modbus-Slave-Example/Modbus Example.sln"
   ```
   Or run the precompiled binary in `Modbus Example/bin/Debug/Modbus Example.exe`.

2. **Connect from ModScope Studio**:
   - Connection Type: **TCP**
   - Host: `127.0.0.1`
   - Port: `502`
   - Click **Connect**

3. **Verify Registers in Data Monitor**:
   - **Table**: Select `InputRegister`
   - **Start Addr**: `30001` (or raw `0`)
   - **Count**: `20`
   - **Display Format**: Toggle between `ASCII`, `Float32`, `Double64`, and `UInt16` to observe dynamic updates.
