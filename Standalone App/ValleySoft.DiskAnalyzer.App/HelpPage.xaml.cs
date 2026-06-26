using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ValleySoft_DiskAnalyzer_App;

public sealed partial class HelpPage : Page
{
    public HelpPage()
    {
        this.InitializeComponent();
        HelpNavView.SelectedItem = HelpNavView.MenuItems[0];
    }

    private void HelpNavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (this.Frame.CanGoBack)
        {
            this.Frame.GoBack();
        }
    }

    private void HelpNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var navItem = args.InvokedItemContainer as NavigationViewItem;
        if (navItem == null) return;

        ScanningSection.Visibility = Visibility.Collapsed;
        FilteringSection.Visibility = Visibility.Collapsed;
        NavigationSection.Visibility = Visibility.Collapsed;
        ViewOptionsSection.Visibility = Visibility.Collapsed;

        string tag = navItem.Tag?.ToString();
        HelpNavView.Header = navItem.Content;

        switch (tag)
        {
            case "Scanning":
                ScanningSection.Visibility = Visibility.Visible;
                break;
            case "Filtering":
                FilteringSection.Visibility = Visibility.Visible;
                break;
            case "Navigation":
                NavigationSection.Visibility = Visibility.Visible;
                break;
            case "ViewOptions":
                ViewOptionsSection.Visibility = Visibility.Visible;
                break;
        }
    }
}
