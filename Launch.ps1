$ErrorActionPreference = "Stop"

# ── Self-elevate if not running as Administrator ─────────────────────────────

# 1. Run the build script
Write-Host "Running build-v1.4.0.ps1..."
& ".\build-v1.4.0.ps1"

# 2. Get the generated MSIX file path (on x64 platform)
$Version = "1.4.0"
$msixPath = Resolve-Path "out\App\ValleySoft.DiskAnalyzer.App_$($Version)_x64.msix" -ErrorAction SilentlyContinue
if (!$msixPath) {
    Write-Error "Could not find generated MSIX file!"
}

# 3. Clean up existing registration of the package
Write-Host "Checking for existing package registration..."
$existing = Get-AppxPackage -Name "ValleySoft.ValleySoftDiskAnalyzer"
if ($existing) {
    Write-Host "Removing older version: $($existing.PackageFullName)"
    Remove-AppxPackage -Package $existing.PackageFullName -ErrorAction SilentlyContinue
}

# Trust the signing certificate so Add-AppxPackage accepts the MSIX
# certutil works without requiring a separate elevated process
$cerPath = Resolve-Path "out\App\ValleySoft.cer" -ErrorAction SilentlyContinue
if ($cerPath) {
    Write-Host "Trusting signing certificate..."
    # CurrentUser\TrustedPeople (no elevation needed)
    Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" -ErrorAction SilentlyContinue | Out-Null
    # LocalMachine\TrustedPeople via certutil (works if process has sufficient rights)
    & certutil -addstore -f "TrustedPeople" "$cerPath" 2>&1 | Out-Null
    Write-Host "Certificate trusted."
}

# 4. Install the new MSIX package
Write-Host "Installing/Sideloading new package: $msixPath"
Add-AppxPackage -Path $msixPath -ForceApplicationShutdown

# 5. Refresh the Start Menu shell so the tile appears immediately
Write-Host "Refreshing Start Menu..."
Start-Sleep -Milliseconds 1500

# Notify the shell that the app list has changed
$code = @"
using System;
using System.Runtime.InteropServices;
public class ShellRefresh {
    [DllImport("shell32.dll")] public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    public static void Notify() { SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero); }
}
"@
Add-Type -TypeDefinition $code -Language CSharp
[ShellRefresh]::Notify()

# 6. Register Windows File Explorer Context Menu
Write-Host "Registering Windows Explorer Context Menu..."
$aliasPath = "$env:LOCALAPPDATA\Microsoft\WindowsApps\ValleySoft.DiskAnalyzer.exe"
$appDataFolder = "$env:LOCALAPPDATA\ValleySoft.DiskAnalyzer"
New-Item -ItemType Directory -Path $appDataFolder -Force | Out-Null
$iconPath = "$appDataFolder\AppIcon.ico"
if (Test-Path ".\Standalone App\ValleySoft.DiskAnalyzer.App\Assets\AppIcon.ico") {
    Copy-Item -Path ".\Standalone App\ValleySoft.DiskAnalyzer.App\Assets\AppIcon.ico" -Destination $iconPath -Force
}

New-Item -Path "HKCU:\Software\Classes\Directory\shell\ValleySoft.DiskAnalyzer\command" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\Directory\shell\ValleySoft.DiskAnalyzer" -Name "(default)" -Value "Analyze with DiskAnalyzer"
Set-ItemProperty -Path "HKCU:\Software\Classes\Directory\shell\ValleySoft.DiskAnalyzer" -Name "Icon" -Value $iconPath
Set-ItemProperty -Path "HKCU:\Software\Classes\Directory\shell\ValleySoft.DiskAnalyzer\command" -Name "(default)" -Value "`"$aliasPath`" --path `"%1`""

New-Item -Path "HKCU:\Software\Classes\Directory\Background\shell\ValleySoft.DiskAnalyzer\command" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\Directory\Background\shell\ValleySoft.DiskAnalyzer" -Name "(default)" -Value "Analyze with DiskAnalyzer"
Set-ItemProperty -Path "HKCU:\Software\Classes\Directory\Background\shell\ValleySoft.DiskAnalyzer" -Name "Icon" -Value $iconPath
Set-ItemProperty -Path "HKCU:\Software\Classes\Directory\Background\shell\ValleySoft.DiskAnalyzer\command" -Name "(default)" -Value "`"$aliasPath`" --path `"%V`""

New-Item -Path "HKCU:\Software\Classes\Drive\shell\ValleySoft.DiskAnalyzer\command" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\Drive\shell\ValleySoft.DiskAnalyzer" -Name "(default)" -Value "Analyze with DiskAnalyzer"
Set-ItemProperty -Path "HKCU:\Software\Classes\Drive\shell\ValleySoft.DiskAnalyzer" -Name "Icon" -Value $iconPath
Set-ItemProperty -Path "HKCU:\Software\Classes\Drive\shell\ValleySoft.DiskAnalyzer\command" -Name "(default)" -Value "`"$aliasPath`" --path `"%1`""

# 7. Update PowerToys Run Plugin
$ptRunPluginDir = "$env:LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer"
Write-Host "Publishing and updating PowerToys Run Plugin in $ptRunPluginDir..."
Stop-Process -Name "PowerToys.PowerLauncher", "PowerToys.Run" -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $ptRunPluginDir -Force | Out-Null
dotnet publish "Community.PowerToys.Run.Plugin.DiskAnalyzer.csproj" -c Release -p:Platform=x64 -o $ptRunPluginDir -v q

Write-Host ""
Write-Host "========================================="
Write-Host "  ValleySoft Disk Analyzer v1.4.0        "
Write-Host "  Successfully installed on your laptop! "
Write-Host "========================================="
Write-Host ""

