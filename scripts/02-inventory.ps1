# 02-inventory.ps1 — saves the list of installed applications (read-only).
# Deletes NOTHING. Used to find package ids before any cleanup.
#
#   .\scripts\02-inventory.ps1 -TvIp 192.168.1.50
#
param(
    [Parameter(Mandatory = $true)][string]$TvIp,
    [string]$Sdb,
    [string]$OutFile = ".\dist\inventaire.txt"
)
. "$PSScriptRoot\lib.ps1"

$sdbPath = Find-Sdb $Sdb
& $sdbPath start-server | Out-Null
$serial = Connect-Tv -Sdb $sdbPath -Ip $TvIp

Write-Title (Tr "Reading the application inventory" "Lecture de l'inventaire des applications")
$list = (Invoke-Native $sdbPath @('-s', $serial, 'shell', '0', 'vd_applist')).Output
if (-not $list -or $list -match 'not found|error') { $list = (Invoke-Native $sdbPath @('-s', $serial, 'shell', '0', 'applist')).Output }
if (-not $list) { throw (Tr "Cannot read the application list on this firmware." "Impossible de lire la liste des applications sur ce firmware.") }

$dir = Split-Path -Parent $OutFile
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
$list | Set-Content -LiteralPath $OutFile -Encoding UTF8

$count = ($list | Measure-Object -Line).Lines
Write-Host (Tr "Inventory saved: $OutFile ($count lines)" "Inventaire enregistré : $OutFile ($count lignes)") -ForegroundColor Green
Write-Host (Tr "Open this file to find the ids (e.g. 9Ur5IzDKqV.TizenYouTube)." "Ouvrez ce fichier pour retrouver les identifiants (ex: 9Ur5IzDKqV.TizenYouTube).")
