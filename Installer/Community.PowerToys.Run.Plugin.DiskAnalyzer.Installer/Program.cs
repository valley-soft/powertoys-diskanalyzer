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
            string ptPluginsDir = Path.Combine(localAppData, "Microsoft", "PowerToys", "PowerToys Run", "Plugins");
            string targetDir = Path.Combine(ptPluginsDir, "DiskAnalyzer");

            Console.WriteLine($"Target Installation Directory:");
            Console.WriteLine(targetDir + "\n");

            if (!Directory.Exists(ptPluginsDir))
            {
                Console.WriteLine("ERROR: PowerToys Run plugins directory not found!");
                Console.WriteLine("Are you sure PowerToys is installed?");
                WaitForExit();
                return;
            }

            Console.WriteLine("Extracting files...");

            try
            {
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }
                Directory.CreateDirectory(targetDir);

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
                        archive.ExtractToDirectory(targetDir);
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
