using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Linq;

namespace ValleySoft_DiskAnalyzer_App;

public partial class App : Application
{
    private MainWindow? m_window;
    public MainWindow? MainWindow => m_window;

    public App()
    {
        InitializeComponent();

        // WinUI managed exceptions (UI thread)
        this.UnhandledException += (s, e) =>
        {
            e.Handled = true; // Prevent unhandled process termination where possible
            try
            {
                var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ValleySoft.DiskAnalyzer");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(folder, "app_crash.log"),
                    e.Exception.ToString() + "\nMessage: " + e.Message);
            }
            catch { }
        };

        // Background thread crashes not caught by WinUI — covers uncategorized telemetry events
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ValleySoft.DiskAnalyzer");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(folder, "app_domain_crash.log"),
                    e.ExceptionObject?.ToString() ?? "Unknown exception");
            }
            catch { }
        };

        // Prevent unobserved Task exceptions (fire-and-forget) from terminating the process
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved();
        };
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        string initialPath = "";
        RegisterContextMenu();

        try
        {
            var cmdArgs = System.Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1)
            {
                for (int i = 1; i < cmdArgs.Length; i++)
                {
                    if (cmdArgs[i].Equals("--path", StringComparison.OrdinalIgnoreCase) && i + 1 < cmdArgs.Length)
                    {
                        initialPath = cmdArgs[i + 1];
                        break;
                    }
                    else if (!cmdArgs[i].StartsWith("-") && (System.IO.Directory.Exists(cmdArgs[i]) || System.IO.File.Exists(cmdArgs[i]) || System.IO.DriveInfo.GetDrives().Any(d => d.Name.Equals(cmdArgs[i], StringComparison.OrdinalIgnoreCase))))
                    {
                        initialPath = cmdArgs[i];
                        break;
                    }
                }
            }
        }
        catch { }

        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            bool alwaysAdmin = localSettings.Values["AlwaysRunAsAdmin"] as bool? ?? false;
            
            if (alwaysAdmin && !IsAdministrator())
            {
                string aliasPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "WindowsApps", "ValleySoft.DiskAnalyzer.exe");

                bool aliasExists = false;
                try
                {
                    var attr = System.IO.File.GetAttributes(aliasPath);
                    if (attr != (System.IO.FileAttributes)(-1))
                    {
                        aliasExists = true;
                    }
                }
                catch { }

                string exePath = aliasExists ? aliasPath
                    : System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                      ?? "ValleySoft.DiskAnalyzer.exe";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Verb = "runas",           // Triggers UAC elevation prompt
                    UseShellExecute = true,   // Required for Verb to work
                    CreateNoWindow = false
                };

                if (!string.IsNullOrEmpty(initialPath))
                {
                    startInfo.Arguments = $"--path \"{initialPath}\"";
                }

                try
                {
                    System.Diagnostics.Process.Start(startInfo);
                    System.Environment.Exit(0);
                    return;
                }
                catch
                {
                    // User cancelled UAC prompt — continue launching as normal user
                }
            }
        }
        catch { }

        try
        {
            m_window = new MainWindow(initialPath);
            m_window.Activate();
        }
        catch (Exception ex)
        {
            try
            {
                var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ValleySoft.DiskAnalyzer");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "dcomp_init_crash.log"), ex.ToString());
            }
            catch { }

            string errorMessage = "A critical system initialization error occurred.\n\n" +
                                   "This is often caused by composition or display driver issues on pre-release Windows Insider builds (e.g. missing dcompi.dll or composition registration).\n\n" +
                                   $"Details: {ex.Message}\n\n" +
                                   "Please ensure your graphics drivers and Windows installation are up to date.";
            
            MessageBox(IntPtr.Zero, errorMessage, "Disk Analyzer - Initialization Error", 0x00000010 /* MB_ICONERROR */);
            System.Environment.Exit(1);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    private static bool IsAdministrator()
    {
        using (System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent())
        {
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }

    private static void RegisterContextMenu()
    {
        try
        {
            string aliasPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WindowsApps", "ValleySoft.DiskAnalyzer.exe");

            bool aliasExists = false;
            try
            {
                var attr = System.IO.File.GetAttributes(aliasPath);
                if (attr != (System.IO.FileAttributes)(-1))
                {
                    aliasExists = true;
                }
            }
            catch { }

            string exePath = aliasExists ? aliasPath
                : System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                  ?? "ValleySoft.DiskAnalyzer.exe";

            string appDataFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ValleySoft.DiskAnalyzer");
            System.IO.Directory.CreateDirectory(appDataFolder);

            string iconPath = System.IO.Path.Combine(appDataFolder, "AppIcon.ico");
            if (!System.IO.File.Exists(iconPath))
            {
                try
                {
                    string installedIcon = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                    if (System.IO.File.Exists(installedIcon))
                    {
                        System.IO.File.Copy(installedIcon, iconPath, true);
                    }
                }
                catch { }
            }

            string iconTarget = System.IO.File.Exists(iconPath) ? iconPath : exePath;

            // 1. Directory Context Menu
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell\ValleySoft.DiskAnalyzer"))
            {
                if (key != null)
                {
                    key.SetValue("", "Analyze with DiskAnalyzer");
                    key.SetValue("Icon", iconTarget);
                    using (var cmdKey = key.CreateSubKey("command"))
                    {
                        cmdKey?.SetValue("", $"\"{exePath}\" --path \"%1\"");
                    }
                }
            }

            // 2. Directory Background Context Menu
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\Background\shell\ValleySoft.DiskAnalyzer"))
            {
                if (key != null)
                {
                    key.SetValue("", "Analyze with DiskAnalyzer");
                    key.SetValue("Icon", iconTarget);
                    using (var cmdKey = key.CreateSubKey("command"))
                    {
                        cmdKey?.SetValue("", $"\"{exePath}\" --path \"%V\"");
                    }
                }
            }

            // 3. Drive Context Menu
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\Drive\shell\ValleySoft.DiskAnalyzer"))
            {
                if (key != null)
                {
                    key.SetValue("", "Analyze with DiskAnalyzer");
                    key.SetValue("Icon", iconTarget);
                    using (var cmdKey = key.CreateSubKey("command"))
                    {
                        cmdKey?.SetValue("", $"\"{exePath}\" --path \"%1\"");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error registering context menu: {ex.Message}");
        }
    }
}
