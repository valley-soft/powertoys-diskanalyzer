TreeSize-like disk usage analyzer for PowerToys Run.

### Installation

1. Download the zip for your architecture (x64 or ARM64)
2. Extract to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer`
3. Restart PowerToys
4. Open PowerToys Run and type `ds`

### Usage

| Command | Description |
| :--- | :--- |
| `ds` | Show help and available commands |
| `ds drives` | List all drives with used/free/total space |
| `ds C:\Users` | Scan a folder and show subfolders by size |
| `ds largest C:\` | Find the largest files recursively |
| `ds top C:\` | Show top subfolders by size |

### Changes in v1.2.0

* **GUI:** Added standalone WPF window GUI via `ds gui` command
* **Installation:** Created custom C# Single-File native installer for Winget distribution
* **Search:** Added `ds ext` command to filter files by extension
* **Search:** Added `ds empty` command to find empty folders
* **Context Menu:** Added support to send files to the Recycle Bin

### Changes in v1.1.0

* **Feature:** Added "Allocated on Disk" size metrics to scan results alongside actual logical size.
* **Performance:** Implemented parallel processing for deep directory scans, significantly improving speed.
* **Update:** Upgraded target framework to `.NET 10.0` for latest PowerToys compatibility.
* **Fix:** Corrected an issue where the plugin icon was missing from the PowerToys Run settings menu.
