# Copyright & Authorship Documentation

## Project Ownership

**ModScope** is an open-source Modbus Diagnostic Studio and protocol engineering suite.

- **Primary Maintainer & Author:** Bharat and the ModScope Project Contributors
- **Project Repository:** [https://github.com/modscope/modscope](https://github.com/modscope/modscope)
- **Copyright Statement:**  
  `Copyright (c) 2024-2026 Bharat and ModScope Project Contributors. All rights reserved.`

---

## License Notice

ModScope is licensed under the **MIT License**. For complete terms, see [LICENSE](LICENSE).

```
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

---

## Authorship and Original Works

1. **Protocol Implementation:**
   - The Modbus RTU, Modbus TCP, MBAP framing, CRC-16 table implementation, and transaction management in `ModScope.ModbusMaster` were authored from public Modbus specifications (*Modbus Application Protocol Specification V1.1b3* and *Modbus Messaging on TCP/IP Implementation Guide V1.0b*).
   - No proprietary or reverse-engineered code from proprietary SCADA, PLC, or diagnostic software (such as Modscan, Modpoll, CAS Modbus Scanner, Kepware, or Schneider Electric software) is incorporated.

2. **User Interface Artwork and Theme:**
   - The industrial dark color scheme, grid layouts, telemetry widgets, and control forms are original works created using standard Avalonia UI layout primitives.
   - All keyboard shortcuts use standard cross-platform platform defaults (e.g., standard OS controls).

3. **Third-Party Copyrights:**
   - ModScope incorporates and links against external third-party open-source libraries.
   - Ownership of all third-party libraries, fonts, and frameworks remains with their respective copyright holders.
   - Full credits, copyright notices, and license texts are cataloged in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and [DEPENDENCIES.md](DEPENDENCIES.md).
