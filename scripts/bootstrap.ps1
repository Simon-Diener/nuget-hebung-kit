<#
.SYNOPSIS
  Make a target repository "GitHub Copilot CLI ready" for a NuGet Hebung.

.DESCRIPTION
  Copies this kit's skills, custom agents, hooks, and risk knowledge base into a
  target repo, ensures an AGENTS.md operating contract exists, and creates the
  persistence layout. After running, open the target repo, start `copilot`, and
  run /nuget-hebung. Idempotent: existing files are not overwritten unless -Force.

.PARAMETER TargetRepo
  Absolute path to the repository you want to prepare.

.PARAMETER Force
  Overwrite existing kit files in the target (skills/agents/hooks/risk KB).

.EXAMPLE
  ./scripts/bootstrap.ps1 -TargetRepo C:\dev\Repos\nuget-hebung-demo
#>
param(
    [Parameter(Mandatory = $true)] [string] $TargetRepo,
    [switch] $Force
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

# 1. Skills (the workflow + handoff).
Copy-Tree '.github/skills/nuget-hebung' '.github/skills/nuget-hebung'
Copy-Tree '.github/skills/handoff'      '.github/skills/handoff'

# 2. Custom agents (model-pinned subagents).
Copy-File '.github/agents/nuget-project-investigator.agent.md' '.github/agents/nuget-project-investigator.agent.md'
Copy-File '.github/agents/nuget-package-updater.agent.md'      '.github/agents/nuget-package-updater.agent.md'

# 3. Hooks (context-guard -> handoff nudge).
Copy-File '.github/hooks/hooks.json'        '.github/hooks/hooks.json'
Copy-File '.github/hooks/context-guard.ps1' '.github/hooks/context-guard.ps1'
Copy-File '.github/hooks/context-guard.sh'  '.github/hooks/context-guard.sh'

# 4. Risk knowledge base.
Copy-File 'docs/risks-nuget-hebung.md' 'docs/risks-nuget-hebung.md'

# 5. AGENTS.md operating contract (only if the target has none).
$targetAgents = Join-Path $TargetRepo 'AGENTS.md'
if ((Test-Path $targetAgents) -and -not $Force) {
    Write-Host "exists (kept): AGENTS.md  [merge the kit's AGENTS.md manually if needed]"
} else {
    Copy-Item -Force (Join-Path $Kit 'AGENTS.md') $targetAgents
    Write-Host "copied: AGENTS.md"
}

# 6. Persistence layout + gitignore for scratch.
foreach ($d in @('docs/handoffs', 'docs/nuget-hebung/agentresults')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $TargetRepo $d) | Out-Null
}
Write-Host "created: docs/handoffs, docs/nuget-hebung/agentresults"

$gi = Join-Path $TargetRepo '.gitignore'
$giLine = 'tasks/'
if (-not (Test-Path $gi) -or -not (Select-String -Path $gi -SimpleMatch $giLine -Quiet)) {
    Add-Content -Path $gi -Value "`n# Copilot scratch (context-guard state, throwaway notes)`ntasks/"
    Write-Host "updated: .gitignore (+ tasks/)"
}

Write-Host "`nDone. Next steps:"
Write-Host "  1. cd `"$TargetRepo`""
Write-Host "  2. git checkout -b feature/nuget-hebung"
Write-Host "  3. git add -A; git commit -m `"chore: install NuGet Hebung CLI kit`""
Write-Host "  4. Start the CLI:  copilot"
Write-Host "  5. In the CLI, run:  /nuget-hebung"
