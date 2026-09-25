# Releasing Barbatos.Pallas

A release is two packages on nuget.org, Barbatos.Pallas.Engine and Barbatos.Pallas.DependencyInjection, and the
installer of the app, all of one version and all from one commit. Publishing is the maintainer's: it is a GitHub
Release, and nothing else publishes. The workflow `.github/workflows/barbatos-pallas-cd-nuget.yml` then signs, tests,
packs and pushes the packages; the installer is built and signed on the maintainer's machine (packaging/README.md).

## Once, before the first release

1. **The repository** is `Barbatos-Labs/Barbatos.Pallas` on GitHub. CI (`barbatos-pallas-ci.yml`) runs on every push
   to `main`; a release is refused on a commit it has not passed.
2. **The `production` environment** (Settings → Environments). The release job runs in it, so its protection rules -
   a required reviewer, say - hold every release until they are met.
3. **The `STRONG_NAME_KEY` secret**, in that environment: the base64 of `barbatos.snk`, the key Barbatos.i18n and
   Barbatos.Wpf are signed with, so that all three carry the public key token `0aed45c810bf67e6`. On the machine that
   holds the key:

   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes('barbatos.snk')) | Set-Clipboard
   ```

   The key is never committed: `*.snk` is gitignored, and the workflow deletes the file it writes once the packages
   are built. `src/core/Directory.Build.props` signs every core library whenever `src/barbatos.snk` exists, so a
   build with the key on a developer's machine signs too; nothing else needs it.
4. **A trusted publishing policy on nuget.org** (the account menu → Trusted Publishing), so that no API key is ever
   stored: repository owner `Barbatos-Labs`, repository `Barbatos.Pallas`, workflow file `barbatos-pallas-cd-nuget.yml`,
   environment `production`. The workflow logs in as `phamhung`, the nuget.org account that owns the policy and the
   packages; if they belong to another account, the `user` of the login step changes with them.

## Every release

1. **CI is green on the commit** on `main` that becomes the release.
2. **The version.** `VersionPrefix` in `Directory.Build.props` is the packages' version, by semantic versioning: a
   patch for a fix, a minor version for anything added to the public API, a major version for an incompatible
   change - a line of `PublicAPI.Shipped.txt` removed or changed among them. When the app ships with the
   release, its version is `<Version>` in `src/app/Barbatos.Pallas.Wpf/Barbatos.Pallas.Wpf.csproj` and
   `Identity.Version` in `packaging/Barbatos.Pallas.json`, together (`PackagingProfileTests`); at 1.0.0 it is the
   packages' (decision of 24 Sep 2026).
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
      net10.0, and checks that every assembly the packages hold carries the Barbatos token;
   4. pushes the packages and their symbols to nuget.org through trusted publishing, and attaches them to the release.
6. **The installer**, when the app ships: built from the same commit with `barbatos-pack release`
   (packaging/README.md), then attached to the release:

   ```bash
   gh release upload v1.0.0 artifacts/installer/barbatos-pallas-v1.0.0-setup.exe
   ```

## After the release

- **The package-validation baseline.** Set `PackageValidationBaselineVersion` in `src/core/Directory.Build.props` to
  the version just published, so that the next pack compares itself with it and fails on an incompatible change
  (docs/ARCHITECTURE.md §7). It cannot be set before the version exists on nuget.org.
- nuget.org validates and indexes a new version before it can be installed; that takes minutes.

## When a step fails

- **Before the push**, nothing is published: fix the cause, then run the failed job again, or delete the release and
  its tag and publish a new one.
- **After a push**, run the failed job again: `--skip-duplicate` passes over what is already on nuget.org.
- **A published version is never replaced.** A wrong one is unlisted on nuget.org and followed by a patch release.
