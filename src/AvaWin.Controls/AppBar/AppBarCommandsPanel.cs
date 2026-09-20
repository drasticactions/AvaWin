using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaWin.Controls;

/// <summary>
/// The commands layout of a bar: <see cref="AppBarCommandSection.Selection"/> commands wrap from the left and
/// <see cref="AppBarCommandSection.Global"/> commands wrap from the right. When the commands do not fit at full
/// width, the panel switches the bar to the reduced layout: labels hidden, 8px icon margins.
/// </summary>
public sealed class AppBarCommandsPanel : Panel
{
    /// <summary>Defines the <see cref="ForceReduced"/> property.</summary>
    public static readonly StyledProperty<bool> ForceReducedProperty = AvaloniaProperty.Register<AppBarCommandsPanel, bool>(nameof(ForceReduced));

    private readonly Dictionary<Control, double> _fullWidths = new();
    private bool _reduced;

    static AppBarCommandsPanel()
    {
        AffectsMeasure<AppBarCommandsPanel>(ForceReducedProperty);
    }

    /// <summary>Whether the panel is reduced even when the commands fit. The compact closed display mode sets it.</summary>
    public bool ForceReduced { get => GetValue(ForceReducedProperty); set => SetValue(ForceReducedProperty, value); }

    /// <summary>Raised when the reduced state changes.</summary>
    public event EventHandler? ReducedChanged;

    /// <summary>Whether the panel is in the reduced layout now.</summary>
    public bool IsReduced => _reduced;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width;

        // Step 1: measure at full width to learn what the commands want.
        if (!_reduced)
        {
            foreach (var child in Children)
            {
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                _fullWidths[child] = child.DesiredSize.Width;
            }
        }

        var full = 0d;
        foreach (var child in Children)
        {
            if (child.IsVisible && _fullWidths.TryGetValue(child, out var w))
            {
                full += w;
            }
        }

        var shouldReduce = ForceReduced || (!double.IsInfinity(width) && full > width);
        if (shouldReduce != _reduced)
        {
            _reduced = shouldReduce;
            foreach (var child in Children)
            {
                AppBar.SetIsReduced(child, _reduced);
            }

            ReducedChanged?.Invoke(this, EventArgs.Empty);
        }

        if (_reduced)
        {
            foreach (var child in Children)
            {
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            }
        }

        // Step 2: wrap the two groups into rows to find the height.
        var (leftWidth, rightWidth) = Split(width);
        var rowsLeft = Wrap(leftWidth, AppBarCommandSection.Selection);
        var rowsRight = Wrap(rightWidth, AppBarCommandSection.Global);
        var height = Math.Max(Sum(rowsLeft), Sum(rowsRight));
        var neededWidth = 0d;
        foreach (var child in Children)
        {
            if (child.IsVisible)
            {
                neededWidth += child.DesiredSize.Width;
            }
        }

        return new Size(double.IsInfinity(width) ? neededWidth : width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (leftWidth, rightWidth) = Split(finalSize.Width);
        Place(finalSize, leftWidth, AppBarCommandSection.Selection, fromLeft: true);
        Place(finalSize, rightWidth, AppBarCommandSection.Global, fromLeft: false);
        return finalSize;
    }

    /// <summary>
    /// The two groups share the row. Each gets its ideal width when both fit. Otherwise the width is split in
    /// proportion to their ideal widths, so neither overlaps the other.
    /// </summary>
    private (double left, double right) Split(double width)
    {
        var idealLeft = 0d;
        var idealRight = 0d;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            if (SectionOf(child) == AppBarCommandSection.Selection)
            {
                idealLeft += child.DesiredSize.Width;
            }
            else
            {
                idealRight += child.DesiredSize.Width;
            }
        }

        if (double.IsInfinity(width) || idealLeft + idealRight <= width || idealLeft + idealRight <= 0)
        {
            return (double.IsInfinity(width) ? idealLeft : Math.Max(idealLeft, width - idealRight), double.IsInfinity(width) ? idealRight : Math.Min(idealRight, width));
        }

        var left = Math.Floor(width * idealLeft / (idealLeft + idealRight));
        return (left, width - left);
    }

    private static double Sum(List<(List<Control> items, double height)> rows)
    {
        var h = 0d;
        foreach (var r in rows)
        {
            h += r.height;
        }

        return h;
    }

    private List<(List<Control> items, double height)> Wrap(double maxWidth, AppBarCommandSection section)
    {
        var rows = new List<(List<Control>, double)>();
        var current = new List<Control>();
        var x = 0d;
        var rowHeight = 0d;
        foreach (var child in Children)
        {
            if (!child.IsVisible || SectionOf(child) != section)
            {
                continue;
            }

            var w = child.DesiredSize.Width;
            if (current.Count > 0 && x + w > maxWidth)
            {
                rows.Add((current, rowHeight));
                current = new List<Control>();
                x = 0;
                rowHeight = 0;
            }

            current.Add(child);
            x += w;
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
        }

        if (current.Count > 0)
        {
            rows.Add((current, rowHeight));
        }

        return rows;
    }

    private void Place(Size finalSize, double groupWidth, AppBarCommandSection section, bool fromLeft)
    {
        var rows = Wrap(groupWidth, section);
        var y = 0d;
        foreach (var (items, height) in rows)
        {
            if (fromLeft)
            {
                var x = 0d;
                foreach (var child in items)
                {
                    child.Arrange(new Rect(x, y, child.DesiredSize.Width, height));
                    x += child.DesiredSize.Width;
                }
            }
            else
            {
                var x = finalSize.Width;
                for (var i = items.Count - 1; i >= 0; i--)
                {
                    var child = items[i];
                    x -= child.DesiredSize.Width;
                    child.Arrange(new Rect(x, y, child.DesiredSize.Width, height));
                }
            }

            y += height;
        }
    }

    private static AppBarCommandSection SectionOf(Control c) =>
        c is AppBarCommand cmd ? cmd.Section : AppBarCommandSection.Global;
}
