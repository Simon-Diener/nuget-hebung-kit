---
name: ps5-to-ps6
description: 'Use when migrating a Noxum Publishing Studio solution from PS5 to PS6 — i.e. raising a customer solution from .NET Framework 4.7.1 to .NET 8 with SDK-style projects and the PS6 (5.4.0+/2.x/3.x) package set. Drives a persisted, modular, bottom-up, partial-success workflow: snapshot the IST state via tooling, uninstall everything, then migrate project-by-project (least dependencies first) — scaffold SDK-style net8, probe the Noxum feed for net8-available packages, install, build, resolve transitive gaps, apply known code/config breaking-change fixes — recording per-step what worked / what did not / why / what to do. Self-contained; uses the ps5to6-* tools and the ps5-to-ps6-investigator / ps5-to-ps6-migrator agents. Read docs/ps5-to-ps6/migration-kb.md before Phase 4.'
---

# PS5 → PS6 migration — drive a modular, partial-success product migration

You are the **orchestrator**. PS6 is the next generation of Noxum Publishing
Studio: same lineage as PS5 but a different technology base — **PS5 targets .NET
Framework 4.7.1, PS6 targets .NET 8**. A customer solution is mostly
**dependency-only projects** (no own code, only references) plus a few **code
projects** (customer extensions / integrations).

This is a **moving target**: not every old package has a net8 successor, so the
migration is **partially successful by design**. Work **modularly, bottom-up**
(least dependencies first) — a uniform big-bang raise has failed before due to
cross-dependencies. In .NET 8, dependencies load **transitively**: do NOT re-add a
package just because PS5 listed it; add back only what the build proves missing
**and** that has a net8 build.

You own one durable artifact — the **run folder** `docs/ps5-to-ps6/`. You delegate
the fan-out (investigation, hard migrations) to subagents and keep the synthesis,
ordering, decisions, and report. **Announce each phase as you enter it.** Read
`docs/ps5-to-ps6/migration-kb.md` once before Phase 4.

## Non-negotiable operating rules

- **Persisted state.** `docs/ps5-to-ps6/plan.md` is the single source of truth:
  current phase, scope, ordered plan, decisions, and the exact next step
  (`## Resume`). Update and commit it at the end of **every** phase. A fresh
  session must resume from it alone.
- **Context-limit awareness & handoff.** Watch for context pressure (the
  `context-guard` hook nudges you; also self-monitor). When you sense it, before
  doing more work: (1) update `plan.md` `## Resume` with the exact next step,
  (2) run the `handoff` skill, (3) commit, (4) tell the user to continue in a
  fresh session that opens with *"Read docs/ps5-to-ps6/plan.md and continue from
  Resume."* The plan IS the baton.
- **Deterministic tooling over agent code.** Use the `ps5to6-*` tools for
  snapshot / uninstall / feed-probe / scaffold / report. Reserve subagents for
  judgement (cross-checks, transitive-gap resolution, code/config fixes). This
  keeps token cost down — it is what makes an AI-driven Hebung cost-effective.
- **Evidence before claims.** Never say a build/restore/test passed without
  showing the command output.
- **Per-project build gate (hard stop).** A project is *done* only when
  `dotnet build <project>` has been run **and its output shown**, and it reached
  exactly one of two terminal states:
  - **green** (exit 0) → ✅ raised, or ⚠️ partial if optional packages with no
    net8 build were dropped but the project still compiles; or
  - **blocked** ⛔ → the build fails *solely* because a required **non-Noxum**
    dependency has no net8 build anywhere (confirmed via feed-probe / `dotnet
    package search`), recorded in `gaps.md` with the failing output.

  **Any other red state is "not done"** — missing packages that *do* have a net8
  build, unresolved transitive gaps, un-applied KB code/config fixes, or an
  empty/near-empty scaffold. **You MUST NOT start the next project until the
  current one reaches a terminal state.** No build output = not done. This
  invariant is what keeps the bottom-up order valid: a project builds only after
  everything it references is green.
- **Git safety.** Feature branch; small atomic commits; never commit directly to
  `main`/`master`/`staging`/`release`. One project (or one logical fix) per commit.
- **Partial success is a valid outcome.** A hard gap (a non-Noxum dependency with
  no net8 build) is recorded as a blocker with a recommendation — not a reason to
  stall the whole run. Move to the next project.

## Tools (the `ps5to6-*` single-file apps)

Installed by `scripts/bootstrap.ps1` under `tools/ps5to6/dist/` (or run via
`dotnet run --project tools/ps5to6/src/<Tool>` in the kit repo):

| Tool | Use |
|---|---|
| `ps5to6-snapshot <root> <outDir>` | Inventory: classification (code vs dependency-only), TFM, SDK/legacy, packages (MS/System flagged), project graph + bottom-up order → `inventory.json` + `inventory.md`. |
| `ps5to6-uninstall-all <root> [--apply] [--keep-packages-config]` | Strip every package reference (dry-run by default). |
| `ps5to6-feed-probe <nugetConfigDir> <packageListFile> <outJson>` | Highest net8-compatible version per package on the configured feeds. |
| `ps5to6-scaffold-project <Service\|RichClient\|PublishingService\|Configuration> <packagesJson> <outCsproj>` | Generate the SDK-style net8 `.csproj` for a known PS project type. |
| `ps5to6-report <runStatusJson> <outMd>` | Aggregate the run status into `report.md`. |

## Phases

### Phase 0 — Preflight & persistence bootstrap
1. Confirm a feature branch (create `feature/ps5-to-ps6` if needed). Confirm `dotnet --version`.
2. **Feed check.** Confirm the **Noxum NuGet feed** is defined in the solution's
   `nuget.config`. If missing, **STOP and ask the user** for the feed URL +
   credentials source, then write/extend `nuget.config`. Without it, feed-probe
   answers are wrong.
3. Confirm the `ps5to6-*` tools are runnable.
4. Create `docs/ps5-to-ps6/` + `agentresults/`; seed `plan.md` from the template
   below; set phase to `1 — Snapshot`; commit (`docs(ps5-to-ps6): bootstrap run plan`).

### Phase 1 — Snapshot (IST state, via tooling)
Run `ps5to6-snapshot <solutionRoot> docs/ps5-to-ps6` → `inventory.json` + `inventory.md`.
Dispatch **`ps5-to-ps6-investigator`** subagents to **cross-check the largest /
most-complex projects** (verify classification + dependency capture, and grep for
code-level breaking-change sites). Commit the inventory + agent reports.

### Phase 1.5 — Triage & routing (steer the token budget)
The inventory now exists, so size the job once and get the user's sign-off before
spending on execution:
1. **Size it** from `inventory.json`: project count, dependency-only vs code
   projects, and how many code projects show breaking-change sites (investigator
   reports).
2. **Recommend the orchestrator / planning model:** large or code-heavy →
   orchestrate on **Opus `high`** (`xhigh` only for the master plan); small, mostly
   dependency-only → **Sonnet `high`** suffices. State it; let the user confirm.
3. **Route every project** per the rubric in
   [`docs/conventions/model-and-effort.md`](../../../docs/conventions/model-and-effort.md)
   (*Per-item model routing & budget*): dependency-only → **no agent**; code-light →
   Sonnet `medium`; code-heavy / breaking → Opus; security → Opus `high`. Write the
   **planned** model/effort + a coarse token bucket per project to `cost-ledger.md`
   *before* executing.
4. **Set a soft budget:** record a `## Budget` target in `plan.md`. If the summed
   estimate exceeds it, propose cheaper routing (more no-agent/scripted, Sonnet over
   Opus, cross-check only the top-N largest projects) and ask — never hard-stop
   mid-migration.

Set phase to `2 — Uninstall-all`; commit.

### Phase 2 — Uninstall-all (clean baseline)
Run `ps5to6-uninstall-all <solutionRoot> --apply`. The build is expected to break —
this is the clean baseline. Commit it. (Microsoft/System packages are recorded in
the snapshot as IST but are not re-added by default; .NET 8 supplies them.)

### Phase 3 — Order
Read the bottom-up order from `inventory.json` into `dependency-graph.md`. This is
the project order for Phase 4 (a project is migrated only after everything it
references).

### Phase 4 — Per-project migration loop (bottom-up)
Read `migration-kb.md` first, and apply the per-project model/effort **routing
decided in Phase 1.5** (default down, escalate on evidence). For each project in
order:
1. **Classify** the role (Service / RichClient / PublishingService / Configuration
   / custom).
2. **feed-probe** the KB package set for that role → which Noxum packages have a
   net8 build; collect any that do not.
3. **Scaffold** (known role) with `ps5to6-scaffold-project` using the feed-probe
   confirmed versions, applying the rename map. Custom projects: convert by hand
   (or via the migrator agent).
4. **Install** the confirmed packages (`dotnet add package`), apply rename map.
   **Scaffold sanity:** for a known role (Service / RichClient / PublishingService),
   the project MUST end up with the role's core package set installed. An empty or
   near-empty `<ItemGroup>` means feed-probe returned no net8 build for the role's
   core packages — **STOP and report**; do not scaffold an empty project and move
   on (this is exactly how a "migrated" project ends up with zero packages).
5. **Build — the mandatory gate.** Run `dotnet build <project>` and read the
   output. Then apply the **per-project build gate** rule:
   - **Green** and dependency-only → record + commit, **no agent**.
   - **Green** and code project → record + commit.
   - **Red** → the project is **not done**; dispatch the migrator (step 6). The
     only red state that lets you advance is a recorded **blocker** (a genuine
     hard gap per the gate rule).
6. Dispatch **`ps5-to-ps6-migrator`** for a red project: resolve missing transitive
   deps (add back only net8-available ones, confirmed by feed-probe), apply KB
   **code + config** breaking-change fixes, keep building until **green** or a
   **hard gap** (a required non-Noxum dependency with no net8 build) is reached.
   The migrator returns the final `dotnet build` result and which terminal state
   it hit. **If it comes back red-but-fixable, re-dispatch — do not accept it as
   done.**
7. **Gate decision — record the terminal state.** Confirm the project is green or
   a recorded blocker, then append the `steps.md` block (works / doesn't / why /
   do) **including the `dotnet build` result line (exit code + error count) as
   evidence**. Persist safe package mappings to `mappings.md`; record gaps/blockers
   + recommendations in `gaps.md`. Append a `cost-ledger.md` row. Tick the project
   in `plan.md` `## Projects` only now.
8. Commit per project; update `plan.md` `## Resume`. Hand off if context tight.

**Definition of done (per project) — all four, or it is not done and you may not advance:**
1. `dotnet build <project>` was run and its output shown.
2. Result is **green**, or a **blocker** recorded in `gaps.md` with the failing output.
3. `steps.md` block appended, including the build result line.
4. Project committed and ticked in `plan.md` `## Projects`.

### Phase 5 — Report
Build a `runStatus` JSON (per-project outcome `Raised|Partial|Blocked`, unmapped
Noxum packages, missing net8 dependencies) and run `ps5to6-report` →
`docs/ps5-to-ps6/report.md`. Summarize to the user: what was raised, what is
partial/blocked, how breaking changes were handled, and the cost summary.

## Per-step minimal feedback (`steps.md`)

Append one block per project; keep phrasing minimal:

```markdown
### <projectId> — ✅ raised | ⚠️ partial | ⛔ blocked
- Works: <what built/installed>
- Doesn't: <what failed / what's missing>
- Why: <root cause, one line>
- Do: <recommendation>
```

## Cost ledger (`cost-ledger.md`)

Two entries per project make cost *steerable*: the **planned** model/effort + token
bucket written in Phase 1.5 *before* dispatch, then the **actual** after the work.
This feeds the user's AI-vs-manual cost/benefit estimate.

```markdown
| Project | Plan/Actual | Model | Effort | #Subagents | Wall-clock | Token bucket |
|---|---|---|---|---|---|---|
```

## Run folder (`docs/ps5-to-ps6/`, committed)

| File | Holds |
|------|-------|
| `plan.md` | State machine: phase, scope, ordered plan, `## Resume`. Updated every phase. |
| `migration-kb.md` | The PS5→PS6 transform reference (rename map, package sets, config + code breaking changes). |
| `inventory.json` / `inventory.md` | Snapshot (IST state). |
| `dependency-graph.md` | Project-reference graph + bottom-up order. |
| `agentresults/<projectId>/report.md` | Investigator cross-check reports. |
| `steps.md` | Per-step minimal feedback log. |
| `mappings.md` | Safe net8 package mappings that worked (current → net8). |
| `gaps.md` | No-net8 packages, unmapped Noxum packages, blockers + recommendations. |
| `cost-ledger.md` | Per-project model/effort/subagent/token row. |
| `report.md` | Final report. |

## `plan.md` template (create in Phase 0)

```markdown
# PS5 → PS6 — Run Plan

> Persisted state for this migration. The orchestrator updates this every phase.
> To resume in a fresh session: read this file top-to-bottom, then do `## Resume`.

## Status
- Phase: 0 — Preflight
- Branch: feature/ps5-to-ps6
- Started: <date>

## Scope
- Solution root: <path>
- Noxum feed: <url, or "TODO — ask user">
- Exceptions / pins: <packages or projects to leave alone>

## Budget
- Orchestrator model: <Opus high | Sonnet high> (decided in Phase 1.5)
- Token target: <soft target, or "none"> — orchestrator asks before exceeding

## Projects
- [ ] <projectId> — <role> — snapshotted? migrated? outcome?

## Execution order
<bottom-up order from inventory.json, filled in Phase 3>

## Decisions
<scope + gap decisions, with the reason, so they are not re-litigated>

## Resume
<the single exact next step a fresh agent should take>
```
