using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaWin.Animations;

namespace AvaWin.Controls;

/// <summary>
/// A virtualized list or grid of <see cref="ListViewItem"/>s: selection chrome, tap behaviors, keyboard
/// navigation, groups with headers, incremental loading and an entrance animation.
/// </summary>
[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer), IsRequired = true)]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter), IsRequired = true)]
[TemplatePart("PART_Progress", typeof(ProgressBar))]
[PseudoClasses(":horizontal", ":vertical", ":grouped", ":loading", ":selectionmode", ":filled")]
public partial class ListView : SelectingItemsControl
{
    /// <summary>Defines the <see cref="Layout"/> property.</summary>
    public static readonly StyledProperty<ListViewLayout> LayoutProperty = AvaloniaProperty.Register<ListView, ListViewLayout>(nameof(Layout));

    /// <summary>Defines the <see cref="GroupsSource"/> property.</summary>
    public static readonly StyledProperty<IEnumerable?> GroupsSourceProperty = AvaloniaProperty.Register<ListView, IEnumerable?>(nameof(GroupsSource));

    /// <summary>Defines the <see cref="GroupKeySelector"/> property.</summary>
    public static readonly StyledProperty<Func<object?, object?>?> GroupKeySelectorProperty = AvaloniaProperty.Register<ListView, Func<object?, object?>?>(nameof(GroupKeySelector));

    /// <summary>Defines the <see cref="GroupsKeySelector"/> property.</summary>
    public static readonly StyledProperty<Func<object?, object?>?> GroupsKeySelectorProperty = AvaloniaProperty.Register<ListView, Func<object?, object?>?>(nameof(GroupsKeySelector));

    /// <summary>Defines the <see cref="GroupHeaderTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> GroupHeaderTemplateProperty = AvaloniaProperty.Register<ListView, IDataTemplate?>(nameof(GroupHeaderTemplate));

    /// <summary>Defines the <see cref="SelectionMode"/> property. It hides the flags enum of the base class.</summary>
    public static new readonly StyledProperty<ListSelectionMode> SelectionModeProperty = AvaloniaProperty.Register<ListView, ListSelectionMode>(nameof(SelectionMode), ListSelectionMode.Multi);

    /// <summary>Defines the <see cref="TapBehavior"/> property.</summary>
    public static readonly StyledProperty<TapBehavior> TapBehaviorProperty = AvaloniaProperty.Register<ListView, TapBehavior>(nameof(TapBehavior), TapBehavior.InvokeOnly);

    /// <summary>Defines the <see cref="SwipeBehavior"/> property.</summary>
    public static readonly StyledProperty<SwipeBehavior> SwipeBehaviorProperty = AvaloniaProperty.Register<ListView, SwipeBehavior>(nameof(SwipeBehavior), SwipeBehavior.None);

    /// <summary>Defines the <see cref="PressFeedback"/> property.</summary>
    public static readonly AttachedProperty<PressFeedback> PressFeedbackProperty = ListViewItem.PressFeedbackProperty.AddOwner<ListView>();

    /// <summary>Defines the <see cref="GroupHeaderTapBehavior"/> property.</summary>
    public static readonly StyledProperty<GroupHeaderTapBehavior> GroupHeaderTapBehaviorProperty = AvaloniaProperty.Register<ListView, GroupHeaderTapBehavior>(nameof(GroupHeaderTapBehavior), GroupHeaderTapBehavior.Invoke);

    /// <summary>Defines the <see cref="SelectionStyle"/> property.</summary>
    public static readonly StyledProperty<ListViewSelectionStyle> SelectionStyleProperty = AvaloniaProperty.Register<ListView, ListViewSelectionStyle>(nameof(SelectionStyle), ListViewSelectionStyle.Bordered);

    /// <summary>Defines the <see cref="CurrentIndex"/> property.</summary>
    public static readonly StyledProperty<int> CurrentIndexProperty = AvaloniaProperty.Register<ListView, int>(nameof(CurrentIndex), -1, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="ItemsDraggable"/> property.</summary>
    public static readonly StyledProperty<bool> ItemsDraggableProperty = AvaloniaProperty.Register<ListView, bool>(nameof(ItemsDraggable));

    /// <summary>Defines the <see cref="ItemsReorderable"/> property.</summary>
    public static readonly StyledProperty<bool> ItemsReorderableProperty = AvaloniaProperty.Register<ListView, bool>(nameof(ItemsReorderable));

    /// <summary>Defines the <see cref="IsSelectionModeActive"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectionModeActiveProperty = AvaloniaProperty.Register<ListView, bool>(nameof(IsSelectionModeActive));

    /// <summary>Defines the <see cref="PagesToLoad"/> property.</summary>
    public static readonly StyledProperty<int> PagesToLoadProperty = AvaloniaProperty.Register<ListView, int>(nameof(PagesToLoad), 5);

    /// <summary>Defines the <see cref="PagesToLoadThreshold"/> property.</summary>
    public static readonly StyledProperty<int> PagesToLoadThresholdProperty = AvaloniaProperty.Register<ListView, int>(nameof(PagesToLoadThreshold), 2);

    /// <summary>Defines the <see cref="AutomaticallyLoadPages"/> property.</summary>
    public static readonly StyledProperty<bool> AutomaticallyLoadPagesProperty = AvaloniaProperty.Register<ListView, bool>(nameof(AutomaticallyLoadPages), true);

    /// <summary>Defines the <see cref="LoadingState"/> property.</summary>
    public static readonly DirectProperty<ListView, ListViewLoadingState> LoadingStateProperty = AvaloniaProperty.RegisterDirect<ListView, ListViewLoadingState>(nameof(LoadingState), o => o.LoadingState);

    /// <summary>Defines the <see cref="ItemInvoked"/> event.</summary>
    public static readonly RoutedEvent<ListViewItemInvokedEventArgs> ItemInvokedEvent = RoutedEvent.Register<ListView, ListViewItemInvokedEventArgs>(nameof(ItemInvoked), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="GroupHeaderInvoked"/> event.</summary>
    public static readonly RoutedEvent<ListViewGroupHeaderInvokedEventArgs> GroupHeaderInvokedEvent = RoutedEvent.Register<ListView, ListViewGroupHeaderInvokedEventArgs>(nameof(GroupHeaderInvoked), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="SelectionChanging"/> event.</summary>
    public static readonly RoutedEvent<ListViewSelectionChangingEventArgs> SelectionChangingEvent = RoutedEvent.Register<ListView, ListViewSelectionChangingEventArgs>(nameof(SelectionChanging), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="LoadingStateChanged"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> LoadingStateChangedEvent = RoutedEvent.Register<ListView, RoutedEventArgs>(nameof(LoadingStateChanged), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ContentAnimating"/> event.</summary>
    public static readonly RoutedEvent<ListViewContentAnimatingEventArgs> ContentAnimatingEvent = RoutedEvent.Register<ListView, ListViewContentAnimatingEventArgs>(nameof(ContentAnimating), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="KeyboardNavigating"/> event.</summary>
    public static readonly RoutedEvent<ListViewKeyboardNavigatingEventArgs> KeyboardNavigatingEvent = RoutedEvent.Register<ListView, ListViewKeyboardNavigatingEventArgs>(nameof(KeyboardNavigating), RoutingStrategies.Bubble);

    private static readonly FuncTemplate<Panel?> DefaultPanel = new(() => new ListViewPanel());

    private ScrollViewer? _scroller;
    private ListViewPanel? _panel;
    private ListViewLoadingState _loadingState = ListViewLoadingState.ItemsLoading;
    private List<ListViewGroupInfo>? _groups;
    private bool _itemsHooked;
    private bool _entranceDone;
    private bool _loadingMore;
    private int _anchorIndex = -1;
    private ListViewItem? _pressedItem;
    private ListViewLayout? _hookedLayout;

    static ListView()
    {
        ItemsPanelProperty.OverrideDefaultValue<ListView>(DefaultPanel);
        SelectingItemsControl.SelectionModeProperty.OverrideDefaultValue<ListView>(Avalonia.Controls.SelectionMode.Multiple | Avalonia.Controls.SelectionMode.Toggle);
        FocusableProperty.OverrideDefaultValue<ListView>(false);
        AutoScrollToSelectedItemProperty.OverrideDefaultValue<ListView>(false);
        LayoutProperty.Changed.AddClassHandler<ListView>((l, e) => l.OnLayoutChanged(e.OldValue as ListViewLayout));
        SelectionModeProperty.Changed.AddClassHandler<ListView>((l, _) => l.ApplySelectionMode());
        SelectionStyleProperty.Changed.AddClassHandler<ListView>((l, _) => l.ApplySelectionStyle());
        IsSelectionModeActiveProperty.Changed.AddClassHandler<ListView>((l, e) => l.ApplySelectionModeActive((bool)e.NewValue!));
        GroupKeySelectorProperty.Changed.AddClassHandler<ListView>((l, _) => l.RebuildGroups());
        GroupsSourceProperty.Changed.AddClassHandler<ListView>((l, _) => l.RebuildGroups());
        GroupsKeySelectorProperty.Changed.AddClassHandler<ListView>((l, _) => l.RebuildGroups());
        CurrentIndexProperty.Changed.AddClassHandler<ListView>((l, e) => l.OnCurrentIndexChanged((int)e.NewValue!));
    }

    /// <summary>Initializes a new instance.</summary>
    public ListView()
    {
        // A current value, not a local one, so a style setter can still replace the layout.
        SetCurrentValue(LayoutProperty, new ListLayout());
        AddHandler(ListViewGroupHeader.InvokedEvent, OnGroupHeaderInvoked);
        AddHandler(GotFocusEvent, OnItemGotFocus);
        // Tunnel, because the ScrollViewer of the template would otherwise take PageUp, PageDown, Home and End first.
        AddHandler(KeyDownEvent, OnListKeyDown, RoutingStrategies.Tunnel);
        InitializeDrag();
        InitializeSwipe();
        ApplySelectionMode();
        ApplySelectionStyle();
        UpdatePseudoClasses();
    }

    /// <summary>How the items are arranged: <see cref="ListLayout"/> (default), <see cref="GridLayout"/> or <see cref="CellSpanningLayout"/>.</summary>
    public ListViewLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>The group objects. Each is matched to its items by key, through <see cref="GroupsKeySelector"/>.</summary>
    public IEnumerable? GroupsSource { get => GetValue(GroupsSourceProperty); set => SetValue(GroupsSourceProperty, value); }

    /// <summary>Returns the group key of an item. Consecutive items with equal keys form a group. Null means no groups.</summary>
    public Func<object?, object?>? GroupKeySelector { get => GetValue(GroupKeySelectorProperty); set => SetValue(GroupKeySelectorProperty, value); }

    /// <summary>Returns the key of a <see cref="GroupsSource"/> element. By default the element is its own key.</summary>
    public Func<object?, object?>? GroupsKeySelector { get => GetValue(GroupsKeySelectorProperty); set => SetValue(GroupsKeySelectorProperty, value); }

    /// <summary>The template for a group header. Its data is the group object from <see cref="GroupsSource"/>, or the key.</summary>
    public IDataTemplate? GroupHeaderTemplate { get => GetValue(GroupHeaderTemplateProperty); set => SetValue(GroupHeaderTemplateProperty, value); }

    /// <summary>None, Single or Multi (default).</summary>
    public new ListSelectionMode SelectionMode { get => GetValue(SelectionModeProperty); set => SetValue(SelectionModeProperty, value); }

    /// <summary>What a tap on an item does: invoke only (default), invoke and select, or invoke and toggle.</summary>
    public TapBehavior TapBehavior { get => GetValue(TapBehaviorProperty); set => SetValue(TapBehaviorProperty, value); }

    /// <summary>
    /// What a touch or pen swipe across the scroll axis does. With <see cref="SwipeBehavior.Select"/>, past
    /// <see cref="SwipeSelectThreshold"/> a release toggles the selection, and past <see cref="SwipeDragThreshold"/>
    /// the item follows the contact and a release reorders it within its group, when <see cref="ItemsReorderable"/> is
    /// set. The mouse is not affected. Default <see cref="SwipeBehavior.None"/>.
    /// </summary>
    public SwipeBehavior SwipeBehavior { get => GetValue(SwipeBehaviorProperty); set => SetValue(SwipeBehaviorProperty, value); }

    /// <summary>How the items answer a press: scale (default), tilt toward the contact, or nothing. The items inherit it.</summary>
    public PressFeedback PressFeedback { get => GetValue(PressFeedbackProperty); set => SetValue(PressFeedbackProperty, value); }

    /// <summary>Whether a tap on a group header raises <see cref="GroupHeaderInvoked"/> or does nothing.</summary>
    public GroupHeaderTapBehavior GroupHeaderTapBehavior { get => GetValue(GroupHeaderTapBehaviorProperty); set => SetValue(GroupHeaderTapBehaviorProperty, value); }

    /// <summary>The selection chrome: an accent border with a corner checkmark (default), or an accent fill.</summary>
    public ListViewSelectionStyle SelectionStyle { get => GetValue(SelectionStyleProperty); set => SetValue(SelectionStyleProperty, value); }

    /// <summary>The index of the focused item. Setting it moves focus and scrolls to the item.</summary>
    public int CurrentIndex { get => GetValue(CurrentIndexProperty); set => SetValue(CurrentIndexProperty, value); }

    /// <summary>The data of the focused item.</summary>
    public object? CurrentItem
    {
        get => CurrentIndex >= 0 && CurrentIndex < ItemCount ? ItemsView[CurrentIndex] : null;
        set => CurrentIndex = value is null ? -1 : ItemsView.IndexOf(value);
    }

    /// <summary>Whether items can be dragged out of the list, as <see cref="DragFormat"/> and text.</summary>
    public bool ItemsDraggable { get => GetValue(ItemsDraggableProperty); set => SetValue(ItemsDraggableProperty, value); }

    /// <summary>Whether a drop of the list's own items reorders the source list.</summary>
    public bool ItemsReorderable { get => GetValue(ItemsReorderableProperty); set => SetValue(ItemsReorderableProperty, value); }

    /// <summary>The phone selection mode. A vertical list shows a check box on each item. Other layouts show an inner accent border.</summary>
    public bool IsSelectionModeActive { get => GetValue(IsSelectionModeActiveProperty); set => SetValue(IsSelectionModeActiveProperty, value); }

    /// <summary>How many pages <see cref="LoadMorePages"/> asks an <see cref="ISupportIncrementalLoading"/> source for. A page is one viewport of items.</summary>
    public int PagesToLoad { get => GetValue(PagesToLoadProperty); set => SetValue(PagesToLoadProperty, value); }

    /// <summary>How many pages before the end of the items the list starts to load more.</summary>
    public int PagesToLoadThreshold { get => GetValue(PagesToLoadThresholdProperty); set => SetValue(PagesToLoadThresholdProperty, value); }

    /// <summary>Whether the list loads more pages on its own as the user scrolls near the end.</summary>
    public bool AutomaticallyLoadPages { get => GetValue(AutomaticallyLoadPagesProperty); set => SetValue(AutomaticallyLoadPagesProperty, value); }

    /// <summary>The index of the first item in the viewport.</summary>
    public int IndexOfFirstVisible => VisibleRange().first;

    /// <summary>The index of the last item in the viewport.</summary>
    public int IndexOfLastVisible => VisibleRange().last;

    /// <summary>The scroll offset along the orientation of the layout.</summary>
    public double ScrollPosition
    {
        get => _scroller is null ? 0 : (Orientation == Orientation.Vertical ? _scroller.Offset.Y : _scroller.Offset.X);
        set
        {
            if (_scroller is { } s)
            {
                s.Offset = Orientation == Orientation.Vertical ? new Vector(s.Offset.X, value) : new Vector(value, s.Offset.Y);
            }
        }
    }

    /// <summary>How far the list has loaded: the viewport, the items, or everything.</summary>
    public ListViewLoadingState LoadingState
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

    /// <summary>The selection model: indexes, Select, Deselect, SelectAll and Clear.</summary>
    public new ISelectionModel Selection { get => base.Selection; set => base.Selection = value; }

    /// <summary>The data of the selected items.</summary>
    public new IList? SelectedItems => base.SelectedItems;

    /// <summary>The computed groups, or null when the list has no groups.</summary>
    public IReadOnlyList<ListViewGroupInfo>? Groups => _groups;

    /// <summary>The scroll orientation of the layout.</summary>
    public Orientation Orientation => Layout?.Orientation ?? Orientation.Vertical;

    /// <summary>Raised when the user taps an item, presses Enter on it, or clicks it.</summary>
    public event EventHandler<ListViewItemInvokedEventArgs> ItemInvoked { add => AddHandler(ItemInvokedEvent, value); remove => RemoveHandler(ItemInvokedEvent, value); }

    /// <summary>Raised when the user taps a group header.</summary>
    public event EventHandler<ListViewGroupHeaderInvokedEventArgs> GroupHeaderInvoked { add => AddHandler(GroupHeaderInvokedEvent, value); remove => RemoveHandler(GroupHeaderInvokedEvent, value); }

    /// <summary>Raised before the user changes the selection. Cancelable. Changes made from code do not raise it.</summary>
    public event EventHandler<ListViewSelectionChangingEventArgs> SelectionChanging { add => AddHandler(SelectionChangingEvent, value); remove => RemoveHandler(SelectionChangingEvent, value); }

    /// <summary>Raised when <see cref="LoadingState"/> changes.</summary>
    public event EventHandler<RoutedEventArgs> LoadingStateChanged { add => AddHandler(LoadingStateChangedEvent, value); remove => RemoveHandler(LoadingStateChangedEvent, value); }

    /// <summary>Raised before the entrance or a content transition animation. Cancel it to skip the animation.</summary>
    public event EventHandler<ListViewContentAnimatingEventArgs> ContentAnimating { add => AddHandler(ContentAnimatingEvent, value); remove => RemoveHandler(ContentAnimatingEvent, value); }

    /// <summary>Raised when a key moves the focused item. Cancelable.</summary>
    public event EventHandler<ListViewKeyboardNavigatingEventArgs> KeyboardNavigating { add => AddHandler(KeyboardNavigatingEvent, value); remove => RemoveHandler(KeyboardNavigatingEvent, value); }

    /// <summary>Scrolls the item at <paramref name="index"/> into the viewport.</summary>
    public void EnsureVisible(int index)
    {
        if (index >= 0 && index < ItemCount)
        {
            _panel?.BringIndexIntoView(index);
        }
    }

    /// <summary>Scrolls an item or a group header into the viewport.</summary>
    public void EnsureVisible(ListViewEntity entity)
    {
        if (entity.Type == ListViewEntityType.Item)
        {
            EnsureVisible(entity.Index);
        }
        else if (_groups is { } groups && entity.Index >= 0 && entity.Index < groups.Count)
        {
            EnsureVisible(groups[entity.Index].Start);
        }
    }

    /// <summary>The realized container of an item, or null when it is not realized.</summary>
    public Control? ElementFromIndex(int index) => ContainerFromIndex(index);

    /// <summary>The index of the item that contains <paramref name="element"/>, or -1.</summary>
    public int IndexOfElement(Control element)
    {
        var container = element as ListViewItem ?? element.FindAncestorOfType<ListViewItem>();
        return container is null ? -1 : IndexFromContainer(container);
    }

    /// <summary>Applies the item template to the container of the item again.</summary>
    public void ResetItem(int index)
    {
        if (ContainerFromIndex(index) is ListViewItem item)
        {
            item.ContentTemplate = ItemTemplate;
            item.Content = ItemsView[index];
        }
    }

    /// <summary>Applies the group header template to the header of the group again.</summary>
    public void ResetGroupHeader(int groupIndex)
    {
        if (_panel is { } p && p.RealizedHeaders.TryGetValue(groupIndex, out var h) && h is ListViewGroupHeader header && _groups is { } groups && groupIndex < groups.Count)
        {
            header.Bind(groups[groupIndex], groupIndex);
        }
    }

    /// <summary>Measures the uniform cell again and does a new layout.</summary>
    public void RecalculateItemPosition() => _panel?.Recalculate();

    /// <summary>Asks an <see cref="ISupportIncrementalLoading"/> source for <see cref="PagesToLoad"/> pages.</summary>
    public Task<int> LoadMorePages()
    {
        if (ItemsSource is not ISupportIncrementalLoading src || !src.HasMoreItems || _loadingMore)
        {
            return Task.FromResult(0);
        }

        return LoadMoreCore(src);
    }

    /// <summary>The realized item containers.</summary>
    public IEnumerable<ListViewItem> RealizedItems => GetRealizedContainers().OfType<ListViewItem>();

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) => NeedsContainer<ListViewItem>(item, out recycleKey);

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new ListViewItem();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is ListViewItem lvi)
        {
            lvi.SetSelectionModeActive(IsSelectionModeActive);
            if (!_entranceDone)
            {
                lvi.Opacity = 0;
            }
            else
            {
                lvi.ClearValue(OpacityProperty);
            }
        }
    }

    /// <inheritdoc/>
    protected override void ClearContainerForItemOverride(Control container)
    {
        // The template is cleared before the content, so a typed FuncDataTemplate never has to build null.
        if (container is ListViewItem lvi)
        {
            lvi.ClearValue(ContentControl.ContentTemplateProperty);
            lvi.ClearValue(ContentControl.ContentProperty);
            return;
        }

        base.ClearContainerForItemOverride(container);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_scroller is { } old)
        {
            old.ScrollChanged -= OnScrollChanged;
        }

        _scroller = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        if (_scroller is { } s)
        {
            s.ScrollChanged += OnScrollChanged;
        }

        Dispatcher.UIThread.Post(HookPanel, DispatcherPriority.Loaded);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (!_itemsHooked)
        {
            _itemsHooked = true;
            ItemsView.CollectionChanged += OnItemsCollectionChanged;
        }

        RebuildGroups();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LoadingState = ListViewLoadingState.ItemsLoading;
        Dispatcher.UIThread.Post(() => _ = FinishLoadAsync(), DispatcherPriority.Loaded);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pressedItem = ContainerOf(e.Source);
        BeginDragTracking(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        TrackDrag(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        CancelDragTracking();
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        CancelDragTracking();
        var item = ContainerOf(e.Source);
        var pressed = _pressedItem;
        _pressedItem = null;
        if (item is null || !ReferenceEquals(item, pressed) || e.Handled)
        {
            return;
        }

        var index = IndexFromContainer(item);
        if (index < 0)
        {
            return;
        }

        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            // A right-click toggles the selection in Multi mode, whatever the tap behavior.
            if (SelectionMode == ListSelectionMode.Multi && item.IsSelectable)
            {
                ToggleSelection(index);
                e.Handled = true;
            }

            return;
        }

        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        e.Handled = true;
        item.Focus();
        var mods = e.KeyModifiers;
        switch (TapBehavior)
        {
            case TapBehavior.DirectSelect:
                DirectSelect(index, mods, item);
                Invoke(index, item);
                break;
            case TapBehavior.ToggleSelect:
                if (item.IsSelectable)
                {
                    ToggleSelection(index);
                }

                Invoke(index, item);
                break;
            case TapBehavior.InvokeOnly:
                Invoke(index, item);
                break;
        }
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        var item = ContainerOf(e.Source);
        var focused = item is null ? CurrentIndex : IndexFromContainer(item);
        if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (SelectionMode == ListSelectionMode.Multi && ItemCount > 0)
            {
                ApplySelection(Enumerable.Range(0, ItemCount).ToList());
                e.Handled = true;
            }

            return;
        }

        if (item is null)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                Invoke(focused, item);
                e.Handled = true;
                return;
            case Key.Space:
                if (item.IsSelectable && SelectionMode != ListSelectionMode.None)
                {
                    if (SelectionMode == ListSelectionMode.Multi && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        SelectRangeTo(focused);
                    }
                    else if (SelectionMode == ListSelectionMode.Multi)
                    {
                        ToggleSelection(focused);
                    }
                    else
                    {
                        ApplySelection([focused]);
                    }
                }

                e.Handled = true;
                return;
        }

        var direction = e.Key switch
        {
            Key.Up => NavigationDirection.Up,
            Key.Down => NavigationDirection.Down,
            Key.Left => NavigationDirection.Left,
            Key.Right => NavigationDirection.Right,
            Key.Home => NavigationDirection.First,
            Key.End => NavigationDirection.Last,
            Key.PageUp => NavigationDirection.PageUp,
            Key.PageDown => NavigationDirection.PageDown,
            _ => (NavigationDirection?)null,
        };
        if (direction is null || _panel is null)
        {
            return;
        }

        var next = _panel.NextIndex(focused, direction.Value);
        if (next < 0 || next == focused)
        {
            e.Handled = true;
            return;
        }

        RaiseEvent(new ListViewKeyboardNavigatingEventArgs(KeyboardNavigatingEvent, focused, next));
        FocusIndex(next);
        if (TapBehavior == TapBehavior.DirectSelect && SelectionMode != ListSelectionMode.None)
        {
            if (SelectionMode == ListSelectionMode.Multi && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                SelectRangeTo(next);
            }
            else if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                _anchorIndex = next;
                ApplySelection([next]);
            }
        }

        e.Handled = true;
    }

    private void HookPanel()
    {
        if (ItemsPanelRoot is ListViewPanel panel && !ReferenceEquals(panel, _panel))
        {
            _panel = panel;
        }

        ConfigurePanel();
    }

    private void ConfigurePanel()
    {
        if (_panel is null)
        {
            return;
        }

        var layout = Layout ?? new ListLayout();
        _panel.Orientation = layout.Orientation;
        _panel.MaxItemsPerLine = layout.MaxItemsPerLine;
        _panel.StretchCrossAxis = layout.StretchCrossAxis;
        _panel.HeaderPosition = layout.GroupHeaderPosition;
        var (groupInfo, spanning) = layout switch
        {
            CellSpanningLayout cs => (cs.GroupInfo, true),
            GridLayout g => (g.GroupInfo, g.IsCellSpanning),
            _ => (null, false),
        };
        _panel.Configure(_groups, CreateGroupHeader, layout.ItemInfoCallback, groupInfo, spanning);
        Classes.Set("win-grid", layout is not ListLayout);
        Classes.Set("win-listlayout", layout is ListLayout);
        UpdatePseudoClasses();
    }

    private Control CreateGroupHeader(ListViewGroupInfo group)
    {
        var header = new ListViewGroupHeader { ContentTemplate = GroupHeaderTemplate };
        header.Bind(group, _groups?.IndexOf(group) ?? -1);
        return header;
    }

    private void OnLayoutChanged(ListViewLayout? old)
    {
        if (_hookedLayout is { } h)
        {
            h.LayoutChanged -= OnLayoutPropertyChanged;
        }

        _hookedLayout = Layout;
        if (_hookedLayout is { } n)
        {
            n.LayoutChanged += OnLayoutPropertyChanged;
        }

        _ = old;
        ConfigurePanel();
    }

    private void OnLayoutPropertyChanged(object? sender, EventArgs e) => ConfigurePanel();

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildGroups();
        if (CurrentIndex >= ItemCount)
        {
            SetCurrentValue(CurrentIndexProperty, ItemCount - 1);
        }
    }

    private void RebuildGroups()
    {
        var selector = GroupKeySelector;
        if (selector is null)
        {
            _groups = null;
        }
        else
        {
            var groups = new List<ListViewGroupInfo>();
            var lookup = new List<(object? key, object? group)>();
            if (GroupsSource is { } src)
            {
                var keyOf = GroupsKeySelector ?? (g => g);
                foreach (var g in src)
                {
                    lookup.Add((keyOf(g), g));
                }
            }

            object? currentKey = null;
            var start = 0;
            var count = 0;
            var i = 0;
            foreach (var item in ItemsView)
            {
                var key = selector(item);
                if (count == 0 || !Equals(key, currentKey))
                {
                    if (count > 0)
                    {
                        groups.Add(new ListViewGroupInfo(currentKey, HeaderFor(currentKey, lookup), start, count));
                    }

                    currentKey = key;
                    start = i;
                    count = 0;
                }

                count++;
                i++;
            }

            if (count > 0)
            {
                groups.Add(new ListViewGroupInfo(currentKey, HeaderFor(currentKey, lookup), start, count));
            }

            _groups = groups;
        }

        ConfigurePanel();
    }

    private static object? HeaderFor(object? key, List<(object? key, object? group)> lookup)
    {
        foreach (var (k, g) in lookup)
        {
            if (Equals(k, key))
            {
                return g;
            }
        }

        return key;
    }

    private void ApplySelectionMode()
    {
        SetCurrentValue(SelectingItemsControl.SelectionModeProperty, SelectionMode == ListSelectionMode.Multi
            ? Avalonia.Controls.SelectionMode.Multiple | Avalonia.Controls.SelectionMode.Toggle
            : Avalonia.Controls.SelectionMode.Single | Avalonia.Controls.SelectionMode.Toggle);
        if (SelectionMode == ListSelectionMode.None)
        {
            Selection.Clear();
        }
    }

    private void ApplySelectionStyle()
    {
        ListStyle.SetSelectionStyle(this, SelectionStyle == ListViewSelectionStyle.Filled ? AvaWin.SelectionStyle.Filled : AvaWin.SelectionStyle.Bordered);
        PseudoClasses.Set(":filled", SelectionStyle == ListViewSelectionStyle.Filled);
    }

    private void ApplySelectionModeActive(bool active)
    {
        PseudoClasses.Set(":selectionmode", active);
        foreach (var item in RealizedItems)
        {
            item.SetSelectionModeActive(active);
        }
    }

    private void OnCurrentIndexChanged(int index)
    {
        if (index >= 0 && index < ItemCount && !(ContainerFromIndex(index)?.IsKeyboardFocusWithin ?? false))
        {
            FocusIndex(index);
        }
    }

    private void FocusIndex(int index)
    {
        if (_panel is null || index < 0 || index >= ItemCount)
        {
            return;
        }

        var container = _panel.BringIndexIntoView(index);
        container?.Focus(NavigationMethod.Directional);
        SetCurrentValue(CurrentIndexProperty, index);
    }

    private void OnItemGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (ContainerOf(e.Source) is { } item)
        {
            var index = IndexFromContainer(item);
            if (index >= 0)
            {
                SetCurrentValue(CurrentIndexProperty, index);
            }
        }
    }

    private void Invoke(int index, ListViewItem item)
    {
        if (index < 0)
        {
            return;
        }

        RaiseEvent(new ListViewItemInvokedEventArgs(ItemInvokedEvent, index, ItemsView[index], item));
    }

    private void DirectSelect(int index, KeyModifiers mods, ListViewItem item)
    {
        if (SelectionMode == ListSelectionMode.None || !item.IsSelectable)
        {
            return;
        }

        if (SelectionMode == ListSelectionMode.Multi && mods.HasFlag(KeyModifiers.Shift))
        {
            SelectRangeTo(index);
        }
        else if (SelectionMode == ListSelectionMode.Multi && mods.HasFlag(KeyModifiers.Control))
        {
            ToggleSelection(index);
        }
        else
        {
            _anchorIndex = index;
            ApplySelection([index]);
        }
    }

    private void ToggleSelection(int index)
    {
        if (SelectionMode == ListSelectionMode.None)
        {
            return;
        }

        var current = Selection.SelectedIndexes.ToList();
        List<int> next;
        if (current.Contains(index))
        {
            next = current.Where(i => i != index).ToList();
        }
        else if (SelectionMode == ListSelectionMode.Single)
        {
            next = [index];
        }
        else
        {
            next = current.Append(index).ToList();
        }

        _anchorIndex = index;
        ApplySelection(next);
    }

    private void SelectRangeTo(int index)
    {
        var anchor = _anchorIndex >= 0 ? _anchorIndex : (Selection.SelectedIndexes.Count > 0 ? Selection.SelectedIndexes[0] : index);
        var lo = Math.Min(anchor, index);
        var hi = Math.Max(anchor, index);
        ApplySelection(Enumerable.Range(lo, hi - lo + 1).ToList());
    }

    private void ApplySelection(List<int> newSelection)
    {
        var old = Selection.SelectedIndexes.ToList();
        if (old.Count == newSelection.Count && !old.Except(newSelection).Any())
        {
            return;
        }

        var args = new ListViewSelectionChangingEventArgs(SelectionChangingEvent, old, newSelection);
        RaiseEvent(args);
        if (args.Cancel)
        {
            return;
        }

        using (Selection.BatchUpdate())
        {
            Selection.Clear();
            foreach (var i in newSelection)
            {
                Selection.Select(i);
            }
        }
    }

    private void OnGroupHeaderInvoked(object? sender, RoutedEventArgs e)
    {
        if (e.Source is ListViewGroupHeader { Group: { } group } header && GroupHeaderTapBehavior == GroupHeaderTapBehavior.Invoke)
        {
            e.Handled = true;
            RaiseEvent(new ListViewGroupHeaderInvokedEventArgs(GroupHeaderInvokedEvent, header.GroupIndex, group, header));
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e) => MaybeLoadMore();

    private (int first, int last) VisibleRange()
    {
        if (_panel is null || _scroller is null || ItemCount == 0)
        {
            return (0, Math.Max(0, ItemCount - 1));
        }

        var offset = _scroller.Offset;
        var vp = _scroller.Viewport;
        var first = _panel.IndexAt(new Point(offset.X, offset.Y));
        var last = _panel.IndexAt(new Point(offset.X + Math.Max(0, vp.Width - 1), offset.Y + Math.Max(0, vp.Height - 1)));
        if (first < 0)
        {
            first = 0;
        }

        if (last < first)
        {
            last = first;
        }

        return (first, last);
    }

    private void MaybeLoadMore()
    {
        if (!AutomaticallyLoadPages || _loadingMore || ItemsSource is not ISupportIncrementalLoading src || !src.HasMoreItems || _panel is null)
        {
            return;
        }

        var pageSize = PageSize();
        var (_, last) = VisibleRange();
        if (last >= ItemCount - PagesToLoadThreshold * pageSize)
        {
            _ = LoadMoreCore(src);
        }
    }

    private int PageSize()
    {
        if (_panel is null || _scroller is null)
        {
            return 20;
        }

        var cellMain = Orientation == Orientation.Vertical ? _panel.CellSize.Height : _panel.CellSize.Width;
        var vpMain = Orientation == Orientation.Vertical ? _scroller.Viewport.Height : _scroller.Viewport.Width;
        if (cellMain <= 0 || vpMain <= 0)
        {
            return 20;
        }

        return Math.Max(1, (int)Math.Ceiling(vpMain / cellMain) * _panel.ItemsPerLine);
    }

    private async Task<int> LoadMoreCore(ISupportIncrementalLoading src)
    {
        _loadingMore = true;
        try
        {
            var added = await src.LoadMoreItemsAsync(PagesToLoad * PageSize());
            return added;
        }
        finally
        {
            _loadingMore = false;
            Dispatcher.UIThread.Post(MaybeLoadMore, DispatcherPriority.Background);
        }
    }

    private async Task FinishLoadAsync()
    {
        if (!this.IsAttachedToVisualTree())
        {
            return;
        }

        HookPanel();
        UpdateLayout();
        LoadingState = ListViewLoadingState.ViewPortLoaded;
        LoadingState = ListViewLoadingState.ItemsLoaded;
        if (!_entranceDone)
        {
            _entranceDone = true;
            var items = RealizedItems.ToList();
            foreach (var i in items)
            {
                i.ClearValue(OpacityProperty);
            }

            var args = new ListViewContentAnimatingEventArgs(ContentAnimatingEvent, "entrance");
            RaiseEvent(args);
            if (!args.Cancel && items.Count > 0)
            {
                await WinAnimations.EnterContent(items);
            }
        }

        LoadingState = ListViewLoadingState.Complete;
        MaybeLoadMore();
    }

    private static ListViewItem? ContainerOf(object? source) =>
        source as ListViewItem ?? (source as Visual)?.FindAncestorOfType<ListViewItem>();

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
        PseudoClasses.Set(":grouped", _groups is { Count: > 0 });
        PseudoClasses.Set(":loading", _loadingState < ListViewLoadingState.ItemsLoaded);
    }
}
