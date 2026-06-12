# Smoke check: skill + agent frontmatter validity, hook JSON, and internal links.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$errs = New-Object System.Collections.Generic.List[string]

function Get-Frontmatter([string]$text) {
    if ($text -match "(?s)^---\r?\n(.*?)\r?\n---") { return $Matches[1] }
    return $null
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
