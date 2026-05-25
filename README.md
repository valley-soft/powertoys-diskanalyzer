# DiskAnalyzer - PowerToys Run Plugin

A [PowerToys Run](https://aka.ms/PowerToysOverview) plugin that brings **disk usage analysis** directly into your launcher. Instantly explore drive and folder sizes without leaving your keyboard.

## Features

- List all drives with used / free / total space
- Browse any folder and see children ranked by size
- Recursively find the largest files and folders inside any path
- Accurate sizes for cloud folders (iCloud, OneDrive)
- Keyboard-first: clicking a result keeps the `ds` prefix so you can keep drilling down

## Requirements

- [Microsoft PowerToys](https://github.com/microsoft/PowerToys) v0.97.0 or later
- Windows 10 / 11 (x64 or ARM64)

## Installation

1. Download the zip for your architecture from [Releases](https://github.com/thetsaw/PowerToys.Plugin/releases/latest)
2. Close PowerToys
3. Extract the zip into `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer\`
4. Restart PowerToys
5. Open PowerToys Run with `Alt+Space` and type `ds ` to get started

## Usage

All commands begin with the `ds` keyword.

| Command | Result |
|---------|--------|
| `ds C:\` | List top-level folders sorted by size |
| `ds C:\Users\Photos` | Drill into any subfolder |
| `ds largest C:\Users` | Show the single largest item inside a path |

## Building from Source

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```
git clone https://github.com/thetsaw/PowerToys.Plugin.git
dotnet publish -c Release -r win-x64 --self-contained false -o publish/x64
dotnet publish -c Release -r win-arm64 --self-contained false -o publish/arm64
```

## License

MIT (c) [thetsaw](https://github.com/thetsaw)
