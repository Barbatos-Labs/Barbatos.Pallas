# Releasing Barbatos.Pallas

Two things are released, each by a workflow of its own and from a tag of its own (maintainer, 25 Sep 2026):

| What | Tag | Workflow | Published to |
|---|---|---|---|
| The packages, Barbatos.Pallas.Engine and Barbatos.Pallas.DependencyInjection | `v<version>` - a GitHub Release the maintainer publishes | `.github/workflows/barbatos-pallas-cd-nuget.yml` | nuget.org, and the release |
| The app, its signed Windows installer | `app-v<version>` - a tag the maintainer pushes | `.github/workflows/barbatos-pallas-release-app.yml` | a GitHub Release the workflow creates |

Their versions are their own: `VersionPrefix` in `Directory.Build.props` for the packages, `<Version>` in the Wpf
csproj and `Identity.Version` in `packaging/Barbatos.Pallas.json` for the app. At 1.0.0 they are one (decision of
24 Sep 2026). Publishing is the maintainer's: nothing but those two acts publishes, and run by hand from the Actions
tab either workflow is a rehearsal that publishes nothing.

## Once, before the first release

1. **The repository** is `Barbatos-Labs/Barbatos.Pallas` on GitHub. CI (`barbatos-pallas-ci.yml`) runs on every push
   to `main`; both workflows refuse a commit it has not passed.
2. **The `production` environment** (Settings → Environments). The job that signs runs in it in both workflows, so its
   protection rules - a required reviewer, say - hold every release until they are met.
3. **The secrets**, in that environment or in the repository:

   | Secret | What | Used by |
   |---|---|---|
   | `STRONG_NAME_KEY` | The base64 of `barbatos.snk`, the key of Barbatos.Pallas and of nothing else, public key token `1c94c30b213a8345`. From 1.0.0 on it is part of the identity of every assembly: changing it later is an incompatible change | both |
   | `BARBATOS_PACKAGES_TOKEN` | A classic PAT with `read:packages`, for the private feed barbatos-pack is installed from | the app |
   | `SIGNING_CERTIFICATE_PFX` | The base64 of `packaging/certificates/barbatos-codesign.pfx`, the leaf of the Barbatos Labs chain | the app |
   | `SIGNING_CERTIFICATE_PASSWORD` | Its password | the app |

   `build/Copy-SecretToClipboard.ps1` puts the base64 of a key file on the clipboard, never on the screen, and keeps it
   out of Windows' clipboard history and cloud clipboard. It says what it took, to check before pasting: for a `.snk`
   the public key token - `1c94c30b213a8345` is Barbatos.Pallas's - and for a `.pfx` the certificate, opened with the
   `.password.txt` beside it. Paste the value into the secret, then empty the clipboard:

   ```bash
   powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Path src/barbatos.snk
   ```

   ```bash
   powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Path packaging/certificates/barbatos-codesign.pfx
   ```

   ```bash
   powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Clear
   ```

   `SIGNING_CERTIFICATE_PASSWORD` is the content of `packaging/certificates/barbatos-codesign.password.txt`, typed or
   copied by hand.

   No key is ever committed: `*.snk` and the `.pfx` are gitignored, and each workflow deletes what it writes once it
   has built. `src/core/Directory.Build.props` signs every core library whenever `src/barbatos.snk` exists, so a build
   on the maintainer's machine, which keeps the key there, signs too. The two public certificates in
   `packaging/certificates/` are committed: the app's workflow trusts the root on its runner, or signtool could not
   verify what it signed.
4. **A trusted publishing policy on nuget.org** (the account menu → Trusted Publishing), so that no API key is ever
   stored: repository owner `Barbatos-Labs`, repository `Barbatos.Pallas`, workflow file `barbatos-pallas-cd-nuget.yml`,
   environment `production`. The workflow logs in as `phamhung`, the nuget.org account that owns the policy and the
   packages; if they belong to another account, the `user` of the login step changes with them.

## Releasing the packages

1. **CI is green on the commit** on `main` that becomes the release.
2. **The version.** `VersionPrefix` in `Directory.Build.props`, by semantic versioning: a patch for a fix, a minor
   version for anything added to the public API, a major version for an incompatible change - a line of
   `PublicAPI.Shipped.txt` removed or changed among them.
3. **The public API ships.** `./build/Move-PublicApiToShipped.ps1` moves every entry of each core library's
   `PublicAPI.Unshipped.txt` into its `PublicAPI.Shipped.txt`, where removing or changing one is an incompatible
   change from then on. The workflow refuses a release while an Unshipped file still declares an entry. Commit the
   version and the API files together.
4. **A rehearsal.** Actions → barbatos-pallas-cd-nuget → Run workflow, on `main`: everything a release does but the
   push, with the real key. Its artifact `nuget-packages` holds the signed packages it would have published.
5. **The GitHub Release.** Tag `v<version>` - `v1.0.0` - on that commit, and publish. The workflow:
   1. refuses a tag that is not `v` and the packages' version, a commit CI has not passed, and an API not shipped;
   2. writes the key, builds in Release, and runs the 40 test assemblies on the signed build;
   3. packs, installs both packages into programs outside the repository, calculates with them on net8.0, net9.0 and
      net10.0, and checks that every assembly the packages hold carries the token of the key;
   4. pushes the packages and their symbols to nuget.org through trusted publishing, and attaches them to the release.

## Releasing the app

1. **CI is green on the commit**, and the app's version is `<Version>` in
   `src/app/Barbatos.Pallas.Wpf/Barbatos.Pallas.Wpf.csproj` and `Identity.Version` in `packaging/Barbatos.Pallas.json`,
   together (`PackagingProfileTests`).
2. **A rehearsal.** Actions → barbatos-pallas-release-app → Run workflow, on `main`: the installer built and signed,
   as the artifact `barbatos-pallas-installer`, and no release.
3. **The tag.** `git tag app-v1.0.0` on that commit, and push it. The workflow:
   1. refuses a tag that is not `app-v` and the app's version, and a commit CI has not passed;
   2. installs Inno Setup, pinned to the maintainer's 6.4.3 and checked for its publisher's signature, and
      barbatos-pack, pinned, from the private feed;
   3. places the signing certificate and the strong-name key, trusts the root on the runner, and runs
      `barbatos-pack release --profile packaging/Barbatos.Pallas.json --strict`: validate, build, sign, verify,
      package;
   4. checks the installer - its version is the tag's, it and every Barbatos binary it carries are validly signed and
      timestamped, and the engine carries the token of the key - and deletes the keys;
   5. creates the GitHub Release `app-v<version>`, "Barbatos Pallas <version>", with the installer and its SHA-256.

Barbatos.PackagingEngine, whose tool barbatos-pack is, is a private repository; this one is public, and GitHub lets a
private repository's reusable workflows be called "only from private repositories". So the app's workflow installs
the tool itself rather than calling the engine's workflow, as Barbatos.RMCP does. A public repository's run logs are
public too: no step prints a secret, and nothing but the installer is uploaded.

The installer can still be built on the maintainer's machine, from the checkout of the engine beside this one
(packaging/README.md) - the same profile and the same pipeline.

## After a release

- **The package-validation baseline.** Set `PackageValidationBaselineVersion` in `src/core/Directory.Build.props` to
  the packages' version just published, so that the next pack compares itself with it and fails on an incompatible
  change (docs/ARCHITECTURE.md §7). It cannot be set before the version exists on nuget.org.
- nuget.org validates and indexes a new version before it can be installed; that takes minutes.

## When a step fails

- **Before anything is published**, fix the cause, then run the failed job again, or delete the release or the tag and
  make a new one.
- **After a push to nuget.org**, run the failed job again: `--skip-duplicate` passes over what is already there.
- **A published version is never replaced.** A wrong package is unlisted on nuget.org and followed by a patch release;
  a wrong installer is followed by a new version of the app.
