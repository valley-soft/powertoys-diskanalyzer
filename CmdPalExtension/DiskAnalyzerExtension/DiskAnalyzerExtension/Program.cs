// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiskAnalyzerExtension;

public class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        var crashPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "ValleySoft.DiskAnalyzer", "cmdpal_crash.log");

        // Keep crash reporters — these only fire on hard crashes, not on every activation
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(crashPath)!);
                System.IO.File.WriteAllText(crashPath, $"[CRASH] Unhandled: {e.ExceptionObject}\n");
            }
            catch { }
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved(); // Prevent process termination from unobserved task exceptions
        };

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            global::Shmuelie.WinRTServer.ComServer server = new();
            ManualResetEvent extensionDisposedEvent = new(false);

            // Signal exit when all COM instances are released
            server.Empty += (s, e) => extensionDisposedEvent.Set();

            try
            {
                DiskAnalyzerExtension extensionInstance = new(extensionDisposedEvent);
                server.RegisterClass<DiskAnalyzerExtension, IExtension>(() => extensionInstance);
                server.Start();
                extensionDisposedEvent.WaitOne();
                server.Stop();
                server.UnsafeDispose();
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(crashPath)!);
                    System.IO.File.WriteAllText(crashPath, ex.ToString());
                }
                catch { }
            }
        }
        else
        {
            Console.WriteLine("Not being launched as a Extension... dumping reflection info.");
            try
            {
                using (var writer = new System.IO.StreamWriter("reflection_dump.txt"))
                {
                    var type = typeof(Microsoft.CommandPalette.Extensions.Toolkit.ListPage);
                    writer.WriteLine("ListPage methods:");
                    foreach (var m in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
                    {
                        writer.WriteLine(m.ToString());
                    }
                    writer.WriteLine("\nListPage properties:");
                    foreach (var p in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        writer.WriteLine(p.ToString());
                    }
                    writer.WriteLine("Toolkit classes:");
                    var assembly = typeof(Microsoft.CommandPalette.Extensions.Toolkit.ListPage).Assembly;
                    foreach (var t in assembly.GetTypes())
                    {
                        if (t.Namespace != null && t.Namespace.Contains("Toolkit"))
                        {
                            writer.WriteLine(t.Name);
                        }
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
        }
    }
}
