using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CheckPrerequisites();
        }

        private void CheckPrerequisites()
        {
            bool hasPowerToys = IsPowerToysInstalled();
            if (!hasPowerToys)
            {
                CheckPlugin.IsChecked = false;
                CheckPlugin.IsEnabled = false;
                TxtPowerToysMissing.Visibility = Visibility.Visible;
                BtnInstallPowerToys.Visibility = Visibility.Visible;
            }
            else
            {
                CheckPlugin.IsEnabled = true;
                TxtPowerToysMissing.Visibility = Visibility.Collapsed;
                BtnInstallPowerToys.Visibility = Visibility.Collapsed;
            }
        }

        private bool IsPowerToysInstalled()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            
            // Check common install paths
            if (System.IO.File.Exists(System.IO.Path.Combine(localAppData, "PowerToys", "PowerToys.exe"))) return true;
            if (System.IO.File.Exists(System.IO.Path.Combine(programFiles, "PowerToys", "PowerToys.exe"))) return true;
            
            // Checking the Microsoft/PowerToys folder in LocalAppData is also a good indicator
            if (System.IO.Directory.Exists(System.IO.Path.Combine(localAppData, "Microsoft", "PowerToys"))) return true;

            return false;
        }

        private async void BtnInstallPowerToys_Click(object sender, RoutedEventArgs e)
        {
            BtnInstallPowerToys.IsEnabled = false;
            BtnInstallPowerToys.Content = "Installing...";
            LogMessage("Starting PowerToys installation via winget...");

            await Task.Run(() =>
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install Microsoft.PowerToys --accept-source-agreements --accept-package-agreements",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                try
                {
                    using (System.Diagnostics.Process? proc = System.Diagnostics.Process.Start(psi))
                    {
                        if (proc != null)
                        {
                            proc.WaitForExit();
                            if (proc.ExitCode == 0)
                            {
                                LogMessage("Success: PowerToys installed successfully.");
                                Dispatcher.Invoke(CheckPrerequisites);
                            }
                            else
                            {
                                LogMessage("Failed to install PowerToys. Try manually installing from Microsoft Store.");
                            }
                        }
                        else
                        {
                            LogMessage("Failed to start winget process.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Error invoking winget: " + ex.Message);
                }
            });
            
            BtnInstallPowerToys.IsEnabled = true;
            BtnInstallPowerToys.Content = "Install PowerToys";
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            bool isCleanInstall = RadioClean.IsChecked == true;
            bool installPlugin = CheckPlugin.IsChecked == true;
            bool installCmdPal = CheckCmdPal.IsChecked == true;
            bool installApp = CheckApp.IsChecked == true;

            if (!installPlugin && !installCmdPal && !installApp)
            {
                MessageBox.Show("Please select at least one component to install.", "ValleySoft Disk Analyzer Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Lock UI
            BtnInstall.IsEnabled = false;
            CheckPlugin.IsEnabled = false;
            CheckCmdPal.IsEnabled = false;
            CheckApp.IsEnabled = false;
            RadioExpress.IsEnabled = false;
            RadioClean.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            LogText.Text = "";

            await Task.Run(() =>
            {
                InstallManager.PerformInstall(isCleanInstall, installPlugin, installCmdPal, installApp, LogMessage);
            });

            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;
            BtnInstall.Content = "Finish";
            BtnInstall.Click -= BtnInstall_Click;
            BtnInstall.Click += (s, ev) => { Application.Current.Shutdown(); };
            BtnInstall.IsEnabled = true;
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogText.Text += message + "\n";
                LogScroll.ScrollToEnd();
            });
        }
    }
}
