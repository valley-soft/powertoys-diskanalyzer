using System;
using System.Windows;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool isSilent = false;
            bool isCleanInstall = false;
            bool installPlugin = false;
            bool installCmdPal = false;
            bool installApp = false;

            if (e.Args.Length > 0)
            {
                foreach (var arg in e.Args)
                {
                    string a = arg.ToLowerInvariant();
                    if (a == "--silent" || a == "-s") isSilent = true;
                    if (a == "--clean") isCleanInstall = true;
                    if (a == "--express") isCleanInstall = false;
                    if (a == "--plugin") installPlugin = true;
                    if (a == "--cmdpal") installCmdPal = true;
                    if (a == "--standalone") installApp = true;
                    if (a == "--all") { installPlugin = true; installCmdPal = true; installApp = true; }
                }

                if (isSilent)
                {
                    InstallManager.PerformInstall(isCleanInstall, installPlugin, installCmdPal, installApp, (msg) => { Console.WriteLine(msg); });
                    Shutdown();
                    return;
                }
            }

            // Normal GUI Mode
            MainWindow wnd = new MainWindow();
            wnd.Show();
        }
    }
}
