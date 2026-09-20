using Avalonia;
using Avalonia.Controls.Primitives;

namespace AvaWin.Controls;

/// <summary>
/// Shows one <c>Symbols.ttf</c> glyph: an <see cref="AppBarIcon"/> or a raw <see cref="Glyph"/> string. It uses
/// <c>WinSymbolFontFamily</c>. <c>FontSize</c> and <c>Foreground</c> are inherited.
/// </summary>
public sealed class SymbolIcon : TemplatedControl
{
    /// <summary>Defines the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<AppBarIcon?> IconProperty =
        AvaloniaProperty.Register<SymbolIcon, AppBarIcon?>(nameof(Icon));

    /// <summary>Defines the <see cref="Glyph"/> property.</summary>
    public static readonly StyledProperty<string?> GlyphProperty =
        AvaloniaProperty.Register<SymbolIcon, string?>(nameof(Glyph));

    /// <summary>Defines the <see cref="ActualGlyph"/> property.</summary>
    public static readonly DirectProperty<SymbolIcon, string> ActualGlyphProperty =
        AvaloniaProperty.RegisterDirect<SymbolIcon, string>(nameof(ActualGlyph), o => o.ActualGlyph);

    private string _actualGlyph = string.Empty;

    /// <summary>The named icon to show. When set, it wins over <see cref="Glyph"/>.</summary>
    public AppBarIcon? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>A raw glyph string, for example <see cref="WinSymbol.Back"/>.</summary>
    public string? Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>The glyph that is shown.</summary>
    public string ActualGlyph
    {
        get => _actualGlyph;
        private set => SetAndRaise(ActualGlyphProperty, ref _actualGlyph, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty || change.Property == GlyphProperty)
        {
            ActualGlyph = Icon is { } icon ? AppBarIcons.Glyph(icon) : Glyph ?? string.Empty;
        }
    }
}
