TreeSize-like disk usage analyzer for PowerToys Run and Windows Command Palette.

### Components

This release includes two separate tools:
- **PowerToys Run Plugin** (`ds` keyword in Alt+Space) â€” labeled *DiskAnalyzer (PowerToys Run)* in Command Palette
- **Command Palette Extension** (MSIX) â€” labeled *DiskAnalyzer* in Command Palette, no keyword needed

### Installation â€” PowerToys Run Plugin

1. Download `DiskAnalyzerInstaller-v1.2.0-x64.exe` (or `arm64`)
2. Exit PowerToys (right-click tray icon â†’ Exit)
3. Run the installer â€” it copies files to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer`
4. Restart PowerToys and enable the plugin in Settings â†’ PowerToys Run â†’ Plugins

### Installation â€” Command Palette Extension

1. Download `DiskAnalyzerCommandPalleteVersion_1.2.0.0_x64.msix` (or `arm64`)
2. Double-click the `.msix` file and click **Install**
3. Open Command Palette â€” *DiskAnalyzer* will appear as a top-level entry

### Usage â€” PowerToys Run

| Command | Description |
| :--- | :--- |
| `ds` | Show help and available commands |
| `ds drives` | List all drives with used/free/total space |
| `ds C:\Users` | Scan a folder and show subfolders by size |
| `ds largest C:\` | Find the largest files recursively |
| `ds top C:\` | Show top subfolders by size |
| `ds ext C:\ .mp4` | Find largest files by extension |
| `ds empty C:\` | Find empty folders |
| `ds gui` | Open the standalone GUI window |

### Usage â€” Command Palette Extension

| Command | Description |
| :--- | :--- |
| `drives` | List all drives |
| `top C:\` | Top folders ranked by size |
| `largest C:\` | Find largest files recursively |
| `ext C:\ .mp4` | Find files by extension |
| `empty C:\` | Find empty folders |
| `C:\Users` | Scan any folder path |

### Changes in v1.2.0

* **New**: Native Command Palette MSIX Extension â€” type commands directly without a keyword prefix
* **New**: Async background scanning in CmdPal â€” live *Scanningâ€¦* placeholder updates automatically
* **New**: Interactive drill-down in CmdPal â€” click results to navigate
* **New**: Standalone WPF GUI window (`ds gui`) with full tree explorer
* **New**: `ds ext` command â€” filter files by extension
* **New**: `ds empty` command â€” find empty folders
* **New**: ARM64 support â€” installer and MSIX for ARM64
* **New**: PowerToys Run plugin labeled *DiskAnalyzer (PowerToys Run)* in CmdPal
* **Fixed**: Disk used space now matches Windows Explorer exactly
* **Fixed**: Folder size calculation avoids reparse point loops

### Changes in v1.1.0

* **Feature:** Added "Allocated on Disk" size metrics to scan results
* **Performance:** Implemented parallel processing for deep directory scans
* **Update:** Upgraded target framework to `.NET 10.0`
* **Fix:** Corrected missing plugin icon in PowerToys Run settings

