// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DiskAnalyzerExtension;

public partial class DiskAnalyzerExtensionCommandsProvider : CommandProvider
{
    private static IconInfo SafeIcon(string relativePath, string fallbackGlyph = "\ue71b")
    {
        try
        {
            var icon = IconHelpers.FromRelativePath(relativePath);
            return icon ?? new IconInfo(fallbackGlyph);
        }
        catch
        {
            return new IconInfo(fallbackGlyph);
        }
    }

    public DiskAnalyzerExtensionCommandsProvider()
    {
        DisplayName = "ValleySoft Disk Analyzer (Command Palette)";
        Icon        = SafeIcon("Assets\\DiskAnalyzerLight.png");
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return new ICommandItem[]
        {
            new ListItem(new DiskAnalyzerExtensionPage())
            {
                Title    = "ValleySoft Disk Analyzer (Command Palette)",
                Subtitle = "Interactive in-palette disk space usage analyzer",
                Icon     = Icon,
            },
            new ListItem(new MyAnonymousCommand(() => 
            {
                try
                {
                    string aliasPath = System.IO.Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                        "Microsoft", "WindowsApps", "ValleySoft.DiskAnalyzer.exe");

                    bool aliasExists = false;
                    try
                    {
                        var attr = System.IO.File.GetAttributes(aliasPath);
                        if (attr != (System.IO.FileAttributes)(-1))
                        {
                            aliasExists = true;
                        }
                    }
                    catch { }

                    string exePath = aliasExists ? aliasPath : "ValleySoft.DiskAnalyzer.exe";

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }
                catch { }
            }))
            {
                Title    = "ValleySoft Disk Analyzer (Standalone App)",
                Subtitle = "Launch standalone graphical WinUI 3 window",
                Icon     = Icon,
            },
            new ListItem(new MyAnonymousCommand(() => 
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powertoys://run",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    try
                    {
                        string aliasPath = System.IO.Path.Combine(
                            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                            "Microsoft", "WindowsApps", "ValleySoft.DiskAnalyzer.exe");

                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = aliasPath,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            }))
            {
                Title    = "ValleySoft Disk Analyzer (PowerToys Run)",
                Subtitle = "Open PowerToys Run plugin launcher (ds <path>)",
                Icon     = Icon,
            }
        };
    }
}
