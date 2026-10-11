# Dependencies & License Audit Matrix

This document provides a comprehensive inventory of all direct and transitive dependencies utilized in the **ModScope** solution, auditing their copyright holders, license types, source repositories, and compatibility with the **MIT License**.

---

## 1. Direct Production Dependencies

| Package / Asset | Version | License | Copyright Holder / Author | Source Repository | Distribution Rights & Compatibility |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **System.IO.Ports** | 8.0.0 | **MIT** | Microsoft Corporation | [dotnet/runtime](https://github.com/dotnet/runtime) | Permissive. Compatible with MIT. |
| **Avalonia** | 11.1.3 | **MIT** | AvaloniaUI OÜ & Contributors | [AvaloniaUI/Avalonia](https://github.com/AvaloniaUI/Avalonia) | Permissive. Compatible with MIT. |
| **Avalonia.Desktop** | 11.1.3 | **MIT** | AvaloniaUI OÜ & Contributors | [AvaloniaUI/Avalonia](https://github.com/AvaloniaUI/Avalonia) | Permissive. Compatible with MIT. |
| **Avalonia.Themes.Fluent** | 11.1.3 | **MIT** | AvaloniaUI OÜ & Contributors | [AvaloniaUI/Avalonia](https://github.com/AvaloniaUI/Avalonia) | Permissive. Compatible with MIT. |
| **Avalonia.Controls.DataGrid** | 11.1.3 | **MIT** | AvaloniaUI OÜ & Contributors | [AvaloniaUI/Avalonia](https://github.com/AvaloniaUI/Avalonia) | Permissive. Compatible with MIT. |
| **Avalonia.Diagnostics** | 11.1.3 | **MIT** | AvaloniaUI OÜ & Contributors | [AvaloniaUI/Avalonia](https://github.com/AvaloniaUI/Avalonia) | Permissive. Debug diagnostic tooling. |
| **Avalonia.Fonts.Inter** | 11.1.3 | **SIL OFL 1.1** (Font) / **MIT** (Wrapper) | Rasmus Andersson & AvaloniaUI | [rsms/inter](https://github.com/rsms/inter) | Permissive font redistribution under SIL OFL 1.1. Bundled and redistributable. |
| **CommunityToolkit.Mvvm** | 8.3.2 | **MIT** | Microsoft Corporation & .NET Foundation | [CommunityToolkit/dotnet](https://github.com/CommunityToolkit/dotnet) | Permissive. Source generators and MVVM contracts. |
| **CsvHelper** | 33.0.1 | **MS-PL** / **Apache-2.0** | Josh Close | [JoshClose/CsvHelper](https://github.com/JoshClose/CsvHelper) | Permissive open source. Permitted in MIT commercial/binary distributions with attribution. |

---

## 2. Standalone Package: `ModScope.ModbusMaster`

The standalone class library `ModScope.ModbusMaster` is purposefully isolated from UI and application dependencies:

- **Direct Dependency:** `System.IO.Ports` (v8.0.0, MIT License, Microsoft Corporation).
- **Transitive Dependencies:** `runtime.linux-x64.runtime.native.System.IO.Ports`, `runtime.linux-arm64.runtime.native.System.IO.Ports`, `runtime.osx-x64.runtime.native.System.IO.Ports`.
- **Verdict:** 100% MIT-compatible. Safe for public distribution on NuGet.org under MIT.

---

## 3. Direct Test & Development Dependencies

| Package | Version | License | Copyright Holder / Author | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| **xunit** | 2.5.3 | **Apache-2.0** | .NET Foundation and Contributors | Unit & Integration Testing (Not shipped with binary release). |
| **xunit.runner.visualstudio** | 2.5.3 | **MIT** / **Apache-2.0** | .NET Foundation and Contributors | Test runner integration. |
| **Microsoft.NET.Test.Sdk** | 17.8.0 | **MIT** | Microsoft Corporation | Build-time MSBuild test runner. |
| **coverlet.collector** | 6.0.0 | **MIT** | Toni Solarin-Sodara | Code coverage collector. |

---

## 4. Key Transitive Native Binaries (Avalonia Graphics & Windowing)

| Native Binary / Package | Version | License | Copyright Holder | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **SkiaSharp** / `libSkiaSharp` | 2.88.8 | **MIT** | Google LLC & Microsoft Corporation | 2D Graphics engine for Avalonia. Permissive. |
| **HarfBuzzSharp** / `libHarfBuzzSharp` | 7.3.0.2 | **MIT** | HarfBuzz Contributors & Microsoft Corp | Text shaping engine. Permissive. |
| **Tmds.DBus.Protocol** | 0.16.0 | **MIT** | Tom Deseyn | Linux D-Bus communication. Permissive. |

---

## 5. Security Advisory & Dependency Risk Disclosure

During the automated pre-release dependency audit, the following transitive dependency was evaluated:

| Package | Transitive Via | Advisory ID | Severity | Assessment & Mitigation |
| :--- | :--- | :--- | :--- | :--- |
| `Tmds.DBus.Protocol` (v0.16.0) | `Avalonia.FreeDesktop` -> `Avalonia.Desktop` | [GHSA-xrw6-gwf8-vvr9](https://github.com/advisories/GHSA-xrw6-gwf8-vvr9) | High | **Scope:** Affects Linux D-Bus desktop message protocol decoding under malicious input. ModScope does not expose D-Bus endpoints to untrusted remote networks; communication is strictly local desktop UI windowing. ModScope core libraries (`ModScope.ModbusMaster`, `ModScope.Core`, `ModScope.Diagnostics`, `ModScope.Infrastructure`) do **not** depend on D-Bus. This transitive dependency is tracked upstream in the Avalonia UI repository for the next patch release. |

---

## 6. Compatibility & Legal Audit Summary

1. **No GPL / AGPL Incompatibilities:** None of the direct or transitive dependencies impose copyleft constraints (such as GPL-2.0, GPL-3.0, or AGPL-3.0).
2. **Attribution Requirements:** All dependencies requiring attribution (MIT, Apache-2.0, SIL Open Font License) are fully credited with original notices preserved in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
3. **Patent Retaliation / Grants:** Standard Apache-2.0 patent grants apply to CsvHelper and xUnit components.
4. **Trademark Notice:** "ModScope" is a trademark of the ModScope Project. "Modbus" is a registered trademark of Modicon / Schneider Electric / The Modbus Organization. References to Modbus are descriptive protocol indicators under nominative fair use.
