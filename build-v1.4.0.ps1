$ErrorActionPreference = "Stop"

$PluginProject    = "Community.PowerToys.Run.Plugin.DiskAnalyzer.csproj"
$InstallerProject = "Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer.csproj"
$PayloadZip       = "Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer\payload.zip"
$CmdPalProject    = "CmdPalExtension\DiskAnalyzerExtension\DiskAnalyzerExtension\DiskAnalyzerExtension.csproj"
$CmdPalDir        = "CmdPalExtension\DiskAnalyzerExtension\DiskAnalyzerExtension"

$StandaloneProject = "Standalone App\ValleySoft.DiskAnalyzer.App\ValleySoft.DiskAnalyzer.App.csproj"
$StandaloneDir     = "Standalone App\ValleySoft.DiskAnalyzer.App"

Write-Host "========================================="
Write-Host "  Building DiskAnalyzer v1.4.0          "
Write-Host "========================================="

Write-Host "Checking for ValleySoft certificate..."
if (-not (Test-Path "ValleySoft.pfx")) {
    Write-Host "Generating new self-signed certificate (CN=ValleySoft)..."
    $cert = New-SelfSignedCertificate -Type Custom -Subject "CN=ValleySoft" -KeyUsage DigitalSignature -FriendlyName "ValleySoft" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.7.1.5.5.7.3.3", "2.5.29.19={text}")
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
foreach ($dir in @("out\Installer", "out\App")) {
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
}

# Create a placeholder payload.zip so MSBuild can evaluate the EmbeddedResource at restore time.
# This will be overwritten with actual content during step 4 of each architecture build.
if (-not (Test-Path $PayloadZip)) {
    $placeholderDir = "temp_placeholder"
    New-Item -ItemType Directory -Path "$placeholderDir\placeholder" -Force | Out-Null
    Set-Content -Path "$placeholderDir\placeholder\placeholder.txt" -Value "placeholder"
    Compress-Archive -Path "$placeholderDir\*" -DestinationPath $PayloadZip -Force
    Remove-Item -Path $placeholderDir -Recurse -Force
    Write-Host "Created placeholder payload.zip for installer project evaluation."
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

    $msixSearchApp = Get-ChildItem -Path "$StandaloneDir\AppPackages" -Filter "ValleySoft.DiskAnalyzer.App_*.msix" -Recurse -ErrorAction SilentlyContinue |
                  Where-Object { $_.FullName -like "*$Arch*" } |
                  Sort-Object LastWriteTime -Descending |
                  Select-Object -First 1

    if ($msixSearchApp) {
        $msixPath = $msixSearchApp.FullName
        Write-Host "Signing MSIX using SignTool..."
        $thumbprint = (Get-PfxCertificate -FilePath "ValleySoft.cer").Thumbprint
        $signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
        if (Test-Path $signtool) {
            & $signtool sign /fd SHA256 /a /sha1 $thumbprint "$msixPath"
        } else {
            Write-Host "Warning: signtool.exe not found at $signtool"
        }
        
        # Copy MSIX to final output directory
        $appOutputDir = "out\App"
        Copy-Item -Path $msixPath -Destination "$appOutputDir\ValleySoft.DiskAnalyzer.App_$($Version)_$Arch.msix" -Force
        
        # ── Collect Debug Symbols (PDBs) ──
        Write-Host "Collecting debug symbols (PDBs)..."
        $publishBinDir = Get-ChildItem -Path "$StandaloneDir\bin" -Directory -Recurse -ErrorAction SilentlyContinue |
                         Where-Object { $_.FullName -like "*Release*win-$Arch*\publish" } |
                         Select-Object -First 1
        $symbolZip = "$appOutputDir\ValleySoft.DiskAnalyzer.Symbols_$($Version)_$Arch.zip"
        if (Test-Path $symbolZip) { Remove-Item $symbolZip -Force }
        
        if ($publishBinDir) {
            $pdbFiles = Get-ChildItem -Path $publishBinDir.FullName -Filter "*.pdb"
            if ($pdbFiles.Count -gt 0) {
                Compress-Archive -Path ($pdbFiles.FullName) -DestinationPath $symbolZip -Force
                Write-Host "Symbols zip packaged successfully at: $symbolZip"
            }
        }
    }

    if (Test-Path "ValleySoft.cer") {
        $appOutputDir = "out\App"
        Copy-Item -Path "ValleySoft.cer" -Destination "$appOutputDir\ValleySoft.cer" -Force
    }

    # -- 4. Package plugin payload for installer -------------------------────
    Write-Host ""
    Write-Host "[4/4] Zipping plugin payload for installer..."
    if (Test-Path $PayloadZip) { Remove-Item $PayloadZip -Force }
    # Verify temp_payload has content before zipping
    $payloadItems = Get-ChildItem -Path "temp_payload" -Recurse
    if ($payloadItems.Count -eq 0) {
        Write-Host "Warning: temp_payload is empty - skipping installer build for $Arch."
    } else {
        Compress-Archive -Path "temp_payload\*" -DestinationPath $PayloadZip -Force -ErrorAction Stop
        $payloadSizeMB = [Math]::Round((Get-Item $PayloadZip).Length / 1048576, 2)
        Write-Host ("Payload zip created at: " + $PayloadZip + " (" + $payloadSizeMB + " MB)")

        # -- 5. Build standalone installer .exe -------------------------
        Write-Host ""
        Write-Host "[5/5] Zipping payload and building installer ($Arch)..."
        if (Test-Path "temp_installer_build") { Remove-Item "temp_installer_build" -Recurse -Force }
        dotnet publish $InstallerProject -c Release -r $WinArch `
            -p:PublishSingleFile=true -p:SelfContained=false -p:PublishTrimmed=false `
            -o "temp_installer_build"

        $FinalExe = "out\Installer\ValleySoft.DiskAnalyzerInstaller-v$($Version)-$Arch.exe"
        if (Test-Path $FinalExe) { Remove-Item $FinalExe -Force }
        $installerExe = Get-ChildItem -Path "temp_installer_build" -Filter "DiskAnalyzerInstaller.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($installerExe) {
            Move-Item -Path $installerExe.FullName -Destination $FinalExe -Force
            Write-Host "Installer written to: $FinalExe"
        } else {
            Write-Host "Warning: DiskAnalyzerInstaller.exe not found in temp_installer_build. Skipping installer move."
        }
    }

    # Cleanup temp folders (keep payload.zip so next arch loop doesn't need a placeholder)
    foreach ($tmp in @("temp_payload", "temp_installer_build")) {
        if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
    }
}

Write-Host ""
Write-Host "========================================="
Write-Host "SUCCESS!"
Write-Host "========================================="
Write-Host "PowerToys Run Plugin Installer:"
Write-Host "  -> out\Installer\ValleySoft.DiskAnalyzerInstaller-v$Version-x64.exe"
Write-Host "Standalone App & Command Palette Extension MSIX:"
Write-Host "  -> out\App\ValleySoft.DiskAnalyzer.App_$Version-x64.msix"
Write-Host "Symbols Packaged:"
Write-Host "  -> out\App\ValleySoft.DiskAnalyzer.Symbols_$Version-x64.zip"
Write-Host ""
