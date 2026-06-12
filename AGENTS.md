# AGENTS.md — operating contract for this repo (GitHub Copilot CLI)

This repo is wired for **GitHub Copilot CLI**. Read this file first; it is read
natively by the CLI (and by Claude Code via `CLAUDE.md` → `@AGENTS.md`).

## Skill-first rule

Before responding to a request — including clarifying questions — check whether
an installed skill applies and use it. If there is even a small chance a skill
applies, use it. For a NuGet upgrade ("Hebung") of any size, use the
**`nuget-hebung`** skill. When a session gets long or context feels tight, use
the **`handoff`** skill.

## Dogma (summary)

- **Plan before code.** Brainstorm → consolidate findings → resolve conflicts →
  write an approved plan → execute → verify.
- **Persisted state.** Durable run state lives in `docs/` and is committed, so a
  fresh session can rebuild context from files alone. Never rely on in-context
  memory surviving a compaction.
- **Incremental over big-bang.** Small atomic commits, Conventional Commits, one
  logical change per commit.
- **No laziness** — no placeholders/TODOs; complete, production-ready changes.
- **Unit tests only** for verification — do not run integration tests against
  shared customer systems.
- **Evidence before claims** — run the command and show output before saying
  something works.
- **Push/commit is gated** — never commit directly to `main`/`master`/`staging`/
  `release`; feature branch + PR only.

## Subagents

Heavy fan-out work is delegated to subagents with pinned models:

- **`nuget-project-investigator`** (`.github/agents/`) — read-only per-project
  investigation during a Hebung; pinned to `claude-opus-4.8`, run at medium
  reasoning effort. Writes `docs/nuget-hebung/agentresults/<projectId>/report.md`.
- **`nuget-package-updater`** (`.github/agents/`) — executes one approved,
  independent upgrade lane (parallelizable); pinned to `claude-opus-4.8`.

The orchestrator keeps synthesis, graph-building, conflict resolution, and the
plan; it delegates investigation and execution.

## Context management & handoff

The `agentStop` hook in `.github/hooks/` detects a long / context-pressured
session and forces a turn that persists state (update
`docs/nuget-hebung/plan.md` `## Resume`, run the `handoff` skill, commit). You
may also run `handoff` manually anytime you sense drift.

## Persistence — where durable artifacts live (committed)

| Path | Holds |
|------|-------|
| `docs/nuget-hebung/plan.md` | The persisted run state machine for a NuGet Hebung |
| `docs/nuget-hebung/agentresults/<projectId>/` | Per-project investigation reports |
| `docs/nuget-hebung/{dependency-graph,state-graph,conflicts}.md` | Consolidated graphs + conflict decisions |
| `docs/handoffs/` | Session-state snapshots (written by the `handoff` skill / hook) |
| `docs/risks-nuget-hebung.md` | NuGet upgrade risk knowledge base |
| `tasks/` | Throwaway scratch (gitignored) |

## Layout

- `.github/skills/` — `nuget-hebung`, `handoff` (portable `SKILL.md`).
- `.github/agents/` — `nuget-project-investigator`, `nuget-package-updater`.
- `.github/hooks/hooks.json` — `agentStop` context-guard → handoff nudge.
- `docs/risks-nuget-hebung.md` — risk knowledge base for the upgrade.
