using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace AvaWin.Controls;

/// <summary>One item of a Pivot: a header, shown in the header track, and content.</summary>
[TemplatePart("PART_ContentPresenter", typeof(Avalonia.Controls.Presenters.ContentPresenter), IsRequired = true)]
[PseudoClasses(":selected")]
public sealed class PivotItem : HeaderedContentControl
{
    /// <summary>Defines the <see cref="IsSelected"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<PivotItem, bool>(nameof(IsSelected));

    static PivotItem()
    {
        IsSelectedProperty.Changed.AddClassHandler<PivotItem>((i, e) => i.PseudoClasses.Set(":selected", (bool)e.NewValue!));
        FocusableProperty.OverrideDefaultValue<PivotItem>(false);
    }

    /// <summary>Whether this is the selected item of the Pivot.</summary>
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
}

/// <summary>Arguments for <see cref="Pivot.PivotSelectionChanged"/>, <see cref="Pivot.ItemAnimationStart"/>, <see cref="Pivot.ItemAnimationEnd"/> and <see cref="Pivot.ItemLoaded"/>.</summary>
public sealed class PivotSelectionChangedEventArgs : Avalonia.Interactivity.RoutedEventArgs
{
    internal PivotSelectionChangedEventArgs(Avalonia.Interactivity.RoutedEvent routedEvent, int index, PivotItem? item) : base(routedEvent)
    {
        Index = index;
        Item = item;
    }

    /// <summary>The selected index.</summary>
    public int Index { get; }

    /// <summary>The selected item.</summary>
    public PivotItem? Item { get; }
}
