# Releasing Barbatos.Pallas

The packages and the app are released apart, each by a workflow of its own (maintainer, 25 Sep 2026), kept as simple
as the release workflows of Barbatos.i18n and Barbatos.Wpf:

| What | How | Workflow |
|---|---|---|
| The packages, Barbatos.Pallas.Engine and Barbatos.Pallas.DependencyInjection | Publish a GitHub Release tagged `v<version>` | `barbatos-pallas-cd-nuget.yml`: builds signed, runs the tests, packs, pushes to nuget.org |
| The app, its signed Windows installer | Publish a GitHub Release tagged `app-v<version>`, or push the tag | `barbatos-pallas-release-app.yml`: builds the installer with barbatos-pack and attaches it to that release, creating it when only the tag was pushed. The package workflow passes over an `app-` tag |

Their versions are their own: `VersionPrefix` in `Directory.Build.props` for the packages, `<Version>` in the Wpf
csproj and `Identity.Version` in `packaging/Barbatos.Pallas.json` for the app (`PackagingProfileTests` keeps those two
equal). At 1.0.0 they are one.

## Once

1. **The `production` environment** (Settings → Environments), in which both workflows run.
2. **The secrets**, in that environment:

   | Secret | What | Used by |
   |---|---|---|
   | `STRONG_NAME_KEY` | The base64 of `src/barbatos.snk`, the key of Barbatos.Pallas, public key token `1c94c30b213a8345` | both |
   | `BARBATOS_PACKAGES_TOKEN` | A classic PAT with `read:packages`, for the private feed barbatos-pack is installed from | the app |
   | `SIGNING_CERTIFICATE_PFX` | The base64 of `packaging/certificates/barbatos-codesign.pfx` | the app |
   | `SIGNING_CERTIFICATE_PASSWORD` | The content of `packaging/certificates/barbatos-codesign.password.txt` | the app |

   `build/Copy-SecretToClipboard.ps1` puts the base64 of a key file on the clipboard, never on the screen, and says
   which key it is:

   ```bash
   powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Path src/barbatos.snk
   ```

   ```bash
   powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Path packaging/certificates/barbatos-codesign.pfx
   ```

   ```bash
   powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Clear
   ```

3. **A trusted publishing policy on nuget.org**: repository owner `Barbatos-Labs`, repository `Barbatos.Pallas`,
   workflow file `barbatos-pallas-cd-nuget.yml`, environment `production`, for the account `phamhung`.

No key is ever committed: `*.snk` and the `.pfx` are gitignored. The two public certificates in
`packaging/certificates/` are committed, for the app's workflow to trust the Barbatos Labs root on its runner.

## Before a release

On the commit to release, with CI green:

1. **The public API ships**, for the packages: `./build/Move-PublicApiToShipped.ps1` moves every entry of
   `PublicAPI.Unshipped.txt` into `PublicAPI.Shipped.txt`. Commit that with the version.
2. **The benchmark and mutation gates**, on the maintainer's machine (CI does not run them):

   ```bash
   dotnet run --project benchmarks/Barbatos.Pallas.Benchmarks -c Release -- --gate
   ```

   and `dotnet stryker --skip-version-check` from each of `tests/Barbatos.Pallas.<Package>.Tests` for Numerics,
   Expressions, Engine, LinearAlgebra, Statistics, Solvers, Spreadsheet, Graphing, DependencyInjection and
   Presentation: every benchmark within its target, every score at 90% or more.

## Releasing

- **The packages:** publish a GitHub Release tagged `v1.0.0` on that commit.
- **The app:** publish a GitHub Release tagged `app-v1.0.0` on that commit, or push that tag.

## After

- Set `PackageValidationBaselineVersion` in `src/core/Directory.Build.props` to the packages' version just published,
  so that the next pack fails on an incompatible change.
- A version on nuget.org is never replaced: a wrong one is unlisted and followed by a patch release. If a run fails
  before the push, fix the cause and run it again.
