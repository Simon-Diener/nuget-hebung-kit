# Agentic-coding dogma & workflow

> This is the long-form dogma; the load-bearing summary lives in `AGENTS.md`
> (**Dogma**). It consolidates the team's global agentic-coding rules and the
> engineering-workflow prompt into one reference. `AGENTS.md` wins on any
> conflict — it is the operating contract.

## Core principles

- **Problem clarity first.** No code without a clear problem statement. If the
  request is ambiguous, ask — do not guess.
- **Simplicity first.** Choose the simplest viable solution; complex patterns
  need an explicit reason. Get it correct and working before abstracting.
- **Readability priority.** Code must be immediately understandable by the next
  human or agent who touches it.
- **Dependency minimalism.** No new library or framework without a request or a
  compelling, stated justification.
- **Industry-standard adherence.** Follow the established conventions of the
  language and stack; match the surrounding code.
- **Strategic documentation.** Comment the non-obvious *why*, not the obvious
  *what*. Write docs in English; rewrite non-English docs you find into English.
- **Test-driven thinking.** Design every unit to be easily testable from the
  start.

## Workflow

Plan before code: **Understand → Plan → Implement → Verify → Complete.**

1. **Understand.** Read the relevant files, search patterns, map dependencies.
   Parallelise independent reads. Locate every change point before editing.
2. **Plan.** For any non-trivial task (3+ steps or an architectural decision),
   write the plan and get it approved before implementing. Name 3–5 edge cases
   the plan must cover.
3. **Implement.** Apply changes atomically — one file/concern at a time. Write
   tests first for new behaviour and bug fixes (TDD where it applies).
4. **Verify.** Run build / lint / tests immediately after changes (see *Quality
   gates* in `AGENTS.md`). If something goes sideways, **stop and re-plan** —
   do not keep pushing. Cap blind retries (≈3) before stepping back.
5. **Complete.** Report what changed, the test status, and a "try it" command.
   List deferred work as explicit next steps. Then stop — no filler follow-ups.

### Feature-branch flow

- A dedicated branch per feature/task (`feature/...` or `task/...`), branched
  before the first edit lands. Never commit to a protected branch.
- All work and tests complete on the branch; open a PR to the integration
  branch; wait for review before merge.

## Code quality

- **DRY.** No duplicate logic — reuse or extend what exists.
- **Clean architecture.** Cleanly formatted, logically structured, consistent
  patterns. Refactor over append; prefer simplifying to adding when fixing.
- **Robust error handling at real boundaries.** Validate external input; handle
  edge cases. Do not bury defects in swallowing try/catch or uninvited
  fallbacks (see *Error analysis* in `AGENTS.md`).
- **Code-smell thresholds — refactor when exceeded:** functions > 30 lines,
  files > 300 lines, nesting > 2 levels, classes with > 5 public methods.

### Naming, control flow, typing

- **Names carry meaning.** Functions are verbs (`generateDateString`), variables
  are nouns (`numSuccessfulRequests`). Avoid cryptic abbreviations (`genYmdStr`,
  `n`, `resMs`).
- **Flat control flow.** Guard clauses and early returns; handle errors first;
  max nesting depth 3. Never catch an error without meaningful handling.
- **Explicit typing on public boundaries.** Annotate function signatures and
  public APIs; avoid `any` and unchecked casts.
- **Constants over magic values.** Name every magic string/number.

## Self-improvement loop

After any correction from the user, capture the pattern in `tasks/lessons.md`
(gitignored scratch) as a rule that prevents the same mistake recurring. Review
it at the start of work in a repo. Durable, cross-cutting learnings graduate
from scratch into the commit message (the *why*) and memory — and, if they are a
standing rule, are *proposed* for `AGENTS.md` (the user approves; never edit it
silently). Do **not** create a memory per `.md` file.

## Communication

- **Concise and direct.** 1–4 lines unless complexity demands more. Answer "is
  11 prime?" with "Yes".
- **No preamble**, no "I'll do X / doing X / done with X" narration, no
  unnecessary follow-up offers, no emojis.
- **Action over explanation** — for "fix the login bug": read → fix → test →
  report the one-line result.
- **Markdown**: backticks for file/function names, language-tagged code blocks,
  bullets for lists.
- **Scale explanation depth** to complexity; offer alternatives with brief
  pros/cons when genuinely relevant; say so plainly when a request exceeds the
  available context or capability.

## Task tracking & persistence

- The committed **state machine is `docs/nuget-hebung/plan.md`** (per the
  `nuget-hebung` skill) — not a `TASK_LIST.md`. There is one tracker, and it is
  committed so a fresh session rebuilds context from files alone.
- `tasks/` (incl. `tasks/todo.md`, `tasks/lessons.md`) is **gitignored scratch**
  — useful within a session, never the source of truth.
- `docs/` is durable, committed state: keep the relevant `.md` files current as
  work progresses. (Editing docs is expected — they are the persistence layer.)

## Tooling & safety

- **Use the file/search tools**, not shell `cat`/`grep`/`echo`, for reading,
  editing, and searching. Absolute paths.
- **Respect gated permissions.** Do not bypass confirmations with `-y`/`-f`, and
  do not assume `&&` chaining — this repo's shell is PowerShell. Explain a
  modifying command before running it.
- **No secrets in source.** Use the platform mechanism (User Secrets, env vars,
  secret store).
- **No malicious or destructive commands**; no git history rewrites or force
  pushes without explicit confirmation.
