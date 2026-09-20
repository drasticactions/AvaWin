using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace AvaWin.Controls;

/// <summary>
/// The visible part of an <see cref="AppBar"/>, hosted in the overlay layer. Its ControlTheme carries the bar
/// template. The owning bar mirrors its state pseudo-classes here. It also works on its own, as an inline
/// command bar in the page.
/// </summary>
[TemplatePart("PART_Root", typeof(Border))]
[TemplatePart("PART_Body", typeof(Grid))]
[TemplatePart("PART_CommandsHost", typeof(AppBarCommandsPanel))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_InvokeButton", typeof(Button), IsRequired = true)]
[PseudoClasses(":open", ":closed", ":opening", ":closing", ":top", ":bottom", ":minimal", ":compact", ":none", ":reduced", ":sticky", ":commands", ":custom")]
public class AppBarPresenter : ContentControl
{
    /// <summary>Defines the <see cref="IsOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<AppBarPresenter, bool>(nameof(IsOpen), true);

    /// <summary>Defines the <see cref="Placement"/> property.</summary>
    public static readonly StyledProperty<AppBarPlacement> PlacementProperty = AvaloniaProperty.Register<AppBarPresenter, AppBarPlacement>(nameof(Placement), AppBarPlacement.Bottom);

    /// <summary>Defines the <see cref="Layout"/> property.</summary>
    public static readonly StyledProperty<AppBarLayout> LayoutProperty = AvaloniaProperty.Register<AppBarPresenter, AppBarLayout>(nameof(Layout), AppBarLayout.Commands);

    /// <summary>Defines the <see cref="ClosedDisplayMode"/> property.</summary>
    public static readonly StyledProperty<AppBarClosedDisplayMode> ClosedDisplayModeProperty = AvaloniaProperty.Register<AppBarPresenter, AppBarClosedDisplayMode>(nameof(ClosedDisplayMode), AppBarClosedDisplayMode.Minimal);

    /// <summary>Defines the <see cref="IsSticky"/> property.</summary>
    public static readonly StyledProperty<bool> IsStickyProperty = AvaloniaProperty.Register<AppBarPresenter, bool>(nameof(IsSticky));

    /// <summary>Defines the <see cref="SafeAreaPadding"/> property.</summary>
    public static readonly StyledProperty<Thickness> SafeAreaPaddingProperty = AvaloniaProperty.Register<AppBarPresenter, Thickness>(nameof(SafeAreaPadding));

    private AppBarCommandsPanel? _commandsPanel;
    private Button? _invokeButton;

    static AppBarPresenter()
    {
        IsOpenProperty.Changed.AddClassHandler<AppBarPresenter>((p, _) => p.UpdatePseudoClasses());
        PlacementProperty.Changed.AddClassHandler<AppBarPresenter>((p, _) => p.UpdatePseudoClasses());
        LayoutProperty.Changed.AddClassHandler<AppBarPresenter>((p, _) => p.UpdatePseudoClasses());
        ClosedDisplayModeProperty.Changed.AddClassHandler<AppBarPresenter>((p, _) => p.UpdatePseudoClasses());
        IsStickyProperty.Changed.AddClassHandler<AppBarPresenter>((p, _) => p.UpdatePseudoClasses());
    }

    /// <summary>Initializes a new instance.</summary>
    public AppBarPresenter()
    {
        Commands = new AvaloniaList<AppBarCommand>();
        Commands.CollectionChanged += OnCommandsChanged;
        UpdatePseudoClasses();
    }

    /// <summary>Open (the full bar) or closed (the closed display mode strip). Mirrors <see cref="AppBar.IsOpen"/>. An inline presenter is open by default.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Mirrors <see cref="AppBar.Placement"/>.</summary>
    public AppBarPlacement Placement { get => GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>Mirrors <see cref="AppBar.Layout"/>.</summary>
    public AppBarLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>Mirrors <see cref="AppBar.ClosedDisplayMode"/>.</summary>
    public AppBarClosedDisplayMode ClosedDisplayMode { get => GetValue(ClosedDisplayModeProperty); set => SetValue(ClosedDisplayModeProperty, value); }

    /// <summary>Mirrors <see cref="AppBar.IsSticky"/>.</summary>
    public bool IsSticky { get => GetValue(IsStickyProperty); set => SetValue(IsStickyProperty, value); }

    /// <summary>The safe-area inset on the edge of the bar. It pads <c>PART_Root</c>, so the background reaches the screen edge and the commands stay clear. The owning <see cref="AppBar"/> sets it.</summary>
    public Thickness SafeAreaPadding { get => GetValue(SafeAreaPaddingProperty); set => SetValue(SafeAreaPaddingProperty, value); }

    /// <summary>The commands laid out by <c>PART_CommandsHost</c>.</summary>
    [Avalonia.Metadata.Content]
    public AvaloniaList<AppBarCommand> Commands { get; }

    /// <summary>The bar this presenter belongs to, or null for an inline presenter.</summary>
    public AppBar? Owner { get; internal set; }

    internal AppBarCommandsPanel? CommandsPanel => _commandsPanel;

    internal void SetTransition(bool opening, bool closing)
    {
        PseudoClasses.Set(":opening", opening);
        PseudoClasses.Set(":closing", closing);
    }

    private void UpdatePseudoClasses()
    {
        var open = IsOpen;
        PseudoClasses.Set(":open", open);
        PseudoClasses.Set(":closed", !open);
        PseudoClasses.Set(":top", Placement == AppBarPlacement.Top);
        PseudoClasses.Set(":bottom", Placement == AppBarPlacement.Bottom);
        PseudoClasses.Set(":minimal", ClosedDisplayMode == AppBarClosedDisplayMode.Minimal);
        PseudoClasses.Set(":compact", ClosedDisplayMode == AppBarClosedDisplayMode.Compact);
        PseudoClasses.Set(":none", ClosedDisplayMode == AppBarClosedDisplayMode.None);
        PseudoClasses.Set(":sticky", IsSticky);
        PseudoClasses.Set(":commands", Layout == AppBarLayout.Commands);
        PseudoClasses.Set(":custom", Layout == AppBarLayout.Custom);
        if (_commandsPanel is { } panel)
        {
            panel.ForceReduced = !open && ClosedDisplayMode == AppBarClosedDisplayMode.Compact;
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_commandsPanel is { } old)
        {
            old.ReducedChanged -= OnReducedChanged;
            old.Children.Clear();
        }

        if (_invokeButton is { } oldButton)
        {
            oldButton.Click -= OnInvokeClick;
        }

        _commandsPanel = e.NameScope.Find<AppBarCommandsPanel>("PART_CommandsHost");
        _invokeButton = e.NameScope.Find<Button>("PART_InvokeButton");
        if (_commandsPanel is { } panel)
        {
            panel.ReducedChanged += OnReducedChanged;
        }

        if (_invokeButton is { } button)
        {
            button.Click += OnInvokeClick;
        }

        SyncCommands();
        UpdatePseudoClasses();
        UpdateReduced();
    }

    private void OnCommandsChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncCommands();

    private void SyncCommands()
    {
        if (_commandsPanel is null)
        {
            return;
        }

        _commandsPanel.Children.Clear();
        foreach (var c in Commands)
        {
            if (c.Parent is Panel p && p != _commandsPanel)
            {
                p.Children.Remove(c);
            }

            _commandsPanel.Children.Add(c);
        }
    }

    internal void UpdateReduced()
    {
        PseudoClasses.Set(":reduced", _commandsPanel?.IsReduced == true);
    }

    private void OnReducedChanged(object? sender, System.EventArgs e)
    {
        UpdateReduced();
        Owner?.UpdatePseudoClasses();
    }

    private void OnInvokeClick(object? sender, RoutedEventArgs e)
    {
        if (Owner is { } owner)
        {
            owner.IsOpen = !owner.IsOpen;
        }
        else
        {
            SetCurrentValue(IsOpenProperty, !IsOpen);
        }
    }
}
