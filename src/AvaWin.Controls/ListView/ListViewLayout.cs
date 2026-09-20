using System;
using Avalonia;
using Avalonia.Layout;

namespace AvaWin.Controls;

/// <summary>The base class of the ListView layouts: <see cref="ListLayout"/>, <see cref="GridLayout"/> and <see cref="CellSpanningLayout"/>.</summary>
public abstract class ListViewLayout : AvaloniaObject
{
    /// <summary>Defines the <see cref="Orientation"/> property.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<ListViewLayout, Orientation>(nameof(Orientation), Orientation.Vertical);

    /// <summary>Defines the <see cref="GroupHeaderPosition"/> property.</summary>
    public static readonly StyledProperty<GroupHeaderPosition> GroupHeaderPositionProperty = AvaloniaProperty.Register<ListViewLayout, GroupHeaderPosition>(nameof(GroupHeaderPosition));

    /// <summary>The scroll direction. ListLayout is vertical by default. GridLayout is horizontal by default.</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Where a group header sits: above its group, or to the left of it.</summary>
    public GroupHeaderPosition GroupHeaderPosition { get => GetValue(GroupHeaderPositionProperty); set => SetValue(GroupHeaderPositionProperty, value); }

    /// <summary>Raised when a property that affects layout changes.</summary>
    public event EventHandler? LayoutChanged;

    /// <summary>The maximum number of items per line. 0 means as many as fit. 1 means a list.</summary>
    internal abstract int MaxItemsPerLine { get; }

    /// <summary>Whether items fill the cross axis (a list) or keep their uniform size (a grid).</summary>
    internal abstract bool StretchCrossAxis { get; }

    /// <summary>Optional: the size of the item at an index.</summary>
    internal virtual Func<int, GridItemInfo>? ItemInfoCallback => null;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>One item per row (vertical) or per column (horizontal). Items stretch across the other axis.</summary>
public sealed class ListLayout : ListViewLayout
{
    internal override int MaxItemsPerLine => 1;

    internal override bool StretchCrossAxis => true;
}

/// <summary>
/// A uniform grid that wraps along the cross axis and scrolls along <see cref="ListViewLayout.Orientation"/>,
/// horizontal by default. With a <see cref="GroupInfo"/> that enables cell spanning, it behaves like
/// <see cref="CellSpanningLayout"/>.
/// </summary>
public sealed class GridLayout : ListViewLayout
{
    /// <summary>Defines the <see cref="MaximumRowsOrColumns"/> property.</summary>
    public static readonly StyledProperty<int> MaximumRowsOrColumnsProperty = AvaloniaProperty.Register<GridLayout, int>(nameof(MaximumRowsOrColumns));

    /// <summary>Defines the <see cref="ItemInfo"/> property.</summary>
    public static readonly StyledProperty<Func<int, GridItemInfo>?> ItemInfoProperty = AvaloniaProperty.Register<GridLayout, Func<int, GridItemInfo>?>(nameof(ItemInfo));

    /// <summary>Defines the <see cref="GroupInfo"/> property.</summary>
    public static readonly StyledProperty<Func<int, GroupInfo>?> GroupInfoProperty = AvaloniaProperty.Register<GridLayout, Func<int, GroupInfo>?>(nameof(GroupInfo));

    static GridLayout()
    {
        OrientationProperty.OverrideDefaultValue<GridLayout>(Orientation.Horizontal);
    }

    /// <summary>The maximum number of rows (horizontal) or columns (vertical). 0 means as many as fit.</summary>
    public int MaximumRowsOrColumns { get => GetValue(MaximumRowsOrColumnsProperty); set => SetValue(MaximumRowsOrColumnsProperty, value); }

    /// <summary>The uniform cell size. When null, the first item is measured.</summary>
    public Func<int, GridItemInfo>? ItemInfo { get => GetValue(ItemInfoProperty); set => SetValue(ItemInfoProperty, value); }

    /// <summary>The cell size of a group. <see cref="Controls.GroupInfo.EnableCellSpanning"/> switches to cell spanning.</summary>
    public Func<int, GroupInfo>? GroupInfo { get => GetValue(GroupInfoProperty); set => SetValue(GroupInfoProperty, value); }

    internal override int MaxItemsPerLine => MaximumRowsOrColumns;

    internal override bool StretchCrossAxis => false;

    internal override Func<int, GridItemInfo>? ItemInfoCallback => ItemInfo;

    internal bool IsCellSpanning => GroupInfo is { } gi && ItemInfo is not null && gi(0).EnableCellSpanning;
}

/// <summary>
/// Items span whole multiples of the cell size of their group. They are packed first-fit along the scroll axis.
/// </summary>
public sealed class CellSpanningLayout : ListViewLayout
{
    /// <summary>Defines the <see cref="MaximumRowsOrColumns"/> property.</summary>
    public static readonly StyledProperty<int> MaximumRowsOrColumnsProperty = AvaloniaProperty.Register<CellSpanningLayout, int>(nameof(MaximumRowsOrColumns));

    /// <summary>Defines the <see cref="ItemInfo"/> property.</summary>
    public static readonly StyledProperty<Func<int, GridItemInfo>?> ItemInfoProperty = AvaloniaProperty.Register<CellSpanningLayout, Func<int, GridItemInfo>?>(nameof(ItemInfo));

    /// <summary>Defines the <see cref="GroupInfo"/> property.</summary>
    public static readonly StyledProperty<Func<int, GroupInfo>?> GroupInfoProperty = AvaloniaProperty.Register<CellSpanningLayout, Func<int, GroupInfo>?>(nameof(GroupInfo));

    static CellSpanningLayout()
    {
        OrientationProperty.OverrideDefaultValue<CellSpanningLayout>(Orientation.Horizontal);
    }

    /// <summary>The maximum number of cells across the scroll axis. 0 means as many as fit.</summary>
    public int MaximumRowsOrColumns { get => GetValue(MaximumRowsOrColumnsProperty); set => SetValue(MaximumRowsOrColumnsProperty, value); }

    /// <summary>Required: the size of the item at an index.</summary>
    public Func<int, GridItemInfo>? ItemInfo { get => GetValue(ItemInfoProperty); set => SetValue(ItemInfoProperty, value); }

    /// <summary>Required: the cell size of a group.</summary>
    public Func<int, GroupInfo>? GroupInfo { get => GetValue(GroupInfoProperty); set => SetValue(GroupInfoProperty, value); }

    internal override int MaxItemsPerLine => MaximumRowsOrColumns;

    internal override bool StretchCrossAxis => false;

    internal override Func<int, GridItemInfo>? ItemInfoCallback => ItemInfo;
}
