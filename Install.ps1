<#
.SYNOPSIS
    Installs the ValleySoft Disk Analyzer MSIX package on any Windows PC.

.DESCRIPTION
    Downloads the signing certificate from the same folder as the MSIX,
    trusts it in your Trusted People store, then sideloads the package.
    No manual certificate steps required.

.EXAMPLE
    # Run from the folder containing the .msix and .cer files:
    powershell -ExecutionPolicy Bypass -File Install.ps1

.NOTES
    Requires Windows 10 1809 or later.
    Developer Mode does NOT need to be enabled.
#>

$ErrorActionPreference = "Stop"


$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "========================================="
Write-Host "  ValleySoft Disk Analyzer - Installer   "
Write-Host "========================================="
Write-Host ""

# ── 1. Find the MSIX file ────────────────────────────────────────────────────
$msix = Get-ChildItem -Path $ScriptDir -Filter "ValleySoft.DiskAnalyzer.App_*.msix" |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $msix) {
    Write-Error "No ValleySoft.DiskAnalyzer.App_*.msix file found in $ScriptDir"
    exit 1
}
Write-Host "Found package: $($msix.Name)"

# ── 2. Trust the signing certificate ─────────────────────────────────────────
$cerPath = Join-Path $ScriptDir "ValleySoft.cer"
if (Test-Path $cerPath) {
    Write-Host "Trusting ValleySoft signing certificate..."
    Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" -ErrorAction SilentlyContinue | Out-Null
    & certutil -addstore -f "TrustedPeople" "$cerPath" 2>&1 | Out-Null
    Write-Host "Certificate trusted."
} else {
    Write-Warning "ValleySoft.cer not found. The installation may fail if the certificate is not already trusted."
}

# ── 3. Remove any existing version ───────────────────────────────────────────
Write-Host "Checking for existing installation..."
$existing = Get-AppxPackage -Name "ValleySoft.ValleySoftDiskAnalyzer" -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Removing existing version: $($existing.PackageFullName)"
    Remove-AppxPackage -Package $existing.PackageFullName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

# ── 4. Install ────────────────────────────────────────────────────────────────
Write-Host "Installing package..."
try {
    Add-AppxPackage -Path $msix.FullName -ForceApplicationShutdown
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host "  Successfully installed!                " -ForegroundColor Green
    Write-Host "  Launch: ValleySoft Disk Analyzer       " -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
} catch {
    Write-Host ""
    Write-Host "Installation failed: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "If you see a trust error, right-click ValleySoft.cer -> Install Certificate" -ForegroundColor Yellow
    Write-Host "-> Local Machine -> Trusted People, then re-run this script." -ForegroundColor Yellow
    exit 1
}
