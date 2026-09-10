## What's new in v1.5.2.0
- **Startup Crash Fix (Hotfix)**: Resolved a critical XAML resource resolution crash (0x800f1000) on startup by defining complete theme dictionaries for Light, Dark, and Default modes. Also hardened unselected tab data grid measurement and collection initialization.
- **Enhanced Backdrop Stability**: Hardened Mica system backdrop initialization with graceful runtime fallback for Windows 10, battery saver, and environments lacking hardware composition.
- **Top Files DataGrid Optimization**: Stabilized column layout rendering and optimized collection binding for unmeasured tabs.
- **Interactive Donut Chart**: Dedicated Donut Chart tab visualizing disk space distribution across top folders with Fluent accent colors, center total size display, and click-to-drill-down navigation.
- **Top 100 Largest Files Tab**: Dedicated "Top Files" tab with multithreaded scanning and early pruning that surfaces the 100 largest files across the entire drive, including root-level system files.
- **Large & Old Files Quick Filter**: One-click toolbar toggle filter button to instantly isolate files larger than 100 MB not modified in 12+ months with a live match count badge.
- **Send to Recycle Bin**: Right-click context menu action in the results grid to safely send files or folders to the Windows Recycle Bin with a confirmation dialog.
- **Command Palette Settings & Async Loading**: Native Command Palette Settings integration with persistent toggles for "Show Hidden Files" and "Max Scan Depth", plus background async query execution.
