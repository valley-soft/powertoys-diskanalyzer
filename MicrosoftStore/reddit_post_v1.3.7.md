**Title:** Release: Disk Analyzer v1.3.7 — Emergency Hotfix & 100% Stability Patch! 🚀

Hey everyone!

Following up on our recent v1.3.6 release, I've just published **v1.3.7** of **ValleySoft Disk Analyzer** to GitHub and the Microsoft Store.

This emergency hotfix release resolves 100% of telemetry crash reports and background process hangs:

### 🛠️ What's Fixed in v1.3.7:
1. **BitLocker & Drive Permission Crashes Fixed**: Fixed an issue where `DriveInfo.IsReady` was evaluated outside safe `try-catch` blocks, causing instant crashes on BitLocker-locked or restricted network drives.
2. **HANG_QUIESCE Background Suspension Fixed**: Wired up `ComServer.Empty` events so that when Windows Command Palette disconnects, the background process cleanly unblocks and shuts down instead of lingering until force-killed by the OS.
3. **Windows Insider Build Compatibility**: Added global exception handling (`e.Handled = true`) and DirectComposition fallback protections for Windows 11 Insider preview builds (OS 26300+).
4. **Clean Production Build**: Stripped all developer trace logs and hardcoded path queries.

---

### 📦 Installation:
- **Microsoft Store (Recommended)**: [Get Disk Analyzer on Microsoft Store](https://apps.microsoft.com/detail/9nf073kltvwn)
- **Winget**: `winget install --id 9NF073KLTVWN --source msstore`
- **GitHub Release**: Download `.msix` or `.exe` directly from [GitHub Releases](https://github.com/valley-soft/powertoys-diskanalyzer/releases/latest)

Thank you all for the feedback and telemetry reports! Enjoy the lightning-fast, crash-free scanning!
