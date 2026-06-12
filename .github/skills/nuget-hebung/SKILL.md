---
name: nuget-hebung
description: Use when upgrading ("raising" / "Hebung") NuGet packages across a .NET solution, especially a large multi-project one, including bumping packages to a newer target framework (e.g. net472 -> net8.0). Drives a persisted, phase-by-phase, subagent-driven workflow — brainstorming, parallel per-project investigation written to docs/nuget-hebung/agentresults/, consolidation into a dependency + state graph, conflict resolution with the user, an approved ordered plan, parallel execution, and full-solution verification. Self-contained: needs no external "superpowers" skills. Read docs/risks-nuget-hebung.md once before planning.
---

# NuGet Hebung — drive a large NuGet upgrade with a persisted, subagent-driven workflow

You are the **orchestrator**. You own one durable artifact — the **run folder**
`docs/nuget-hebung/` — and you move the work through eight phases. Sub-work is
delegated to subagents; the heavy thinking and the user conversation stay with
you. This skill is self-contained: it does not depend on any external skill pack.

Read `docs/risks-nuget-hebung.md` once before Phase 3. **Never do a big-bang
upgrade.** Announce each phase as you enter it.

## Non-negotiable operating rules

- **Persisted state.** `docs/nuget-hebung/plan.md` is the single source of truth
  for this run. It carries the current phase, decisions, and the exact next step.
  Update it and commit it at the end of **every** phase. A fresh session must be
  able to resume from it alone.
- **Context-limit awareness & handoff.** Watch for context pressure (the
  `context-guard` hook will nudge you; you should also self-monitor — long
  transcript, repeated re-reading, the model losing the thread). When you sense
  it, **before doing more work**: (1) update `plan.md`'s `## Resume` section with
  the exact next step, (2) run the `handoff` skill to snapshot state, (3) commit,
  (4) tell the user to continue in a fresh session that opens with
  *"Read docs/nuget-hebung/plan.md and continue from Resume."* The plan doc IS the
  baton — never rely on in-context memory to survive a compaction.
- **Delegate the fan-out, keep the synthesis.** Investigation and execution are
  done by subagents (one unit of work each, fresh context). Consolidation,
  graph-building, conflict resolution, and the plan stay with you.
- **Evidence before claims.** Never say a build/restore/test passed without
  showing the command output.
- **Git safety.** Work on a feature branch; small atomic commits; never commit
  directly to `main`/`master`/`staging`/`release`. One logical bump per commit.

## The run folder (`docs/nuget-hebung/`)

| File | Holds |
|------|-------|
| `plan.md` | The persisted state machine: current phase, scope decisions, ordered plan, `## Resume` (exact next step). Updated every phase. |
| `agentresults/<projectId>/report.md` | One investigation report per project, written by an investigator subagent. |
| `dependency-graph.md` | Consolidated project-reference graph + package-usage matrix. |
| `state-graph.md` | Per package: current version -> highest available next version, per project, with security/feature classification. |
| `conflicts.md` | Version conflicts, diamond/downgrade risks, and their resolutions. |

Create `docs/nuget-hebung/` and `agentresults/` in Phase 0. Commit it — this is
durable shared memory, not scratch.

---

## Phase 0 — Preflight & persistence bootstrap

1. Confirm a feature branch (create `feature/nuget-hebung` if needed). Confirm
   `dotnet --version` works.
2. **NuGet feed / config check.** Look for a `nuget.config` (or `NuGet.Config`)
   at the repo root or solution dir. If **missing or it does not define the feeds
   this solution needs** (private feeds are common — e.g. an internal Noxum/Azure
   Artifacts feed), **stop and ask the user** for the feed URL(s) and any
   credentials source, then write/extend `nuget.config`. Without the right feed,
   "highest available version" answers will be wrong. Verify with
   `dotnet restore` (or `dotnet nuget list source`).
3. Discover the solution shape: `*.sln`, every `*.csproj`/`*.vbproj`/`*.fsproj`,
   the package style (`PackageReference` vs `packages.config`), and whether
   Central Package Management (`Directory.Packages.props`) is present.
4. Create `docs/nuget-hebung/plan.md` from the template at the bottom of this
   skill, fill in the project list, set phase to `1 — Brainstorming`, commit
   (`docs(nuget-hebung): bootstrap run plan`).

## Phase 1 — Brainstorming / scope (talk to the user, one question at a time)

Decide and record in `plan.md` under `## Scope`. Ask only what you cannot infer:

- **Driver:** security-driven (minimal, targeted, fast-tracked) or feature-driven
  (deliberate, breaking-change risk)? Keep them in separate PRs.
- **Target-framework bumps:** are any projects being raised to the next .NET
  version (e.g. `net472`/`net471` -> `net6.0`/`net8.0`)? Which projects, and to
  which TFM? TFM bumps are their own workstream and cascade to consumers.
- **Migration map (if a TFM/major bump):** ask whether there is a known
  **old-package -> new-package rename map** and known config/code transforms
  (large vendor upgrades often rename or split packages and change config —
  see the worked example in `docs/risks-nuget-hebung.md`). Capture any the user
  provides; the investigators will look for the rest.
- **Exceptions / pins:** any packages that must NOT be upgraded, must stay on a
  specific version, or are explicitly out of scope.
- **Adopt Central Package Management first?** Strongly recommended for large
  solutions (see the risk KB).

Write the answers to `plan.md` `## Scope`, set phase to `2 — Investigation`,
commit.

## Phase 2 — Investigation (parallel subagent fan-out)

Dispatch **one investigator subagent per project** (or per small cluster of tiny
projects). Use the **`nuget-project-investigator`** custom agent
(`.github/agents/nuget-project-investigator.agent.md`) — it is pinned to
**`claude-opus-4.8`**; run it at **medium reasoning effort**. Launch independent
investigators **in parallel**.

Give each investigator, in its prompt, the complete context it needs (subagents
are stateless): the absolute project path, the scope decisions + any migration
map from Phase 1, and the required output path
`docs/nuget-hebung/agentresults/<projectId>/report.md`. Each report must cover:

- Project metadata: TFM(s), package style, SDK vs legacy.
- **Every** package reference: name, current version, **highest available next
  version** on the configured feeds (`dotnet list package --outdated` /
  `dotnet package search` / feed query), and the highest version compatible with
  the (possibly bumped) target TFM.
- Per package: security vs feature classification, known breaking changes /
  rename (from the migration map or release notes), and whether it forces a TFM
  bump.
- Project-reference dependencies (which projects this one depends on).
- Risk flags: `packages.config`, binding redirects, analyzers/source-generators,
  native assets, `msxsl:script`/`mscorlib`-type framework-only APIs for TFM bumps.

**Wait for every investigator to finish.** Then set phase to `3 — Consolidation`,
commit the reports.

## Phase 3 — Consolidation (you, the orchestrator)

Read the risk KB (`docs/risks-nuget-hebung.md`) if you have not. Read every
`agentresults/*/report.md` and synthesize:

- **`dependency-graph.md`** — the MSBuild project-reference graph (who references
  whom; this drives update order) plus a package-usage matrix (which projects use
  each package and at which versions).
- **`state-graph.md`** — for each package: `current -> highest-next` target, the
  per-project current versions, and security/feature tag. Flag version drift
  (same package, different versions across projects).

Set phase to `4 — Conflict feedback`, commit.

## Phase 4 — Conflict feedback & resolution (brainstorm round 2)

Produce a concise **feedback report** (in `conflicts.md` and summarized to the
user) of everything needing a human decision:

- Version-unification / diamond conflicts; potential **NU1605** silent
  downgrades; mutually-exclusive transitive constraints.
- Packages whose highest-next forces a TFM bump not yet agreed.
- `packages.config` projects that cannot cleanly migrate.
- Ambiguous renames or packages with no clear successor.

Resolve each with the user (one question at a time). Record decisions in
`conflicts.md` and `plan.md`. Set phase to `5 — Plan`, commit.

## Phase 5 — Plan document → user review & approval

Turn the graphs + decisions into an **ordered, lane-based plan** in `plan.md`
`## Execution plan`:

- **Sequential bottlenecks (do first):** adopt CPM / edit
  `Directory.Packages.props`; unify & bump **shared foundational packages**
  (logging, DI, serialization, HTTP, ORM); **TFM bumps** on foundational libs;
  `packages.config` -> `PackageReference` migrations.
- **Parallelizable lanes (fan out after):** leaf projects (tests, top-level
  apps); disjoint dependency clusters that share no upgradeable package — each is
  a candidate for its own execution subagent.

For each step list: project(s), package(s) `current -> target`, lane
(sequential/parallel), blocked-by, and verification. Recommend enabling lock
files (`RestorePackagesWithLockFile=true`) before bulk work.

**Present the plan to the user and get explicit approval before executing.**
Set phase to `6 — Execution`, commit.

## Phase 6 — Execution (parallel where possible, via subagents)

Work the plan **bottom-up**, sequential bottlenecks first, then fan out the
parallel lanes. For independent lanes, dispatch one **`nuget-package-updater`**
subagent per lane (see `.github/agents/nuget-package-updater.agent.md`); keep
edits to the central `Directory.Packages.props` serialized (single-file
bottleneck — never edit it from two parallel agents at once).

Each unit of work:

1. Bump the version (prefer CPM `Directory.Packages.props`; use transitive
   pinning for CVE-only fixes so you don't touch every project). Apply any
   migration-map renames / config transforms for TFM bumps.
2. `dotnet restore` then `dotnet build` — watch for **NU1605** (silent downgrade =
   wrong order) and, on .NET Framework, stale binding redirects.
3. Run **unit tests** for the affected project(s); show output.
4. Commit small — one logical bump per commit.
5. **Tick the step off in `plan.md`** and commit, so progress survives a restart.

After each lane completes, update `plan.md` `## Resume`. If context gets tight,
hand off (see operating rules) — the ticked plan lets a fresh session continue.

## Phase 7 — Verify the whole solution & resolve errors

Run, from a clean state, and **show the output** for each:

- `dotnet restore --locked-mode` (reproducible restore; or plain restore if no
  lock file)
- `dotnet build` of the full solution — clean
- unit tests across the solution — pass
- `dotnet list package --vulnerable --include-transitive` — clean

On any failure: diagnose, fix (or surface to the user if it needs a decision),
re-run. Only when all four are green, write a final summary to `plan.md`
(what was raised `current -> target`, what was deferred and why, any remaining
blockers), set phase to `Done`, commit. Recommend opening the PR per the repo's
PR checklist.

---

## `plan.md` template (create in Phase 0)

```markdown
# NuGet Hebung — Run Plan

> Persisted state for this upgrade. The orchestrator updates this every phase.
> To resume in a fresh session: read this file top-to-bottom, then do `## Resume`.

## Status
- Phase: 0 — Preflight
- Branch: feature/nuget-hebung
- Started: <date>

## Scope
- Driver: <security | feature>
- TFM bumps: <projects -> target TFM, or "none">
- Migration map: <link/notes, or "discover during investigation">
- Exceptions / pins: <packages to leave alone>
- Central Package Management: <adopt? present?>

## Projects
- [ ] <projectId> — <path> — investigated? consolidated? updated?

## Execution plan
<filled in Phase 5: ordered sequential bottlenecks, then parallel lanes>

## Decisions
<scope + conflict decisions, with the reason, so they aren't re-litigated>

## Resume
<the single exact next step a fresh agent should take>
```
