# 03-build-launcher.ps1 — builds and signs the launcher into a .wgt package.
#
#   .\scripts\03-build-launcher.ps1 -ProfileName APLauncher
#   .\scripts\03-build-launcher.ps1 -ProfileName APLauncher -SfProDir "C:\Users\<you>\Downloads\sf-pro-display_2"
#
param(
    [string]$ProfileName = "APLauncher",
    [string]$TizenCli,
    [string]$LauncherDir = (Join-Path (Split-Path $PSScriptRoot -Parent) "launcher"),
    [string]$OutFile = (Join-Path (Split-Path $PSScriptRoot -Parent) "dist\APL-Launcher.wgt"),
    [string]$SfProDir   # optional: copies the SF Pro fonts before building
)
. "$PSScriptRoot\lib.ps1"

$tizen = Find-Tizen $TizenCli
if (-not (Test-Path (Join-Path $LauncherDir "config.xml"))) {
    throw (Tr "Project not found: $LauncherDir (no config.xml)." "Projet introuvable : $LauncherDir (pas de config.xml).")
}

# Optional copy of the SF Pro fonts (not included in the repository)
if ($SfProDir) {
    $fontsDst = Join-Path $LauncherDir "fonts"
    New-Item -ItemType Directory -Force -Path $fontsDst | Out-Null
    foreach ($f in @("SFPRODISPLAYREGULAR.OTF","SFPRODISPLAYMEDIUM.OTF","SFPRODISPLAYBOLD.OTF")) {
        $src = Join-Path $SfProDir $f
        if (Test-Path $src) { Copy-Item -LiteralPath $src -Destination $fontsDst -Force; Write-Host (Tr "Font copied: $f" "Police copiée : $f") }
        else { Write-Host (Tr "Font missing (skipped): $f" "Police absente (ignorée) : $f") -ForegroundColor DarkYellow }
    }
}

# Checks that the profile exists
$profiles = (Invoke-Native $tizen @('security-profiles', 'list')).Output
if (-not ($profiles -match [regex]::Escape($ProfileName))) {
    Write-Host (Tr "Warning: '$ProfileName' is not in the list of profiles, trying anyway." "Attention : '$ProfileName' n'apparaît pas dans la liste des profils, on tente quand même.") -ForegroundColor DarkYellow
}

Write-Title (Tr "Building the .wgt package (profile '$ProfileName')" "Compilation du paquet .wgt (profil '$ProfileName')")
# Removes an old .wgt from the project folder
Get-ChildItem -LiteralPath $LauncherDir -Filter *.wgt -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host (Tr "(If nothing moves for 2 minutes, close the window and try again.)" "(Si plus rien ne bouge pendant 2 minutes, ferme la fenêtre et réessaie.)") -ForegroundColor DarkGray
$r = Invoke-Native $tizen @('package', '-t', 'wgt', '-s', $ProfileName, '--', $LauncherDir)
Write-Host $r.Output
if ($r.ExitCode -ne 0 -or $r.Output -match 'fail|not exist|Invalid') { throw (Tr "The build failed. Tizen's message: $($r.Output)" "La compilation a échoué. Message de Tizen : $($r.Output)") }

$wgt = Get-ChildItem -LiteralPath $LauncherDir -Filter *.wgt | Select-Object -First 1
if (-not $wgt) { throw (Tr "No .wgt was produced." "Aucun .wgt produit.") }

$dir = Split-Path -Parent $OutFile
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
Copy-Item -LiteralPath $wgt.FullName -Destination $OutFile -Force

Write-Host ""
Write-Host (Tr "Package ready: $OutFile ($([math]::Round((Get-Item $OutFile).Length/1KB)) KB)" "Paquet prêt : $OutFile ($([math]::Round((Get-Item $OutFile).Length/1KB)) Ko)") -ForegroundColor Green
Write-Host (Tr "Install it with: .\scripts\04-install-launcher.ps1 -TvIp 192.168.1.50" "Installez-le avec : .\scripts\04-install-launcher.ps1 -TvIp 192.168.1.50")
