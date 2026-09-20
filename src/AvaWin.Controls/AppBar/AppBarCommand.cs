using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvaWin.Controls;

/// <summary>
/// A command in an <see cref="AppBar"/> or <c>NavBar</c>: a ring icon with a label, a toggle, a flyout opener, a
/// separator, or a host for custom content. It implements <see cref="ICommandBarElement"/>, so it also works in
/// Avalonia's <c>CommandBar</c>.
/// </summary>
public class AppBarCommand : Button, ICommandBarElement
{
    /// <summary>Defines the <see cref="Id"/> property.</summary>
    public static readonly StyledProperty<string?> IdProperty = AvaloniaProperty.Register<AppBarCommand, string?>(nameof(Id));

    /// <summary>Defines the <see cref="Type"/> property.</summary>
    public static readonly StyledProperty<AppBarCommandType> TypeProperty = AvaloniaProperty.Register<AppBarCommand, AppBarCommandType>(nameof(Type));

    /// <summary>Defines the <see cref="Label"/> property.</summary>
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<AppBarCommand, string?>(nameof(Label));

    /// <summary>Defines the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<AppBarCommand, object?>(nameof(Icon));

    /// <summary>Defines the <see cref="Section"/> property.</summary>
    public static readonly StyledProperty<AppBarCommandSection> SectionProperty = AvaloniaProperty.Register<AppBarCommand, AppBarCommandSection>(nameof(Section));

    /// <summary>Defines the <see cref="IsSelected"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<AppBarCommand, bool>(nameof(IsSelected), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="FirstElementFocus"/> property.</summary>
    public static readonly StyledProperty<IInputElement?> FirstElementFocusProperty = AvaloniaProperty.Register<AppBarCommand, IInputElement?>(nameof(FirstElementFocus));

    /// <summary>Defines the <see cref="LastElementFocus"/> property.</summary>
    public static readonly StyledProperty<IInputElement?> LastElementFocusProperty = AvaloniaProperty.Register<AppBarCommand, IInputElement?>(nameof(LastElementFocus));

    /// <summary>Defines the <see cref="IsInOverflow"/> property.</summary>
    public static readonly StyledProperty<bool> IsInOverflowProperty = AvaloniaProperty.Register<AppBarCommand, bool>(nameof(IsInOverflow));

    /// <summary>Defines the <see cref="IsCompact"/> property.</summary>
    public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<AppBarCommand, bool>(nameof(IsCompact));

    /// <summary>Defines the <see cref="IconContent"/> property.</summary>
    public static readonly DirectProperty<AppBarCommand, object?> IconContentProperty =
        AvaloniaProperty.RegisterDirect<AppBarCommand, object?>(nameof(IconContent), o => o.IconContent);

    private object? _iconContent;

    static AppBarCommand()
    {
        TypeProperty.Changed.AddClassHandler<AppBarCommand>((c, _) => c.UpdatePseudoClasses());
        IsSelectedProperty.Changed.AddClassHandler<AppBarCommand>((c, _) => c.UpdatePseudoClasses());
        IconProperty.Changed.AddClassHandler<AppBarCommand>((c, _) => c.UpdateIconContent());
        AppBar.IsReducedProperty.Changed.AddClassHandler<AppBarCommand>((c, _) => c.UpdatePseudoClasses());
    }

    /// <summary>Initializes a new instance.</summary>
    public AppBarCommand()
    {
        UpdatePseudoClasses();
    }

    /// <summary>An identifier for <see cref="AppBar.GetCommandById"/>.</summary>
    public string? Id { get => GetValue(IdProperty); set => SetValue(IdProperty, value); }

    /// <summary>What kind of command this is.</summary>
    public AppBarCommandType Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>The text under the icon.</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>
    /// An <see cref="AppBarIcon"/>, a glyph string, an <see cref="IImage"/> sprite of 160×80 pixels, or any
    /// <see cref="Control"/>.
    /// </summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Which group the command belongs to: global (right) or selection (left).</summary>
    public AppBarCommandSection Section { get => GetValue(SectionProperty); set => SetValue(SectionProperty, value); }

    /// <summary>Whether a toggle command is on.</summary>
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    /// <summary>The first focusable element of a content command.</summary>
    public IInputElement? FirstElementFocus { get => GetValue(FirstElementFocusProperty); set => SetValue(FirstElementFocusProperty, value); }

    /// <summary>The last focusable element of a content command.</summary>
    public IInputElement? LastElementFocus { get => GetValue(LastElementFocusProperty); set => SetValue(LastElementFocusProperty, value); }

    /// <inheritdoc/>
    public bool IsInOverflow { get => GetValue(IsInOverflowProperty); set => SetValue(IsInOverflowProperty, value); }

    /// <inheritdoc/>
    public bool IsCompact { get => GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }

    /// <summary>The object that the icon slot shows: a glyph string, a sprite control, or the control given as <see cref="Icon"/>.</summary>
    public object? IconContent
    {
        get => _iconContent;
        private set => SetAndRaise(IconContentProperty, ref _iconContent, value);
    }

    /// <inheritdoc/>
    protected override void OnClick()
    {
        switch (Type)
        {
            case AppBarCommandType.Toggle:
                SetCurrentValue(IsSelectedProperty, !IsSelected);
                break;
            case AppBarCommandType.Flyout when Flyout is { } flyout:
                base.OnClick();
                flyout.ShowAt(this, WinFlyoutPlacement.Auto);
                return;
        }

        base.OnClick();
    }

    private void UpdateIconContent()
    {
        IconContent = Icon switch
        {
            null => null,
            AppBarIcon i => AppBarIcons.Glyph(i),
            string s when AppBarIcons.TryParse(s, out var named) && s.Length > 1 => AppBarIcons.Glyph(named),
            string s => s,
            IImage image => new AppBarSpriteIcon { Source = image, Owner = this },
            var other => other,
        };
    }

    private void UpdatePseudoClasses()
    {
        var t = Type;
        PseudoClasses.Set(":button", t == AppBarCommandType.Button);
        PseudoClasses.Set(":toggle", t == AppBarCommandType.Toggle);
        PseudoClasses.Set(":flyout", t == AppBarCommandType.Flyout);
        PseudoClasses.Set(":separator", t == AppBarCommandType.Separator);
        PseudoClasses.Set(":content", t == AppBarCommandType.Content);
        PseudoClasses.Set(":selected", t == AppBarCommandType.Toggle && IsSelected);
        PseudoClasses.Set(":reduced", AppBar.GetIsReduced(this));
        Focusable = t is not (AppBarCommandType.Separator or AppBarCommandType.Content);
    }
}
