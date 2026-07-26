TreeSize-like disk usage analyzer for PowerToys Run and Windows Command Palette.

### Components

This release includes three tools, bundled into two easy installations:
- **Standalone App (WinUI 3)** and **Command Palette Extension** — both bundled together in the native `.msix` package!
- **PowerToys Run Plugin** (`ds` keyword in Alt+Space) — installed via the standalone `.exe` installer.

---

### Screenshots

#### 1. Standalone App (WinUI 3)
![GUI — Main Overview](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-ui.png)
![GUI — Visual Chart Analysis](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-visual-chart.png)
![GUI — Run as Administrator](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-runas-admin-banner.png)
![GUI — Professional Help Page](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-help-page.png)

#### 2. PowerToys Run Plugin
![Help commands overview](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-help-commands.png)
![Scanning top-level folders on C:](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-top-folders.png)

#### 3. Command Palette Extension
![CmdPal - Screenshot 1](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/cmdpal-screenshot-1.png)
![CmdPal - Screenshot 2](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/cmdpal-screenshot-2.png)
![CmdPal - Screenshot 3](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/cmdpal-screenshot-3.png)

---

### Installation — Standalone App & Command Palette Extension (Unified MSIX)

1. Download **`ValleySoft.DiskAnalyzer.App_1.3.7_x64.msix`** (or `arm64`) from the assets below.
2. Double-click the `.msix` file and click **Install**.
3. You're done! The Standalone App will be in your Start Menu, and the Command Palette Extension will automatically be registered in the Windows Command Palette.

> **Recommended:** The Microsoft Store is the easiest way to install and keep the app automatically updated!
>
> [![Get it from Microsoft](https://get.microsoft.com/images/en-us%20dark.svg)](https://apps.microsoft.com/detail/9nf073kltvwn?hl=en-US&gl=US)
>
> Alternatively, you can install it instantly via the command line using `winget`:
> ```powershell
> winget install --id 9NF073KLTVWN --source msstore
> ```

### Installation — PowerToys Run Plugin

1. Download **`ValleySoft.DiskAnalyzerInstaller-v1.3.7-x64.exe`** (or `arm64`)
2. Exit PowerToys (right-click tray icon → Exit)
3. Run the installer — it will clean install to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer`
4. Restart PowerToys and enable the plugin in Settings → PowerToys Run → Plugins

### Usage

| Command | Description |
| :--- | :--- |
| `ds drives` | List all drives |
| `ds top C:\` | Top folders ranked by size |
| `ds largest C:\` | Find largest files recursively |
| `ds ext C:\ .mp4` | Find files by extension |
| `ds empty C:\` | Find empty folders |
| `ds gui` | Open the standalone GUI window |

### Changes in v1.3.7

#### Fixed
- **BitLocker & Drive Permission Crashes**: Wrapped drive property queries (`IsReady`, `TotalSize`, `AvailableFreeSpace`) inside safe try-catch blocks to eliminate unhandled exceptions on BitLocker-locked or restricted drives.
- **HANG_QUIESCE Background Suspension**: Wired up `ComServer.Empty` event to automatically signal process exit when Command Palette disconnects, preventing process suspension timeouts.
- **Windows Insider Build Compatibility**: Added global `e.Handled = true` exception handling and DirectComposition fallback protections for Windows 11 Insider preview builds (OS 26300+).
- **Cross-Thread UI Exceptions**: Fixed a critical bug where folder icons failed to load and threw stowed `RPC_E_WRONG_THREAD` exceptions by ensuring `BitmapImage` creation is marshaled to the `DispatcherQueue` UI thread.
- **PowerToys Run Clipboard Lock Protection**: Prevented unhandled external exceptions (which crashed PT Run) by wrapping clipboard copies in try-catch blocks.
- **Unified Assets Packaging**: Fixed missing theme-light, theme-dark, and scaled asset copies in the build output by using a wildcard pattern in the `.csproj` file.
- **About Page Layout Centering**: Centered and resized the About page content Grid to render perfectly on all window sizes.
- **Windows Insider Warning InfoBar**: Added an informational warning banner at launch to notify users running on preview Windows Insider builds.
- **Parallel Scanning Race Condition**: Materialized subdirectories before parallel execution to eliminate lazy enumeration race conditions.
- **UI Thread Deadlocks & Flooding**: Offloaded `SHGetFileInfo` shell queries to background tasks and throttled UI progress reporting to prevent `DispatcherQueue` flooding.
- **Clean Production Build**: Removed hardcoded developer trace paths and safely wrapped all file logging.
