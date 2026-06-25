# Microsoft Store Publishing

This folder contains everything you need to bundle the application for upload to the **Microsoft Partner Center** (Microsoft Store).

Because the standalone WinUI 3 App and the Command Palette Extension are unified into the same `.msix` container, publishing to the Microsoft Store covers both components seamlessly!

## Instructions

1. **Set your Identity (One-Time Setup)**
   Before building for the Store, ensure your `Package.appxmanifest` and `.csproj` are configured with your official Partner Center Identity.
   You must replace the identity names with your official reserved ones from the Microsoft Partner Center:
   * Edit `Standalone App\ValleySoft.DiskAnalyzer.App\Package.appxmanifest` (`<Identity Name="..." Publisher="..." />`)
   * Edit `Standalone App\ValleySoft.DiskAnalyzer.App\ValleySoft.DiskAnalyzer.App.csproj` (`<AppxPackageIdentityName>` and `<AppxPackagePublisher>`)

2. **Run the Build Script**
   Simply run the script in this folder:
   ```powershell
   .\build-store-bundle.ps1
   ```
   *Note: This script passes the MSBuild flag `-p:UapAppxPackageBuildMode=StoreUpload`, which strips out local test certificates and prepares the package for Microsoft's ingestion pipeline.*

3. **Upload to Partner Center**
   The script will generate a single file: `ValleySoft.DiskAnalyzer_StoreBundle.msixbundle`.
   Upload this bundle directly to the Packages section of your Partner Center submission!
