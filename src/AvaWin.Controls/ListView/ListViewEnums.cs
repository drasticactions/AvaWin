using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaWin.Controls;

/// <summary>How many items a ListView can select.</summary>
public enum ListSelectionMode
{
    /// <summary>No selection.</summary>
    None,

    /// <summary>One item at a time.</summary>
    Single,

    /// <summary>Any number of items (default).</summary>
    Multi,
}

/// <summary>What a tap on an item does.</summary>
public enum TapBehavior
{
    /// <summary>A tap invokes and selects the item. In Multi mode, Ctrl toggles and Shift selects a range. Keyboard focus moves the selection.</summary>
    DirectSelect,

    /// <summary>A tap invokes the item and toggles its selection. Keyboard focus does not select.</summary>
    ToggleSelect,

    /// <summary>A tap only invokes the item (default).</summary>
    InvokeOnly,

    /// <summary>A tap does nothing.</summary>
    None,
}

/// <summary>What a touch swipe across the scroll axis of a <see cref="ListView"/> does.</summary>
public enum SwipeBehavior
{
    /// <summary>A swipe selects the item, and a longer one reorders it.</summary>
    Select,

    /// <summary>A swipe does nothing.</summary>
    None,
}

/// <summary>What a tap on a group header does.</summary>
public enum GroupHeaderTapBehavior
{
    /// <summary>A tap raises <see cref="ListView.GroupHeaderInvoked"/>.</summary>
    Invoke,

    /// <summary>A tap does nothing.</summary>
    None,
}

/// <summary>How far a ListView has loaded.</summary>
public enum ListViewLoadingState
{
    /// <summary>The list realizes items.</summary>
    ItemsLoading,

    /// <summary>The items in the viewport are realized.</summary>
    ViewPortLoaded,

    /// <summary>All requested items are realized.</summary>
    ItemsLoaded,

    /// <summary>The entrance animation is finished.</summary>
    Complete,
}

/// <summary>The selection chrome of a ListView item.</summary>
public enum ListViewSelectionStyle
{
    /// <summary>A 4px accent border and a corner checkmark (default).</summary>
    Bordered,

    /// <summary>An accent fill.</summary>
    Filled,
}

/// <summary>Where group headers sit relative to their group.</summary>
public enum GroupHeaderPosition
{
    /// <summary>Above the group.</summary>
    Top,

    /// <summary>To the left of the group.</summary>
    Left,
}

/// <summary>What a <see cref="ListViewEntity"/> points at.</summary>
public enum ListViewEntityType
{
    /// <summary>An item.</summary>
    Item,

    /// <summary>A group header.</summary>
    GroupHeader,
}

/// <summary>An item or a group header, by index.</summary>
public readonly record struct ListViewEntity(ListViewEntityType Type, int Index);

/// <summary>The size of a grid layout item.</summary>
public readonly record struct GridItemInfo(double Width, double Height);

/// <summary>The cell size of a group, and whether items can span cells.</summary>
public readonly record struct GroupInfo(bool EnableCellSpanning, double CellWidth, double CellHeight);

/// <summary>
/// An items source that can fetch more items on demand.
/// </summary>
public interface ISupportIncrementalLoading
{
    /// <summary>Whether another page can be requested.</summary>
    bool HasMoreItems { get; }

    /// <summary>Loads up to <paramref name="count"/> more items and returns how many were added.</summary>
    Task<int> LoadMoreItemsAsync(int count);
}

/// <summary>A view that can take part in a SemanticZoom.</summary>
public interface IZoomableView
{
    /// <summary>The control that renders the view.</summary>
    Control Element { get; }

    /// <summary>Called when a zoom begins.</summary>
    void BeginZoom();

    /// <summary>Called when a zoom ends. <paramref name="isCurrentView"/> tells whether this view is now shown.</summary>
    void EndZoom(bool isCurrentView);

    /// <summary>The item under the zoom center and its position, or null.</summary>
    (object? Item, Rect Position)? GetCurrentItem();

    /// <summary>A position hint from the other view.</summary>
    void SetCurrentItem(double x, double y);

    /// <summary>Aligns to the item chosen in the other view.</summary>
    void PositionItem(object? item, Rect position);

    /// <summary>Configures the view for its role in the zoom.</summary>
    void ConfigureForZoom(bool isZoomedOut, bool isCurrentView, Action triggerZoom, int prefetchedPages);

    /// <summary>Receives pointer events for the pinch gesture.</summary>
    void HandlePointer(PointerEventArgs e);
}
