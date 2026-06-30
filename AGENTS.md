# AGENTS.md — operating contract for this repo

> **Single source of truth.** This repo is wired for **GitHub Copilot CLI**;
> Claude Code reads it via a one-line `CLAUDE.md` (`@AGENTS.md`), and other
> AGENTS.md-aware tools read it natively. Read this file first. These rules are
> the default; deviations need an explicit reason. The long-form dogma lives in
> [`docs/conventions/dogma.md`](docs/conventions/dogma.md).

## Project

A small, self-contained kit that makes the CLI drive a large, safe **NuGet
upgrade ("NuGet Hebung")** across a multi-project .NET solution — with a
**persisted plan**, **parallel subagent investigation/execution**, and
**automated context-limit handoff**. Durable run state lives in `docs/` and is
committed, so a fresh session rebuilds context from files alone.

## Skill-first rule

Before responding to a request — including clarifying questions — check whether
an installed skill applies and use it. If there is even a small chance a skill
applies, use it. For a NuGet upgrade ("Hebung") of any size, use the
**`nuget-hebung`** skill. When a session gets long or context feels tight, use
the **`handoff`** skill.

## Dogma (summary)

- **Plan before code.** Brainstorm → consolidate findings → resolve conflicts →
  write an approved plan → execute → verify.
- **Persisted state.** Durable run state lives in `docs/` and is committed.
  Never rely on in-context memory surviving a compaction.
- **Incremental over big-bang.** Small atomic Conventional Commits, one logical
  change each.
- **No laziness** — no placeholders/TODOs; complete, production-ready changes.
- **Unit tests only** for verification — never integration tests against shared
  customer systems.
- **Evidence before claims** — run the command and show output before saying
  something works.
- **Push/commit is gated** — never commit directly to a protected branch.

Full version, with the workflow phases and code-quality detail:
[`docs/conventions/dogma.md`](docs/conventions/dogma.md).

## Language policy

Input: any language. **Output: English only** — code, identifiers, comments,
doc-strings, `.md` files, log/commit messages, PR descriptions. (Exception:
user-facing UI copy follows the product's language.) Translate non-English
leftovers you encounter while editing a file.

## Error analysis — fix the cause, not the symptom

- **Diagnose before patching.** Trace the failure to its origin and confirm the
  cause before reasoning further.
- **No band-aids.** Fix the actual defect — never wrap it in a try/catch,
  null-check, fallback value, or "robust" emergency exit.
- **Fix where it belongs.** Address the cause in its own layer, not downstream.
  Prefer the primitive that removes the whole class of problem over a local hack.
- **No uninvited fallbacks.** No default values, `?? "..."`, swallowing
  try/catch, or compat shims unless asked. Missing config fails loudly (`throw`).
  Validate only at real boundaries (HTTP input, external APIs).
- **Ambiguous cause → stop and report** with the evidence and what you ruled out.
  A recurring error is a signal to consult the user, not to guess again.

## Code quality

- **Refactor over append.** Integrate cleanly; split units that grow too large
  or mix concerns.
- **Small, reviewable changes** — one concern each; split large refactors.
- **No speculative features or premature abstraction** — build what's needed now.
- **No backwards-compat cruft** — no dead re-exports, `// removed:` notes, or
  unused `_var` renames. Delete what is unused.
- **No drive-by edits.** Touch only what the task requires; mention unrelated
  smells, don't fix them.

## Working agreement

- **Surface assumptions, don't pick silently.** Name plausible readings and ask.
- **Evidence before claims.** Run the command and show output; if tests fail or
  a step was skipped, say so.
- **Push back when scope is wrong** — a request that violates the project's
  non-goals or load-bearing invariants gets flagged, not silently followed.
- **Keep this contract alive.** When durable cross-cutting knowledge emerges,
  *propose* adding it here (show the wording; the user approves — never edit
  silently). Route by scope: cross-cutting rule → `AGENTS.md`; domain detail → a
  `docs/*.md`; one-off gotcha → commit message / memory.

## Quality gates

A fixed set of checks — **build, test, lint/format — must pass before any change
is complete.** Never leave a broken build, red test, or formatter diff. Fill in
the target solution's commands:

```bash
# C# / .NET — always pass the solution explicitly
dotnet build  <Solution>.slnx
dotnet test   <Solution>.slnx            # unit tests only
dotnet format <Solution>.slnx --verify-no-changes

# JS / TS — via the project's package scripts
npm run build && npm test && npm run lint && npm run format:check
```

## Git workflow

- **Never commit directly to a protected branch** (`main` / `master` /
  `staging` / `release` / the integration branch) — feature branch + PR only.
- Branch early — before the first edit lands.
- Small atomic Conventional Commits, one logical change each; the message
  explains *why*.
- No `--no-verify`; no destructive git operations without explicit confirmation.

## Conventions

Cross-stack:

- **Match the surrounding code**; the repo's formatter/linter config is
  authoritative — let it run, don't fight it.
- **No secrets in source** — use the platform mechanism (User Secrets, env vars,
  secret store).
- **Verify current framework/library APIs against the docs** (context7 /
  microsoft-learn), not memory.

Per-language (keep only what applies):

- **C#:** explicit types over `var` in production code; config via `IOptions<T>`;
  async end-to-end (`Async` suffix, forward `CancellationToken`; no `.Result` /
  `.Wait()`); structured logging via `ILogger<T>` message templates, never string
  interpolation; `Add*`/`Use*` extension methods in a companion `*Extensions.cs`.
- **JavaScript / TypeScript:** prefer TypeScript and explicit types on public
  boundaries; `const` over `let`, never `var`; no `any` without reason;
  async/await over raw promise chains; ES module imports, no dead re-exports.
- **CSS:** follow the project's methodology (utility-first, BEM, CSS modules)
  consistently; avoid brittle magic numbers — use design tokens / custom
  properties; keep selector specificity low.

## Model & effort selection

Use the **lowest viable model and effort** for the task; escalate only on
evidence (first attempt failed · 5+ files · architectural decision ·
security-critical). Default to Sonnet for most coding. The full task→model→effort
mapping and the per-kit application table:
[`docs/conventions/model-and-effort.md`](docs/conventions/model-and-effort.md).

## Subagents

Heavy fan-out work is delegated to subagents with a default model, escalated per
the model/effort convention:

- **`nuget-project-investigator`** (`.github/agents/`) — read-only per-project
  investigation during a Hebung; default `claude-sonnet-4.6`, escalated to Opus
  for security-critical or large projects, run at medium reasoning effort.
  Writes `docs/nuget-hebung/agentresults/<projectId>/report.md`.
- **`nuget-package-updater`** (`.github/agents/`) — executes one approved,
  independent upgrade lane (parallelizable); default `claude-sonnet-4.6`,
  escalated to Opus on first-attempt failure or a migration-heavy lane.

The orchestrator keeps synthesis, graph-building, conflict resolution, and the
plan (Opus / high effort); it delegates investigation and execution.

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
| `docs/conventions/dogma.md` | Long-form agentic-coding dogma & workflow |
| `docs/conventions/model-and-effort.md` | Model + effort selection convention (token saving) |
| `docs/handoffs/` | Session-state snapshots (written by the `handoff` skill / hook) |
| `docs/risks-nuget-hebung.md` | NuGet upgrade risk knowledge base |
| `tasks/` | Throwaway scratch (gitignored) |

## Layout

- `.github/skills/` — `nuget-hebung`, `handoff`, `refactor` (portable `SKILL.md`).
- `.github/agents/` — `nuget-project-investigator`, `nuget-package-updater`.
- `.github/hooks/hooks.json` — `agentStop` context-guard → handoff nudge.
- `docs/conventions/` — `dogma.md`, `model-and-effort.md`.
- `docs/risks-nuget-hebung.md` — risk knowledge base for the upgrade.
