# DiskAnalyzer â€” PowerToys Run Plugin & Command Palette Extension

[![Version](https://img.shields.io/badge/version-1.2.0-blue.svg)](https://github.com/valley-soft/powertoys-diskanalyzer/releases/latest)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://github.com/valley-soft/powertoys-diskanalyzer)
[![PowerToys](https://img.shields.io/badge/PowerToys-v0.97.0+-orange.svg)](https://github.com/microsoft/PowerToys)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://opensource.org/licenses/MIT)
[![Downloads](https://img.shields.io/github/downloads/valley-soft/powertoys-diskanalyzer/total.svg)](https://github.com/valley-soft/powertoys-diskanalyzer/releases)

A [PowerToys Run](https://aka.ms/PowerToysOverview) plugin **and** a native Windows Command Palette extension that brings **TreeSize-like disk usage analysis** directly into your launcher â€” plus a full standalone GUI window. Instantly explore drive and folder sizes without leaving your keyboard.

---

## Components

This project ships two separate tools that work together:

| Component | Where it appears | How to trigger |
|-----------|-----------------|----------------|
| **PowerToys Run Plugin** | PowerToys Run (`Alt+Space`) and Command Palette (labeled *DiskAnalyzer (PowerToys Run)*) | Type `ds` keyword |
| **Command Palette Extension** | Windows Command Palette (labeled *ValleySoft Disk Analyzer*) | Open CmdPal and type a command directly |

---

## Features

### PowerToys Run Plugin (`ds` keyword)
- ðŸ–¥ï¸ List all drives with used / free / total space and a visual usage bar
- ðŸ“‚ Browse any folder â€” subfolders and files ranked by size
- ðŸ” Recursively find the largest files inside any path
- ðŸ“Š Show top-level subdirectories ranked by total size
- ðŸ”Ž Filter files by extension (e.g. find all `.mp4` files)
- ðŸ“ Find empty folders inside any path
- ðŸªŸ Launch the full standalone **GUI window** with one command
- ðŸ“‹ Context menu: open in Explorer, copy path, copy size, drill down, find largest files
- âš¡ Results cached for 10 seconds â€” no redundant re-scans

### Command Palette Extension (native CmdPal)
- ðŸ–¥ï¸ Type commands directly â€” no keyword prefix needed
- ðŸ“‚ Async background scanning â€” shows a *Scanningâ€¦* placeholder while working
- ðŸ”„ Results appear automatically when the scan finishes
- ðŸ–±ï¸ Click any result to drill down interactively
- ðŸ“‹ Context menu: copy path, copy size, open in Explorer, drill into subfolders

---

## PowerToys Run Commands

![Help commands overview](Images/ptrun-help-commands.png)

![Advanced commands: largest, top, ext, empty](Images/ptrun-advanced-commands.png)

### Scanning Folders

![Scanning top-level folders on C:\](Images/ptrun-top-folders.png)

![Scanning C:\WINDOWS folder](Images/ptrun-folder-scan.png)

### Standalone GUI Window (`ds gui`)

![GUI â€” all drives overview with C:\ selected](Images/gui-drive-overview.png)

![GUI â€” drilled into C:\WINDOWS](Images/gui-drill-down.png)

- ðŸ—‚ï¸ Left pane: expandable tree view of all your drives and folders
- ðŸ“‹ Right pane: sortable table showing Name, Size, Allocated, Items, Modified date
- â¬…ï¸ **Back button** â€” navigate back through your browsing history
- ðŸ“‚ **Browse button** â€” open any folder with a standard folder picker dialog
- ðŸ”„ **Refresh button** â€” rescan the current folder
- ðŸ–±ï¸ **Double-click** a folder row to drill down into it
- ðŸ–±ï¸ **Double-click** a file row to reveal it in File Explorer
- âœ… Sizes sorted correctly by bytes â€” not alphabetically
- ðŸ”’ Safe: no delete functionality â€” read-only tool

---

## Requirements

- [Microsoft PowerToys](https://github.com/microsoft/PowerToys) v0.97.0 or later
- Windows 10 / 11 (x64 or ARM64)
- .NET 10 Runtime (included with PowerToys)

---

## Installation

### PowerToys Run Plugin

#### Method 1 â€” Standalone Installer (Recommended)

1. Download **`DiskAnalyzerInstaller-v1.2.0-x64.exe`** (or `arm64`) from [Releases](https://github.com/valley-soft/powertoys-diskanalyzer/releases/latest)
2. **Exit PowerToys completely** â€” Right-click the PowerToys icon in the system tray â†’ **Exit**
3. **Run the installer** â€” it will automatically extract and copy plugin files to:
   ```
   %LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer\
   ```
4. **Restart PowerToys** from the Start menu
5. **Enable the plugin** â€” Open PowerToys Settings â†’ PowerToys Run â†’ Plugins â†’ find **DiskAnalyzer** â†’ toggle **ON**

#### Method 2 â€” Manual (ZIP)

1. Download the ZIP from [Releases](https://github.com/valley-soft/powertoys-diskanalyzer/releases/latest)
2. Exit PowerToys completely
3. Extract the ZIP to:
   ```
   %LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer\
   ```
4. Restart PowerToys and enable the plugin in Settings

---

### Command Palette Extension (MSIX)

1. Download **`DiskAnalyzerCommandPalleteVersion_1.2.0.0_x64.msix`** (or `arm64`) from [Releases](https://github.com/valley-soft/powertoys-diskanalyzer/releases/latest)
2. **Double-click** the `.msix` file â€” Windows will launch the installer
3. Click **Install**
4. Open the **Windows Command Palette** â€” *ValleySoft Disk Analyzer* will appear as a top-level entry

> **Note:** The MSIX package is self-signed for development/sideloading. You may need to enable **Developer Mode** in Windows Settings â†’ System â†’ For developers, or trust the certificate manually.

---

### Method 3 â€” Build from Source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
git clone https://github.com/valley-soft/powertoys-diskanalyzer.git
cd powertoys-diskanalyzer
.\build-v1.2.0.ps1
# Installers and MSIX packages appear in out\
```

---

## Usage

### PowerToys Run Plugin

Open PowerToys Run (`Alt+Space`) and type `ds` followed by a command.

| Command | Description |
|---------|-------------|
| `ds` | Show help and all available commands |
| `ds drives` | List all drives with used / free / total space and a usage bar |
| `ds C:\` | Scan a folder â€” shows subfolders and files sorted by size |
| `ds C:\Users\Photos` | Drill into any subfolder |
| `ds largest C:\` | Find the largest files recursively inside a path |
| `ds top C:\` | Show top-level subfolders ranked by total size |
| `ds ext C:\ .mp4` | Find the largest files of a specific extension |
| `ds empty C:\` | Find empty folders inside a path |
| `ds gui` | Open the full standalone GUI window |
| `ds gui C:\Users` | Open the GUI window pre-navigated to a specific folder |

#### Context Menu (right-click / `>` on any result)

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open in File Explorer |
| `Ctrl+C` | Copy path to clipboard |
| `Ctrl+Shift+C` | Copy size to clipboard |
| `Ctrl+Enter` | Drill down into the selected folder |
| `Ctrl+L` | Find largest files inside the selected folder |

#### Tips

- Clicking a folder result automatically prefills `ds <path>` so you can keep drilling down
- Paths with spaces are supported â€” wrap them in quotes: `ds "C:\My Folder"`
- Results are cached for 10 seconds to avoid redundant re-scans
- The `ds gui` window works independently â€” you can close PowerToys Run after launching it

---

### Command Palette Extension

Open the Windows Command Palette and click **ValleySoft Disk Analyzer** (or search for it). Then type directly:

| Command | Description |
|---------|-------------|
| `drives` | List all drives with used / free / total space |
| `top C:\` | Top-level folders ranked by size |
| `largest C:\` | Find the largest files recursively |
| `ext C:\ .mp4` | Find largest files of a specific extension |
| `empty C:\` | Find empty folders |
| `C:\Users` | Scan any absolute folder path â€” ranked by size |

Results appear **as you type** â€” scanning runs in the background with a *Scanningâ€¦* placeholder and updates automatically when done. Click any result to drill down interactively.

---

## GUI Window â€” How to Use

Launch with `ds gui` from PowerToys Run, then press Enter.

| Action | How |
|--------|-----|
| Browse drives | All drives appear in the left tree on startup |
| Expand a drive/folder | Click the â–¶ arrow in the left tree |
| View folder contents | Click any drive or folder in the left tree |
| Drill into a subfolder | **Double-click** any folder row in the right grid |
| Reveal a file | **Double-click** any file row â€” opens File Explorer |
| Go back | Click **â† Back** button |
| Pick any folder | Click **Browse...** button |
| Rescan current view | Click **Refresh** button |
| Sort columns | Click any column header (Size/Allocated sort by bytes correctly) |

---

## Settings

Configure in PowerToys Settings â†’ PowerToys Run â†’ DiskAnalyzer.

| Setting | Default | Description |
|---------|---------|-------------|
| Maximum results | 15 | Number of items to display (5â€“50) |
| Default scan depth | 1 | How many levels deep to scan (1â€“5) |
| Include hidden files | Off | Include items with the Hidden attribute |
| Show percentage of parent | On | Display what % of the parent each item uses |

---

## Project Structure

| File / Folder | Purpose |
|---------------|---------| 
| `Main.cs` | Plugin entry point â€” handles queries, results, and context menus |
| `DiskAnalyzerHelper.cs` | File system scanning, size calculation, progress bars |
| `DiskAnalyzerWindow.xaml` / `.cs` | Standalone GUI window (WPF) |
| `DiskItemInfo.cs` | Data model for scanned files/folders |
| `plugin.json` | PowerToys metadata (name, keyword, version, icons) |
| `Images/` | Plugin icon assets and README screenshots |
| `CmdPalExtension/` | Native Command Palette MSIX extension project |
| `Installer/` | Single-file native installer source |
| `build-v1.2.0.ps1` | Build script â€” compiles PT Run plugin + CmdPal MSIX for x64 & ARM64, installs locally |
| `out/` | Final output: installer `.exe` files, CmdPal `.msix` packages, and ZIPs |

---

## Version History

### v1.2.0 â€” 2026-06-14
- âœ¨ **New**: Native **Command Palette MSIX Extension** â€” type commands directly in CmdPal without a keyword
  - Async background scanning with live *Scanningâ€¦* placeholder
  - Interactive drill-down by clicking results
  - Supports `drives`, `top`, `largest`, `ext`, `empty`, and any folder path
- âœ¨ **New**: Standalone GUI window (`ds gui`) with full drive/folder tree explorer
  - Expandable left tree pane showing all drives
  - Right grid with sortable Name, Size, Allocated, Items, Modified columns
  - Double-click to drill down into folders
  - Double-click files to reveal in File Explorer
  - â† Back navigation, Browse folder picker, Refresh
  - Sizes sort correctly by bytes (not alphabetically)
- âœ¨ **New**: `ds ext <path> <extension>` â€” find largest files by extension
- âœ¨ **New**: `ds empty <path>` â€” find empty folders
- âœ¨ **New**: ARM64 support â€” separate installer and MSIX for ARM64 devices
- âœ¨ **New**: PowerToys Run plugin labeled *DiskAnalyzer (PowerToys Run)* in CmdPal to distinguish from the MSIX extension
- ðŸ› ï¸ **Fixed**: Disk used space now matches Windows Explorer exactly
- ðŸ› ï¸ **Fixed**: Folder size calculation uses queue-based BFS (avoids reparse points)
- ðŸ“¦ **New**: Single-file native `.exe` installer for both x64 and ARM64
- ðŸ“¦ **New**: MSIX packages for Command Palette extension (x64 and ARM64)

### v1.1.0 â€” 2026-06-10
- Updated target framework to net10.0-windows
- Fixed missing plugin icons in PowerToys settings
- Added allocated on-disk size to scan results
- Improved scanning performance with parallel processing

### v1.0.2 â€” 2026-05-24
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

---

## License

[MIT](https://opensource.org/licenses/MIT) Â© [ValleySoft](https://github.com/valley-soft)
