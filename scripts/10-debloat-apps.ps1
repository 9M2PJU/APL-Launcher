# 10-debloat-apps.ps1 — uninstalls optional THIRD-PARTY applications through
# developer mode (no root). SAFE:
#   * simulation mode by default (removes nothing until -Apply is passed);
#   * flatly refuses to touch the system (com.samsung.*, org.tizen.*);
#   * whitelist of the apps you keep, always protected.
#
#   # 1) See what would be removed (does NOTHING):
#   .\scripts\10-debloat-apps.ps1 -TvIp 192.168.1.50
#
#   # 2) Really apply:
#   .\scripts\10-debloat-apps.ps1 -TvIp 192.168.1.50 -Apply
#
param(
    [Parameter(Mandatory = $true)][string]$TvIp,
    [string[]]$Targets,
    [switch]$Apply,
    [string]$Sdb
)
. "$PSScriptRoot\lib.ps1"

# Apps you told us you use — NEVER deleted.
$KEEP = @(
    "9Ur5IzDKqV.TizenYouTube",         # Samsung's YouTube (Optimize can remove it)
    "xvvl3S1bvH.TizenBrewStandalone",  # TizenBrew
    "xvvl3S1bvH.StandaloneService",    # TizenBrew service
    "111299001912",                    # official YouTube (TizenTube uses it)
    "TSLinkTV01.TizenSteamLink",       # Steam Link
    "MoonLightS.MoonlightWasm",        # Moonlight
    "6YLB5zo9te.HotIptv",              # Hot IPTV
    "org.tizen.browser"               # Samsung Internet
)

# Default targets: alternative third-party players (edit to suit your needs).
if (-not $Targets -or $Targets.Count -eq 0) {
    $Targets = @(
        "IXimNTGMZ4.OTTPlayer",
        "yr6faEBKiv.XCIPTVPlayer",
        "To6gi5fDeT.IBOPlayerTV",
        "O1oM4DEEIN.SmartIPTV",
        "dUas1Su1Gh.MaxPlayer",
        "IkWsFHtOY9.Stremio",
        "kIciSQlYEM.plex"
    )
}

$sdbPath = Find-Sdb $Sdb
& $sdbPath start-server | Out-Null
$serial = Connect-Tv -Sdb $sdbPath -Ip $TvIp

Write-Title ($(if ($Apply) { Tr "Uninstalling (REAL)" "Désinstallation (RÉEL)" } else { Tr "Simulation (nothing is deleted)" "Simulation (aucune suppression)" }))

foreach ($pkg in $Targets) {
    # Safeguards
    if ($KEEP -contains $pkg) { Write-Host (Tr "PROTECTED (kept)  : $pkg" "PROTÉGÉ (gardé)   : $pkg") -ForegroundColor Green; continue }
    if ($pkg -match '^(com\.samsung|org\.tizen)') {
        Write-Host (Tr "REFUSED (system)  : $pkg" "REFUSÉ (système)  : $pkg") -ForegroundColor Red; continue
    }

    if (-not $Apply) {
        Write-Host (Tr "To remove         : $pkg" "À retirer         : $pkg") -ForegroundColor Yellow
        continue
    }

    try {
        $out = (Invoke-Native $sdbPath @('-s', $serial, 'uninstall', $pkg)).Output
        if ($out -match 'fail|error|not installed') {
            Write-Host (Tr "Skipped           : $pkg ($out)" "Ignoré            : $pkg ($out)") -ForegroundColor DarkYellow
        } else {
            Write-Host (Tr "Deleted           : $pkg" "Supprimé          : $pkg") -ForegroundColor Green
        }
    } catch {
        Write-Host (Tr "Failed            : $pkg ($_)" "Échec             : $pkg ($_)") -ForegroundColor DarkYellow
    }
}

Write-Host ""
if (-not $Apply) {
    Write-Host (Tr "This was a SIMULATION. Run it again with -Apply to really delete." "Ceci était une SIMULATION. Relancez avec -Apply pour supprimer réellement.") -ForegroundColor Cyan
} else {
    Write-Host (Tr "An app deleted by mistake can be reinstalled from the Samsung Apps Store." "Une app supprimée par erreur se réinstalle depuis le Samsung Apps Store.") -ForegroundColor Cyan
}
Write-Host (Tr "For the Samsung ADS themselves, use the DNS list: extras\dns-blocklist-samsung.txt" "Pour les PUBS Samsung elles-mêmes, utilisez la liste DNS : extras\dns-blocklist-samsung.txt") -ForegroundColor Cyan
