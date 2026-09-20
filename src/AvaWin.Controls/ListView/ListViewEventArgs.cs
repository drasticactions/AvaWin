using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaWin.Controls;

/// <summary>Arguments for <see cref="ListView.ItemInvoked"/>.</summary>
public sealed class ListViewItemInvokedEventArgs : RoutedEventArgs
{
    internal ListViewItemInvokedEventArgs(RoutedEvent routedEvent, int index, object? item, ListViewItem container) : base(routedEvent)
    {
        Index = index;
        Item = item;
        Container = container;
    }

    /// <summary>The item index.</summary>
    public int Index { get; }

    /// <summary>The data item.</summary>
    public object? Item { get; }

    /// <summary>The item container.</summary>
    public ListViewItem Container { get; }
}

/// <summary>Arguments for <see cref="ListView.GroupHeaderInvoked"/>.</summary>
public sealed class ListViewGroupHeaderInvokedEventArgs : RoutedEventArgs
{
    internal ListViewGroupHeaderInvokedEventArgs(RoutedEvent routedEvent, int groupIndex, ListViewGroupInfo group, ListViewGroupHeader header) : base(routedEvent)
    {
        GroupIndex = groupIndex;
        Group = group;
        Header = header;
    }

    /// <summary>The group index.</summary>
    public int GroupIndex { get; }

    /// <summary>The group.</summary>
    public ListViewGroupInfo Group { get; }

    /// <summary>The header control.</summary>
    public ListViewGroupHeader Header { get; }
}

/// <summary>Arguments for <see cref="ListView.SelectionChanging"/>. Cancelable. Raised for changes made by the user.</summary>
public sealed class ListViewSelectionChangingEventArgs : RoutedEventArgs
{
    internal ListViewSelectionChangingEventArgs(RoutedEvent routedEvent, IReadOnlyList<int> oldSelection, IReadOnlyList<int> newSelection) : base(routedEvent)
    {
        OldSelection = oldSelection;
        NewSelection = newSelection;
    }

    /// <summary>The indexes that are selected before the change.</summary>
    public IReadOnlyList<int> OldSelection { get; }

    /// <summary>The indexes that will be selected.</summary>
    public IReadOnlyList<int> NewSelection { get; }

    /// <summary>Set to true to keep the old selection.</summary>
    public bool Cancel { get; set; }
}

/// <summary>Arguments for <see cref="ListView.KeyboardNavigating"/>.</summary>
public sealed class ListViewKeyboardNavigatingEventArgs : RoutedEventArgs
{
    internal ListViewKeyboardNavigatingEventArgs(RoutedEvent routedEvent, int oldFocus, int newFocus) : base(routedEvent)
    {
        OldFocus = oldFocus;
        NewFocus = newFocus;
    }

    /// <summary>The index that had focus.</summary>
    public int OldFocus { get; }

    /// <summary>The index that will get focus.</summary>
    public int NewFocus { get; }
}

/// <summary>Arguments for <see cref="ListView.ContentAnimating"/> (cancelable).</summary>
public sealed class ListViewContentAnimatingEventArgs : RoutedEventArgs
{
    internal ListViewContentAnimatingEventArgs(RoutedEvent routedEvent, string type) : base(routedEvent)
    {
        Type = type;
    }

    /// <summary>The animation: <c>entrance</c> or <c>contentTransition</c>.</summary>
    public string Type { get; }

    /// <summary>Set to true to skip the animation.</summary>
    public bool Cancel { get; set; }
}

/// <summary>The payload placed in the drag <see cref="Avalonia.Input.DataTransfer"/> by a dragging ListView.</summary>
public sealed record ListViewDragData(ListView Source, IReadOnlyList<int> Indexes, IReadOnlyList<object?> Items);

/// <summary>Arguments for the ListView drag events.</summary>
public sealed class ListViewDragEventArgs : RoutedEventArgs
{
    internal ListViewDragEventArgs(RoutedEvent routedEvent, IReadOnlyList<int> indexes, IReadOnlyList<object?> items, Avalonia.Input.IDataTransfer? dataTransfer, int insertIndex) : base(routedEvent)
    {
        Indexes = indexes;
        Items = items;
        DataTransfer = dataTransfer;
        InsertIndex = insertIndex;
    }

    /// <summary>The indexes of the dragged items. Empty for external data.</summary>
    public IReadOnlyList<int> Indexes { get; }

    /// <summary>The dragged items. Empty for external data.</summary>
    public IReadOnlyList<object?> Items { get; }

    /// <summary>The data being dragged.</summary>
    public Avalonia.Input.IDataTransfer? DataTransfer { get; }

    /// <summary>Where the items would be inserted, or −1 when the pointer is not over the list.</summary>
    public int InsertIndex { get; }

    /// <summary>Set to true on <c>ItemDragStart</c> to cancel the drag, or on <c>ItemDragDrop</c> to stop the built-in reorder.</summary>
    public bool Cancel { get; set; }
}
