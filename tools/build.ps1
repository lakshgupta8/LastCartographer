# The Windows build on this machine, as CI makes it: the Addressables content, the 64-bit player stamped with its
# version, then a smoke run of the result. Pass -Development for a development build, -SteamAppId/-SteamDepotId to
# also write Steam's depot scripts beside it (Builds/steam). Unity must be closed on this project.
param(
    [string]$Unity = "D:/Unity/6000.3.7f1/Editor/Unity.exe",
    [string]$Output = "Builds/Windows",
    [string]$Version = "",
    [switch]$Development,
    [string]$SteamAppId = "",
    [string]$SteamDepotId = "",
    [string]$SteamBranch = "",
    [switch]$NoSmoke
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "LastCartographer"
$log = Join-Path $root "logs/build.log"
New-Item -ItemType Directory -Force -Path (Join-Path $root "logs") | Out-Null

$unityArgs = @("-batchmode", "-nographics", "-projectPath", $project, "-buildTarget", "Win64",
          "-executeMethod", "OWSBG.Build.GameBuild.BuildFromCommandLine", "-buildOutput", $Output, "-logFile", $log)
if ($Version) { $unityArgs += @("-buildVersion", $Version) }
if ($Development) { $unityArgs += "-development" }
if ($SteamAppId -and $SteamDepotId) { $unityArgs += @("-steamAppId", $SteamAppId, "-steamDepotId", $SteamDepotId) }
if ($SteamBranch) { $unityArgs += @("-steamBranch", $SteamBranch) }

$p = Start-Process -FilePath $Unity -ArgumentList $unityArgs -PassThru -NoNewWindow
$null = $p.Handle
$p.WaitForExit()
Select-String -Path $log -Pattern "\[OWSBG\] (build|building|Addressables)" | ForEach-Object { Write-Host $_.Line }
if ($p.ExitCode -ne 0) { Write-Host "build failed ($($p.ExitCode)); see $log"; exit $p.ExitCode }
if ($NoSmoke) { exit 0 }
& (Join-Path $PSScriptRoot "smoke.ps1") -Exe (Join-Path $project "$Output/LastCartographer.exe") -Log (Join-Path $root "logs/smoke.log")
exit $LASTEXITCODE
