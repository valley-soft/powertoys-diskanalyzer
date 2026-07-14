**Title:** Release: Disk Analyzer v1.3.5 (PowerToys Run Plugin & WinUI 3 App) - Restored real-time scanning UI, certificate fixes, and performance updates!

**Body:**

Hey everyone!

I just pushed version **1.3.5** of **Disk Analyzer** to GitHub and the Microsoft Store.

This release focuses on quality-of-life updates and behind-the-scenes stability fixes based on your feedback:

* **Restored smooth real-time scanning**: The scanning UI now streams progress smoothly as it traverses your drive (fixed the stuttering/frozen UI behavior from the previous release).
* **Decoupled sideloading installers**: Separated the PowerToys Run Plugin (executable installer) from the Standalone WinUI App (MSIX) for much easier manual installation.
* **Certificate fixes**: Resolved the certificate verification errors (`0x800B010A`) on the self-signed MSIX package.
* **Command Palette stability**: Cleaned up legacy registry index configurations for Microsoft's new Command Palette.

Check it out on GitHub: https://github.com/valley-soft/powertoys-diskanalyzer
Or download it directly from the Microsoft Store!

Thanks for the feedback and support! Let me know if you run into any issues.
