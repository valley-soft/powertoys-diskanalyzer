# DiskAnalyzer v1.1.0  Build & Package Script
$dir = $PSScriptRoot
$csproj = Get-ChildItem -Path $dir -Recurse -Filter "*.csproj" | Select-Object -First 1 -ExpandProperty FullName
Write-Host "Found project: $csproj" -ForegroundColor Cyan

Write-Host "==> Building x64..." -ForegroundColor Cyan
dotnet publish $csproj -c Release -r win-x64 --no-self-contained -o "$dir\out\x64"
if ($LASTEXITCODE -ne 0) { Write-Host "x64 build FAILED" -ForegroundColor Red; exit 1 }

Write-Host "==> Building ARM64..." -ForegroundColor Cyan
dotnet publish $csproj -c Release -r win-arm64 --no-self-contained -o "$dir\out\arm64"
if ($LASTEXITCODE -ne 0) { Write-Host "ARM64 build FAILED" -ForegroundColor Red; exit 1 }


# Remove PowerToys DLLs/PDBs bundled by dotnet publish (provided by host at runtime)
Write-Host "==> Removing unnecessary PowerToys DLLs..." -ForegroundColor Cyan
$ptDlls = @("PowerToys.Common.UI","PowerToys.ManagedCommon","PowerToys.Settings.UI.Lib","Wox.Infrastructure","Wox.Plugin")
foreach ($d in $ptDlls) {
    Remove-Item "$dir\out\x64\$d.dll","$dir\out\x64\$d.pdb" -ErrorAction SilentlyContinue
    Remove-Item "$dir\out\arm64\$d.dll","$dir\out\arm64\$d.pdb" -ErrorAction SilentlyContinue
}
Write-Host "==> Packaging ZIPs (files inside DiskAnalyzer/ subfolder)..." -ForegroundColor Cyan

# x64  stage files inside DiskAnalyzer/ so the zip has the required folder
$stageX64 = "$dir\stage\x64\DiskAnalyzer"
Remove-Item "$dir\stage\x64" -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stageX64 -Force | Out-Null
Copy-Item "$dir\out\x64\*" $stageX64 -Recurse -Force
Compress-Archive -Path "$dir\stage\x64\DiskAnalyzer" -DestinationPath "$dir\DiskAnalyzer-1.1.0-x64.zip" -Force

# ARM64  same staging approach
$stageARM64 = "$dir\stage\arm64\DiskAnalyzer"
Remove-Item "$dir\stage\arm64" -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stageARM64 -Force | Out-Null
Copy-Item "$dir\out\arm64\*" $stageARM64 -Recurse -Force
Compress-Archive -Path "$dir\stage\arm64\DiskAnalyzer" -DestinationPath "$dir\DiskAnalyzer-1.1.0-ARM64.zip" -Force

# Clean up staging folder
Remove-Item "$dir\stage" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "==> Installing to PowerToys..." -ForegroundColor Cyan
$dest = "$env:LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer"
Stop-Process -Name PowerToys -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $dest -Force | Out-Null
Copy-Item "$dir\out\x64\*" $dest -Recurse -Force

$ptoys = @(
    "$env:ProgramFiles\PowerToys\PowerToys.exe",
        "$env:LOCALAPPDATA\PowerToys\PowerToys.exe"
        ) | Where-Object { Test-Path $_ } | Select-Object -First 1
        if ($ptoys) { Start-Process $ptoys; Write-Host "PowerToys restarted." -ForegroundColor Green }
        else { Write-Host "PowerToys.exe not found  restart it manually." -ForegroundColor Yellow }

        Write-Host ""
        Write-Host "Done! DiskAnalyzer v1.1.0 installed." -ForegroundColor Green
        Get-Item "$dir\DiskAnalyzer-1.1.0-*.zip" | Format-Table Name, @{N="Size";E={"{0:N0} KB" -f ($_.Length/1KB)}}
