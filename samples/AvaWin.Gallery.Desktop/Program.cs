using System;
using Avalonia;

namespace AvaWin.Gallery.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        LaunchOptions.Parse(args);
        App.DesktopWindowFactory = () => new MainWindow();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseWaylandWithFallback()
            .UseSkia()
            .WithAvaWinFonts()
            .LogToTrace();
}
