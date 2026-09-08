# ValleySoft Disk Analyzer – Roadmap

## Release Cadence
**Target: Bi-Weekly (every two weeks)**

---

## Components

| Component | Description | Current Version |
|---|---|---|
| 🖥️ **Standalone App** | WinUI 3 MSIX app (Microsoft Store + sideload) | 1.5.0.0 |
| 🔍 **PowerToys Run Plugin** | `ds` keyword plugin for PowerToys Run (launcher) | 1.5.0 |
| 🪟 **PowerToys Run GUI** | WPF floating window launched from PowerToys Run | 1.5.0 |
| 🎨 **Command Palette Extension** | Native CmdPal WinRT/COM extension (MSIX) | 1.5.0 |

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

## ✅ v1.4.x — Completed

| Release | Date | Highlights |
|---|---|---|
| **v1.4.0** | 2026-08-07 | Explorer context menu, icon extraction, CmdPal top-level commands, CSV export resilience, live item count, zero-crash telemetry fixes |
| **v1.4.1** | 2026-08-21 | Store patch: less intrusive rating prompt (fires once after 10 scans, 5s delay), Store version policy fix |

---

## 🚀 v1.5.0 — *Visualization, Depth & Platform Alignment*
**Target: ~2 weeks from Sep 7**
**Theme: Make every component more powerful, more visual, and aligned with PowerToys v0.101 APIs**

### 🖥️ Standalone App
- [ ] **Pie / Donut Chart Toggle** — interactive visual alternate for folder space distribution alongside the existing bar chart; click slices to drill into subfolders
- [ ] **"Top Largest Files" Tab** — dedicated tab scanning the entire selected drive, listing the 100 biggest files across all subfolders (size, path, extension, last modified)
- [ ] **"Large & Old Files" Smart Filter** — one-click filter: files >100 MB not accessed/modified in 12+ months — helps users identify safe cleanup targets
- [ ] **Send to Recycle Bin** — right-click context menu "Send to Recycle Bin" on files and folders directly from the DataGrid (safe delete, recoverable)

### 🔍 PowerToys Run Plugin
- [ ] **Monitor for `Community.PowerToys.Run.Plugin.Dependencies` v0.98.0+** — upgrade when available to stay aligned with PowerToys Run host; currently on v0.97.0 ✅
- [ ] **Scan History / Recents** — `ds recent` command shows the last 5 scanned paths for quick re-scan without retyping
- [ ] **Result Count in Subtitle** — show item count + total size in the plugin result subtitle line (e.g. `C:\Users → 42.3 GB · 15,823 items`) for at-a-glance info without opening GUI
- [ ] **Prepare for PowerToys Run v2** — audit `IPlugin`/`IContextMenu`/`ISettingProvider` usage and begin planning async migration

### 🪟 PowerToys Run GUI (DiskAnalyzerWindow)
- [ ] **Pie / Donut Chart Side Panel** — collapsible side panel showing a donut chart for the currently selected folder (feature-parity with Standalone App)
- [ ] **Full Keyboard Navigation** — `Enter` to drill in, `Backspace` to go up, `F5` to rescan — keyboard-only flow matching PowerToys' own keyboard-first philosophy
- [ ] **Live Theme Sync** — detect PowerToys `ActualTheme` changes at runtime and apply dark/light without a restart

### 🎨 Command Palette Extension
- [ ] **Switch to `IDynamicListAsync`** — load scan results asynchronously to prevent CmdPal freezing during large folder scans (PowerToys v0.101 API)
- [ ] **Adaptive Cards Detail Pane** — render a disk usage mini-chart and stats (total size, item count, top files) in the CmdPal item detail pane when a folder is selected
- [ ] **Extension Settings via Native Controls** — use PowerToys v0.101 reusable extension settings controls to add: scan hidden files toggle, max depth, excluded paths list — no custom settings UI needed
- [ ] **Per-Command Enable/Disable** — independently enable/disable each of our 3 top-level CmdPal commands using the new per-command toggle support
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
