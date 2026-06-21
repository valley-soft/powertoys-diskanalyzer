TreeSize-like disk usage analyzer for PowerToys Run and Windows Command Palette.

### Components

This release includes three separate tools:
- **Standalone App (WinUI 3)** — fully featured GUI window with interactive Visual Charts!
- **PowerToys Run Plugin** (`ds` keyword in Alt+Space) — labeled *DiskAnalyzer (PowerToys Run)* in Command Palette
- **Command Palette Extension** (MSIX) — labeled *DiskAnalyzer* in Command Palette, no keyword needed

---

### Standalone WinUI 3 App Screenshots!

![GUI — Main Overview](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/standalone-app-ui.png)

![GUI — Visual Chart Analysis](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/Docs/Images/standalone-app-visual-chart.png)

---

### Installation — Standalone App

1. Download **`ValleySoft.DiskAnalyzer.App_1.3.0.0_x64.msix`** (or `arm64`) from the assets below.
2. Double-click the `.msix` file and click **Install**.
3. Launch the app from your Start Menu.

> **Note:** The standalone app is not yet available in the Microsoft Store. When it is officially available, the Microsoft Store will be the recommended way to install and keep the app automatically updated!
>
> [![Get it from Microsoft](https://get.microsoft.com/images/en-us%20dark.svg)](https://apps.microsoft.com/detail/9nf073kltvwn?hl=en-US&gl=US)
>
> Alternatively, you can install it instantly via the command line using `winget`:
> ```powershell
> winget install --id 9NF073KLTVWN --source msstore
> ```

### Installation — PowerToys Run Plugin

1. Download **`ValleySoft.DiskAnalyzerInstaller-v1.3.0-x64.exe`** (or `arm64`)
2. Exit PowerToys (right-click tray icon → Exit)
3. Run the installer — it will flawlessly clean install to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer`
4. Restart PowerToys and enable the plugin in Settings → PowerToys Run → Plugins

### Installation — Command Palette Extension

1. Download **`DiskAnalyzerExtension_1.3.0.0_x64.msix`** (or `arm64`)
2. Double-click the `.msix` file and click **Install**
3. Open Command Palette — *DiskAnalyzer* will appear as a top-level entry

### Usage

| Command | Description |
| :--- | :--- |
| `ds drives` | List all drives |
| `ds top C:\` | Top folders ranked by size |
| `ds largest C:\` | Find largest files recursively |
| `ds ext C:\ .mp4` | Find files by extension |
| `ds empty C:\` | Find empty folders |
| `ds gui` | Open the standalone GUI window |

### Changes in v1.3.0

* **New**: Fully featured Standalone WinUI 3 App with interactive Visual Charts!
* **New**: Unified Installer features a flawless 1-click Clean Install mode
* **Fixed**: PowerToys Run `AssemblyLoadContext` bug completely resolved by natively compiling core logic into the plugin
* **Fixed**: Standalone App sizes and calculations rigorously synced with Windows Explorer
