using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using AvaWin.Animations;

namespace AvaWin.Controls;

/// <summary>
/// Wheel and trackpad paging for the horizontal layout. The snap points of the viewport would turn a page on
/// every wheel event, and a trackpad sends many tiny deltas, so the list would sprint to its end. Instead, wheel
/// deltas accumulate until they add up to one notch. That moves exactly one page, eased over 250 ms. Further input
/// is ignored until the page has settled.
/// </summary>
public partial class NavBarContainer
{
    /// <summary>The accumulated wheel delta, in notches, that turns a page.</summary>
    private const double WheelNotch = 1.0;

    private double _wheelAccumulator;
    private CancellationTokenSource? _pageAnimation;

    private void InitializeWheelPaging()
    {
        AddHandler(PointerWheelChangedEvent, OnWheelTunnel, RoutingStrategies.Tunnel);
    }

    private void OnWheelTunnel(object? sender, PointerWheelEventArgs e)
    {
        if (Layout != Orientation.Horizontal || _surface is null || _surface.PageCount <= 1)
        {
            return;
        }

        // The viewport must never get wheel input, because its snap points would turn a page per event.
        e.Handled = true;
        var delta = Math.Abs(e.Delta.X) >= Math.Abs(e.Delta.Y) ? e.Delta.X : e.Delta.Y;
        if (delta == 0)
        {
            return;
        }

        if (_pageAnimation is not null)
        {
            // A page turn is in progress: the rest of the gesture is ignored.
            return;
        }

        // Wheel up, or a right-to-left trackpad swipe (a positive delta), goes to the previous page, like ScrollViewer.
        _wheelAccumulator += delta;
        if (Math.Abs(_wheelAccumulator) < WheelNotch)
        {
            return;
        }

        var direction = _wheelAccumulator > 0 ? -1 : 1;
        _wheelAccumulator = 0;
        AnimateToPage(CurrentPage + direction);
    }

    /// <summary>Eases the viewport onto <paramref name="page"/> over 250 ms. Jumps at once when animations are off.</summary>
    private async void AnimateToPage(int page)
    {
        if (_viewport is null || _surface is null)
        {
            return;
        }

        page = Math.Clamp(page, 0, Math.Max(0, _surface.PageCount - 1));
        var from = _viewport.Offset;
        var to = new Vector(page * _surface.PageWidth, from.Y);
        if (from == to)
        {
            return;
        }

        if (!WinAnimations.IsEnabled || WinAnimations.TimeScale <= 0)
        {
            GoToPage(page);
            return;
        }

        _pageAnimation?.Cancel();
        var cts = new CancellationTokenSource();
        _pageAnimation = cts;
        try
        {
            var animation = new Animation
            {
                Duration = AnimationRunner.Ms(250),
                Easing = WinEasing.Snap,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(ScrollViewer.OffsetProperty, from) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(ScrollViewer.OffsetProperty, to) } },
                },
            };
            await animation.RunAsync(_viewport, cts.Token);
            if (!cts.IsCancellationRequested)
            {
                GoToPage(page);
            }
        }
        finally
        {
            if (ReferenceEquals(_pageAnimation, cts))
            {
                _pageAnimation = null;
            }

            cts.Dispose();
            _wheelAccumulator = 0;
        }
    }
}
