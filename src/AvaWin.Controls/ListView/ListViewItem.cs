using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;
using Avalonia.Input;

namespace AvaWin.Controls;

/// <summary>
/// The container of one item: the selection border or fill, the corner checkmark, a 3px hover outline and a 2px
/// focus outline. <see cref="ListView"/> creates one for every item.
/// </summary>
[TemplatePart("PART_Container", typeof(Border))]
[TemplatePart("PART_SelectionBackground", typeof(Border))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter), IsRequired = true)]
[TemplatePart("PART_SelectionBorder", typeof(Border))]
[TemplatePart("PART_Checkmark", typeof(Panel))]
[TemplatePart("PART_HoverOutline", typeof(Border))]
[TemplatePart("PART_FocusOutline", typeof(Border))]
[PseudoClasses(":selected", ":pressed", ":backdrop", ":nonselectable", ":dragsource", ":dragover", ":selectionmode")]
public class ListViewItem : ContentControl, ISelectable
{
    /// <summary>Defines the <see cref="IsSelected"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty = Avalonia.Controls.Primitives.SelectingItemsControl.IsSelectedProperty.AddOwner<ListViewItem>();

    /// <summary>Defines the <see cref="IsSelectable"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectableProperty = AvaloniaProperty.Register<ListViewItem, bool>(nameof(IsSelectable), true);

    /// <summary>Defines the <see cref="IsBackdrop"/> property.</summary>
    public static readonly StyledProperty<bool> IsBackdropProperty = AvaloniaProperty.Register<ListViewItem, bool>(nameof(IsBackdrop));

    static ListViewItem()
    {
        SelectableMixin.Attach<ListViewItem>(IsSelectedProperty);
        PressedMixin.Attach<ListViewItem>();
        FocusableProperty.OverrideDefaultValue<ListViewItem>(true);
        IsSelectableProperty.Changed.AddClassHandler<ListViewItem>((i, e) => i.PseudoClasses.Set(":nonselectable", !(bool)e.NewValue!));
        IsBackdropProperty.Changed.AddClassHandler<ListViewItem>((i, e) => i.PseudoClasses.Set(":backdrop", (bool)e.NewValue!));
    }

    /// <summary>Whether the item is selected.</summary>
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    /// <summary>Whether the item can be selected.</summary>
    public bool IsSelectable { get => GetValue(IsSelectableProperty); set => SetValue(IsSelectableProperty, value); }

    /// <summary>Whether the item shows the placeholder look while its content loads.</summary>
    public bool IsBackdrop { get => GetValue(IsBackdropProperty); set => SetValue(IsBackdropProperty, value); }

    internal void SetDragState(bool source, bool over)
    {
        PseudoClasses.Set(":dragsource", source);
        PseudoClasses.Set(":dragover", over);
    }

    internal void SetSelectionModeActive(bool active) => PseudoClasses.Set(":selectionmode", active);
}
