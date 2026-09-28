# Upload a build to Steam with SteamPipe, from the depot scripts the build wrote (tools/build.ps1 -SteamAppId ...
# -SteamDepotId ...). steamcmd must be on PATH (or in $env:STEAMCMD), and the Steamworks account is read from
# $env:STEAM_USERNAME; steamcmd asks for the password and Steam Guard code itself, and caches them after the first time.
# Nothing here is stored in the repository.
param(
    [Parameter(Mandatory = $true)][string]$AppId,
    [string]$ScriptDir = "LastCartographer/Builds/steam"
)
$ErrorActionPreference = "Stop"
$steamcmd = if ($env:STEAMCMD) { $env:STEAMCMD } else { "steamcmd" }
if (-not $env:STEAM_USERNAME) { Write-Error "set STEAM_USERNAME to the build account"; exit 1 }
$script = Join-Path (Resolve-Path $ScriptDir) "app_build_$AppId.vdf"
if (-not (Test-Path $script)) { Write-Error "no ${script}: build with -SteamAppId $AppId -SteamDepotId <depot> first"; exit 1 }
& $steamcmd +login $env:STEAM_USERNAME +run_app_build "$script" +quit
exit $LASTEXITCODE
