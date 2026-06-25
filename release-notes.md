TreeSize-like disk usage analyzer for PowerToys Run and Windows Command Palette.

### Components

This release includes three tools, bundled into two easy installations:
- **Standalone App (WinUI 3)** and **Command Palette Extension** — both bundled together in the native `.msix` package!
- **PowerToys Run Plugin** (`ds` keyword in Alt+Space) — installed via the standalone `.exe` installer.

---

### Screenshots

#### 1. Standalone App (WinUI 3)
![GUI — Main Overview](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/standalone-app-ui.png)
![GUI — Visual Chart Analysis](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/standalone-app-visual-chart.png)
![GUI — Run as Administrator](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/standalone-app-run-as-admin.png)

#### 2. PowerToys Run Plugin
![Help commands overview](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/ptrun-help-commands.png)
![Scanning top-level folders on C:](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/ptrun-top-folders.png)

#### 3. Command Palette Extension
![CmdPal - Screenshot 1](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/cmdpal-screenshot-1.png)
![CmdPal - Screenshot 2](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/cmdpal-screenshot-2.png)
![CmdPal - Screenshot 3](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/cmdpal-screenshot-3.png)

---

### Installation — Standalone App & Command Palette Extension (Unified MSIX)

1. Download **`ValleySoft.DiskAnalyzer.App_1.3.1.0_x64.msix`** (or `arm64`) from the assets below.
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

1. Download **`ValleySoft.DiskAnalyzerInstaller-v1.3.1-x64.exe`** (or `arm64`)
2. Exit PowerToys (right-click tray icon → Exit)
3. Run the installer — it will flawlessly clean install to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer`
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

### Changes in v1.3.1

#### Added
* **"Always Run as Administrator"** toggle added to the Standalone App's File menu. It automatically saves your preference and natively relaunches the app via UAC!
* **Live UI Streaming**: The Standalone App now populates folders incrementally in real-time as the scan progresses, similar to TreeSize Free.

#### Changed
* **Extreme Performance Optimization**: Refactored the core scanning engine to eliminate blocking array allocations and bypass heavy P/Invoke and string operations. SSD scanning speeds have been radically improved, completely resolving UI hangs and slow scans.
* **Admin Integration**: Restructured the app manifests so that elevated privileges are optional via the UI toggle instead of forcing UAC prompts on every launch.

#### Fixed
* **Command Palette Bug Fixes**: Fixed a critical COM marshalling bug that caused the Command Palette to crash when rendering the extension items. Also eliminated duplicate ghost entries caused by identity changes!
