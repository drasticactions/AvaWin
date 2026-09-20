using Avalonia;
using Avalonia.Controls;

namespace AvaWin;

/// <summary>How a selected list item is drawn.</summary>
public enum SelectionStyle
{
    /// <summary>An accent fill with white content. The default of ListBox. ListView opts in with a class.</summary>
    Filled,

    /// <summary>A 4px accent border with a checkmark triangle in the top right corner. The default of ListView.</summary>
    Bordered,
}

/// <summary>
/// Attached, inherited list style switches. Set one on a <see cref="ListBox"/> or any ancestor, and every item
/// container picks it up: <c>win:ListStyle.SelectionStyle="Bordered"</c>. The style class
/// <c>win-selectionstyle-bordered</c> on a ListBox sets it too.
/// </summary>
public static class ListStyle
{
    /// <summary>Defines the <c>ListStyle.SelectionStyle</c> attached property.</summary>
    public static readonly AttachedProperty<SelectionStyle> SelectionStyleProperty =
        AvaloniaProperty.RegisterAttached<Control, SelectionStyle>("SelectionStyle", typeof(ListStyle), SelectionStyle.Filled, inherits: true);

    /// <summary>Gets the selection style.</summary>
    public static SelectionStyle GetSelectionStyle(Control element) => element.GetValue(SelectionStyleProperty);

    /// <summary>Sets the selection style.</summary>
    public static void SetSelectionStyle(Control element, SelectionStyle value) => element.SetValue(SelectionStyleProperty, value);
}
