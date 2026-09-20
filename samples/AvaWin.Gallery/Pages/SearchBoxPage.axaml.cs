using System.Linq;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class SearchBoxPage : SamplePage
{
    private readonly SearchViewModel _vm = new();

    public SearchBoxPage()
    {
        DataContext = _vm;
        InitializeComponent();
        Search.SuggestionsRequested += OnSuggestionsRequested;
        Search.QuerySubmitted += (_, e) =>
        {
            _vm.Results = _vm.Matches(e.QueryText).ToList();
            _vm.Status = $"QuerySubmitted “{e.QueryText}”: {_vm.Results.Count} results";
        };
        Search.ResultSuggestionChosen += (_, e) =>
        {
            var c = (Contact)e.Tag!;
            _vm.Results = [c];
            _vm.Status = $"ResultSuggestionChosen: {c.Name}";
        };
    }

    private void OnSuggestionsRequested(object? sender, SearchSuggestionsRequestedEventArgs e)
    {
        if (e.QueryText.Length == 0)
        {
            return;
        }

        // Query completions first, then a labeled separator and the top people as result suggestions.
        var matches = _vm.Matches(e.QueryText).Take(6).ToList();
        foreach (var city in matches.Select(m => m.City).Distinct().Take(2))
        {
            e.Suggestions.Add(new SearchSuggestion(SearchSuggestionKind.Query, city));
        }

        if (matches.Count > 0)
        {
            e.Suggestions.Add(new SearchSuggestion(SearchSuggestionKind.Separator, "People"));
            foreach (var m in matches.Take(4))
            {
                e.Suggestions.Add(new SearchSuggestion(SearchSuggestionKind.Result, m.Name, m.City, null, m));
            }
        }
    }
}
