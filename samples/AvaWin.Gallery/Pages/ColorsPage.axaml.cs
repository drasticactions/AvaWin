using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaWin.Gallery.Framework;

namespace AvaWin.Gallery.Pages;

/// <summary>One resource key and its live value, resolved through the page so variant, accent and platform switches repaint.</summary>
public sealed class TokenSwatch(string key, Control scope) : AvaloniaObject
{
    public string Key { get; } = key;

    public IBrush? Brush => scope.TryFindResource(Key, scope.ActualThemeVariant, out var v) switch
    {
        true when v is IBrush b => b,
        true when v is Color c => new SolidColorBrush(c),
        _ => null,
    };
}

public partial class ColorsPage : SamplePage
{
    public ColorsPage()
    {
        InitializeComponent();
        Fill();
        App.Theme.PropertyChanged += (_, _) => Fill();
        ActualThemeVariantChanged += (_, _) => Fill();
    }

    private void Fill()
    {
        AccentList.ItemsSource = Swatches(TokenKeys.All.Where(k => k.Contains("Accent") || k.StartsWith("WinLink") || k == "WinTextSelectionColor"));
        BaseList.ItemsSource = Swatches(TokenKeys.All.Where(k => k.StartsWith("SystemBase") || k.StartsWith("SystemAlt") || k.StartsWith("SystemChrome") || k.StartsWith("SystemList")));
        SemanticList.ItemsSource = Swatches(TokenKeys.All.Where(k => k.StartsWith("Win") && k.EndsWith("Brush")));
    }

    private List<TokenSwatch> Swatches(IEnumerable<string> keys) => keys.Select(k => new TokenSwatch(k, this)).ToList();
}
