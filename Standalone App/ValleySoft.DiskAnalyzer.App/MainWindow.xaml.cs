using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ValleySoft_DiskAnalyzer_App;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

            AppWindow.SetIcon(System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "AppIcon.ico"));

            // Load saved theme
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue("Theme", out var themeObj) && themeObj is string tag)
                {
                    if (Content is FrameworkElement frameworkElement)
                    {
                        if (tag == "Light")
                            frameworkElement.RequestedTheme = ElementTheme.Light;
                        else if (tag == "Dark")
                            frameworkElement.RequestedTheme = ElementTheme.Dark;
                        else
                            frameworkElement.RequestedTheme = ElementTheme.Default;
                    }
                }
            }
            catch { }

            RootFrame.Navigate(typeof(MainPage));
        }
        catch (System.Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "crash_main.txt"),
                ex.ToString());
        }
    }
}
