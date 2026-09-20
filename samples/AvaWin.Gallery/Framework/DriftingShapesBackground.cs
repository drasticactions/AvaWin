using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvaWin.Gallery.Framework;

/// <summary>
/// A live background: translucent circles drift across a gradient and are redrawn every frame while the control is
/// in the tree (frozen while animations are disabled). <see cref="Offset"/> shifts the whole scene sideways.
/// </summary>
public sealed class DriftingShapesBackground : Control
{
    public static readonly StyledProperty<double> OffsetProperty = AvaloniaProperty.Register<DriftingShapesBackground, double>(nameof(Offset));
    public static readonly StyledProperty<Color> StartColorProperty = AvaloniaProperty.Register<DriftingShapesBackground, Color>(nameof(StartColor), Color.Parse("#4617B4"));
    public static readonly StyledProperty<Color> EndColorProperty = AvaloniaProperty.Register<DriftingShapesBackground, Color>(nameof(EndColor), Color.Parse("#2672EC"));

    private const int Count = 14;
    private readonly (double X, double Y, double Radius, double Speed, double Phase)[] _shapes = new (double, double, double, double, double)[Count];
    private readonly DispatcherTimer _clock = new(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) => { });
    private TimeSpan _time;
    private DateTime _last;

    static DriftingShapesBackground()
    {
        AffectsRender<DriftingShapesBackground>(OffsetProperty, StartColorProperty, EndColorProperty);
    }

    public DriftingShapesBackground()
    {
        var rnd = new Random(7);
        for (var i = 0; i < Count; i++)
        {
            _shapes[i] = (rnd.NextDouble(), rnd.NextDouble(), 40 + rnd.NextDouble() * 120, 0.02 + rnd.NextDouble() * 0.05, rnd.NextDouble() * Math.PI * 2);
        }

        _clock.Tick += OnTick;
    }

    /// <summary>Horizontal shift of the scene, in pixels.</summary>
    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    public Color StartColor { get => GetValue(StartColorProperty); set => SetValue(StartColorProperty, value); }

    public Color EndColor { get => GetValue(EndColorProperty); set => SetValue(EndColorProperty, value); }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _last = DateTime.UtcNow;
        _clock.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _clock.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        if (AvaWin.Animations.WinAnimations.IsEnabled)
        {
            _time += now - _last;
            InvalidateVisual();
        }

        _last = now;
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(StartColor, 0), new GradientStop(EndColor, 1) },
        }, bounds);

        // Each circle orbits its anchor slowly; the scene wraps so the parallax offset never runs out of shapes.
        var t = _time.TotalSeconds;
        var span = bounds.Width + 400;
        using (context.PushClip(bounds))
        {
            foreach (var (x, y, radius, speed, phase) in _shapes)
            {
                var cx = ((x * span + Math.Sin(t * speed + phase) * 60 - Offset) % span + span) % span - 200;
                var cy = y * bounds.Height + Math.Cos(t * speed * 1.3 + phase) * 30;
                var alpha = (byte)(40 + 40 * (0.5 + 0.5 * Math.Sin(t * speed * 2 + phase)));
                context.DrawEllipse(new SolidColorBrush(Colors.White, alpha / 255.0), null, new Point(cx, cy), radius, radius);
            }
        }
    }
}
