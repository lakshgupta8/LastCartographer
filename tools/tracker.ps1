# The tracker from the plan (PRO-02, docs/design/tracker.md), through the GitHub CLI (gh, signed in to the repo).
#   pwsh tools/tracker.ps1              # export the plan, then make what's missing: milestones, labels, an issue per row
#   pwsh tools/tracker.ps1 -DryRun      # say what would be made, make nothing
#   pwsh tools/tracker.ps1 -Json logs/tracker.json   # use an export already made (no Unity)
# One issue per plan row, titled "[NAR-01] …", in its milestone, labelled by its area and "plan"; a row in progress
# gets "in-progress"; a row marked done closes its issue. Existing issues are found by the "[ID]" at the front of
# their title and never made twice. Exit code: 0 done, 1 something could not be made, 2 gh or the export is missing.
param(
    [string]$Unity = "D:/Unity/6000.3.7f1/Editor/Unity.exe",
    [string]$Json = "",
    [string]$Repo = "",
    [switch]$DryRun
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { Write-Host "tracker: gh (the GitHub CLI) is not installed"; exit 2 }
$repoArgs = @()
if ($Repo) { $repoArgs = @("--repo", $Repo) }

if (-not $Json) {
    $Json = Join-Path $root "logs/tracker.json"
    $log = Join-Path $root "logs/tracker.log"
    New-Item -ItemType Directory -Force -Path (Join-Path $root "logs") | Out-Null
    & $Unity -batchmode -nographics -projectPath (Join-Path $root "LastCartographer") -executeMethod OWSBG.Setup.TrackerSetup.Export -trackerOut $Json -quit -logFile $log
    if ($LASTEXITCODE -ne 0) { Write-Host "tracker: the plan did not export (see $log)"; exit 2 }
}
if (-not (Test-Path $Json)) { Write-Host "tracker: no export at $Json"; exit 2 }
$plan = Get-Content $Json -Raw | ConvertFrom-Json
Write-Host "tracker: $($plan.rows.Count) rows, $($plan.milestones.Count) milestones from the plan"

function Invoke-Gh {
    param([string[]]$GhArgs)
    if ($DryRun) { Write-Host "  would: gh $($GhArgs -join ' ')"; return $null }
    $out = & gh @GhArgs @repoArgs 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Host "  gh $($GhArgs[0]) $($GhArgs[1]) failed: $out"; $script:failed = $true }
    return $out
}
$script:failed = $false

# Milestones M0..M5, by title.
$existing = @(& gh api "repos/{owner}/{repo}/milestones?state=all&per_page=100" @repoArgs | ConvertFrom-Json)
foreach ($m in $plan.milestones) {
    $title = "$($m.id) $($m.name)"
    if ($existing | Where-Object { $_.title -eq $title }) { continue }
    Write-Host "milestone: $title"
    Invoke-Gh @("api", "repos/{owner}/{repo}/milestones", "-f", "title=$title", "-f", "description=$($m.exit) ($($m.length))") | Out-Null
}

# Labels: the areas (the same as triage.ps1's), "plan" and "in-progress".
$labels = @()
foreach ($a in @("nar", "des", "cmb", "prg", "chr", "env", "aud", "pro")) { $labels += ,@("area:$a", "5319e7", "Plan section $($a.ToUpper())") }
$labels += ,@("plan", "0052cc", "A row of docs/02-production-plan.md")
$labels += ,@("in-progress", "1d76db", "The plan marks it [~]: a v1 exists, the row is open")
foreach ($l in $labels) { Invoke-Gh @("label", "create", $l[0], "--color", $l[1], "--description", $l[2], "--force") | Out-Null }

# Issues: one per row, found by the [ID] at the front of the title.
$issues = @(& gh issue list --state all --limit 1000 --label plan --json number,title,state @repoArgs | ConvertFrom-Json)
$made = 0; $closed = 0; $kept = 0
foreach ($r in $plan.rows) {
    $have = $issues | Where-Object { $_.title -like "[$($r.id)]*" } | Select-Object -First 1
    if ($have) {
        if ($r.status -eq "Done" -and $have.state -eq "OPEN") {
            Write-Host "close: $($have.title)"
            Invoke-Gh @("issue", "close", "$($have.number)", "--comment", "Marked done in the plan.") | Out-Null; $closed++
        } elseif ($r.status -eq "InProgress" -and $have.state -eq "OPEN") {
            Invoke-Gh @("issue", "edit", "$($have.number)", "--add-label", "in-progress") | Out-Null; $kept++
        } else { $kept++ }
        continue
    }
    if ($r.status -eq "Done") { continue }
    $after = if ($r.after.Count -gt 0) { ($r.after -join ", ") } else { "nothing" }
    $body = "**Plan row** $($r.id), line $($r.line) of ``docs/02-production-plan.md``.`n`n$($r.task)`n`n- **Milestone:** $($r.milestone)$(if ($r.milestone -ne $r.milestoneEnd) { " to $($r.milestoneEnd)" })`n- **After:** $after$(if ($r.ref) { "`n- **Ref:** $($r.ref)" })`n`nThe plan is the source; when this changes, change the row first."
    $ms = $plan.milestones | Where-Object { $_.id -eq $r.milestone } | Select-Object -First 1
    $labelList = "plan,$($r.area)" + $(if ($r.status -eq "InProgress") { ",in-progress" } else { "" })
    $ghArgs = @("issue", "create", "--title", $r.issueTitle, "--body", $body, "--label", $labelList)
    if ($ms) { $ghArgs += @("--milestone", "$($ms.id) $($ms.name)") }
    Write-Host "issue: $($r.issueTitle)"
    Invoke-Gh $ghArgs | Out-Null; $made++
}
Write-Host "tracker: $made made, $closed closed, $kept already there"
if ($script:failed) { exit 1 }
exit 0
