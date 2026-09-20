using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using AvaWin.Animations;

namespace AvaWin.Controls;

/// <summary>
/// A track of headers above one visible <see cref="PivotItem"/>. The selected header comes first and the others
/// follow and wrap round. The selection changes on a header click, on Left, Right, Home or End on a focused header,
/// on the hover navigation buttons, or on a horizontal swipe with the mouse, a finger or a pen. It wraps around at the
/// ends.
/// </summary>
[TemplatePart("PART_Title", typeof(ContentPresenter))]
[TemplatePart("PART_Headers", typeof(Grid))]
[TemplatePart("PART_HeadersPanel", typeof(PivotHeadersPanel), IsRequired = true)]
[TemplatePart("PART_PrevButton", typeof(Button))]
[TemplatePart("PART_NextButton", typeof(Button))]
[TemplatePart("PART_Viewport", typeof(Panel), IsRequired = true)]
[TemplatePart("PART_ItemHost", typeof(ContentPresenter), IsRequired = true)]
[PseudoClasses(":locked", ":shownavbuttons", ":animating", ":hastitle")]
public sealed class Pivot : SelectingItemsControl
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<Pivot, object?>(nameof(Title));

    /// <summary>Defines the <see cref="IsLocked"/> property.</summary>
    public static readonly StyledProperty<bool> IsLockedProperty = AvaloniaProperty.Register<Pivot, bool>(nameof(IsLocked));

    /// <summary>Defines the <see cref="HeaderTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty = AvaloniaProperty.Register<Pivot, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>Defines the <see cref="PivotSelectionChanged"/> event.</summary>
    public static readonly RoutedEvent<PivotSelectionChangedEventArgs> PivotSelectionChangedEvent = RoutedEvent.Register<Pivot, PivotSelectionChangedEventArgs>(nameof(PivotSelectionChanged), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ItemAnimationStart"/> event.</summary>
    public static readonly RoutedEvent<PivotSelectionChangedEventArgs> ItemAnimationStartEvent = RoutedEvent.Register<Pivot, PivotSelectionChangedEventArgs>(nameof(ItemAnimationStart), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ItemAnimationEnd"/> event.</summary>
    public static readonly RoutedEvent<PivotSelectionChangedEventArgs> ItemAnimationEndEvent = RoutedEvent.Register<Pivot, PivotSelectionChangedEventArgs>(nameof(ItemAnimationEnd), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ItemLoaded"/> event.</summary>
    public static readonly RoutedEvent<PivotSelectionChangedEventArgs> ItemLoadedEvent = RoutedEvent.Register<Pivot, PivotSelectionChangedEventArgs>(nameof(ItemLoaded), RoutingStrategies.Bubble);

    private const double SwipeThreshold = 50;

    private PivotHeadersPanel? _headersPanel;
    private Grid? _headers;
    private Panel? _viewport;
    private ContentPresenter? _itemHost;
    private Button? _prev;
    private Button? _next;
    private readonly List<Button> _headerButtons = new();
    private readonly List<PivotItem> _pivotItems = new();
    private readonly Dictionary<object, PivotItem> _wrappers = new(ReferenceEqualityComparer.Instance);
    private static readonly object NullKey = new();
    private bool _itemsHooked;
    private int _shownIndex = -1;
    private CancellationTokenSource? _transition;
    private Point? _swipeStart;
    private bool _swipeHandled;
    private int _touchGestureId = -1;
    private double _touchDistance;

    static Pivot()
    {
        SelectionModeProperty.OverrideDefaultValue<Pivot>(SelectionMode.Single | SelectionMode.AlwaysSelected);
        IsLockedProperty.Changed.AddClassHandler<Pivot>((p, _) => p.UpdatePseudoClasses());
        TitleProperty.Changed.AddClassHandler<Pivot>((p, _) => p.UpdatePseudoClasses());
        SelectedIndexProperty.Changed.AddClassHandler<Pivot>((p, e) => p.OnSelectedIndexChanged((int)e.OldValue!, (int)e.NewValue!));
        FocusableProperty.OverrideDefaultValue<Pivot>(false);
    }

    /// <summary>Initializes a new instance.</summary>
    public Pivot()
    {
        UpdatePseudoClasses();

        // Touch and pen swipes need a recognizer: nested ScrollViewers capture the pointer after a few pixels of
        // vertical drift, so raw pointer moves never reach the Pivot.
        GestureRecognizers.Add(new ScrollGestureRecognizer
        {
            CanHorizontallyScroll = true,
            CanVerticallyScroll = false,
            IsScrollInertiaEnabled = false,
        });
        AddHandler(ScrollGestureEvent, OnScrollGesture);
        AddHandler(ScrollGestureEndedEvent, OnScrollGestureEnded);
    }

    /// <summary>The small title above the headers.</summary>
    public object? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Whether the pivot is locked: only the selected header is shown, and swiping and navigation are disabled.</summary>
    public bool IsLocked { get => GetValue(IsLockedProperty); set => SetValue(IsLockedProperty, value); }

    /// <summary>The template for the header content.</summary>
    public IDataTemplate? HeaderTemplate { get => GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty, value); }

    /// <summary>Raised when the selection changes, with the index and the item. The base <see cref="SelectingItemsControl.SelectionChanged"/> is raised too.</summary>
    public event EventHandler<PivotSelectionChangedEventArgs> PivotSelectionChanged { add => AddHandler(PivotSelectionChangedEvent, value); remove => RemoveHandler(PivotSelectionChangedEvent, value); }

    /// <summary>Raised when the transition to the new item starts.</summary>
    public event EventHandler<PivotSelectionChangedEventArgs> ItemAnimationStart { add => AddHandler(ItemAnimationStartEvent, value); remove => RemoveHandler(ItemAnimationStartEvent, value); }

    /// <summary>Raised when the transition to the new item ends.</summary>
    public event EventHandler<PivotSelectionChangedEventArgs> ItemAnimationEnd { add => AddHandler(ItemAnimationEndEvent, value); remove => RemoveHandler(ItemAnimationEndEvent, value); }

    /// <summary>Raised when the new item is shown.</summary>
    public event EventHandler<PivotSelectionChangedEventArgs> ItemLoaded { add => AddHandler(ItemLoadedEvent, value); remove => RemoveHandler(ItemLoadedEvent, value); }

    /// <summary>The selected item as a <see cref="PivotItem"/>.</summary>
    public PivotItem? SelectedPivotItem => PivotItemAt(SelectedIndex);

    /// <summary>Selects the next item. It wraps around at the end.</summary>
    public void SelectNext() => Step(1);

    /// <summary>Selects the previous item. It wraps around at the start.</summary>
    public void SelectPrevious() => Step(-1);

    /// <summary>The <see cref="PivotItem"/> of the item at <paramref name="index"/>. A data item gets a wrapper.</summary>
    public PivotItem? PivotItemAt(int index) => index >= 0 && index < _pivotItems.Count ? _pivotItems[index] : null;

    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (!_itemsHooked)
        {
            _itemsHooked = true;
            ItemsView.CollectionChanged += OnItemsChanged;
        }

        RebuildItems();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildItems();

    private void RebuildItems()
    {
        _pivotItems.Clear();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var item in ItemsView)
        {
            PivotItem pi;
            if (item is PivotItem existing)
            {
                pi = existing;
            }
            else
            {
                var key = item ?? NullKey;
                if (!_wrappers.TryGetValue(key, out pi!))
                {
                    pi = new PivotItem { Content = item, Header = item?.ToString() };
                    _wrappers[key] = pi;
                }

                pi.ContentTemplate = ItemTemplate;
                seen.Add(key);
            }

            _pivotItems.Add(pi);
        }

        foreach (var key in _wrappers.Keys.Where(k => !seen.Contains(k)).ToList())
        {
            _wrappers.Remove(key);
        }

        RebuildHeaders();
        if (SelectedIndex < 0 && ItemCount > 0)
        {
            SelectedIndex = 0;
        }
        else if (SelectedIndex >= ItemCount)
        {
            SelectedIndex = ItemCount - 1;
        }
        else
        {
            ShowSelected(animate: false);
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Unhook();
        _headersPanel = e.NameScope.Find<PivotHeadersPanel>("PART_HeadersPanel");
        _headers = e.NameScope.Find<Grid>("PART_Headers");
        _viewport = e.NameScope.Find<Panel>("PART_Viewport");
        _itemHost = e.NameScope.Find<ContentPresenter>("PART_ItemHost");
        _prev = e.NameScope.Find<Button>("PART_PrevButton");
        _next = e.NameScope.Find<Button>("PART_NextButton");
        if (_prev is { } p)
        {
            p.Click += OnPrevClick;
        }

        if (_next is { } n)
        {
            n.Click += OnNextClick;
        }

        if (_headers is { } h)
        {
            h.PointerEntered += OnHeadersPointerEntered;
            h.PointerExited += OnHeadersPointerExited;
            h.SizeChanged += OnHeadersSizeChanged;
            ClipHeaders(h);
        }

        RebuildHeaders();
        _shownIndex = -1;
        ShowSelected(animate: false);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (IsLocked || ItemCount < 2 || e.Pointer.Type != PointerType.Mouse || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _swipeStart = e.GetPosition(this);
        _swipeHandled = false;
    }

    private void OnScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        // Gestures from nested scrollers bubble up here too; only the Pivot's own recognizer has it as the source.
        if (!ReferenceEquals(e.Source, this) || IsLocked || ItemCount < 2)
        {
            return;
        }

        if (e.Id != _touchGestureId)
        {
            _touchGestureId = e.Id;
            _touchDistance = 0;
            _swipeHandled = false;
        }

        // Handling makes the recognizer capture the pointer, so nested scrollers no longer get it.
        e.Handled = true;
        _touchDistance += e.Delta.X;
        if (!_swipeHandled && Math.Abs(_touchDistance) >= SwipeThreshold)
        {
            _swipeHandled = true;
            Step(_touchDistance > 0 ? 1 : -1);
        }
    }

    private void OnScrollGestureEnded(object? sender, ScrollGestureEndedEventArgs e)
    {
        if (e.Id == _touchGestureId)
        {
            _touchGestureId = -1;
            _touchDistance = 0;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_swipeStart is not { } start || _swipeHandled)
        {
            return;
        }

        var delta = e.GetPosition(this) - start;
        if (Math.Abs(delta.X) >= SwipeThreshold && Math.Abs(delta.X) > Math.Abs(delta.Y))
        {
            _swipeHandled = true;
            Step(delta.X < 0 ? 1 : -1);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _swipeStart = null;
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _swipeStart = null;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || IsLocked || e.Source is not Button b || !_headerButtons.Contains(b))
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
                Step(-1);
                e.Handled = true;
                break;
            case Key.Right:
                Step(1);
                e.Handled = true;
                break;
            case Key.Home:
                SelectedIndex = 0;
                e.Handled = true;
                break;
            case Key.End:
                SelectedIndex = ItemCount - 1;
                e.Handled = true;
                break;
        }

        if (e.Handled)
        {
            FocusSelectedHeader();
        }
    }

    private void Step(int direction)
    {
        if (ItemCount == 0)
        {
            return;
        }

        var n = ItemCount;
        SelectedIndex = (((SelectedIndex + direction) % n) + n) % n;
    }

    private void FocusSelectedHeader()
    {
        if (SelectedIndex >= 0 && SelectedIndex < _headerButtons.Count)
        {
            _headerButtons[SelectedIndex].Focus(NavigationMethod.Directional);
        }
    }

    private void Unhook()
    {
        if (_prev is { } p)
        {
            p.Click -= OnPrevClick;
        }

        if (_next is { } n)
        {
            n.Click -= OnNextClick;
        }

        if (_headers is { } h)
        {
            h.PointerEntered -= OnHeadersPointerEntered;
            h.PointerExited -= OnHeadersPointerExited;
            h.SizeChanged -= OnHeadersSizeChanged;
        }

        if (_headersPanel is { } hp)
        {
            hp.Children.Clear();
        }

        _headerButtons.Clear();
    }

    /// <summary>The header row is shorter than its type. The looping headers are clipped at the sides only, and the
    /// descenders run <see cref="HeaderDescenderOverflow"/> pixels into the content.</summary>
    private const double HeaderDescenderOverflow = 16;

    private void OnHeadersSizeChanged(object? sender, SizeChangedEventArgs e) => ClipHeaders((Grid)sender!);

    private static void ClipHeaders(Grid headers) =>
        headers.Clip = new RectangleGeometry(new Rect(0, 0, headers.Bounds.Width, headers.Bounds.Height + HeaderDescenderOverflow));

    private void OnPrevClick(object? sender, RoutedEventArgs e) => SelectPrevious();

    private void OnNextClick(object? sender, RoutedEventArgs e) => SelectNext();

    private void OnHeadersPointerEntered(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            PseudoClasses.Set(":shownavbuttons", !IsLocked && ItemCount > 1);
        }
    }

    private void OnHeadersPointerExited(object? sender, PointerEventArgs e) => PseudoClasses.Set(":shownavbuttons", false);

    private void RebuildHeaders()
    {
        if (_headersPanel is null)
        {
            return;
        }

        _headersPanel.Children.Clear();
        _headerButtons.Clear();
        for (var i = 0; i < ItemCount; i++)
        {
            var index = i;
            var item = PivotItemAt(i);
            var button = new Button { Classes = { "win-pivot-header" } };
            if (item is not null)
            {
                button[!ContentControl.ContentProperty] = item[!HeaderedContentControl.HeaderProperty];
                button[!ContentControl.ContentTemplateProperty] = HeaderTemplate is not null
                    ? this[!HeaderTemplateProperty]
                    : item[!HeaderedContentControl.HeaderTemplateProperty];
            }

            button.Click += (_, _) => SelectedIndex = index;
            _headerButtons.Add(button);
            _headersPanel.Children.Add(button);
        }

        UpdateHeaderStates();
    }

    private void UpdateHeaderStates()
    {
        for (var i = 0; i < _headerButtons.Count; i++)
        {
            _headerButtons[i].Classes.Set("selected", i == SelectedIndex);
        }

        for (var i = 0; i < _pivotItems.Count; i++)
        {
            _pivotItems[i].IsSelected = i == SelectedIndex;
        }

        if (_headersPanel is { } hp)
        {
            hp.StartIndex = Math.Max(0, SelectedIndex);
        }
    }

    private void OnSelectedIndexChanged(int oldIndex, int newIndex)
    {
        UpdateHeaderStates();
        var item = SelectedPivotItem;
        RaiseEvent(new PivotSelectionChangedEventArgs(PivotSelectionChangedEvent, newIndex, item));
        var direction = 0;
        if (oldIndex >= 0 && newIndex >= 0 && ItemCount > 1)
        {
            var n = ItemCount;
            var forward = ((newIndex - oldIndex) % n + n) % n;
            direction = forward <= n / 2 ? 1 : -1;
        }

        Show(animate: oldIndex >= 0 && direction != 0, direction, oldIndex);
    }

    private void ShowSelected(bool animate) => Show(animate, 1, -1);

    /// <summary>
    /// Starts the transition to the current selection. A transition in progress is canceled first. Its animations
    /// stop where they are and the content and header state is reset. So quick clicks never stack animations on
    /// the same elements.
    /// </summary>
    private async void Show(bool animate, int direction, int oldIndex)
    {
        if (_itemHost is null)
        {
            return;
        }

        var previous = _transition;
        _transition = null;
        previous?.Cancel();
        previous?.Dispose();
        var index = SelectedIndex;
        var item = SelectedPivotItem;
        var oldContent = _itemHost.Content as Control;
        if (oldContent is not null)
        {
            oldContent.ClearValue(OpacityProperty);
            oldContent.ClearValue(RenderTransformProperty);
        }

        _headersPanel?.ClearValue(RenderTransformProperty);
        if (index == _shownIndex && ReferenceEquals(oldContent, item))
        {
            PseudoClasses.Set(":animating", false);
            return;
        }

        _shownIndex = index;
        if (!animate || oldContent is null || item is null || !this.IsAttachedToVisualTree())
        {
            _itemHost.Content = item;
            PseudoClasses.Set(":animating", false);
            if (item is not null)
            {
                RaiseEvent(new PivotSelectionChangedEventArgs(ItemLoadedEvent, index, item));
            }

            return;
        }

        var outgoing = oldContent;
        var incoming = item;

        var cts = new CancellationTokenSource();
        _transition = cts;
        var token = cts.Token;
        PseudoClasses.Set(":animating", true);
        RaiseEvent(new PivotSelectionChangedEventArgs(ItemAnimationStartEvent, index, item));

        // The track has already rotated to put the new header first. It slides in from where the old one was.
        var headerShift = _headerButtons.Count > oldIndex && oldIndex >= 0 ? _headerButtons[oldIndex].Bounds.Width : 0;
        var headersTask = _headersPanel is { } hp && headerShift > 0
            ? AnimationRunner.Run(hp, [AnimationRunner.Transform(AnimationRunner.Translate(direction > 0 ? headerShift : -headerShift, 0), TransformOperations.Identity, 0, 250, WinEasing.Snap)], token)
            : Task.FromResult(true);

        // The old content fades out over 117 ms. Then the new content slides in 40 px over 550 ms and fades in over 170 ms, in the swipe direction.
        var exited = await AnimationRunner.Run(outgoing, [AnimationRunner.Opacity(1, 0, 0, 117, WinEasing.Linear)], token);
        if (!exited)
        {
            return;
        }

        outgoing.ClearValue(OpacityProperty);
        _itemHost.Content = incoming;
        RaiseEvent(new PivotSelectionChangedEventArgs(ItemLoadedEvent, index, incoming));
        var entered = await AnimationRunner.Run(incoming,
        [
            AnimationRunner.Transform(AnimationRunner.Translate(direction > 0 ? 40 : -40, 0), TransformOperations.Identity, 0, 550, WinEasing.Standard),
            AnimationRunner.Opacity(0, 1, 0, 170, WinEasing.Standard),
        ], token);
        await headersTask;
        if (!entered || token.IsCancellationRequested)
        {
            return;
        }

        _transition = null;
        cts.Dispose();
        PseudoClasses.Set(":animating", false);
        RaiseEvent(new PivotSelectionChangedEventArgs(ItemAnimationEndEvent, index, incoming));
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":locked", IsLocked);
        PseudoClasses.Set(":hastitle", Title is not null);
        if (IsLocked)
        {
            PseudoClasses.Set(":shownavbuttons", false);
        }
    }
}
