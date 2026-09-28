# Triage against the bug bar (PRO-07, docs/design/bug-bar.md), through the GitHub CLI (gh, signed in to the repo).
#   pwsh tools/triage.ps1                    # open bugs by severity, the unsorted ones, and the bar for the milestone
#   pwsh tools/triage.ps1 -Milestone rc      # beta (the default), rc or release
#   pwsh tools/triage.ps1 -Labels            # make or refresh the tracker's labels (idempotent)
# Exit code: 0 when the counts meet the milestone's bar, 1 when they don't, 2 when gh is missing or not signed in.
# The labels and the bar mirror OWSBG.Core.BugBar; a test (BugBarTests) holds this file to it.
param(
    [ValidateSet("beta", "rc", "release")] [string]$Milestone = "beta",
    [switch]$Labels,
    [string]$Repo = ""
)
$ErrorActionPreference = "Stop"

# most severe first; -1 is no limit
$severities = @("blocker", "critical", "major", "minor", "trivial")
$bar = @{
    beta    = @(0, 3, -1, -1, -1)
    rc      = @(0, 0, 10, -1, -1)
    release = @(0, 0, 0, 25, -1)
}
$labelSpecs = @(
    @("severity:blocker",  "b60205", "The game crashes, a save is lost or wrong, or no ending can be reached. Nobody can play past it."),
    @("severity:critical", "d93f0b", "Progress stops for some players, or an ending goes wrong: a soft-lock, a gate that stays shut, a boss that can't be hurt."),
    @("severity:major",    "fbca04", "Wrong but passable: a flag, a fate, a commission or a purse wrong; a room over budget; an attack with no read."),
    @("severity:minor",    "0e8a16", "Cosmetic or feel: a clip, a typo, a missing sound, a hit that lands soft."),
    @("severity:trivial",  "c5def5", "Nobody would notice without being told."),
    @("area:nar", "5319e7", "Plan section NAR"),
    @("area:des", "5319e7", "Plan section DES"),
    @("area:cmb", "5319e7", "Plan section CMB"),
    @("area:prg", "5319e7", "Plan section PRG"),
    @("area:chr", "5319e7", "Plan section CHR"),
    @("area:env", "5319e7", "Plan section ENV"),
    @("area:aud", "5319e7", "Plan section AUD"),
    @("area:pro", "5319e7", "Plan section PRO"),
    @("triage",             "ededed", "New: no severity or area yet"),
    @("needs-repro",        "ededed", "Nobody has made it happen again"),
    @("fixed-needs-verify", "ededed", "Fixed on main, with its test; not yet seen fixed in a build"),
    @("verified",           "ededed", "Seen fixed in a build"),
    @("known-issue",        "ededed", "Ships as it is; on the known-issues list"),
    @("wontfix",            "ededed", "Won't be fixed, and why is in the thread")
)

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { Write-Host "triage: gh (the GitHub CLI) is not installed"; exit 2 }
$repoArgs = @()
if ($Repo) { $repoArgs = @("--repo", $Repo) }

if ($Labels) {
    foreach ($spec in $labelSpecs) {
        & gh label create $spec[0] --color $spec[1] --description $spec[2] --force @repoArgs
        if ($LASTEXITCODE -ne 0) { Write-Host "triage: could not make label $($spec[0])"; exit 2 }
    }
    Write-Host "triage: $($labelSpecs.Count) labels in place"
    exit 0
}

$json = & gh issue list --state open --limit 500 --json number,title,labels @repoArgs
if ($LASTEXITCODE -ne 0) { Write-Host "triage: gh could not list the issues (signed in? gh auth login)"; exit 2 }
$issues = $json | ConvertFrom-Json

$open = @(0, 0, 0, 0, 0)
$unsorted = @()
foreach ($issue in $issues) {
    $names = @($issue.labels | ForEach-Object { $_.name })
    $sev = -1
    for ($i = 0; $i -lt $severities.Count; $i++) { if ($names -contains "severity:$($severities[$i])") { $sev = $i; break } }
    $isBug = ($names -contains "triage") -or ($sev -ge 0)
    if (-not $isBug) { continue }
    $sorted = $sev -ge 0 -and (@($names | Where-Object { $_ -like "area:*" }).Count -gt 0)
    if (-not $sorted) { $unsorted += "#$($issue.number) $($issue.title)"; }
    if ($sev -ge 0 -and -not ($names -contains "known-issue") -and -not ($names -contains "wontfix")) { $open[$sev]++ }
}

Write-Host "open bugs against the $Milestone bar:"
$fail = $false
for ($i = 0; $i -lt $severities.Count; $i++) {
    $max = $bar[$Milestone][$i]
    $limit = if ($max -lt 0) { "no limit" } else { "at most $max" }
    $mark = if ($max -ge 0 -and $open[$i] -gt $max) { $fail = $true; "OVER" } else { "ok" }
    Write-Host ("  {0,-9} {1,3}  ({2})  {3}" -f $severities[$i], $open[$i], $limit, $mark)
}
if ($unsorted.Count -gt 0) {
    Write-Host "to triage ($($unsorted.Count)): a severity and an area each"
    $unsorted | ForEach-Object { Write-Host "  $_" }
}
if ($fail) { Write-Host "triage: the $Milestone bar is not met"; exit 1 }
Write-Host "triage: the $Milestone bar is met"
exit 0
