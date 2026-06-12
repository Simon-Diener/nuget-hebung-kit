#!/usr/bin/env bash
# context-guard -- Copilot CLI `agentStop` hook (bash variant).
#
# Forces one follow-up turn to persist state when a session looks
# context-pressured. agentStop output IS processed: {"decision":"block",
# "reason":"..."} forces another turn using `reason` as the prompt.
#
# Approximates context pressure from turn count + transcript size. Needs `jq`;
# if `jq` is missing it degrades to a turn-count-only heuristic. Prints ONLY
# JSON on stdout and fails OPEN ({}) on any error.

set -euo pipefail

TURN_THRESHOLD="${CONTEXT_GUARD_TURNS:-30}"
REARM_INTERVAL="${CONTEXT_GUARD_REARM:-25}"
SIZE_THRESH_MB="${CONTEXT_GUARD_SIZE_MB:-3}"

emit() { printf '%s' "$1"; }
trap 'emit "{}"' ERR

raw="$(cat || true)"
if [ -z "$raw" ]; then emit '{}'; exit 0; fi

if command -v jq >/dev/null 2>&1; then
  sid="$(printf '%s' "$raw" | jq -r '.sessionId // .session_id // "unknown"')"
  cwd="$(printf '%s' "$raw" | jq -r '.cwd // "."')"
  tp="$(printf '%s' "$raw"  | jq -r '.transcriptPath // .transcript_path // ""')"
else
  sid="unknown"; cwd="."; tp=""
fi

state_dir="$cwd/tasks/.context-guard"
mkdir -p "$state_dir"
state_file="$state_dir/$sid.json"

turns=0; next_trigger="$TURN_THRESHOLD"
if [ -f "$state_file" ] && command -v jq >/dev/null 2>&1; then
  turns="$(jq -r '.turns // 0' "$state_file")"
  next_trigger="$(jq -r '.nextTrigger // '"$TURN_THRESHOLD" "$state_file")"
fi
turns=$((turns + 1))

transcript_mb=0
if [ -n "$tp" ] && [ -f "$tp" ]; then
  bytes="$(wc -c < "$tp" 2>/dev/null || echo 0)"
  transcript_mb=$(( bytes / 1048576 ))
fi

reason=""
if [ "$turns" -ge "$next_trigger" ]; then
  if [ "$transcript_mb" -ge "$SIZE_THRESH_MB" ]; then
    reason="the transcript is large (~${transcript_mb} MB)"
  else
    reason="this session is ${turns} turns long"
  fi
  next_trigger=$((turns + REARM_INTERVAL))
fi

printf '{"turns":%s,"nextTrigger":%s}' "$turns" "$next_trigger" > "$state_file"

if [ -n "$reason" ]; then
  msg="[context-guard] ${reason}, so context may be running low. Before anything else, persist state: if a NuGet Hebung is in progress, update docs/nuget-hebung/plan.md (current phase, ticked steps, exact '## Resume' line); then invoke the \`handoff\` skill to write docs/handoffs/<today>-<topic>.md; commit both. Then tell the user to continue in a fresh session opening with 'Read docs/nuget-hebung/plan.md and continue from Resume.' (Heuristic -- if you just handed off, simply finish.)"
  jq -cn --arg r "$msg" '{decision:"block",reason:$r}' 2>/dev/null || printf '{"decision":"block","reason":%s}' "\"$msg\""
else
  emit '{}'
fi
