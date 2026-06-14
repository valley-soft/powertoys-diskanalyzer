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
        try { System.IO.File.AppendAllText(@"C:\Users\thetn\cmdpal_log.txt", $"[Main] started with args: {string.Join(", ", args)}\n"); } catch {}
        var logPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "cmdpal_ext_trace.log");
        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            System.IO.File.AppendAllText(logPath, $"[CRASH] Unhandled: {e.ExceptionObject}\n");
        };
        TaskScheduler.UnobservedTaskException += (s, e) => {
            System.IO.File.AppendAllText(logPath, $"[CRASH] UnobservedTask: {e.Exception}\n");
        };

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            try { System.IO.File.AppendAllText(@"C:\Users\thetn\cmdpal_log.txt", "[Main] Registering COM Server...\n"); } catch {}
            global::Shmuelie.WinRTServer.ComServer server = new();

            ManualResetEvent extensionDisposedEvent = new(false);
            
            // We are instantiating an extension instance once above, and returning it every time the callback in RegisterExtension below is called.
            // This makes sure that only one instance of SampleExtension is alive, which is returned every time the host asks for the IExtension object.
            // If you want to instantiate a new instance each time the host asks, create the new instance inside the delegate.
            var crashPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "cmdpal_crash.log");
            try {
                System.IO.File.AppendAllText(logPath, "[Program] RegisterProcessAsComServer starting\n");
                DiskAnalyzerExtension extensionInstance = new(extensionDisposedEvent);
                server.RegisterClass<DiskAnalyzerExtension, IExtension>(() => extensionInstance);
                System.IO.File.AppendAllText(logPath, "[Program] Class registered\n");
                server.Start();
                System.IO.File.AppendAllText(logPath, "[Program] Server started, waiting...\n");
                try { System.IO.File.AppendAllText(@"C:\Users\thetn\cmdpal_log.txt", "[Main] COM Server started. Waiting...\n"); } catch {}
                extensionDisposedEvent.WaitOne();
                server.Stop();
                try { System.IO.File.AppendAllText(@"C:\Users\thetn\cmdpal_log.txt", "[Main] COM Server stopped.\n"); } catch {}
                System.IO.File.AppendAllText(logPath, "[Program] Disposed, exiting.\n");
                server.UnsafeDispose();
            } catch (Exception ex) {
                System.IO.File.WriteAllText(crashPath, ex.ToString());
            }
        }
        else
        {
            Console.WriteLine("Not being launched as a Extension... dumping reflection info.");
            try {
                using (var writer = new System.IO.StreamWriter("reflection_dump.txt")) {
                    var type = typeof(Microsoft.CommandPalette.Extensions.Toolkit.ListPage);
                    writer.WriteLine("ListPage methods:");
                    foreach (var m in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)) {
                        writer.WriteLine(m.ToString());
                    }
                    writer.WriteLine("\nListPage properties:");
                    foreach (var p in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) {
                        writer.WriteLine(p.ToString());
                    }
                    writer.WriteLine("Toolkit classes:");
                    var assembly = typeof(Microsoft.CommandPalette.Extensions.Toolkit.ListPage).Assembly;
                    foreach (var t in assembly.GetTypes()) {
                        if (t.Namespace != null && t.Namespace.Contains("Toolkit")) {
                            writer.WriteLine(t.Name);
                        }
                    }
                }
            } catch (Exception e) { Console.WriteLine(e); }
        }
    }
}
