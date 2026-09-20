using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using AvaWin.Animations;

namespace AvaWin.Controls;

/// <summary>Custom page animations for <see cref="FlipView.SetCustomAnimations"/>.</summary>
/// <param name="Next">Runs on a move forward, with the outgoing and the incoming page.</param>
/// <param name="Previous">Runs on a move backward.</param>
/// <param name="Jump">Runs on a jump to a page that is not adjacent.</param>
public sealed record FlipViewAnimations(
    Func<Control?, Control?, Task>? Next = null,
    Func<Control?, Control?, Task>? Previous = null,
    Func<Control?, Control?, Task>? Jump = null);

/// <summary>Arguments for <see cref="FlipView.PageVisibilityChanged"/>.</summary>
public sealed class FlipViewPageVisibilityEventArgs : RoutedEventArgs
{
    internal FlipViewPageVisibilityEventArgs(RoutedEvent routedEvent, Control? source, bool visible, int index) : base(routedEvent)
    {
        Page = source;
        Visible = visible;
        Index = index;
    }

    /// <summary>The page element.</summary>
    public Control? Page { get; }

    /// <summary>Whether the page became visible.</summary>
    public bool Visible { get; }

    /// <summary>The page index.</summary>
    public int Index { get; }
}

/// <summary>
/// Shows one page at a time. Navigation buttons of 69×39 pixels appear on hover. Adjacent pages slide over
/// 300 ms, and a jump cross-fades. It supports horizontal and vertical swipes. It does not wrap around.
/// </summary>
[TemplatePart("PART_Viewport", typeof(Panel), IsRequired = true)]
[TemplatePart("PART_PageA", typeof(ContentPresenter), IsRequired = true)]
[TemplatePart("PART_PageB", typeof(ContentPresenter), IsRequired = true)]
[TemplatePart("PART_PreviousButton", typeof(Button), IsRequired = true)]
[TemplatePart("PART_NextButton", typeof(Button), IsRequired = true)]
[PseudoClasses(":horizontal", ":vertical", ":cannext", ":canprevious", ":animating")]
public sealed class FlipView : SelectingItemsControl
{
    /// <summary>Defines the <see cref="Orientation"/> property.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<FlipView, Orientation>(nameof(Orientation), Orientation.Horizontal);

    /// <summary>Defines the <see cref="ItemSpacing"/> property.</summary>
    public static readonly StyledProperty<double> ItemSpacingProperty = AvaloniaProperty.Register<FlipView, double>(nameof(ItemSpacing));

    /// <summary>Defines the <see cref="PageSelected"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> PageSelectedEvent = RoutedEvent.Register<FlipView, RoutedEventArgs>(nameof(PageSelected), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="PageVisibilityChanged"/> event.</summary>
    public static readonly RoutedEvent<FlipViewPageVisibilityEventArgs> PageVisibilityChangedEvent = RoutedEvent.Register<FlipView, FlipViewPageVisibilityEventArgs>(nameof(PageVisibilityChanged), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="PageCompleted"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> PageCompletedEvent = RoutedEvent.Register<FlipView, RoutedEventArgs>(nameof(PageCompleted), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="DataSourceCountChanged"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> DataSourceCountChangedEvent = RoutedEvent.Register<FlipView, RoutedEventArgs>(nameof(DataSourceCountChanged), RoutingStrategies.Bubble);

    private const double SwipeThreshold = 50;

    private Panel? _viewport;
    private ContentPresenter? _pageA;
    private ContentPresenter? _pageB;
    private ContentPresenter? _current;
    private Button? _prev;
    private Button? _next;
    private FlipViewAnimations _animations = new();
    private int _shownIndex = -1;
    private int _generation;
    private bool _itemsHooked;
    private Point? _swipeStart;
    private bool _swipeHandled;

    static FlipView()
    {
        SelectionModeProperty.OverrideDefaultValue<FlipView>(SelectionMode.Single | SelectionMode.AlwaysSelected);
        OrientationProperty.Changed.AddClassHandler<FlipView>((f, _) => f.UpdatePseudoClasses());
        SelectedIndexProperty.Changed.AddClassHandler<FlipView>((f, e) => f.OnSelectedIndexChanged((int)e.OldValue!, (int)e.NewValue!));
        FocusableProperty.OverrideDefaultValue<FlipView>(true);
    }

    /// <summary>Initializes a new instance.</summary>
    public FlipView()
    {
        UpdatePseudoClasses();
    }

    /// <summary>The direction of the page slide and swipe.</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The gap between pages during the slide.</summary>
    public double ItemSpacing { get => GetValue(ItemSpacingProperty); set => SetValue(ItemSpacingProperty, value); }

    /// <summary>The index of the shown page. The same as <see cref="SelectingItemsControl.SelectedIndex"/>.</summary>
    public int CurrentPage { get => SelectedIndex; set => SelectedIndex = value; }

    /// <summary>The number of pages.</summary>
    public int Count => ItemCount;

    /// <summary>Raised when a new page has settled.</summary>
    public event EventHandler<RoutedEventArgs> PageSelected { add => AddHandler(PageSelectedEvent, value); remove => RemoveHandler(PageSelectedEvent, value); }

    /// <summary>Raised when a page becomes visible or invisible.</summary>
    public event EventHandler<FlipViewPageVisibilityEventArgs> PageVisibilityChanged { add => AddHandler(PageVisibilityChangedEvent, value); remove => RemoveHandler(PageVisibilityChangedEvent, value); }

    /// <summary>Raised when the content of the page is rendered.</summary>
    public event EventHandler<RoutedEventArgs> PageCompleted { add => AddHandler(PageCompletedEvent, value); remove => RemoveHandler(PageCompletedEvent, value); }

    /// <summary>Raised when the number of pages changes.</summary>
    public event EventHandler<RoutedEventArgs> DataSourceCountChanged { add => AddHandler(DataSourceCountChangedEvent, value); remove => RemoveHandler(DataSourceCountChangedEvent, value); }

    /// <summary>The element shown as the page now.</summary>
    public Control? CurrentPageElement => _current?.Child;

    /// <summary>Moves to the next page. Returns false at the last page.</summary>
    public Task<bool> NextAsync() => MoveAsync(1);

    /// <summary>Moves to the previous page. Returns false at the first page.</summary>
    public Task<bool> PreviousAsync() => MoveAsync(-1);

    /// <summary>Replaces the page animations. Null restores the defaults.</summary>
    public void SetCustomAnimations(FlipViewAnimations animations) => _animations = animations ?? new FlipViewAnimations();

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Unhook();
        _viewport = e.NameScope.Find<Panel>("PART_Viewport");
        _pageA = e.NameScope.Find<ContentPresenter>("PART_PageA");
        _pageB = e.NameScope.Find<ContentPresenter>("PART_PageB");
        _prev = e.NameScope.Find<Button>("PART_PreviousButton");
        _next = e.NameScope.Find<Button>("PART_NextButton");
        _current = _pageA;
        if (_pageB is { } b)
        {
            b.IsVisible = false;
        }

        if (_prev is { } p)
        {
            p.Click += OnPrevClick;
        }

        if (_next is { } n)
        {
            n.Click += OnNextClick;
        }

        _shownIndex = -1;
        Show(SelectedIndex, animate: false, direction: 0);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (!_itemsHooked)
        {
            _itemsHooked = true;
            ItemsView.CollectionChanged += OnItemsChanged;
        }

        if (SelectedIndex < 0 && ItemCount > 0)
        {
            SelectedIndex = 0;
        }
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return false;
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (ItemCount < 2 || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _swipeStart = e.GetPosition(this);
        _swipeHandled = false;
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
        var along = Orientation == Orientation.Horizontal ? delta.X : delta.Y;
        var across = Orientation == Orientation.Horizontal ? delta.Y : delta.X;
        if (Math.Abs(along) >= SwipeThreshold && Math.Abs(along) > Math.Abs(across))
        {
            _swipeHandled = true;
            _ = MoveAsync(along < 0 ? 1 : -1);
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
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        var horizontal = Orientation == Orientation.Horizontal;
        switch (e.Key)
        {
            case Key.Right when horizontal:
            case Key.Down when !horizontal:
            case Key.PageDown:
                _ = NextAsync();
                e.Handled = true;
                break;
            case Key.Left when horizontal:
            case Key.Up when !horizontal:
            case Key.PageUp:
                _ = PreviousAsync();
                e.Handled = true;
                break;
            case Key.Home:
                SelectedIndex = ItemCount > 0 ? 0 : -1;
                e.Handled = true;
                break;
            case Key.End:
                SelectedIndex = ItemCount - 1;
                e.Handled = true;
                break;
        }
    }

    private Task<bool> MoveAsync(int direction)
    {
        var target = SelectedIndex + direction;
        if (target < 0 || target >= ItemCount)
        {
            return Task.FromResult(false);
        }

        SelectedIndex = target;
        return Task.FromResult(true);
    }

    private void OnPrevClick(object? sender, RoutedEventArgs e) => _ = PreviousAsync();

    private void OnNextClick(object? sender, RoutedEventArgs e) => _ = NextAsync();

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(DataSourceCountChangedEvent));
        if (SelectedIndex < 0 && ItemCount > 0)
        {
            SelectedIndex = 0;
        }
        else if (SelectedIndex >= ItemCount)
        {
            SelectedIndex = ItemCount - 1;
        }
        else if (_shownIndex == SelectedIndex && _current is not null && !ReferenceEquals(_current.Content, SelectedIndex >= 0 ? ItemsView[SelectedIndex] : null))
        {
            _shownIndex = -1;
            Show(SelectedIndex, animate: false, direction: 0);
        }

        UpdatePseudoClasses();
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
    }

    private void OnSelectedIndexChanged(int oldIndex, int newIndex)
    {
        UpdatePseudoClasses();
        RaiseEvent(new RoutedEventArgs(PageSelectedEvent));
        var direction = oldIndex < 0 || newIndex < 0 ? 0 : Math.Abs(newIndex - oldIndex) == 1 ? Math.Sign(newIndex - oldIndex) : 2;
        Show(newIndex, animate: oldIndex >= 0 && direction != 0, direction);
    }

    private async void Show(int index, bool animate, int direction) => await ShowAsync(index, animate, direction);

    private async Task ShowAsync(int index, bool animate, int direction)
    {
        if (_current is null || _pageA is null || _pageB is null)
        {
            return;
        }

        if (index == _shownIndex)
        {
            return;
        }

        var generation = ++_generation;
        var oldIndex = _shownIndex;
        _shownIndex = index;
        var item = index >= 0 && index < ItemCount ? ItemsView[index] : null;
        var outgoing = _current;
        var incoming = ReferenceEquals(_current, _pageA) ? _pageB : _pageA;
        // The content is set before the template, so a typed FuncDataTemplate never has to build null.
        incoming.Content = item;
        incoming.ContentTemplate = ItemTemplate;
        incoming.IsVisible = true;
        incoming.ClearValue(OpacityProperty);
        incoming.ClearValue(RenderTransformProperty);
        _current = incoming;
        RaiseEvent(new FlipViewPageVisibilityEventArgs(PageVisibilityChangedEvent, incoming.Child, true, index));

        if (animate && outgoing.Content is not null && this.IsAttachedToVisualTree())
        {
            PseudoClasses.Set(":animating", true);
            try
            {
                var custom = direction switch { 1 => _animations.Next, -1 => _animations.Previous, _ => _animations.Jump };
                if (custom is not null)
                {
                    await custom(outgoing.Child, incoming.Child);
                }
                else if (direction == 2)
                {
                    await WinAnimations.CrossFade(incoming, outgoing);
                }
                else
                {
                    var size = (Orientation == Orientation.Horizontal ? _viewport?.Bounds.Width : _viewport?.Bounds.Height) ?? Bounds.Width;
                    var distance = size + ItemSpacing;
                    var sign = direction > 0 ? -1 : 1;
                    var outTo = Orientation == Orientation.Horizontal ? AnimationRunner.Translate(sign * distance, 0) : AnimationRunner.Translate(0, sign * distance);
                    var inFrom = Orientation == Orientation.Horizontal ? AnimationRunner.Translate(-sign * distance, 0) : AnimationRunner.Translate(0, -sign * distance);
                    await Task.WhenAll(
                        AnimationRunner.Run(outgoing, AnimationRunner.Transform(TransformOperations.Identity, outTo, 0, 300, WinEasing.Standard)),
                        AnimationRunner.Run(incoming, AnimationRunner.Transform(inFrom, TransformOperations.Identity, 0, 300, WinEasing.Standard)));
                }
            }
            finally
            {
                if (generation == _generation)
                {
                    PseudoClasses.Set(":animating", false);
                }
            }
        }

        if (generation != _generation)
        {
            return;
        }

        RaiseEvent(new FlipViewPageVisibilityEventArgs(PageVisibilityChangedEvent, outgoing.Child, false, oldIndex));
        outgoing.IsVisible = false;
        outgoing.ContentTemplate = null;
        outgoing.Content = null;
        outgoing.ClearValue(OpacityProperty);
        outgoing.ClearValue(RenderTransformProperty);
        RaiseEvent(new RoutedEventArgs(PageCompletedEvent));
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
        PseudoClasses.Set(":cannext", SelectedIndex >= 0 && SelectedIndex < ItemCount - 1);
        PseudoClasses.Set(":canprevious", SelectedIndex > 0);
    }
}
