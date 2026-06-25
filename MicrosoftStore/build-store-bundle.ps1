<#
.SYNOPSIS
    Builds the MSIX packages and creates a unified .msixbundle for the Microsoft Store.
.DESCRIPTION
    This script compiles the standalone App (which includes the CmdPal Extension) 
    for both x64 and ARM64, and then uses MakeAppx to bundle them into a single 
    .msixbundle ready for upload to the Microsoft Partner Center.
#>

$ErrorActionPreference = "Stop"

$StandaloneDir = "..\Standalone App\ValleySoft.DiskAnalyzer.App"
$ProjectFile   = "$StandaloneDir\ValleySoft.DiskAnalyzer.App.csproj"

# Ensure Windows SDK tools (like makeappx.exe) are accessible
$makeAppxPath = (Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\makeappx.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName

if (-not $makeAppxPath) {
    Write-Error "Could not find makeappx.exe in Windows SDK kits. Ensure Windows SDK is installed."
    exit 1
}

Write-Host "MakeAppx found at: $makeAppxPath"

$Architectures = @("x64", "arm64")
$BundleMapping = "bundle_mapping.txt"

# Clear out any old data
if (Test-Path "bundle_mapping.txt") { Remove-Item "bundle_mapping.txt" -Force }
if (Test-Path "ValleySoft.DiskAnalyzer_StoreBundle.msixbundle") { Remove-Item "ValleySoft.DiskAnalyzer_StoreBundle.msixbundle" -Force }

# Clean AppPackages in target project to prevent stale builds
if (Test-Path "$StandaloneDir\AppPackages") {
    Remove-Item "$StandaloneDir\AppPackages" -Recurse -Force
}

$msixPaths = @()

foreach ($Arch in $Architectures) {
    $WinArch = "win-$Arch"

    Write-Host "`n========================================="
    Write-Host "  Building Store MSIX ($Arch)            "
    Write-Host "========================================="

    # We do NOT pass a test certificate password here, because for the actual Store, 
    # packages should either be unsigned (the Store signs them) or you use your actual 
    # Publisher certificate. The MSBuild target GenerateAppxPackageOnBuild will create it.
    dotnet publish $ProjectFile -c Release -r $WinArch -p:GenerateAppxPackageOnBuild=true -p:UapAppxPackageBuildMode=StoreUpload

    # Find the generated MSIX file
    $msixFile = Get-ChildItem -Path "$StandaloneDir\AppPackages" -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -like "*$Arch*" -and $_.FullName -notmatch "_Test" } |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1

    if (-not $msixFile) {
        Write-Error "Failed to find generated MSIX for $Arch in AppPackages directory."
        exit 1
    }

    Write-Host "Found MSIX for $Arch: $($msixFile.FullName)"
    $msixPaths += $msixFile
}

Write-Host "`nCreating bundle_mapping.txt..."
"[Files]" | Out-File $BundleMapping -Encoding UTF8
foreach ($file in $msixPaths) {
    "`"$($file.FullName)`" `"$($file.Name)`"" | Out-File $BundleMapping -Encoding UTF8 -Append
}

Write-Host "`nGenerating .msixbundle..."
& $makeAppxPath bundle /f $BundleMapping /p "ValleySoft.DiskAnalyzer_StoreBundle.msixbundle"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS! Your Store bundle is ready:" -ForegroundColor Green
    Write-Host "MicrosoftStore\ValleySoft.DiskAnalyzer_StoreBundle.msixbundle"
} else {
    Write-Error "Failed to create .msixbundle"
}
