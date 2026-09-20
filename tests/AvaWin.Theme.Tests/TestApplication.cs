using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using AvaWin.Animations;

[assembly: AvaloniaTestApplication(typeof(AvaWin.Theme.Tests.TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace AvaWin.Theme.Tests;

public class TestApplication : Application
{
    public TestApplication()
    {
        WinAnimations.TimeScale = 0;
        Theme = new AvaWinTheme();
        Styles.Add(Theme);
    }

    public AvaWinTheme Theme { get; }

    public static TestApplication Instance => (TestApplication)Current!;

    /// <summary>The accent the theme falls back to: the headless platform accent, else the default purple.</summary>
    public static Avalonia.Media.Color DefaultAccent =>
        Instance.PlatformSettings?.GetColorValues().AccentColor1 ?? AccentColors.WinJsAccent;

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .WithAvaWinFonts()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
