using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaWin.Controls;

/// <summary>
/// Mouse wheel scrolling for the horizontal layout. A ScrollViewer only applies a vertical wheel delta to its
/// vertical offset, so a panorama would ignore the wheel. The Hub turns wheel notches into horizontal travel
/// instead, as the Windows 8.1 Hub did. A section whose own content can scroll vertically keeps the wheel.
/// </summary>
public sealed partial class Hub
{
    /// <summary>The horizontal travel, in pixels, of one wheel notch.</summary>
    private const double WheelStep = 150;

    private void InitializeWheelScrolling()
    {
        AddHandler(PointerWheelChangedEvent, OnWheelTunnel, RoutingStrategies.Tunnel);
    }

    private void OnWheelTunnel(object? sender, PointerWheelEventArgs e)
    {
        if (Orientation != Orientation.Horizontal || _viewport is null || e.KeyModifiers != KeyModifiers.None)
        {
            return;
        }

        // A sideways trackpad swipe already scrolls the viewport.
        var notches = e.Delta.Y;
        if (e.Delta.X != 0 || notches == 0)
        {
            return;
        }

        var max = _viewport.Extent.Width - _viewport.Viewport.Width;
        if (max <= 0 || NestedScrollableTakesWheel(e.Source as Visual, notches))
        {
            return;
        }

        // Wheel down (a negative delta) advances the panorama, as it advances a vertical list.
        e.Handled = true;
        var x = Math.Clamp(_viewport.Offset.X - notches * WheelStep, 0, max);
        if (x != _viewport.Offset.X)
        {
            CancelSnap();
            _viewport.Offset = new Vector(x, _viewport.Offset.Y);
        }
    }

    /// <summary>Whether a scroll viewer between the wheel source and the Hub can still scroll the way the wheel points.</summary>
    private bool NestedScrollableTakesWheel(Visual? source, double notches)
    {
        for (var v = source; v is not null && v != this; v = v.GetVisualParent())
        {
            if (v is not ScrollViewer sv || sv == _viewport)
            {
                continue;
            }

            var room = sv.Extent.Height - sv.Viewport.Height;
            if (room > 0 && (notches < 0 ? sv.Offset.Y < room - 0.5 : sv.Offset.Y > 0.5))
            {
                return true;
            }
        }

        return false;
    }
}
