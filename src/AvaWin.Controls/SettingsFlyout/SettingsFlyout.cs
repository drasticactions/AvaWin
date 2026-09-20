using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaWin.Animations;
using AvaWin.Controls.Primitives;

namespace AvaWin.Controls;

/// <summary>The width of a settings pane: narrow (345px) or wide (645px).</summary>
public enum SettingsFlyoutWidth
{
    /// <summary>A 345px pane.</summary>
    Narrow,

    /// <summary>A 645px pane.</summary>
    Wide,
}

/// <summary>
/// A full-height pane docked to the right edge, hosted in the <see cref="OverlayLayer"/> of the TopLevel. It has
/// a header (a small BackButton and a title) and scrolling content. A click outside it or Escape closes it. The
/// element declared in the page is a zero-size placeholder. <see cref="Pane"/> is the visible pane. This is a
/// <c>TemplatedControl</c> with its own Header and Content, not a HeaderedContentControl, because the presenter
/// owns the logical parent of the content.
/// </summary>
[PseudoClasses(":open", ":closed", ":narrow", ":wide")]
public sealed class SettingsFlyout : TemplatedControl
{
    /// <summary>Defines the <see cref="Header"/> property.</summary>
    public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<SettingsFlyout, object?>(nameof(Header));

    /// <summary>Defines the <see cref="HeaderTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty = AvaloniaProperty.Register<SettingsFlyout, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>Defines the <see cref="Content"/> property.</summary>
    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<SettingsFlyout, object?>(nameof(Content));

    /// <summary>Defines the <see cref="ContentTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty = AvaloniaProperty.Register<SettingsFlyout, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Defines the <see cref="PaneWidth"/> property.</summary>
    public static readonly StyledProperty<SettingsFlyoutWidth> PaneWidthProperty =
        AvaloniaProperty.Register<SettingsFlyout, SettingsFlyoutWidth>(nameof(PaneWidth));

    /// <summary>Defines the <see cref="SettingsCommandId"/> property.</summary>
    public static readonly StyledProperty<string?> SettingsCommandIdProperty =
        AvaloniaProperty.Register<SettingsFlyout, string?>(nameof(SettingsCommandId));

    /// <summary>Defines the <see cref="IsOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<SettingsFlyout, bool>(nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="IgnoreSafeArea"/> property.</summary>
    public static readonly StyledProperty<bool> IgnoreSafeAreaProperty =
        AvaloniaProperty.Register<SettingsFlyout, bool>(nameof(IgnoreSafeArea));

    /// <summary>Defines the <see cref="Opening"/> event.</summary>
    public static readonly RoutedEvent<CancelRoutedEventArgs> OpeningEvent = RoutedEvent.Register<SettingsFlyout, CancelRoutedEventArgs>(nameof(Opening), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="Opened"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent = RoutedEvent.Register<SettingsFlyout, RoutedEventArgs>(nameof(Opened), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="Closing"/> event.</summary>
    public static readonly RoutedEvent<CancelRoutedEventArgs> ClosingEvent = RoutedEvent.Register<SettingsFlyout, CancelRoutedEventArgs>(nameof(Closing), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="Closed"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent = RoutedEvent.Register<SettingsFlyout, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);

    private readonly SettingsFlyoutPresenter _presenter;
    private readonly EdgeOverlayHost _host;
    private TopLevel? _topLevel;
    private int _generation;
    private bool _reverting;

    static SettingsFlyout()
    {
        PaneWidthProperty.Changed.AddClassHandler<SettingsFlyout>((f, _) => f.UpdatePseudoClasses());
        IsOpenProperty.Changed.AddClassHandler<SettingsFlyout>((f, e) => f.OnIsOpenChanged((bool)e.NewValue!));
        IsEnabledProperty.Changed.AddClassHandler<SettingsFlyout>((f, _) => f._presenter.IsEnabled = f.IsEnabled);
        IgnoreSafeAreaProperty.Changed.AddClassHandler<SettingsFlyout>((f, _) => f._host.RespectsSafeArea = !f.IgnoreSafeArea);
    }

    /// <summary>Initializes a new instance.</summary>
    public SettingsFlyout()
    {
        _presenter = new SettingsFlyoutPresenter { Owner = this };
        _host = new EdgeOverlayHost(_presenter, Dock.Right) { IsLightDismissEnabled = false };
        _host.LightDismissRequested += (_, _) => Hide();
        _host.SafeAreaPaddingChanged += (_, _) => _presenter.SafeAreaPadding = _host.SafeAreaPadding;
        _presenter.IsVisible = false;
        Focusable = false;
        UpdatePseudoClasses();
    }

    /// <summary>The title in the header of the pane.</summary>
    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Template for <see cref="Header"/>.</summary>
    public IDataTemplate? HeaderTemplate { get => GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty, value); }

    /// <summary>The pane body.</summary>
    [Content]
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Template for <see cref="Content"/>.</summary>
    public IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>Whether the pane is narrow (345px) or wide (645px).</summary>
    public SettingsFlyoutWidth PaneWidth { get => GetValue(PaneWidthProperty); set => SetValue(PaneWidthProperty, value); }

    /// <summary>An identifier for the pane, defined by the app.</summary>
    public string? SettingsCommandId { get => GetValue(SettingsCommandIdProperty); set => SetValue(SettingsCommandIdProperty, value); }

    /// <summary>Whether the pane is open. Two-way.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Whether the pane docks to the physical screen edge instead of the safe area (status bar, notch, home indicator). Default false.</summary>
    public bool IgnoreSafeArea { get => GetValue(IgnoreSafeAreaProperty); set => SetValue(IgnoreSafeAreaProperty, value); }

    /// <summary>Raised before the pane opens. Cancelable.</summary>
    public event EventHandler<CancelRoutedEventArgs> Opening { add => AddHandler(OpeningEvent, value); remove => RemoveHandler(OpeningEvent, value); }

    /// <summary>Raised after the pane has opened.</summary>
    public event EventHandler<RoutedEventArgs> Opened { add => AddHandler(OpenedEvent, value); remove => RemoveHandler(OpenedEvent, value); }

    /// <summary>Raised before the pane closes. Cancelable.</summary>
    public event EventHandler<CancelRoutedEventArgs> Closing { add => AddHandler(ClosingEvent, value); remove => RemoveHandler(ClosingEvent, value); }

    /// <summary>Raised after the pane has closed.</summary>
    public event EventHandler<RoutedEventArgs> Closed { add => AddHandler(ClosedEvent, value); remove => RemoveHandler(ClosedEvent, value); }

    /// <summary>The visible pane, hosted in the overlay layer.</summary>
    public SettingsFlyoutPresenter Pane => _presenter;

    /// <summary>Opens the pane.</summary>
    public void Show() => SetCurrentValue(IsOpenProperty, true);

    /// <summary>Closes the pane.</summary>
    public void Hide() => SetCurrentValue(IsOpenProperty, false);

    internal EdgeOverlayHost Host => _host;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SyncPresenter();
        if (!_host.Attach(this))
        {
            return;
        }

        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel);
        _presenter.IsVisible = IsOpen;
        _host.IsLightDismissEnabled = IsOpen;
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        _topLevel = null;
        _host.Detach();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DataContextProperty || change.Property == ContentProperty ||
            change.Property == HeaderProperty || change.Property == ContentTemplateProperty ||
            change.Property == HeaderTemplateProperty)
        {
            SyncPresenter();
        }
    }

    private void SyncPresenter()
    {
        _presenter.DataContext = DataContext;
        _presenter.Header = Header;
        _presenter.HeaderTemplate = HeaderTemplate;
        _presenter.Content = Content;
        _presenter.ContentTemplate = ContentTemplate;
        _presenter.IsEnabled = IsEnabled;
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsOpen)
        {
            Hide();
            e.Handled = true;
        }
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

        UpdatePseudoClasses();
        _ = ApplyStateAsync(open);
    }

    private async System.Threading.Tasks.Task ApplyStateAsync(bool open)
    {
        var generation = ++_generation;
        _host.IsLightDismissEnabled = open;
        var width = (PaneWidth == SettingsFlyoutWidth.Wide ? 645 : 345) + _presenter.SafeAreaPadding.Right;
        var offset = new WinOffset(width + 19, 0, false);
        if (open)
        {
            _presenter.IsVisible = true;
            await WinAnimations.ShowPanel(_presenter, offset);
        }
        else
        {
            await WinAnimations.HidePanel(_presenter, offset);
        }

        if (generation != _generation)
        {
            return;
        }

        if (!open)
        {
            _presenter.ClearValue(Visual.RenderTransformProperty);
            _presenter.IsVisible = false;
        }

        RaiseEvent(new RoutedEventArgs(open ? OpenedEvent : ClosedEvent));
    }

    private void UpdatePseudoClasses()
    {
        void Set(IPseudoClasses c)
        {
            c.Set(":open", IsOpen);
            c.Set(":closed", !IsOpen);
            c.Set(":narrow", PaneWidth == SettingsFlyoutWidth.Narrow);
            c.Set(":wide", PaneWidth == SettingsFlyoutWidth.Wide);
        }

        Set(PseudoClasses);
        Set(_presenter.StateClasses);
    }
}

/// <summary>The visible pane of a <see cref="SettingsFlyout"/>. Its ControlTheme carries the pane template.</summary>
[TemplatePart("PART_Root", typeof(Border))]
[TemplatePart("PART_Body", typeof(Border))]
[TemplatePart("PART_Header", typeof(Grid))]
[TemplatePart("PART_BackButton", typeof(BackButton))]
[TemplatePart("PART_HeaderPresenter", typeof(ContentPresenter), IsRequired = true)]
[TemplatePart("PART_Content", typeof(ScrollViewer))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[PseudoClasses(":open", ":closed", ":narrow", ":wide")]
public sealed class SettingsFlyoutPresenter : HeaderedContentControl
{
    /// <summary>Defines the <see cref="SafeAreaPadding"/> property.</summary>
    public static readonly StyledProperty<Thickness> SafeAreaPaddingProperty = AvaloniaProperty.Register<SettingsFlyoutPresenter, Thickness>(nameof(SafeAreaPadding));

    private BackButton? _back;

    /// <summary>The safe-area inset on the edge of the pane. It pads <c>PART_Root</c>, so the background reaches the screen edge and the content stays clear. The owning <see cref="SettingsFlyout"/> sets it.</summary>
    public Thickness SafeAreaPadding { get => GetValue(SafeAreaPaddingProperty); set => SetValue(SafeAreaPaddingProperty, value); }

    /// <summary>The flyout this presenter belongs to.</summary>
    public SettingsFlyout? Owner { get; internal set; }

    internal IPseudoClasses StateClasses => PseudoClasses;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_back is { } old)
        {
            old.Click -= OnBackClick;
        }

        _back = e.NameScope.Find<BackButton>("PART_BackButton");
        if (_back is { } back)
        {
            back.IsAutoEnabled = false;
            back.IsEnabled = true;
            back.Click += OnBackClick;
        }
    }

    private void OnBackClick(object? sender, RoutedEventArgs e) => Owner?.Hide();
}
