using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaWin.Gallery.Framework;

/// <summary>
/// Lays its children out as equal-width columns, or stacks them vertically when <see cref="IsStacked"/> is set
/// (a styled property, so <c>g|SamplePage:narrow g|ResponsiveColumns</c> can flip it).
/// </summary>
public sealed class ResponsiveColumns : Panel
{
    public static readonly StyledProperty<bool> IsStackedProperty = AvaloniaProperty.Register<ResponsiveColumns, bool>(nameof(IsStacked));
    public static readonly StyledProperty<double> ColumnSpacingProperty = AvaloniaProperty.Register<ResponsiveColumns, double>(nameof(ColumnSpacing), 40);
    public static readonly StyledProperty<double> RowSpacingProperty = AvaloniaProperty.Register<ResponsiveColumns, double>(nameof(RowSpacing), 24);

    static ResponsiveColumns()
    {
        AffectsMeasure<ResponsiveColumns>(IsStackedProperty, ColumnSpacingProperty, RowSpacingProperty);
    }

    public bool IsStacked { get => GetValue(IsStackedProperty); set => SetValue(IsStackedProperty, value); }

    public double ColumnSpacing { get => GetValue(ColumnSpacingProperty); set => SetValue(ColumnSpacingProperty, value); }

    public double RowSpacing { get => GetValue(RowSpacingProperty); set => SetValue(RowSpacingProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var count = Children.Count;
        if (count == 0)
        {
            return default;
        }

        if (IsStacked)
        {
            double height = 0, width = 0;
            foreach (var child in Children)
            {
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                height += child.DesiredSize.Height;
                width = Math.Max(width, child.DesiredSize.Width);
            }

            return new Size(width, height + RowSpacing * (count - 1));
        }

        var column = Math.Max(0, (availableSize.Width - ColumnSpacing * (count - 1)) / count);
        double tallest = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(column, availableSize.Height));
            tallest = Math.Max(tallest, child.DesiredSize.Height);
        }

        return new Size(double.IsInfinity(availableSize.Width) ? column * count + ColumnSpacing * (count - 1) : availableSize.Width, tallest);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var count = Children.Count;
        if (count == 0)
        {
            return finalSize;
        }

        if (IsStacked)
        {
            double y = 0;
            foreach (var child in Children)
            {
                child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height + RowSpacing;
            }

            return finalSize;
        }

        var column = Math.Max(0, (finalSize.Width - ColumnSpacing * (count - 1)) / count);
        double x = 0;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(x, 0, column, finalSize.Height));
            x += column + ColumnSpacing;
        }

        return finalSize;
    }
}
