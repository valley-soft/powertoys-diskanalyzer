# ValleySoft Disk Analyzer v1.4.0

TreeSize-like disk usage analyzer for PowerToys Run, Windows Command Palette, and Standalone WinUI 3 App.

### Components

This release includes three tools, bundled into two easy installations:
- **Standalone App (WinUI 3)** and **Command Palette Extension** — both bundled together in the native `.msix` package!
- **PowerToys Run Plugin** (`ds` keyword in Alt+Space) — installed via the standalone `.exe` installer.

---

### Screenshots

#### 1. Standalone App (WinUI 3)
![GUI — Main Overview](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-ui%20ver%201.4.0.png)
![GUI — Visual Chart Analysis](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-visual-chart.png)
![GUI — Run as Administrator](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-runas-admin-banner.png)
![GUI — Professional Help Page](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-help-page%20ver%201.4.0.png)
![GUI — About Page](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-about-page.png)

#### 2. PowerToys Run Plugin
![Help commands overview](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-help-commands.png)
![Scanning top-level folders on C:](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-top-folders.png)

#### 3. Command Palette Extension
![CmdPal - Screenshot 1](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/cmdpal-screenshot-1.png)
![CmdPal - Screenshot 2](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/cmdpal-screenshot-2.png)
![CmdPal - Screenshot 3](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/cmdpal-screenshot-3.png)

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
