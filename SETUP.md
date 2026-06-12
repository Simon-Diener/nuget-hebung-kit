# SETUP — make a repo GitHub Copilot CLI ready, then run a NuGet Hebung

A step-by-step guide for someone new to GitHub Copilot CLI. By the end you will
have a target repo that any teammate can clone, open in the CLI, and run
`/nuget-hebung` to drive a large, persisted, subagent-driven NuGet upgrade.

> You do **not** need Claude Code, the VS Code extension, or any "superpowers"
> plugin. Everything here is self-contained and runs in the Copilot **CLI**.

---

## 0. Prerequisites (once per machine)

1. **GitHub Copilot CLI.** Install and log in:
   ```bash
   npm install -g @github/copilot      # or your platform's installer
   copilot                             # then run /login and follow the prompts
   ```
   In the CLI you can confirm it works with `/version` and `/help`.
2. **.NET SDK** — needed by the upgrade itself: `dotnet --version`.
3. **A target repo** — the .NET solution you want to upgrade, as a local git repo.

---

## 1. Get this kit

Clone this kit next to your target repo (so the bootstrap script can copy from
it):

```bash
git clone <your-fork>/nuget-hebung-kit
```

Optional sanity check that the kit is intact:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File nuget-hebung-kit/scripts/smoke-check.ps1
# expect: SMOKE CHECK PASSED
```

---

## 2. Make your target repo CLI-ready

You have two options. **Option A is recommended.**

### Option A — automatic (one command)

From inside the kit, run the bootstrap script and point it at your repo:

```powershell
cd nuget-hebung-kit
./scripts/bootstrap.ps1 -TargetRepo C:\path\to\your-repo
```

It copies the skills, the model-pinned agents, the context-guard hook, and the
risk knowledge base into your repo; ensures an `AGENTS.md` exists; creates the
`docs/` persistence layout; adds `tasks/` to `.gitignore`; and grants the NuGet
Hebung subagent allow-list for your repo location in the Copilot CLI permissions
store (see [Permissions](#26-permissions-stop-the-subagents-re-prompting) below).
Existing files are kept unless you pass `-Force`. Pass `-SkipPermissions` to
leave the permissions store untouched.

### Option B — manual (understand each piece)

Copy these from the kit into the **same paths** in your target repo:

| Copy from kit | Into target | Why |
|---|---|---|
| `AGENTS.md` | `AGENTS.md` | Operating contract the CLI reads on every session. If you already have one, **merge** rather than overwrite. |
| `.github/skills/nuget-hebung/` | `.github/skills/nuget-hebung/` | The upgrade workflow (`/nuget-hebung`). |
| `.github/skills/handoff/` | `.github/skills/handoff/` | Session-state persistence. |
| `.github/agents/*.agent.md` | `.github/agents/` | The model-pinned investigator + updater subagents. |
| `.github/hooks/*` | `.github/hooks/` | `agentStop` context-guard → handoff nudge. |
| `docs/risks-nuget-hebung.md` | `docs/risks-nuget-hebung.md` | Risk KB the skill reads before planning. |

Then create empty `docs/handoffs/` and `docs/nuget-hebung/agentresults/`, and add
`tasks/` to `.gitignore`. Finally, grant the subagent allow-list for your repo
location (see the next section):

```powershell
./scripts/grant-permissions.ps1 -TargetRepo C:\path\to\your-repo
```

> **Where can the skills live?** The CLI auto-discovers `.github/skills/`,
> `.claude/skills/`, and `.agents/skills/`. This kit uses `.github/skills/`. If
> you also want the same skills in Claude Code, put them in `.claude/skills/`
> instead (the CLI reads that too).

Commit the result on a feature branch:

```bash
cd C:\path\to\your-repo
git checkout -b feature/nuget-hebung
git add -A
git commit -m "chore: install NuGet Hebung CLI kit"
```

---

## 2.6 Permissions: stop the subagents re-prompting

During a Hebung the investigator and updater subagents run the same handful of
commands over and over. Without an allow-list the CLI asks you to approve each
one every session, which stalls the parallel fan-out. The kit therefore records
a small, role-scoped allow-list for **your repo location** in the Copilot CLI
permissions store.

- **Where it is stored:** `~/.copilot/permissions-config.json` (or
  `$COPILOT_HOME/permissions-config.json`), keyed by the repo's absolute path.
  This is a **user-profile file, not part of the repo** — so the allow-list is
  granted per machine/location, never committed.
- **What is granted** (the union both subagents need; the store is per-location,
  not per-agent):
  - commands: `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet list`,
    `dotnet list package`, `dotnet package search`, `dotnet nuget`, `git add`,
    `git commit`, `Write-Output`, `Get-Content`, `Get-ChildItem`, `Select-String`
  - file writes (`.csproj`, `Directory.Packages.props`, reports, `plan.md`)
- **What is deliberately *not* granted** (stays gated behind a prompt):
  `git push`, branch operations, and anything off the list.

`bootstrap.ps1` runs this for you. To run, re-run, or apply it manually:

```powershell
./scripts/grant-permissions.ps1 -TargetRepo C:\path\to\your-repo
```

It is idempotent and merge-safe: re-running adds nothing twice, and it never
disturbs other locations or approvals you granted by hand. To **reset**, delete
your repo's entry from `permissions-config.json` while no CLI session is running
in that repo (or run `/reset-allowed-tools` inside the CLI for the live session).

> Identifiers follow `copilot help permissions`: command approval is on a
> first-level subcommand basis (e.g. `git push`, `dotnet build`), so the
> `dotnet` subcommands are listed explicitly.

---

## 3. Verify the CLI sees everything

Open the target repo in the CLI:

```bash
cd C:\path\to\your-repo
copilot
```

Then, inside the CLI:

- `/env` — confirms which instructions, skills, agents, and hooks are loaded.
- `/skills` — you should see `nuget-hebung` and `handoff`.
- `/agent` — you should see `nuget-project-investigator` and
  `nuget-package-updater` (these are delegate-only workers).

If a skill or agent is missing, re-check the paths in step 2 and restart the CLI.

---

## 4. Run the upgrade

In the CLI, in the target repo, simply run:

```text
/nuget-hebung
```

The skill drives eight phases and **talks to you** at the decision points:

| Phase | What happens | Your involvement |
|---|---|---|
| 0 Preflight | Branch + feed check; creates `docs/nuget-hebung/plan.md` | Provide the **NuGet feed / nuget.config** if missing |
| 1 Brainstorm | Scope questions | Answer: security vs feature? **TFM bumps** (e.g. net472→net8)? renames/migration map? exceptions/pins? |
| 2 Investigate | Parallel `nuget-project-investigator` subagents (claude-opus-4.8) write one report per project to `docs/nuget-hebung/agentresults/<projectId>/` | none (runs autonomously) |
| 3 Consolidate | Builds `dependency-graph.md` + `state-graph.md` | none |
| 4 Conflicts | Feedback report of conflicts/diamonds/NU1605 risks | Resolve the flagged conflicts |
| 5 Plan | Ordered, lane-based plan in `plan.md` | **Review and approve** before execution |
| 6 Execute | `nuget-package-updater` subagents run independent lanes in parallel; one bump per commit | none unless blocked |
| 7 Verify | restore + build + unit tests + vulnerability scan across the solution | Decide on any remaining blockers |

Everything durable is committed under `docs/nuget-hebung/`, so progress is never
lost.

---

## 5. Long sessions & resuming (automatic handoff)

Big upgrades can outlast one context window. This kit handles that:

- The **context-guard hook** watches session length and, when context looks
  tight, forces a turn that updates `docs/nuget-hebung/plan.md` and writes a
  handoff — then tells you to continue in a fresh session.
- To **resume**, start a new CLI session in the repo and type:
  ```text
  Read docs/nuget-hebung/plan.md and continue from Resume.
  ```
- You can also persist anytime by running `/handoff` manually.

That is the whole loop: **bootstrap → `/nuget-hebung` → answer the questions →
approve the plan → let it execute and verify**, resuming across sessions as
needed.
