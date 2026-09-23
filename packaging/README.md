# Packaging the Windows app

The installer is built by [Barbatos.PackagingEngine](https://github.com/Barbatos-Labs/Barbatos.PackagingEngine)
(`barbatos-pack`), the tool every Barbatos desktop app ships through, from the one profile in this folder:
validate → build → sign → verify → package, in that order, so nothing unsigned is ever packaged.

| | |
|---|---|
| `Barbatos.Pallas.json` | The profile: the app's identity, how it is built, how it is signed, the Inno Setup installer. JSONC - the comments are documentation of the values |
| `identity.lock.json` | The ledger that pins `Identity.AppGuid`. **Committed**, written by `validate`. A different AppGuid fails validation, because it names the folder every user's session lives in |
| `languages/Vietnamese.isl` | Inno Setup's Vietnamese wizard text, which Inno does not bundle. **Committed** - unofficial, by memecoder, from [issrc](https://github.com/jrsoftware/issrc/blob/main/Files/Languages/Unofficial/Vietnamese.isl), written for Inno 6.5; under 6.4 a few download messages fall back to English |
| `certificates/` | The code-signing leaf of the Barbatos Labs chain and its password. **Gitignored, never committed** |

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
itself signed. It needs Inno Setup 6 and the Windows SDK's `signtool`; `barbatos-pack doctor` says whether they were
found.

## Signing

The app is signed with the same chain as every Barbatos app - one root for the organisation, not one per repository.
`certificates/` holds its leaf, `barbatos-codesign.pfx` (which carries the chain), `barbatos-codesign.password.txt`,
and the two public certificates. The password is read from `BARBATOS_CERT_PASSWORD` when that is set - as it would be
in CI - and from the `.password.txt` otherwise; it is never written into the profile. A machine that has not trusted
the Barbatos Labs root fails the verify step; that is the machine's trust, not a bad signature
(Barbatos.PackagingEngine's `install-certificate.ps1`).

## Versions

The app's version is its own and numeric - `0.1.0` - because the engine refuses a prerelease label for an application
(`BPE1011`). The packages keep the repository's `0.1.0-preview.1`. The profile's `Identity.Version` and the
`.csproj`'s `<Version>` say the same number, and `PackagingProfileTests` in Barbatos.Pallas.Wpf.Tests fails when they
do not, or when the AppId, the uninstall key or the product name drift from the profile.
