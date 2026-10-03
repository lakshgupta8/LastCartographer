# Run the performance probe (OWSBG.Narrative.PerfProbe) in a Windows build: uncapped, it walks a route of rooms,
# samples frames and render counters in each, times each transition, and writes a JSON report against PerfBudget.
# Exit code: 0 within budget, 1 over, 2 a room never came in. It opens a window for as long as it runs (a minute or so):
# a -batchmode player doesn't render, so -Batch measures the CPU side only. -Advisory reports a budget miss as a
# warning and exits 0 (CI's hosted runners are not the target machine); a room that never comes in still fails.
param(
    [string]$Exe = "LastCartographer/Builds/Windows/LastCartographer.exe",
    [string]$Out = "logs/perf.json",
    [string]$Log = "logs/perf.log",
    [int]$Width = 1920,
    [int]$Height = 1080,
    [switch]$Batch,
    [switch]$Attribute,
    [switch]$Advisory,
    [int]$TimeoutSeconds = 600
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Exe)) { Write-Error "no build at $Exe"; exit 1 }
$outPath = [System.IO.Path]::GetFullPath($Out)
$logPath = [System.IO.Path]::GetFullPath($Log)
New-Item -ItemType Directory -Force -Path (Split-Path $outPath), (Split-Path $logPath) | Out-Null
$argList = @("-perf", "-skipPrologue", "-perfOut", "`"$outPath`"", "-logFile", "`"$logPath`"",
             "-screen-width", $Width, "-screen-height", $Height, "-screen-fullscreen", "0")
if ($Batch) { $argList += @("-batchmode", "-nographics") }
if ($Attribute) { $argList += "-perfAttribute" }
$p = Start-Process -FilePath $Exe -ArgumentList $argList -PassThru
$null = $p.Handle
if (-not $p.WaitForExit($TimeoutSeconds * 1000)) { $p.Kill(); Write-Host "perf: no answer in $TimeoutSeconds s"; exit 2 }
Select-String -Path $logPath -Pattern "\[OWSBG\] perf:" | ForEach-Object { Write-Host $_.Line }
Write-Host "report: $outPath; exit code $($p.ExitCode)"
if ($Advisory -and $p.ExitCode -eq 1) {
    $over = (Select-String -Path $logPath -Pattern "\[OWSBG\] perf: over budget: (.*)$" | Select-Object -Last 1).Matches[0].Groups[1].Value
    Write-Host "::warning title=perf probe::over budget on this machine (advisory): $over"
    exit 0
}
exit $p.ExitCode
