// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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

        InitializeSettings();
    }

    private void InitializeSettings()
    {
        try
        {
            var localValues = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            bool initHidden = (localValues["CmdPal_ShowHiddenFiles"] as bool?) ?? true;
            int initDepth = (localValues["CmdPal_MaxScanDepth"] as int?) ?? 0;

            var showHidden = new ToggleSetting(
                "CmdPal_ShowHiddenFiles",
                "Show Hidden Files",
                "Include hidden files and folders in disk scans",
                initHidden);

            var depthChoices = new List<ChoiceSetSetting.Choice>
            {
                new("Unlimited", "0"),
                new("1 Level", "1"),
                new("2 Levels", "2"),
                new("3 Levels", "3"),
                new("5 Levels", "5")
            };

            var maxDepth = new ChoiceSetSetting(
                "CmdPal_MaxScanDepth",
                "Max Scan Depth",
                "Maximum directory depth to scan (0 for unlimited)",
                depthChoices)
            {
                Value = initDepth.ToString()
            };

            var extSettings = new Settings();
            extSettings.Add(showHidden);
            extSettings.Add(maxDepth);

            extSettings.SettingsChanged += (sender, args) =>
            {
                try
                {
                    var vals = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
                    vals["CmdPal_ShowHiddenFiles"] = showHidden.Value;
                    if (int.TryParse(maxDepth.Value, out int d))
                    {
                        vals["CmdPal_MaxScanDepth"] = d;
                    }
                }
                catch { }
            };

            Settings = extSettings;
        }
        catch { }
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
            }
        };
    }
}


