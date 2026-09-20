using Avalonia;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace AvaWin.Theme.Tests;

public class PlatformTests
{
    [AvaloniaFact]
    public void Window_Is_Themed_With_Overlay_Layer()
    {
        var window = ThemeTestHelpers.Host(new Border(), "Dark", Platform.Desktop);
        Assert.NotNull(window.Background);
        Assert.Equal(Colors.Black.A, ((ISolidColorBrush)window.Background!).Color.A);
        Assert.NotNull(Avalonia.Controls.Primitives.OverlayLayer.GetOverlayLayer(window));
        var adorner = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(window.Content as Visual ?? window);
        Assert.NotNull(adorner);
        Assert.NotNull(adorner!.DefaultFocusAdorner);
        var button = new Button { Content = "x", Flyout = new Flyout { Content = new TextBlock { Text = "f" } } };
        window.Content = button;
        window.UpdateLayout();
        button.Flyout!.ShowAt(button);
        window.UpdateLayout();
        Assert.True(button.Flyout.IsOpen);
        button.Flyout.Hide();
    }

    [AvaloniaFact]
    public void Platform_Switch_Changes_Geometry_Resource()
    {
        var window = ThemeTestHelpers.Host(new Border(), "Light", Platform.Desktop);
        Assert.True(window.TryFindResource("WinButtonMinHeight", ThemeVariant.Light, out var desktop));
        Assert.Equal(32d, (double)desktop!);
        Assert.True(window.TryFindResource("WinIsPhone", ThemeVariant.Light, out var isPhone));
        Assert.False((bool)isPhone!);

        TestApplication.Instance.Theme.Platform = Platform.Phone;
        Assert.Equal(Platform.Phone, TestApplication.Instance.Theme.ActualPlatform);
        Assert.True(window.TryFindResource("WinButtonMinHeight", ThemeVariant.Light, out var phone));
        Assert.Equal(39d, (double)phone!);
        Assert.True(window.TryFindResource("WinIsPhone", ThemeVariant.Light, out isPhone));
        Assert.True((bool)isPhone!);
    }

    [AvaloniaFact]
    public void Platform_Switch_Repaints_Live_Control()
    {
        var text = new TextBlock { Text = "x" };
        text.Bind(Layoutable.MinHeightProperty, text.GetResourceObservable("WinButtonMinHeight"));
        var window = ThemeTestHelpers.Host(text, "Light", Platform.Desktop);
        Assert.Equal(32, text.MinHeight);
        TestApplication.Instance.Theme.Platform = Platform.Phone;
        window.UpdateLayout();
        Assert.Equal(39, text.MinHeight);
        TestApplication.Instance.Theme.Platform = Platform.Desktop;
        window.UpdateLayout();
        Assert.Equal(32, text.MinHeight);
    }

    [AvaloniaFact]
    public void Phone_Overlay_Is_Variant_Aware()
    {
        var window = ThemeTestHelpers.Host(new Border(), "Light", Platform.Phone);
        Assert.True(window.TryFindResource("WinPageBackgroundBrush", ThemeVariant.Light, out var light));
        Assert.Equal(Colors.White, ((ISolidColorBrush)light!).Color);
        Assert.True(window.TryFindResource("WinPageBackgroundBrush", ThemeVariant.Dark, out var dark));
        Assert.Equal(Colors.Black, ((ISolidColorBrush)dark!).Color);
    }

    [AvaloniaFact]
    public void Auto_Resolves_To_Desktop_Here()
    {
        TestApplication.Instance.Theme.Platform = Platform.Auto;
        Assert.Equal(Platform.Desktop, TestApplication.Instance.Theme.ActualPlatform);
    }

    [AvaloniaFact]
    public void Type_Ramp_Resolves_In_Both_Platforms()
    {
        var window = ThemeTestHelpers.Host(new Border(), "Light", Platform.Desktop);
        foreach (var name in new[] { "XXLarge", "XLarge", "Large", "Medium", "Small", "XSmall", "XXSmall", "Button", "Input", "Body" })
        {
            foreach (var platform in ThemeTestHelpers.Platforms)
            {
                TestApplication.Instance.Theme.Platform = platform;
                Assert.True(window.TryFindResource("WinFontSize" + name, ThemeVariant.Light, out var size) && size is double);
                Assert.True(window.TryFindResource("WinFontWeight" + name, ThemeVariant.Light, out var weight) && weight is FontWeight);
            }
        }
    }

    [AvaloniaFact]
    public void Fonts_Are_Embedded()
    {
        var window = ThemeTestHelpers.Host(new Border(), "Light", Platform.Desktop);
        Assert.True(window.TryFindResource("WinFontFamily", ThemeVariant.Light, out var f));
        var family = Assert.IsType<FontFamily>(f);
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family), out var gt));
        Assert.Equal("Selawik", gt.FamilyName);
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family, weight: FontWeight.Light), out var light));
        Assert.Equal(FontWeight.Light, light.Weight);
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family, weight: FontWeight.SemiBold), out var semibold));
        Assert.Equal(FontWeight.SemiBold, semibold.Weight);

        Assert.True(window.TryFindResource("WinSymbolFontFamily", ThemeVariant.Light, out var s));
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface((FontFamily)s!), out var symbols));
        Assert.Equal("Symbols", symbols.FamilyName);
        Assert.True(symbols.CharacterToGlyphMap.TryGetGlyph(0xE0D5, out _), "Symbols.ttf lacks the back glyph");
        foreach (var icon in AppBarIcons.All)
        {
            Assert.True(symbols.CharacterToGlyphMap.TryGetGlyph(AppBarIcons.Codepoint(icon), out _), $"Symbols.ttf lacks {icon}");
        }
    }
}
