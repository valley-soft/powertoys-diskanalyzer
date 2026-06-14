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
            
            // Command Palette Directory
            string cmdPalDir = Path.Combine(localAppData, "Microsoft", "PowerToys", "CmdPal", "Plugins", "DiskAnalyzer");

            Console.WriteLine("Target Installation Directories:");
            Console.WriteLine("1. " + ptRunDir);
            Console.WriteLine("2. " + cmdPalDir + "\n");

            Console.WriteLine("Extracting files...");

            try
            {
                // Clean old directories
                if (Directory.Exists(ptRunDir)) Directory.Delete(ptRunDir, true);
                if (Directory.Exists(cmdPalDir)) Directory.Delete(cmdPalDir, true);
                
                Directory.CreateDirectory(ptRunDir);
                Directory.CreateDirectory(cmdPalDir);

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
                    
                    // Reset stream position to extract again
                    stream.Position = 0;
                    using (ZipArchive archive = new ZipArchive(stream))
                    {
                        // Extract to Command Palette
                        archive.ExtractToDirectory(cmdPalDir);
                    }
                }

                Console.WriteLine("\nSUCCESS: DiskAnalyzer installed perfectly!");
                Console.WriteLine("Please fully restart PowerToys for the plugin to be loaded.");
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
