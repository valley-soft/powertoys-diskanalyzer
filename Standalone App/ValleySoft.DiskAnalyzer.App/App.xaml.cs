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
    private Window m_window;
    public Window MainWindow => m_window;

    public App()
    {
        InitializeComponent();
        this.UnhandledException += (s, e) =>
        {
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "crash.txt"), 
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
                    UseShellExecute = true,
                    WorkingDirectory = System.Environment.CurrentDirectory,
                    FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "ValleySoft.DiskAnalyzer.exe",
                    Verb = "runas"
                };
                System.Diagnostics.Process.Start(startInfo);
                System.Environment.Exit(0);
                return;
            }
        }
        catch { }

        m_window = new MainWindow();
        m_window.Activate();
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
