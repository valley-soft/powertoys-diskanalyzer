using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer
{
    public static class InstallManager
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, uint dwFlags);

        private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;

        private static void ScheduleDeleteOnReboot(string path)
        {
            try
            {
                MoveFileEx(path, null, MOVEFILE_DELAY_UNTIL_REBOOT);
            }
            catch { }
        }

        private static bool IsPackageInstalled(string packageName)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    $"-NoProfile -ExecutionPolicy Bypass -Command \"Get-AppxPackage -Name '{packageName}'\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var p = Process.Start(psi);
                if (p == null) return false;
                string stdout = p.StandardOutput.ReadToEnd();
                p.WaitForExit(10000);
                return !string.IsNullOrWhiteSpace(stdout);
            }
            catch
            {
                return false;
            }
        }

        public static void PerformInstall(bool isCleanInstall, bool installPlugin, bool installCmdPal, bool installApp, Action<string> log)
        {
            if (!installPlugin && !installCmdPal && !installApp)
            {
                log("Nothing selected to install.");
                return;
            }

            log("Extracting installation payload...");

            string tempDir = Path.Combine(Path.GetTempPath(), "DiskAnalyzer_Install_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // Extract embedded payload zip
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer.payload.zip";
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) throw new Exception("Payload zip not found inside the installer executable.");
                    using (var archive = new ZipArchive(stream))
                        archive.ExtractToDirectory(tempDir);
                }

                if (installPlugin) InstallPowerToysPlugin(tempDir, isCleanInstall, log);
                if (installCmdPal) InstallMsix(Path.Combine(tempDir, "ValleySoft.CmdPal.msix"), "Command Palette Extension", log);
                if (installApp)    InstallMsix(Path.Combine(tempDir, "ValleySoft.StandaloneApp.msix"), "Standalone App", log);

                log("\nINSTALLATION COMPLETE!");
            }
            catch (Exception ex)
            {
                log($"\nERROR: {ex.Message}");
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PowerToys Run Plugin
        // ─────────────────────────────────────────────────────────────────────
        private static void InstallPowerToysPlugin(string extractDir, bool cleanInstall, Action<string> log)
        {
            log("\nInstalling PowerToys Run Plugin...");

            string localAppData  = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string ptRunDir      = Path.Combine(localAppData, "Microsoft", "PowerToys", "PowerToys Run", "Plugins", "DiskAnalyzer");
            string payloadDir    = Path.Combine(extractDir, "Plugin");
            string settingsFile  = Path.Combine(ptRunDir, "settings.json");
            string? settingsBackup = null;

            if (!Directory.Exists(payloadDir))
            {
                log("Warning: Plugin binaries missing from payload.");
                return;
            }

            // Back up settings for Express upgrade
            if (!cleanInstall && File.Exists(settingsFile))
            {
                try { settingsBackup = File.ReadAllText(settingsFile); }
                catch { }
            }

            // Kill PowerToys processes
            log("Closing PowerToys...");
            foreach (var name in new[] { "PowerToys", "PowerToys.PowerLauncher", "PowerToys.Settings", "PowerToys.Peek" })
            {
                foreach (var proc in Process.GetProcessesByName(name))
                {
                    try { proc.Kill(); proc.WaitForExit(3000); } catch { }
                }
            }
            System.Threading.Thread.Sleep(2000);

            // Write a copy script and run it via a scheduled task (runs as SYSTEM — bypasses Program Files lock)
            string copyScript = Path.Combine(Path.GetTempPath(), "da_plugin_copy.ps1");
            string statusFile = Path.Combine(Path.GetTempPath(), "da_install_status.txt");
            if (File.Exists(statusFile))
            {
                try { File.Delete(statusFile); } catch { }
            }
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string ptExe = Path.Combine(programFiles, "PowerToys", "PowerToys.exe");

            File.WriteAllText(copyScript,
                $"$src = '{payloadDir}'\r\n" +
                $"$dst = '{ptRunDir}'\r\n" +
                $"$status = '{statusFile}'\r\n" +
                "Set-Content -Path $status -Value 'Started'\r\n" +
                $"$oldSystemDir = '{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerToys", "modules", "launcher", "Plugins", "DiskAnalyzer")}'\r\n" +
                "if (Test-Path $oldSystemDir) { Remove-Item -Path $oldSystemDir -Recurse -Force -ErrorAction SilentlyContinue }\r\n" +
                "if (Test-Path $dst) { Remove-Item -Path $dst -Recurse -Force -ErrorAction SilentlyContinue }\r\n" +
                "if (!(Test-Path $dst)) { New-Item -ItemType Directory -Path $dst -Force | Out-Null }\r\n" +
                "Get-ChildItem -Path $src -Recurse | ForEach-Object {\r\n" +
                "    $rel = $_.FullName.Substring($src.Length + 1)\r\n" +
                "    $target = Join-Path $dst $rel\r\n" +
                "    if ($_.PSIsContainer) {\r\n" +
                "        if (!(Test-Path $target)) { New-Item -ItemType Directory -Path $target -Force | Out-Null }\r\n" +
                "    } else {\r\n" +
                "        if (Test-Path $target) {\r\n" +
                "            try {\r\n" +
                "                Copy-Item -Path $_.FullName -Destination $target -Force\r\n" +
                "            } catch {\r\n" +
                "                $oldPath = $target + '.' + [Guid]::NewGuid().ToString('N').Substring(0,8) + '.old'\r\n" +
                "                try {\r\n" +
                "                    Rename-Item -Path $target -NewName [System.IO.Path]::GetFileName($oldPath) -Force\r\n" +
                "                    Copy-Item -Path $_.FullName -Destination $target -Force\r\n" +
                "                } catch {\r\n" +
                "                    # Failed to rename or copy\r\n" +
                "                }\r\n" +
                "            }\r\n" +
                "        } else {\r\n" +
                "            Copy-Item -Path $_.FullName -Destination $target -Force\r\n" +
                "        }\r\n" +
                "    }\r\n" +
                "}\r\n" +
                "Set-Content -Path $status -Value 'Done'\r\n"
            );

            // Use schtasks to run as SYSTEM (elevated, no UAC needed since we're already admin)
            string taskName = "DiskAnalyzerPluginInstall_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            RunCmd("schtasks", $"/Create /F /TN \"{taskName}\" /TR \"powershell.exe -NonInteractive -ExecutionPolicy Bypass -File '{copyScript}'\" /SC ONCE /ST 00:00 /RU SYSTEM", log);
            RunCmd("schtasks", $"/Run /TN \"{taskName}\"", log);

            log("Copying plugin files...");
            
            // Loop checking status
            int elapsed = 0;
            while (elapsed < 15000) // 15 seconds max
            {
                if (File.Exists(statusFile))
                {
                    try
                    {
                        string content = File.ReadAllText(statusFile).Trim();
                        if (content == "Done")
                        {
                            break;
                        }
                    }
                    catch { }
                }
                System.Threading.Thread.Sleep(500);
                elapsed += 500;
            }

            RunCmd("schtasks", $"/Delete /F /TN \"{taskName}\"", log);

            try { File.Delete(copyScript); } catch { }
            try { if (File.Exists(statusFile)) File.Delete(statusFile); } catch { }

            // Verify copy
            string mainDll = Path.Combine(ptRunDir, "Community.PowerToys.Run.Plugin.DiskAnalyzer.dll");
            if (File.Exists(mainDll))
            {
                log($"Plugin installed successfully to {ptRunDir}");

                // Restore settings
                if (settingsBackup != null)
                {
                    try { File.WriteAllText(settingsFile, settingsBackup); log("Settings restored."); }
                    catch { }
                }
            }
            else
            {
                log("Warning: Plugin DLL not found after copy. Check that the installer ran as Administrator.");
            }

            // Restart PowerToys (runs in user session since the installer runs in the user session)
            log("Restarting PowerToys...");
            if (File.Exists(ptExe))
            {
                try { Process.Start(ptExe); } catch { }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MSIX packages (CmdPal + Standalone App)
        // ─────────────────────────────────────────────────────────────────────
        private static void InstallMsix(string msixPath, string componentName, Action<string> log)
        {
            log($"\nInstalling {componentName}...");

            if (!File.Exists(msixPath))
            {
                log($"Warning: {componentName} MSIX missing from payload.");
                return;
            }

            // Import certificate if present alongside the MSIX
            string? dirName = Path.GetDirectoryName(msixPath);
            if (dirName != null)
            {
                string cerPath = Path.Combine(dirName, "ValleySoft.cer");
                if (File.Exists(cerPath))
                {
                    RunPowerShell($"Import-Certificate -FilePath '{cerPath}' -CertStoreLocation Cert:\\LocalMachine\\TrustedPeople", log, silent: true);
                }
            }

            // Step 1: Remove any existing package with the same publisher/package name to avoid 0x80073CFB
            string packageName = componentName.Contains("Standalone") ? "B66E5954-FDAC-43E7-B4F4-EC969822E519" : "DiskAnalyzerExtension";
            log("Checking for existing installation...");
            if (IsPackageInstalled(packageName))
            {
                log($"Existing {componentName} package found. Uninstalling...");
                string removeCmd = $"Get-AppxPackage -Name '{packageName}' | Remove-AppxPackage";
                RunPowerShell(removeCmd, log, silent: true);

                // Wait and validate it is gone
                int checkRetries = 10;
                while (checkRetries-- > 0 && IsPackageInstalled(packageName))
                {
                    System.Threading.Thread.Sleep(500);
                }

                if (IsPackageInstalled(packageName))
                {
                    log($"Warning: Failed to uninstall existing {componentName} via standard user command. Attempting all-users package removal...");
                    RunPowerShell($"Get-AppxPackage -Name '{packageName}' -AllUsers | Remove-AppxPackage -AllUsers", log, silent: true);

                    checkRetries = 10;
                    while (checkRetries-- > 0 && IsPackageInstalled(packageName))
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                }

                if (IsPackageInstalled(packageName))
                {
                    log($"Error: Could not uninstall old package ID for {componentName}. The installation may fail.");
                }
                else
                {
                    log("Old package uninstalled successfully.");
                }
            }
            else
            {
                log("No existing installation found.");
            }

            // Step 2: Install fresh
            log($"Deploying {componentName}...");
            string installCmd = $"Add-AppxPackage -Path '{msixPath}'";

            var result = RunPowerShellWithResult(installCmd, log);
            if (result.exitCode == 0)
            {
                log($"Success: {componentName} installed.");
            }
            else
            {
                log($"Failed: {componentName} deployment failed (exit {result.exitCode}).");
                if (!string.IsNullOrWhiteSpace(result.error))
                    log($"Error: {result.error.Trim()}");

                // Fallback: copy MSIX to desktop
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fallback = Path.Combine(desktop, Path.GetFileName(msixPath));
                try
                {
                    File.Copy(msixPath, fallback, true);
                    log($"Action Required: Double-click to install manually: {fallback}");
                }
                catch { }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        private static void RunCmd(string exe, string args, Action<string> log)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(10000);
            }
            catch (Exception ex)
            {
                log($"  cmd error: {ex.Message}");
            }
        }

        private static void RunPowerShell(string command, Action<string>? log, bool silent = false)
        {
            RunPowerShellWithResult(command, silent ? null : log);
        }

        private static (int exitCode, string error) RunPowerShellWithResult(string command, Action<string>? log)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                if (p == null) return (-1, "Failed to start PowerShell process");
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(30000);

                if (!string.IsNullOrWhiteSpace(stdout) && log != null)
                    log(stdout.Trim());

                return (p.ExitCode, stderr);
            }
            catch (Exception ex)
            {
                log?.Invoke($"  PowerShell error: {ex.Message}");
                return (-1, ex.Message);
            }
        }
    }
}
