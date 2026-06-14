using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=============================================");
            Console.WriteLine(" DiskAnalyzer v1.2.0 Installer for PowerToys ");
            Console.WriteLine("=============================================\n");

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            // PowerToys Run Directory
            string ptRunDir = Path.Combine(localAppData, "Microsoft", "PowerToys", "PowerToys Run", "Plugins", "DiskAnalyzer");
            Console.WriteLine("Target Installation Directory:");
            Console.WriteLine("1. " + ptRunDir + "\n");

            Console.WriteLine("Extracting files...");

            try
            {
                // Clean old directories
                if (Directory.Exists(ptRunDir)) Directory.Delete(ptRunDir, true);
                
                Directory.CreateDirectory(ptRunDir);

                // Read embedded zip file
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "Community.PowerToys.Run.Plugin.DiskAnalyzer.Installer.payload.zip";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Console.WriteLine("ERROR: Payload zip not found inside the installer!");
                        WaitForExit();
                        return;
                    }

                    using (ZipArchive archive = new ZipArchive(stream))
                    {
                        // Extract to PowerToys Run
                        archive.ExtractToDirectory(ptRunDir);
                    }
                }

                Console.WriteLine("\nSUCCESS: DiskAnalyzer installed perfectly!");
                Console.WriteLine("Please fully restart PowerToys for the plugin to be loaded.");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("\nERROR: Installation failed because the files are in use.");
                Console.WriteLine("PowerToys is currently running and locking the plugin files!");
                Console.WriteLine("-> Please right-click the PowerToys icon in your system tray (bottom right), click 'Exit', and run this installer again.");
                Console.WriteLine("\nTechnical Details: " + ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine("\nERROR: Installation failed because the files are in use.");
                Console.WriteLine("PowerToys is currently running and locking the plugin files!");
                Console.WriteLine("-> Please right-click the PowerToys icon in your system tray (bottom right), click 'Exit', and run this installer again.");
                Console.WriteLine("\nTechnical Details: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR: Installation failed.");
                Console.WriteLine(ex.Message);
            }

            WaitForExit();
        }

        static void WaitForExit()
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
