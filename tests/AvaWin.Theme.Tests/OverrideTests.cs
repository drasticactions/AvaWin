using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace AvaWin.Theme.Tests;

public class OverrideTests
{
    [AvaloniaFact]
    public void Application_Resource_Override_Wins()
    {
        Application.Current!.Resources["WinButtonBackgroundBrush"] = new SolidColorBrush(Colors.Red);
        var window = ThemeTestHelpers.Host(new Border(), "Light", Platform.Desktop);
        Assert.True(window.TryFindResource("WinButtonBackgroundBrush", ThemeVariant.Light, out var v));
        Assert.Equal(Colors.Red, ((ISolidColorBrush)v!).Color);
    }

    [AvaloniaFact]
    public void Palettes_Dark_Accent_Reaches_Accent_Brush()
    {
        TestApplication.Instance.Theme.Palettes[ThemeVariant.Dark] = new ColorPaletteResources { Accent = Colors.Orange };
        var window = ThemeTestHelpers.Host(new Border(), "Dark", Platform.Desktop);
        Assert.True(window.TryFindResource("SystemAccentColor", ThemeVariant.Dark, out var c));
        Assert.Equal(Colors.Orange, (Color)c!);
        Assert.True(window.TryFindResource("WinAccentButtonBackgroundBrush", ThemeVariant.Dark, out var b));
        Assert.Equal(Colors.Orange, ((ISolidColorBrush)b!).Color);
        // Light is untouched.
        Assert.True(window.TryFindResource("SystemAccentColor", ThemeVariant.Light, out var l));
        Assert.Equal(TestApplication.DefaultAccent, (Color)l!);
    }

    [AvaloniaFact]
    public void Palettes_BaseHigh_Reaches_Text_Brush()
    {
        TestApplication.Instance.Theme.Palettes[ThemeVariant.Light] = new ColorPaletteResources { BaseHigh = Colors.Navy };
        var window = ThemeTestHelpers.Host(new Border(), "Light", Platform.Desktop);
        Assert.True(window.TryFindResource("WinTextBrush", ThemeVariant.Light, out var b));
        Assert.Equal(Colors.Navy, ((ISolidColorBrush)b!).Color);
    }

    [AvaloniaFact]
    public void Every_Layer2_Brush_Resolves_In_All_Combinations()
    {
        var window = ThemeTestHelpers.Host(new Border(), "Light", Platform.Desktop);
        foreach (var platform in ThemeTestHelpers.Platforms)
        {
            TestApplication.Instance.Theme.Platform = platform;
            foreach (var variant in ThemeTestHelpers.Variants)
            {
                foreach (var key in Layer2Keys.All)
                {
                    Assert.True(window.TryFindResource(key, ThemeTestHelpers.ToVariant(variant), out var v) && v is IBrush,
                        $"{key} did not resolve to a brush under {variant}/{platform}");
                }
            }
        }
    }
}
