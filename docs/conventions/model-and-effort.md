# Model & effort selection — the token-saving convention

> The load-bearing summary lives in `AGENTS.md` (**Model & effort selection**).
> This doc is the long-form version. It governs which model **and** which
> reasoning effort to use for a task across this repo's tooling (Claude Code,
> Copilot CLI), so the *lowest viable* model and effort is used by default.

## Principle

**Use the lowest viable model and the lowest viable effort for the task;
escalate only on evidence.** Cost scales with both axes, so neither is free.
Start low, and step up only when the work in front of you demonstrably needs it
(see *Upgrade criteria*) — not pre-emptively "to be safe".

## Model selection

| Task | Model | Rationale |
|---|---|---|
| Exploration / search | Haiku | Fast, cheap, good enough to find files |
| Trivial single-file edit | Haiku | Clear, contained change |
| Writing docs | Haiku | Structure is simple |
| Multi-file implementation | **Sonnet** | Best balance for coding — **the default** |
| PR / code review | Sonnet | Understands context, catches nuance |
| Complex architecture | Opus | Deep reasoning needed |
| Security analysis | Opus | Cannot afford to miss vulnerabilities |
| Hard debugging | Opus | Needs the whole system in mind |

**Default to Sonnet for ~90% of coding work.** Haiku is for genuinely simple,
contained work; Opus is reserved for the hard minority.

Model IDs in this repo's tooling: Haiku = `claude-haiku-4-5`,
Sonnet = `claude-sonnet-4.6`, Opus = `claude-opus-4.8`.

## Effort selection

This axis is **not** in the upstream guide — it is this repo's addition.
Pick the lowest effort that fits, on the same escalate-on-evidence rule.

| Effort | When |
|---|---|
| low | Mechanical: format, rename, a single trivial edit |
| medium | Standard implementation / investigation — the default |
| high | Architecture, conflict resolution, security-sensitive work |
| xhigh / max | Rare — whole-system debugging, deeply entangled reasoning |

(Claude Code exposes `low` / `medium` / `high` / `xhigh` / `max`. Copilot CLI
exposes a coarser "reasoning effort"; map to the nearest tier.)

## Upgrade criteria

Step **up** a model (typically Sonnet → Opus) when any of these hold:

- the first attempt **failed**;
- the task spans **5+ files**;
- it requires an **architectural decision**;
- it touches **security-critical** code.

Absent these, do not upgrade. The same evidence-first rule applies to effort:
raise it for genuine architecture/security/whole-system reasoning, not by habit.

## Context economy

Tokens spent on context are tokens not spent on the task. Keep context lean:

- **Prefer CLI + skills over heavy MCP servers.** Most platforms already ship a
  capable CLI; an MCP that merely wraps it is a standing context tax. A skill
  that shells out to the CLI is the cheaper equivalent.
- **Use session memory + handoff.** Persist durable state to `docs/` and snapshot
  with the [`handoff` skill](../../.github/skills/handoff/SKILL.md) rather than
  relying on in-context memory surviving a compaction.
- **Compact at logical intervals**, not only when forced — finish a unit of work,
  persist it, then compact.
- **Delegate fan-out to subagents** so the orchestrator's context stays clean;
  give each subagent only what its one task needs.

## Application to this kit

Concrete model + effort per role in a NuGet Hebung. The orchestrator picks per
this table and escalates per the *Upgrade criteria*.

| Role | Model | Effort |
|---|---|---|
| Orchestrator: synthesis, graphs, conflict resolution, planning | Opus | high |
| `nuget-project-investigator` | Sonnet (Opus for security-critical / large projects) | medium |
| `nuget-package-updater` | Sonnet (Opus on first-attempt failure / migration-heavy lane) | medium |
| `handoff` snapshot | Haiku | low |

Where the harness allows per-delegation model overrides, the orchestrator sets
the model per this table when dispatching. Where only the agent's frontmatter
`model` is settable, that holds the default and escalation happens by
re-dispatching on the higher model.

---

*Model table and context-economy principles distilled from the longform Claude
Code token-economy guide (`https://github.com/affaan-m/ECC/blob/main/the-longform-guide.md`).
The effort dimension and the per-kit application table are this repo's
additions.*
