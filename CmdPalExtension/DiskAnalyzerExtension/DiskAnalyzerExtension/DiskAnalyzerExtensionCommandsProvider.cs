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
        DisplayName = "Disk Analyzer Command Pallete Version";
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
            }
        };
    }
}
