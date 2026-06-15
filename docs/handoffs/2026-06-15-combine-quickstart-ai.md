# Handoff — combine quickstart_ai into nuget-hebung-kit — 2026-06-15

## Goal
Merge the useful contents of the sibling repo `C:\dev\Repos\quickstart_ai` into
this `nuget-hebung-kit`, deciding what combines into what. User will state their
vision + which skills/markdowns they want; this session did the research phase only.

## Status
- Research/comparison phase complete; presented a full comparison to the user.
- No NuGet Hebung in progress (this is a meta task on the kit repo itself).
- Branch: `feature/subagent-allowed-commands` (clean tree before this handoff).
- Awaiting the user's vision before any combining work begins. Nothing merged yet.

## Done this session
- Located the other repo: `C:\dev\Repos\quickstart_ai`.
- Read both repos' key files and produced a comparison (overlap, unique pieces,
  LLM-agent compatibility matrix). No files changed except this handoff.

## Key findings (so a fresh session need not re-derive)
- **Two generations of the same idea.** `nuget-hebung-kit` = newer, CLI-native,
  self-contained, deeper. `quickstart_ai` = older, VS-Code-first, superpowers-
  dependent, convention-rich.
- **Overlap (both):** `nuget-hebung` skill, `handoff` skill, context-guard hook,
  `docs/risks-nuget-hebung.md`, AGENTS.md, README, notebook.ipynb, smoke-check.ps1, LICENSE.
- **Maturity gap on shared pieces:** kit `nuget-hebung` SKILL = 246 lines / 8 phases /
  persisted `docs/nuget-hebung/` run folder / feed preflight / conflict resolution /
  migration maps; quickstart = 77 lines / 4 phases / superpowers-dependent /
  writes `docs/knowledge/`. kit handoff is Hebung-aware; kit risks KB 116 vs 82 lines.
- **Only in nuget-hebung-kit:** `refactor` skill; 2 custom agents
  (`nuget-project-investigator`, `nuget-package-updater`, pinned `claude-opus-4.8`);
  `scripts/bootstrap.ps1` + `grant-permissions.ps1`; `SETUP.md`; `CLAUDE.md` (@AGENTS.md bridge).
- **Only in quickstart_ai:** `setup` skill (provisions a TARGET repo: vendors
  superpowers, commits instructions + .vscode/settings.json, builds knowledge base);
  `.github/copilot-instructions.md` (VS Code "default-on" layer); `.vscode/settings.json`;
  `docs/conventions/{dogma,workflow,requirements}.md` (rich eng standards);
  `docs/claude-copilot-compatibility.md` (write-once-vs-duplicate reference);
  `docs/superpowers/{specs,plans}/` (design history); `PreCompact` handoff hook + hooks README.
- **Compatibility:** skills (`SKILL.md`) + `AGENTS.md` are the genuine write-once core
  (Copilot reads `.github/skills` AND `.claude/skills`; Claude only `.claude/skills`;
  Claude reads AGENTS.md only as fallback when no CLAUDE.md). Custom agents, hooks, and
  the VS Code default-on layer must be authored per tool.

## Key decisions
- (Proposed, NOT yet confirmed by user) Use the **kit as the base** and absorb
  quickstart_ai's standout unique assets: `docs/conventions/`, the compatibility doc,
  the `setup` skill, and the VS Code default-on layer. Await user confirmation.

## Open questions / blockers
- Awaiting the user's vision: which skills/markdowns to combine, and the target
  audience (CLI-only vs also VS Code / Claude Code). This decides whether to pull in
  quickstart's VS-Code-specific layer or stay CLI-native.

## Next concrete step
Read the user's stated vision, then propose an exact merge map (source file ->
destination in nuget-hebung-kit, with which version wins on overlaps) before editing.

## Pointers
- This repo: C:\dev\Repos\nuget-hebung-kit  (AGENTS.md = operating contract)
- Other repo: C:\dev\Repos\quickstart_ai
- Compatibility reference: C:\dev\Repos\quickstart_ai\docs\claude-copilot-compatibility.md
- Conventions to consider absorbing: C:\dev\Repos\quickstart_ai\docs\conventions\
