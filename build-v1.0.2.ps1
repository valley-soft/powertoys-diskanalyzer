# DiskAnalyzer v1.0.2 — Build & Install Script
$dir = "D:\Projects\Community.PowerToys.Run.Plugin.DiskAnalyzer"
$csproj = Get-ChildItem -Path $dir -Recurse -Filter "*.csproj" | Select-Object -First 1 -ExpandProperty FullName
Write-Host "Found project: $csproj" -ForegroundColor Cyan

Write-Host "==> Building x64..." -ForegroundColor Cyan
dotnet publish $csproj -c Release -r win-x64 --no-self-contained -o $dir\out\x64
if ($LASTEXITCODE -ne 0) { Write-Host "x64 build FAILED" -ForegroundColor Red; exit 1 }

Write-Host "==> Building ARM64..." -ForegroundColor Cyan
dotnet publish $csproj -c Release -r win-arm64 --no-self-contained -o $dir\out\arm64
if ($LASTEXITCODE -ne 0) { Write-Host "ARM64 build FAILED" -ForegroundColor Red; exit 1 }

Write-Host "==> Packaging ZIPs..." -ForegroundColor Cyan
Compress-Archive -Path $dir\out\x64\* -DestinationPath $dir\DiskAnalyzer-1.0.2-x64.zip -Force
Compress-Archive -Path $dir\out\arm64\* -DestinationPath $dir\DiskAnalyzer-1.0.2-ARM64.zip -Force

Write-Host "==> Installing to PowerToys..." -ForegroundColor Cyan
$dest = "$env:LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer"
Stop-Process -Name PowerToys -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $dest -Force | Out-Null
Copy-Item $dir\out\x64\* $dest -Recurse -Force

$ptoys = @(
    "$env:ProgramFiles\PowerToys\PowerToys.exe",
    "$env:LOCALAPPDATA\PowerToys\PowerToys.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($ptoys) { Start-Process $ptoys; Write-Host "PowerToys restarted." -ForegroundColor Green }
else { Write-Host "PowerToys.exe not found — restart it manually." -ForegroundColor Yellow }

Write-Host ""
Write-Host "Done! DiskAnalyzer v1.0.2 installed." -ForegroundColor Green
Get-Item $dir\DiskAnalyzer-1.0.2-*.zip | Format-Table Name, @{N="Size";E={"{0:N0} KB" -f ($_.Length/1KB)}}
