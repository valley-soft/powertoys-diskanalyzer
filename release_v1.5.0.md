TreeSize-like disk usage analyzer for PowerToys Run, Windows Command Palette, and Standalone WinUI 3 App.

### Changes in v1.5.0

#### Added
- **Interactive Donut Chart**: Added a dedicated Donut Chart tab in the Standalone App to visualize folder space distribution with responsive scaling, Fluent accent colors, center total size display, and tap-to-drill-down navigation.
- **Top 100 Largest Files Tab**: Added a dedicated Top Files tab that multithreadedly scans the entire drive/root to surface the 100 largest files with early size pruning and root-level system files (pagefile.sys, hiberfil.sys, swapfile.sys) support.
- **Large & Old Files Quick Filter**: Added a one-click toolbar toggle filter button to instantly isolate files larger than 100 MB not modified in the last 12 months with a live match count badge.
- **Send to Recycle Bin**: Added a right-click context menu option in the results grid to safely move files or folders to the Windows Recycle Bin with a confirmation dialog and dynamic list updates.
- **PowerToys Run Recent Scans (ds recent)**: Added a history mechanism to PowerToys Run storing the last 5 scanned paths for instant 1-click re-scanning.
- **Richer Result Subtitles**: PowerToys Run query results now show both total size and formatted item counts (e.g. 42.3 GB · 15,823 items).
- **PowerToys Run Full Keyboard Navigation**: Added complete keyboard accessibility to the floating GUI (Enter to drill down, Backspace to navigate up one level, and F5 to refresh/rescan).
- **PowerToys Run Collapsible Donut Panel**: Added a toggleable side chart panel in the PowerToys Run GUI window featuring a high-contrast visual donut chart.
- **Live Theme Synchronization**: Added dynamic system theme tracking (UserPreferenceChanged) to the PowerToys Run GUI, instantly switching between dark and light themes without requiring an app restart.
- **High-Contrast Usage Bars**: Redesigned PowerToys Run mini usage progress bars using unicode contrast blocks (█ / ░) ensuring clear visibility across dark and light modes.
- **Native Command Palette Settings**: Replaced placeholder settings with native CommandProvider.Settings integration, introducing persistent "Show Hidden Files" and "Max Scan Depth" controls.
- **Command Palette Asynchronous Loading**: Command Palette extension now streams and queries results asynchronously in the background, eliminating UI freezes and maintaining launcher responsiveness.
- **Enhanced Excel-Sortable CSV Export**: Export formatted .csv files including unquoted numeric Size (Bytes) and Allocated Size (Bytes) columns with UTF-8 BOM encoding for direct numeric sorting and formula calculations in Microsoft Excel.

#### Fixed & Improved
- **Bar Chart Dynamic Scaling & Horizontal Scrolling**: Fixed bar chart item clipping on smaller displays by adding horizontal scrolling and dynamic bar height scaling.
- **Drill-Down Tap Safety**: Prevented aggregate "Other (N items)" summary bars from triggering invalid navigation while maintaining tap-to-drill-down on all named item slices and bars.
- **Clean Command Palette Provider Registration**: Purged stale ghost provider cache entries for a clean single-entry settings experience.
- **Modernized Standalone Help Page**: Fully refreshed Help and Features documentation detailing Donut Charts, Top Files, Old & Large filtering, Recycle Bin integration, and launcher commands.
