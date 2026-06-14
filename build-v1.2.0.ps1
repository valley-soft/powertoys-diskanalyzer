$ErrorActionPreference = "Stop"

$PluginProject    = "Community.PowerToys.Run.Plugin.DiskAnalyzer.csproj"
$InstallerProject = "Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer.csproj"
$PayloadZip       = "Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer\payload.zip"
$CmdPalProject    = "CmdPalExtension\DiskAnalyzerExtension\DiskAnalyzerExtension\DiskAnalyzerExtension.csproj"
$CmdPalDir        = "CmdPalExtension\DiskAnalyzerExtension\DiskAnalyzerExtension"

Write-Host "========================================="
Write-Host "  Building DiskAnalyzer v1.2.0          "
Write-Host "========================================="

# Extract version from plugin.json
$pluginJson = Get-Content "plugin.json" -Raw | ConvertFrom-Json
$Version    = $pluginJson.Version
Write-Host "Version: v$Version"

$Architectures = @("x64", "arm64")

# Ensure output directories exist
foreach ($dir in @("out\CmdPal", "out\Installer", "out\CmdPal\MSIX")) {
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
}

foreach ($Arch in $Architectures) {
    $WinArch = "win-$Arch"

    Write-Host ""
    Write-Host "========================================="
    Write-Host "  Architecture: $Arch                  "
    Write-Host "========================================="

    # ── 1. Build PowerToys Run Plugin ──────────────────────────────────────
    Write-Host ""
    Write-Host "[1/4] Building PowerToys Run plugin ($Arch)..."
    if (Test-Path "temp_build") { Remove-Item "temp_build" -Recurse -Force }
    dotnet publish $PluginProject -c Release -p:Platform=$Arch -o "temp_build\$Arch"

    # ── 2. Package plugin payload for installer ─────────────────────────────
    Write-Host ""
    Write-Host "[2/4] Zipping plugin payload for installer..."
    if (Test-Path $PayloadZip) { Remove-Item $PayloadZip -Force }
    Compress-Archive -Path "temp_build\$Arch\*" -DestinationPath $PayloadZip

    # ── 3. Build standalone installer .exe ─────────────────────────────────
    Write-Host ""
    Write-Host "[3/4] Building standalone installer .exe ($Arch)..."
    if (Test-Path "temp_installer_build") { Remove-Item "temp_installer_build" -Recurse -Force }
    dotnet publish $InstallerProject -c Release -r $WinArch `
        -p:PublishSingleFile=true --self-contained true -p:PublishTrimmed=false `
        -o "temp_installer_build"

    $FinalExe = "out\Installer\DiskAnalyzerInstaller-v$($Version)-$Arch.exe"
    if (Test-Path $FinalExe) { Remove-Item $FinalExe -Force }
    Move-Item -Path "temp_installer_build\DiskAnalyzerInstaller.exe" -Destination $FinalExe -Force

    # ── 4. Build CmdPal MSIX extension ─────────────────────────────────────
    Write-Host ""
    Write-Host "[4/4] Building Command Palette MSIX extension ($Arch)..."
    Push-Location $CmdPalDir
    try {
        dotnet publish "DiskAnalyzerExtension.csproj" -c Release -r $WinArch `
            -p:GenerateAppxPackageOnBuild=true 2>&1 | Write-Host
    } finally {
        Pop-Location
    }

    # Locate the generated MSIX
    $msixSearch = Get-ChildItem -Path "$CmdPalDir\AppPackages" -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue |
                  Where-Object { $_.FullName -like "*$($Arch.Replace("arm64","arm64").Replace("x64","x64"))*" } |
                  Sort-Object LastWriteTime -Descending |
                  Select-Object -First 1

    if ($msixSearch) {
        $MsixDest = "out\CmdPal\MSIX\DiskAnalyzerExtension_CmdPal_v$($Version)_$Arch.msix"
        if (Test-Path $MsixDest) { Remove-Item $MsixDest -Force }
        Copy-Item -Path $msixSearch.FullName -Destination $MsixDest
        Write-Host "  MSIX copied -> $MsixDest"
    } else {
        Write-Warning "  Could not find generated MSIX for $Arch"
    }

    # Also produce an unpackaged ZIP (for manual sideloading)
    Write-Host "  Building unpackaged CmdPal ZIP ($Arch)..."
    if (Test-Path "temp_cmdpal_build") { Remove-Item "temp_cmdpal_build" -Recurse -Force }
    dotnet publish $CmdPalProject -c Release -r $WinArch -o "temp_cmdpal_build" 2>&1 | Out-Null

    $CmdPalZip = "out\CmdPal\DiskAnalyzerExtension_CmdPal_v$($Version)_$Arch.zip"
    if (Test-Path $CmdPalZip) { Remove-Item $CmdPalZip -Force }
    Compress-Archive -Path "temp_cmdpal_build\*" -DestinationPath $CmdPalZip -Force
    Write-Host "  ZIP copied -> $CmdPalZip"

    # Cleanup temp folders
    foreach ($tmp in @("temp_build", "temp_installer_build", "temp_cmdpal_build")) {
        if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
    }
}

# ── Local install (x64 MSIX) ───────────────────────────────────────────────
Write-Host ""
Write-Host "========================================="
Write-Host "  Installing x64 MSIX locally           "
Write-Host "========================================="

$LocalMsix = "out\CmdPal\MSIX\DiskAnalyzerExtension_CmdPal_v$($Version)_x64.msix"
if (Test-Path $LocalMsix) {
    $existing = Get-AppxPackage | Where-Object { $_.Name -like "*DiskAnalyzer*" }
    if ($existing) {
        Write-Host "Removing existing package: $($existing.PackageFullName)"
        Remove-AppxPackage -Package $existing.PackageFullName
        Start-Sleep -Seconds 2
    }
    Write-Host "Installing $LocalMsix ..."
    Add-AppxPackage -Path (Resolve-Path $LocalMsix).Path
    Write-Host "Local install complete."
} else {
    Write-Warning "x64 MSIX not found at $LocalMsix - skipping local install."
}

# ── Summary ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "========================================="
Write-Host "  SUCCESS!                              "
Write-Host "========================================="
Write-Host "PowerToys Run Installers:"
Write-Host "  -> out\Installer\DiskAnalyzerInstaller-v$Version-x64.exe"
Write-Host "  -> out\Installer\DiskAnalyzerInstaller-v$Version-arm64.exe"
Write-Host ""
Write-Host "Command Palette MSIX packages:"
Write-Host "  -> out\CmdPal\MSIX\DiskAnalyzerExtension_CmdPal_v$($Version)_x64.msix"
Write-Host "  -> out\CmdPal\MSIX\DiskAnalyzerExtension_CmdPal_v$($Version)_arm64.msix"
Write-Host ""
Write-Host "Command Palette unpackaged ZIPs:"
Write-Host "  -> out\CmdPal\DiskAnalyzerExtension_CmdPal_v$($Version)_x64.zip"
Write-Host "  -> out\CmdPal\DiskAnalyzerExtension_CmdPal_v$($Version)_arm64.zip"
