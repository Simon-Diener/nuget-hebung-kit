@AGENTS.md

<!--
This repo's operating contract lives in AGENTS.md (read natively by Copilot CLI).
Claude Code auto-loads CLAUDE.md, so this one-line import bridges it in.

CLI-specific surfaces are NOT portable to Claude Code as-is:
- Skills (.github/skills/) — Claude reads .claude/skills/. Copy or symlink if you
  also use Claude Code.
- Custom agents (.github/agents/*.agent.md) — Claude uses .claude/agents/*.md with
  different frontmatter. Re-author if needed.
- Hooks (.github/hooks/hooks.json, agentStop) — Claude uses .claude/settings.json
  with different event names. Re-author if needed.
-->
