using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ValleySoft_DiskAnalyzer_App;

public sealed partial class HelpPage : Page
{
    public HelpPage()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) =>
        {
            try
            {
                if (HelpNavView != null && HelpNavView.MenuItems.Count > 0)
                {
                    HelpNavView.SelectedItem = HelpNavView.MenuItems[0];
                }
            }
            catch { }
        };
    }

    private void HelpNavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        try
        {
            if (this.Frame != null && this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }
        catch { }
    }

    private void HelpNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            var navItem = args.InvokedItemContainer as NavigationViewItem;
            if (navItem == null) return;

            WhatsNewSection.Visibility = Visibility.Collapsed;
            ScanningSection.Visibility = Visibility.Collapsed;
            FilteringSection.Visibility = Visibility.Collapsed;
            NavigationSection.Visibility = Visibility.Collapsed;
            ViewOptionsSection.Visibility = Visibility.Collapsed;

            string? tag = navItem.Tag?.ToString();
            HelpNavView.Header = navItem.Content;

            switch (tag)
            {
                case "WhatsNew":
                    WhatsNewSection.Visibility = Visibility.Visible;
                    break;
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HelpPage ItemInvoked error: {ex.Message}");
        }
    }
}
