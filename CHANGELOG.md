# Changelog

All notable changes to the **ModScope** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-10-11

### Added
- **Modbus ASCII Protocol:** Complete serial ASCII master transport with ASCII frame framing (`:`, `CR`, `LF`) and Longitudinal Redundancy Check (LRC) computation.
- **64-bit Double Precision (Double64):** Decoding and packing across 4 consecutive 16-bit registers (8 bytes) with IEEE 754 precision and clean industrial formatting.
- **32-bit Float32 Pre-fetching:** Lookahead buffer reading `Count + 3` registers so that all monitored rows up to the range limit have complete adjacent words (`NextWord1..3`).
- **Standard PLC Addressing:** Modbus PLC 5-digit (`00001`, `10001`, `30001`, `40001`) and 6-digit (`000001`..`400001`) address formats alongside 0-based raw protocol addressing.
- **Custom Tag Persistence & File Dialogs:** Per-register custom Tag Name and Description editing with native OS file dialogs (`Load Config...`, `Save As...`) to save and reload workspace profiles.
- **Low-Resolution IPC Ergonomics:** Optimized window layout for 1366x768 industrial displays; application opens maximized by default with scrollable toolbars and no overflowing buttons.
- **Polling Button Reactive State:** Start/Stop Polling button now automatically toggles text ("Start Polling" vs "Stop Polling") and accent color (Blue `#0284C7` vs Red `#DC2626`).
- **Modbus Slave Example:** Integrated example slave in `examples/Modbus-Slave-Example` with dynamic float, double, integer, and string register feeds.
- **Comprehensive Unit Testing:** Expanded test suite to 38 unit tests verifying Single, Double, Int32, Int64, and ASCII roundtrips across Big-Endian, Little-Endian, Word-Swap, and Byte-Swap orders.

## [1.0.0] - 2026-10-10

### Added
- Independent `ModScope.ModbusMaster` NuGet library supporting Modbus RTU and Modbus TCP.
- Complete Modbus function code suite: FC01, FC02, FC03, FC04, FC05, FC06, FC15, FC16, FC22, FC23.
- Fast table-driven CRC-16 computation and validation.
- Industrial data format decoders: Hex, Binary, Int16, UInt16, Int32, UInt32, IEEE Float32, ASCII string with BigEndian, LittleEndian, ByteSwap, and WordSwap orders.
- Microsecond-accurate Raw Frame TX/RX Sniffer and protocol parser.
- Built-in In-Memory Modbus TCP Simulator server (port 5020).
- Automated Device Bus Discovery scanner for Slave IDs.
- Automated Commissioning Sequence test runner with read-back verification.
- Tamper-evident Audit Logging service and export to CSV/JSON/Markdown reports.
- Ergonomic dark industrial Avalonia desktop application interface.
