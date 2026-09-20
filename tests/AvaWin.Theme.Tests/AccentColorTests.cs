using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace AvaWin.Theme.Tests;

public class AccentColorTests
{
    [Fact]
    public void Default_Accent_Produces_WinJS_Ramp()
    {
        var ramp = AccentColors.Compute(AccentColors.WinJsAccent);
        Assert.Equal(Color.Parse("#FF4617B4"), ramp.Accent);
        Assert.Equal(Color.Parse("#FF5F37BE"), ramp.Light1);
        Assert.Equal(Color.Parse("#FF7241E4"), ramp.Light2);
        Assert.Equal(Color.Parse("#FF8152EF"), ramp.Light3);
        Assert.Equal(Color.Parse("#FF3E1499"), ramp.Dark1);
        Assert.Equal(Color.Parse("#FF35117F"), ramp.Dark2);
        Assert.Equal(Color.Parse("#FF2B0E66"), ramp.Dark3);
        Assert.Equal(Color.Parse("#FF5B2EC5"), ramp.OnDark);
        Assert.Equal(Color.Parse("#FF724BCD"), ramp.OnDarkHover);
        Assert.Equal(Color.Parse("#FF7E4FEC"), ramp.OnDarkPressed);
        Assert.Equal(Color.Parse("#FF8A57FF"), ramp.OnDarkVivid);
        Assert.Equal(Color.Parse("#FF4F1ACB"), ramp.Link);
        Assert.Equal(Color.Parse("#FF9C72FF"), ramp.LinkOnDark);
        Assert.Equal(Color.Parse("#FF5729C1"), ramp.TextSelection);
    }

    [Fact]
    public void Fluent_Blue_Matches_Fluent_Ramp()
    {
        // The documented shades of #0078D7.
        var ramp = AccentColors.Compute(Color.Parse("#FF0078D7"));
        var (d1, d2, d3, l1, l2, l3) = AccentColors.CalculateAccentShades(Color.Parse("#FF0078D7"));
        Assert.Equal(d1, ramp.Dark1);
        Assert.Equal(l3, ramp.Light3);
        // Sanity on the HSL algorithm itself: lighter shades are lighter, darker are darker.
        Assert.True(l1.ToHsl().L > ramp.Accent.ToHsl().L);
        Assert.True(d1.ToHsl().L < ramp.Accent.ToHsl().L);
        Assert.True(l2.ToHsl().L > l1.ToHsl().L && l3.ToHsl().L > l2.ToHsl().L);
        Assert.True(d2.ToHsl().L < d1.ToHsl().L && d3.ToHsl().L < d2.ToHsl().L);
        Assert.Equal(ramp.Accent, ramp.Link);
        Assert.Equal(ramp.OnDarkVivid, ramp.LinkOnDark);
    }

    [AvaloniaFact]
    public void AccentColor_Override_Changes_SystemAccentColor_And_Ramp()
    {
        var window = ThemeTestHelpers.Host(new Border(), "Light", Platform.Desktop);
        Assert.True(window.TryFindResource("SystemAccentColor", ThemeVariant.Light, out var before));
        Assert.Equal(TestApplication.DefaultAccent, (Color)before!);

        TestApplication.Instance.Theme.AccentColor = Color.Parse("#FF0078D7");
        Assert.True(window.TryFindResource("SystemAccentColor", ThemeVariant.Light, out var after));
        Assert.Equal(Color.Parse("#FF0078D7"), (Color)after!);
        Assert.True(window.TryFindResource("SystemAccentColorLight1", ThemeVariant.Light, out var l1));
        Assert.Equal(AccentColors.CalculateAccentShades(Color.Parse("#FF0078D7")).l1, (Color)l1!);

        TestApplication.Instance.Theme.AccentColor = null;
        Assert.True(window.TryFindResource("SystemAccentColor", ThemeVariant.Light, out var reset));
        Assert.Equal(TestApplication.DefaultAccent, (Color)reset!);
    }
}
