**Title:** Release: Disk Analyzer v1.3.6 (PowerToys Run Plugin & WinUI 3 App) - Real-time scanning, context menus, and stability updates!

**Body:**

Hey everyone!

I just pushed version **1.3.6** of **Disk Analyzer** to GitHub and the Microsoft Store.

This release focuses on quality-of-life updates, security hardening, and behind-the-scenes stability fixes based on your feedback:

* **Right-click shortcuts**: You can now right-click any item in the scan table to instantly open it in Explorer or copy the file path.
* **Editable address bar**: Double-click the breadcrumb trail to type or paste folder paths directly, just like Windows File Explorer.
* **Smoother real-time scans**: Restored smooth visual updates to the directory list as your drives are scanned.
* **No more startup freezes or crashes**: Resolved random crashes during deep folder scans and moved drive detection to background threads so the app never shows "Not Responding" on launch.
* **Decoupled sideloading setup**: Separated the PowerToys Run Plugin (.exe installer) from the Standalone WinUI App (.msix) to ensure a clean manual installation, and resolved manual certificate verification errors (`0x800B010A`).

Check it out on GitHub: https://github.com/valley-soft/powertoys-diskanalyzer
Or download it directly from the Microsoft Store!

Thanks for the feedback and support! Let me know if you run into any issues.

