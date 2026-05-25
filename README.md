# DiskAnalyzer  PowerToys Run Plugin

A [PowerToys Run](https://aka.ms/PowerToysOverview) plugin that brings **disk usage analysis** directly into your launcher. Instantly explore drive and folder sizes without leaving your keyboard.

![PowerToys Run](https://img.shields.io/badge/PowerToys%20Run-Plugin-blue) ![Version](https://img.shields.io/badge/version-1.0.2-green) ![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

---

## Features

- List all drives with used / free / total space
- - Browse any folder and see children ranked by size
  - - Recursively find the largest files and folders inside any path
    - - Accurate sizes for cloud folders (iCloud, OneDrive)
      - - Keyboard-first  clicking a result keeps the `ds` prefix so you can keep drilling down
       
        - ---

        ## Requirements

        - [Microsoft PowerToys](https://github.com/microsoft/PowerToys) v0.97.0 or later
        - - Windows 10 / 11 (x64 or ARM64)
         
          - ---

          ## Installation

          1. Download the zip for your architecture from [Releases](https://github.com/thetsaw/PowerToys.Plugin/releases/latest)
          2.    - `DiskAnalyzer-1.0.2-x64.zip`
                -    - `DiskAnalyzer-1.0.2-ARM64.zip`
                     - 2. Close PowerToys.
                       3. 3. Extract the zip into:
                          4.    ```
                                   %LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\DiskAnalyzer\
                                   ```
                                4. Restart PowerToys.
                                5. 5. Open PowerToys Run (`Alt+Space`) and type `ds ` to get started.
                                  
                                   6. ---
                                  
                                   7. ## Usage
                                  
                                   8. All commands begin with the `ds` keyword.
                                  
                                   9. | Command | Result |
                                   10. |---|---|
                                   11. | `ds C:\` | List top-level folders sorted by size |
                                   12. | `ds C:\Users\Photos` | Drill into any subfolder |
                                   13. | `ds largest C:\Users` | Show the single largest item inside a path |
                                  
                                   14. ---
                                  
                                   15. ## Building from Source
                                  
                                   16. ```bash
                                       git clone https://github.com/thetsaw/PowerToys.Plugin.git
                                       cd PowerToys.Plugin

                                       # x64
                                       dotnet publish -c Release -r win-x64 --self-contained false -o publish/x64

                                       # ARM64
                                       dotnet publish -c Release -r win-arm64 --self-contained false -o publish/arm64
                                       ```

                                       Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later.

                                       ---

                                       ## License

                                       MIT  [thetsaw](https://github.com/thetsaw)
