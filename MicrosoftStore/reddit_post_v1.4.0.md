**Title:** Release: Disk Analyzer v1.4.0 — File Explorer Context Menu, True Binary Icons & Command Palette Upgrades! 🚀

Hey everyone!

I'm excited to share **v1.4.0** of **ValleySoft Disk Analyzer** — the free, TreeSize-like disk space analyzer built for Windows 11, PowerToys Run, and Windows Command Palette.

### 🚀 What's New in Version 1.4.0:

1. 📁 **Windows File Explorer Context Menu**:
   - Right-click any folder, drive, or folder background in Windows File Explorer and select **"Analyze with DiskAnalyzer"** to launch directly into target scan view.
   - Includes the official application icon in the context menu.

2. 🖼️ **Crisp Executable File Icon Extraction**:
   - Native Win32 `ExtractIconEx` P/Invoke loads authentic high-resolution application icons for `.exe` and `.dll` binaries in the scan results DataGrid.

3. 🎨 **3 Top-Level Command Palette Shortcuts**:
   - Dedicated Command Palette entries for **(Command Palette View)**, **(Standalone App)**, and **(PowerToys Run)**.
   - Interactive in-palette subfolder navigation with an **"Up one level"** return item.

4. 📊 **Interactive Visual Chart & "Other Items" Summary Bar**:
   - Displays the **Top 15 Largest Items** with color-coded bars, while aggregating remaining items into a clean **"Other (200 items)"** summary bar.
   - Tap/click any bar directly to drill down into subfolders or reveal files in Explorer.

5. 💾 **Admin-Resilient CSV Export**:
   - Dynamic `Export CSV` button state and native `comdlg32.dll` (`GetSaveFileName`) save dialog fallback to guarantee CSV export works 100% reliably under Administrator UAC integrity boundaries.

6. ⚡ **Name Column Auto-Width & Real-Time Live Filter**:
   - Default 350px minimum width for the DataGrid Name column so file names are never truncated.
   - Instant live search filtering as you type (keywords, `*.mp4`, `*.exe`, `*.zip`).

---

### 📦 Download & Update Links:
- **Microsoft Store**: [Get Disk Analyzer on Microsoft Store](https://apps.microsoft.com/detail/9nf073kltvwn)
- **Winget**: `winget install ValleySoft.DiskAnalyzer`
- **GitHub Release**: Download `.msix` or `.exe` installer from [GitHub Releases](https://github.com/valley-soft/powertoys-diskanalyzer/releases/latest)
- **Web**: [Visit Official Website](https://valley-soft.github.io/powertoys-diskanalyzer/)

Thank you for all the feedback and support! Let me know what features you'd like to see next in v1.5.0!
