# PS5→PS6 Skill, Agents, KB & Kit Wiring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the `ps5-to-ps6` orchestrator skill, its two supporting agents, the migration knowledge base, and wire the kit (smoke-check, grant-permissions, AGENTS/README/SETUP, and the `bootstrap.ps1` installer) so a target solution can be set up self-contained.

**Architecture:** Markdown skill/agent/KB docs under `.github/` and `docs/ps5-to-ps6/`, plus PowerShell glue. The orchestrator skill drives the persisted, bottom-up, partial-success migration using the Plan-A tools (`ps5to6-snapshot`, `ps5to6-uninstall-all`, `ps5to6-feed-probe`, `ps5to6-scaffold-project`, `ps5to6-report`) and the two agents. `bootstrap.ps1` ships the pre-published single-file exes so the target needs no build.

**Tech Stack:** Markdown (SKILL/agent/KB), PowerShell 5.1 (`bootstrap.ps1`, `grant-permissions.ps1`, `smoke-check.ps1`), the Plan-A .NET tools.

## Global Constraints

- **Depends on Plan A** (`2026-06-30-ps5-to-ps6-tooling.md`) being complete. Consumes the tool assembly names: `ps5to6-snapshot`, `ps5to6-uninstall-all`, `ps5to6-feed-probe`, `ps5to6-scaffold-project`, `ps5to6-report`.
- Output language: English only.
- Run folder for a live migration: `docs/ps5-to-ps6/` in the TARGET solution (the skill creates it; do not create run artifacts in this kit repo).
- Agents default to `claude-sonnet-4.6`; Opus escalation per `docs/conventions/model-and-effort.md`.
- Never commit directly to a protected branch. Conventional Commits, one logical change each.
- PowerShell edits: keep Windows PowerShell 5.1 compatible (no `&&`, no ternary). Match the existing style of `bootstrap.ps1`.
- Do NOT edit silently-loaded contract files beyond what each task specifies.

---

### Task 1: Migration knowledge base — `docs/ps5-to-ps6/migration-kb.md`

**Files:**
- Create: `docs/ps5-to-ps6/migration-kb.md`

**Interfaces:**
- Consumes: nothing.
- Produces: the durable reference consulted by the skill and both agents. Its section anchors are referenced by the SKILL and agents: `## Package rename map`, `## Required & optional packages per project type`, `## Config-file transforms`, `## Source-code breaking changes`.

- [ ] **Step 1: Create the KB with the exact distilled content below**

`docs/ps5-to-ps6/migration-kb.md`:
````markdown
# PS5 → PS6 migration knowledge base

> Distilled from the product team's PS6 release notes ("6.0.0") and the official
> "Adjustments" migration checklist (valid up to PS5 5.4.0.392). The orchestrator
> and the `ps5-to-ps6-*` agents consult this before touching a project.
> **Core idea:** PS6 targets .NET 8 (PS5 targeted .NET Framework 4.7.1). Migrate
> projects to **SDK-style with `PackageReference`**, uninstall all previous
> packages/references, then install the PS6 package set. .NET 8 loads transitive
> dependencies automatically — do NOT re-add a package just because PS5 listed it;
> add back only what the build proves is missing AND that has a net8 build.

## Per-project target framework

| Project role | TargetFramework |
|---|---|
| Service | `net8.0` |
| RichClient | `net8.0-windows` |
| PublishingService | `net8.0` |
| Configuration | `net8.0` |

## Package rename map (old → new)

| Old | New |
|---|---|
| `LicenseTool` | `Noxum.PS5.SoftDevTools.LicenseTool` |
| `PS5WinClient` | `Noxum.PS5.Application.RichClient` |
| `Noxum.IDML.*` (writers/readers/etc.) | `Noxum.Publishing.*` (`*.Cmd` for command-line writers) |
| `Noxum.Publishing.HtmlWriter.IdmlToHtmlConverter` | `Noxum.Publishing.HtmlWriter.IpubToHtmlConverter` |
| `Noxum.Publishing.Core.IdmlExtension` | `Noxum.Publishing.Core.IpubExtension` |
| XML namespace `ipub:IdmlExtension` | `ipub:IpubExtension` |
| `Noxum.IDML.PdfWriter.exe` | `Noxum.Publishing.FoWriter.Cmd.dll` + AntennaHouse wrapper (see config transforms) |

## Required & optional packages per project type

**Service** (`net8.0`) — required: `Noxum.PS5.Service` 5.4.0+, `Noxum.PS5.Server` 5.4.0+,
`Noxum.PS5.Publishing.PS5Transformer` 5.4.0+, `Noxum.PS5.WindowsService.BinaryServiceModule` 5.4.0+,
`Noxum.PS5.Types` 2.0.0+, `Noxum.Publishing.Core` 2.1+, `Noxum.Publishing.HtmlWriter` 2.1+,
`Noxum.Publishing.ImageConverter` 1.4.1+, `Noxum.Publishing.Wrappers.*` 1.4+, `Noxum.Storage.Messaging.Mail` 1.1.0+.
Optional: `Noxum.PS5.Editors.ObjectDataExport.Server` 3.0.0+, `Noxum.PS5.Editors.ObjectDataImport.Server` 3.0.0+,
`Noxum.Editing.ImageLabelingEditor.PS5AuxProcessor` 2.0.0+, `Noxum.PS5.Extensions.ConditionalContent.Server` 2.0.0+,
`Noxum.PS5.Reports.*` 2.0.0+, `Noxum.PS5.Workflow.ActivityWorkflowManager` 2.0.0+,
`Noxum.PS5.Workflow.ActivityWorkflowConfiguration` 2.0.0+, project-specific server-side packages.

**RichClient** (`net8.0-windows`) — required: `Noxum.PS5.Application.RichClient` 5.4.0+, `Noxum.PS5.Client` 5.4.0+,
`Noxum.PS5.Win32` 5.4.0+, `Noxum.PS5.Types.DragDrop` 2.0.0+, `Noxum.PS5.Types.UITypeEditors` 2.0.0+,
`Noxum.Publishing.XamlWriter` 2.0.0+, `Noxum.Icons.Desktop.Controls` 1.1.2+, `Noxum.Localization.Controls` 1.1.0+,
`ActiproSoftware.Controls.WinForms.SyntaxEditor.Addons.XML` 23.1.3.
Optional: the `Noxum.PS5.Editors.*` client/editor set 2.0.0+/3.0.0+, `Noxum.Editing.*` editors,
`Noxum.PS5.Extensions.ConditionalContent.Client` 2.0.0+, `Noxum.PS5.Extensions.ContentChecker.Control` 2.0.0+,
project-specific client-side packages.

**PublishingService** (`net8.0`) — required: `Noxum.Services.Publishing.Service` 3.0.1+,
`Noxum.Publishing.ParameterExtractor` 2.0.0+, `Noxum.Compression.Zip` 2.0.0+, `Noxum.Publishing.Core` 2.1+,
`Noxum.Publishing.*Writer.Cmd` / `*Reader.Cmd` / `*ToStyle.Cmd` 2.1+, `Noxum.Publishing.XmlValidator.Cmd` 2.1+,
`Noxum.Publishing.XslTransformer.Cmd` 2.1+. Optional: `Noxum.Publishing.ObjectData.*` 2.0.1+.

**Configuration** (`net8.0`) — required: `Noxum.PS5.Configuration` 5.4.0+.
Optional: the `Noxum.PS5.Editors.*.Configuration(s)` set 2.0.0+/3.0.0+, `Noxum.Editing.*.Configuration(s)`,
`Noxum.Publishing.ObjectData.Schema` 2.0.1+.

## Config-file transforms

- **App settings:** replace `PS5Service.exe.config` → `appsettings.json`; `PS5WinClient.exe.config` → `PS5WinClient.dll.config`;
  `Noxum.PS5.Publishing.PS5Transformer.exe.config` → `appsettings.json` (ConnectionStrings only). PublishingService 3.0.1 needs its own `appsettings.json`.
- **GlobalDefinitions.config:** raise `globalDefinitions/@version` to `6.0.0`.
- **InfoProviderDefinitionList.config:** set `PS5.TopicTypeIcon` type to `Noxum.PS5.Controls.PSUIInfoProvider, Noxum.PS5.Controls`.
- **PublisherDefinitionList.config:** remove `server`, `tcpPort`, `endpoint` attributes (moved to appsettings.json).
- **XSLT:** remove `msxsl:script` blocks (call an extension function instead); replace external-URI `document()` with `nxps:ResolveConfiguration(...)`; replace `assembly=mscorlib` with `assembly=System.Private.CoreLib` (not needed in XAML/RESX).
- **Publishing framework references:** replace all `Noxum.IDML.*` type strings with `Noxum.Publishing.*`; `IdmlToHtmlConverter`→`IpubToHtmlConverter`; `IdmlExtension`→`IpubExtension`; `ipub:IdmlExtension`→`ipub:IpubExtension`.
- **PublishingService.config:** validate against schema; set `remotingtcpport="-1"`; replace `Noxum.Publishing.Wrappers.*.exe`→`.dll` and `Noxum.Publishing.ObjectData.*.exe`→`.dll` (also in ParameterExtractor `-executable` args); replace `Noxum.IDML.PdfWriter.exe` with `Noxum.Publishing.FoWriter.Cmd.dll` followed by a `Noxum.Publishing.Wrappers.AntennaHouse.dll` executable element; replace remaining `Noxum.IDML.*.exe` with the equivalent `Noxum.Publishing.*.Cmd.dll`.

## Source-code breaking changes

- **`Thread.Abort()` removed.** Threads can no longer be aborted in .NET 8. Convert cooperative cancellation via `CancellationToken` for subclasses of:
  - `Noxum.PS5.Server.Jobs.ServerJob` (async server jobs) — honor cancellation, stop reliably within timeout.
  - `Noxum.PS5.Server.Data.PreparationModule` — same, in the `Execute` override.
  - `Noxum.PS5.Server.ServiceModules.ServiceModuleBase` and the `Noxum.PS5.WindowsService.BinaryServiceModule` base classes (`PreviewConverterBase`, `BinaryMetaCollectorBase`, `BinaryAuxiliaryProcessorBase`) — Task/CancellationToken lifecycle; finish within the stop timeout.
- **`msxsl:script` removed** in XSL transforms (see config transforms — move to extension functions).
- **Removed controls/types:** `Noxum.PS5.Controls.BinaryImportWPF`, `Noxum.PS5.Windows.Controls.BinaryImport`, `Noxum.PS5.Windows.Controls.WorkflowStatusInfo`, the `WpfConverter` preview converter.
- **Removed capabilities:** Windows Event Log support; multiple configurations ("Mandators"); multiple user sessions; custom `InProcServer_HttpServiceModuleType`.
````

- [ ] **Step 2: Commit**

```bash
git add docs/ps5-to-ps6/migration-kb.md
git commit -m "docs(ps5-to-ps6): add PS5->PS6 migration knowledge base"
```

---

### Task 2: Orchestrator skill — `.github/skills/ps5-to-ps6/SKILL.md`

**Files:**
- Create: `.github/skills/ps5-to-ps6/SKILL.md`

**Interfaces:**
- Consumes: the Plan-A tools (by assembly name), the KB (Task 1), both agents (Task 3).
- Produces: the user-invocable `ps5-to-ps6` skill. Defines the run folder layout and the `plan.md` + `steps.md` templates the agents and a resumed session rely on.

- [ ] **Step 1: Create the skill**

Author `.github/skills/ps5-to-ps6/SKILL.md` with YAML frontmatter (`name: ps5-to-ps6`, a `description` that triggers on "PS5 to PS6 / Publishing Studio migration / raise a PS5 solution to PS6 / net8") and these sections, modeled on the existing `nuget-hebung` SKILL (same persisted-state + handoff discipline):

1. **Operating rules** (verbatim discipline): persisted state in `docs/ps5-to-ps6/plan.md` is the single source of truth, updated+committed every phase; context-limit handoff (update `## Resume`, run `handoff`, commit); delegate fan-out to the two agents, keep synthesis; evidence before claims; git safety (feature branch, atomic commits, never to protected); allowed commands pre-approved by `grant-permissions.ps1`.
2. **Tools** — the five SFAs and what each does (point to `tools/ps5to6/dist/` exes installed by bootstrap, or `dotnet run` in dev). Read the KB (`docs/ps5-to-ps6/migration-kb.md`) before Phase 4.
3. **Phases** (announce each):
   - **Phase 0 Preflight** — feature branch; `dotnet --version`; confirm the Noxum feed is in `nuget.config` (STOP and ask if missing); confirm the `ps5to6-*` tools are runnable; create `docs/ps5-to-ps6/` + `agentresults/`; seed `plan.md`; commit.
   - **Phase 1 Snapshot** — run `ps5to6-snapshot <root> docs/ps5-to-ps6`; dispatch `ps5-to-ps6-investigator` to cross-check the largest/most-complex projects; commit.
   - **Phase 2 Uninstall-all** — run `ps5to6-uninstall-all <root> --apply`; build is expected to break (baseline); commit the clean baseline.
   - **Phase 3 Order** — read `inventory.json` bottom-up order into `dependency-graph.md`.
   - **Phase 4 Per-project loop (bottom-up)** — for each project: classify role (Service/RichClient/PublishingService/Configuration/custom); run `ps5to6-feed-probe` over the KB package set; `ps5to6-scaffold-project` for known roles; `dotnet add package` the confirmed set (apply rename map); `dotnet build`. If clean AND dependency-only → record + commit, no agent. Otherwise dispatch `ps5-to-ps6-migrator` to resolve transitive gaps / apply KB code+config fixes. Keep pulling net8-available missing deps until the project builds or a hard gap is reached → record blocker, move on (partial success). Persist safe mappings to `mappings.md`; append the per-step record to `steps.md`; append a row to `cost-ledger.md`; commit per project; update `## Resume`; hand off if context tight.
   - **Phase 5 Report** — run `ps5to6-report <runStatusJson> docs/ps5-to-ps6/report.md`; summarize to the user.
4. **Per-step minimal feedback** — the exact `steps.md` block format (see template below); keep phrasing minimal (works / doesn't / why / do).
5. **Cost ledger** — append per project: `project | phase | model | effort | #subagent-dispatches | wall-clock | token-bucket`.
6. **Run folder table** and the `plan.md` / `steps.md` templates.

Include this `steps.md` entry template verbatim in the skill:
```markdown
### <projectId> — ✅ raised | ⚠️ partial | ⛔ blocked
- Works: <what built/installed>
- Doesn't: <what failed / what's missing>
- Why: <root cause, one line>
- Do: <recommendation>
```

Include a `plan.md` template with `## Status`, `## Scope`, `## Projects` (checklist), `## Execution order`, `## Decisions`, `## Resume` — mirroring the `nuget-hebung` plan template but for this workflow.

- [ ] **Step 2: Commit**

```bash
git add .github/skills/ps5-to-ps6/SKILL.md
git commit -m "feat(skill): add ps5-to-ps6 migration orchestrator skill"
```

---

### Task 3: Supporting agents

**Files:**
- Create: `.github/agents/ps5-to-ps6-investigator.agent.md`
- Create: `.github/agents/ps5-to-ps6-migrator.agent.md`

**Interfaces:**
- Consumes: KB (Task 1), the tools.
- Produces: two custom agents, both `model: claude-sonnet-4.6`, `userInvocable: false`, with the Opus-escalation note. Modeled on the existing `nuget-project-investigator` / `nuget-package-updater` agent files.

- [ ] **Step 1: Create the investigator agent**

`.github/agents/ps5-to-ps6-investigator.agent.md` — frontmatter (`name`, `description`, `model: claude-sonnet-4.6`, `userInvocable: false`). Body: read-only role; cross-checks one project's snapshot classification + dependency capture against the actual files; greps for code-level breaking-change sites listed in the KB (`Thread.Abort`, `msxsl:script`, `document(`, `mscorlib`, subclasses of `ServerJob`/`PreparationModule`/`ServiceModuleBase`/BinaryService bases, `Noxum.IDML.*`). Writes `docs/ps5-to-ps6/agentresults/<projectId>/report.md`. Hard rule: read-only — never edits project/source files. Allowed commands: read-only `dotnet list`/`restore`/`package search`, search tools, write only its report.

- [ ] **Step 2: Create the migrator agent**

`.github/agents/ps5-to-ps6-migrator.agent.md` — frontmatter (`model: claude-sonnet-4.6`, escalate to Opus on first-attempt failure or migration-heavy/code-heavy project). Body: read-write; executes ONE project's migration loop when the scripted scaffold+build is insufficient — resolve transitive gaps (`dotnet add package` only net8-available, confirmed via feed-probe), apply KB code + config transforms, keep building until green or a hard gap; record the per-step `steps.md` block and persist safe mappings. Hard rules: stay in the assigned project; one logical commit; never re-add a package without a net8 build; surface ambiguous gaps to the orchestrator rather than guessing.

- [ ] **Step 3: Commit**

```bash
git add .github/agents/ps5-to-ps6-investigator.agent.md .github/agents/ps5-to-ps6-migrator.agent.md
git commit -m "feat(agents): add ps5-to-ps6 investigator and migrator subagents"
```

---

### Task 4: Permissions + smoke-check

**Files:**
- Modify: `scripts/grant-permissions.ps1`
- Modify: `scripts/smoke-check.ps1`

**Interfaces:**
- Consumes: the new skill/agents/tools file paths.
- Produces: pre-approved commands for the migration; smoke-check coverage of the new kit.

- [ ] **Step 1: Read both scripts to learn their existing patterns**

Run: open `scripts/grant-permissions.ps1` and `scripts/smoke-check.ps1`; identify how the allow-list entries and presence-assertions are expressed.

- [ ] **Step 2: Add the new allow-list entries to `grant-permissions.ps1`**

Add (matching the existing entry style): `dotnet publish`, `dotnet add package`, `dotnet remove package`, and invocation of the published tools `tools/ps5to6/dist/ps5to6-*`. Keep `git push` / branch ops gated (do not add them).

- [ ] **Step 3: Add presence assertions to `smoke-check.ps1`**

Assert existence of: `.github/skills/ps5-to-ps6/SKILL.md`, `.github/agents/ps5-to-ps6-investigator.agent.md`, `.github/agents/ps5-to-ps6-migrator.agent.md`, `docs/ps5-to-ps6/migration-kb.md`, `tools/ps5to6/Ps5To6.Tools.sln`. Follow the existing assertion/`SMOKE CHECK PASSED` pattern.

- [ ] **Step 4: Run smoke-check**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-check.ps1`
Expected: `SMOKE CHECK PASSED`.

- [ ] **Step 5: Commit**

```bash
git add scripts/grant-permissions.ps1 scripts/smoke-check.ps1
git commit -m "chore(kit): grant ps5-to-ps6 commands and smoke-check the new kit"
```

---

### Task 5: Contract docs — AGENTS.md, README, SETUP

**Files:**
- Modify: `AGENTS.md`
- Modify: `README.md` (if present)
- Modify: `SETUP.md` (if present)

**Interfaces:**
- Consumes: all prior tasks.
- Produces: the operating contract reflects the new skill, agents, KB, run folder, and per-step feedback convention.

- [ ] **Step 1: Update `AGENTS.md`**

- Skill-first rule: add a line — for a PS5→PS6 product migration, use the `ps5-to-ps6` skill.
- Subagents section: add `ps5-to-ps6-investigator` and `ps5-to-ps6-migrator` (default sonnet, Opus escalation).
- Persistence table: add `docs/ps5-to-ps6/` (run folder), `docs/ps5-to-ps6/migration-kb.md`, and `tools/ps5to6/` (the SFA tooling).
- Layout: add `.github/skills/ps5-to-ps6`, the two agents, and `tools/ps5to6/`.
- Add a one-line pointer to the per-step minimal-feedback convention (works/doesn't/why/do), per the user's steer that this be recorded in skill or AGENTS.md.

- [ ] **Step 2: Update `README.md` / `SETUP.md`** (only the sections that enumerate skills/agents/tooling; if a file is absent, skip it).

- [ ] **Step 3: Commit**

```bash
git add AGENTS.md README.md SETUP.md
git commit -m "docs(contract): register ps5-to-ps6 skill, agents, tooling, and run folder"
```

---

### Task 6: Extend `bootstrap.ps1` (the setup script — FINAL step)

**Files:**
- Modify: `scripts/bootstrap.ps1`
- Create (build artifact, git-ignored in tools but shipped to target): publish step producing `tools/ps5to6/dist/*`

**Interfaces:**
- Consumes: everything above.
- Produces: `bootstrap.ps1` installs the full PS5→PS6 kit into a target solution self-contained.

- [ ] **Step 1: Publish the single-file tools to a dist folder**

Add a helper in `bootstrap.ps1` (or a referenced step) that runs, for each tool, `dotnet publish` to `tools/ps5to6/dist/<tool>/` with `-c Release -p:PublishSingleFile=true --self-contained false -r win-x64`. (Note: `tools/ps5to6/.gitignore` ignores `dist/`; the kit publishes on demand. Decide per spec §8: publish during bootstrap so the dist is fresh.) Run:
```bash
dotnet publish tools/ps5to6/src/Snapshot/Snapshot.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o tools/ps5to6/dist/snapshot
```
(repeat for UninstallAll, FeedProbe, ScaffoldProject, Report).

- [ ] **Step 2: Extend the copy steps in `bootstrap.ps1`**

After the existing nuget-hebung copies, add (using the existing `Copy-Tree`/`Copy-File` helpers):
```powershell
# PS5 -> PS6 migration kit.
Copy-Tree '.github/skills/ps5-to-ps6' '.github/skills/ps5-to-ps6'
Copy-File '.github/agents/ps5-to-ps6-investigator.agent.md' '.github/agents/ps5-to-ps6-investigator.agent.md'
Copy-File '.github/agents/ps5-to-ps6-migrator.agent.md'     '.github/agents/ps5-to-ps6-migrator.agent.md'
Copy-File 'docs/ps5-to-ps6/migration-kb.md' 'docs/ps5-to-ps6/migration-kb.md'
Copy-Tree 'tools/ps5to6/dist' 'tools/ps5to6/dist'   # pre-published single-file exes
```
Add `docs/ps5-to-ps6/agentresults` to the persistence-layout `foreach` directory list. Update the "Next steps" footer to mention `/ps5-to-ps6`.

- [ ] **Step 3: Smoke-test bootstrap into a throwaway target**

Run (use the scratchpad as a throwaway target):
```bash
mkdir -p "$TEMP/ps6-bootstrap-test" && powershell -NoProfile -ExecutionPolicy Bypass -File scripts/bootstrap.ps1 -TargetRepo "$TEMP/ps6-bootstrap-test" -SkipPermissions
```
Expected: copies the ps5-to-ps6 skill, both agents, the KB, and `tools/ps5to6/dist` into the target; prints the updated next-steps footer; no errors.

- [ ] **Step 4: Commit**

```bash
git add scripts/bootstrap.ps1
git commit -m "feat(bootstrap): install the ps5-to-ps6 kit (skill, agents, KB, published tools) into target"
```

---

## Self-Review

**Spec coverage:**
- §7 migration KB → Task 1 (content embedded verbatim). ✓
- §1/§4 orchestrator skill + phases → Task 2. ✓
- §3 investigator + migrator agents → Task 3. ✓
- §5 per-step minimal feedback (steps.md template) → Task 2 + AGENTS pointer in Task 5. ✓
- §6 run folder layout → Task 2 (defined in skill). ✓
- §8 smoke-check + grant-permissions → Task 4. ✓
- §8 AGENTS/README/SETUP → Task 5. ✓
- §8 bootstrap.ps1 extension + pre-published dist (final step) → Task 6. ✓
- Cost ledger → Task 2 (defined in skill). ✓

**Placeholder scan:** the KB content is fully embedded (no TBD). The SKILL/agent bodies are specified by required sections + exact templates and the data they must use (the KB) rather than full verbatim prose, because their content is reference/instructional text the implementer composes from the embedded KB; the load-bearing fixed artifacts (steps.md template, plan.md sections, phase list, allow-list entries, copy lines, publish commands) are given verbatim. No "implement later" steps remain.

**Type/name consistency:** tool assembly names (`ps5to6-snapshot`, `ps5to6-uninstall-all`, `ps5to6-feed-probe`, `ps5to6-scaffold-project`, `ps5to6-report`) match Plan A. Run-folder filenames (`inventory.json`, `dependency-graph.md`, `steps.md`, `mappings.md`, `gaps.md`, `cost-ledger.md`, `report.md`) match spec §6. Agent file names match the skill's dispatch references.
