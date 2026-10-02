# The first test round's gate (PRO-04, docs/design/playtest-round1.md): an answers CSV (a row a tester: tester,start,
# next,... one to five, blank where they couldn't answer) and the folder of session JSON files the game wrote
# (-playtest), through OWSBG.Core.Playtest.Gate, to a Markdown report. Unity must be closed on this project.
#   pwsh tools/playtest-gate.ps1                               # logs/playtest/answers.csv and logs/playtest/*.json
#   pwsh tools/playtest-gate.ps1 -Answers a.csv -Sessions dir -Report out.md
# Exit code: 0 the round's bar is met, 1 not yet, 2 the files couldn't be read.
param(
    [string]$Unity = "D:/Unity/6000.3.7f1/Editor/Unity.exe",
    [string]$Answers = "logs/playtest/answers.csv",
    [string]$Sessions = "logs/playtest",
    [string]$Report = "logs/playtest/gate.md"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "LastCartographer"
$log = Join-Path $root "logs/playtest-gate.log"
New-Item -ItemType Directory -Force -Path (Join-Path $root "logs/playtest") | Out-Null
$answersPath = [System.IO.Path]::GetFullPath((Join-Path $root $Answers))
$sessionsPath = [System.IO.Path]::GetFullPath((Join-Path $root $Sessions))
$reportPath = [System.IO.Path]::GetFullPath((Join-Path $root $Report))
if (-not (Test-Path $answersPath)) {
    Write-Host "playtest gate: no answers at $answersPath"
    Write-Host "start from docs/playtest/round1/answers-template.csv"
    exit 2
}
# Unity.exe is a windowed program: "&" would not wait for it, and its exit code would be lost.
$p = Start-Process -FilePath $Unity -PassThru -NoNewWindow -ArgumentList @("-batchmode", "-nographics", "-projectPath", "`"$project`"", "-executeMethod", "OWSBG.Setup.PlaytestGateSetup.Run", "-playtestAnswers", "`"$answersPath`"", "-playtestSessions", "`"$sessionsPath`"", "-playtestReport", "`"$reportPath`"", "-quit", "-logFile", "`"$log`"")
$null = $p.Handle
$p.WaitForExit()
$code = $p.ExitCode
if (Test-Path $reportPath) { Get-Content $reportPath | Write-Host }
Write-Host "playtest gate: exit code $code (0 met, 1 not yet, 2 unreadable); log at $log"
exit $code
