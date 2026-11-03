# 02b-cert-profile.ps1 — registers a Tizen signing profile from
# your existing Samsung certificates (author.p12 + distributor.p12).
#
# Run it ONLY ONCE (or if "tizen security-profiles list" is empty).
# The passwords are asked for with masked input and are never shown
# or written to disk by this script.
#
#   .\scripts\02b-cert-profile.ps1 -CertDir "C:\Users\<you>\SamsungCertificate\myscreen"
#
param(
    [Parameter(Mandatory = $true)][string]$CertDir,
    [string]$ProfileName = "APLauncher",
    [string]$Author = "author.p12",
    [string]$Distributor = "distributor.p12",
    [string]$TizenCli
)
. "$PSScriptRoot\lib.ps1"

$tizen = Find-Tizen $TizenCli
$authorP12 = Join-Path $CertDir $Author
$distP12   = Join-Path $CertDir $Distributor
if (-not (Test-Path $authorP12)) { throw (Tr "Not found: $authorP12" "Introuvable : $authorP12") }
if (-not (Test-Path $distP12))   { throw (Tr "Not found: $distP12" "Introuvable : $distP12") }

function Read-Plain([string]$prompt) {
    $sec = Read-Host -AsSecureString $prompt
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

Write-Title (Tr "Creating signing profile '$ProfileName'" "Création du profil de signature '$ProfileName'")
$authorPw = Read-Plain (Tr "AUTHOR certificate password (author.p12)" "Mot de passe du certificat AUTEUR (author.p12)")
$distPw   = Read-Plain (Tr "DISTRIBUTOR certificate password (distributor.p12)" "Mot de passe du certificat DISTRIBUTEUR (distributor.p12)")

& $tizen security-profiles add -n $ProfileName -a $authorP12 -p $authorPw -d $distP12 -dp $distPw
$authorPw = $null; $distPw = $null

Write-Host ""
Write-Host (Tr "Available profiles:" "Profils disponibles :") -ForegroundColor Cyan
& $tizen security-profiles list
Write-Host ""
Write-Host (Tr "Profile '$ProfileName' ready. You can build: 03-build-launcher.ps1" "Profil '$ProfileName' prêt. Vous pouvez compiler : 03-build-launcher.ps1") -ForegroundColor Green
