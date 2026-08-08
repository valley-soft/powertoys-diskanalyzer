using Microsoft.UI.Xaml;
using System;

namespace ValleySoft_DiskAnalyzer_App;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow(string initialPath = "")
    {
        try
        {
            InitializeComponent();



            try
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }
            catch { }

            try
            {
                SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            }
            catch { }

            try
            {
                AppWindow.SetIcon(System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
            }
            catch { }

            // Load saved theme
            string savedTheme = "Default";
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue("Theme", out var themeObj) && themeObj is string tag)
                {
                    savedTheme = tag;
                }
            }
            catch { }

            SetAppTheme(savedTheme);

            RootFrame.Navigate(typeof(MainPage), initialPath);
        }
        catch (System.Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "crash_main.txt"),
                ex.ToString());
        }
    }

    public void SetAppTheme(string tag)
    {
        ElementTheme theme = tag switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (Content is FrameworkElement frameworkElement)
        {
            frameworkElement.RequestedTheme = theme;
        }

        try
        {
            if (AppTitleBar != null)
            {
                AppTitleBar.RequestedTheme = theme;
            }

            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar != null)
            {
                bool isDark = theme == ElementTheme.Dark || 
                    (theme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);

                if (isDark)
                {
                    AppWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                    AppWindow.TitleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                    AppWindow.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(40, 255, 255, 255);
                    AppWindow.TitleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                    AppWindow.TitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(70, 255, 255, 255);
                    AppWindow.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(120, 255, 255, 255);
                }
                else
                {
                    // Light mode: High-contrast sharp black caption buttons (#0F172A)
                    AppWindow.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 15, 23, 42);
                    AppWindow.TitleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 15, 23, 42);
                    AppWindow.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(40, 0, 0, 0);
                    AppWindow.TitleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 15, 23, 42);
                    AppWindow.TitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(70, 0, 0, 0);
                    AppWindow.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(140, 15, 23, 42);
                }
            }
        }
        catch { }
    }
}
