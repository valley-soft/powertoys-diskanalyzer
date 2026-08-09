<#
.SYNOPSIS
    One-time setup: generates a strong certificate password and stores it
    as a persistent user environment variable, then creates the signing certificate.

.DESCRIPTION
    Run this ONCE on any new machine before running build-v1.4.0.ps1.
    The password is never stored in any file — only in your Windows user profile.

.NOTES
    After running, open a NEW terminal window for the env var to be available.
#>

$ErrorActionPreference = "Stop"

# ── 1. Generate a cryptographically strong 32-char password ──────────────────
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$StrongPassword = [Convert]::ToBase64String($bytes).Substring(0, 32) -replace "[^A-Za-z0-9]", "X"

# ── 2. Persist to Windows user environment (survives reboots, never in files) ─
[System.Environment]::SetEnvironmentVariable("VALLEYSOFT_CERT_PASSWORD", $StrongPassword, "User")
$env:VALLEYSOFT_CERT_PASSWORD = $StrongPassword
Write-Host "VALLEYSOFT_CERT_PASSWORD set as a persistent user environment variable." -ForegroundColor Green
Write-Host "(Open a new terminal for other sessions to inherit this value.)"

# ── 3. Remove any stale certs and key files ──────────────────────────────────
$RepoRoot = $PSScriptRoot
Push-Location $RepoRoot
try {
    Remove-Item "ValleySoft.pfx", "ValleySoft.cer" -ErrorAction SilentlyContinue
    Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*ValleySoft*" } | Remove-Item -ErrorAction SilentlyContinue
    Get-ChildItem Cert:\CurrentUser\TrustedPeople | Where-Object { $_.Subject -like "*ValleySoft*" } | Remove-Item -ErrorAction SilentlyContinue

    # ── 4. Generate new certificate with strong password ─────────────────────
    Write-Host "Generating new self-signed certificate (CN=ValleySoft)..."
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject "CN=ValleySoft" `
        -KeyUsage DigitalSignature `
        -FriendlyName "ValleySoft DiskAnalyzer" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

    $securePassword = ConvertTo-SecureString -String $StrongPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath "ValleySoft.pfx" -Password $securePassword | Out-Null
    Export-Certificate  -Cert $cert -FilePath "ValleySoft.cer" | Out-Null

    # ── 5. Trust the certificate ──────────────────────────────────────────────
    Import-Certificate -FilePath "ValleySoft.cer" -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" | Out-Null
    try {
        Import-Certificate -FilePath "ValleySoft.cer" -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" -ErrorAction SilentlyContinue | Out-Null
    } catch {}

    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host "  Dev certificate setup complete!        " -ForegroundColor Green
    Write-Host "  Thumbprint: $($cert.Thumbprint)        " -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: Open a NEW terminal before running build-v1.4.0.ps1" -ForegroundColor Yellow
} finally {
    Pop-Location
}
