# Contributing to ModScope

Thank you for your interest in contributing to **ModScope**! We welcome bug reports, feature requests, protocol extensions, and pull requests.

---

## Developer Certificate of Origin (DCO)

To ensure that all contributions are lawfully licensed under the project's [MIT License](LICENSE), ModScope enforces the **Developer Certificate of Origin (DCO)**, Version 1.1.

By making a contribution to this project, you certify that:

```
Developer Certificate of Origin
Version 1.1

Copyright (C) 2004, 2006 The Linux Foundation and its contributors.

Everyone is permitted to copy and distribute verbatim copies of this
license document, but changing it is not allowed.

By making a contribution to this project, I certify that:

(a) The contribution was created in whole or in part by me and I
    have the right to submit it under the open source license
    indicated in the file; or

(b) The contribution is based upon previous work that, to the best
    of my knowledge, is covered under an appropriate open source
    license and I have the right under that license to submit that
    work with modifications, whether created in whole or in part
    by me, under the same open source license (unless I am
    permitted to submit under a different license), as indicated
    in the file; or

(c) The contribution was provided directly to me by some other
    person who certified (a), (b) or (c) and I have not modified
    it.

(d) I understand and agree that this project and the contribution
    are public and that a record of the contribution (including all
    personal information I submit with it, including my sign-off) is
    maintained indefinitely and may be redistributed consistent with
    this project or the open source license(s) involved.
```

### How to Sign Your Work

Please certify your commit by adding a `Signed-off-by` trailer line to each commit message:

```bash
git commit -s -m "feat(modbus): add FC23 support with validation"
```

---

## Original Implementation & Third-Party Code Rules

1. **No Proprietary Code:** Never commit proprietary, decompiled, or reverse-engineered code from closed-source automation tools (e.g. Modscan, Modpoll, Kepware, Schneider/Siemens software).
2. **Third-Party Assets:** If adding a third-party dependency, icon, or font:
   - Check that its license is permissive (MIT, Apache-2.0, BSD-2/3-Clause, SIL OFL).
   - Copyleft licenses (GPL, AGPL) are **strictly prohibited**.
   - Update `DEPENDENCIES.md` and `THIRD-PARTY-NOTICES.md` in the same pull request.
3. **No Trademark Infringement:** Do not introduce proprietary corporate trademarks or logos into UI assets.
4. **Architecture Boundaries:** Keep `ModScope.ModbusMaster` completely independent of Avalonia, UI packages, or OS-specific APIs.
5. **Quality Gate:** All PRs must pass `dotnet test` and include corresponding test coverage for any new function code or data converter.
