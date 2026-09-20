using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AvaWin.Controls;

/// <summary>
/// The items panel of a <see cref="NavBarContainer"/>. Horizontal: a paged grid of <see cref="MaxRows"/> rows,
/// filled column first. Each page is as wide as the viewport and is a regular scroll snap point. When
/// <see cref="FixedSize"/> is false, the commands stretch to fill the page. Vertical: a plain stack.
/// </summary>
public sealed class NavBarSurfacePanel : Panel, IScrollSnapPointsInfo
{
    /// <summary>Defines the <see cref="Orientation"/> property.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<NavBarSurfacePanel, Orientation>(nameof(Orientation));

    /// <summary>Defines the <see cref="MaxRows"/> property.</summary>
    public static readonly StyledProperty<int> MaxRowsProperty = AvaloniaProperty.Register<NavBarSurfacePanel, int>(nameof(MaxRows), 1);

    /// <summary>Defines the <see cref="FixedSize"/> property.</summary>
    public static readonly StyledProperty<bool> FixedSizeProperty = AvaloniaProperty.Register<NavBarSurfacePanel, bool>(nameof(FixedSize));

    /// <summary>Defines the <see cref="ViewportWidth"/> property.</summary>
    public static readonly StyledProperty<double> ViewportWidthProperty = AvaloniaProperty.Register<NavBarSurfacePanel, double>(nameof(ViewportWidth), double.NaN);

    /// <summary>Defines the <see cref="LeadingEdge"/> property.</summary>
    public static readonly StyledProperty<double> LeadingEdgeProperty = AvaloniaProperty.Register<NavBarSurfacePanel, double>(nameof(LeadingEdge), 25);

    private int _pages = 1;
    private int _rowsPerPage = 1;
    private int _columnsPerPage = 1;
    private double _pageWidth;

    static NavBarSurfacePanel()
    {
        AffectsMeasure<NavBarSurfacePanel>(OrientationProperty, MaxRowsProperty, FixedSizeProperty, ViewportWidthProperty, LeadingEdgeProperty);
    }

    /// <summary>Horizontal (a paged grid) or vertical (a list).</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The number of rows per page (horizontal).</summary>
    public int MaxRows { get => GetValue(MaxRowsProperty); set => SetValue(MaxRowsProperty, value); }

    /// <summary>Whether commands keep their natural width instead of stretching to fill the page.</summary>
    public bool FixedSize { get => GetValue(FixedSizeProperty); set => SetValue(FixedSizeProperty, value); }

    /// <summary>The width of one page. The container binds it to the viewport width.</summary>
    public double ViewportWidth { get => GetValue(ViewportWidthProperty); set => SetValue(ViewportWidthProperty, value); }

    /// <summary>The space at each page edge for the navigation arrows: a 17px arrow and an 8px margin.</summary>
    public double LeadingEdge { get => GetValue(LeadingEdgeProperty); set => SetValue(LeadingEdgeProperty, value); }

    /// <summary>The number of pages after the last measure.</summary>
    public int PageCount => _pages;

    /// <summary>The width of one page after the last measure.</summary>
    public double PageWidth => _pageWidth;

    /// <summary>The number of items per page after the last measure.</summary>
    public int ItemsPerPage => Math.Max(1, _rowsPerPage * _columnsPerPage);

    /// <summary>Raised when <see cref="PageCount"/> changes.</summary>
    public event EventHandler? PagesChanged;

    /// <inheritdoc/>
    bool IScrollSnapPointsInfo.AreHorizontalSnapPointsRegular { get => true; set { } }

    /// <inheritdoc/>
    bool IScrollSnapPointsInfo.AreVerticalSnapPointsRegular { get => false; set { } }

    /// <inheritdoc/>
    public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;

    /// <inheritdoc/>
    event EventHandler<RoutedEventArgs>? IScrollSnapPointsInfo.VerticalSnapPointsChanged { add { } remove { } }

    /// <inheritdoc/>
    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment) => Array.Empty<double>();

    /// <inheritdoc/>
    public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
    {
        offset = 0;
        return orientation == Orientation.Horizontal && this.Orientation == Orientation.Horizontal ? _pageWidth : 0;
    }

    /// <summary>The page that contains the item at <paramref name="index"/>.</summary>
    public int PageOf(int index) => Orientation == Orientation.Horizontal ? Math.Clamp(index / ItemsPerPage, 0, Math.Max(0, _pages - 1)) : 0;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var count = 0;
        foreach (var c in Children)
        {
            if (c.IsVisible)
            {
                count++;
            }
        }

        if (Orientation == Orientation.Vertical)
        {
            var h = 0d;
            var w = 0d;
            foreach (var c in Children)
            {
                if (!c.IsVisible)
                {
                    continue;
                }

                c.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                h += c.DesiredSize.Height;
                w = Math.Max(w, c.DesiredSize.Width);
            }

            SetPages(1, 1, count, double.IsInfinity(availableSize.Width) ? w : availableSize.Width);
            return new Size(double.IsInfinity(availableSize.Width) ? w : availableSize.Width, h);
        }

        var viewport = ViewportWidth;
        if (double.IsNaN(viewport) || viewport <= 0)
        {
            viewport = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        }

        var itemW = 0d;
        var itemH = 0d;
        foreach (var c in Children)
        {
            if (!c.IsVisible)
            {
                continue;
            }

            c.Measure(Size.Infinity);
            itemW = Math.Max(itemW, c.DesiredSize.Width);
            itemH = Math.Max(itemH, c.DesiredSize.Height);
        }

        if (count == 0 || viewport <= 0 || itemW <= 0)
        {
            SetPages(1, 1, Math.Max(1, count), viewport);
            return new Size(viewport, itemH);
        }

        var usable = Math.Max(0, viewport - 2 * LeadingEdge);
        var maxColumns = Math.Max(1, (int)Math.Floor(usable / itemW));
        var rows = Math.Clamp((int)Math.Ceiling(count / (double)maxColumns), 1, Math.Max(1, MaxRows));
        var cols = Math.Min(maxColumns, count);
        var pages = (int)Math.Ceiling(count / (double)(cols * rows));
        SetPages(pages, rows, cols, viewport);
        return new Size(viewport * pages, itemH * rows);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Orientation == Orientation.Vertical)
        {
            var y = 0d;
            foreach (var c in Children)
            {
                if (!c.IsVisible)
                {
                    continue;
                }

                c.Arrange(new Rect(0, y, finalSize.Width, c.DesiredSize.Height));
                y += c.DesiredSize.Height;
            }

            return finalSize;
        }

        var itemW = 0d;
        var itemH = 0d;
        var count = 0;
        foreach (var c in Children)
        {
            if (c.IsVisible)
            {
                itemW = Math.Max(itemW, c.DesiredSize.Width);
                itemH = Math.Max(itemH, c.DesiredSize.Height);
                count++;
            }
        }

        var viewport = _pageWidth;
        var usable = Math.Max(0, viewport - 2 * LeadingEdge);
        var maxColumns = itemW > 0 ? Math.Max(1, (int)Math.Floor(usable / itemW)) : 1;
        var cellW = itemW;
        if (!FixedSize && itemW > 0)
        {
            var extra = usable - _columnsPerPage * itemW;
            var distribute = extra - (maxColumns - _columnsPerPage) * itemW;
            cellW = itemW + Math.Max(0, distribute) / maxColumns;
        }

        var i = 0;
        foreach (var c in Children)
        {
            if (!c.IsVisible)
            {
                continue;
            }

            var column = i / _rowsPerPage;
            var row = i % _rowsPerPage;
            var page = column / _columnsPerPage;
            var colInPage = column % _columnsPerPage;
            var x = page * viewport + LeadingEdge + colInPage * cellW;
            c.Arrange(new Rect(x, row * itemH, cellW, itemH));
            i++;
        }

        return new Size(Math.Max(finalSize.Width, viewport * _pages), Math.Max(finalSize.Height, itemH * _rowsPerPage));
    }

    private void SetPages(int pages, int rows, int cols, double pageWidth)
    {
        var changed = pages != _pages || Math.Abs(pageWidth - _pageWidth) > 0.5;
        _pages = Math.Max(1, pages);
        _rowsPerPage = Math.Max(1, rows);
        _columnsPerPage = Math.Max(1, cols);
        _pageWidth = pageWidth;
        if (changed)
        {
            HorizontalSnapPointsChanged?.Invoke(this, new RoutedEventArgs());
            PagesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
