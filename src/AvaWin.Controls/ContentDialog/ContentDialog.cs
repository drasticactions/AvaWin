using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using AvaWin.Animations;
using AvaWin.Controls.Primitives;

namespace AvaWin.Controls;

/// <summary>How a dialog was dismissed.</summary>
public enum ContentDialogResult
{
    /// <summary>Escape, the hardware back button, or <see cref="ContentDialog.Hide"/> without a result.</summary>
    None,

    /// <summary>The primary command.</summary>
    Primary,

    /// <summary>The secondary command.</summary>
    Secondary,
}

/// <summary>Arguments for <see cref="ContentDialog.BeforeHide"/> and <see cref="ContentDialog.AfterHide"/>.</summary>
public sealed class ContentDialogHideEventArgs : CancelRoutedEventArgs
{
    internal ContentDialogHideEventArgs(RoutedEvent routedEvent, ContentDialogResult result) : base(routedEvent)
    {
        Result = result;
    }

    /// <summary>How the dialog is being dismissed.</summary>
    public ContentDialogResult Result { get; }
}

/// <summary>
/// A modal dialog over the whole window: a dimmed backdrop and a centered panel with a title, custom content and
/// up to two equal-width commands. <see cref="ShowAsync"/> shows it and completes with the
/// <see cref="ContentDialogResult"/> once it is hidden. Escape dismisses it with
/// <see cref="ContentDialogResult.None"/>. The element declared in the page is a zero-size placeholder.
/// <see cref="Dialog"/> is the visible presenter, hosted in the <see cref="OverlayLayer"/> of the TopLevel.
/// </summary>
[PseudoClasses(":open", ":closed")]
public sealed class ContentDialog : TemplatedControl
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<ContentDialog, object?>(nameof(Title));

    /// <summary>Defines the <see cref="Content"/> property.</summary>
    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<ContentDialog, object?>(nameof(Content));

    /// <summary>Defines the <see cref="ContentTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty = AvaloniaProperty.Register<ContentDialog, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Defines the <see cref="PrimaryCommandText"/> property.</summary>
    public static readonly StyledProperty<string?> PrimaryCommandTextProperty = AvaloniaProperty.Register<ContentDialog, string?>(nameof(PrimaryCommandText));

    /// <summary>Defines the <see cref="SecondaryCommandText"/> property.</summary>
    public static readonly StyledProperty<string?> SecondaryCommandTextProperty = AvaloniaProperty.Register<ContentDialog, string?>(nameof(SecondaryCommandText));

    /// <summary>Defines the <see cref="IsPrimaryCommandEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsPrimaryCommandEnabledProperty = AvaloniaProperty.Register<ContentDialog, bool>(nameof(IsPrimaryCommandEnabled), true);

    /// <summary>Defines the <see cref="IsSecondaryCommandEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsSecondaryCommandEnabledProperty = AvaloniaProperty.Register<ContentDialog, bool>(nameof(IsSecondaryCommandEnabled), true);

    /// <summary>Defines the <see cref="IsOpen"/> property.</summary>
    public static readonly DirectProperty<ContentDialog, bool> IsOpenProperty = AvaloniaProperty.RegisterDirect<ContentDialog, bool>(nameof(IsOpen), o => o.IsOpen);

    /// <summary>Defines the <see cref="BeforeShow"/> event.</summary>
    public static readonly RoutedEvent<CancelRoutedEventArgs> BeforeShowEvent = RoutedEvent.Register<ContentDialog, CancelRoutedEventArgs>(nameof(BeforeShow), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="AfterShow"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> AfterShowEvent = RoutedEvent.Register<ContentDialog, RoutedEventArgs>(nameof(AfterShow), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="BeforeHide"/> event.</summary>
    public static readonly RoutedEvent<ContentDialogHideEventArgs> BeforeHideEvent = RoutedEvent.Register<ContentDialog, ContentDialogHideEventArgs>(nameof(BeforeHide), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="AfterHide"/> event.</summary>
    public static readonly RoutedEvent<ContentDialogHideEventArgs> AfterHideEvent = RoutedEvent.Register<ContentDialog, ContentDialogHideEventArgs>(nameof(AfterHide), RoutingStrategies.Bubble);

    private readonly ContentDialogPresenter _presenter;
    private readonly EdgeOverlayHost _host;
    private TopLevel? _topLevel;
    private TaskCompletionSource<ContentDialogResult>? _dismissed;
    private IInputElement? _previousFocus;
    private bool _isOpen;
    private bool _busy;
    private ContentDialogResult? _pendingHide;

    static ContentDialog()
    {
        IsEnabledProperty.Changed.AddClassHandler<ContentDialog>((d, _) => d._presenter.IsEnabled = d.IsEnabled);
    }

    /// <summary>Initializes a new instance.</summary>
    public ContentDialog()
    {
        _presenter = new ContentDialogPresenter { Owner = this, IsVisible = false, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch };
        _host = new EdgeOverlayHost(_presenter, Dock.Top) { IsLightDismissEnabled = false };
        // The presenter covers the whole layer, backdrop included, so the docked-edge alignment does not apply.
        _presenter.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
        _presenter.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        _presenter.CommandClicked += (_, result) => OnCommandClicked(result);
        Focusable = false;
        UpdatePseudoClasses();
    }

    /// <summary>The title at the top of the dialog.</summary>
    public object? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The body of the dialog.</summary>
    [Content]
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Template for <see cref="Content"/>.</summary>
    public IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>The text of the primary button. Empty hides the button.</summary>
    public string? PrimaryCommandText { get => GetValue(PrimaryCommandTextProperty); set => SetValue(PrimaryCommandTextProperty, value); }

    /// <summary>The text of the secondary button. Empty hides the button.</summary>
    public string? SecondaryCommandText { get => GetValue(SecondaryCommandTextProperty); set => SetValue(SecondaryCommandTextProperty, value); }

    /// <summary>Whether the primary button is enabled.</summary>
    public bool IsPrimaryCommandEnabled { get => GetValue(IsPrimaryCommandEnabledProperty); set => SetValue(IsPrimaryCommandEnabledProperty, value); }

    /// <summary>Whether the secondary button is enabled.</summary>
    public bool IsSecondaryCommandEnabled { get => GetValue(IsSecondaryCommandEnabledProperty); set => SetValue(IsSecondaryCommandEnabledProperty, value); }

    /// <summary>Whether the dialog is shown. Read-only: use <see cref="ShowAsync"/> and <see cref="Hide"/>.</summary>
    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            if (SetAndRaise(IsOpenProperty, ref _isOpen, value))
            {
                UpdatePseudoClasses();
            }
        }
    }

    /// <summary>Raised before the dialog shows. Cancelable.</summary>
    public event EventHandler<CancelRoutedEventArgs> BeforeShow { add => AddHandler(BeforeShowEvent, value); remove => RemoveHandler(BeforeShowEvent, value); }

    /// <summary>Raised after the dialog has shown.</summary>
    public event EventHandler<RoutedEventArgs> AfterShow { add => AddHandler(AfterShowEvent, value); remove => RemoveHandler(AfterShowEvent, value); }

    /// <summary>Raised before the dialog hides. Cancelable. <see cref="ContentDialogHideEventArgs.Result"/> says why it hides.</summary>
    public event EventHandler<ContentDialogHideEventArgs> BeforeHide { add => AddHandler(BeforeHideEvent, value); remove => RemoveHandler(BeforeHideEvent, value); }

    /// <summary>Raised after the dialog has hidden.</summary>
    public event EventHandler<ContentDialogHideEventArgs> AfterHide { add => AddHandler(AfterHideEvent, value); remove => RemoveHandler(AfterHideEvent, value); }

    /// <summary>The visible dialog, hosted in the overlay layer.</summary>
    public ContentDialogPresenter Dialog => _presenter;

    /// <summary>
    /// Shows the dialog and completes when it is hidden, with how it was dismissed. Throws when the dialog is
    /// already shown. Completes at once with <see cref="ContentDialogResult.None"/> when <see cref="BeforeShow"/>
    /// is canceled.
    /// </summary>
    public Task<ContentDialogResult> ShowAsync()
    {
        if (_dismissed is not null)
        {
            throw new InvalidOperationException("The ContentDialog is already showing.");
        }

        var before = new CancelRoutedEventArgs(BeforeShowEvent);
        RaiseEvent(before);
        if (before.Cancel)
        {
            return Task.FromResult(ContentDialogResult.None);
        }

        _dismissed = new TaskCompletionSource<ContentDialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = _dismissed.Task;
        _ = ShowCoreAsync();
        return task;
    }

    /// <summary>Hides the dialog with the given result. A hide requested during the entrance animation takes effect after it.</summary>
    public void Hide(ContentDialogResult result = ContentDialogResult.None)
    {
        if (_dismissed is null)
        {
            return;
        }

        if (_busy)
        {
            _pendingHide = result;
            return;
        }

        _ = HideCoreAsync(result);
    }

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
        if (change.Property == DataContextProperty || change.Property == TitleProperty || change.Property == ContentProperty ||
            change.Property == ContentTemplateProperty || change.Property == PrimaryCommandTextProperty ||
            change.Property == SecondaryCommandTextProperty || change.Property == IsPrimaryCommandEnabledProperty ||
            change.Property == IsSecondaryCommandEnabledProperty)
        {
            SyncPresenter();
        }
    }

    private void SyncPresenter()
    {
        _presenter.DataContext = DataContext;
        _presenter.Header = Title;
        _presenter.Content = Content;
        _presenter.ContentTemplate = ContentTemplate;
        _presenter.PrimaryCommandText = PrimaryCommandText;
        _presenter.SecondaryCommandText = SecondaryCommandText;
        _presenter.IsPrimaryCommandEnabled = IsPrimaryCommandEnabled;
        _presenter.IsSecondaryCommandEnabled = IsSecondaryCommandEnabled;
        _presenter.IsEnabled = IsEnabled;
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _dismissed is not null)
        {
            Hide(ContentDialogResult.None);
            e.Handled = true;
        }
    }

    private void OnCommandClicked(ContentDialogResult result)
    {
        if (IsOpen && !_busy)
        {
            Hide(result);
        }
    }

    private async Task ShowCoreAsync()
    {
        _busy = true;
        try
        {
            _host.Attach(this);
            _previousFocus = _topLevel?.FocusManager?.GetFocusedElement();
            _presenter.IsVisible = true;
            IsOpen = true;
            _presenter.UpdateLayout();
            _presenter.FocusInitial();
            await WinAnimations.FadeIn(_presenter);
            RaiseEvent(new RoutedEventArgs(AfterShowEvent));
        }
        finally
        {
            _busy = false;
        }

        if (_pendingHide is { } pending)
        {
            _pendingHide = null;
            await HideCoreAsync(pending);
        }
    }

    private async Task HideCoreAsync(ContentDialogResult result)
    {
        var before = new ContentDialogHideEventArgs(BeforeHideEvent, result);
        RaiseEvent(before);
        if (before.Cancel)
        {
            return;
        }

        var dismissed = _dismissed;
        _busy = true;
        try
        {
            await WinAnimations.FadeOut(_presenter);
            _presenter.IsVisible = false;
            IsOpen = false;
            _dismissed = null;
            if (_previousFocus is { } previous)
            {
                previous.Focus();
                _previousFocus = null;
            }

            RaiseEvent(new ContentDialogHideEventArgs(AfterHideEvent, result));
        }
        finally
        {
            _busy = false;
        }

        dismissed?.TrySetResult(result);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":open", _isOpen);
        PseudoClasses.Set(":closed", !_isOpen);
        _presenter.StateClasses.Set(":open", _isOpen);
        _presenter.StateClasses.Set(":closed", !_isOpen);
    }
}

/// <summary>The visible dialog of a <see cref="ContentDialog"/>. Its ControlTheme carries the dialog template.</summary>
[TemplatePart("PART_Backdrop", typeof(Border))]
[TemplatePart("PART_Dialog", typeof(Border), IsRequired = true)]
[TemplatePart("PART_Title", typeof(ContentPresenter))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_PrimaryCommand", typeof(Button), IsRequired = true)]
[TemplatePart("PART_SecondaryCommand", typeof(Button), IsRequired = true)]
[PseudoClasses(":open", ":closed")]
public sealed class ContentDialogPresenter : HeaderedContentControl
{
    /// <summary>Defines the <see cref="PrimaryCommandText"/> property.</summary>
    public static readonly StyledProperty<string?> PrimaryCommandTextProperty = AvaloniaProperty.Register<ContentDialogPresenter, string?>(nameof(PrimaryCommandText));

    /// <summary>Defines the <see cref="SecondaryCommandText"/> property.</summary>
    public static readonly StyledProperty<string?> SecondaryCommandTextProperty = AvaloniaProperty.Register<ContentDialogPresenter, string?>(nameof(SecondaryCommandText));

    /// <summary>Defines the <see cref="IsPrimaryCommandEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsPrimaryCommandEnabledProperty = AvaloniaProperty.Register<ContentDialogPresenter, bool>(nameof(IsPrimaryCommandEnabled), true);

    /// <summary>Defines the <see cref="IsSecondaryCommandEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsSecondaryCommandEnabledProperty = AvaloniaProperty.Register<ContentDialogPresenter, bool>(nameof(IsSecondaryCommandEnabled), true);

    private Border? _dialog;
    private Button? _primary;
    private Button? _secondary;
    private ContentPresenter? _content;

    static ContentDialogPresenter()
    {
        PrimaryCommandTextProperty.Changed.AddClassHandler<ContentDialogPresenter>((p, _) => p.UpdateCommands());
        SecondaryCommandTextProperty.Changed.AddClassHandler<ContentDialogPresenter>((p, _) => p.UpdateCommands());
    }

    /// <summary>The text of the primary button. Empty hides it.</summary>
    public string? PrimaryCommandText { get => GetValue(PrimaryCommandTextProperty); set => SetValue(PrimaryCommandTextProperty, value); }

    /// <summary>The text of the secondary button. Empty hides it.</summary>
    public string? SecondaryCommandText { get => GetValue(SecondaryCommandTextProperty); set => SetValue(SecondaryCommandTextProperty, value); }

    /// <summary>Whether the primary button is enabled.</summary>
    public bool IsPrimaryCommandEnabled { get => GetValue(IsPrimaryCommandEnabledProperty); set => SetValue(IsPrimaryCommandEnabledProperty, value); }

    /// <summary>Whether the secondary button is enabled.</summary>
    public bool IsSecondaryCommandEnabled { get => GetValue(IsSecondaryCommandEnabledProperty); set => SetValue(IsSecondaryCommandEnabledProperty, value); }

    /// <summary>The dialog this presenter belongs to, or null for an inline presenter.</summary>
    public ContentDialog? Owner { get; internal set; }

    /// <summary>Raised when a command button is clicked.</summary>
    public event EventHandler<ContentDialogResult>? CommandClicked;

    internal IPseudoClasses StateClasses => PseudoClasses;

    /// <summary>Moves focus to the first focusable element of the content, else to the primary button, else to the dialog.</summary>
    internal void FocusInitial()
    {
        var target = _content is { } c ? FirstFocusable(c) : null;
        (target ?? _primary ?? (IInputElement)this).Focus();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_primary is { } p)
        {
            p.Click -= OnPrimaryClick;
        }

        if (_secondary is { } s)
        {
            s.Click -= OnSecondaryClick;
        }

        _dialog = e.NameScope.Find<Border>("PART_Dialog");
        _content = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _primary = e.NameScope.Find<Button>("PART_PrimaryCommand");
        _secondary = e.NameScope.Find<Button>("PART_SecondaryCommand");
        if (_primary is { } p2)
        {
            p2.Click += OnPrimaryClick;
        }

        if (_secondary is { } s2)
        {
            s2.Click += OnSecondaryClick;
        }

        UpdateCommands();
    }

    private void OnPrimaryClick(object? sender, RoutedEventArgs e) => CommandClicked?.Invoke(this, ContentDialogResult.Primary);

    private void OnSecondaryClick(object? sender, RoutedEventArgs e) => CommandClicked?.Invoke(this, ContentDialogResult.Secondary);

    /// <summary>Two commands share the row. A lone command takes the right half.</summary>
    private void UpdateCommands()
    {
        if (_primary is null || _secondary is null)
        {
            return;
        }

        var hasPrimary = !string.IsNullOrEmpty(PrimaryCommandText);
        var hasSecondary = !string.IsNullOrEmpty(SecondaryCommandText);
        _primary.IsVisible = hasPrimary;
        _secondary.IsVisible = hasSecondary;
        Grid.SetColumn(_primary, hasPrimary && !hasSecondary ? 1 : 0);
        Grid.SetColumn(_secondary, 1);
        PseudoClasses.Set(":commands", hasPrimary || hasSecondary);
    }

    private static IInputElement? FirstFocusable(Visual root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is IInputElement { Focusable: true, IsEffectivelyEnabled: true, IsEffectivelyVisible: true } e && child is not ContentPresenter)
            {
                return e;
            }

            if (FirstFocusable(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}

/// <summary>
/// Positions the dialog panel. It is centered horizontally. It is centered vertically when the window is at least
/// 640px tall, else attached to the top. It fills the window height when the window is shorter than the maximum
/// dialog height (758px) and the content wants more than half of it. Otherwise it is sized to its content.
/// </summary>
public sealed class ContentDialogPanel : Panel
{
    private const double VerticallyCenteredThreshold = 640;
    private const double DialogMaxHeight = 758;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
        {
            child.Measure(new Size(DialogWidth(child, availableSize), double.PositiveInfinity));
            if (FillsHeight(child, availableSize))
            {
                child.Measure(new Size(DialogWidth(child, availableSize), availableSize.Height));
            }
        }

        return availableSize;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            var width = DialogWidth(child, finalSize);
            var height = FillsHeight(child, finalSize) ? finalSize.Height : Math.Min(child.DesiredSize.Height, finalSize.Height);
            var x = Math.Max(0, (finalSize.Width - width) / 2);
            var y = finalSize.Height >= VerticallyCenteredThreshold ? Math.Max(0, (finalSize.Height - height) / 2) : 0;
            child.Arrange(new Rect(x, y, width, height));
        }

        return finalSize;
    }

    private static bool FillsHeight(Control child, Size window) => window.Height < DialogMaxHeight && child.DesiredSize.Height > window.Height / 2;

    /// <summary>The dialog is as wide as the window, within its minimum and maximum width.</summary>
    private static double DialogWidth(Control child, Size window) => Math.Clamp(window.Width, child.MinWidth, double.IsInfinity(child.MaxWidth) ? window.Width : child.MaxWidth);
}
