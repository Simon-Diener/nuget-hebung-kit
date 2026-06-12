# NuGet Hebung — Risks, Challenges, and Best Practices

Reference knowledge base for the `nuget-hebung` skill. Read it once before
consolidating findings (Phase 3) and planning (Phase 5). Sources: Microsoft
Learn (NuGet / .NET docs), the .NET Blog, NuGet/Home. Verify exact MSBuild
property names against MS Learn before committing config.

## Core risks

- **Version unification / diamond conflicts.** Only one version of a package is
  used per project. *Direct-dependency-wins* lets a top-level reference override
  transitive versions — but can silently **downgrade** a package, surfacing as
  **NU1605**. Mutually-exclusive cousin constraints fail resolution; fix by
  adding an explicit top-level reference.
  <https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution>
- **Compile-time vs runtime breaks.** Build success ≠ safe. Binding redirects
  (.NET Framework), assembly-load conflicts, exact-version mismatches, and
  behavioral changes only fail at runtime → automated unit tests are mandatory.
- **packages.config vs PackageReference.** packages.config (legacy, often older
  .NET Framework + web projects) is the highest-risk, lowest-automation cohort.
  Migrate to PackageReference where supported; ASP.NET Framework web projects may
  have to stay. Watch for stale binding redirects after migration.
  <https://learn.microsoft.com/en-us/nuget/consume-packages/migrate-packages-config-to-package-reference>
- **TFM bumps.** A newer package may drop your target framework, forcing a TFM
  bump that cascades to consumers. Plan as its own workstream; use
  `<TargetFrameworks>` multi-targeting for phased migration.
- **Analyzers / source generators / native assets** shipped as packages run at
  build time or load per-RID — test the build *output*, not just compilation.

## Central Package Management (the main lever)

`Directory.Packages.props` with `ManagePackageVersionsCentrally=true`; projects
reference by name, versions defined centrally as `<PackageVersion>`.

- Turns hundreds of per-project edits into one file; prevents version drift.
- `VersionOverride` — per-project escape hatch for staged rollouts.
- `GlobalPackageReference` — apply a package (e.g. analyzers) to every project.
- **Transitive pinning** (`CentralPackageTransitivePinningEnabled=true`) — raise
  a transitive version (e.g. a CVE fix) without adding references everywhere.
- Friction: VS Package Manager UI is disabled under CPM; expect NU1008 / NU1507
  during onboarding.
  <https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management>
- **CPM edits are a single-file bottleneck** — never edit `Directory.Packages.props`
  from two parallel execution subagents at once.

## Security vs feature upgrades

`dotnet list package --vulnerable --include-transitive` and NuGet Audit
(`NuGetAuditMode=all`) flag CVEs. Use `dotnet nuget why` to trace a vulnerable
transitive. Security fixes = smallest version clearing the CVE (ideally via
transitive pinning), fast-tracked; never bundled with risky feature bumps.
<https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages>

## Lock files

`RestorePackagesWithLockFile=true` → committed `packages.lock.json`; CI restores
with `--locked-mode` / `RestoreLockedMode=true` for reproducible, fail-fast,
rollback-friendly restores. Recommended before bulk work.
<https://devblogs.microsoft.com/dotnet/enable-repeatable-package-restores-using-a-lock-file/>

## NuGet feeds / nuget.config

Large/enterprise solutions usually pull from a **private feed** (Azure Artifacts,
an internal Noxum feed, etc.) in addition to nuget.org. If `nuget.config` is
missing or omits a needed feed, version queries ("highest available") and restore
will be wrong or fail. **In Phase 0 the orchestrator must confirm the feed config
before any investigation.**
<https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file>

## Ordering: parallelizable vs sequential

**Sequential (first):** CPM / `Directory.Packages.props` edits; shared
foundational packages (logging, DI, serialization, HTTP, ORM); TFM bumps on
foundational libs; binding-redirect-affecting migrations.

**Parallel (after):** leaf projects (tests, top-level apps); disjoint dependency
clusters — ideal for parallel execution subagents / worktrees.

## Tooling

- **Inventory/graph:** `dotnet list package --include-transitive/--outdated/
  --vulnerable`, `dotnet nuget why`, `dotnet package search`, MSBuild
  project-reference graph.
- **Breaking-change detection:** Package Validation (`EnablePackageValidation`,
  `PackageValidationBaselineVersion`), Microsoft.DotNet.ApiCompat.Tool,
  PublicApiGenerator; plus upstream release notes for behavioral breaks.

## Worked example — a major TFM upgrade is more than a version bump

A real upgrade across a major boundary (here: a vendor "PS5 → PS6" upgrade that
also moves `net471` → `net8.0`) shows what a `nuget-hebung` plan must capture
beyond `<PackageReference Version>` numbers. Treat this as the *shape* of the
migration map the brainstorming phase should collect, not as fixed values.

- **Package renames / splits.** Old packages are renamed or replaced, so a plain
  version bump is wrong — you must map old → new:
  - `PS5Service` → `Noxum.PS5.Service`
  - `PS5WinClient` → `Noxum.PS5.Application.RichClient`
  - `Noxum.IDML.*` → `Noxum.Publishing.*` (a whole namespace/package family)
  - `Noxum.Services.Publishing.WindowsService` → `Noxum.Services.Publishing.Service`
- **TFM suffixes matter.** WPF/WinForms projects need `net8.0-windows` (not bare
  `net8.0`); WinExe startup projects follow their referenced client.
- **Implicit framework references go away.** On SDK-style .NET 8, remove legacy
  `<Reference Include="System.*">` assembly references — they are implicit.
- **Config format changes.** `*.exe.config` (XML) → `appsettings.json`; some
  `.exe` host references become `.dll`; remoting/binding settings move out of XML
  config into JSON host config.
- **Code/XSLT API breaks on the new runtime.** `msxsl:script` blocks
  (`urn:schemas-microsoft-com:xslt`) are unsupported on .NET 8; `assembly=mscorlib`
  must become `assembly=System.Private.CoreLib` in XSLT CLR namespaces. These
  surface only at runtime, reinforcing "build success ≠ done".

The lesson for the skill: for any major/TFM upgrade, the investigators must
report renames and config/code transforms (not just version deltas), and the
plan must sequence the TFM bump + rename + config/code changes together per
project, then verify at runtime via unit tests.
