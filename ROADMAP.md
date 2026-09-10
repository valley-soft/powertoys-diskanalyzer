# ValleySoft Disk Analyzer – Roadmap

## Release Cadence
**Target: Bi-Weekly (every two weeks)**

---

## Components

| Component | Description | Current Version |
|---|---|---|
| 🖥️ **Standalone App** | WinUI 3 MSIX app (Microsoft Store + sideload) | 1.5.1.0 |
| 🔍 **PowerToys Run Plugin** | `ds` keyword plugin for PowerToys Run (launcher) | 1.5.1 |
| 🪟 **PowerToys Run GUI** | WPF floating window launched from PowerToys Run | 1.5.1 |
| 🎨 **Command Palette Extension** | Native CmdPal WinRT/COM extension (MSIX) | 1.5.1 |

---

## PowerToys Alignment (PowerToys v0.101, September 2026)

> Researched from the official PowerToys GitHub — latest stable: **v0.101.2362**, latest preview: **v0.101.2492**.

### Platform Signals

| PowerToys Change | Impact on DiskAnalyzer |
|---|---|
| **Migrated to .NET 10** (v0.100.0) | Our plugins must target `net10.0-windows` — already done ✅ |
| **Command Palette Compact Mode** | Our CmdPal extension should adapt gracefully to compact mode (icon + short title only) |
| **CmdPal Extension Gallery** (v0.100) | We should submit to `microsoft/CmdPal-Extensions` so users can discover and install us without manual MSIX |
| **CmdPal: `IDynamicListAsync`** | Switch our scan results list loading to async — prevents CmdPal freezing during scans |
| **CmdPal: Adaptive Cards in detail pane** | Use for disk usage charts in the CmdPal result detail pane |
| **CmdPal: `IFormContent2` with action IDs** | Add inline scan settings forms in CmdPal |
| **CmdPal: Per-command enable/disable** | Wire up independent enable/disable for each of our 3 top-level commands |
| **CmdPal: Reusable extension settings controls** | Use for hidden files, max depth, excluded paths — no custom settings UI needed |
| **PowerToys Run v2 (in development)** | Watch for async plugin API — future migration needed |
| **CmdPal API breaking changes (Issue #41125)** | `IGridProperties`, `IExtendedAttributesProvider` may be renamed before 1.0; treat as unstable |
| **Win11 Run dialog integration** | CmdPal extensions now also run in Windows 11 Run dialog — valuable free reach |
| **Extension Gallery (WinGet-backed)** | Manual MSIX distribution deprecated for CmdPal — submit PR to `microsoft/CmdPal-Extensions` |

---

## ✅ Releases & History

| Release | Date | Highlights |
|---|---|---|
| **v1.5.1** | 2026-09-09 | Hotfix: 100% self-contained Windows App SDK runtime packaging, hardened Mica system backdrop initialization, Top Files DataGrid division-by-zero layout cycle fix on unmeasured tabs, and synchronized version metadata |
| **v1.5.0** | 2026-09-07 | Donut chart tab, Top 100 Files whole-drive scan, Old & Large quick filter, Recycle Bin context menu, `ds recent` history, richer subtitles, full keyboard navigation, collapsible WPF donut panel, live theme sync, unicode shaded usage bars, native CmdPal settings, and async queries |
| **v1.4.1** | 2026-08-21 | Store patch: less intrusive rating prompt (fires once after 10 scans, 5s delay), Store version policy fix |
| **v1.4.0** | 2026-08-07 | Explorer context menu, icon extraction, CmdPal top-level commands, CSV export resilience, live item count, zero-crash telemetry fixes |

---

## 🚀 v1.5.0 — *Visualization, Depth & Platform Alignment* (Completed ✅)
**Released: September 7, 2026**
**Theme: Make every component more powerful, more visual, and aligned with PowerToys v0.101 APIs**

### 🖥️ Standalone App
- [x] **Pie / Donut Chart Toggle** — interactive visual alternate for folder space distribution alongside the existing bar chart; click slices to drill into subfolders
- [x] **"Top Largest Files" Tab** — dedicated tab scanning the entire selected drive, listing the 100 biggest files across all subfolders (size, path, extension, last modified, system files included)
- [x] **"Large & Old Files" Smart Filter** — one-click filter: files >100 MB not accessed/modified in 12+ months with live badge count
- [x] **Send to Recycle Bin** — right-click context menu "Send to Recycle Bin" on files and folders directly from the DataGrid (safe delete, recoverable)
- [x] **Enhanced CSV Export for Excel** — export formatted `.csv` files with raw numeric `Size (Bytes)` and `Allocated Size (Bytes)` columns alongside UTF-8 BOM encoding for direct numeric sorting in Microsoft Excel

### 🔍 PowerToys Run Plugin
- [x] **Scan History / Recents** — `ds recent` command shows the last 5 scanned paths for quick re-scan without retyping
- [x] **Result Count in Subtitle** — show item count + total size in the plugin result subtitle line (e.g. `C:\Users → 42.3 GB · 15,823 items`) for at-a-glance info without opening GUI
- [x] **High-Contrast Usage Bars** — unicode contrast blocks (`█` / `░`) ensuring high legibility across dark and light themes
- [ ] **Monitor for `Community.PowerToys.Run.Plugin.Dependencies` v0.98.0+** — upgrade when available to stay aligned with PowerToys Run host; currently on v0.97.0 ✅
- [ ] **Prepare for PowerToys Run v2** — audit `IPlugin`/`IContextMenu`/`ISettingProvider` usage and begin planning async migration

### 🪟 PowerToys Run GUI (DiskAnalyzerWindow)
- [x] **Pie / Donut Chart Side Panel** — collapsible side panel showing a donut chart for the currently selected folder
- [x] **Full Keyboard Navigation** — `Enter` to drill in, `Backspace` to go up, `F5` to rescan — keyboard-only flow matching PowerToys' own keyboard-first philosophy
- [x] **Live Theme Sync** — detect system `UserPreferenceChanged` events at runtime and apply dark/light without a restart

### 🎨 Command Palette Extension
- [x] **Switch to Async Scanning** — load scan results asynchronously to prevent CmdPal freezing during large folder scans
- [x] **Extension Settings via Native Controls** — native `CommandProvider.Settings` integration for "Show Hidden Files" and "Max Scan Depth"
- [ ] **Adaptive Cards Detail Pane** — render a disk usage mini-chart and stats in the CmdPal item detail pane
- [ ] **Submit to CmdPal Extension Gallery** — submit PR to `microsoft/CmdPal-Extensions` so users can install via WinGet from within CmdPal's settings (no manual MSIX download needed)

---

## 🔭 v1.6.0 — *Multi-Layer Representation & Customization*
**Target: ~4 weeks**

### 🖥️ Standalone App
- [ ] **Sunburst / Treemap Chart** — zoomable multi-layer chart; click to drill down through folder hierarchy without leaving the chart view
- [ ] **Custom Accent Themes** — Fluent accent color picker to match user's Windows 11 system accent color

### 🔍 PowerToys Run Plugin + GUI
- [ ] **Results Grouping** — group results by drive, category (Documents, Videos, etc.), or size tier in the Run results list

### 🎨 Command Palette Extension
- [ ] **Per-Command Enable/Disable** — independently enable/disable each of our 3 top-level CmdPal commands using granular toggles
- [ ] **Native `IExplorerCommand` COM Server** — promote the Windows Explorer right-click entry to a **top-level** context menu item in Windows 11 (no more "Show more options" sub-menu)
- [ ] **`IFormContent2` Inline Scan Forms** — use action-ID form support to present scan parameter pickers inline in CmdPal (folder depth selector, hidden file toggle)
- [ ] **CmdPal Compact Mode Adaptation** — ensure our extension gracefully collapses to icon + short name in PowerToys v0.101 compact mode (no truncated/broken UI)

---

## 🗑️ v1.7.0 — *Actionable Management & Automation*
**Target: ~6 weeks**

### All Components
- [ ] **Safe Delete to Recycle Bin** — full-surface delete support from Run Plugin, GUI, CmdPal, and Standalone App
- [ ] **Scheduled Scan Reports** — configure weekly scan of a folder; Windows toast notification summarizes size changes and new large files since last scan
- [ ] **Winget Manifest Auto-Update** — GitHub Actions workflow that submits WinGet manifest PR on every tagged GitHub release (currently manual)

---

## 🔬 Ongoing / Housekeeping

- [ ] **Upgrade `System.Drawing.Common` 8.0.0 → 9.0.0** — resolve MSB3277 assembly conflict build warnings
- [ ] **Upgrade `CommunityToolkit.WinUI` 7.1.2 → latest stable** when released
- [ ] **Upgrade `WindowsSdkPackageVersion`** in CmdPal extension from `10.0.26100.68-preview` to stable when available
- [ ] **Monitor `Microsoft.CommandPalette.Extensions`** for stable 1.0 API graduation (currently `0.9.x` preview; treat `IGridProperties` and `IExtendedAttributesProvider` as unstable per Issue #41125)
- [ ] **Add GitHub Actions CI** — build validation on every PR to catch regressions automatically
- [ ] **CmdPal API 1.0 Migration** — once Issue #41125 closes, do a full API review pass and update all extension code to stable interfaces
