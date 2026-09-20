using Avalonia;

namespace AvaWin;

/// <summary>AppBuilder extensions for AvaWin.</summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Registers the <see cref="AvaWinFontCollection"/> (Selawik and Symbols), so the <c>fonts:AvaWin#Selawik</c>
    /// and <c>fonts:AvaWin#Symbols</c> families of the theme resolve. Without it, the theme uses Segoe UI and
    /// Segoe UI Symbol where they exist, and the system sans-serif elsewhere.
    /// </summary>
    public static AppBuilder WithAvaWinFonts(this AppBuilder builder) =>
        builder.ConfigureFonts(fontManager => fontManager.AddFontCollection(new AvaWinFontCollection()));
}
