# PRO-06's gate (docs/design/performance.md): every probe report in a folder (tools/perf.ps1 -Out logs/perf/<machine>.json,
# run on each tester's machine) judged against the target: 60 fps at 1920x1080 on a GTX 1060-class card, every room
# transition under 100 ms. A faster card's report is projected onto the target but can't prove it. Unity must be
# closed on this project.
#   pwsh tools/perf-gate.ps1                              # logs/perf/*.json, report in logs/perf/gate.md
#   pwsh tools/perf-gate.ps1 -Reports dir -Report out.md
# Exit code: 0 met, 1 not yet, 2 the folder couldn't be read.
param(
    [string]$Unity = "D:/Unity/6000.3.7f1/Editor/Unity.exe",
    [string]$Reports = "logs/perf",
    [string]$Report = "logs/perf/gate.md"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "LastCartographer"
$log = Join-Path $root "logs/perf-gate.log"
$reportsPath = [System.IO.Path]::GetFullPath((Join-Path $root $Reports))
$reportPath = [System.IO.Path]::GetFullPath((Join-Path $root $Report))
New-Item -ItemType Directory -Force -Path $reportsPath | Out-Null
# Unity.exe is a windowed program: "&" would not wait for it, and its exit code would be lost.
$p = Start-Process -FilePath $Unity -PassThru -NoNewWindow -ArgumentList @("-batchmode", "-nographics", "-projectPath", "`"$project`"", "-executeMethod", "OWSBG.Setup.PerfGateSetup.Run", "-perfReports", "`"$reportsPath`"", "-perfGate", "`"$reportPath`"", "-quit", "-logFile", "`"$log`"")
$null = $p.Handle
$p.WaitForExit()
$code = $p.ExitCode
if (Test-Path $reportPath) { Get-Content $reportPath | Write-Host }
Write-Host "perf gate: exit code $code (0 met, 1 not yet, 2 unreadable); log at $log"
exit $code
