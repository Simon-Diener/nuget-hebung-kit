# Handoff — agentic-coding dogma done; define next skill — 2026-06-30

## Goal
Two things, one finished and one starting. (1) **Done:** absorb the team's new
agentic-coding sources into this repo as a lean `AGENTS.md` + linked dogma /
model-effort docs, and retune the pinned subagents. (2) **Next:** the task is
**not just a NuGet Hebung** — define a **new, highly specific skill + supporting
agents** that execute that (still-to-be-described) task **interactively, step by
step**, and **prepare the scripts** the skill needs (preferably **.NET single-file
applications** and **PowerShell** where possible).

## Status
- No NuGet Hebung in progress — this is meta work on the kit repo itself.
- Branch: `feature/agentic-coding-dogma` (clean tree at handoff). Not yet PR'd —
  user will say when to open a PR.
- Part (1) is complete and verified (smoke-check `SMOKE CHECK PASSED`).
- Part (2) is **not started** — the user will describe the real task in the next
  session, then we run brainstorming → spec → plan → execute (same loop as part 1).

## Done this session
- `docs/superpowers/specs/2026-06-30-agentic-coding-dogma-and-model-effort-design.md` — approved design (commit `11acb10`).
- `docs/superpowers/plans/2026-06-30-agentic-coding-dogma-and-model-effort.md` — implementation plan (`93a580c`, path-fix `d226b4c`).
- `docs/conventions/model-and-effort.md` — token-saving model + effort convention (`95739d3`).
- `docs/conventions/dogma.md` — consolidated dogma/workflow, conflict resolutions baked in (`4a54ef6`).
- `AGENTS.md` — rewritten lean contract linking both docs, extended persistence table (`d411775`).
- `.github/agents/*.agent.md`, `README.md`, `SETUP.md` — subagents retuned `claude-opus-4.8` → `claude-sonnet-4.6` + Opus-escalation notes; stale prose fixed (`be343fe`).

## In flight
- Nothing mid-edit. All work committed; tree clean.

## Key decisions
- **Layout:** lean `AGENTS.md` + two linked docs under `docs/conventions/`
  (`dogma.md`, `model-and-effort.md`) — no separate `docs/dogma/` folder.
- **Conflict resolutions (from sources, approved):** dropped APEX persona theater,
  dropped mandatory `[SF]`/`[DRY]` tagging, `docs/` is editable durable state,
  unit-tests-only (no integration tests), no `-y`/`-f`/`&&` (gated perms + PowerShell),
  `plan.md` is the one committed tracker (not `TASK_LIST.md`), learnings → commit + memory.
- **Models:** lowest-viable-model+effort, default Sonnet, escalate to Opus on
  evidence (failed attempt / 5+ files / architecture / security). Latest Sonnet is
  **4.6** (no Sonnet 4.8 exists); 4.8 is Opus-only. Effort stays **prose-guided**
  (Copilot exposes `--reasoning-effort` as a session CLI flag, but an agent-frontmatter
  `effort:` key is unconfirmed).

## Open questions / blockers
- **The real task is undefined** — user will describe it next session ("quite some
  info" incoming), plus extra information.
- **Model ID format:** repo uses dotted `claude-sonnet-4.6` / `claude-opus-4.8`;
  canonical Anthropic IDs are dashed (`claude-sonnet-4-6` / `claude-opus-4-8`).
  Kept dotted to match what the kit originally shipped. Switch to dashed only if
  Copilot CLI validates against canonical IDs (open question, not yet tested).

## Next concrete step
Listen to the user's full task description + extra info. Then invoke the
**brainstorming** skill to scope a **new, highly specific skill + supporting
agents** that drive the task **interactively, step by step**, plus the
**scripts** it needs (**.NET single-file apps** / **PowerShell** preferred).
Do not write code until a design is approved (brainstorming → spec → plan →
execute), per this repo's dogma.

## Pointers
- Operating contract: `AGENTS.md`
- Dogma + model/effort: `docs/conventions/dogma.md`, `docs/conventions/model-and-effort.md`
- This work's design + plan: `docs/superpowers/specs/2026-06-30-...-design.md`, `docs/superpowers/plans/2026-06-30-...md`
- Source inputs (boss + Obviousworks): `C:\Users\diener\Downloads\AGENTS.md`, `AGENTS (1).md`, `global_rules.md`, `master-prompt.txt`
- Token-economy guide distilled from: https://github.com/affaan-m/ECC/blob/main/the-longform-guide.md
- Verify the kit: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-check.ps1`
