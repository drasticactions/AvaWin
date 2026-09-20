using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaWin.Controls;

/// <summary><see cref="IZoomableView"/> for SemanticZoom.</summary>
public partial class ListView : IZoomableView
{
    private bool _isZoomedOutView;
    private Action? _triggerZoom;

    /// <summary>This list as a SemanticZoom view.</summary>
    public IZoomableView ZoomableView => this;

    /// <inheritdoc/>
    Control IZoomableView.Element => this;

    /// <inheritdoc/>
    void IZoomableView.BeginZoom()
    {
    }

    /// <inheritdoc/>
    void IZoomableView.EndZoom(bool isCurrentView)
    {
    }

    /// <inheritdoc/>
    (object? Item, Rect Position)? IZoomableView.GetCurrentItem()
    {
        if (ItemCount == 0 || _panel is null)
        {
            return null;
        }

        var index = CurrentIndex >= 0 && CurrentIndex < ItemCount && ContainerFromIndex(CurrentIndex) is { } focused && IsFullyVisible(focused)
            ? CurrentIndex
            : IndexOfFirstVisible;
        index = Math.Clamp(index, 0, ItemCount - 1);
        var rect = _panel.RectOf(index);
        var pos = _panel.TranslatePoint(rect.Position, this) ?? rect.Position;
        return (ItemsView[index], new Rect(pos, rect.Size));
    }

    /// <inheritdoc/>
    void IZoomableView.SetCurrentItem(double x, double y)
    {
        if (_panel is null)
        {
            return;
        }

        var p = this.TranslatePoint(new Point(x, y), _panel) ?? new Point(x, y);
        var index = _panel.IndexAt(p);
        if (index >= 0)
        {
            SetCurrentValue(CurrentIndexProperty, index);
        }
    }

    /// <inheritdoc/>
    void IZoomableView.PositionItem(object? item, Rect position)
    {
        var index = item is null ? -1 : ItemsView.IndexOf(item);
        if (index < 0 && item is not null && _groups is { } groups)
        {
            // The other view handed over a group object: the list positions on its first item.
            foreach (var g in groups)
            {
                if (Equals(g.Header, item) || Equals(g.Key, item))
                {
                    index = g.Start;
                    break;
                }
            }
        }

        if (index < 0)
        {
            return;
        }

        EnsureVisible(index);
        SetCurrentValue(CurrentIndexProperty, index);
        _ = position;
    }

    /// <inheritdoc/>
    void IZoomableView.ConfigureForZoom(bool isZoomedOut, bool isCurrentView, Action triggerZoom, int prefetchedPages)
    {
        _isZoomedOutView = isZoomedOut;
        _triggerZoom = triggerZoom;
        _ = isCurrentView;
        _ = prefetchedPages;
        if (isZoomedOut)
        {
            // Invoking an item of the zoomed-out view zooms back in on that item.
            ItemInvoked -= OnZoomedOutItemInvoked;
            ItemInvoked += OnZoomedOutItemInvoked;
        }
        else
        {
            ItemInvoked -= OnZoomedOutItemInvoked;
        }
    }

    /// <inheritdoc/>
    void IZoomableView.HandlePointer(PointerEventArgs e)
    {
    }

    private void OnZoomedOutItemInvoked(object? sender, ListViewItemInvokedEventArgs e)
    {
        if (_isZoomedOutView)
        {
            SetCurrentValue(CurrentIndexProperty, e.Index);
            _triggerZoom?.Invoke();
        }
    }

    private bool IsFullyVisible(Control container)
    {
        if (_scroller is null)
        {
            return true;
        }

        var topLeft = container.TranslatePoint(new Point(0, 0), _scroller);
        if (topLeft is not { } p)
        {
            return false;
        }

        var rect = new Rect(p, container.Bounds.Size);
        return new Rect(_scroller.Viewport).Contains(rect);
    }
}
