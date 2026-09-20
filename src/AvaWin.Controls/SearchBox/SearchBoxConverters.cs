using Avalonia.Data.Converters;

namespace AvaWin.Controls;

/// <summary>Converters used by the SearchBox suggestion template.</summary>
public static class SearchBoxConverters
{
    /// <summary><see cref="SearchSuggestion.Text"/>.</summary>
    public static readonly IValueConverter Text = new FuncValueConverter<SearchSuggestion?, string?>(s => s?.Text);

    /// <summary><see cref="SearchSuggestion.DetailText"/>.</summary>
    public static readonly IValueConverter DetailText = new FuncValueConverter<SearchSuggestion?, string?>(s => s?.DetailText);

    /// <summary><see cref="SearchSuggestion.Image"/>.</summary>
    public static readonly IValueConverter Image = new FuncValueConverter<SearchSuggestion?, Avalonia.Media.IImage?>(s => s?.Image);
}
