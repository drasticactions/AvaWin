using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Transformation;
using AvaWin.Animations;

namespace AvaWin.Controls;

/// <summary>The stage of a swipe on an item.</summary>
public enum ListViewSwipeStage
{
    /// <summary>No swipe, or a swipe short of <see cref="ListView.SwipeSelectThreshold"/>.</summary>
    None,

    /// <summary>Past <see cref="ListView.SwipeSelectThreshold"/>: a release toggles the selection.</summary>
    Select,

    /// <summary>Past <see cref="ListView.SwipeDragThreshold"/>: the item follows the contact and a release reorders it.</summary>
    Drag,
}

/// <summary>Swipe select and swipe reorder for touch and pen.</summary>
public partial class ListView
{
    /// <summary>How far a touch moves before the list decides between a swipe and a pan.</summary>
    public const double SwipeSlop = 4;

    /// <summary>How far along the swipe axis an item must move before a release toggles its selection.</summary>
    public const double SwipeSelectThreshold = 40;

    /// <summary>How far along the swipe axis an item must move before it detaches and follows the contact.</summary>
    public const double SwipeDragThreshold = 110;

    private const double SwipeResistance = 0.35;

    private IPointer? _swipePointer;
    private ListViewItem? _swipeItem;
    private int _swipeIndex = -1;
    private Point _swipeOrigin;
    private bool _swipeClaimed;
    private bool _swipeDragStarted;
    private bool _swipeDragOver;
    private ListViewSwipeStage _swipeStage;
    private List<Control>? _swipeAffected;
    private int _swipeGeneration;

    /// <summary>The stage of the swipe in progress.</summary>
    public ListViewSwipeStage SwipeStage => _swipeStage;

    private void InitializeSwipe()
    {
        AddHandler(PointerPressedEvent, OnSwipePressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnSwipeMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnSwipeReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerCaptureLostEvent, OnSwipeCaptureLost, RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private bool CanSwipe(ListViewItem item) =>
        SwipeBehavior == SwipeBehavior.Select && item.IsSelectable && IsEffectivelyEnabled &&
        (SelectionMode != ListSelectionMode.None || ItemsReorderable);

    private void OnSwipePressed(object? sender, PointerPressedEventArgs e)
    {
        if (_swipeClaimed)
        {
            return;
        }

        ResetSwipeTracking();
        if (e.Pointer.Type is not (PointerType.Touch or PointerType.Pen) || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (ContainerOf(e.Source) is not { } item || !CanSwipe(item))
        {
            return;
        }

        var index = IndexFromContainer(item);
        if (index < 0)
        {
            return;
        }

        _swipePointer = e.Pointer;
        _swipeItem = item;
        _swipeIndex = index;
        _swipeOrigin = e.GetPosition(this);
    }

    private void OnSwipeMoved(object? sender, PointerEventArgs e)
    {
        if (_swipePointer is null || e.Pointer != _swipePointer || _swipeItem is not { } item)
        {
            return;
        }

        var delta = e.GetPosition(this) - _swipeOrigin;
        var vertical = Orientation == Orientation.Vertical;
        var along = vertical ? delta.X : delta.Y;
        var across = vertical ? delta.Y : delta.X;
        if (!_swipeClaimed)
        {
            if (Math.Max(Math.Abs(along), Math.Abs(across)) <= SwipeSlop)
            {
                return;
            }

            if (Math.Abs(along) <= Math.Abs(across))
            {
                ResetSwipeTracking();
                return;
            }

            _swipeClaimed = true;
            _swipeGeneration++;
            e.PreventGestureRecognition();
            CancelDragTracking();
            _pressedItem = null;
            item.IsSwiping = true;
            e.Pointer.Capture(this);
            item.Transitions = null;
            item.ClearValue(Visual.RenderTransformOriginProperty);
        }

        e.Handled = true;
        var distance = Math.Abs(along);
        var dragging = _swipeStage == ListViewSwipeStage.Drag
            ? Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) >= SwipeDragThreshold
            : distance >= SwipeDragThreshold;
        var stage = dragging && ItemsReorderable ? ListViewSwipeStage.Drag
            : distance >= SwipeSelectThreshold && SelectionMode != ListSelectionMode.None ? ListViewSwipeStage.Select
            : ListViewSwipeStage.None;
        if (stage == ListViewSwipeStage.Drag && !EnterSwipeDrag(item))
        {
            stage = SelectionMode != ListSelectionMode.None ? ListViewSwipeStage.Select : ListViewSwipeStage.None;
        }

        if (stage != ListViewSwipeStage.Drag && _swipeStage == ListViewSwipeStage.Drag)
        {
            LeaveSwipeDrag(item);
        }

        _swipeStage = stage;
        item.SetSwipeState(stage == ListViewSwipeStage.Select);
        if (stage == ListViewSwipeStage.Drag)
        {
            var b = TransformOperations.CreateBuilder(2);
            b.AppendTranslate(delta.X, delta.Y);
            b.AppendScale(1.05, 1.05);
            item.RenderTransform = b.Build();
            UpdateSwipeDrop(e);
        }
        else
        {
            var offset = Math.Sign(along) * (distance <= SwipeSelectThreshold
                ? distance
                : SwipeSelectThreshold + (Math.Min(distance, SwipeDragThreshold) - SwipeSelectThreshold) * SwipeResistance);
            var b = TransformOperations.CreateBuilder(1);
            b.AppendTranslate(vertical ? offset : 0, vertical ? 0 : offset);
            item.RenderTransform = b.Build();
        }
    }

    private void OnSwipeReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_swipePointer is null || e.Pointer != _swipePointer)
        {
            return;
        }

        if (!_swipeClaimed || _swipeItem is not { } item)
        {
            ResetSwipeTracking();
            return;
        }

        e.Handled = true;
        var stage = _swipeStage;
        var index = _swipeIndex;
        if (stage == ListViewSwipeStage.Drag)
        {
            var insert = _dropIndex >= 0 ? _dropIndex : index;
            EndSwipe(item, reposition: false, raiseEnd: false);
            DropSwiped(index, insert);
        }
        else if (stage == ListViewSwipeStage.Select)
        {
            var wasSelected = Selection.IsSelected(index);
            ToggleSelection(index);
            EndSwipe(item, reposition: true, selected: Selection.IsSelected(index) != wasSelected ? Selection.IsSelected(index) : null);
        }
        else
        {
            EndSwipe(item, reposition: true);
        }

        e.Pointer.Capture(null);
    }

    private void OnSwipeCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_swipeClaimed || e.Pointer != _swipePointer || !ReferenceEquals(e.Source, this))
        {
            return;
        }

        if (_swipeItem is { } item)
        {
            EndSwipe(item, reposition: true);
        }
        else
        {
            ResetSwipeTracking();
        }
    }

    private bool EnterSwipeDrag(ListViewItem item)
    {
        if (_swipeStage == ListViewSwipeStage.Drag)
        {
            return true;
        }

        var indexes = new[] { _swipeIndex };
        var items = new[] { ItemsView[_swipeIndex] };
        if (!_swipeDragStarted)
        {
            var start = new ListViewDragEventArgs(ItemDragStartEvent, indexes, items, null, -1);
            RaiseEvent(start);
            if (start.Cancel)
            {
                return false;
            }

            _swipeDragStarted = true;
        }
        else
        {
            RaiseEvent(new ListViewDragEventArgs(ItemDragEnterEvent, indexes, items, null, -1));
        }

        _swipeDragOver = true;
        item.SetDragState(source: true, over: false);
        item.ZIndex = 100;
        _swipeAffected = RealizedItems.Where(i => !ReferenceEquals(i, item)).Cast<Control>().ToList();
        _ = WinAnimations.DragSourceStart(Array.Empty<Control>(), _swipeAffected);
        return true;
    }

    private void LeaveSwipeDrag(ListViewItem item)
    {
        item.SetDragState(source: false, over: false);
        item.ClearValue(ZIndexProperty);
        HideDropIndicator();
        if (_swipeAffected is { } affected)
        {
            _ = WinAnimations.DragSourceEnd(Array.Empty<Control>(), null, affected);
            _swipeAffected = null;
        }

        if (_swipeDragOver)
        {
            _swipeDragOver = false;
            RaiseEvent(new ListViewDragEventArgs(ItemDragLeaveEvent, [_swipeIndex], [ItemsView[_swipeIndex]], null, -1));
        }
    }

    private void UpdateSwipeDrop(PointerEventArgs e)
    {
        if (_panel is null)
        {
            return;
        }

        var (start, end) = SwipeGroupRange(_swipeIndex);
        var insert = Math.Clamp(_panel.InsertionIndexAt(e.GetPosition(_panel)), start, end);
        if (insert == _dropIndex)
        {
            return;
        }

        _dropIndex = insert;
        RaiseEvent(new ListViewDragEventArgs(ItemDragBetweenEvent, [_swipeIndex], [ItemsView[_swipeIndex]], null, insert));
        ShowDropIndicator(insert);
    }

    private (int start, int end) SwipeGroupRange(int index)
    {
        if (_groups is { Count: > 0 } groups)
        {
            foreach (var g in groups)
            {
                if (index >= g.Start && index < g.Start + g.Count)
                {
                    return (g.Start, g.Start + g.Count);
                }
            }
        }

        return (0, ItemCount);
    }

    private void DropSwiped(int index, int insert)
    {
        var indexes = new[] { index };
        var items = new[] { ItemsView[index] };
        var drop = new ListViewDragEventArgs(ItemDragDropEvent, indexes, items, null, insert);
        RaiseEvent(drop);
        if (!drop.Cancel && insert != index && insert != index + 1)
        {
            ReorderItems(indexes, insert, keepSelection: true);
        }

        RaiseEvent(new ListViewDragEventArgs(ItemDragEndEvent, indexes, items, null, -1));
    }

    private void EndSwipe(ListViewItem item, bool reposition, bool? selected = null, bool raiseEnd = true)
    {
        if (_swipeStage == ListViewSwipeStage.Drag)
        {
            LeaveSwipeDragQuietly(item);
        }

        if (_swipeDragStarted && raiseEnd)
        {
            RaiseEvent(new ListViewDragEventArgs(ItemDragEndEvent, [_swipeIndex], [ItemsView[_swipeIndex]], null, -1));
        }

        item.SetSwipeState(false);
        item.IsSwiping = false;
        var generation = _swipeGeneration;
        ResetSwipeTracking();
        if (!reposition)
        {
            item.ClearValue(Visual.RenderTransformProperty);
            item.ClearValue(Animatable.TransitionsProperty);
            return;
        }

        var run = selected switch
        {
            true => WinAnimations.SwipeSelect(new Control[] { item }, Array.Empty<Control>()),
            false => WinAnimations.SwipeDeselect(new Control[] { item }, Array.Empty<Control>()),
            _ => WinAnimations.PointerUp(item),
        };
        _ = ClearTransformAfter(item, run, generation);
    }

    private void LeaveSwipeDragQuietly(ListViewItem item)
    {
        _swipeDragOver = false;
        item.SetDragState(source: false, over: false);
        item.ClearValue(ZIndexProperty);
        HideDropIndicator();
        if (_swipeAffected is { } affected)
        {
            _ = WinAnimations.DragSourceEnd(Array.Empty<Control>(), null, affected);
            _swipeAffected = null;
        }
    }

    private async System.Threading.Tasks.Task ClearTransformAfter(ListViewItem item, System.Threading.Tasks.Task run, int generation)
    {
        await run;
        if (generation == _swipeGeneration && !ReferenceEquals(_swipeItem, item))
        {
            item.ClearValue(Visual.RenderTransformProperty);
            item.ClearValue(Animatable.TransitionsProperty);
        }
    }

    private void ResetSwipeTracking()
    {
        _swipePointer = null;
        _swipeItem = null;
        _swipeIndex = -1;
        _swipeClaimed = false;
        _swipeDragStarted = false;
        _swipeDragOver = false;
        _swipeStage = ListViewSwipeStage.None;
    }
}
