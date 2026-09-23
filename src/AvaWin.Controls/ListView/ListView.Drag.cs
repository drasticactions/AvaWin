using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaWin.Animations;

namespace AvaWin.Controls;

/// <summary>Drag and reorder support.</summary>
public partial class ListView
{
    /// <summary>The in-process drag format that carries <see cref="ListViewDragData"/>.</summary>
    public static readonly DataFormat<ListViewDragData> DragFormat = DataFormat.CreateInProcessFormat<ListViewDragData>("avawin.listview.items");

    /// <summary>Defines the <see cref="ItemDragStart"/> event.</summary>
    public static readonly RoutedEvent<ListViewDragEventArgs> ItemDragStartEvent = RoutedEvent.Register<ListView, ListViewDragEventArgs>(nameof(ItemDragStart), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ItemDragEnter"/> event.</summary>
    public static readonly RoutedEvent<ListViewDragEventArgs> ItemDragEnterEvent = RoutedEvent.Register<ListView, ListViewDragEventArgs>(nameof(ItemDragEnter), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ItemDragBetween"/> event.</summary>
    public static readonly RoutedEvent<ListViewDragEventArgs> ItemDragBetweenEvent = RoutedEvent.Register<ListView, ListViewDragEventArgs>(nameof(ItemDragBetween), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ItemDragLeave"/> event.</summary>
    public static readonly RoutedEvent<ListViewDragEventArgs> ItemDragLeaveEvent = RoutedEvent.Register<ListView, ListViewDragEventArgs>(nameof(ItemDragLeave), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ItemDragChanged"/> event.</summary>
    public static readonly RoutedEvent<ListViewDragEventArgs> ItemDragChangedEvent = RoutedEvent.Register<ListView, ListViewDragEventArgs>(nameof(ItemDragChanged), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ItemDragDrop"/> event.</summary>
    public static readonly RoutedEvent<ListViewDragEventArgs> ItemDragDropEvent = RoutedEvent.Register<ListView, ListViewDragEventArgs>(nameof(ItemDragDrop), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ItemDragEnd"/> event.</summary>
    public static readonly RoutedEvent<ListViewDragEventArgs> ItemDragEndEvent = RoutedEvent.Register<ListView, ListViewDragEventArgs>(nameof(ItemDragEnd), RoutingStrategies.Bubble);

    private const double DragThreshold = 8;

    private PointerPressedEventArgs? _dragTrigger;
    private Point _dragOrigin;
    private ListViewItem? _dragCandidate;
    private ListViewItem? _dragSource;
    private bool _dragging;
    private int _dropIndex = -1;
    private Border? _dropIndicator;
    private Canvas? _dropAdorner;

    /// <summary>Raised when a drag starts. Cancel it to stop the drag.</summary>
    public event EventHandler<ListViewDragEventArgs> ItemDragStart { add => AddHandler(ItemDragStartEvent, value); remove => RemoveHandler(ItemDragStartEvent, value); }

    /// <summary>Raised when a drag enters the list.</summary>
    public event EventHandler<ListViewDragEventArgs> ItemDragEnter { add => AddHandler(ItemDragEnterEvent, value); remove => RemoveHandler(ItemDragEnterEvent, value); }

    /// <summary>The insertion point changed.</summary>
    public event EventHandler<ListViewDragEventArgs> ItemDragBetween { add => AddHandler(ItemDragBetweenEvent, value); remove => RemoveHandler(ItemDragBetweenEvent, value); }

    /// <summary>Raised when a drag leaves the list.</summary>
    public event EventHandler<ListViewDragEventArgs> ItemDragLeave { add => AddHandler(ItemDragLeaveEvent, value); remove => RemoveHandler(ItemDragLeaveEvent, value); }

    /// <summary>The dragged data changed while over the list.</summary>
    public event EventHandler<ListViewDragEventArgs> ItemDragChanged { add => AddHandler(ItemDragChangedEvent, value); remove => RemoveHandler(ItemDragChangedEvent, value); }

    /// <summary>Raised when the user drops on the list. Cancel it to stop the built-in reorder.</summary>
    public event EventHandler<ListViewDragEventArgs> ItemDragDrop { add => AddHandler(ItemDragDropEvent, value); remove => RemoveHandler(ItemDragDropEvent, value); }

    /// <summary>Raised when a drag that started in this list ends.</summary>
    public event EventHandler<ListViewDragEventArgs> ItemDragEnd { add => AddHandler(ItemDragEndEvent, value); remove => RemoveHandler(ItemDragEndEvent, value); }

    /// <summary>The insertion index shown while something is dragged over the list, or −1.</summary>
    public int DropIndex => _dropIndex;

    private void InitializeDrag()
    {
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
        DragDrop.SetAllowDrop(this, true);
    }

    private void BeginDragTracking(PointerPressedEventArgs e)
    {
        _dragTrigger = null;
        _dragCandidate = null;
        if (!(ItemsDraggable || ItemsReorderable) || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
            (SwipeBehavior == SwipeBehavior.Select && e.Pointer.Type is PointerType.Touch or PointerType.Pen))
        {
            return;
        }

        var item = ContainerOf(e.Source);
        if (item is null || !item.IsSelectable)
        {
            return;
        }

        _dragTrigger = e;
        _dragOrigin = e.GetPosition(this);
        _dragCandidate = item;
    }

    private void TrackDrag(PointerEventArgs e)
    {
        if (_dragTrigger is null || _dragCandidate is null || _dragging)
        {
            return;
        }

        var delta = e.GetPosition(this) - _dragOrigin;
        if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
        {
            return;
        }

        var trigger = _dragTrigger;
        var source = _dragCandidate;
        _dragTrigger = null;
        _dragCandidate = null;
        _ = StartDragAsync(trigger, source);
    }

    private void CancelDragTracking()
    {
        _dragTrigger = null;
        _dragCandidate = null;
    }

    private async System.Threading.Tasks.Task StartDragAsync(PointerPressedEventArgs trigger, ListViewItem source)
    {
        var index = IndexFromContainer(source);
        if (index < 0)
        {
            return;
        }

        // When the pressed item is selected, the whole selection is dragged. Otherwise only that item is.
        var indexes = Selection.IsSelected(index) && SelectionMode != ListSelectionMode.None
            ? Selection.SelectedIndexes.OrderBy(i => i).ToList()
            : [index];
        var items = indexes.Select(i => ItemsView[i]).ToList();
        // One item carrying both formats: macOS makes a drag image per item but a pasteboard item only for
        // non-in-process formats, and AppKit throws when those counts differ.
        var item = DataTransferItem.Create(DragFormat, new ListViewDragData(this, indexes, items));
        item.SetText(string.Join(Environment.NewLine, items.Select(i => i?.ToString())));
        var data = new DataTransfer();
        data.Add(item);
        var startArgs = new ListViewDragEventArgs(ItemDragStartEvent, indexes, items, data, -1);
        RaiseEvent(startArgs);
        if (startArgs.Cancel)
        {
            ((IDisposable)data).Dispose();
            return;
        }

        _dragging = true;
        _dragSource = source;
        source.SetDragState(source: true, over: false);
        var affected = RealizedItems.Where(i => !ReferenceEquals(i, source)).ToList();
        _ = WinAnimations.DragSourceStart(source, affected);
        try
        {
            await DragDrop.DoDragDropAsync(trigger, data, ItemsReorderable ? DragDropEffects.Move | DragDropEffects.Copy : DragDropEffects.Copy | DragDropEffects.Move);
        }
        finally
        {
            _dragging = false;
            HideDropIndicator();
            source.SetDragState(source: false, over: false);
            _dragSource = null;
            await WinAnimations.DragSourceEnd(source, null, affected);
            RaiseEvent(new ListViewDragEventArgs(ItemDragEndEvent, indexes, items, data, -1));
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        var payload = e.DataTransfer.TryGetValue(DragFormat);
        var (indexes, items) = Payload(payload);
        RaiseEvent(new ListViewDragEventArgs(ItemDragEnterEvent, indexes, items, e.DataTransfer, -1));
        UpdateDrop(e, payload);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var payload = e.DataTransfer.TryGetValue(DragFormat);
        UpdateDrop(e, payload);
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        var (indexes, items) = Payload(e.DataTransfer.TryGetValue(DragFormat));
        HideDropIndicator();
        RaiseEvent(new ListViewDragEventArgs(ItemDragLeaveEvent, indexes, items, e.DataTransfer, -1));
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var payload = e.DataTransfer.TryGetValue(DragFormat);
        var (indexes, items) = Payload(payload);
        var insert = _dropIndex >= 0 ? _dropIndex : InsertionIndexFor(e);
        HideDropIndicator();
        var args = new ListViewDragEventArgs(ItemDragDropEvent, indexes, items, e.DataTransfer, insert);
        RaiseEvent(args);
        if (args.Cancel || payload is null || !ReferenceEquals(payload.Source, this) || !ItemsReorderable)
        {
            return;
        }

        if (ReorderItems(indexes, insert))
        {
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private void UpdateDrop(DragEventArgs e, ListViewDragData? payload)
    {
        var acceptable = payload is not null ? ItemsReorderable && ReferenceEquals(payload.Source, this) : true;
        if (!acceptable)
        {
            e.DragEffects = DragDropEffects.None;
            HideDropIndicator();
            return;
        }

        e.DragEffects = payload is not null ? DragDropEffects.Move : e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Move);
        var insert = InsertionIndexFor(e);
        if (insert != _dropIndex)
        {
            _dropIndex = insert;
            var (indexes, items) = Payload(payload);
            RaiseEvent(new ListViewDragEventArgs(ItemDragBetweenEvent, indexes, items, e.DataTransfer, insert));
            ShowDropIndicator(insert);
        }
    }

    private int InsertionIndexFor(DragEventArgs e)
    {
        if (_panel is null || ItemCount == 0)
        {
            return 0;
        }

        return Math.Clamp(_panel.InsertionIndexAt(e.GetPosition(_panel)), 0, ItemCount);
    }

    private static (IReadOnlyList<int>, IReadOnlyList<object?>) Payload(ListViewDragData? payload) =>
        payload is null ? (Array.Empty<int>(), Array.Empty<object?>()) : (payload.Indexes, payload.Items);

    private bool ReorderItems(IReadOnlyList<int> indexes, int insert, bool keepSelection = false)
    {
        if (ItemsSource is not IList list || list.IsReadOnly || list.IsFixedSize || indexes.Count == 0)
        {
            return false;
        }

        var moving = indexes.OrderBy(i => i).Select(i => list[i]).ToList();
        var kept = keepSelection ? Selection.SelectedIndexes.Select(i => list[i]).ToList() : null;
        var target = insert - indexes.Count(i => i < insert);
        foreach (var i in indexes.OrderByDescending(i => i))
        {
            list.RemoveAt(i);
        }

        target = Math.Clamp(target, 0, list.Count);
        for (var k = 0; k < moving.Count; k++)
        {
            list.Insert(target + k, moving[k]);
        }

        // The moved items are selected again after the collection change.
        var count = moving.Count;
        Dispatcher.UIThread.Post(() =>
        {
            using (Selection.BatchUpdate())
            {
                Selection.Clear();
                if (kept is not null)
                {
                    foreach (var item in kept)
                    {
                        if (list.IndexOf(item) is var i and >= 0)
                        {
                            Selection.Select(i);
                        }
                    }
                }
                else
                {
                    for (var k = 0; k < count; k++)
                    {
                        Selection.Select(target + k);
                    }
                }
            }
        }, DispatcherPriority.Loaded);
        return true;
    }

    private void ShowDropIndicator(int insert)
    {
        if (_panel is null || _scroller is null)
        {
            return;
        }

        if (_dropIndicator is null)
        {
            _dropIndicator = new Border { IsHitTestVisible = false };
            _dropIndicator.Bind(Border.BackgroundProperty, this.GetResourceObservable("ListViewItemSelectionBorderBrush"));
            _dropAdorner = new Canvas { IsHitTestVisible = false, Children = { _dropIndicator } };
        }

        var count = ItemCount;
        var vertical = Orientation == Avalonia.Layout.Orientation.Vertical;
        var perLine = _panel.ItemsPerLine;
        Rect anchor;
        bool after;
        if (insert < count)
        {
            anchor = _panel.RectOf(insert);
            after = false;
        }
        else
        {
            anchor = _panel.RectOf(count - 1);
            after = true;
        }

        var toList = _panel.TranslatePoint(anchor.Position, this) ?? anchor.Position;
        double x, y, w, h;
        if (perLine > 1 && !_panel.IsCellSpanning)
        {
            // In a grid the line runs across the cell on the cross axis, at its leading or trailing edge.
            if (vertical)
            {
                x = after ? toList.X + anchor.Width : toList.X;
                y = toList.Y;
                w = 4;
                h = anchor.Height;
            }
            else
            {
                x = toList.X;
                y = after ? toList.Y + anchor.Height : toList.Y;
                w = anchor.Width;
                h = 4;
            }
        }
        else if (vertical)
        {
            x = toList.X;
            y = after ? toList.Y + anchor.Height : toList.Y - 2;
            w = anchor.Width;
            h = 4;
        }
        else
        {
            x = after ? toList.X + anchor.Width : toList.X - 2;
            y = toList.Y;
            w = 4;
            h = anchor.Height;
        }

        Canvas.SetLeft(_dropIndicator, x);
        Canvas.SetTop(_dropIndicator, y);
        _dropIndicator.Width = Math.Max(4, w);
        _dropIndicator.Height = Math.Max(4, h);
        if (AdornerLayer.GetAdorner(this) != _dropAdorner)
        {
            AdornerLayer.SetAdorner(this, _dropAdorner);
        }
    }

    private void HideDropIndicator()
    {
        _dropIndex = -1;
        if (_dropAdorner is not null && AdornerLayer.GetAdorner(this) == _dropAdorner)
        {
            AdornerLayer.SetAdorner(this, null);
        }
    }
}
