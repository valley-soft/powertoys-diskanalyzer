# ValleySoft Disk Analyzer v1.4.0 Release Notes

TreeSize-like disk usage analyzer for PowerToys Run, Windows Command Palette, and Standalone WinUI 3 App.

---

## 🚀 What's New in Version 1.4.0

### 📁 1. Windows File Explorer Context Menu Integration
- **Direct Target Analysis**: Right-click any folder, drive, or folder background in Windows File Explorer and select **"Analyze with DiskAnalyzer"** to launch and directly analyze that target.
- **Official App Icon**: Context menu entry displays the official ValleySoft DiskAnalyzer icon (`AppIcon.ico`).

### ⭐ 2. Microsoft Store Rating Prompts
- **Unobtrusive Timed Prompt**: Non-intrusive WinUI 3 dialog prompts users to rate the app on the Microsoft Store after 3 completed scans.
- **Store Review Link**: Direct integration with `ms-windows-store://review/?ProductId=9NF073KLTVWN`.

### 🖼️ 3. True Executable File Icon Extraction
- **Native Win32 Shell Extraction**: Uses `ExtractIconEx` to load crisp, high-resolution embedded application icons for `.exe` and `.dll` files in the scan results DataGrid.

### 🎨 4. Command Palette Top-Level Shortcuts
- **3 Top-Level Commands**: Direct entries for **(Command Palette)**, **(Standalone App)**, and **(PowerToys Run)**.
- **In-Palette Drill-Down**: Subfolder exploration with an **"Up one level"** return item.

### 🛡️ 5. Zero-Crash & Zero-Hang Telemetry Overhaul
- **`MOAPPLICATION_HANG` Watchdog Fix**: Eliminated synchronous COM thread file logging during Command Palette activation.
- **WinUI Composition Exception Fix**: Resolved `ObservableCollection.ToList()` multi-threading race conditions on live UI grid renders.
- **Global Unhandled Exception Handlers**: Added `AppDomain` and `TaskScheduler` exception logging across all 3 applications.

### 📊 6. Admin-Resilient CSV Export & UI Polish
- **Native Win32 Save Dialog Fallback**: `comdlg32.dll` (`GetSaveFileName`) fallback guarantees CSV export works reliably under Administrator elevation and packaged MSIX UAC boundaries.
- **Dynamic Button State**: `Export CSV` button starts disabled (greyed out) on launch and while scanning, enabling automatically once a scan finishes.
- **Live Item Count Status Bar**: Bottom status bar displaying exact item counts (e.g. `212 items`).

---

## 📦 Release Assets (v1.4.0)

- **PowerToys Run Plugin Installer**: `ValleySoft.DiskAnalyzerInstaller-v1.4.0-x64.exe`
- **Standalone App & Command Palette Extension (MSIX)**: `ValleySoft.DiskAnalyzer.App_1.4.0-x64.msix`
- **ARM64 Installers & MSIX**: `ValleySoft.DiskAnalyzerInstaller-v1.4.0-arm64.exe` / `ValleySoft.DiskAnalyzer.App_1.4.0-arm64.msix`
- **Symbols Packages**: `ValleySoft.DiskAnalyzer.Symbols_1.4.0-x64.zip`
