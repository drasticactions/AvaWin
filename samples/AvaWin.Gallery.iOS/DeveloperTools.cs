using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;

namespace AvaWin.Gallery.iOS;

/// <summary>Debug-build hooks for the Avalonia developer tools; no-ops in release builds.</summary>
internal static class DeveloperTools
{
    public static AppBuilder Attach(AppBuilder builder)
    {
#if AVAWIN_DEVTOOLS
        return builder
            .WithDeveloperTools(o => o.ConnectOnStartup = false)
            .AfterSetup(_ => ConnectAsync());
#else
        return builder;
#endif
    }

#if AVAWIN_DEVTOOLS
    /// <summary>
    /// Sets the diagnostics library's private process id override to the real pid (or <c>AVALONIA_DEVTOOLS_PROCESS_ID</c>),
    /// then connects to the tool at <c>AVALONIA_DEVTOOLS_HTTP_PORT</c> (default 29414). See <c>tools/ios-sim-devtools.py</c>.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "Debug-only reflection into the diagnostics library, which is not trimmed in Debug.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Debug-only reflection into the diagnostics library, which is not trimmed in Debug.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Debug-only reflection into the diagnostics library, which is not trimmed in Debug.")]
    private static async void ConnectAsync()
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var diagnostics = typeof(AvaloniaUI.DiagnosticsSupport.DeveloperToolsOptions).Assembly;
        var managerType = diagnostics.GetType("AvaloniaUI.DiagnosticsSupport.Protocol.DiagnosticsConnectionManager");
        var handlerType = diagnostics.GetType("AvaloniaUI.DiagnosticsSupport.Services.TargetHandler");
        if (managerType is null || handlerType is null)
        {
            Console.WriteLine("[devtools] diagnostics types not found");
            return;
        }

        var pid = int.TryParse(Environment.GetEnvironmentVariable("AVALONIA_DEVTOOLS_PROCESS_ID"), out var configured) ? configured : Environment.ProcessId;
        var instanceField = managerType.GetField("<Instance>k__BackingField", all);
        for (var attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                var manager = instanceField?.GetValue(null);
                if (manager is not null && TrySetOverride(manager, handlerType, all, pid, 0))
                {
                    var port = int.TryParse(Environment.GetEnvironmentVariable("AVALONIA_DEVTOOLS_HTTP_PORT"), out var p) ? p : 29414;
                    Console.WriteLine($"[devtools] reporting process id {pid}, connecting to port {port}");
                    var protocol = AvaloniaUI.DiagnosticsSupport.DeveloperToolsProtocol.CreateHttp(new Uri($"http://127.0.0.1:{port}"));
                    var activate = managerType.GetMethod("ActivateAsync", all)!;
                    await (Task)activate.Invoke(manager, [protocol, AvaloniaUI.DiagnosticsSupport.DeveloperToolsRunner.NoOp, System.Threading.CancellationToken.None])!;
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[devtools] process id override failed: {ex.Message}");
                return;
            }

            await Task.Delay(200);
        }

        Console.WriteLine("[devtools] could not find the target handler to set the process id");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Debug-only reflection into the diagnostics library, which is not trimmed in Debug.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Debug-only reflection into the diagnostics library, which is not trimmed in Debug.")]
    private static bool TrySetOverride(object root, Type handlerType, BindingFlags all, int pid, int depth)
    {
        if (depth > 6)
        {
            return false;
        }

        if (handlerType.IsInstanceOfType(root))
        {
            foreach (var field in handlerType.GetFields(all))
            {
                if (field.Name.Contains("processIdOverride", StringComparison.Ordinal))
                {
                    field.SetValue(root, (int?)pid);
                    return true;
                }
            }

            return false;
        }

        if (root is IDictionary dictionary)
        {
            foreach (var value in dictionary.Values)
            {
                if (value is not null && TrySetOverride(value, handlerType, all, pid, depth + 1))
                {
                    return true;
                }
            }

            return false;
        }

        for (var type = root.GetType(); type is not null && type != typeof(object); type = type.BaseType)
        {
            foreach (var field in type.GetFields(all & ~BindingFlags.Static))
            {
                if (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType.IsEnum)
                {
                    continue;
                }

                var value = field.GetValue(root);
                if (value is null || value is Delegate || value is Type)
                {
                    continue;
                }

                if (value.GetType().Assembly != handlerType.Assembly && value is not IDictionary)
                {
                    continue;
                }

                if (TrySetOverride(value, handlerType, all, pid, depth + 1))
                {
                    return true;
                }
            }
        }

        return false;
    }
#endif
}
