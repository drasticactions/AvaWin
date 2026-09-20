using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using AvaWin.Animations;

namespace AvaWin.Controls;

/// <summary>
/// A panorama of <see cref="HubSection"/>s that scrolls horizontally or vertically, with a staggered entrance
/// animation, a loading ring and interactive section headers.
/// </summary>
[TemplatePart("PART_Viewport", typeof(ScrollViewer), IsRequired = true)]
[TemplatePart("PART_Progress", typeof(ProgressBar))]
[PseudoClasses(":horizontal", ":vertical", ":loading")]
public sealed partial class Hub : ItemsControl
{
    /// <summary>Defines the <see cref="Orientation"/> property.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<Hub, Orientation>(nameof(Orientation));

    /// <summary>Defines the <see cref="HeaderTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty = AvaloniaProperty.Register<Hub, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>Defines the <see cref="SectionOnScreen"/> property.</summary>
    public static readonly DirectProperty<Hub, int> SectionOnScreenProperty = AvaloniaProperty.RegisterDirect<Hub, int>(nameof(SectionOnScreen), o => o.SectionOnScreen, (o, v) => o.SectionOnScreen = v);

    /// <summary>Defines the <see cref="ScrollPosition"/> property.</summary>
    public static readonly DirectProperty<Hub, double> ScrollPositionProperty = AvaloniaProperty.RegisterDirect<Hub, double>(nameof(ScrollPosition), o => o.ScrollPosition, (o, v) => o.ScrollPosition = v);

    /// <summary>Defines the <see cref="LoadingState"/> property.</summary>
    public static readonly DirectProperty<Hub, HubLoadingState> LoadingStateProperty = AvaloniaProperty.RegisterDirect<Hub, HubLoadingState>(nameof(LoadingState), o => o.LoadingState);

    /// <summary>Defines the <see cref="HeaderInvoked"/> event.</summary>
    public static readonly RoutedEvent<HubHeaderInvokedEventArgs> HeaderInvokedEvent = RoutedEvent.Register<Hub, HubHeaderInvokedEventArgs>(nameof(HeaderInvoked), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="LoadingStateChanged"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> LoadingStateChangedEvent = RoutedEvent.Register<Hub, RoutedEventArgs>(nameof(LoadingStateChanged), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ContentAnimating"/> event.</summary>
    public static readonly RoutedEvent<HubContentAnimatingEventArgs> ContentAnimatingEvent = RoutedEvent.Register<Hub, HubContentAnimatingEventArgs>(nameof(ContentAnimating), RoutingStrategies.Bubble);

    private static readonly FuncTemplate<Panel?> DefaultPanel = new(() => new StackPanel { Orientation = Orientation.Horizontal });

    private ScrollViewer? _viewport;
    private HubLoadingState _loadingState = HubLoadingState.Loading;
    private bool _entranceDone;
    private int _loadId;

    static Hub()
    {
        ItemsPanelProperty.OverrideDefaultValue<Hub>(DefaultPanel);
        OrientationProperty.Changed.AddClassHandler<Hub>((h, _) => h.OnOrientationChanged());
        FocusableProperty.OverrideDefaultValue<Hub>(false);
    }

    /// <summary>Initializes a new instance.</summary>
    public Hub()
    {
        AddHandler(HubSection.HeaderInvokedEvent, OnSectionHeaderInvoked);
        InitializeWheelScrolling();
        UpdatePseudoClasses();
    }

    /// <summary>The scroll direction of the panorama.</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The template for the header content of every section. When null, each section shows its own header.</summary>
    public IDataTemplate? HeaderTemplate { get => GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty, value); }

    /// <summary>The index of the first section in the viewport.</summary>
    public int IndexOfFirstVisible => VisibleRange().first;

    /// <summary>The index of the last section in the viewport.</summary>
    public int IndexOfLastVisible => VisibleRange().last;

    /// <summary>The index of the section at the leading edge. Setting it scrolls to that section.</summary>
    public int SectionOnScreen
    {
        get => VisibleRange().first;
        set => ScrollToSection(value);
    }

    /// <summary>The scroll offset along the orientation. It raises change notifications as the user pans.</summary>
    public double ScrollPosition
    {
        get => _viewport is null ? 0 : (Orientation == Orientation.Horizontal ? _viewport.Offset.X : _viewport.Offset.Y);
        set
        {
            if (_viewport is { } vp)
            {
                vp.Offset = Orientation == Orientation.Horizontal ? new Vector(value, vp.Offset.Y) : new Vector(vp.Offset.X, value);
            }
        }
    }

    /// <summary>Whether the hub is still loading, or is complete.</summary>
    public HubLoadingState LoadingState
    {
        get => _loadingState;
        private set
        {
            if (SetAndRaise(LoadingStateProperty, ref _loadingState, value))
            {
                UpdatePseudoClasses();
                RaiseEvent(new RoutedEventArgs(LoadingStateChangedEvent));
            }
        }
    }

    /// <summary>Raised when the user clicks an interactive section header.</summary>
    public event EventHandler<HubHeaderInvokedEventArgs> HeaderInvoked { add => AddHandler(HeaderInvokedEvent, value); remove => RemoveHandler(HeaderInvokedEvent, value); }

    /// <summary>Raised when <see cref="LoadingState"/> changes.</summary>
    public event EventHandler<RoutedEventArgs> LoadingStateChanged { add => AddHandler(LoadingStateChangedEvent, value); remove => RemoveHandler(LoadingStateChangedEvent, value); }

    /// <summary>Raised before the entrance, a content transition, an insert or a remove animation. Cancel it to skip the animation.</summary>
    public event EventHandler<HubContentAnimatingEventArgs> ContentAnimating { add => AddHandler(ContentAnimatingEvent, value); remove => RemoveHandler(ContentAnimatingEvent, value); }

    /// <summary>The section containers, in order.</summary>
    public IEnumerable<HubSection> Sections => Enumerable.Range(0, ItemCount).Select(ContainerFromIndex).OfType<HubSection>();

    /// <summary>Scrolls so that the section at <paramref name="index"/> sits at the leading edge.</summary>
    public void ScrollToSection(int index)
    {
        if (_viewport is null || index < 0 || index >= ItemCount || ContainerFromIndex(index) is not { } section)
        {
            return;
        }

        var surface = ItemsPanelRoot;
        if (surface is null)
        {
            return;
        }

        var pos = section.TranslatePoint(new Point(0, 0), surface) ?? default;
        var padding = surface.Margin;
        _viewport.Offset = Orientation == Orientation.Horizontal
            ? new Vector(Math.Max(0, pos.X - padding.Left), _viewport.Offset.Y)
            : new Vector(_viewport.Offset.X, Math.Max(0, pos.Y - padding.Top));
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is HubSection)
        {
            recycleKey = null;
            return false;
        }

        recycleKey = DefaultRecycleKey;
        return true;
    }

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new HubSection();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is HubSection section)
        {
            section.Orientation = Orientation;
            if (!ReferenceEquals(section, item))
            {
                section.Content = item;
                section.ContentTemplate = ItemTemplate;
                section.Header ??= item?.ToString();
            }

            if (HeaderTemplate is { } ht)
            {
                section.HeaderTemplate = ht;
            }

            if (!_entranceDone)
            {
                section.Opacity = 0;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_viewport is { } old)
        {
            UnhookSnapping(old);
        }

        _viewport = e.NameScope.Find<ScrollViewer>("PART_Viewport");
        if (_viewport is { } vp)
        {
            HookSnapping(vp);
        }
        Dispatcher.UIThread.Post(OnOrientationChanged, DispatcherPriority.Loaded);
    }

    /// <summary>Raises <see cref="ScrollPositionProperty"/> as the viewport scrolls.</summary>
    private void OnViewportScrolled(object? sender, ScrollChangedEventArgs e)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var delta = horizontal ? e.OffsetDelta.X : e.OffsetDelta.Y;
        if (delta != 0)
        {
            var now = ScrollPosition;
            RaisePropertyChanged(ScrollPositionProperty, now - delta, now);
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var id = ++_loadId;
        LoadingState = HubLoadingState.Loading;
        PseudoClasses.Set(":loading", true);
        Dispatcher.UIThread.Post(() => _ = LoadAsync(id), DispatcherPriority.Loaded);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _loadId++;
    }

    private async System.Threading.Tasks.Task LoadAsync(int id)
    {
        if (id != _loadId)
        {
            return;
        }

        UpdateLayout();
        var sections = Sections.ToList();
        // The progress ring goes away before the entrance animation. The state is complete after the animation.
        PseudoClasses.Set(":loading", false);
        if (!_entranceDone)
        {
            _entranceDone = true;
            var args = new HubContentAnimatingEventArgs(ContentAnimatingEvent, HubAnimationType.Entrance, -1, null);
            RaiseEvent(args);
            foreach (var s in sections)
            {
                s.ClearValue(OpacityProperty);
            }

            if (!args.Cancel)
            {
                var (first, last) = VisibleRange();
                var visible = sections.Where((_, i) => i >= first && i <= last).ToList();
                if (visible.Count > 0)
                {
                    await WinAnimations.EnterContentStaggered(visible, new WinOffset(100, 0), 83);
                }
            }
        }

        if (id == _loadId)
        {
            LoadingState = HubLoadingState.Complete;
        }
    }

    private (int first, int last) VisibleRange()
    {
        if (_viewport is null || ItemsPanelRoot is null || ItemCount == 0)
        {
            return (0, Math.Max(0, ItemCount - 1));
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var viewStart = horizontal ? _viewport.Offset.X : _viewport.Offset.Y;
        var viewEnd = viewStart + (horizontal ? _viewport.Viewport.Width : _viewport.Viewport.Height);
        if (viewEnd <= viewStart)
        {
            return (0, ItemCount - 1);
        }

        var first = -1;
        var last = -1;
        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFromIndex(i) is not { } c)
            {
                continue;
            }

            var pos = c.TranslatePoint(new Point(0, 0), ItemsPanelRoot) ?? default;
            var start = (horizontal ? pos.X : pos.Y) + (horizontal ? ItemsPanelRoot.Margin.Left : ItemsPanelRoot.Margin.Top);
            var end = start + (horizontal ? c.Bounds.Width : c.Bounds.Height);
            if (end > viewStart + 1 && start < viewEnd - 1)
            {
                if (first < 0)
                {
                    first = i;
                }

                last = i;
            }
        }

        return first < 0 ? (0, Math.Max(0, ItemCount - 1)) : (first, last);
    }

    private void OnOrientationChanged()
    {
        if (ItemsPanelRoot is StackPanel sp)
        {
            sp.Orientation = Orientation;
        }

        foreach (var s in Sections)
        {
            s.Orientation = Orientation;
        }

        UpdatePseudoClasses();
    }

    private void OnSectionHeaderInvoked(object? sender, RoutedEventArgs e)
    {
        if (e.Source is HubSection section)
        {
            e.Handled = true;
            RaiseEvent(new HubHeaderInvokedEventArgs(HeaderInvokedEvent, IndexFromContainer(section), section));
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
    }
}
