using System;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;

namespace AvaWin;

/// <summary>
/// The embedded font collection (<c>fonts:AvaWin</c>) with Selawik, which is metric-compatible with Segoe UI, and
/// <c>Symbols.ttf</c>. Register it with <see cref="AppBuilderExtensions.WithAvaWinFonts"/>.
/// </summary>
public sealed class AvaWinFontCollection : EmbeddedFontCollection
{
    /// <summary>The collection key, <c>fonts:AvaWin</c>.</summary>
    public static readonly Uri CollectionKey = new("fonts:AvaWin", UriKind.Absolute);

    private static readonly Uri s_source = new("avares://AvaWin.Theme/Assets/Fonts", UriKind.Absolute);
    private static readonly Uri s_semilight = new("avares://AvaWin.Theme/Assets/FontsExtra/selawksl.ttf", UriKind.Absolute);

    /// <summary>Initializes the collection and loads the embedded faces.</summary>
    public AvaWinFontCollection() : base(CollectionKey, s_source)
    {
        // Selawik Semilight declares usWeightClass 300, the same as Selawik Light, so the folder scan would drop
        // one of the two. It is loaded on its own and registered under the SemiLight (350) key it stands in for.
        try
        {
            using var stream = AssetLoader.Open(s_semilight);
            if (TryAddGlyphTypeface(stream, out var semilight) || semilight is not null)
            {
                TryAddGlyphTypeface(semilight, new FontCollectionKey(FontStyle.Normal, FontWeight.SemiLight, FontStretch.Normal));
            }
        }
        catch (Exception)
        {
            // The rest of the family still works. A SemiLight request gets the nearest face.
        }
    }
}
