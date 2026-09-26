# Packaging the Windows app

The installer is built by [Barbatos.PackagingEngine](https://github.com/Barbatos-Labs/Barbatos.PackagingEngine)
(`barbatos-pack`), the tool every Barbatos desktop app ships through, from the one profile in this folder:
validate → build → sign → verify → package, in that order, so nothing unsigned is ever packaged.

| | |
|---|---|
| `Barbatos.Pallas.json` | The profile: the app's identity, how it is built, how it is signed, the Inno Setup installer. JSONC - the comments are documentation of the values |
| `identity.lock.json` | The ledger that pins `Identity.AppGuid`. **Committed**, written by `validate`. A different AppGuid fails validation, because it names the folder every user's session lives in |
| `languages/Vietnamese.isl` | Inno Setup's Vietnamese wizard text, which Inno does not bundle. **Committed** - unofficial, by memecoder, from [issrc](https://github.com/jrsoftware/issrc/blob/main/Files/Languages/Unofficial/Vietnamese.isl), written for Inno 6.5 and later; Inno Setup 7.1.0 compiles it without a warning (26 Sep 2026) |
| `certificates/` | The code-signing leaf of the Barbatos Labs chain and its password - **gitignored, never committed** - and the two public certificates, which are committed |

## Cutting an installer

From the repository root, with a checkout of Barbatos.PackagingEngine beside this one (or `barbatos-pack` installed
as a tool):

```bash
dotnet run --project ../Barbatos.PackagingEngine/src/Barbatos.PackagingEngine.Cli -f net10.0 -- validate --profile packaging/Barbatos.Pallas.json --strict
```

```bash
dotnet run --project ../Barbatos.PackagingEngine/src/Barbatos.PackagingEngine.Cli -f net10.0 -- release --profile packaging/Barbatos.Pallas.json --strict
```

It publishes the app self-contained for win-x64 into `artifacts/installer/<version>/`, signs the Barbatos binaries
with a timestamp, verifies every signature, and writes `artifacts/installer/barbatos-pallas-v<version>-setup.exe`,
itself signed. It needs Inno Setup - 7.1.0 on the maintainer's machine; barbatos-pack 1.1.0 finds 7, 6 or 5 - and the
Windows SDK's `signtool`; `barbatos-pack doctor` says whether they were
found.

## Signing

The app is signed with the same chain as every Barbatos app - one root for the organisation, not one per repository.
`certificates/` holds its leaf, `barbatos-codesign.pfx` (which carries the chain), and `barbatos-codesign.password.txt`,
both gitignored, and the two public certificates, `barbatos-ca.cer` and `barbatos-codesign.cer`, which are committed:
they carry no key, and the release workflow trusts the root on its runner. The password is read from
`BARBATOS_CERT_PASSWORD` when that is set - as it is in CI - and from the `.password.txt` otherwise; it is never
written into the profile. A machine that has not trusted the Barbatos Labs root fails the verify step; that is the
machine's trust, not a bad signature (Barbatos.PackagingEngine's `install-certificate.ps1`).

## Releasing from CI

Pushing a tag `app-v<version>` runs `.github/workflows/barbatos-pallas-release-app.yml`, which builds this same
profile through the same pipeline on a Windows runner and publishes a GitHub Release with the installer
(docs/RELEASING.md). Barbatos.PackagingEngine is private and this repository is public, so the workflow installs
barbatos-pack from the private feed itself, pinned, rather than calling the engine's reusable workflow.

## Versions

The app's version is its own and numeric - `1.0.0` - because the engine refuses a prerelease label for an application
(`BPE1011`). The packages carry the repository's, and the first release is 1.0.0 for both. The profile's
`Identity.Version` and the `.csproj`'s `<Version>` say the same number, and `PackagingProfileTests` in
Barbatos.Pallas.Wpf.Tests fails when they do not, or when the AppId, the uninstall key or the product name drift from
the profile.
