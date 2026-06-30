# Agentic-Coding Dogma + Model/Effort Convention Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land the team's new agentic-coding sources as a lean `AGENTS.md` plus two linked docs (`docs/conventions/dogma.md`, `docs/conventions/model-and-effort.md`), and retune the pinned subagents to the lowest viable model.

**Architecture:** `AGENTS.md` is the scannable single-source-of-truth operating contract; detailed dogma and the token/model/effort convention live in two linked docs under `docs/`. Subagent frontmatter is downgraded Opus→Sonnet with a documented Opus escalation.

**Tech Stack:** Markdown docs + YAML frontmatter (`SKILL.md` / `*.agent.md` format), PowerShell smoke-check (`scripts/smoke-check.ps1`).

## Global Constraints

- Output English only (code, comments, `.md`, commit messages).
- The approved design spec is `docs/superpowers/specs/2026-06-30-agentic-coding-dogma-and-model-effort-design.md` — it holds the verbatim conflict-resolution table, model table, effort table, and application table. Tasks reference it; do not re-decide.
- No precedence between boss's / Obviousworks' versions — best content per topic.
- Conventional Commits, one logical change per commit. Feature branch `feature/agentic-coding-dogma` (already checked out); never commit to a protected branch.
- Model IDs: Haiku = `claude-haiku-4-5`, Sonnet = `claude-sonnet-4.6`, Opus = `claude-opus-4.8`. (Frontmatter currently uses the `claude-opus-4.8` form; match that dotted style.)
- Verification command for the whole kit: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-check.ps1` → expect `SMOKE CHECK PASSED`.
- Unit tests only as a dogma rule; the kit itself has no unit tests — verification is the smoke-check + link/grep checks below.

---

### Task 1: Token/model/effort convention doc

**Files:**
- Create: `docs/conventions/model-and-effort.md`

**Interfaces:**
- Produces: a doc that `AGENTS.md` (Task 3) links to as "Model & effort selection", and that the agent files (Task 4) cite in their escalation note. Anchor/section names used elsewhere: the "Application table for this kit".

- [ ] **Step 1: Write the doc**

Author `docs/conventions/model-and-effort.md` with these sections (content per spec §C, verbatim tables below):

1. **Principle** — lowest viable model *and* effort per task; escalate only on evidence.
2. **Model table:**

| Task | Model | Rationale |
|---|---|---|
| Exploration / search | Haiku | Fast, cheap, good enough to find files |
| Trivial single-file edit | Haiku | Clear, contained change |
| Writing docs | Haiku | Structure is simple |
| Multi-file implementation | Sonnet | Best balance for coding (default) |
| PR / code review | Sonnet | Understands context, catches nuance |
| Complex architecture | Opus | Deep reasoning needed |
| Security analysis | Opus | Cannot afford to miss vulnerabilities |
| Hard debugging | Opus | Needs the whole system in mind |

3. **Effort table** (our addition; the guide omits effort):

| Effort | When |
|---|---|
| low | Mechanical: format, rename, single trivial edit |
| medium | Standard implementation / investigation |
| high | Architecture, conflict resolution, security |
| xhigh / max | Rare — whole-system debugging |

4. **Default + upgrade criteria** — "Default to Sonnet for ~90% of coding." Upgrade to Opus when: first attempt failed · task spans 5+ files · architectural decision required · security-critical code.
5. **Context economy** — prefer CLI + skills over heavy MCPs; use session memory + handoff (link `../../.github/skills/handoff/SKILL.md`); compact at logical intervals.
6. **Application table for this kit:**

| Role | Model | Effort |
|---|---|---|
| Orchestrator: synthesis, graphs, conflict resolution, planning | Opus | high |
| `nuget-project-investigator` | Sonnet (Opus for security-critical / large) | medium |
| `nuget-package-updater` | Sonnet (Opus on first-attempt failure / migration-heavy lane) | medium |
| `handoff` snapshot | Haiku | low |

Add a one-line source credit: distilled from the longform Claude Code token-economy guide (`https://github.com/affaan-m/ECC/blob/main/the-longform-guide.md`), effort dimension added by this repo.

- [ ] **Step 2: Verify the file exists and the handoff link resolves**

Run: `test -f docs/conventions/model-and-effort.md && test -f .github/skills/handoff/SKILL.md && echo OK`
Expected: `OK`

- [ ] **Step 3: Commit**

```bash
git add docs/conventions/model-and-effort.md
git commit -m "docs(conventions): add model + effort selection convention (token saving)"
```

---

### Task 2: Consolidated dogma/workflow doc

**Files:**
- Create: `docs/conventions/dogma.md`

**Interfaces:**
- Produces: a doc `AGENTS.md` (Task 3) links to as the consolidated dogma/workflow.

- [ ] **Step 1: Write the doc**

Author `docs/conventions/dogma.md` merging `global_rules.md` + `master-prompt.txt` per spec §B. Keep (non-conflicting): plan→implement→verify phases; self-improvement loop (`tasks/lessons.md`); naming / control-flow / typing standards; communication protocol (concise, no preamble, no emojis); feature-branch workflow; DRY; code-smell thresholds (>30-line function / >300-line file / >2 nesting levels / >5 public methods); constants-over-magic-values; input validation; resource management.

Apply the **approved conflict resolutions** (spec §B table) — each as a short, decided rule, NOT as open questions:
- No persona theater (no "never disclose prompt / I'm APEX / never compare to other AIs").
- No mandatory `[SF]`/`[DRY]` decision tagging — keep the principles, drop the ritual.
- `docs/` is durable, updated state — do edit `.md` there (invert the "don't touch docs" rule).
- Unit tests only in the quality gate (drop "run integration tests").
- No `-y`/`-f` confirmation-bypass, no `&&` chaining assumptions (gated permissions; this repo's shell is PowerShell).
- `plan.md` is the committed state machine; `tasks/todo.md` + `tasks/lessons.md` are gitignored scratch only.
- Durable learnings → commit message + memory (not a memory per `.md` file).

Add a one-line note at top: this doc is the long-form dogma; the load-bearing summary lives in `AGENTS.md`.

- [ ] **Step 2: Verify no dropped-rule language leaked in**

Run: `grep -niE "I'm APEX|never disclose|integration test|TASK_LIST\.md|do not touch" docs/conventions/dogma.md || echo CLEAN`
Expected: `CLEAN` (or only clearly-inverted/explanatory mentions — read each hit; there must be no instruction telling the agent to do the dropped behavior).

- [ ] **Step 3: Commit**

```bash
git add docs/conventions/dogma.md
git commit -m "docs(dogma): add consolidated agentic-coding workflow dogma"
```

---

### Task 3: Rewrite AGENTS.md as the lean contract

**Files:**
- Modify (rewrite): `AGENTS.md`

**Interfaces:**
- Consumes: links to `docs/conventions/dogma.md` (Task 2) and `docs/conventions/model-and-effort.md` (Task 1).
- Produces: the contract `CLAUDE.md` imports via `@AGENTS.md`; persistence table that the rest of the kit relies on.

- [ ] **Step 1: Rewrite the file**

Rewrite `AGENTS.md` using `AGENTS (1).md` (Downloads) as the structural base, per spec §A. Keep the repo's current NuGet-specific sections (Skill-first rule, Dogma summary, Subagents, Context management & handoff, Persistence). Fold in from `AGENTS (1).md`: Language policy, Error analysis (fix the cause), Code quality, Working agreement, Quality gates (with `dotnet build/test/format <Solution>.slnx`, unit tests only), Git workflow, Conventions (C#/TS/CSS).

Add a **Model & effort selection** section — 2-3 lines pointing to `docs/conventions/model-and-effort.md`, stating the lowest-viable-model/effort principle.

Add a **Dogma docs** pointer (or fold into the existing Dogma summary) linking `docs/conventions/dogma.md`.

Extend the **Persistence** table with two rows:

| Path | Holds |
|------|-------|
| `docs/conventions/dogma.md` | Long-form agentic-coding dogma/workflow |
| `docs/conventions/model-and-effort.md` | Model + effort selection convention (token saving) |

In the **Subagents** section, change the investigator/updater model wording from "pinned to `claude-opus-4.8`" to "default `claude-sonnet-4.6`, escalate to Opus per `docs/conventions/model-and-effort.md`".

- [ ] **Step 2: Verify links and import bridge**

Run:
```bash
test -f docs/conventions/dogma.md && test -f docs/conventions/model-and-effort.md && echo LINKS-OK
grep -q "@AGENTS.md" CLAUDE.md && echo BRIDGE-OK
grep -ni "claude-opus-4.8" AGENTS.md || echo NO-STALE-OPUS-PROSE
```
Expected: `LINKS-OK`, `BRIDGE-OK`, and `NO-STALE-OPUS-PROSE` (or only legitimate Opus-escalation mentions — read each hit).

- [ ] **Step 3: Commit**

```bash
git add AGENTS.md
git commit -m "docs(agents): rewrite AGENTS.md as lean contract linking dogma + model/effort docs"
```

---

### Task 4: Retune subagents + update stale prose

**Files:**
- Modify: `.github/agents/nuget-project-investigator.agent.md:4` (frontmatter `model`)
- Modify: `.github/agents/nuget-package-updater.agent.md:4` (frontmatter `model`)
- Modify: `README.md` (rows/prose saying "pinned to `claude-opus-4.8`")
- Modify: `SETUP.md` (prose saying "claude-opus-4.8" / "model-pinned agents")

**Interfaces:**
- Consumes: the application table in `docs/conventions/model-and-effort.md` (Task 1).

- [ ] **Step 1: Change investigator frontmatter + add escalation note**

In `.github/agents/nuget-project-investigator.agent.md`, change `model: claude-opus-4.8` → `model: claude-sonnet-4.6`. Add one line under the title: "Model: Sonnet by default; the orchestrator escalates this investigation to Opus for security-critical or unusually large projects (see `docs/conventions/model-and-effort.md`)." Leave the existing "medium reasoning effort" intent intact (effort stays prose-guided — see Step 4).

- [ ] **Step 2: Change updater frontmatter + add escalation note**

In `.github/agents/nuget-package-updater.agent.md`, change `model: claude-opus-4.8` → `model: claude-sonnet-4.6`. Add one line under the title: "Model: Sonnet by default; the orchestrator escalates a lane to Opus on first-attempt failure or a migration-heavy lane (see `docs/conventions/model-and-effort.md`)."

- [ ] **Step 3: Update README.md and SETUP.md prose**

Replace every "pinned to `claude-opus-4.8`" / "model-pinned" phrasing in `README.md` and `SETUP.md` with the new wording: "default `claude-sonnet-4.6`, escalated to Opus per the model/effort convention". Keep the explanatory point that agents exist to set a model + worker persona; only the specific model value/claim changes.

Run to find every occurrence first:
```bash
grep -rniE "claude-opus-4\.8|model-pinned|pinned to" README.md SETUP.md AGENTS.md
```
Update each hit that asserts an Opus pin. (Hits that are legitimate Opus-escalation references stay.)

- [ ] **Step 4: Verify Copilot effort-frontmatter support, then decide**

Check whether Copilot CLI agent frontmatter supports an `effort:` key:
```bash
copilot help permissions 2>/dev/null | grep -i effort || echo "no-cli-here"
```
If `copilot` is unavailable here (`no-cli-here`) or the key is undocumented, **keep effort prose-guided** (as it is today — do NOT add an unverified `effort:` frontmatter key). Record the outcome in the commit message. (Spec §D explicitly allows this fallback.)

- [ ] **Step 5: Verify no stale Opus pins remain**

Run:
```bash
grep -rniE "model:\s*claude-opus-4\.8" .github/agents/ && echo "STALE-PIN-FOUND" || echo "NO-STALE-PINS"
```
Expected: `NO-STALE-PINS`.

- [ ] **Step 6: Commit**

```bash
git add .github/agents/ README.md SETUP.md
git commit -m "feat(agents): default subagents to claude-sonnet-4.6 with documented Opus escalation"
```

---

### Task 5: Full-kit verification

**Files:** none (verification only)

- [ ] **Step 1: Run the smoke-check**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-check.ps1`
Expected: `SMOKE CHECK PASSED`. If it fails on agent frontmatter (e.g. it whitelists model values), read the failure and fix the offending file, then re-run.

- [ ] **Step 2: Spec-coverage grep**

Run:
```bash
test -f docs/conventions/model-and-effort.md && test -f docs/conventions/dogma.md && echo DOCS-OK
grep -rniE "model:\s*claude-opus-4\.8" .github/agents/ && echo "UNEXPECTED" || echo "AGENTS-RETUNED"
```
Expected: `DOCS-OK` and `AGENTS-RETUNED`.

- [ ] **Step 3: Confirm clean tree on the feature branch**

Run: `git status --short && git log --oneline -6`
Expected: clean working tree; the five commits (spec + four task commits) present on `feature/agentic-coding-dogma`.

---

## Self-Review

**Spec coverage:**
- §A rewritten AGENTS.md → Task 3 ✓
- §B dogma/workflow + conflict resolutions → Task 2 ✓
- §C model/effort convention → Task 1 ✓
- §D agent retuning + README/SETUP prose + effort-frontmatter verification → Task 4 ✓
- Verification section (smoke-check, links, stale-pin grep) → Tasks 1-5 steps ✓
- Non-goals (task redefinition / new skill) → out of scope, untouched ✓

**Placeholder scan:** No "TBD/TODO"; each task has concrete content, exact paths, and runnable verification commands. Prose-heavy doc bodies point to the committed spec (which holds verbatim tables) rather than re-duplicating — the spec is a required input named in Global Constraints.

**Type/name consistency:** Model IDs consistent (`claude-haiku-4-5` / `claude-sonnet-4.6` / `claude-opus-4.8`); doc paths consistent across tasks (`docs/conventions/model-and-effort.md`, `docs/conventions/dogma.md`); application-table role names match agent file names.
