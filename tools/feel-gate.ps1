# The feel-test's gate (PRO-03, docs/design/feel-test.md): an answers CSV (a row a tester: tester,goes,stops,... one
# to five, blank where they couldn't answer) and the folder of session JSON files the game wrote (-feel), through
# OWSBG.Core.FeelTest.Gate, to a Markdown report. Unity must be closed on this project.
#   pwsh tools/feel-gate.ps1                                   # logs/feel/answers.csv and logs/feel/*.json
#   pwsh tools/feel-gate.ps1 -Answers a.csv -Sessions dir -Report out.md
# Exit code: 0 the gate is met, 1 not yet, 2 the files couldn't be read.
param(
    [string]$Unity = "D:/Unity/6000.3.7f1/Editor/Unity.exe",
    [string]$Answers = "logs/feel/answers.csv",
    [string]$Sessions = "logs/feel",
    [string]$Report = "logs/feel/gate.md"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "LastCartographer"
$log = Join-Path $root "logs/feel-gate.log"
New-Item -ItemType Directory -Force -Path (Join-Path $root "logs/feel") | Out-Null
$answersPath = [System.IO.Path]::GetFullPath((Join-Path $root $Answers))
$sessionsPath = [System.IO.Path]::GetFullPath((Join-Path $root $Sessions))
$reportPath = [System.IO.Path]::GetFullPath((Join-Path $root $Report))
if (-not (Test-Path $answersPath)) {
    Write-Host "feel gate: no answers at $answersPath"
    Write-Host "the header is: tester,goes,stops,weight,lands,tap,late,early,wall,dash,hits,pogo,hour"
    exit 2
}
& $Unity -batchmode -nographics -projectPath $project -executeMethod OWSBG.Setup.FeelGateSetup.Run `
    -feelAnswers $answersPath -feelSessions $sessionsPath -feelReport $reportPath -quit -logFile $log
$code = $LASTEXITCODE
if (Test-Path $reportPath) { Get-Content $reportPath | Write-Host }
Write-Host "feel gate: exit code $code (0 met, 1 not yet, 2 unreadable); log at $log"
exit $code
