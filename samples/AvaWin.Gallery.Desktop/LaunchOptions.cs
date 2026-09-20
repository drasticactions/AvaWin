using System;
using System.Collections.Generic;

namespace AvaWin.Gallery.Desktop;

/// <summary>
/// Command-line switches used by the visual-review gate: <c>--page Button --variant Dark --platform Phone
/// --disable-all --screenshot out.png</c>. With <c>--screenshot</c> the app renders the page to a PNG and exits.
/// <c>--window-state Maximized,Normal</c> walks the window through those states first, one delay apart, the way the
/// caption buttons do.
/// </summary>
public sealed class LaunchOptions
{
    public static LaunchOptions Current { get; private set; } = new();

    public string? Page { get; init; }
    public string Variant { get; init; } = "System";
    public string Platform { get; init; } = "Auto";
    public bool DisableAll { get; init; }
    public string? Screenshot { get; init; }
    public double Width { get; init; } = 1200;
    public double Height { get; init; } = 800;
    public int DelayMs { get; init; } = 700;
    public double Scroll { get; init; }
    public string? Invoke { get; init; }
    public int? InvokeDelayMs { get; init; }
    public string? WindowStates { get; init; }

    public static void Parse(IReadOnlyList<string> args)
    {
        string? page = null, variant = "System", platform = "Auto", shot = null;
        var disable = false;
        double w = 1200, h = 800;
        var delay = 700;
        var scroll = 0d;
        string? invoke = null;
        int? invokeDelay = null;
        string? states = null;
        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--page" when i + 1 < args.Count: page = args[++i]; break;
                case "--variant" when i + 1 < args.Count: variant = args[++i]; break;
                case "--platform" when i + 1 < args.Count: platform = args[++i]; break;
                case "--screenshot" when i + 1 < args.Count: shot = args[++i]; break;
                case "--size" when i + 1 < args.Count:
                    var parts = args[++i].Split('x');
                    w = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                    h = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--disable-all": disable = true; break;
                case "--invoke" when i + 1 < args.Count: invoke = args[++i]; break;
                case "--invoke-delay" when i + 1 < args.Count: invokeDelay = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture); break;
                case "--scroll" when i + 1 < args.Count: scroll = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture); break;
                case "--delay" when i + 1 < args.Count: delay = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture); break;
                case "--window-state" when i + 1 < args.Count: states = args[++i]; break;
            }
        }

        Current = new LaunchOptions { Page = page, Variant = variant!, Platform = platform!, Screenshot = shot, DisableAll = disable, Width = w, Height = h, DelayMs = delay, Scroll = scroll, Invoke = invoke, InvokeDelayMs = invokeDelay, WindowStates = states };
    }
}
