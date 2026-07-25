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

namespace ValleySoft_DiskAnalyzer_App;

public partial class App : Application
{
    private MainWindow? m_window;
    public MainWindow? MainWindow => m_window;

    public App()
    {
        InitializeComponent();
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
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            bool alwaysAdmin = localSettings.Values["AlwaysRunAsAdmin"] as bool? ?? false;
            
            if (alwaysAdmin && !IsAdministrator())
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -WindowStyle Hidden -Command \"Start-Process 'ValleySoft.DiskAnalyzer.exe' -Verb RunAs\"",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(startInfo);
                System.Environment.Exit(0);
                return;
            }
        }
        catch { }

        try
        {
            m_window = new MainWindow();
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
            System.Environment.Exit(1);
        }
    }

    private static bool IsAdministrator()
    {
        using (System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent())
        {
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
