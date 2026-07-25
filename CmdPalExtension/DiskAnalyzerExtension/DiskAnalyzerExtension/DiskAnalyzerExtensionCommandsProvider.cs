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
                Title    = DisplayName,
                Subtitle = "Analyze disk space usage",
                Icon     = Icon,
            },
            new ListItem(new MyAnonymousCommand(() => 
            {
                try
                {
                    var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
                    var exePath = System.IO.Path.Combine(baseDir, "ValleySoft.DiskAnalyzer.exe");
                    var targetFile = System.IO.File.Exists(exePath) ? exePath : "ValleySoft.DiskAnalyzer.exe";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = targetFile,
                        UseShellExecute = true
                    });
                }
                catch { }
            }))
            {
                Title = "ValleySoft Disk Analyzer (Standalone App)",
                Subtitle = "Launch standalone graphical window",
                Icon = SafeIcon("Assets\\DiskAnalyzerLight.png")
            }
        };
    }
}
