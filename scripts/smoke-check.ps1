# Smoke check: skill + agent frontmatter validity, hook JSON, and internal links.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$errs = New-Object System.Collections.Generic.List[string]

function Get-Frontmatter([string]$text) {
    if ($text -match "(?s)^---\r?\n(.*?)\r?\n---") { return $Matches[1] }
    return $null
}

# Catch the YAML plain-scalar breakers that silently drop a skill/agent from the
# index: an unquoted value containing ': ' (read as a nested mapping) or ' #'
# (read as a comment), and quoted values that aren't closed. Dependency-free.
function Add-YamlScalarWarnings([string]$fm, [string]$path, $errs) {
    foreach ($line in ($fm -split "\r?\n")) {
        if ($line -match '^\s*#') { continue }
        if ($line -notmatch '^([A-Za-z0-9_.\-]+):(?:[ \t]+(.*))?$') { continue }
        $key = $Matches[1]; $val = $Matches[2]
        if ($null -eq $val -or $val.Trim().Length -eq 0) { continue }
        $t = $val.Trim()
        $q = $t.Substring(0, 1)
        if ($q -eq "'" -or $q -eq '"') {
            if ($t.Length -lt 2 -or -not $t.EndsWith($q)) {
                $errs.Add("${path}: frontmatter '$key' value is not a closed $q...$q string")
            }
            continue
        }
        # Balanced flow collection ([...] / {...}) is valid YAML; leave it alone.
        if (($t.StartsWith('[') -and $t.EndsWith(']')) -or ($t.StartsWith('{') -and $t.EndsWith('}'))) { continue }
        if ($t -match ':[ \t]') { $errs.Add("${path}: frontmatter '$key' value contains ': ' which YAML reads as a nested mapping -- wrap the value in single quotes") }
        if ($t -match '[ \t]#')  { $errs.Add("${path}: frontmatter '$key' value contains ' #' which starts a YAML comment -- wrap the value in single quotes") }
    }
}

# 1. Every skill has valid frontmatter with name == parent dir + a description.
$skillsDir = Join-Path $root '.github/skills'
if (Test-Path $skillsDir) {
    foreach ($dir in Get-ChildItem -Path $skillsDir -Directory | Sort-Object Name) {
        $name = $dir.Name
        $path = Join-Path $dir.FullName 'SKILL.md'
        if (-not (Test-Path $path)) { $errs.Add("missing $path"); continue }
        $fm = Get-Frontmatter (Get-Content -Raw $path)
        if ($null -eq $fm) { $errs.Add("${path}: no frontmatter"); continue }
        if ($fm -notmatch "name:\s*$name\b") { $errs.Add("${path}: name does not match dir '$name'") }
        if ($fm -notmatch "description:")    { $errs.Add("${path}: no description") }
        Add-YamlScalarWarnings $fm $path $errs
    }
}

# 2. Every agent has frontmatter with name == file stem, a description, and a model.
$agentsDir = Join-Path $root '.github/agents'
if (Test-Path $agentsDir) {
    foreach ($af in Get-ChildItem -Path $agentsDir -Filter *.md | Sort-Object Name) {
        $stem = $af.Name -replace '(\.agent)?\.md$',''
        $fm = Get-Frontmatter (Get-Content -Raw $af.FullName)
        if ($null -eq $fm) { $errs.Add("$($af.FullName): no frontmatter"); continue }
        if ($fm -notmatch "name:\s*$stem\b") { $errs.Add("$($af.FullName): name does not match file '$stem'") }
        if ($fm -notmatch "description:")    { $errs.Add("$($af.FullName): no description") }
        if ($fm -notmatch "model:")          { $errs.Add("$($af.FullName): no model (expected a pinned model)") }
        Add-YamlScalarWarnings $fm $af.FullName $errs
    }
}

# 3. Internal markdown links point to existing files (fenced code blocks stripped).
$linkRe  = [regex]'\[[^\]]+\]\(([^)]+)\)'
$fenceRe = [regex]'(?sm)^(`{3,})[^\n]*\n.*?^\1[ \t]*$'
$mdFiles = Get-ChildItem -Path $root -Recurse -Filter *.md |
    Where-Object { $_.FullName -notmatch '\\\.git\\' }
foreach ($md in $mdFiles) {
    $content = $fenceRe.Replace((Get-Content -Raw $md.FullName), '')
    foreach ($m in $linkRe.Matches($content)) {
        $target = ($m.Groups[1].Value -split '#')[0]
        if ([string]::IsNullOrEmpty($target)) { continue }
        if ($target -match '^(https?://|mailto:)') { continue }
        $resolved = Join-Path $md.DirectoryName $target
        if (-not (Test-Path $resolved)) { $errs.Add("$($md.FullName): broken link -> $target") }
    }
}

# 4. Hook configs are valid JSON with version + a hooks object.
$hooksDir = Join-Path $root '.github/hooks'
if (Test-Path $hooksDir) {
    foreach ($hf in Get-ChildItem -Path $hooksDir -Filter *.json) {
        try {
            $h = Get-Content -Raw $hf.FullName | ConvertFrom-Json
            if (-not $h.version) { $errs.Add("$($hf.FullName): missing 'version'") }
            if (-not $h.hooks)   { $errs.Add("$($hf.FullName): missing 'hooks' object") }
        } catch {
            $errs.Add("$($hf.FullName): invalid JSON")
        }
    }
}

if ($errs.Count -gt 0) {
    Write-Output 'SMOKE CHECK FAILED:'
    foreach ($e in $errs) { Write-Output "  - $e" }
    exit 1
}
Write-Output 'SMOKE CHECK PASSED'
