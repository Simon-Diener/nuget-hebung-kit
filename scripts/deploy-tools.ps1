<#
.SYNOPSIS
  (Re)build the PS5->PS6 single-file tools and deploy them into a target repo.

.DESCRIPTION
  Publishes the five ps5to6-* tools fresh and writes them into
  <TargetRepo>\tools\ps5to6\dist, wiping any existing dist first (so a previous
  bootstrap's nested dist\dist is removed cleanly). Use this when you only need
  to refresh the tools in a target without running the full bootstrap (e.g. after
  a tool fix). Requires the .NET 8 SDK -- the tools target net8.0.

.PARAMETER TargetRepo
  Absolute path to the repo whose tools/ps5to6/dist should be (re)built.

.PARAMETER Configuration
  Build configuration. Default: Release.

.EXAMPLE
  ./scripts/deploy-tools.ps1 -TargetRepo C:\dev\Repos\Audi.PS5
#>
param(
    [Parameter(Mandatory = $true)] [string] $TargetRepo,
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$Kit = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path $TargetRepo)) { throw "Target repo not found: $TargetRepo" }
$TargetRepo = (Resolve-Path $TargetRepo).Path

$tools   = 'Snapshot', 'UninstallAll', 'FeedProbe', 'ScaffoldProject', 'Report'
$distDir = Join-Path $TargetRepo 'tools/ps5to6/dist'

Write-Host "Kit:    $Kit"
Write-Host "Target: $TargetRepo`n"

# Wipe the target dist first -- also removes a previously nested dist\dist.
if (Test-Path $distDir) {
    Remove-Item -Recurse -Force $distDir
    Write-Host "cleaned: tools/ps5to6/dist"
}

foreach ($t in $tools) {
    $proj = Join-Path $Kit "tools/ps5to6/src/$t/$t.csproj"
    if (-not (Test-Path $proj)) { throw "Tool project missing in kit: $proj" }
    $out = Join-Path $distDir $t
    Write-Host "publishing: $t -> tools/ps5to6/dist/$t"
    & dotnet publish $proj -c $Configuration -r win-x64 --self-contained false -p:PublishSingleFile=true -o $out | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $t (exit $LASTEXITCODE)" }
}

$exes = @(Get-ChildItem $distDir -Recurse -Filter 'ps5to6-*.exe')
Write-Host "`nDeployed $($exes.Count) tool exe(s) to: $distDir"
foreach ($e in $exes) { Write-Host "  $($e.FullName)" }
if ($exes.Count -ne $tools.Count) {
    Write-Warning "Expected $($tools.Count) exes but found $($exes.Count) -- check the publish output above."
}
