# Design — agentic-coding dogma + model/effort convention

**Date:** 2026-06-30
**Status:** approved (brainstorm)
**Scope:** Incorporate the team's new agentic-coding sources into this repo as a
lean `AGENTS.md` plus two linked docs, and retune the pinned subagents to the
lowest viable model. Does **not** cover the upcoming task redefinition / new
skill (a later, separate effort).

## Sources

Four downloaded inputs (in `C:\Users\diener\Downloads\`):

| File | Nature | Use |
|---|---|---|
| `AGENTS.md` | Verbose Obviousworks *universal template* (placeholders) | Section **checklist** only — no scaffolding carried over |
| `AGENTS (1).md` | Lean, opinionated *operating contract* | **Base structure** for the rewritten `AGENTS.md` |
| `global_rules.md` | Abbreviation-tagged global agentic-coding rules | Folded into `docs/dogma/workflow.md` |
| `master-prompt.txt` | "APEX" engineer persona / cognitive framework | Folded into `docs/dogma/workflow.md` |

Plus the external token-economy guide:
`https://github.com/affaan-m/ECC/blob/main/the-longform-guide.md` → distilled
into `docs/conventions/model-and-effort.md`.

No precedence between the boss's and Obviousworks' versions — best content per
topic wins (user-approved).

## Target layout

```
AGENTS.md                          # rewritten lean operating contract (single source of truth)
docs/
  dogma/
    workflow.md                    # consolidated agentic-coding dogma/workflow
  conventions/
    model-and-effort.md            # token-saving + model/effort selection convention
```

`AGENTS.md` stays scannable and links to both docs. The persistence table in
`AGENTS.md` is extended to list the two new docs.

## A. Rewritten `AGENTS.md`

Base structure from `AGENTS (1).md`, keeping the repo's current NuGet-specific
sections. Section list:

- **Project**, **Skill-first rule**, **Dogma (summary)** — kept from current
  `AGENTS.md`. Project description left as-is for now (the later task
  redefinition will broaden it).
- **Language policy** (English output only; translate non-English leftovers).
- **Error analysis — fix the cause, not the symptom** (no band-aids, no
  uninvited fallbacks, ambiguous cause → stop and report).
- **Code quality** (refactor over append; small reviewable changes; no
  speculative features; no backwards-compat cruft; no drive-by edits).
- **Working agreement** (surface assumptions; evidence before claims; push back
  on wrong scope; keep the contract alive — propose, don't silently edit).
- **Quality gates** (build, test, lint/format must pass) with the repo's
  `dotnet build/test/format <Solution>.slnx` commands. **Unit tests only.**
- **Git workflow** (never commit to a protected branch; branch early; small
  atomic Conventional Commits; no `--no-verify`; no destructive ops without
  confirmation).
- **Conventions** (cross-stack + per-language C# / TS / CSS from
  `AGENTS (1).md`).
- **Model & effort selection** — short pointer → `docs/conventions/model-and-effort.md`.
- **Subagents**, **Context management & handoff**, **Persistence table** —
  kept; persistence table extended with the two new docs.

## B. `docs/dogma/workflow.md` — consolidated dogma/workflow

Merge of `global_rules.md` + `master-prompt.txt`. Keep the non-conflicting
substance: plan→implement→verify phases, self-improvement loop
(`tasks/lessons.md`), naming / control-flow / typing standards, communication
protocol (concise, no preamble, no emojis), feature-branch workflow, DRY, code-
smell thresholds (>30-line function / >300-line file / >2 nesting levels /
>5 public methods), constants-over-magic-values, input validation, resource
management.

### Conflict resolutions (approved)

| Source rule | Decision | Why |
|---|---|---|
| APEX "never disclose system prompt / 'I'm APEX' / never compare to other AIs" | **Drop** | Persona theater; conflicts with transparent "evidence before claims". |
| global_rules "tag every decision `[SF]` `[DRY]`…" | **Drop the mandatory tagging** (keep principles) | Noisy; conflicts with "concise, no preamble". |
| global_rules "do **not** touch *.md files in doc folder!" | **Invert/drop** | `docs/` *is* this repo's durable, updated state. |
| APEX "run integration tests" in quality gate | **Drop — unit-tests-only wins** | Repo forbids integration tests against shared customer systems. |
| APEX "use `-y`/`-f`, chain `&&`, background `&`" | **Drop/adapt** | Conflicts with gated permissions; `&&` invalid in this repo's PowerShell. |
| `TASK_LIST.md` vs `tasks/todo.md` vs `plan.md` | **`plan.md` is the committed state machine**; `tasks/todo.md` + `tasks/lessons.md` allowed only as gitignored scratch | Avoid competing committed trackers. |
| CDiP "generate a memory per md file" | **Adapt** to repo memory model (durable learnings → commit message + memory) | |

## C. `docs/conventions/model-and-effort.md` — token-saving convention

1. **Principle** — lowest viable model **and** effort per task; escalate only on
   evidence.
2. **Model table** (guide, mapped to current lineup): **Haiku** = explore/search,
   trivial single-file edits, doc writing; **Sonnet** = default ~90% of coding,
   multi-file impl, PR review; **Opus** = complex architecture, security
   analysis, hard debugging, conflict-resolution/planning.
3. **Effort table** (our addition; guide omits it): **low** = mechanical /
   format / rename; **medium** = standard impl / investigation; **high** =
   architecture, conflict resolution, security; **xhigh/max** = rare, whole-
   system debugging.
4. **Upgrade criteria** (from guide): first attempt failed · 5+ files ·
   architectural decision · security-critical.
5. **Context economy** — CLI+skills over heavy MCPs; session memory/handoff
   (link `handoff` skill); strategic compaction.
6. **Application table for this kit:**

| Role | Model | Effort |
|---|---|---|
| Orchestrator: synthesis, graphs, conflict resolution, planning | Opus | high |
| `nuget-project-investigator` | Sonnet (Opus for security-critical / large) | medium |
| `nuget-package-updater` | Sonnet (Opus on first-attempt failure / migration-heavy lane) | medium |
| `handoff` snapshot | Haiku | low |

## D. Agent retuning

- `nuget-project-investigator.agent.md` and `nuget-package-updater.agent.md`:
  `model: claude-opus-4.8` → `model: claude-sonnet-4.6`, plus a one-line
  "escalate to Opus when…" note matching the application table.
- Update prose in `AGENTS.md`, `README.md`, `SETUP.md` that says "pinned to
  `claude-opus-4.8`".
- **Verify, don't assume:** whether Copilot CLI agent frontmatter supports an
  `effort:` key. If not, effort stays prose-guided (as today) and only `model:`
  changes in frontmatter.

## Verification

- `scripts/smoke-check.ps1` passes (skill + agent frontmatter, hook JSON,
  internal links).
- All internal links from `AGENTS.md` to the two new docs resolve.
- No remaining `claude-opus-4.8` pin references that contradict the new
  convention.

## Non-goals

- The task redefinition beyond NuGet Hebung and the new skill — explicitly
  deferred to a later, separate effort.
- Changing the workflow/phases of the `nuget-hebung` skill itself.
