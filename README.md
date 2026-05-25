# DiskAnalyzer - PowerToys Run Plugin

[![Version](https://img.shields.io/badge/version-1.0.2-blue.svg)](https://github.com/thetsaw/PowerToys.Plugin/releases/latest)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://github.com/thetsaw/PowerToys.Plugin)
[![PowerToys](https://img.shields.io/badge/PowerToys-v0.97.0+-orange.svg)](https://github.com/microsoft/PowerToys)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://opensource.org/licenses/MIT)

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
5. Open PowerToys Run with `Alt+Space` and type `ds` to get started

## Usage

Open PowerToys Run (`Alt+Space`) and type `ds` followed by a command.

### Commands

| Command | Description |
|---------|-------------|
| `ds` | Show help and all available commands |
| `ds drives` | List all drives with used / free / total space and a usage bar |
| `ds C:\` | Scan a folder — shows subfolders and files sorted by size |
| `ds C:\Users\Photos` | Drill into any subfolder |
| `ds largest C:\` | Find the largest files recursively inside a path |
| `ds top C:\` | Show top-level subfolders ranked by total size |

### Context Menu (right-click any result)

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open in File Explorer |
| `Ctrl+C` | Copy path to clipboard |
| `Ctrl+Shift+C` | Copy size to clipboard |
| `Ctrl+Enter` | Drill down into the selected folder |
| `Ctrl+L` | Find largest files inside the selected folder |

### Tips

- Clicking a folder result automatically prefills `ds <path>` so you can keep drilling down without retyping
- Paths with spaces are supported — wrap them in quotes: `ds "C:\My Folder"`
- Results are cached for 10 seconds to avoid redundant re-scans

## Settings

Configure in PowerToys Settings → PowerToys Run → DiskAnalyzer.

| Setting | Default | Description |
|---------|---------|-------------|
| Maximum results | 15 | Number of items to display (5–50) |
| Default scan depth | 1 | How many levels deep to scan (1–5) |
| Include hidden files | Off | Include items with the Hidden attribute |
| Show percentage of parent | On | Display what % of the parent each item uses |

## Building from Source

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
git clone https://github.com/thetsaw/PowerToys.Plugin.git
dotnet publish -c Release -r win-x64 --self-contained false -o publish/x64
dotnet publish -c Release -r win-arm64 --self-contained false -o publish/arm64
```

## Version History

### v1.0.2 - 2026-05-24
- Updated target framework to net9.0-windows
- Updated Community.PowerToys.Run.Plugin.Dependencies to v0.97.0
- Compatible with PowerToys v0.97.0 and later

### v1.0.1
- Bug fixes and stability improvements

### v1.0.0
- Initial release
- List drives with used / free / total space
- Browse folder sizes ranked by largest
- Recursive largest file/folder search
- Cloud folder support (iCloud, OneDrive)


## License

[MIT](https://opensource.org/licenses/MIT) © [thetsaw](https://github.com/thetsaw)

