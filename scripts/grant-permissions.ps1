<#
.SYNOPSIS
  Persist the NuGet Hebung subagent allow-list into GitHub Copilot CLI's
  permissions store for a target repo, so the subagents stop re-prompting for
  routine, expected operations.

.DESCRIPTION
  GitHub Copilot CLI records approved tool/command permissions per project
  location in permissions-config.json (default `~/.copilot/`, or under
  $COPILOT_HOME if set). This script merges a fixed, role-scoped allow-list into
  the entry for -TargetRepo without disturbing other locations or any approvals
  you added manually. Re-running is idempotent.

  The allow-list is the UNION of what the two Hebung subagents need, because the
  store is keyed per directory location with a single flat tool_approvals list
  (it cannot be scoped per agent). The per-role split is documented in the agent
  profiles under .github/agents/. It deliberately does NOT include `git push` or
  branch operations, so those stay gated behind a prompt.

  Permission identifiers follow `copilot help permissions`: approval is on a
  first-level subcommand basis (e.g. "git push"), and persisted identifiers are
  exact command stems, so dotnet subcommands are enumerated explicitly.

.PARAMETER TargetRepo
  Absolute path to the repository whose location entry should be granted the
  allow-list. The path is resolved and used verbatim as the location key.

.PARAMETER ConfigPath
  Path to permissions-config.json. Defaults to
  "$COPILOT_HOME/permissions-config.json" if COPILOT_HOME is set, otherwise
  "$env:USERPROFILE\.copilot\permissions-config.json". Useful for testing.

.PARAMETER PassThru
  Emit the resulting allow-list (command identifiers) to the pipeline.

.EXAMPLE
  ./scripts/grant-permissions.ps1 -TargetRepo C:\dev\Repos\nuget-hebung-demo

.NOTES
  To reset: delete the location's entry from permissions-config.json (do this
  while no CLI session is running in that repo).
#>
param(
    [Parameter(Mandatory = $true)] [string] $TargetRepo,
    [string] $ConfigPath,
    [switch] $PassThru
)

$ErrorActionPreference = 'Stop'

# --- The allow-list -------------------------------------------------------
# Union of the read-only investigator set and the build/test/commit updater set.
# Keep `git push` and branch operations OUT so protected-branch safety holds.
$AllowedCommands = @(
    # restore is shared by both roles
    'dotnet restore'
    # investigator: read-only inventory + feed/org queries
    'dotnet list'
    'dotnet list package'
    'dotnet package search'
    'dotnet nuget'
    # updater: build + unit tests
    'dotnet build'
    'dotnet test'
    # PS5->PS6 migrator: SDK-style conversion + package install, and publishing the tools
    'dotnet add'
    'dotnet remove'
    'dotnet publish'
    # PS5->PS6 single-file migration tools (invoked directly from tools/ps5to6/dist)
    'ps5to6-snapshot'
    'ps5to6-uninstall-all'
    'ps5to6-feed-probe'
    'ps5to6-scaffold-project'
    'ps5to6-report'
    # updater: commit one logical bump at a time (NOT push, NOT branch ops)
    'git add'
    'git commit'
    # read-only display / search used during investigation
    'Write-Output'
    'Get-Content'
    'Get-ChildItem'
    'Select-String'
)

# --- Resolve paths --------------------------------------------------------
if (-not (Test-Path $TargetRepo)) { throw "Target repo not found: $TargetRepo" }
$TargetRepo = (Resolve-Path $TargetRepo).Path

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $base = if ($env:COPILOT_HOME) { $env:COPILOT_HOME } else { Join-Path $env:USERPROFILE '.copilot' }
    $ConfigPath = Join-Path $base 'permissions-config.json'
}

# --- Load (or initialize) the config --------------------------------------
if (Test-Path $ConfigPath) {
    $cfg = Get-Content -Raw -Path $ConfigPath | ConvertFrom-Json
} else {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ConfigPath) | Out-Null
    $cfg = [pscustomobject]@{ locations = [pscustomobject]@{} }
}
if ($null -eq $cfg.locations) {
    $cfg | Add-Member -NotePropertyName locations -NotePropertyValue ([pscustomobject]@{}) -Force
}

# --- Merge the allow-list into this location ------------------------------
# Preserve any command identifiers the user already approved here, and preserve
# approval entries of other kinds verbatim. Always ensure a single consolidated
# `commands` entry and a `write` entry.
$existing = $cfg.locations.PSObject.Properties[$TargetRepo]
$existingCmds = @()
$otherApprovals = @()
if ($existing) {
    foreach ($ta in @($existing.Value.tool_approvals)) {
        if ($null -eq $ta) { continue }
        if ($ta.kind -eq 'commands') { $existingCmds += @($ta.commandIdentifiers) }
        elseif ($ta.kind -eq 'write') { } # re-added below
        else { $otherApprovals += $ta }
    }
}

$mergedCmds = @($existingCmds + $AllowedCommands | Where-Object { $_ } | Select-Object -Unique)

$approvals = @()
$approvals += [pscustomobject]@{ kind = 'commands'; commandIdentifiers = $mergedCmds }
$approvals += [pscustomobject]@{ kind = 'write' }
foreach ($o in $otherApprovals) { $approvals += $o }

$locValue = [pscustomobject]@{ tool_approvals = @($approvals) }
$cfg.locations | Add-Member -NotePropertyName $TargetRepo -NotePropertyValue $locValue -Force

# --- Write back (UTF-8, no BOM) -------------------------------------------
$json = $cfg | ConvertTo-Json -Depth 12
[System.IO.File]::WriteAllText($ConfigPath, $json, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "Granted NuGet Hebung allow-list for location:"
Write-Host "  $TargetRepo"
Write-Host "in: $ConfigPath"
Write-Host "  commands ($($mergedCmds.Count)): $($mergedCmds -join ', ')"
Write-Host "  write: yes"
Write-Host "  (git push / branch ops intentionally NOT granted -- they stay gated)"

if ($PassThru) { $mergedCmds }
