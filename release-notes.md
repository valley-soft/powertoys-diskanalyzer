TreeSize-like disk usage analyzer for PowerToys Run and Windows Command Palette.

### Components

This release includes two separate tools:
- **PowerToys Run Plugin** (ds keyword in Alt+Space) — labeled *DiskAnalyzer (PowerToys Run)* in Command Palette
- **Command Palette Extension** (MSIX) — labeled *DiskAnalyzer* in Command Palette, no keyword needed

### Installation — Standalone App (Microsoft Store)

The full standalone GUI version of Disk Analyzer is officially available on the Microsoft Store. This is the recommended way to install and keep the app automatically updated!

[![Get it from Microsoft](https://get.microsoft.com/images/en-us%20dark.svg)](https://apps.microsoft.com/detail/9nf073kltvwn?hl=en-US&gl=US)

Alternatively, install via the command line:
winget install --id 9NF073KLTVWN --source msstore

### Installation — PowerToys Run Plugin

1. Download ValleySoft.DiskAnalyzerInstaller-v1.3.0-x64.exe (or rm64)
2. Exit PowerToys (right-click tray icon ? Exit)
3. Run the installer — it will flawlessly clean install to %LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer
4. Restart PowerToys and enable the plugin in Settings ? PowerToys Run ? Plugins

### Installation — Command Palette Extension

1. Download DiskAnalyzerExtension_1.3.0.0_x64.msix (or rm64)
2. Double-click the .msix file and click **Install**
3. Open Command Palette — *DiskAnalyzer* will appear as a top-level entry

### Usage

| Command | Description |
| :--- | :--- |
| ds drives | List all drives |
| ds top C:\ | Top folders ranked by size |
| ds largest C:\ | Find largest files recursively |
| ds ext C:\ .mp4 | Find files by extension |
| ds empty C:\ | Find empty folders |
| ds gui | Open the standalone GUI window |

### Changes in v1.3.0

* **New**: Fully featured Standalone WinUI 3 App with interactive Visual Charts!
* **New**: Unified Installer features a flawless 1-click Clean Install mode
* **Fixed**: PowerToys Run AssemblyLoadContext bug completely resolved by natively compiling core logic into the plugin
* **Fixed**: Standalone App sizes and calculations rigorously synced with Windows Explorer

### Changes in v1.2.0

* **New**: Native Command Palette MSIX Extension — type commands directly without a keyword prefix
* **New**: Standalone WPF GUI window (ds gui) with full tree explorer
* **New**: ds ext command — filter files by extension
* **New**: ds empty command — find empty folders
* **New**: ARM64 support — installer and MSIX for ARM64
* **New**: PowerToys Run plugin labeled *DiskAnalyzer (PowerToys Run)* in CmdPal
* **Fixed**: Disk used space now matches Windows Explorer exactly
* **Fixed**: Folder size calculation avoids reparse point loops
