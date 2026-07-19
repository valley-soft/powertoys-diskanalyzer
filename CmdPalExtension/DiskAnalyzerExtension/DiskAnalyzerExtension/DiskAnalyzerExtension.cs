// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace DiskAnalyzerExtension;

[Guid("14c8d1ee-3935-45ee-89e6-76f654e6295b")]
public sealed partial class DiskAnalyzerExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly DiskAnalyzerExtensionCommandsProvider _provider = new();

    public DiskAnalyzerExtension(ManualResetEvent extensionDisposedEvent)
    {
        var logPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "cmdpal_ext_trace.log");
        System.IO.File.AppendAllText(logPath, "[DiskAnalyzerExtension] Constructor called\n");
        this._extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        var logPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "cmdpal_ext_trace.log");
        System.IO.File.AppendAllText(logPath, $"[DiskAnalyzerExtension] GetProvider called for {providerType}\n");
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    public void Dispose()
    {
        // Signal the main thread to exit the COM server gracefully
        _extensionDisposedEvent.Set();
    }
}
