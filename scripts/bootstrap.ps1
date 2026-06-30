<#
.SYNOPSIS
  Make a target repository "GitHub Copilot CLI ready" for a NuGet Hebung or a
  PS5 -> PS6 migration.

.DESCRIPTION
  Copies this kit's skills, custom agents, hooks, and risk knowledge base into a
  target repo, ensures an AGENTS.md operating contract exists, creates the
  persistence layout, and grants the NuGet Hebung subagent allow-list for the
  target location in your Copilot CLI permissions store (so the subagents stop
  re-prompting for routine restore/build/test/search + git add/commit + file
  writes; git push stays gated). After running, open the target repo, start
  `copilot`, and run /nuget-hebung. Idempotent: existing files are not
  overwritten unless -Force.

.PARAMETER TargetRepo
  Absolute path to the repository you want to prepare.

.PARAMETER Force
  Overwrite existing kit files in the target (skills/agents/hooks/risk KB).

.PARAMETER SkipPermissions
  Do not touch the Copilot CLI permissions store (skip granting the allow-list).

.EXAMPLE
  ./scripts/bootstrap.ps1 -TargetRepo C:\dev\Repos\nuget-hebung-demo
#>
param(
    [Parameter(Mandatory = $true)] [string] $TargetRepo,
    [switch] $Force,
    [switch] $SkipPermissions,
    [switch] $SkipToolBuild
)

$ErrorActionPreference = 'Stop'
$Kit = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path $TargetRepo)) { throw "Target repo not found: $TargetRepo" }
$TargetRepo = (Resolve-Path $TargetRepo).Path
Write-Host "Kit:    $Kit"
Write-Host "Target: $TargetRepo`n"

function Copy-Tree($relSource, $relDest) {
    $src = Join-Path $Kit $relSource
    $dst = Join-Path $TargetRepo $relDest
    if (-not (Test-Path $src)) { Write-Warning "skip (missing in kit): $relSource"; return }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
    if ((Test-Path $dst) -and -not $Force) {
        Write-Host "exists (kept): $relDest  [use -Force to overwrite]"
    } else {
        Copy-Item -Recurse -Force $src $dst
        Write-Host "copied: $relDest"
    }
}

function Copy-File($relSource, $relDest) {
    $src = Join-Path $Kit $relSource
    $dst = Join-Path $TargetRepo $relDest
    if (-not (Test-Path $src)) { Write-Warning "skip (missing in kit): $relSource"; return }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
    if ((Test-Path $dst) -and -not $Force) {
        Write-Host "exists (kept): $relDest  [use -Force to overwrite]"
    } else {
        Copy-Item -Force $src $dst
        Write-Host "copied: $relDest"
    }
}

# 1. Skills (the workflows + handoff).
Copy-Tree '.github/skills/nuget-hebung' '.github/skills/nuget-hebung'
Copy-Tree '.github/skills/ps5-to-ps6'   '.github/skills/ps5-to-ps6'
Copy-Tree '.github/skills/handoff'      '.github/skills/handoff'

# 2. Custom agents (model-pinned subagents).
Copy-File '.github/agents/nuget-project-investigator.agent.md' '.github/agents/nuget-project-investigator.agent.md'
Copy-File '.github/agents/nuget-package-updater.agent.md'      '.github/agents/nuget-package-updater.agent.md'
Copy-File '.github/agents/ps5-to-ps6-investigator.agent.md'    '.github/agents/ps5-to-ps6-investigator.agent.md'
Copy-File '.github/agents/ps5-to-ps6-migrator.agent.md'        '.github/agents/ps5-to-ps6-migrator.agent.md'

# 3. Hooks (context-guard -> handoff nudge).
Copy-File '.github/hooks/hooks.json'        '.github/hooks/hooks.json'
Copy-File '.github/hooks/context-guard.ps1' '.github/hooks/context-guard.ps1'
Copy-File '.github/hooks/context-guard.sh'  '.github/hooks/context-guard.sh'

# 4. Risk knowledge base.
Copy-File 'docs/risks-nuget-hebung.md' 'docs/risks-nuget-hebung.md'

# 4b. PS5 -> PS6 migration kit: the transform KB, plus the single-file tools
#     (published fresh here, then copied so the target is self-contained and
#     needs no build of its own). -SkipToolBuild reuses an existing dist/.
Copy-File 'docs/ps5-to-ps6/migration-kb.md' 'docs/ps5-to-ps6/migration-kb.md'

$distRoot = Join-Path $Kit 'tools/ps5to6/dist'
if ($SkipToolBuild) {
    Write-Host "skipped: publishing ps5to6 tools  [will copy existing dist if present]"
} else {
    $ps6ToolProjects = @(
        'tools/ps5to6/src/Snapshot/Snapshot.csproj',
        'tools/ps5to6/src/UninstallAll/UninstallAll.csproj',
        'tools/ps5to6/src/FeedProbe/FeedProbe.csproj',
        'tools/ps5to6/src/ScaffoldProject/ScaffoldProject.csproj',
        'tools/ps5to6/src/Report/Report.csproj'
    )
    foreach ($proj in $ps6ToolProjects) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($proj)
        $out  = Join-Path $distRoot $name
        Write-Host "publishing: $name -> tools/ps5to6/dist/$name"
        & dotnet publish (Join-Path $Kit $proj) -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $out | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $proj (exit $LASTEXITCODE)" }
    }
}
if (Test-Path $distRoot) {
    Copy-Tree 'tools/ps5to6/dist' 'tools/ps5to6/dist'
} else {
    Write-Warning "tools/ps5to6/dist not found (and -SkipToolBuild was set) -- target will lack the ps5to6 tools; re-run without -SkipToolBuild."
}

# 5. AGENTS.md operating contract (only if the target has none).
$targetAgents = Join-Path $TargetRepo 'AGENTS.md'
if ((Test-Path $targetAgents) -and -not $Force) {
    Write-Host "exists (kept): AGENTS.md  [merge the kit's AGENTS.md manually if needed]"
} else {
    Copy-Item -Force (Join-Path $Kit 'AGENTS.md') $targetAgents
    Write-Host "copied: AGENTS.md"
}

# 6. Persistence layout + gitignore for scratch.
foreach ($d in @('docs/handoffs', 'docs/nuget-hebung/agentresults', 'docs/ps5-to-ps6/agentresults')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $TargetRepo $d) | Out-Null
}
Write-Host "created: docs/handoffs, docs/nuget-hebung/agentresults, docs/ps5-to-ps6/agentresults"

$gi = Join-Path $TargetRepo '.gitignore'
$giLine = 'tasks/'
if (-not (Test-Path $gi) -or -not (Select-String -Path $gi -SimpleMatch $giLine -Quiet)) {
    Add-Content -Path $gi -Value "`n# Copilot scratch (context-guard state, throwaway notes)`ntasks/"
    Write-Host "updated: .gitignore (+ tasks/)"
}

# 7. Grant the NuGet Hebung subagent allow-list for the target location, so the
#    subagents stop re-prompting for routine restore/build/test/search + git
#    add/commit + file writes. git push / branch ops stay gated. Skip with
#    -SkipPermissions. This writes to your user-level permissions store
#    (~/.copilot/permissions-config.json or $COPILOT_HOME), not into the repo.
if ($SkipPermissions) {
    Write-Host "skipped: permissions allow-list  [run scripts/grant-permissions.ps1 later, or omit -SkipPermissions]"
} else {
    & (Join-Path $PSScriptRoot 'grant-permissions.ps1') -TargetRepo $TargetRepo
}

Write-Host "`nDone. Next steps:"
Write-Host "  1. cd `"$TargetRepo`""
Write-Host "  2. git checkout -b feature/nuget-hebung"
Write-Host "  3. git add -A; git commit -m `"chore: install NuGet Hebung CLI kit`""
Write-Host "  4. Start the CLI:  copilot"
Write-Host "  5. In the CLI, run:  /nuget-hebung  (NuGet upgrade)  -or-  /ps5-to-ps6  (PS5->PS6 migration)"
