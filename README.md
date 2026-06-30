# nuget-hebung-kit

A small, self-contained kit that makes **GitHub Copilot CLI** drive a large,
safe **NuGet upgrade ("NuGet Hebung")** across a multi-project .NET solution —
with a **persisted plan**, **parallel subagent investigation/execution**, and
**automated context-limit handoff**.

No Claude Code, no VS Code extension, no external "superpowers" plugin required.

It also ships a **PS5 → PS6** product-migration workflow (Noxum Publishing Studio,
.NET Framework 4.7.1 → .NET 8, SDK-style conversion, partial-success by design) —
the `ps5-to-ps6` skill with its own agents and single-file tooling under
`tools/ps5to6/`.

## What's in it

| Piece | Path | Role |
|---|---|---|
| **`nuget-hebung` skill** | `.github/skills/nuget-hebung/SKILL.md` | The 8-phase workflow: preflight + feed check → brainstorm scope → parallel per-project investigation → consolidate into dependency + state graphs → resolve conflicts → approved ordered plan → parallel execution → full-solution verification. Persists everything to `docs/nuget-hebung/`. |
| **`handoff` skill** | `.github/skills/handoff/SKILL.md` | Snapshots session state so a fresh session resumes cleanly. |
| **`nuget-project-investigator` agent** | `.github/agents/` | Read-only per-project investigator; default `claude-sonnet-4.6`, escalated to Opus per the model/effort convention. |
| **`nuget-package-updater` agent** | `.github/agents/` | Executes one approved, independent upgrade lane (parallelizable); default `claude-sonnet-4.6`, escalated to Opus per the model/effort convention. |
| **`ps5-to-ps6` skill** | `.github/skills/ps5-to-ps6/SKILL.md` | PS5→PS6 product migration: snapshot the IST state → uninstall all → bottom-up per-project scaffold / feed-probe / install / build / breaking-change fix, partial-success, persisted to `docs/ps5-to-ps6/`. |
| **`ps5-to-ps6-investigator` / `ps5-to-ps6-migrator` agents** | `.github/agents/` | Cross-check one project's snapshot; execute one project's migration. Default `claude-sonnet-4.6`, escalated to Opus per the convention. |
| **PS5→PS6 migration KB** | `docs/ps5-to-ps6/migration-kb.md` | Package rename map, per-role package sets, config + code breaking-change transforms. |
| **PS5→PS6 tools** | `tools/ps5to6/` | Single-file .NET 8 apps: `snapshot`, `uninstall-all`, `feed-probe`, `scaffold-project`, `report` (+ xUnit tests). |
| **context-guard hook** | `.github/hooks/` | `agentStop` hook that detects context pressure and forces a handoff turn. |
| **risk KB** | `docs/risks-nuget-hebung.md` | NuGet upgrade risks, CPM, lock files, ordering, and a worked TFM-bump example. |
| **bootstrap + grant-permissions + deploy-tools + smoke-check** | `scripts/` | Install the kit into a target repo (and grant the subagent allow-list); grant/reset permissions standalone; `deploy-tools.ps1 -TargetRepo <path>` (re)builds just the ps5to6 tools into a target (wipes any stale `dist` first); validate the kit. |

## Why a skill *and* agents (not just a skill)

The **skill** owns the workflow and the conversation; you invoke it once with
`/nuget-hebung`. The **agents** exist only for the parts a skill cannot express:
a **default model** (`claude-sonnet-4.6`, escalated to Opus per the model/effort
convention) and a reusable, tool-scoped, delegate-only **worker persona** for the
fan-out. The orchestrator (main session) delegates
investigation and execution to those agents and keeps the synthesis, graphs,
conflict resolution, and plan for itself.

## Quickstart

```powershell
# 1. Clone this kit next to your target repo.
git clone <your-fork>/nuget-hebung-kit

# 2. Install it into your repo (skills, agents, hook, risk KB, AGENTS.md, docs/).
#    Also grants the subagent allow-list for your repo location so the fan-out
#    doesn't stall on permission prompts (git push stays gated; -SkipPermissions
#    opts out). See SETUP.md > "Permissions".
cd nuget-hebung-kit
./scripts/bootstrap.ps1 -TargetRepo C:\path\to\your-repo

# 3. Open the target in the CLI and run the upgrade.
cd C:\path\to\your-repo
copilot
#   then, in the CLI:   /nuget-hebung
```

Full walkthrough for a first-time user: [`SETUP.md`](SETUP.md) (or the same
steps as a notebook: [`notebook.ipynb`](notebook.ipynb)).

## Persistence (durable, committed)

```
docs/
  nuget-hebung/
    plan.md                         # the run's state machine (phase + Resume)
    agentresults/<projectId>/report.md   # one investigation report per project
    dependency-graph.md             # project-reference graph + package matrix
    state-graph.md                  # current -> highest-next per package
    conflicts.md                    # conflicts + resolutions
  handoffs/<date>-<topic>.md        # session snapshots
  risks-nuget-hebung.md             # risk knowledge base
```

A fresh CLI session can rebuild full context from `docs/` alone.

## Validate the kit

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-check.ps1
# checks skill + agent frontmatter, hook JSON, and internal links
```

## Portability note

Skills here use the open [Agent Skills](https://agentskills.io) `SKILL.md`
format, and `AGENTS.md` is read by both Copilot CLI and (via a `CLAUDE.md`
bridge) Claude Code. **Hooks and custom agents are CLI-specific** and would need
re-authoring for other tools — see `CLAUDE.md`.

## License

MIT — see [`LICENSE`](LICENSE).
