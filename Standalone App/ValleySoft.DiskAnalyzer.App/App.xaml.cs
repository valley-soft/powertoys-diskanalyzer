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
        m_window = new MainWindow();
        m_window.Activate();
    }
}
