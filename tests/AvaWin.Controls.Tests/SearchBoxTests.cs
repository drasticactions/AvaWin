using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Controls.Tests;

public class SearchBoxTests
{
    private static (Window window, SearchBox box, TextBox input) Make()
    {
        // The search history is process-wide per context, so each test gets its own context.
        var box = new SearchBox { PlaceholderText = "Search", SearchHistoryContext = Guid.NewGuid().ToString("N") };
        var window = ThemeTestHelpers.Host(new Border { Padding = new Thickness(20), Child = box }, "Light", Platform.Desktop);
        var input = box.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_Input");
        return (window, box, input);
    }

    [AvaloniaFact]
    public void Typing_Raises_QueryChanged_And_SuggestionsRequested_With_Deferral()
    {
        var (window, box, input) = Make();
        var queries = new List<string>();
        box.QueryChanged += (_, e) => queries.Add(e.QueryText);
        box.SuggestionsRequested += (_, e) =>
        {
            var deferral = e.GetDeferral();
            e.Suggestions.Add(new SearchSuggestion(SearchSuggestionKind.Query, e.QueryText + " one"));
            e.Suggestions.Add(new SearchSuggestion(SearchSuggestionKind.Separator, "Results"));
            e.Suggestions.Add(new SearchSuggestion(SearchSuggestionKind.Result, e.QueryText + " result", "detail", null, "TAG"));
            deferral.Dispose();
        };
        input.Focus();
        Assert.Contains(":inputfocus", box.Classes);
        window.KeyTextInput("ab");
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal("ab", box.QueryText);
        Assert.Equal(["ab"], queries);
        Assert.Equal(3, box.Suggestions.Count);
        Assert.True(box.IsSuggestionsOpen);
        Assert.Contains(":open", box.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Enter_Submits_Down_Enter_Chooses_And_Result_Carries_Tag()
    {
        var (window, box, input) = Make();
        var submitted = new List<string>();
        var chosen = new List<object?>();
        box.QuerySubmitted += (_, e) => submitted.Add(e.QueryText);
        box.ResultSuggestionChosen += (_, e) => chosen.Add(e.Tag);
        box.SuggestionsRequested += (_, e) =>
        {
            e.Suggestions.Add(new SearchSuggestion(SearchSuggestionKind.Query, e.QueryText + "-completion"));
            e.Suggestions.Add(new SearchSuggestion(SearchSuggestionKind.Result, "Result", "detail", null, "TAG"));
        };
        input.Focus();
        window.KeyTextInput("q");
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(["q"], submitted);
        Assert.False(box.IsSuggestionsOpen);
        Assert.Equal(["q"], box.SearchHistory);

        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.True(box.IsSuggestionsOpen);
        Assert.Equal(0, box.SelectedSuggestionIndex);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal("q-completion", box.QueryText);
        Assert.Equal(["q", "q-completion"], submitted);

        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.Equal(1, box.SelectedSuggestionIndex);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(["TAG"], chosen);
        Assert.Equal(2, submitted.Count);

        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.True(box.IsSuggestionsOpen);
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.False(box.IsSuggestionsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void ChooseSuggestionOnEnter_Picks_First_And_Button_Submits()
    {
        var (window, box, input) = Make();
        box.ChooseSuggestionOnEnter = true;
        var submitted = new List<string>();
        box.QuerySubmitted += (_, e) => submitted.Add(e.QueryText);
        box.SuggestionsRequested += (_, e) => e.Suggestions.Add(new SearchSuggestion(SearchSuggestionKind.Query, "first"));
        input.Focus();
        window.KeyTextInput("f");
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(["first"], submitted);

        box.QueryText = "typed";
        var button = box.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Button");
        AppBarTests.Click(window, button);
        Assert.Equal(["first", "typed"], submitted);
        window.Close();
    }

    [AvaloniaFact]
    public void FocusOnKeyboardInput_Focuses_The_Box()
    {
        var box = new SearchBox { FocusOnKeyboardInput = true };
        var other = new Button { Content = "x" };
        var window = ThemeTestHelpers.Host(new StackPanel { Children = { other, box } }, "Light", Platform.Desktop);
        var input = box.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_Input");
        other.Focus();
        window.KeyTextInput("z");
        Assert.True(input.IsFocused);
        Assert.Equal("z", box.QueryText);
        window.Close();
    }

    [AvaloniaFact]
    public void Highlight_Splits_Runs()
    {
        var text = new HighlightTextBlock { FullText = "Hello world", Highlight = "wor", HighlightBrush = Avalonia.Media.Brushes.Red };
        var window = ThemeTestHelpers.Host(text, "Light", Platform.Desktop);
        Assert.Equal(3, text.Inlines!.Count);
        window.Close();
    }
}
