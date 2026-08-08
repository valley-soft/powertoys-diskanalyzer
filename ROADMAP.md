# ValleySoft Disk Analyzer Roadmap

## Release Cadence
**Target Cadence: Bi-Weekly (Every other week)**

Releasing every other week provides enough time to develop meaningful, polished features without causing burnout while keeping open-source users engaged with steady updates.

## Completed & Upcoming Versions

### v1.4.0 (Completed - 2026-08-07)
**Focus: Telemetry Stability, Explorer Integration, & Export Features**
- ✅ **Windows File Explorer Context Menu:** Direct "Analyze with DiskAnalyzer" right-click integration on directories, drives, and backgrounds.
- ✅ **Microsoft Store Rating Prompt:** Professional, non-intrusive rating dialog linking to Microsoft Store reviews (`ms-windows-store://review/?ProductId=9NF073KLTVWN`).
- ✅ **True Executable File Icon Extraction:** Native Win32 `ExtractIconEx` P/Invoke to load real embedded icons for `.exe` and `.dll` files.
- ✅ **Command Palette 3 Top-Level Commands:** Dedicated shortcuts for (Command Palette), (Standalone App), and (PowerToys Run).
- ✅ **Admin Elevation CSV Export Resilience:** Native `comdlg32.dll` (`GetSaveFileName`) fallback for Administrator UAC integrity boundaries.
- ✅ **Zero-Crash Telemetry Fixes:** `MOAPPLICATION_HANG` watchdog fix, Composition thread safety, `AppDomain` / `TaskScheduler` unhandled exception traps.
- ✅ **Live Item Count Status Bar:** Dynamic bottom bar displaying exact total item count (`212 items`).

### v1.5.0 (Target: 2 weeks from now)
**Focus: System Integration & Advanced Visualizations**
- **Global "Top 100 Largest Files" View:** Dedicated tab scanning entire drives to display absolute largest files across all subfolders.
- **Interactive Pie & Donut Chart Toggle:** Alternate visual representation for folder space distribution in the Standalone App.
- **Native Windows 11 `IExplorerCommand` COM Server:** Promotion to primary top-level context menu in Windows 11 File Explorer.

### v1.6.0 (Target: 4 weeks from now)
**Focus: Multi-Layer Representation & Customization**
- **Sunburst / Treemap Charts:** Multi-layered visual charts allowing deep folder content inspection without manual drill-down.
- **Custom Accent Themes:** Customizable Fluent accent colors matching personal Windows 11 system themes.

### v1.7.0 (Target: 6 weeks from now)
**Focus: Actionable Management & Cleanup**
- **Built-in Safe Delete:** Send large unwanted files directly to Windows Recycle Bin.
- **"Large Old Files" Filter:** Flag huge files that haven't been accessed or modified in over a year.
