# Pre-Release Open-Source Compliance & Review Checklist

This checklist must be executed and approved before tagging and distributing any public release or publishing packages to NuGet.org.

---

## 1. Automated Dependency & Vulnerability Scan

- [ ] Execute vulnerability check across all packages:
  ```bash
  dotnet list package --vulnerable --include-transitive
  ```
  *(Zero high/critical security advisories allowed).*
- [ ] Execute deprecation check:
  ```bash
  dotnet list package --deprecated
  ```
- [ ] Run compliance audit script (or verify `DEPENDENCIES.md` against `dotnet list package --include-transitive`).

---

## 2. License Compatibility & Copyleft Verification

- [ ] Verify that **no GPL, AGPL, or restrictive copyleft** libraries are present in any shipped component.
- [ ] Confirm that `ModScope.ModbusMaster` depends only on MIT-licensed dependencies (`System.IO.Ports`).
- [ ] Confirm all font assets (Inter Font) are governed by the SIL Open Font License 1.1 and bundled with original notices.
- [ ] Confirm that `CsvHelper` is licensed under MS-PL / Apache-2.0 and compatible with commercial binary distribution with attribution.

---

## 3. Package & Binary Notice Distribution

- [ ] Verify that `LICENSE` is packaged inside `.nupkg`:
  - `PackageLicenseExpression` or `PackageLicenseFile` properly declared in project file.
- [ ] Verify that `README.md` is bundled as `PackageReadmeFile` in `ModScope.ModbusMaster.csproj`.
- [ ] Verify that `THIRD-PARTY-NOTICES.md` is included in all binary ZIP / installer distributions.

---

## 4. Trademarks, Branding & Proprietary Art

- [ ] Confirm application icon and UI artwork are original vector/XAML shapes.
- [ ] Confirm no third-party vendor trademarks (e.g. Modicon, Schneider Electric, Siemens, Rockwell) are used in product titles or iconography.
- [ ] Modbus references are solely functional and nominative.

---

## 5. Contributor Sign-Off & DCO Verification

- [ ] Verify that all commits merged into the release branch include `Signed-off-by` certifying the Developer Certificate of Origin (DCO 1.1).
- [ ] Confirm no unresolved licensing issues or unreviewed third-party contributions exist.
