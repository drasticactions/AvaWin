using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using AvaWin;
using AvaWin.Gallery;

internal static class Program
{
    private static Task Main(string[] args) =>
        BuildAvaloniaApp().WithAvaWinFonts().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
