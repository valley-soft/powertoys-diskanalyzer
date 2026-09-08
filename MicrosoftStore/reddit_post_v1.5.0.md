**Title:** [Release] Disk Analyzer v1.5.0 — Interactive Donut Charts, Top 100 Files Whole-Drive Scan, Recycle Bin Integration & Recent Scans History! 🍩🚀

Hey r/PowerToys community!

I'm excited to share the release of **v1.5.0** of **ValleySoft Disk Analyzer** — the free, 100% open-source TreeSize-like disk usage analyzer built specifically for **PowerToys Run**, the new **Windows Command Palette**, and as a standalone **WinUI 3** app.

---

### 🖼️ Screenshots:
* **Standalone App — Donut Chart Analysis**:
  ![Donut Chart](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-donut-chart%20ver%201.5.0.png)
* **Standalone App — Top 100 Largest Files (Whole Drive Scan)**:
  ![Top 100 Files](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-top-files%20ver%201.5.0.png)
* **Standalone App — Visual Bar Chart**:
  ![Visual Bar Chart](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/standalone-app-visual-chart%20ver%201.5.0.png)
* **PowerToys Run — Floating GUI Donut Side Panel**:
  ![PowerToys Run GUI Panel](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-gui-chart-panel.png)
* **PowerToys Run — Recent Scans History (`ds recent`)**:
  ![Recent Scans](https://raw.githubusercontent.com/valley-soft/powertoys-diskanalyzer/main/docs/Images/ptrun-recent-scans.png)

---

### 🚀 What's New in Version 1.5.0:

#### 🖥️ Standalone App (WinUI 3):
1. 🍩 **Interactive Donut Chart**:
   - Added a dedicated Donut Chart tab with responsive scaling, Fluent accent colors, a center total size readout, and tap-to-drill-down navigation into subfolders.
2. 📁 **Top 100 Largest Files Tab**:
   - Multithreaded whole-drive scan isolating the 100 biggest files across all subfolders (with early size pruning and root system files like `hiberfil.sys`, `pagefile.sys`, and `swapfile.sys` properly analyzed).
3. 🕐 **"Old & Large" Files Smart Filter**:
   - One-click toolbar toggle button that instantly filters for files >100 MB that haven't been modified in 12+ months, complete with a live badge count.
4. 🗑️ **Send to Recycle Bin Context Menu**:
   - Right-click any row in the grid and safely send files or folders to the Windows Recycle Bin with a confirmation dialog.
5. 💾 **Excel-Sortable CSV Export**:
   - CSV exports now include pure numeric `Size (Bytes)` and `Allocated Size (Bytes)` columns alongside formatted strings, saved with UTF-8 BOM encoding for direct numeric sorting and formula calculations in Microsoft Excel.

#### 🔍 PowerToys Run Plugin (`ds` keyword) & GUI:
1. 🕒 **Recent Scans History (`ds recent`)**:
   - Shows your last 5 scanned paths for instant 1-click re-scanning without retyping long paths.
2. 📊 **Richer Result Subtitles**:
   - PowerToys Run query results now display total size alongside exact item counts (e.g., `42.3 GB · 15,823 items`).
3. ⌨️ **Full Keyboard Navigation**:
   - The floating GUI now supports pure keyboard interaction: `Enter` to drill into folders, `Backspace` to go up one level, and `F5` to refresh/rescan.
4. 📊 **Collapsible Donut Side Panel**:
   - Added a collapsible side panel featuring a visual donut chart right inside the PowerToys Run floating GUI window.
5. 🎨 **Live System Theme Sync**:
   - Dynamically tracks Windows Dark/Light theme changes in real time without needing an app restart.
6. ▓ **High-Contrast Shaded Usage Bars**:
   - Redesigned the launcher mini usage bars using unicode contrast blocks (`█` / `░`) for crisp readability across dark and light backgrounds.

#### 🎨 Command Palette Extension (Native Windows CmdPal):
1. ⚙️ **Native Extension Settings**:
   - Integrated with `CommandProvider.Settings` for persistent "Show Hidden Files" and "Max Scan Depth" controls.
2. ⚡ **Asynchronous Background Scanning**:
   - List results now stream asynchronously, keeping Command Palette snappy and completely responsive during deep directory scans.

---

### 🟢 Microsoft Store Version: LIVE NOW!
> 🎉 **Update:** Microsoft just approved **v1.5.0**, and it is officially live on the Microsoft Store! You can install or update directly through the Store app or via `winget`.

---

### ❤️ A Small Request to the Community:
Disk Analyzer is completely free, 100% open-source, and has **zero ads, zero trackers, and zero subscriptions**. 

Recently, someone left a **2-star rating** on the Microsoft Store with the comment: *"Typical case of not fine software with advertising surprises"*, mistaking the standard one-time rating prompt for an advertisement. Even though the public store page currently says *"There aren't any reviews yet"*, that single 2-star rating is currently dragging down the store analytics for a project built entirely in free time for the community.

If you enjoy using Disk Analyzer or find it helpful in PowerToys, **it would mean the world if you could take 30 seconds to drop an honest rating and review on the Microsoft Store**:
👉 **[Rate / Review ValleySoft Disk Analyzer on Microsoft Store](https://apps.microsoft.com/detail/9nf073kltvwn?hl=en-US&gl=US)**

---

### 📦 Download & Links:
- ⭐️ **Microsoft Store (v1.5.0 Live)**: [Get on Microsoft Store](https://apps.microsoft.com/detail/9nf073kltvwn?hl=en-US&gl=US)
- 💻 **Winget**: `winget install ValleySoft.DiskAnalyzer`
- 🐙 **GitHub Release (v1.5.0)**: [Download on GitHub Releases](https://github.com/valley-soft/powertoys-diskanalyzer/releases/tag/v1.5.0)
- 🌐 **Project Website**: [valley-soft.github.io/powertoys-diskanalyzer](https://valley-soft.github.io/powertoys-diskanalyzer/)

Feedback, bug reports, and feature requests are always welcome! Thank you for the support! 🙌
