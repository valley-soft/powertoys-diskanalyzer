TreeSize-like disk usage analyzer for PowerToys Run, Windows Command Palette, and Standalone WinUI 3 App.

### Changes in v1.4.0

#### Added
- **Windows Explorer Context Menu**: Added "Analyze with DiskAnalyzer" right-click context menu item with official application icon for all directories, drives, and folder backgrounds, launching directly into target scan view.
- **Microsoft Store Rating Prompts**: Integrated non-intrusive, professional WinUI 3 dialog after 3 completed scans to rate the app on the Store.
- **Crisp Executable Icon Extraction**: Added native Win32 ExtractIconEx shell extraction for .exe and .dll files in the main DataGrid view.
- **3 Top-Level Command Palette Shortcuts**: Restored explicit shortcuts for (Command Palette View), (Standalone App), and (PowerToys Run).
- **Dynamic CSV Export Button**: Export CSV button starts disabled (greyed out) on launch and while scanning, enabling automatically only after a scan completes.
- **Live Item Count Status Bar**: Real-time bottom status bar displaying exact item counts (e.g. 212 items).
- **Interactive Visual Chart & Other Items Bar**: Added Top 15 largest items visual chart with aggregate "Other (N items)" summary bar and direct tap-to-drill-down navigation.

#### Fixed & Improved
- **Scan Engine Performance & Resource Tuning**: Capped parallel Degree of Parallelism (DOP) to half CPU cores with O(1) file type breakdown lookups. 87.0% lookup overhead reduction for file extension category mapping, 6.8% faster scanning speed, 50% reduced CPU utilization, and ~30% reduced RAM footprint during active directory scans.
- **Zero-Crash Telemetry Fixes**: Eliminated MOAPPLICATION_HANG watchdog issues and WinUI Composition multi-threading race condition crashes.
- **Admin Elevation CSV Export Resilience**: Added native comdlg32.dll (GetSaveFileName) save dialog fallback to guarantee CSV export works reliably under elevated UAC and Administrator environments.
- **Expanded DataGrid Name Column Width**: Set default minimum width of 350px for the Name column so file names are never truncated.
- **Real-Time Live Search Filter**: Fixed filter text box text-changed event handler for instant keyword and extension (*.mp4, *.exe) filtering.
- **PowerToys Run Plugin Version Sync**: Resolved process locking issues during deployment script so plugin.json updates cleanly to v1.4.0.

#### Security
- **Renewed Code Signing Certificate**: Renewed signing certificate with Code Signing EKU and human-readable CN=ValleySoft name.
- **Build Security Hardening**: Removed all hardcoded credentials and purged private key files from repository history.
