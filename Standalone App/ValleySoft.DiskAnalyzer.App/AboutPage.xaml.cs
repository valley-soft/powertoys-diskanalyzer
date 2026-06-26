using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ValleySoft_DiskAnalyzer_App;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        this.InitializeComponent();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.Frame.CanGoBack)
        {
            this.Frame.GoBack();
        }
    }
}
