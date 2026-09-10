## What's new in v1.5.2.0
- **Startup Crash Fix (Hotfix)**: Resolved startup crash (0x800f1000) in Dark theme by defining complete XAML theme dictionaries. Hardened background data grid measurement and collection initialization.
- **Enhanced Backdrop Stability**: Hardened Mica system backdrop initialization with graceful runtime fallback for Windows 10, battery saver, and non-composition environments.
- **Interactive Donut Chart**: New Donut Chart tab visualizing disk space across top folders with Fluent colors, center total size, and click-to-drill-down navigation.
- **Top 100 Largest Files Tab**: Multithreaded scan with early pruning surfacing the 100 largest files across the entire drive, including root-level system files.
- **Large & Old Files Quick Filter**: One-click toolbar filter isolating files larger than 100 MB not modified in 12+ months with live badge count.
- **Send to Recycle Bin**: Right-click context menu action in the results grid to safely send files or folders to the Recycle Bin with confirmation dialog.
- **Command Palette Settings**: Native settings toggles for "Show Hidden Files" and "Max Scan Depth", plus background async query execution.
