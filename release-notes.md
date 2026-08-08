TreeSize-like disk usage analyzer for PowerToys Run and Windows Command Palette.

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

### Installation — Standalone App & Command Palette Extension (Unified MSIX)

1. Download **`ValleySoft.DiskAnalyzer.App_1.4.0_x64.msix`** (or `arm64`) from the assets below.
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
- **Locked Drive Crash Fix**: Resolved a bug where scanning or showing drive listings would crash the app if a drive was locked by BitLocker or had restricted access permissions.
- **Background Process Cleanup**: Fixed an issue where the Command Palette extension remained running in the background after closing, ensuring it shuts down cleanly and saves system resources.
- **Windows Insider Compatibility**: Added fallbacks and exception protections to prevent launch crashes for users running preview Windows 11 Insider builds (build 26300+).
- **Folder Icon Load Fix**: Fixed a bug where folder and file icons occasionally failed to load or caused background crashes, ensuring icons load reliably on the main screen.
- **Clipboard Copy Protection**: Fixed a random crash in PowerToys Run when copying file paths to the clipboard while another app had the clipboard locked.
- **Visual Theme Asset Fix**: Fixed a packaging bug that caused light/dark mode icons and different scale sizes to be missing from installer builds.
- **About Page Layout Centering**: Centered and resized the About page content so it looks clean, centered, and proportional on all window sizes (including maximized windows).
- **Insider Preview Warning Banner**: Added a helpful informational banner at startup to warn Windows Insider users about potential pre-release compatibility issues.
- **Scanning Stability Improvement**: Fixed a scanning race condition that caused occasional crashes when exploring complex directory structures.
- **Interface Lag & Freeze Mitigation**: Offloaded heavy folder information lookups to background tasks and regulated progress bar updates to keep the main window smooth and responsive.
- **Clean Production Release**: Stripped developer test paths and cleaned up file logging for a safer and more stable production install.
