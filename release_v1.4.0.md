# ValleySoft Disk Analyzer v1.4.0 Release Notes

TreeSize-like disk usage analyzer for PowerToys Run, Windows Command Palette, and Standalone WinUI 3 App.

---

## 🚀 What's New in Version 1.4.0

### 🟢 What Got Added:
- **📁 Windows Explorer Context Menu**: Right-click any folder, drive, or folder background in Windows Explorer and select **"Analyze with DiskAnalyzer"** to launch directly into target scan view (featuring the official app icon!).
- **⭐ Microsoft Store Rating Prompts**: Timed, professional WinUI 3 dialog after 3 completed scans to rate the app on the Store.
- **🖼️ Crisp Executable Icon Extraction**: Native Win32 `ExtractIconEx` shell extraction to display real high-res icons for `.exe` and `.dll` binaries in the results grid.
- **🎨 3 Top-Level Command Palette Shortcuts**: Restored explicit shortcuts for **(Command Palette View)**, **(Standalone App)**, and **(PowerToys Run)**.
- **🔘 Dynamic CSV Button State**: Export CSV button starts disabled (greyed out) on launch/scanning and automatically enables once a scan finishes.
- **📊 Live Status Bar & Category Colors**: Bottom status bar displaying exact item counts (e.g. `212 items`) and vibrant Fluent breakdown category colors.
- **📊 Interactive Visual Chart & "Other Items" Bar**: Top 15 largest items visual chart with aggregate "Other (N items)" summary bar and direct tap-to-drill-down navigation.

### 🛠️ What Got Fixed & Improved:
- **⚡ Scan Engine Performance & Resource Tuning**:
  - Capped parallel Degree of Parallelism (DOP) to half CPU cores with O(1) file type breakdown lookups.
  - **87.0% performance improvement (reduction in overhead)** for file extension category mapping.
  - **6.8% faster scanning speed** compared to v1.3.7.
  - **50% reduced CPU utilization** and **~30% reduced RAM allocation footprint** during active directory scans.
- **🛡️ Zero-Crash Telemetry Fixes**: Eliminated `MOAPPLICATION_HANG` watchdog issues and WinUI Composition multi-threading race condition crashes.
- **📊 Admin-Resilient CSV Export**: Native `comdlg32.dll` (`GetSaveFileName`) save dialog fallback guarantees CSV export works reliably under elevated UAC/Administrator environments.
- **⚡ Expanded DataGrid Name Column Width**: Set default minimum width of 350px for the Name column so file names are never truncated.
- **🔍 Real-Time Live Search Filter**: Fixed filter text box text-changed event handler for instant keyword and extension (`*.mp4`, `*.exe`) filtering.
- **⚙️ PowerToys Run Plugin Sync**: Resolved process locking issues in deployment script so `plugin.json` updates cleanly to v1.4.0.

---

## 📦 Release Assets (v1.4.0)

- **PowerToys Run Plugin Installer**: `ValleySoft.DiskAnalyzerInstaller-v1.4.0-x64.exe`
- **Standalone App & Command Palette Extension (MSIX)**: `ValleySoft.DiskAnalyzer.App_1.4.0-x64.msix`
- **ARM64 Installers & MSIX**: `ValleySoft.DiskAnalyzerInstaller-v1.4.0-arm64.exe` / `ValleySoft.DiskAnalyzer.App_1.4.0-arm64.msix`
- **Symbols Packages**: `ValleySoft.DiskAnalyzer.Symbols_1.4.0-x64.zip`
