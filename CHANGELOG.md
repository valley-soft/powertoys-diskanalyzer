# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-06-21

### Added
- **Fully featured Standalone WinUI 3 App** with a beautiful visual Bar Chart for deeper disk analysis!
- **Unified Installer** features a flawless 1-click Clean Install mode, automatically purging old DLLs from `%LOCALAPPDATA%` to prevent version conflicts.
- Completely separated and distinct display names for the Standalone App, Command Palette Extension, and PowerToys Run plugin to eliminate confusion.
- **Unified the Command Palette Extension and Standalone App into a single MSIX package** for seamless Microsoft Store distribution.

### Changed
- Upgraded the Core project and shared logic to **.NET 10.0** for maximum performance and modern API support.
- Extracted the shared core scanning engine into a perfectly synchronized standard, improving accuracy and maintainability.
- Deeply cleaned the repository, permanently ignoring and removing old legacy build artifacts (e.g. `/AppPackages`, `obj`, `bin`).

### Fixed
- **PowerToys Run `AssemblyLoadContext` bug completely resolved!** Core logic is now natively compiled directly into the plugin instead of using `ProjectReference`, fixing all load failures.
- **Standalone App sizes and calculations** rigorously synced with Windows Explorer to ensure accurate byte-for-byte size reporting.
- Resolved hidden files straggler toggles in XAML and WPF; hidden system files are now properly counted and interactable.
- Fixed severe junction point infinite loop bugs in the directory scanner.
- Fixed mojibake text corruption in several source code files.

## [1.2.0] - 2026-06-14

### Added
- Native **Command Palette MSIX Extension** — type commands directly in CmdPal without a keyword.
- Async background scanning with live *Scanning…* placeholder.
- Interactive drill-down by clicking results in the Command Palette.
- Standalone GUI window (`ds gui`) with full drive/folder tree explorer.
- Expandable left tree pane showing all drives in GUI.
- Right grid with sortable Name, Size, Allocated, Items, Modified columns in GUI.
- Double-click to drill down into folders, and reveal files in File Explorer from GUI.
- `ds ext <path> <extension>` command to find largest files by extension.
- `ds empty <path>` command to find empty folders.
- ARM64 support — separate installer and MSIX for ARM64 devices.
- Single-file native `.exe` installer for both x64 and ARM64.

### Changed
- PowerToys Run plugin is now labeled *DiskAnalyzer (PowerToys Run)* in CmdPal to distinguish from the native extension.

### Fixed
- Disk used space calculation now perfectly matches Windows Explorer.
- Folder size calculation now uses queue-based BFS to avoid reparse point infinite loops.

## [1.1.0] - 2026-06-10

### Added
- "Allocated on Disk" size metrics to scan results.
- Parallel processing for deep directory scans to improve performance.

### Changed
- Updated target framework to `.NET 10.0`.

### Fixed
- Corrected missing plugin icon in PowerToys Run settings.

## [1.0.2] - 2026-05-24

### Changed
- Updated target framework to `.NET 9.0`.
- Updated `Community.PowerToys.Run.Plugin.Dependencies` to `v0.97.0`.

### Fixed
- Mouse click on results removing the `ds` prefix.
- Double `ds` appearing in search bar after selecting a result.
- iCloud and OneDrive showing allocated size instead of actual size.
- Folder sizes not fully recursive (capped at 1 level deep).
- Missing string interpolations and prefixes in `ChangeQuery` calls.

## [1.0.1] - 2026-05-20

### Changed
- **Enter** key now scans the selected drive instead of opening File Explorer.
- "Open in Explorer" moved to **Ctrl+O** context menu.

## [1.0.0] - 2026-05-15

### Added
- Initial release of DiskAnalyzer for PowerToys Run.
- List drives with used / free / total space.
- Browse folder sizes ranked by largest.
- Recursive largest file/folder search.
- Cloud folder support (iCloud, OneDrive).
