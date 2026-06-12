---
name: refactor
description: 'Use when restructuring existing code without changing its behavior — extracting functions/modules, renaming, removing duplication or dead code, splitting a god class, decoupling, or tidying a messy area before a feature. Drives a persisted, safety-net-first workflow: establish a green build + test baseline, lock behavior with characterization tests where coverage is thin, then apply small reversible moves one at a time, building and testing (and committing) after each. For a large surface it fans out read-only survey subagents that write findings to docs/refactor/findings/. Self-contained, needs no external skills. Not for behavior changes, bug fixes, or new features — those are separate tasks.'
---

# Refactor — restructure code without changing behavior

You are the **orchestrator** of a behavior-preserving refactor. The contract is
simple and absolute: **the observable behavior of the code does not change.**
Tests are your seatbelt; small reversible steps are your brakes. If you cannot
prove behavior is preserved at each step, you are not refactoring — stop.

You own one durable artifact, the run folder `docs/refactor/`, and move the work
through the phases below. Persist progress to `docs/refactor/plan.md` after every
phase so a fresh session can resume. If context gets tight, invoke the **handoff**
skill (it updates the plan's Resume section) and continue in a new session.

## Iron rules

1. **No behavior change.** No bug fixes, no new features, no API changes unless
   the user explicitly scoped them. If you spot a bug, write it down in the plan;
   do not fix it inside the refactor.
2. **Never refactor on red.** The build and the relevant tests must be green
   *before* you touch anything and *after every step*. A red baseline is a
   blocker — surface it and stop.
3. **One move at a time.** Each step is a single, named refactoring (extract,
   rename, inline, move, dedup…) that is independently reversible and committable.
4. **Verify, then commit.** Build + run the relevant tests after each move; commit
   that one move with a clear message before starting the next.
5. **Coverage first.** If the area you are changing is not covered by tests, add
   *characterization tests* (which pin current behavior, including current quirks)
   **before** restructuring. No safety net → build one or narrow the scope.

## Phase 0 — Preflight

- Create/confirm a feature branch (e.g. `refactor/<area>`); never work on `main`.
- Discover the stack: build tool, test runner, lint/format command. Record the
  exact commands you will use to build and test.
- **Establish the baseline:** run the build and the test suite. Capture the result
  (counts, duration). If anything is red or the project doesn't build, stop and
  report — you cannot refactor against a broken baseline.
- Create `docs/refactor/` and write the initial `plan.md` (template at the bottom).
- Commit the (empty) run folder so the baton exists from the start.

## Phase 1 — Scope & intent (brainstorm with the user)

Confirm before touching code. Ask only what you can't safely infer:

- **Goal / smell:** what is wrong and what does "better" look like? (readability,
  duplication, a god class/function, tangled dependencies, dead code, naming,
  testability before an upcoming feature…)
- **Boundary:** exact files/modules/folders in scope — and explicitly out of scope.
- **Behavior contract:** is the public API/interface frozen, or may signatures
  change internally? Any consumers outside this repo?
- **Definition of done:** the concrete end state and how you'll demonstrate it.
- **Constraints:** formatting/lint rules, perf-sensitive paths, anything not to touch.

Record the answers in `plan.md`. Do not invent scope creep.

## Phase 2 — Survey

Map the target before changing it.

- **Small scope (a few files):** read them yourself; list call sites, dependencies,
  and existing test coverage for the area.
- **Large scope (many files/modules):** fan out **read-only** survey subagents in
  parallel, one per module/cluster, each writing
  `docs/refactor/findings/<area>/report.md` with: responsibilities, public surface,
  callers/callees, duplication, dead code, current test coverage, and the specific
  smells + suggested moves. Investigation only — survey subagents never edit code.

Consolidate findings into the plan: the concrete list of candidate moves and the
**risk/coverage map** (which areas have tests, which need characterization tests).

## Phase 3 — Safety net

For every area you intend to change that lacks adequate coverage, add
**characterization tests** that capture *current* behavior (warts included) — they
are the oracle proving your refactor changed nothing. Run them; they must pass
against the unchanged code. Commit the new tests separately
(`test: characterize <area> before refactor`). If a behavior is too hard to pin
with a test, shrink the refactor scope to what you *can* protect.

## Phase 4 — Plan (ordered moves, user-approved)

Turn the candidate moves into an **ordered sequence of small steps**, each:
the refactoring name + the precise edit, the verify command, and a one-line commit
message. Order for safety: lowest-risk and dependency-free moves first; group moves
that share files so they don't fight. Note any move that crosses a public boundary
and re-confirm it's allowed. **Get the user's approval of the ordered plan before
executing.**

## Phase 5 — Execute (one move at a time)

For each step, in order:

1. Apply the single named refactoring — nothing else. Prefer IDE/tool-assisted
   refactors (rename, extract) over hand edits when available; they're safer.
2. Run the relevant tests + build. **Green:** commit that one move with its planned
   message. **Red:** revert or fix immediately — never stack a second move on red.
3. Tick the step in `plan.md`.

Disjoint, file-independent steps may be parallelized with subagents, but anything
touching shared files stays serialized. Re-run the area's tests after a merge of
parallel work.

## Phase 6 — Verify & wrap up

- Run the **full** build, the **full** test suite, and lint/format. All green.
- Review the cumulative diff: confirm it is structure-only — no behavior, no API,
  no dependency changes slipped in. Public surface unchanged (or only as approved).
- Confirm Definition of Done from Phase 1 is met.
- Summarize for the user: what was restructured, why it's safer/cleaner, the
  before/after of any metrics you tracked (e.g. duplication removed, function size),
  and any bugs you *found but deliberately did not fix* (with locations).

## Run folder

| File | Purpose |
| --- | --- |
| `docs/refactor/plan.md` | The baton: scope, decisions, ordered steps, Resume line. Updated every phase. |
| `docs/refactor/findings/<area>/report.md` | Per-area survey output (large scope only). |

## plan.md template

```markdown
# Refactor — <area> — plan

## Goal & smell
<what's wrong, what "better" looks like>

## Scope
- In:  <files/modules>
- Out: <explicitly excluded>

## Behavior contract
<API frozen? internal-only changes allowed? external consumers?>

## Definition of done
<concrete end state + how it's demonstrated>

## Baseline (Phase 0)
- Build: <command> -> <green/red, when>
- Tests: <command> -> <N passed, duration, when>

## Coverage / risk map (Phase 2)
- <area> — <covered | needs characterization test>

## Ordered steps (Phase 4)
- [ ] 1. <refactoring name> — <precise edit> — verify: <cmd> — commit: "<msg>"
- [ ] 2. ...

## Decisions
- <decision + reason>

## Bugs found (NOT fixed here)
- <location + description>

## Resume
<the single next action a fresh session should take>
```

When context is tight or a session ends, run **handoff** to persist state, then
resume in a fresh session with: *"Read docs/refactor/plan.md and continue from
Resume."*
