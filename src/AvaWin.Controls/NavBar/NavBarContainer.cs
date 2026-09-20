using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;

namespace AvaWin.Controls;

/// <summary>
/// A paged grid (horizontal) or a list (vertical) of <see cref="NavBarCommand"/>s, with page indicators and
/// hover arrows. The items can be commands, or data shown with <see cref="ItemsControl.ItemTemplate"/>.
/// </summary>
[TemplatePart("PART_PageIndicators", typeof(StackPanel))]
[TemplatePart("PART_Viewport", typeof(ScrollViewer), IsRequired = true)]
[TemplatePart("PART_LeftArrow", typeof(Button))]
[TemplatePart("PART_RightArrow", typeof(Button))]
[PseudoClasses(":horizontal", ":vertical", ":hasprevious", ":hasnext", ":paged")]
public partial class NavBarContainer : ItemsControl
{
    /// <summary>Defines the <see cref="Layout"/> property.</summary>
    public static readonly StyledProperty<Orientation> LayoutProperty = AvaloniaProperty.Register<NavBarContainer, Orientation>(nameof(Layout));

    /// <summary>Defines the <see cref="MaxRows"/> property.</summary>
    public static readonly StyledProperty<int> MaxRowsProperty = AvaloniaProperty.Register<NavBarContainer, int>(nameof(MaxRows), 1, coerce: (_, v) => Math.Max(1, v));

    /// <summary>Defines the <see cref="FixedSize"/> property.</summary>
    public static readonly StyledProperty<bool> FixedSizeProperty = AvaloniaProperty.Register<NavBarContainer, bool>(nameof(FixedSize));

    /// <summary>Defines the <see cref="CurrentIndex"/> property.</summary>
    public static readonly StyledProperty<int> CurrentIndexProperty = AvaloniaProperty.Register<NavBarContainer, int>(nameof(CurrentIndex), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="CurrentPage"/> property.</summary>
    public static readonly DirectProperty<NavBarContainer, int> CurrentPageProperty = AvaloniaProperty.RegisterDirect<NavBarContainer, int>(nameof(CurrentPage), o => o.CurrentPage);

    /// <summary>Defines the <see cref="PageCount"/> property.</summary>
    public static readonly DirectProperty<NavBarContainer, int> PageCountProperty = AvaloniaProperty.RegisterDirect<NavBarContainer, int>(nameof(PageCount), o => o.PageCount);

    /// <summary>Defines the <see cref="Invoked"/> event.</summary>
    public static readonly RoutedEvent<NavBarInvokedEventArgs> InvokedEvent = RoutedEvent.Register<NavBarContainer, NavBarInvokedEventArgs>(nameof(Invoked), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="SplitToggle"/> event.</summary>
    public static readonly RoutedEvent<NavBarSplitToggleEventArgs> SplitToggleEvent = RoutedEvent.Register<NavBarContainer, NavBarSplitToggleEventArgs>(nameof(SplitToggle), RoutingStrategies.Bubble);

    private static readonly FuncTemplate<Panel?> DefaultPanel = new(() => new NavBarSurfacePanel());

    private ScrollViewer? _viewport;
    private StackPanel? _indicators;
    private Button? _left;
    private Button? _right;
    private NavBarSurfacePanel? _surface;
    private int _currentPage;
    private int _pageCount = 1;
    private bool _syncingIndex;

    static NavBarContainer()
    {
        ItemsPanelProperty.OverrideDefaultValue<NavBarContainer>(DefaultPanel);
        LayoutProperty.Changed.AddClassHandler<NavBarContainer>((c, _) => c.UpdatePseudoClasses());
        CurrentIndexProperty.Changed.AddClassHandler<NavBarContainer>((c, e) => c.OnCurrentIndexChanged((int)e.NewValue!));
        FocusableProperty.OverrideDefaultValue<NavBarContainer>(false);
    }

    /// <summary>Initializes a new instance.</summary>
    public NavBarContainer()
    {
        AddHandler(NavBarCommand.ClickEvent, OnCommandClick);
        AddHandler(NavBarCommand.SplitToggleEvent, OnCommandSplitToggle);
        InitializeWheelPaging();
        UpdatePseudoClasses();
    }

    /// <summary>Horizontal (a paged grid, default) or vertical (a list).</summary>
    public Orientation Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>The number of rows per page in the horizontal layout.</summary>
    public int MaxRows { get => GetValue(MaxRowsProperty); set => SetValue(MaxRowsProperty, value); }

    /// <summary>Whether horizontal commands keep their 210px width instead of stretching to fill the page.</summary>
    public bool FixedSize { get => GetValue(FixedSizeProperty); set => SetValue(FixedSizeProperty, value); }

    /// <summary>The index of the command the container is scrolled to. Setting it scrolls to that page.</summary>
    public int CurrentIndex { get => GetValue(CurrentIndexProperty); set => SetValue(CurrentIndexProperty, value); }

    /// <summary>The visible page (horizontal).</summary>
    public int CurrentPage { get => _currentPage; private set => SetAndRaise(CurrentPageProperty, ref _currentPage, value); }

    /// <summary>The number of pages (horizontal).</summary>
    public int PageCount { get => _pageCount; private set => SetAndRaise(PageCountProperty, ref _pageCount, value); }

    /// <summary>Raised when the main button of a command is clicked.</summary>
    public event EventHandler<NavBarInvokedEventArgs> Invoked { add => AddHandler(InvokedEvent, value); remove => RemoveHandler(InvokedEvent, value); }

    /// <summary>Raised when the split button of a command toggles.</summary>
    public event EventHandler<NavBarSplitToggleEventArgs> SplitToggle { add => AddHandler(SplitToggleEvent, value); remove => RemoveHandler(SplitToggleEvent, value); }

    /// <summary>Scrolls to <paramref name="page"/> (horizontal).</summary>
    public void GoToPage(int page)
    {
        if (_viewport is null || _surface is null)
        {
            return;
        }

        page = Math.Clamp(page, 0, Math.Max(0, _surface.PageCount - 1));
        _viewport.Offset = new Vector(page * _surface.PageWidth, _viewport.Offset.Y);
        UpdatePage();
    }

    /// <summary>Scrolls to the previous page.</summary>
    public void PreviousPage() => AnimateToPage(CurrentPage - 1);

    /// <summary>Scrolls to the next page.</summary>
    public void NextPage() => AnimateToPage(CurrentPage + 1);

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is NavBarCommand)
        {
            recycleKey = null;
            return false;
        }

        recycleKey = DefaultRecycleKey;
        return true;
    }

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new NavBarCommand();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is NavBarCommand cmd && !ReferenceEquals(cmd, item))
        {
            cmd.Content = item;
            cmd.ContentTemplate = ItemTemplate;
            if (ItemTemplate is null)
            {
                cmd.Content = null;
                cmd.Label = item?.ToString();
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Unhook();
        _viewport = e.NameScope.Find<ScrollViewer>("PART_Viewport");
        _indicators = e.NameScope.Find<StackPanel>("PART_PageIndicators");
        _left = e.NameScope.Find<Button>("PART_LeftArrow");
        _right = e.NameScope.Find<Button>("PART_RightArrow");
        if (_viewport is { } vp)
        {
            vp.ScrollChanged += OnScrollChanged;
            vp.SizeChanged += OnViewportSizeChanged;
        }

        if (_left is { } l)
        {
            l.Click += OnLeftClick;
        }

        if (_right is { } r)
        {
            r.Click += OnRightClick;
        }

        Dispatcher.UIThread.Post(HookSurface, DispatcherPriority.Loaded);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LayoutProperty || change.Property == MaxRowsProperty || change.Property == FixedSizeProperty)
        {
            SyncSurface();
        }
    }

    private void Unhook()
    {
        if (_viewport is { } vp)
        {
            vp.ScrollChanged -= OnScrollChanged;
            vp.SizeChanged -= OnViewportSizeChanged;
        }

        if (_left is { } l)
        {
            l.Click -= OnLeftClick;
        }

        if (_right is { } r)
        {
            r.Click -= OnRightClick;
        }

        if (_surface is { } s)
        {
            s.PagesChanged -= OnPagesChanged;
        }

        _surface = null;
    }

    private void HookSurface()
    {
        if (_surface is not null)
        {
            return;
        }

        if (ItemsPanelRoot is NavBarSurfacePanel surface)
        {
            _surface = surface;
            surface.PagesChanged += OnPagesChanged;
            SyncSurface();
            RebuildIndicators();
        }
    }

    private void SyncSurface()
    {
        if (_surface is null)
        {
            return;
        }

        _surface.Orientation = Layout;
        _surface.MaxRows = MaxRows;
        _surface.FixedSize = FixedSize;
        if (_viewport is { } vp)
        {
            _surface.ViewportWidth = vp.Viewport.Width > 0 ? vp.Viewport.Width : vp.Bounds.Width;
        }
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SyncSurface();
        Dispatcher.UIThread.Post(() => GoToPage(CurrentPage), DispatcherPriority.Loaded);
    }

    private void OnPagesChanged(object? sender, EventArgs e)
    {
        PageCount = _surface?.PageCount ?? 1;
        RebuildIndicators();
        UpdatePage();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e) => UpdatePage();

    private void OnLeftClick(object? sender, RoutedEventArgs e) => PreviousPage();

    private void OnRightClick(object? sender, RoutedEventArgs e) => NextPage();

    private void OnCurrentIndexChanged(int index)
    {
        if (_syncingIndex || _surface is null)
        {
            return;
        }

        GoToPage(_surface.PageOf(index));
    }

    private void UpdatePage()
    {
        if (_viewport is null || _surface is null)
        {
            return;
        }

        var page = _surface.PageWidth > 0 ? (int)Math.Round(_viewport.Offset.X / _surface.PageWidth) : 0;
        page = Math.Clamp(page, 0, Math.Max(0, _surface.PageCount - 1));
        if (page != CurrentPage)
        {
            CurrentPage = page;
            _syncingIndex = true;
            try
            {
                SetCurrentValue(CurrentIndexProperty, page * _surface.ItemsPerPage);
            }
            finally
            {
                _syncingIndex = false;
            }
        }

        UpdatePseudoClasses();
        if (_indicators is { } panel)
        {
            for (var i = 0; i < panel.Children.Count; i++)
            {
                panel.Children[i].Classes.Set("current", i == page);
            }
        }
    }

    private void RebuildIndicators()
    {
        if (_indicators is null)
        {
            return;
        }

        var pages = Layout == Orientation.Horizontal ? PageCount : 1;
        while (_indicators.Children.Count > pages)
        {
            _indicators.Children.RemoveAt(_indicators.Children.Count - 1);
        }

        while (_indicators.Children.Count < pages)
        {
            var index = _indicators.Children.Count;
            var dot = new Button { Classes = { "win-navbar-pageindicator" }, Focusable = false };
            dot.Click += (_, _) => AnimateToPage(index);
            _indicators.Children.Add(dot);
        }

        for (var i = 0; i < _indicators.Children.Count; i++)
        {
            _indicators.Children[i].Classes.Set("current", i == CurrentPage);
        }

        UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses()
    {
        var horizontal = Layout == Orientation.Horizontal;
        PseudoClasses.Set(":horizontal", horizontal);
        PseudoClasses.Set(":vertical", !horizontal);
        var pages = _surface?.PageCount ?? 1;
        PseudoClasses.Set(":paged", horizontal && pages > 1);
        PseudoClasses.Set(":hasprevious", horizontal && CurrentPage > 0);
        PseudoClasses.Set(":hasnext", horizontal && CurrentPage < pages - 1);
    }

    private void OnCommandClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not NavBarCommand cmd)
        {
            return;
        }

        var index = IndexFromContainer(cmd);
        var data = cmd.Content ?? (index >= 0 ? ItemsView[index] : cmd);
        RaiseEvent(new NavBarInvokedEventArgs(InvokedEvent, index, cmd, data));
    }

    private void OnCommandSplitToggle(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not NavBarCommand cmd)
        {
            return;
        }

        var index = IndexFromContainer(cmd);
        var data = cmd.Content ?? (index >= 0 ? ItemsView[index] : cmd);
        RaiseEvent(new NavBarSplitToggleEventArgs(SplitToggleEvent, index, cmd, data, cmd.SplitOpened));
    }
}
