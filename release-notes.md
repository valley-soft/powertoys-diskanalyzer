# ValleySoft Disk Analyzer v1.5.0

TreeSize-like disk usage analyzer for PowerToys Run, Windows Command Palette, and Standalone WinUI 3 App.

### Components

This release includes three tools, bundled into two easy installations:
- **Standalone App (WinUI 3)** and **Command Palette Extension** — both bundled together in the native `.msix` package!
- **PowerToys Run Plugin** (`ds` keyword in Alt+Space) — installed via the standalone `.exe` installer.

---

### Screenshots

#### 1. Standalone App (WinUI 3)
![GUI — Main Overview](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-ui%20ver%201.5.0.png)
![GUI — Donut Chart Analysis](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-donut-chart%20ver%201.5.0.png)
![GUI — Top 100 Files](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-top-files%20ver%201.5.0.png)
![GUI — Visual Bar Chart](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-visual-chart%20ver%201.5.0.png)
![GUI — Help Page](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-help-page%20ver%201.5.0.png)
![GUI — About Page](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-about-page%20ver%201.5.0.png)

#### 2. PowerToys Run Plugin & GUI
![Help commands overview](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-help-commands.png)
![Help commands detailed](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-help-commands-1.png)
![Recent scans history](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-recent-scans.png)
![Scanning top-level folders](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-top-folders.png)
![GUI Chart Panel](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-gui-chart-panel.png)

#### 3. Command Palette Extension
![CmdPal - Screenshot 1](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/cmdpal-screenshot-1.png)
![CmdPal - Screenshot 2](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/cmdpal-screenshot-2.png)
![CmdPal - Screenshot 3](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/cmdpal-screenshot-3.png)

---

### Installation — Standalone App & Command Palette Extension (Unified MSIX)

1. Download **`ValleySoft.DiskAnalyzer.App_1.5.0_x64.msix`** (or `arm64`) from the assets below.
2. Double-click the `.msix` file and click **Install**.
3. You're done! The Standalone App will be in your Start Menu, and the Command Palette Extension will automatically be registered in the Windows Command Palette.

> 💡 **Troubleshooting Certificate Verification (Error 0x800B010A):**
> Because this MSIX is a sideloading release signed with a self-signed certificate, Windows requires you to install the certificate once before installing the app:
> 1. Download **`ValleySoft.cer`** from the release assets below.
> 2. Right-click the `.cer` file and select **Install Certificate**.
> 3. Select **Local Machine**, click Next.
> 4. Choose **Place all certificates in the following store**, click Browse, select **Trusted People**, click OK.
> 5. Click Next, then Finish. Now double-click the `.msix` file to install the app cleanly!

> **Recommended:** The Microsoft Store is the easiest way to install and keep the app automatically updated!
>
> [![Get it from Microsoft](https://get.microsoft.com/images/en-us%20dark.svg)](https://apps.microsoft.com/detail/9nf073kltvwn?hl=en-US&gl=US)
>
> Alternatively, you can install it instantly via the command line using `winget`:
> ```powershell
> winget install --id 9NF073KLTVWN --source msstore
> ```

### Installation — PowerToys Run Plugin

1. Download **`ValleySoft.DiskAnalyzerInstaller-v1.5.0-x64.exe`** (or `arm64`) from the assets below.
2. Exit PowerToys (right-click tray icon → Exit).
3. Run the installer — it will clean install to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer`.
4. Restart PowerToys and enable the plugin in Settings → PowerToys Run → Plugins.

### Usage

| Command | Description |
| :--- | :--- |
| `ds` | Show help and all available commands |
| `ds drives` | List all drives with used / free / total space |
| `ds recent` | Show last 5 scanned paths for instant re-scan |
| `ds top C:\` | Top folders ranked by size |
| `ds largest C:\` | Find largest files recursively |
| `ds ext C:\ .mp4` | Find files by extension |
| `ds empty C:\` | Find empty folders |
| `ds gui` | Open the standalone GUI window |

---

## 🚀 What's New in Version 1.5.0

### 🟢 What Got Added:
- **🍩 Interactive Donut Chart**: Added a dedicated Donut Chart tab in the Standalone App to visualize folder space distribution with responsive scaling, Fluent accent colors, center total size display, and tap-to-drill-down navigation.
- **📁 Top 100 Largest Files Tab**: Added a dedicated Top Files tab that multithreadedly scans the entire drive/root to surface the 100 largest files with early size pruning and root-level system files (`pagefile.sys`, `hiberfil.sys`, `swapfile.sys`) support.
- **🕐 Large & Old Files Quick Filter**: Added a one-click toolbar toggle filter button to instantly isolate files larger than 100 MB not modified in the last 12 months with a live match count badge.
- **🗑️ Send to Recycle Bin**: Added a right-click context menu option in the results grid to safely move files or folders to the Windows Recycle Bin with a confirmation dialog and dynamic list updates.
- **🕒 PowerToys Run Recent Scans (`ds recent`)**: Added a history mechanism to PowerToys Run storing the last 5 scanned paths for instant 1-click re-scanning.
- **📊 Richer Result Subtitles**: PowerToys Run query results now show both total size and formatted item counts (e.g. `42.3 GB · 15,823 items`).
- **⌨️ PowerToys Run Full Keyboard Navigation**: Added complete keyboard accessibility to the floating GUI (`Enter` to drill down, `Backspace` to navigate up one level, and `F5` to refresh/rescan).
- **📊 PowerToys Run Collapsible Donut Panel**: Added a toggleable side chart panel in the PowerToys Run GUI window featuring a high-contrast visual donut chart.
- **🎨 Live Theme Synchronization**: Added dynamic system theme tracking (`UserPreferenceChanged`) to the PowerToys Run GUI, instantly switching between dark and light themes without requiring an app restart.
- **▓ High-Contrast Shaded Usage Bars**: Redesigned PowerToys Run mini usage progress bars (`DiskAnalyzerHelper.CreateMiniBar`) using unicode contrast blocks (`█` / `░`) ensuring clear visibility across dark and light modes.
- **⚙️ Native Command Palette Settings**: Replaced placeholder settings with native `CommandProvider.Settings` integration, introducing persistent "Show Hidden Files" and "Max Scan Depth" controls.
- **⚡ Command Palette Asynchronous Loading**: Command Palette extension now streams and queries results asynchronously in the background, eliminating UI freezes and maintaining launcher responsiveness.
- **💾 Enhanced Excel-Sortable CSV Export**: Export formatted `.csv` files including unquoted numeric `Size (Bytes)` and `Allocated Size (Bytes)` columns with UTF-8 BOM encoding for direct numeric sorting and formula calculations in Microsoft Excel.

### 🛠️ What Got Fixed & Improved:
- **📊 Bar Chart Dynamic Scaling & Horizontal Scrolling**: Fixed bar chart item clipping on smaller displays by adding horizontal scrolling and dynamic bar height scaling.
- **🛡️ Drill-Down Tap Safety**: Prevented aggregate "Other (N items)" summary bars from triggering invalid navigation while maintaining tap-to-drill-down on all named item slices and bars.
- **🧹 Clean Command Palette Provider Registration**: Purged stale ghost provider cache entries for a clean single-entry settings experience.
- **📖 Modernized Standalone Help Page**: Fully refreshed Help and Features documentation detailing Donut Charts, Top Files, Old & Large filtering, Recycle Bin integration, and launcher commands.
