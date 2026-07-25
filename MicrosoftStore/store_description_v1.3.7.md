## What's new in v1.3.7.0
- **Stability**: Resolved all BitLocker and restricted drive enumeration crashes by safely catching property queries inside DriveInfo getters.
- **Resource Management**: Fixed HANG_QUIESCE background process hangs by automatically listening to COM host disconnect events (`ComServer.Empty`) and unblocking process shutdown.
- **Crash Prevention**: Added global exception handling (`e.Handled = true`) and DirectComposition fallback protections for Windows Insider preview builds.
- **Clean Build**: Removed development trace logs and hardcoded user profile paths for zero-impact production execution.
