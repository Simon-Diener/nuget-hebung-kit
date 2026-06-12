---
name: handoff
description: Use when a session is getting long, context feels tight, the model is losing the thread, or a session is ending — captures durable session state into docs/handoffs/ (and, during a NuGet Hebung, updates docs/nuget-hebung/plan.md's Resume section) so a fresh session resumes cleanly. The context-guard hook nudges this automatically past a session-length threshold; you can also run it manually anytime.
---

# Handoff — persist session state before context is lost

Long sessions drift: the agent forgets what matters and quality drops. This skill
snapshots everything needed to resume in a new session. Keep it tight and factual
— it is a baton pass, not a report.

## When to run

- The `context-guard` hook injected a "write a handoff" instruction.
- You notice you are re-reading things you already knew, or losing the thread.
- The user is wrapping up or asks to pause.
- Before a deliberate fresh-session restart on a long task.

## What to write

1. **If a NuGet Hebung is in progress:** first update
   `docs/nuget-hebung/plan.md` — the current phase, any new decisions, ticked-off
   steps, and a precise `## Resume` line. The plan doc is the primary baton for
   that workflow; the handoff below is the general snapshot.
2. Create `docs/handoffs/YYYY-MM-DD-<short-topic>.md` (today's date; update it if
   one already exists for this topic/day). Fill every section concretely — no
   placeholders:

```markdown
# Handoff — <topic> — <YYYY-MM-DD>

## Goal
<one or two sentences: what we are ultimately trying to achieve>

## Status
<where we are right now, 2-4 bullets>

## Done this session
- <committed/verified work, with commit SHAs or file paths>

## In flight
- <work started but not finished; exact state>

## Key decisions
- <decision + the reason, so it isn't re-litigated>

## Open questions / blockers
- <anything awaiting a user answer or external input>

## Next concrete step
<the single next action a fresh agent should take — file, command, or task>

## Pointers
- Active plan: docs/nuget-hebung/plan.md  (if a Hebung is running)
- Other: docs/<...>
```

## After writing

1. Commit (`docs(handoff): snapshot session state`).
2. Tell the user it is written and recommend continuing in a **fresh session**
   that opens with: *"Read docs/handoffs/<file> (and docs/nuget-hebung/plan.md if
   present) and continue from Next step."*
3. On resume, the handoff + plan are enough to rebuild the thread — do not
   re-derive what they already record.
