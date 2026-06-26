# Support for ValleySoft Disk Analyzer

Welcome to the ValleySoft Disk Analyzer support page! 

If you are experiencing issues, have a feature request, or need help understanding your disk usage, we are here to help.

## Contact Us

The fastest way to get support is to email us directly:
📧 **[valleysoftdev29@gmail.com](mailto:valleysoftdev29@gmail.com)**

When reaching out for bug reports, please include:
1. Your Windows version (e.g., Windows 11 23H2)
2. The version of Disk Analyzer you are using (e.g., v1.3.3)
3. A brief description of the issue or a screenshot

## Frequently Asked Questions

**Why does the app need to Run as Administrator?**
Windows protects critical system folders (like `C:\Windows` and `C:\ProgramData`). Without administrator privileges, Disk Analyzer cannot read the sizes of the files inside these folders, which will cause hundreds of gigabytes of your storage to appear "missing" from the scan. Elevating the app allows it to read these file sizes accurately. The app is strictly read-only and never modifies system files.

**How do I use the PowerToys Run integration?**
If you installed the full package, simply open PowerToys Run (usually `Alt + Space`) and type `disk` followed by the drive or folder you want to scan (e.g., `disk C:\`).

**Where can I download the latest version?**
You can always find the latest installers on our [GitHub Releases](https://github.com/valley-soft/powertoys-diskanalyzer/releases) page.
