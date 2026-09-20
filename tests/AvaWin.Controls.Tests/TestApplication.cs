using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using AvaWin.Animations;
using AvaWin.Controls;

[assembly: AvaloniaTestApplication(typeof(AvaWin.Controls.Tests.TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace AvaWin.Controls.Tests;

public class TestApplication : Application
{
    public TestApplication()
    {
        WinAnimations.TimeScale = 0;
        Theme = new AvaWinTheme();
        Styles.Add(Theme);
        Styles.Add(new AvaWinControlsTheme());
    }

    public AvaWinTheme Theme { get; }

    public static TestApplication Instance => (TestApplication)Current!;

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .WithAvaWinFonts()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
