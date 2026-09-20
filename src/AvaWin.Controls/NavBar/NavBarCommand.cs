using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvaWin.Controls;

/// <summary>
/// A wide button with a 40×40 glyph and a label, and an optional split button that toggles
/// <see cref="SplitOpened"/>. <see cref="Location"/> and <see cref="State"/> are the navigation data that
/// <see cref="NavBarContainer.Invoked"/> carries.
/// </summary>
[TemplatePart("PART_Button", typeof(Button), IsRequired = true)]
[TemplatePart("PART_SplitButton", typeof(Button))]
[TemplatePart("PART_Icon", typeof(ContentPresenter))]
[TemplatePart("PART_Label", typeof(TextBlock))]
[PseudoClasses(":split", ":splitopened", ":hasicon", ":content")]
public class NavBarCommand : TemplatedControl
{
    /// <summary>Defines the <see cref="Label"/> property.</summary>
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<NavBarCommand, string?>(nameof(Label));

    /// <summary>Defines the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<NavBarCommand, object?>(nameof(Icon));

    /// <summary>Defines the <see cref="Location"/> property.</summary>
    public static readonly StyledProperty<string?> LocationProperty = AvaloniaProperty.Register<NavBarCommand, string?>(nameof(Location));

    /// <summary>Defines the <see cref="State"/> property.</summary>
    public static readonly StyledProperty<object?> StateProperty = AvaloniaProperty.Register<NavBarCommand, object?>(nameof(State));

    /// <summary>Defines the <see cref="SplitButton"/> property.</summary>
    public static readonly StyledProperty<bool> SplitButtonProperty = AvaloniaProperty.Register<NavBarCommand, bool>(nameof(SplitButton));

    /// <summary>Defines the <see cref="SplitOpened"/> property.</summary>
    public static readonly StyledProperty<bool> SplitOpenedProperty = AvaloniaProperty.Register<NavBarCommand, bool>(nameof(SplitOpened), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="Command"/> property.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<NavBarCommand, ICommand?>(nameof(Command));

    /// <summary>Defines the <see cref="CommandParameter"/> property.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty.Register<NavBarCommand, object?>(nameof(CommandParameter));

    /// <summary>Defines the <see cref="Content"/> property (data-templated commands).</summary>
    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<NavBarCommand, object?>(nameof(Content));

    /// <summary>Defines the <see cref="ContentTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty = AvaloniaProperty.Register<NavBarCommand, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Defines the <see cref="IconContent"/> property.</summary>
    public static readonly DirectProperty<NavBarCommand, object?> IconContentProperty =
        AvaloniaProperty.RegisterDirect<NavBarCommand, object?>(nameof(IconContent), o => o.IconContent);

    /// <summary>Defines the <see cref="Click"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = RoutedEvent.Register<NavBarCommand, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="SplitToggle"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> SplitToggleEvent = RoutedEvent.Register<NavBarCommand, RoutedEventArgs>(nameof(SplitToggle), RoutingStrategies.Bubble);

    private object? _iconContent;
    private Button? _button;
    private Button? _split;

    static NavBarCommand()
    {
        IconProperty.Changed.AddClassHandler<NavBarCommand>((c, _) => c.UpdateIconContent());
        SplitButtonProperty.Changed.AddClassHandler<NavBarCommand>((c, _) => c.UpdatePseudoClasses());
        SplitOpenedProperty.Changed.AddClassHandler<NavBarCommand>((c, _) => c.UpdatePseudoClasses());
        ContentProperty.Changed.AddClassHandler<NavBarCommand>((c, _) => c.UpdatePseudoClasses());
        FocusableProperty.OverrideDefaultValue<NavBarCommand>(false);
    }

    /// <summary>Initializes a new instance.</summary>
    public NavBarCommand()
    {
        UpdatePseudoClasses();
    }

    /// <summary>The text beside the icon.</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>An <see cref="AppBarIcon"/>, a glyph string or a <see cref="Control"/>.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>The navigation target.</summary>
    public string? Location { get => GetValue(LocationProperty); set => SetValue(LocationProperty, value); }

    /// <summary>The navigation payload.</summary>
    public object? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }

    /// <summary>Whether the chevron split button is shown.</summary>
    public bool SplitButton { get => GetValue(SplitButtonProperty); set => SetValue(SplitButtonProperty, value); }

    /// <summary>Whether the split button is in its open state.</summary>
    public bool SplitOpened { get => GetValue(SplitOpenedProperty); set => SetValue(SplitOpenedProperty, value); }

    /// <summary>The command that the main button executes.</summary>
    public ICommand? Command { get => GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>The parameter for <see cref="Command"/>.</summary>
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    /// <summary>Templated content shown instead of the icon and label. <see cref="NavBarContainer"/> sets it for data items.</summary>
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Template for <see cref="Content"/>.</summary>
    public IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>The object that the icon slot shows.</summary>
    public object? IconContent { get => _iconContent; private set => SetAndRaise(IconContentProperty, ref _iconContent, value); }

    /// <summary>Raised when the main button is clicked.</summary>
    public event EventHandler<RoutedEventArgs> Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }

    /// <summary>Raised after the split button toggles <see cref="SplitOpened"/>.</summary>
    public event EventHandler<RoutedEventArgs> SplitToggle { add => AddHandler(SplitToggleEvent, value); remove => RemoveHandler(SplitToggleEvent, value); }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_button is { } ob)
        {
            ob.Click -= OnButtonClick;
        }

        if (_split is { } os)
        {
            os.Click -= OnSplitClick;
        }

        _button = e.NameScope.Find<Button>("PART_Button");
        _split = e.NameScope.Find<Button>("PART_SplitButton");
        if (_button is { } b)
        {
            b.Click += OnButtonClick;
        }

        if (_split is { } s)
        {
            s.Click += OnSplitClick;
        }
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(ClickEvent));
        if (Command is { } cmd && cmd.CanExecute(CommandParameter))
        {
            cmd.Execute(CommandParameter);
        }
    }

    private void OnSplitClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        SetCurrentValue(SplitOpenedProperty, !SplitOpened);
        RaiseEvent(new RoutedEventArgs(SplitToggleEvent));
    }

    private void UpdateIconContent()
    {
        IconContent = Icon switch
        {
            null => null,
            AppBarIcon i => AppBarIcons.Glyph(i),
            string s when s.Length > 1 && AppBarIcons.TryParse(s, out var named) => AppBarIcons.Glyph(named),
            var other => other,
        };
        UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":split", SplitButton);
        PseudoClasses.Set(":splitopened", SplitButton && SplitOpened);
        PseudoClasses.Set(":hasicon", Icon is not null);
        PseudoClasses.Set(":content", Content is not null);
    }
}

/// <summary>Arguments for <see cref="NavBarContainer.Invoked"/>.</summary>
public sealed class NavBarInvokedEventArgs : RoutedEventArgs
{
    internal NavBarInvokedEventArgs(RoutedEvent routedEvent, int index, NavBarCommand command, object? data) : base(routedEvent)
    {
        Index = index;
        NavBarCommand = command;
        Data = data;
    }

    /// <summary>The index of the command in the container.</summary>
    public int Index { get; }

    /// <summary>The command.</summary>
    public NavBarCommand NavBarCommand { get; }

    /// <summary>The data item, or the command itself when the items are commands.</summary>
    public object? Data { get; }
}

/// <summary>Arguments for <see cref="NavBarContainer.SplitToggle"/>.</summary>
public sealed class NavBarSplitToggleEventArgs : RoutedEventArgs
{
    internal NavBarSplitToggleEventArgs(RoutedEvent routedEvent, int index, NavBarCommand command, object? data, bool opened) : base(routedEvent)
    {
        Index = index;
        NavBarCommand = command;
        Data = data;
        Opened = opened;
    }

    /// <summary>The index of the command in the container.</summary>
    public int Index { get; }

    /// <summary>The command.</summary>
    public NavBarCommand NavBarCommand { get; }

    /// <summary>The data item.</summary>
    public object? Data { get; }

    /// <summary>The new <see cref="NavBarCommand.SplitOpened"/> value.</summary>
    public bool Opened { get; }
}
