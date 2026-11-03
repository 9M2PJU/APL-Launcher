# 01-connect.ps1 — checks that the PC can see the TV in developer mode.
#
#   .\scripts\01-connect.ps1 -TvIp 192.168.1.50
#
param(
    [Parameter(Mandatory = $true)][string]$TvIp,
    [string]$Sdb
)
. "$PSScriptRoot\lib.ps1"

$sdbPath = Find-Sdb $Sdb
Write-Title (Tr "Connecting to the TV" "Connexion au téléviseur")
& $sdbPath start-server | Out-Null
$serial = Connect-Tv -Sdb $sdbPath -Ip $TvIp

Write-Title (Tr "Detected devices" "Appareils détectés")
& $sdbPath devices

# Model info (public API, read-only)
try {
    $info = Invoke-RestMethod -Uri "http://$TvIp`:8001/api/v2/" -TimeoutSec 4
    Write-Host ""
    Write-Host ((Tr "Model    : {0}" "Modèle   : {0}") -f $info.device.modelName)
    Write-Host ((Tr "Name     : {0}" "Nom      : {0}") -f $info.device.name)
    Write-Host ("DevMode  : {0}" -f $info.device.developerMode)
} catch {
    Write-Host (Tr "Model info unavailable (harmless)." "Info modèle indisponible (sans gravité).") -ForegroundColor DarkYellow
}

Write-Host ""
Write-Host (Tr "OK. You can now run the inventory (02) then the build (03)." "OK. Vous pouvez lancer l'inventaire (02) puis la compilation (03).") -ForegroundColor Green
