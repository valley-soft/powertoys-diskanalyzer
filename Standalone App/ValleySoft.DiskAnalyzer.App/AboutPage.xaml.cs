using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ValleySoft_DiskAnalyzer_App;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        this.InitializeComponent();

        // Read version dynamically from package manifest — never drifts out of sync
        try
        {
            var ver = Windows.ApplicationModel.Package.Current.Id.Version;
            VersionTextBlock.Text = $"Version {ver.Major}.{ver.Minor}.{ver.Build}";
        }
        catch
        {
            // Unpackaged fallback
            VersionTextBlock.Text = "Version 1.4.0";
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.Frame.CanGoBack)
        {
            this.Frame.GoBack();
        }
    }
}
