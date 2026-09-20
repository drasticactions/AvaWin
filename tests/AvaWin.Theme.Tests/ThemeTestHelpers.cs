using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Styling;

namespace AvaWin.Theme.Tests;

public static partial class ThemeTestHelpers
{
    public static readonly string[] Variants = { "Light", "Dark" };
    public static readonly Platform[] Platforms = { Platform.Desktop, Platform.Phone };

    public static ThemeVariant ToVariant(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    /// <summary>Hosts <paramref name="control"/> in a shown window under the given variant and platform and lays it out.</summary>
    public static Window Host(Control control, string variant, Platform platform, double width = 600, double height = 400)
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

    /// <summary>All keys referenced with DynamicResource / StaticResource in a theme XAML file.</summary>
    public static IReadOnlyList<string> KeysReferencedBy(string xamlDirectory, string controlFileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, xamlDirectory, controlFileName + ".xaml");
        if (!File.Exists(path))
        {
            return Array.Empty<string>();
        }

        var text = File.ReadAllText(path);
        return ResourceRef().Matches(text)
            .Select(m => m.Groups["key"].Value)
            .Where(k => !k.StartsWith("{x:Type", StringComparison.Ordinal))
            .Distinct()
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();
    }

    [GeneratedRegex(@"\{(?:DynamicResource|StaticResource)\s+(?<key>[A-Za-z0-9_.]+)\s*\}")]
    private static partial Regex ResourceRef();
}
