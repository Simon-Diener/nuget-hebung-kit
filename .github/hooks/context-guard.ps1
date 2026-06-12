# context-guard -- Copilot CLI `agentStop` hook.
#
# Detects a long / context-pressured session and, when triggered, FORCES one
# follow-up turn instructing the agent to persist state (the `handoff` skill +
# the NuGet-Hebung plan's Resume section) before context is lost.
#
# Why agentStop: per the Copilot hooks reference, `agentStop` output IS processed
# -- returning {"decision":"block","reason":"..."} forces another turn using
# `reason` as the prompt. (userPromptSubmitted / preCompact output is NOT
# processed, so they cannot inject an instruction.)
#
# The CLI exposes no token telemetry, so we approximate context pressure from
# turn count + transcript size + the presence of an auto-compaction checkpoint.
# Reads the hook payload as JSON on stdin; writes ONLY JSON on stdout:
#   {"decision":"block","reason":"..."}  to force a handoff turn, or  {}.
# Fails OPEN (prints {}) on any error so it never wedges the session.

$ErrorActionPreference = 'Stop'

# Tunables (override via environment).
$TurnThreshold = if ($env:CONTEXT_GUARD_TURNS)   { [int]$env:CONTEXT_GUARD_TURNS }     else { 30 }
$RearmInterval = if ($env:CONTEXT_GUARD_REARM)    { [int]$env:CONTEXT_GUARD_REARM }     else { 25 }
$SizeThreshMb  = if ($env:CONTEXT_GUARD_SIZE_MB)  { [double]$env:CONTEXT_GUARD_SIZE_MB } else { 3 }

function Write-Result($obj) { [Console]::Out.Write(($obj | ConvertTo-Json -Compress)) }

try {
    $raw = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($raw)) { Write-Result @{}; return }
    $p = $raw | ConvertFrom-Json

    $sid = $p.sessionId; if (-not $sid) { $sid = $p.session_id }; if (-not $sid) { $sid = 'unknown' }
    $cwd = $p.cwd;        if (-not $cwd) { $cwd = (Get-Location).Path }

    # Per-session state under the (gitignored) tasks/ scratch area.
    $stateDir = Join-Path $cwd 'tasks/.context-guard'
    if (-not (Test-Path $stateDir)) { New-Item -ItemType Directory -Force -Path $stateDir | Out-Null }
    $stateFile = Join-Path $stateDir ("$sid.json")

    if (Test-Path $stateFile) {
        $state = Get-Content -Raw $stateFile | ConvertFrom-Json
    } else {
        $state = [pscustomobject]@{ turns = 0; nextTrigger = $TurnThreshold; checkpointFired = $false }
    }
    $state.turns = [int]$state.turns + 1

    # Signal 1: transcript size (agentStop payload carries transcriptPath).
    $transcriptMb = 0.0
    $tp = $p.transcriptPath; if (-not $tp) { $tp = $p.transcript_path }
    if ($tp -and (Test-Path $tp)) { $transcriptMb = (Get-Item $tp).Length / 1MB }

    # Signal 2: Copilot's own auto-compaction checkpoint, if reachable.
    $checkpointExists = $false
    $cpDir = Join-Path $HOME ".copilot/session-state/$sid/checkpoints"
    if (Test-Path $cpDir) {
        $checkpointExists = @(Get-ChildItem -Path $cpDir -File -ErrorAction SilentlyContinue).Count -gt 0
    }

    $reasons = @()
    if ($checkpointExists -and -not $state.checkpointFired) {
        $state.checkpointFired = $true
        $reasons += 'Copilot auto-compaction has already occurred (context passed the model threshold)'
    }
    if ($state.turns -ge [int]$state.nextTrigger) {
        if ($transcriptMb -ge $SizeThreshMb) {
            $reasons += ("the transcript is large (~{0:N1} MB)" -f $transcriptMb)
        } else {
            $reasons += ("this session is {0} turns long" -f $state.turns)
        }
        $state.nextTrigger = [int]$state.turns + $RearmInterval
    }

    $state | ConvertTo-Json -Compress | Set-Content -Encoding UTF8 $stateFile

    if ($reasons.Count -gt 0) {
        $why = [string]::Join('; ', $reasons)
        $reason = "[context-guard] $why, so context may be running low. Before doing anything else, " +
                  "persist state so a fresh session can resume cleanly: if a NuGet Hebung is in progress, " +
                  "update docs/nuget-hebung/plan.md (current phase, ticked steps, and an exact '## Resume' line); " +
                  "then invoke the ``handoff`` skill to write docs/handoffs/<today>-<topic>.md; commit both. " +
                  "Then tell the user to continue in a fresh session that opens with " +
                  "'Read docs/nuget-hebung/plan.md and continue from Resume.' " +
                  "(Heuristic trigger -- if you have already just handed off, simply finish.)"
        Write-Result @{ decision = 'block'; reason = $reason }
    } else {
        Write-Result @{}
    }
}
catch {
    Write-Result @{}
}
