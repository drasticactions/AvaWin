using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaWin.Controls;

/// <summary>The <see cref="IZoomableView"/> of a Hub. The current section is the item.</summary>
public sealed partial class Hub : IZoomableView
{
    private Action? _triggerZoom;
    private bool _isZoomedOutView;

    /// <summary>This hub as a SemanticZoom view.</summary>
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
        if (ItemCount == 0)
        {
            return null;
        }

        var index = SectionOnScreen;
        if (ContainerFromIndex(index) is not { } section)
        {
            return null;
        }

        var pos = section.TranslatePoint(new Point(0, 0), this) ?? default;
        return (ItemsView[index], new Rect(pos, section.Bounds.Size));
    }

    /// <inheritdoc/>
    void IZoomableView.SetCurrentItem(double x, double y)
    {
        _ = x;
        _ = y;
    }

    /// <inheritdoc/>
    void IZoomableView.PositionItem(object? item, Rect position)
    {
        var index = item is null ? -1 : ItemsView.IndexOf(item);
        if (index < 0 && item is not null)
        {
            index = Sections.ToList().FindIndex(s => Equals(s.Header, item));
        }

        if (index >= 0)
        {
            ScrollToSection(index);
        }

        _ = position;
    }

    /// <inheritdoc/>
    void IZoomableView.ConfigureForZoom(bool isZoomedOut, bool isCurrentView, Action triggerZoom, int prefetchedPages)
    {
        _isZoomedOutView = isZoomedOut;
        _triggerZoom = triggerZoom;
        _ = isCurrentView;
        _ = prefetchedPages;
        HeaderInvoked -= OnZoomHeaderInvoked;
        if (isZoomedOut)
        {
            HeaderInvoked += OnZoomHeaderInvoked;
        }
    }

    /// <inheritdoc/>
    void IZoomableView.HandlePointer(PointerEventArgs e)
    {
    }

    private void OnZoomHeaderInvoked(object? sender, HubHeaderInvokedEventArgs e)
    {
        if (_isZoomedOutView)
        {
            ScrollToSection(e.Index);
            _triggerZoom?.Invoke();
        }
    }
}
