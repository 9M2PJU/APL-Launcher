# 04-install-launcher.ps1 — installs and starts the launcher on the TV.
# Uses the official developer channel (sdb/tizen). No root, no backdoor.
#
#   .\scripts\04-install-launcher.ps1 -TvIp 192.168.1.50
#   .\scripts\04-install-launcher.ps1 -TvIp 192.168.1.50 -NoLaunch
#
param(
    [Parameter(Mandatory = $true)][string]$TvIp,
    [string]$Wgt = (Join-Path (Split-Path $PSScriptRoot -Parent) "dist\APL-Launcher.wgt"),
    [string]$AppId = "APLauncher.Home",
    [string]$Sdb,
    [string]$TizenCli,
    [switch]$NoLaunch
)
. "$PSScriptRoot\lib.ps1"

if (-not (Test-Path -LiteralPath $Wgt)) { throw (Tr "Package not found: $Wgt. Run 03-build-launcher.ps1 first." "Paquet introuvable : $Wgt. Lancez d'abord 03-build-launcher.ps1.") }

$sdbPath = Find-Sdb $Sdb
& $sdbPath start-server | Out-Null
$serial = Connect-Tv -Sdb $sdbPath -Ip $TvIp

Write-Title (Tr "Installing the launcher" "Installation du launcher")
$tizen = $null
try { $tizen = Find-Tizen $TizenCli } catch { }

$installed = $false
$messages = ""
if ($tizen) {
    $r = Invoke-Native $tizen @('install', '-n', $Wgt, '-s', $serial)
    Write-Host $r.Output
    $messages = $r.Output
    if ($r.ExitCode -eq 0 -and $r.Output -notmatch 'fail') { $installed = $true }
}
if (-not $installed) {
    # 2nd attempt: the install command specific to Samsung screens.
    Invoke-Native $sdbPath @('-s', $serial, 'push', $Wgt, '/home/owner/share/tmp/sdk_tools/tmp/apl.wgt') | Out-Null
    $r = Invoke-Native $sdbPath @('-s', $serial, 'shell', '0', 'vd_appinstall', $AppId, '/home/owner/share/tmp/sdk_tools/tmp/apl.wgt')
    Write-Host $r.Output
    $messages += "`n" + $r.Output
}

# Real check: the app must be in the screen's list of apps.
$list = (Invoke-Native $sdbPath @('-s', $serial, 'shell', '0', 'vd_applist')).Output
if ($list -and $list -notmatch 'not found|unknown|error') {
    if ($list -notmatch [regex]::Escape($AppId)) {
        $why = ($messages -split "`n" | Where-Object { $_ -match 'reason|fail|error' }) -join ' | '
        throw (Tr "the screen did not install the app. Reason given by the screen: $why" "l'écran n'a pas installé l'app. Raison donnée par l'écran : $why")
    }
} elseif (-not $installed) {
    throw (Tr "the screen refused the package. Message: $messages" "l'écran a refusé le paquet. Message : $messages")
}
Write-Host (Tr "Installed." "Installé.") -ForegroundColor Green

if (-not $NoLaunch) {
    Write-Title (Tr "Starting $AppId" "Lancement de $AppId")
    try {
        Invoke-Native $sdbPath @('-s', $serial, 'shell', '0', 'was_execute', $AppId) | Out-Null
        if ($tizen) { Invoke-Native $tizen @('run', '-p', ($AppId -split '\.')[0], '-s', $serial) | Out-Null }
    } catch { }
    Write-Host (Tr "If the screen doesn't switch by itself, open the app from the Samsung home screen (Apps menu)." "Si l'écran ne bascule pas tout seul, ouvrez l'app depuis l'accueil Samsung (menu Apps).") -ForegroundColor DarkYellow
}

Write-Host ""
Write-Host (Tr "Done." "Terminé.") -ForegroundColor Green
