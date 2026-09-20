using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaWin.Controls;

/// <summary>The kinds of suggestion under a <see cref="SearchBox"/>.</summary>
public enum SearchSuggestionKind
{
    /// <summary>A query completion, text only.</summary>
    Query,

    /// <summary>A result with an image, text and detail text. A chosen result raises <see cref="SearchBox.ResultSuggestionChosen"/>.</summary>
    Result,

    /// <summary>A labeled separator line.</summary>
    Separator,
}

/// <summary>One suggestion shown under a <see cref="SearchBox"/>.</summary>
public sealed record SearchSuggestion(SearchSuggestionKind Kind, string Text, string? DetailText = null, IImage? Image = null, object? Tag = null);

/// <summary>Arguments for <see cref="SearchBox.QueryChanged"/>.</summary>
public sealed class SearchQueryChangedEventArgs : RoutedEventArgs
{
    internal SearchQueryChangedEventArgs(RoutedEvent routedEvent, string queryText, string language) : base(routedEvent)
    {
        QueryText = queryText;
        Language = language;
    }

    /// <summary>The current text.</summary>
    public string QueryText { get; }

    /// <summary>The input language tag.</summary>
    public string Language { get; }
}

/// <summary>Arguments for <see cref="SearchBox.QuerySubmitted"/>.</summary>
public sealed class SearchQuerySubmittedEventArgs : RoutedEventArgs
{
    internal SearchQuerySubmittedEventArgs(RoutedEvent routedEvent, string queryText, KeyModifiers keyModifiers) : base(routedEvent)
    {
        QueryText = queryText;
        KeyModifiers = keyModifiers;
    }

    /// <summary>The submitted text.</summary>
    public string QueryText { get; }

    /// <summary>The modifier keys held during the submit.</summary>
    public KeyModifiers KeyModifiers { get; }
}

/// <summary>Arguments for <see cref="SearchBox.SuggestionsRequested"/>. Fill <see cref="Suggestions"/>, with <see cref="GetDeferral"/> when the result is asynchronous.</summary>
public sealed class SearchSuggestionsRequestedEventArgs : RoutedEventArgs
{
    private int _deferrals;
    private readonly TaskCompletionSource _completion = new();

    internal SearchSuggestionsRequestedEventArgs(RoutedEvent routedEvent, string queryText, IList<SearchSuggestion> suggestions) : base(routedEvent)
    {
        QueryText = queryText;
        Suggestions = suggestions;
    }

    /// <summary>The current text.</summary>
    public string QueryText { get; }

    /// <summary>The list to fill.</summary>
    public IList<SearchSuggestion> Suggestions { get; }

    /// <summary>Holds the request open. Dispose the result when the suggestions are ready.</summary>
    public IDisposable GetDeferral()
    {
        Interlocked.Increment(ref _deferrals);
        return new Deferral(this);
    }

    internal Task Completion => _deferrals == 0 ? Task.CompletedTask : _completion.Task;

    private void Complete()
    {
        if (Interlocked.Decrement(ref _deferrals) <= 0)
        {
            _completion.TrySetResult();
        }
    }

    private sealed class Deferral(SearchSuggestionsRequestedEventArgs owner) : IDisposable
    {
        private bool _done;

        public void Dispose()
        {
            if (!_done)
            {
                _done = true;
                owner.Complete();
            }
        }
    }
}

/// <summary>Arguments for <see cref="SearchBox.ResultSuggestionChosen"/>.</summary>
public sealed class SearchResultChosenEventArgs : RoutedEventArgs
{
    internal SearchResultChosenEventArgs(RoutedEvent routedEvent, SearchSuggestion suggestion) : base(routedEvent)
    {
        Suggestion = suggestion;
        Tag = suggestion.Tag;
    }

    /// <summary>The chosen result.</summary>
    public SearchSuggestion Suggestion { get; }

    /// <summary>The tag of the chosen suggestion.</summary>
    public object? Tag { get; }
}

/// <summary>A suggestion row in the flyout: a query, a result or a separator.</summary>
[PseudoClasses(":query", ":result", ":separator", ":selected")]
public sealed class SearchSuggestionItem : ContentControl
{
    /// <summary>Defines the <see cref="IsSelected"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<SearchSuggestionItem, bool>(nameof(IsSelected));

    /// <summary>Defines the <see cref="Suggestion"/> property.</summary>
    public static readonly StyledProperty<SearchSuggestion?> SuggestionProperty = AvaloniaProperty.Register<SearchSuggestionItem, SearchSuggestion?>(nameof(Suggestion));

    /// <summary>Defines the <see cref="Highlight"/> property (the query text to highlight).</summary>
    public static readonly StyledProperty<string?> HighlightProperty = AvaloniaProperty.Register<SearchSuggestionItem, string?>(nameof(Highlight));

    static SearchSuggestionItem()
    {
        IsSelectedProperty.Changed.AddClassHandler<SearchSuggestionItem>((i, e) => i.PseudoClasses.Set(":selected", (bool)e.NewValue!));
        SuggestionProperty.Changed.AddClassHandler<SearchSuggestionItem>((i, _) => i.UpdateKind());
        FocusableProperty.OverrideDefaultValue<SearchSuggestionItem>(false);
    }

    /// <summary>Whether the keyboard selection is on this row.</summary>
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    /// <summary>The suggestion.</summary>
    public SearchSuggestion? Suggestion { get => GetValue(SuggestionProperty); set => SetValue(SuggestionProperty, value); }

    /// <summary>The query text highlighted inside the suggestion text.</summary>
    public string? Highlight { get => GetValue(HighlightProperty); set => SetValue(HighlightProperty, value); }

    private void UpdateKind()
    {
        var k = Suggestion?.Kind;
        PseudoClasses.Set(":query", k == SearchSuggestionKind.Query);
        PseudoClasses.Set(":result", k == SearchSuggestionKind.Result);
        PseudoClasses.Set(":separator", k == SearchSuggestionKind.Separator);
    }
}

/// <summary>
/// A 266×28 text box with a search button and a suggestions flyout. Suggestions are queries, results or
/// separators, with the matched text highlighted. Typing raises <see cref="QueryChanged"/> and
/// <see cref="SuggestionsRequested"/>. Enter raises <see cref="QuerySubmitted"/>, or chooses the selected
/// suggestion when <see cref="ChooseSuggestionOnEnter"/> is true. Up and Down move through the suggestions.
/// Escape closes the flyout.
/// </summary>
[TemplatePart("PART_Input", typeof(TextBox), IsRequired = true)]
[TemplatePart("PART_Button", typeof(Button), IsRequired = true)]
[TemplatePart("PART_Flyout", typeof(Popup))]
[TemplatePart("PART_Suggestions", typeof(ItemsControl))]
[PseudoClasses(":inputfocus", ":open")]
public sealed class SearchBox : TemplatedControl
{
    /// <summary>Defines the <see cref="QueryText"/> property.</summary>
    public static readonly StyledProperty<string> QueryTextProperty = AvaloniaProperty.Register<SearchBox, string>(nameof(QueryText), string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="PlaceholderText"/> property.</summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty = AvaloniaProperty.Register<SearchBox, string?>(nameof(PlaceholderText));

    /// <summary>Defines the <see cref="ChooseSuggestionOnEnter"/> property.</summary>
    public static readonly StyledProperty<bool> ChooseSuggestionOnEnterProperty = AvaloniaProperty.Register<SearchBox, bool>(nameof(ChooseSuggestionOnEnter));

    /// <summary>Defines the <see cref="FocusOnKeyboardInput"/> property.</summary>
    public static readonly StyledProperty<bool> FocusOnKeyboardInputProperty = AvaloniaProperty.Register<SearchBox, bool>(nameof(FocusOnKeyboardInput));

    /// <summary>Defines the <see cref="SearchHistoryDisabled"/> property.</summary>
    public static readonly StyledProperty<bool> SearchHistoryDisabledProperty = AvaloniaProperty.Register<SearchBox, bool>(nameof(SearchHistoryDisabled));

    /// <summary>Defines the <see cref="SearchHistoryContext"/> property.</summary>
    public static readonly StyledProperty<string?> SearchHistoryContextProperty = AvaloniaProperty.Register<SearchBox, string?>(nameof(SearchHistoryContext));

    /// <summary>Defines the <see cref="IsSuggestionsOpen"/> property.</summary>
    public static readonly DirectProperty<SearchBox, bool> IsSuggestionsOpenProperty = AvaloniaProperty.RegisterDirect<SearchBox, bool>(nameof(IsSuggestionsOpen), o => o.IsSuggestionsOpen);

    /// <summary>Defines the <see cref="QueryChanged"/> event.</summary>
    public static readonly RoutedEvent<SearchQueryChangedEventArgs> QueryChangedEvent = RoutedEvent.Register<SearchBox, SearchQueryChangedEventArgs>(nameof(QueryChanged), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="QuerySubmitted"/> event.</summary>
    public static readonly RoutedEvent<SearchQuerySubmittedEventArgs> QuerySubmittedEvent = RoutedEvent.Register<SearchBox, SearchQuerySubmittedEventArgs>(nameof(QuerySubmitted), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="SuggestionsRequested"/> event.</summary>
    public static readonly RoutedEvent<SearchSuggestionsRequestedEventArgs> SuggestionsRequestedEvent = RoutedEvent.Register<SearchBox, SearchSuggestionsRequestedEventArgs>(nameof(SuggestionsRequested), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ResultSuggestionChosen"/> event.</summary>
    public static readonly RoutedEvent<SearchResultChosenEventArgs> ResultSuggestionChosenEvent = RoutedEvent.Register<SearchBox, SearchResultChosenEventArgs>(nameof(ResultSuggestionChosen), RoutingStrategies.Bubble);

    private static readonly Dictionary<string, List<string>> History = new();

    private TextBox? _input;
    private Button? _button;
    private Popup? _flyout;
    private ItemsControl? _list;
    private TopLevel? _topLevel;
    private int _selectedSuggestion = -1;
    private bool _syncingText;
    private bool _isOpen;
    private int _requestId;

    static SearchBox()
    {
        QueryTextProperty.Changed.AddClassHandler<SearchBox>((s, e) => s.OnQueryTextChanged((string)e.NewValue!));
        FocusableProperty.OverrideDefaultValue<SearchBox>(false);
    }

    /// <summary>Initializes a new instance.</summary>
    public SearchBox()
    {
        Suggestions = new AvaloniaList<SearchSuggestion>();
        Suggestions.CollectionChanged += (_, _) => RenderSuggestions();
    }

    /// <summary>The text in the box. Two-way.</summary>
    public string QueryText { get => GetValue(QueryTextProperty); set => SetValue(QueryTextProperty, value); }

    /// <summary>The text shown while the box is empty.</summary>
    public string? PlaceholderText { get => GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }

    /// <summary>Whether Enter chooses the first suggestion instead of submitting the text.</summary>
    public bool ChooseSuggestionOnEnter { get => GetValue(ChooseSuggestionOnEnterProperty); set => SetValue(ChooseSuggestionOnEnterProperty, value); }

    /// <summary>Whether typing anywhere in the window focuses the box.</summary>
    public bool FocusOnKeyboardInput { get => GetValue(FocusOnKeyboardInputProperty); set => SetValue(FocusOnKeyboardInputProperty, value); }

    /// <summary>Whether submitted queries are kept out of the history.</summary>
    public bool SearchHistoryDisabled { get => GetValue(SearchHistoryDisabledProperty); set => SetValue(SearchHistoryDisabledProperty, value); }

    /// <summary>The name of the history this box shares. The history is kept in memory per context.</summary>
    public string? SearchHistoryContext { get => GetValue(SearchHistoryContextProperty); set => SetValue(SearchHistoryContextProperty, value); }

    /// <summary>The suggestions shown under the box. Fill it in <see cref="SuggestionsRequested"/>.</summary>
    public AvaloniaList<SearchSuggestion> Suggestions { get; }

    /// <summary>Whether the suggestions flyout is open.</summary>
    public bool IsSuggestionsOpen { get => _isOpen; private set => SetAndRaise(IsSuggestionsOpenProperty, ref _isOpen, value); }

    /// <summary>The index of the suggestion selected with the keyboard, or −1.</summary>
    public int SelectedSuggestionIndex => _selectedSuggestion;

    /// <summary>Raised when the text changes.</summary>
    public event EventHandler<SearchQueryChangedEventArgs> QueryChanged { add => AddHandler(QueryChangedEvent, value); remove => RemoveHandler(QueryChangedEvent, value); }

    /// <summary>Raised when the user submits the text.</summary>
    public event EventHandler<SearchQuerySubmittedEventArgs> QuerySubmitted { add => AddHandler(QuerySubmittedEvent, value); remove => RemoveHandler(QuerySubmittedEvent, value); }

    /// <summary>Raised when the box needs suggestions for the current text.</summary>
    public event EventHandler<SearchSuggestionsRequestedEventArgs> SuggestionsRequested { add => AddHandler(SuggestionsRequestedEvent, value); remove => RemoveHandler(SuggestionsRequestedEvent, value); }

    /// <summary>Raised when the user chooses a result suggestion.</summary>
    public event EventHandler<SearchResultChosenEventArgs> ResultSuggestionChosen { add => AddHandler(ResultSuggestionChosenEvent, value); remove => RemoveHandler(ResultSuggestionChosenEvent, value); }

    /// <summary>The search history of <see cref="SearchHistoryContext"/>, most recent first.</summary>
    public IReadOnlyList<string> SearchHistory => History.TryGetValue(SearchHistoryContext ?? string.Empty, out var h) ? h : Array.Empty<string>();

    /// <summary>Submits the current text, as the search button and Enter do.</summary>
    public void Submit(KeyModifiers modifiers = KeyModifiers.None)
    {
        var text = QueryText ?? string.Empty;
        if (!SearchHistoryDisabled && text.Length > 0)
        {
            var key = SearchHistoryContext ?? string.Empty;
            if (!History.TryGetValue(key, out var list))
            {
                History[key] = list = new List<string>();
            }

            list.Remove(text);
            list.Insert(0, text);
        }

        Close();
        RaiseEvent(new SearchQuerySubmittedEventArgs(QuerySubmittedEvent, text, modifiers));
    }

    /// <summary>Closes the suggestions flyout.</summary>
    public void Close()
    {
        _selectedSuggestion = -1;
        IsSuggestionsOpen = false;
        PseudoClasses.Set(":open", false);
        if (_flyout is { } f)
        {
            f.IsOpen = false;
        }

        RenderSuggestions();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Unhook();
        _input = e.NameScope.Find<TextBox>("PART_Input");
        _button = e.NameScope.Find<Button>("PART_Button");
        _flyout = e.NameScope.Find<Popup>("PART_Flyout");
        _list = e.NameScope.Find<ItemsControl>("PART_Suggestions");
        if (_input is { } input)
        {
            input.Text = QueryText;
            input.TextChanged += OnInputTextChanged;
            input.GotFocus += OnInputGotFocus;
            input.LostFocus += OnInputLostFocus;
            input.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
        }

        if (_button is { } button)
        {
            button.Click += OnButtonClick;
        }

        if (_list is { } list)
        {
            list.AddHandler(PointerReleasedEvent, OnSuggestionPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        RenderSuggestions();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(TextInputEvent, OnTopLevelTextInput, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel?.RemoveHandler(TextInputEvent, OnTopLevelTextInput);
        _topLevel = null;
    }

    private void Unhook()
    {
        if (_input is { } input)
        {
            input.TextChanged -= OnInputTextChanged;
            input.GotFocus -= OnInputGotFocus;
            input.LostFocus -= OnInputLostFocus;
            input.RemoveHandler(KeyDownEvent, OnInputKeyDown);
        }

        if (_button is { } button)
        {
            button.Click -= OnButtonClick;
        }
    }

    private void OnTopLevelTextInput(object? sender, TextInputEventArgs e)
    {
        if (!FocusOnKeyboardInput || _input is null || _input.IsFocused || string.IsNullOrEmpty(e.Text) || !IsEnabled)
        {
            return;
        }

        var focused = _topLevel?.FocusManager?.GetFocusedElement();
        if (focused is TextBox or Avalonia.Controls.Primitives.TextSelectionHandle)
        {
            return;
        }

        _input.Focus();
        _input.Text = (QueryText ?? string.Empty) + e.Text;
        _input.CaretIndex = _input.Text.Length;
        e.Handled = true;
    }

    private void OnInputGotFocus(object? sender, FocusChangedEventArgs e)
    {
        PseudoClasses.Set(":inputfocus", true);
        if (Suggestions.Count > 0)
        {
            Open();
        }
    }

    private void OnInputLostFocus(object? sender, FocusChangedEventArgs e)
    {
        PseudoClasses.Set(":inputfocus", false);
        Dispatcher.UIThread.Post(() =>
        {
            if (_input?.IsFocused != true && !(_flyout?.IsPointerOver ?? false))
            {
                Close();
            }
        }, DispatcherPriority.Input);
    }

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncingText || _input is null)
        {
            return;
        }

        _syncingText = true;
        try
        {
            SetCurrentValue(QueryTextProperty, _input.Text ?? string.Empty);
        }
        finally
        {
            _syncingText = false;
        }
    }

    private void OnQueryTextChanged(string text)
    {
        if (_input is not null && !_syncingText)
        {
            _syncingText = true;
            try
            {
                _input.Text = text;
            }
            finally
            {
                _syncingText = false;
            }
        }

        RaiseEvent(new SearchQueryChangedEventArgs(QueryChangedEvent, text, System.Globalization.CultureInfo.CurrentUICulture.Name));
        _ = RequestSuggestionsAsync(text);
    }

    private async Task RequestSuggestionsAsync(string text)
    {
        var id = ++_requestId;
        var collected = new List<SearchSuggestion>();
        var args = new SearchSuggestionsRequestedEventArgs(SuggestionsRequestedEvent, text, collected);
        RaiseEvent(args);
        await args.Completion;
        if (id != _requestId)
        {
            return;
        }

        Suggestions.Clear();
        Suggestions.AddRange(collected);
        _selectedSuggestion = -1;
        if (Suggestions.Count > 0 && (_input?.IsFocused ?? false))
        {
            Open();
        }
        else if (Suggestions.Count == 0)
        {
            Close();
        }
    }

    private void Open()
    {
        if (Suggestions.Count == 0)
        {
            return;
        }

        IsSuggestionsOpen = true;
        PseudoClasses.Set(":open", true);
        if (_flyout is { } f)
        {
            f.IsOpen = true;
        }
    }

    private void RenderSuggestions()
    {
        if (_list is null)
        {
            return;
        }

        var items = new List<SearchSuggestionItem>(Suggestions.Count);
        for (var i = 0; i < Suggestions.Count; i++)
        {
            items.Add(new SearchSuggestionItem { Suggestion = Suggestions[i], Highlight = QueryText, IsSelected = i == _selectedSuggestion });
        }

        _list.ItemsSource = items;
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e) => Submit();

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (_selectedSuggestion >= 0 && _selectedSuggestion < Suggestions.Count)
                {
                    Choose(Suggestions[_selectedSuggestion], e.KeyModifiers);
                }
                else if (ChooseSuggestionOnEnter && FirstSelectable() is { } first)
                {
                    Choose(first, e.KeyModifiers);
                }
                else
                {
                    Submit(e.KeyModifiers);
                }

                e.Handled = true;
                break;
            case Key.Escape:
                if (IsSuggestionsOpen)
                {
                    Close();
                    e.Handled = true;
                }

                break;
            case Key.Down:
                if (Suggestions.Count > 0)
                {
                    Open();
                    MoveSelection(1);
                    e.Handled = true;
                }

                break;
            case Key.Up:
                if (IsSuggestionsOpen)
                {
                    MoveSelection(-1);
                    e.Handled = true;
                }

                break;
        }
    }

    private SearchSuggestion? FirstSelectable()
    {
        foreach (var s in Suggestions)
        {
            if (s.Kind != SearchSuggestionKind.Separator)
            {
                return s;
            }
        }

        return null;
    }

    private void MoveSelection(int delta)
    {
        var count = Suggestions.Count;
        if (count == 0)
        {
            return;
        }

        var index = _selectedSuggestion;
        for (var step = 0; step < count; step++)
        {
            index += delta;
            if (index < -1)
            {
                index = count - 1;
            }
            else if (index >= count)
            {
                index = -1;
            }

            if (index < 0 || Suggestions[index].Kind != SearchSuggestionKind.Separator)
            {
                break;
            }
        }

        _selectedSuggestion = index;
        RenderSuggestions();
    }

    private void OnSuggestionPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        var item = (e.Source as Visual)?.FindAncestorOfType<SearchSuggestionItem>(true);
        if (item?.Suggestion is { } s && s.Kind != SearchSuggestionKind.Separator)
        {
            Choose(s, e.KeyModifiers);
            e.Handled = true;
        }
    }

    private void Choose(SearchSuggestion suggestion, KeyModifiers modifiers)
    {
        if (suggestion.Kind == SearchSuggestionKind.Result)
        {
            Close();
            RaiseEvent(new SearchResultChosenEventArgs(ResultSuggestionChosenEvent, suggestion));
            return;
        }

        SetCurrentValue(QueryTextProperty, suggestion.Text);
        Submit(modifiers);
    }
}
