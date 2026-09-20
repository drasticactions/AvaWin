using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaWin.Controls;

/// <summary>
/// Lays out the Pivot header buttons in a row. The selected one comes first, and the earlier headers wrap round to
/// the end. Children beyond the track width are clipped.
/// </summary>
public sealed class PivotHeadersPanel : Panel
{
    /// <summary>Defines the <see cref="StartIndex"/> property.</summary>
    public static readonly StyledProperty<int> StartIndexProperty = AvaloniaProperty.Register<PivotHeadersPanel, int>(nameof(StartIndex));

    static PivotHeadersPanel()
    {
        AffectsArrange<PivotHeadersPanel>(StartIndexProperty);
        AffectsMeasure<PivotHeadersPanel>(StartIndexProperty);
    }

    /// <summary>The index of the child shown first: the selected header.</summary>
    public int StartIndex { get => GetValue(StartIndexProperty); set => SetValue(StartIndexProperty, value); }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var w = 0d;
        var h = 0d;
        foreach (var c in Children)
        {
            c.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            w += c.DesiredSize.Width;
            h = Math.Max(h, c.DesiredSize.Height);
        }

        return new Size(double.IsInfinity(availableSize.Width) ? w : availableSize.Width, h);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var n = Children.Count;
        if (n == 0)
        {
            return finalSize;
        }

        var start = ((StartIndex % n) + n) % n;
        var x = 0d;
        for (var k = 0; k < n; k++)
        {
            var c = Children[(start + k) % n];
            c.Arrange(new Rect(x, 0, c.DesiredSize.Width, finalSize.Height));
            x += c.DesiredSize.Width;
        }

        return finalSize;
    }
}
