$ErrorActionPreference = "Stop"

$PluginProject    = "Community.PowerToys.Run.Plugin.DiskAnalyzer.csproj"
$InstallerProject = "Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer.csproj"
$PayloadZip       = "Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer\payload.zip"
$CmdPalProject    = "CmdPalExtension\DiskAnalyzerExtension\DiskAnalyzerExtension\DiskAnalyzerExtension.csproj"
$CmdPalDir        = "CmdPalExtension\DiskAnalyzerExtension\DiskAnalyzerExtension"

$StandaloneProject = "Standalone App\ValleySoft.DiskAnalyzer.App\ValleySoft.DiskAnalyzer.App.csproj"
$StandaloneDir     = "Standalone App\ValleySoft.DiskAnalyzer.App"

Write-Host "========================================="
Write-Host "  Building DiskAnalyzer v1.3.6          "
Write-Host "========================================="

Write-Host "Checking for ValleySoft certificate..."
if (-not (Test-Path "ValleySoft.pfx")) {
    Write-Host "Generating new self-signed certificate (CN=ValleySoft)..."
    $cert = New-SelfSignedCertificate -Type Custom -Subject "CN=ValleySoft" -KeyUsage DigitalSignature -FriendlyName "ValleySoft" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    $password = ConvertTo-SecureString -String "password" -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath "ValleySoft.pfx" -Password $password | Out-Null
    Export-Certificate -Cert $cert -FilePath "ValleySoft.cer" | Out-Null
}

Write-Host "Importing ValleySoft certificate to trusted stores..."
$password = ConvertTo-SecureString -String "password" -Force -AsPlainText
Import-PfxCertificate -FilePath "ValleySoft.pfx" -CertStoreLocation "Cert:\CurrentUser\My" -Password $password | Out-Null
Import-Certificate -FilePath "ValleySoft.cer" -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" | Out-Null

# Extract version from plugin.json
$pluginJson = Get-Content "plugin.json" -Raw | ConvertFrom-Json
$Version    = $pluginJson.Version
Write-Host "Version: v$Version"

$Architectures = @("x64", "arm64")

# Ensure output directories exist
foreach ($dir in @("out\Installer")) {
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
}

foreach ($Arch in $Architectures) {
    $WinArch = "win-$Arch"

    Write-Host ""
    Write-Host "========================================="
    Write-Host "  Architecture: $Arch                  "
    Write-Host "========================================="

    # Create master payload directory
    if (Test-Path "temp_payload") { Remove-Item "temp_payload" -Recurse -Force }
    New-Item -ItemType Directory -Path "temp_payload\Plugin" -Force | Out-Null

    # ── 1. Build PowerToys Run Plugin ──────────────────────────────────────
    Write-Host ""
    Write-Host "[1/4] Building PowerToys Run plugin ($Arch)..."
    dotnet publish $PluginProject -c Release -p:Platform=$Arch -o "temp_payload\Plugin"

    # CmdPal logic has been merged into the unified Standalone App package.



    # ── 3. Build Unified App & CmdPal MSIX ───────────────────────────────────────
    Write-Host ""
    Write-Host "[3/4] Building Unified App & CmdPal MSIX ($Arch)..."
    Push-Location $StandaloneDir
    try {
        dotnet publish "ValleySoft.DiskAnalyzer.App.csproj" -c Release -r $WinArch `
            -p:GenerateAppxPackageOnBuild=true -p:PackageCertificatePassword=password 2>&1 | Write-Host
    } finally {
        Pop-Location
    }

    $msixSearchApp = Get-ChildItem -Path "$StandaloneDir\AppPackages" -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue |
                  Where-Object { $_.FullName -like "*$Arch*" } |
                  Sort-Object LastWriteTime -Descending |
                  Select-Object -First 1

    if ($msixSearchApp) {
        $msixPath = $msixSearchApp.FullName
        Write-Host "Signing MSIX using SignTool and Certificate Thumbprint from ValleySoft.cer..."
        $thumbprint = (Get-PfxCertificate -FilePath "ValleySoft.cer").Thumbprint
        $signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
        if (Test-Path $signtool) {
            & $signtool sign /fd SHA256 /a /sha1 $thumbprint $msixPath
        } else {
            Write-Host "Warning: signtool.exe not found at $signtool"
        }
        
        # Copy MSIX to final output directory instead of installer payload
        $appOutputDir = "out\App"
        if (!(Test-Path $appOutputDir)) { New-Item -ItemType Directory -Path $appOutputDir -Force | Out-Null }
        Copy-Item -Path $msixPath -Destination "$appOutputDir\ValleySoft.DiskAnalyzer.App_$($Version)_$Arch.msix" -Force
    }

    if (Test-Path "ValleySoft.cer") {
        $appOutputDir = "out\App"
        if (!(Test-Path $appOutputDir)) { New-Item -ItemType Directory -Path $appOutputDir -Force | Out-Null }
        Copy-Item -Path "ValleySoft.cer" -Destination "$appOutputDir\ValleySoft.cer" -Force
    }

    # ── 4. Package plugin payload for installer ───────────────────────────
    Write-Host ""
    Write-Host "[4/4] Zipping plugin payload for installer..."
    if (Test-Path $PayloadZip) { Remove-Item $PayloadZip -Force }
    Compress-Archive -Path "temp_payload\*" -DestinationPath $PayloadZip

    # ── 5. Build standalone installer .exe ─────────────────────────────────
    Write-Host ""
    Write-Host "[5/5] Building unified installer .exe ($Arch)..."
    if (Test-Path "temp_installer_build") { Remove-Item "temp_installer_build" -Recurse -Force }
    dotnet publish $InstallerProject -c Release -r $WinArch `
        -p:PublishSingleFile=true --self-contained false -p:PublishTrimmed=false `
        -o "temp_installer_build"

    $FinalExe = "out\Installer\ValleySoft.DiskAnalyzerInstaller-v$($Version)-$Arch.exe"
    if (Test-Path $FinalExe) { Remove-Item $FinalExe -Force }
    Move-Item -Path "temp_installer_build\DiskAnalyzerInstaller.exe" -Destination $FinalExe -Force

    # Cleanup temp folders
    foreach ($tmp in @("temp_payload", "temp_installer_build")) {
        if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
    }
}

# ── Summary ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "========================================="
Write-Host "SUCCESS!"
Write-Host "========================================="
Write-Host "PowerToys Run Plugin Installer:"
Write-Host "  -> out\Installer\ValleySoft.DiskAnalyzerInstaller-v$Version-x64.exe"
Write-Host "  -> out\Installer\ValleySoft.DiskAnalyzerInstaller-v$Version-arm64.exe"
Write-Host "Standalone App & Command Palette Extension MSIX:"
Write-Host "  -> out\App\ValleySoft.DiskAnalyzer.App_$Version-x64.msix"
Write-Host "  -> out\App\ValleySoft.DiskAnalyzer.App_$Version-arm64.msix"
Write-Host "Certificate:"
Write-Host "  -> out\App\ValleySoft.cer"
Write-Host ""
