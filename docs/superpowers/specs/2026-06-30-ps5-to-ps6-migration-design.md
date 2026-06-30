# Design — PS5 → PS6 migration kit

> Approved design (brainstorming output). Status: **approved 2026-06-30**.
> Next step after user review of this spec: `writing-plans` → implementation plan.

## Problem

The real task is **not** a generic NuGet version bump. It is the migration of a
customer solution from **Noxum Publishing Studio 5 (PS5)** to **PS6**. PS6 is the
next product generation: it shares PS5's lineage but rests on a different
technology base — PS5 targets **.NET Framework 4.7.1**, current PS6 targets
**.NET 8** (a .NET 6 line exists but lags the active .NET 8 development state).

A customer solution is assembled per-customer and consists mostly of
**dependency-only projects** (no own code — they only bundle NuGet/project
references) plus a smaller number of **code projects** (customer-specific
functional extensions, API/data-source integrations, etc.).

### Why this is hard (the constraints that shape the design)

- **Moving target.** PS6 on .NET 8 is under active development. There is no
  guarantee every old package has a net8-compatible successor. The migration can
  only ever be **partially successful** — every package must be checked against
  the internal **Noxum NuGet feed**, and every gap/error needs a deliberate
  decision: ignore, work around, or block.
- **Modular, bottom-up only.** A uniform big-bang raise has failed before due to
  cross-dependencies. We must go **project by project, least-dependencies
  first**.
- **Transitive loading is new.** .NET Framework 4.7.1 (`packages.config`) makes
  every package explicit; .NET 8 SDK-style projects load dependencies
  **transitively**. So we cannot assume every previously-explicit package must be
  re-added. The correct procedure per project is: snapshot current dependencies →
  uninstall everything → install the new Noxum packages → see what is *not*
  pulled in transitively → re-add only those that are missing **and** available
  for net8.
- **SDK-style + net8 conversion.** Projects must be converted to SDK-style with
  `PackageReference` and retargeted (`net8.0`, or `net8.0-windows` for the
  RichClient).
- **Code breaking changes.** Code projects need fixes for APIs that changed or
  were removed in .NET 8 (e.g. `Thread.Abort`, `msxsl:script`, XSLT `document()`,
  `mscorlib`), preserving prior behaviour.
- **Token cost matters.** Work must be attributable per project for a later
  cost/benefit calculation. Prefer deterministic **scripts/tooling** over
  agent-driven code wherever a script can do the job reliably.

## Scope of this work (deliverable)

**Build the reusable kit only** — a new skill + supporting agents + SFA/PowerShell
tooling, inside this repo. There is **no target solution** present; tooling is
validated with **unit tests + synthetic fixtures**. The actual migration runs
later, in a separate session, against a customer solution.

Out of scope: running a real migration; credentials for the live Noxum feed
(the feed-probe tool reads feed config from the target's `nuget.config` at run
time and is tested against an offline feed stub).

## Decisions (locked during brainstorming)

1. **New dedicated skill** `ps5-to-ps6`; the existing generic `nuget-hebung`
   skill stays untouched. Shared concepts (persisted run folder, handoff,
   subagent fan-out) are re-applied, not physically shared.
2. **C# single-file apps (SFA)** for parsing/analysis/feed/report;
   **PowerShell** for thin glue. Push scripting to the limit of what a script can
   do *reliably* — to save tokens.
3. **Approach A — thin tools, smart agent**, extended with a scripted
   `scaffold-project` step: deterministic SDK-style/net8 conversion + package
   install for the known PS project types; the agent only engages when the
   scripted scaffold + build is insufficient (gaps, code fixes, custom projects).
4. **Structured per-project cost ledger** in the run folder.
5. **Minimal-phrasing per-step feedback** (works / doesn't / why / recommendation)
   is a first-class persisted artifact.

## Architecture

### § 1 — Naming & layout

- Skill: `.github/skills/ps5-to-ps6/SKILL.md`
- Agents: `.github/agents/ps5-to-ps6-investigator.agent.md`,
  `.github/agents/ps5-to-ps6-migrator.agent.md`
- Tools: `tools/ps5to6/` — one C# console project per tool (each single-file
  publishable) + one xUnit test project, in a small tools solution.
- Run folder + KB: `docs/ps5-to-ps6/`.

Agents default to `claude-sonnet-4.6`, escalated to Opus per
`docs/conventions/model-and-effort.md` (first-attempt failure · 5+ files ·
architectural decision · security-critical · large/code-heavy project).

### § 2 — C# SFA tooling

Each tool is a focused console app, single-file publishable, individually
runnable, and unit-tested.

1. **`snapshot`** — parse the solution and every project; emit `inventory.json`
   (+ human-readable `inventory.md`). Captures per project: TFM(s), SDK-style vs
   legacy, `OutputType`, **classification** (code vs dependency-only), packages
   (direct from `.csproj`/`packages.config` plus restorable transitive closure,
   with Microsoft/System packages flagged but still recorded as the IST state),
   and the **project-reference graph**. Also emits the **computed bottom-up
   order** (topological sort of the project-reference graph).
2. **`uninstall-all`** — given the inventory, strip all package references from
   every project (and `packages.config` entries). Dry-run + apply modes;
   idempotent. Produces the clean baseline. (State is already persisted by
   `snapshot`, so this is mechanical.)
3. **`feed-probe`** — given a package-id list and target TFM, query the
   configured feeds (NuGet.Protocol; feeds read from the target's `nuget.config`)
   for the highest net8-compatible version. Emit an availability matrix.
4. **`scaffold-project`** — for a recognised PS project type (Service /
   RichClient / PublishingService / Configuration), write the SDK-style net8
   `.csproj` from the KB template + the required/optional Noxum package set,
   applying the rename map. Deterministic happy-path conversion; refuses
   (and defers to the agent) on unrecognised/custom projects.
5. **`report`** — aggregate the run-folder ledgers (`mappings.md`, `gaps.md`,
   `cost-ledger.md`, `steps.md`, per-project status) into the final `report.md`.

**Classification rule (dependency-only vs code):** a project is *dependency-only*
when it has no compilable source contributing real code (only references; no
`.cs` beyond generated/assembly-info). Otherwise it is a *code project*.

### § 3 — Agents (engaged only where judgement is required)

- **`ps5-to-ps6-investigator`** (read-only) — cross-checks the `snapshot` for the
  largest / most-complex projects (verifies classification and dependency
  capture) and greps for code-level breaking-change sites: `Thread.Abort`,
  `msxsl:script`, XSLT `document(`, `mscorlib`, subclasses of `ServerJob` /
  `PreparationModule` / `ServiceModuleBase` / BinaryService base classes, and
  `Noxum.IDML.*` references. Writes
  `docs/ps5-to-ps6/agentresults/<projectId>/report.md`.
- **`ps5-to-ps6-migrator`** (read-write) — runs one project's migration loop when
  the scripted scaffold + build is not enough: resolves transitive gaps, applies
  code breaking-change fixes per the KB, and records the per-step result. Used
  for code projects and any project where scaffold+build fails.

The orchestrator keeps ordering, gap/conflict decisions, the plan, the report,
and the cost ledger.

### § 4 — Orchestrator skill phases

0. **Preflight** — confirm a feature branch; `dotnet --version`; check the Noxum
   feed is defined in `nuget.config` (stop & ask if missing); publish the SFAs;
   discover the solution shape. Create/seed `docs/ps5-to-ps6/plan.md`, commit.
1. **Snapshot** (SFA) → `inventory`; investigator cross-checks the biggest
   projects. Commit.
2. **Uninstall-all** (SFA) → clean baseline. Commit.
3. **Order** — bottom-up order from the snapshot graph.
4. **Per-project loop** (bottom-up). For each project:
   `scaffold-project` → `feed-probe` → install available packages (apply rename
   map) → `dotnet build`. If the build is clean **and** the project is
   dependency-only → record + commit, **no agent**. Otherwise dispatch
   `ps5-to-ps6-migrator` to resolve transitive gaps / apply code fixes. Keep
   pulling missing net8-available deps until the project builds or a **hard gap**
   (a dependency simply unavailable for net8) is reached → record the blocker and
   move on (partial success). Persist safe mappings, append the per-step record
   and the cost-ledger row, commit per project, update `## Resume`, hand off if
   context gets tight.
5. **Report** (SFA) → final `report.md`: which deps remain missing, which Noxum
   packages could not be mapped, how breaking changes / missing projects were
   handled, per-project status, and the cost summary.

### § 5 — Per-step minimal feedback (persisted to `steps.md`)

Appended per project and surfaced to the user in minimal phrasing:

```
### <projectId> — ✅ raised | ⚠️ partial | ⛔ blocked
- Works: <what built/installed>
- Doesn't: <what failed / what's missing>
- Why: <root cause, one line>
- Do: <recommendation>
```

The convention is documented in the SKILL; a one-line pointer is added to
`AGENTS.md`.

### § 6 — Run folder `docs/ps5-to-ps6/` (committed durable state)

| File | Holds |
|------|-------|
| `plan.md` | State machine: current phase, scope, ordered plan, `## Resume`. Updated every phase. |
| `inventory.json` / `inventory.md` | `snapshot` output (IST state). |
| `dependency-graph.md` | Project-reference graph + computed bottom-up order. |
| `agentresults/<projectId>/report.md` | Investigator cross-check reports. |
| `steps.md` | Per-step minimal feedback log (works/doesn't/why/do). |
| `mappings.md` | Safe net8 package mappings that worked (current → net8). |
| `gaps.md` | Packages with no net8 version, unmapped Noxum packages, blockers + recommendations. |
| `cost-ledger.md` | Per project/phase: model, effort, # subagent dispatches, wall-clock, coarse token bucket. |
| `report.md` | Final report. |

### § 7 — Knowledge base `docs/ps5-to-ps6/migration-kb.md`

Distilled from `6.md` (PS6 release notes) and `Adjustments.md` (the official
PS5→PS6 migration checklist). The durable reference the orchestrator and agents
consult:

- **Package rename map:** `LicenseTool` → `Noxum.PS5.SoftDevTools.LicenseTool`;
  `PS5WinClient` → `Noxum.PS5.Application.RichClient`; `Noxum.IDML.*` →
  `Noxum.Publishing.*`; `IdmlToHtmlConverter` → `IpubToHtmlConverter`;
  `Noxum.Publishing.Core.IdmlExtension` → `IpubExtension`; XML namespace
  `ipub:IdmlExtension` → `ipub:IpubExtension`.
- **Required/optional package sets + TFM per project type:** Service (`net8.0`),
  RichClient (`net8.0-windows`), PublishingService (`net8.0`), Configuration
  (`net8.0`) — package lists as enumerated in `Adjustments.md` §1.1–1.4.
- **Config transforms:** `*.exe.config` → `appsettings.json` /
  `*.dll.config`; XSLT `msxsl:script` removal; XSLT `document()` external URI →
  `ResolveExtension`; `assembly=mscorlib` → `System.Private.CoreLib`;
  `GlobalDefinitions/@version` → `6.0.0`; `PublisherDefinitionList` server/tcpPort/
  endpoint attributes removed (moved to appsettings); PublishingService
  `remotingtcpport="-1"`; wrappers/ObjectData `.exe` → `.dll`;
  `Noxum.IDML.PdfWriter` → `FoWriter.Cmd` + AntennaHouse wrapper;
  `InfoProviderDefinitionList` `PS5.TopicTypeIcon` type-string change.
- **Source-code breaking changes:** `Thread.Abort` → cooperative
  `CancellationToken` cancellation for subclasses of `ServerJob`,
  `PreparationModule`, `ServiceModuleBase`, and the BinaryService base classes;
  removed controls (`BinaryImportWPF`, `WorkflowStatusInfo`, `WpfConverter`);
  removed EventLog/Mandator/multi-session capabilities.

### § 8 — Wiring & tests

- **AGENTS.md / README / SETUP** — register the new skill in the skill-first
  rule, the subagents list, the persistence table, and the layout; add the
  per-step feedback convention pointer.
- **`scripts/smoke-check.ps1`** — assert presence of the new skill, both agents,
  the tools solution, and the KB.
- **`scripts/grant-permissions.ps1`** — pre-approve `dotnet publish`, the SFA
  invocations, and `dotnet add/remove package`.
- **`scripts/bootstrap.ps1` (the setup script — final step of the build)** —
  extend the existing kit-installer so that, in addition to the `nuget-hebung`
  components, it copies the new PS5→PS6 kit into a target solution: the
  `ps5-to-ps6` skill, both `ps5-to-ps6-*` agents, the migration KB
  (`docs/ps5-to-ps6/migration-kb.md`), the **built single-file SFA tools** (so
  the target needs no build of the kit), and the `docs/ps5-to-ps6/agentresults`
  persistence layout; and grants the new permission allow-list. Decide whether
  the SFAs are shipped as pre-published single-file exes or built on first use in
  the target — default: ship pre-published exes under a kit `tools/ps5to6/dist/`
  so the target solution is self-contained. Update the script's "Next steps"
  footer to mention `/ps5-to-ps6`.
- **xUnit fixtures** — synthetic legacy `packages.config` project, an SDK-style
  project, a dependency-only project, a mini project-reference graph, and an
  offline feed stub for `feed-probe`. Each SFA unit-tested.

## Testing strategy

Unit tests only (per repo dogma — no integration tests against shared customer
systems). Each SFA is tested against synthetic fixtures committed under the tools
test project. `feed-probe` is tested against a local/offline feed stub, never the
live Noxum feed. `smoke-check.ps1` validates kit structure end-to-end.

## Non-goals / YAGNI

- No live-feed credential handling in this kit.
- No automated SDK-style conversion of arbitrary/custom projects (scaffolding
  covers the known PS types; custom projects defer to the agent).
- No physical sharing of machinery with `nuget-hebung` (re-applied, not
  refactored into a shared core).

## Open items for the plan phase

- Exact tool CLI surface (args/flags) per SFA.
- `inventory.json` schema.
- Target .NET SDK version for building/publishing the SFAs.
