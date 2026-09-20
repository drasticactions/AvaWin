using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaWin.Animations;

namespace AvaWin.Controls;

/// <summary>
/// Proximity snapping for the Hub viewport. Avalonia only offers mandatory snapping, which snaps on every wheel
/// tick, so the Hub applies the proximity rule itself. When a scroll settles within <see cref="SnapProximity"/>
/// of a section start, the viewport eases onto it. Further away, it stays where it is.
/// </summary>
public sealed partial class Hub
{
    /// <summary>Defines the <see cref="SnapProximity"/> property.</summary>
    public static readonly StyledProperty<double> SnapProximityProperty = AvaloniaProperty.Register<Hub, double>(nameof(SnapProximity), 120);

    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(150);

    private DispatcherTimer? _settleTimer;
    private CancellationTokenSource? _snapAnimation;
    private bool _snapping;

    /// <summary>
    /// The distance in pixels from a section start within which a settled scroll snaps to it. 0 disables snapping.
    /// </summary>
    public double SnapProximity { get => GetValue(SnapProximityProperty); set => SetValue(SnapProximityProperty, value); }

    /// <summary>The snap points: the scroll offset of every section start along the orientation.</summary>
    public IReadOnlyList<double> SnapPoints
    {
        get
        {
            var points = new List<double>();
            if (ItemsPanelRoot is not { } surface)
            {
                return points;
            }

            var horizontal = Orientation == Orientation.Horizontal;
            for (var i = 0; i < ItemCount; i++)
            {
                if (ContainerFromIndex(i) is not { } section)
                {
                    continue;
                }

                var pos = section.TranslatePoint(new Point(0, 0), surface) ?? default;
                points.Add(Math.Max(0, horizontal ? pos.X - surface.Margin.Left : pos.Y - surface.Margin.Top));
            }

            return points;
        }
    }

    private void HookSnapping(ScrollViewer viewport)
    {
        viewport.ScrollChanged += OnViewportScrollChanged;
        viewport.ScrollChanged += OnViewportScrolled;
        _settleTimer = new DispatcherTimer { Interval = SettleDelay };
        _settleTimer.Tick += OnScrollSettled;
    }

    private void UnhookSnapping(ScrollViewer viewport)
    {
        viewport.ScrollChanged -= OnViewportScrollChanged;
        viewport.ScrollChanged -= OnViewportScrolled;
        if (_settleTimer is { } t)
        {
            t.Stop();
            t.Tick -= OnScrollSettled;
            _settleTimer = null;
        }

        CancelSnap();
    }

    private void OnViewportScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_snapping || SnapProximity <= 0 || _settleTimer is null)
        {
            return;
        }

        var horizontal = Orientation == Orientation.Horizontal;
        if ((horizontal ? e.OffsetDelta.X : e.OffsetDelta.Y) == 0)
        {
            return;
        }

        // User input during an ease takes over.
        CancelSnap();
        _settleTimer.Stop();
        _settleTimer.Start();
    }

    private void OnScrollSettled(object? sender, EventArgs e)
    {
        _settleTimer?.Stop();
        if (_viewport is null || SnapProximity <= 0)
        {
            return;
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var offset = horizontal ? _viewport.Offset.X : _viewport.Offset.Y;
        var max = horizontal ? _viewport.Extent.Width - _viewport.Viewport.Width : _viewport.Extent.Height - _viewport.Viewport.Height;
        // The end of the panorama is a resting position too. A scroll that reaches it stays there, instead of
        // easing back to a section start that is within proximity of the end.
        var candidates = new List<double>(SnapPoints) { Math.Max(0, max) };
        var best = double.NaN;
        var bestDistance = double.PositiveInfinity;
        foreach (var point in candidates)
        {
            var target = Math.Clamp(point, 0, Math.Max(0, max));
            var distance = Math.Abs(target - offset);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = target;
            }
        }

        // The scroll already rests on a snap point, or is too far from the nearest one: it stays.
        if (bestDistance > 0.5 && bestDistance <= SnapProximity)
        {
            SnapTo(best);
        }
    }

    private async void SnapTo(double target)
    {
        if (_viewport is null)
        {
            return;
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var from = _viewport.Offset;
        var to = horizontal ? new Vector(target, from.Y) : new Vector(from.X, target);
        if (!WinAnimations.IsEnabled || WinAnimations.TimeScale <= 0)
        {
            _viewport.Offset = to;
            return;
        }

        CancelSnap();
        var cts = new CancellationTokenSource();
        _snapAnimation = cts;
        _snapping = true;
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
                _viewport.Offset = to;
            }
        }
        finally
        {
            _snapping = false;
            if (ReferenceEquals(_snapAnimation, cts))
            {
                _snapAnimation = null;
            }

            cts.Dispose();
        }
    }

    private void CancelSnap()
    {
        var running = _snapAnimation;
        _snapAnimation = null;
        running?.Cancel();
    }
}
