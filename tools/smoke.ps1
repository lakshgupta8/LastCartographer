# Boot a Windows build with -smoke (OWSBG.Narrative.SmokeTest): it loads the first room, walks into a second through
# Addressables, checks the build stamp, starts a conversation, and quits with 0 (passed), 1 (failed) or 2 (timed out).
param(
    [string]$Exe = "LastCartographer/Builds/Windows/LastCartographer.exe",
    [string]$Log = "logs/smoke.log",
    [int]$TimeoutSeconds = 300
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Exe)) { Write-Error "no build at $Exe"; exit 1 }
$logPath = [System.IO.Path]::GetFullPath($Log)
New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
$p = Start-Process -FilePath $Exe -ArgumentList "-batchmode", "-nographics", "-smoke", "-logFile", "`"$logPath`"" -PassThru
$null = $p.Handle   # keep the handle so the exit code can be read
if (-not $p.WaitForExit($TimeoutSeconds * 1000)) {
    $p.Kill()
    Write-Host "smoke: the build didn't answer in $TimeoutSeconds s"
    exit 2
}
$line = Select-String -Path $logPath -Pattern "\[OWSBG\] smoke:" | Select-Object -Last 1
if ($line) { Write-Host $line.Line } else { Write-Host "smoke: no result in $logPath" }
Write-Host "exit code $($p.ExitCode)"
exit $p.ExitCode
