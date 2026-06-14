$ErrorActionPreference = "Stop"

$PluginProject = "Community.PowerToys.Run.Plugin.DiskAnalyzer.csproj"
$InstallerProject = "Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer.csproj"
$PayloadZip = "Installer\Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer\payload.zip"

Write-Host "========================================="
Write-Host " Building DiskAnalyzer Installer  "
Write-Host "========================================="

# Extract version from plugin.json automatically
$pluginJson = Get-Content "plugin.json" -Raw | ConvertFrom-Json
$Version = $pluginJson.Version
Write-Host "Detected Version: v$Version"

Write-Host "`n[1/4] Compiling the main DiskAnalyzer plugin (Release x64)..."
if (Test-Path "temp_build") { Remove-Item "temp_build" -Recurse -Force }
dotnet publish $PluginProject -c Release -p:Platform=x64 -o "temp_build\x64" | Out-Null

Write-Host "`n[2/4] Zipping the compiled payload..."
if (Test-Path $PayloadZip) { Remove-Item $PayloadZip -Force }
Compress-Archive -Path "temp_build\x64\*" -DestinationPath $PayloadZip

Write-Host "`n[3/4] Compiling the single-file Installer .exe..."
dotnet publish $InstallerProject -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -p:PublishTrimmed=false -o "out\Installer" | Out-Null

Write-Host "`n[4/4] Cleaning up temp files and renaming output..."
Remove-Item "temp_build" -Recurse -Force

# Rename the executable to include the version
$FinalExe = "out\Installer\DiskAnalyzerInstaller-v$Version-x64.exe"
if (Test-Path $FinalExe) { Remove-Item $FinalExe -Force }
Rename-Item -Path "out\Installer\DiskAnalyzerInstaller.exe" -NewName "DiskAnalyzerInstaller-v$Version-x64.exe"

Write-Host "`n========================================="
Write-Host " SUCCESS! "
Write-Host "========================================="
Write-Host "Your final standalone installer is ready:"
Write-Host "-> $FinalExe"
Write-Host "You can now distribute this single file via GitHub Releases and Winget!"
