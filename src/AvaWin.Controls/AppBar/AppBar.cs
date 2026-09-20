using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using AvaWin.Animations;
using AvaWin.Controls.Primitives;

namespace AvaWin.Controls;

/// <summary>
/// A command bar docked to an edge of the window, shown over the page in the <see cref="OverlayLayer"/> of the
/// TopLevel. It closes on light dismiss, can leave a strip visible while closed, and switches to a reduced layout
/// when the window is narrow. The element declared in the page is a zero-size placeholder. <see cref="Bar"/> is
/// the visible bar. This is not a <c>ContentControl</c>, because the presenter owns the content and a
/// ContentControl placeholder would claim it as a logical child too.
/// </summary>
[PseudoClasses(":open", ":closed", ":opening", ":closing", ":top", ":bottom", ":minimal", ":compact", ":reduced", ":sticky")]
public class AppBar : TemplatedControl
{
    /// <summary>Attached property that marks commands hosted by a reduced bar.</summary>
    public static readonly AttachedProperty<bool> IsReducedProperty =
        AvaloniaProperty.RegisterAttached<AppBar, Control, bool>("IsReduced");

    /// <summary>Defines the <see cref="Content"/> property (used when <see cref="Layout"/> is <see cref="AppBarLayout.Custom"/>).</summary>
    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<AppBar, object?>(nameof(Content));

    /// <summary>Defines the <see cref="ContentTemplate"/> property.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<AppBar, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Defines the <see cref="Placement"/> property.</summary>
    public static readonly StyledProperty<AppBarPlacement> PlacementProperty =
        AvaloniaProperty.Register<AppBar, AppBarPlacement>(nameof(Placement), AppBarPlacement.Bottom);

    /// <summary>Defines the <see cref="Layout"/> property.</summary>
    public static readonly StyledProperty<AppBarLayout> LayoutProperty =
        AvaloniaProperty.Register<AppBar, AppBarLayout>(nameof(Layout), AppBarLayout.Commands);

    /// <summary>Defines the <see cref="ClosedDisplayMode"/> property.</summary>
    public static readonly StyledProperty<AppBarClosedDisplayMode> ClosedDisplayModeProperty =
        AvaloniaProperty.Register<AppBar, AppBarClosedDisplayMode>(nameof(ClosedDisplayMode), AppBarClosedDisplayMode.Minimal);

    /// <summary>Defines the <see cref="IsSticky"/> property.</summary>
    public static readonly StyledProperty<bool> IsStickyProperty = AvaloniaProperty.Register<AppBar, bool>(nameof(IsSticky));

    /// <summary>Defines the <see cref="IsOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<AppBar, bool>(nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="IsRightClickToggleEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsRightClickToggleEnabledProperty =
        AvaloniaProperty.Register<AppBar, bool>(nameof(IsRightClickToggleEnabled), true);

    /// <summary>Defines the <see cref="IgnoreSafeArea"/> property.</summary>
    public static readonly StyledProperty<bool> IgnoreSafeAreaProperty =
        AvaloniaProperty.Register<AppBar, bool>(nameof(IgnoreSafeArea));

    /// <summary>Defines the <see cref="Opening"/> event.</summary>
    public static readonly RoutedEvent<CancelRoutedEventArgs> OpeningEvent = RoutedEvent.Register<AppBar, CancelRoutedEventArgs>(nameof(Opening), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="Opened"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent = RoutedEvent.Register<AppBar, RoutedEventArgs>(nameof(Opened), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="Closing"/> event.</summary>
    public static readonly RoutedEvent<CancelRoutedEventArgs> ClosingEvent = RoutedEvent.Register<AppBar, CancelRoutedEventArgs>(nameof(Closing), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="Closed"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent = RoutedEvent.Register<AppBar, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);

    private readonly AppBarPresenter _presenter;
    private readonly EdgeOverlayHost _host;
    private TopLevel? _topLevel;
    private int _generation;
    private bool _reverting;
    private bool _closing;

    static AppBar()
    {
        PlacementProperty.Changed.AddClassHandler<AppBar>((b, _) => b.UpdatePseudoClasses());
        ClosedDisplayModeProperty.Changed.AddClassHandler<AppBar>((b, _) =>
        {
            b.UpdatePseudoClasses();
            b.ApplyState(animate: false);
        });
        IsStickyProperty.Changed.AddClassHandler<AppBar>((b, _) => b.UpdatePseudoClasses());
        LayoutProperty.Changed.AddClassHandler<AppBar>((b, _) => b.UpdatePseudoClasses());
        IsOpenProperty.Changed.AddClassHandler<AppBar>((b, e) => b.OnIsOpenChanged((bool)e.NewValue!));
        IsEnabledProperty.Changed.AddClassHandler<AppBar>((b, _) => b._presenter.IsEnabled = b.IsEnabled);
        IgnoreSafeAreaProperty.Changed.AddClassHandler<AppBar>((b, _) => b._host.RespectsSafeArea = !b.IgnoreSafeArea);
    }

    /// <summary>Initializes a new instance.</summary>
    public AppBar()
    {
        _presenter = CreatePresenter();
        _presenter.Owner = this;
        _host = new EdgeOverlayHost(_presenter, Placement == AppBarPlacement.Top ? Dock.Top : Dock.Bottom);
        _host.LightDismissRequested += (_, _) => Close();
        _host.SafeAreaPaddingChanged += (_, _) => _presenter.SafeAreaPadding = _host.SafeAreaPadding;
        Focusable = false;
        IsVisible = true;
        UpdatePseudoClasses();
    }

    /// <summary>Gets the attached <see cref="IsReducedProperty"/> value.</summary>
    public static bool GetIsReduced(Control c) => c.GetValue(IsReducedProperty);

    /// <summary>Sets the attached <see cref="IsReducedProperty"/> value.</summary>
    public static void SetIsReduced(Control c, bool value) => c.SetValue(IsReducedProperty, value);

    /// <summary>The commands shown when <see cref="Layout"/> is <see cref="AppBarLayout.Commands"/>.</summary>
    [Content]
    public AvaloniaList<AppBarCommand> Commands => _presenter.Commands;

    /// <summary>Custom content shown when <see cref="Layout"/> is <see cref="AppBarLayout.Custom"/>.</summary>
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Template for <see cref="Content"/>.</summary>
    public Avalonia.Controls.Templates.IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>The edge the bar docks to.</summary>
    public AppBarPlacement Placement { get => GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>Whether the bar shows <see cref="Commands"/> or <see cref="Content"/>.</summary>
    public AppBarLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>What stays visible while the bar is closed.</summary>
    public AppBarClosedDisplayMode ClosedDisplayMode { get => GetValue(ClosedDisplayModeProperty); set => SetValue(ClosedDisplayModeProperty, value); }

    /// <summary>Whether the bar stays open on a click outside it.</summary>
    public bool IsSticky { get => GetValue(IsStickyProperty); set => SetValue(IsStickyProperty, value); }

    /// <summary>Whether the bar is open. Two-way.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Whether a right-click on the page opens and closes a non-sticky bar.</summary>
    public bool IsRightClickToggleEnabled { get => GetValue(IsRightClickToggleEnabledProperty); set => SetValue(IsRightClickToggleEnabledProperty, value); }

    /// <summary>Whether the bar docks to the physical screen edge instead of the safe area (status bar, notch, home indicator). Default false.</summary>
    public bool IgnoreSafeArea { get => GetValue(IgnoreSafeAreaProperty); set => SetValue(IgnoreSafeAreaProperty, value); }

    /// <summary>Raised before the bar opens. Cancelable.</summary>
    public event EventHandler<CancelRoutedEventArgs> Opening { add => AddHandler(OpeningEvent, value); remove => RemoveHandler(OpeningEvent, value); }

    /// <summary>Raised after the bar has opened.</summary>
    public event EventHandler<RoutedEventArgs> Opened { add => AddHandler(OpenedEvent, value); remove => RemoveHandler(OpenedEvent, value); }

    /// <summary>Raised before the bar closes. Cancelable.</summary>
    public event EventHandler<CancelRoutedEventArgs> Closing { add => AddHandler(ClosingEvent, value); remove => RemoveHandler(ClosingEvent, value); }

    /// <summary>Raised after the bar has closed.</summary>
    public event EventHandler<RoutedEventArgs> Closed { add => AddHandler(ClosedEvent, value); remove => RemoveHandler(ClosedEvent, value); }

    /// <summary>The visible bar, hosted in the overlay layer.</summary>
    public AppBarPresenter Bar => _presenter;

    /// <summary>Opens the bar.</summary>
    public void Open() => SetCurrentValue(IsOpenProperty, true);

    /// <summary>Closes the bar.</summary>
    public void Close() => SetCurrentValue(IsOpenProperty, false);

    /// <summary>The command with the given <see cref="AppBarCommand.Id"/>, or null.</summary>
    public AppBarCommand? GetCommandById(string id) => Commands.FirstOrDefault(c => c.Id == id);

    /// <summary>Makes the given commands visible.</summary>
    public void ShowCommands(params AppBarCommand[] commands)
    {
        foreach (var c in commands)
        {
            c.IsVisible = true;
        }
    }

    /// <summary>Hides the given commands.</summary>
    public void HideCommands(params AppBarCommand[] commands)
    {
        foreach (var c in commands)
        {
            c.IsVisible = false;
        }
    }

    /// <summary>Makes the given commands visible and hides all others.</summary>
    public void ShowOnlyCommands(params AppBarCommand[] commands)
    {
        foreach (var c in Commands)
        {
            c.IsVisible = Array.IndexOf(commands, c) >= 0;
        }
    }

    /// <summary>Whether the commands panel is in reduced mode.</summary>
    public bool IsReduced => _presenter.CommandsPanel?.IsReduced == true;

    internal EdgeOverlayHost Host => _host;

    /// <summary>Creates the presenter. <c>NavBar</c> overrides it to use its own theme.</summary>
    protected virtual AppBarPresenter CreatePresenter() => new();

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _presenter.DataContext = DataContext;
        _presenter.IsEnabled = IsEnabled;
        if (!_host.Attach(this))
        {
            return;
        }

        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is { } tl)
        {
            tl.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel);
            tl.AddHandler(PointerReleasedEvent, OnTopLevelPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        ApplyState(animate: false);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_topLevel is { } tl)
        {
            tl.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
            tl.RemoveHandler(PointerReleasedEvent, OnTopLevelPointerReleased);
            _topLevel = null;
        }

        _host.Detach();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DataContextProperty)
        {
            _presenter.DataContext = DataContext;
        }
        else if (change.Property == ContentProperty)
        {
            _presenter.Content = Content;
        }
        else if (change.Property == ContentTemplateProperty)
        {
            _presenter.ContentTemplate = ContentTemplate;
        }
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsOpen)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnTopLevelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!IsRightClickToggleEnabled || IsSticky || e.InitialPressMouseButton != MouseButton.Right)
        {
            return;
        }

        if (e.Source is Visual v && (v == _presenter || v.GetVisualAncestors().Contains(_presenter)))
        {
            return;
        }

        SetCurrentValue(IsOpenProperty, !IsOpen);
    }

    private void OnIsOpenChanged(bool open)
    {
        if (_reverting)
        {
            return;
        }

        var args = new CancelRoutedEventArgs(open ? OpeningEvent : ClosingEvent);
        RaiseEvent(args);
        if (args.Cancel)
        {
            _reverting = true;
            try
            {
                SetCurrentValue(IsOpenProperty, !open);
            }
            finally
            {
                _reverting = false;
            }

            return;
        }

        // A closing bar keeps its open layout while it slides out; the presenter becomes the strip once it has gone.
        _closing = !open;
        UpdatePseudoClasses();
        ApplyState(animate: true);
    }

    private async void ApplyState(bool animate)
    {
        var generation = ++_generation;
        var open = IsOpen;
        var closedVisible = ClosedDisplayMode != AppBarClosedDisplayMode.None;
        _host.IsLightDismissEnabled = open && !IsSticky;

        if (animate && !AnimationRunner.ShouldSkip(_presenter))
        {
            // The bar travels between its open position and the strip it leaves behind, so the slide ends exactly
            // where the closed layout starts. The presenter is in its open layout on both paths; a fresh layout pass
            // gives it its open height before the offset is taken from it.
            _presenter.IsVisible = true;
            _presenter.UpdateLayout();
            var travel = Math.Max(0, _presenter.Bounds.Height - (closedVisible ? ClosedHeight : 0));
            var offset = new WinOffset(0, Placement == AppBarPlacement.Top ? -travel : travel, false);
            _presenter.SetTransition(opening: open, closing: !open);
            await (open ? WinAnimations.ShowEdgeUI(_presenter, offset) : WinAnimations.HideEdgeUI(_presenter, offset));

            if (generation != _generation)
            {
                return;
            }
        }

        // The layout switches before the transition classes go: with :open gone first, a closing bar would show the
        // strip's ellipsis under the open layout's rule for an instant and dip its opacity.
        _closing = false;
        UpdatePseudoClasses();
        _presenter.SetTransition(opening: false, closing: false);
        if (!open)
        {
            _presenter.ClearValue(Visual.RenderTransformProperty);
        }

        _presenter.IsVisible = open || closedVisible;
        if (animate)
        {
            RaiseEvent(new RoutedEventArgs(open ? OpenedEvent : ClosedEvent));
        }
    }

    /// <summary>The height of the strip the closed bar leaves on screen, from the theme.</summary>
    private double ClosedHeight
    {
        get
        {
            var key = ClosedDisplayMode == AppBarClosedDisplayMode.Compact ? "WinAppBarReducedHeight" : "WinAppBarMinimalHeight";
            return _presenter.TryFindResource(key, out var value) && value is double height ? height : 0;
        }
    }

    internal void UpdatePseudoClasses()
    {
        var open = IsOpen;
        PseudoClasses.Set(":open", open);
        PseudoClasses.Set(":closed", !open);
        PseudoClasses.Set(":top", Placement == AppBarPlacement.Top);
        PseudoClasses.Set(":bottom", Placement == AppBarPlacement.Bottom);
        PseudoClasses.Set(":minimal", ClosedDisplayMode == AppBarClosedDisplayMode.Minimal);
        PseudoClasses.Set(":compact", ClosedDisplayMode == AppBarClosedDisplayMode.Compact);
        PseudoClasses.Set(":sticky", IsSticky);
        PseudoClasses.Set(":reduced", IsReduced);
        _host.Edge = Placement == AppBarPlacement.Top ? Dock.Top : Dock.Bottom;
        _presenter.IsOpen = open || _closing;
        _presenter.Placement = Placement;
        _presenter.Layout = Layout;
        _presenter.ClosedDisplayMode = ClosedDisplayMode;
        _presenter.IsSticky = IsSticky;
        _presenter.UpdateReduced();
    }
}
