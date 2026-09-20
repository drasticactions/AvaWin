using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Styling;

namespace AvaWin.Controls.Tests;

public static partial class ThemeTestHelpers
{
    public static readonly string[] Variants = { "Light", "Dark" };
    public static readonly Platform[] Platforms = { Platform.Desktop, Platform.Phone };

    public static ThemeVariant ToVariant(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    public static Window Host(Control control, string variant, Platform platform, double width = 800, double height = 600)
    {
        TestApplication.Instance.Theme.Platform = platform;
        var window = new Window
        {
            RequestedThemeVariant = ToVariant(variant),
            Content = control,
            Width = width,
            Height = height,
        };
        window.Show();
        window.UpdateLayout();
        return window;
    }

    public static IReadOnlyList<string> KeysReferencedBy(string controlFileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ThemeXaml", controlFileName + ".xaml");
        if (!File.Exists(path))
        {
            return Array.Empty<string>();
        }

        var text = File.ReadAllText(path);
        return ResourceRef().Matches(text)
            .Select(m => m.Groups["key"].Value)
            .Distinct()
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();
    }

    [GeneratedRegex(@"\{(?:DynamicResource|StaticResource)\s+(?<key>[A-Za-z0-9_.]+)\s*\}")]
    private static partial Regex ResourceRef();
}
