using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using AvaWin.Animations;

namespace AvaWin.Controls;

/// <summary>Arguments for <see cref="SemanticZoom.ZoomChanged"/>.</summary>
public sealed class SemanticZoomChangedEventArgs : RoutedEventArgs
{
    internal SemanticZoomChangedEventArgs(RoutedEvent routedEvent, bool zoomedOut) : base(routedEvent)
    {
        IsZoomedOut = zoomedOut;
    }

    /// <summary>The new state.</summary>
    public bool IsZoomedOut { get; }
}

/// <summary>
/// Two views that both implement <see cref="IZoomableView"/>, for example a grouped ListView and a ListView of
/// its groups. A cross-fade with a scale switches between them. The zoomed-in view is positioned on the item chosen
/// in the zoomed-out view. The corner button, Ctrl and the wheel, Ctrl with minus or plus, and a pinch zoom.
/// </summary>
[TemplatePart("PART_ZoomedInHost", typeof(Panel), IsRequired = true)]
[TemplatePart("PART_ZoomedOutHost", typeof(Panel), IsRequired = true)]
[TemplatePart("PART_ZoomOutButton", typeof(Button), IsRequired = true)]
[PseudoClasses(":zoomedout", ":zoomedin", ":locked", ":animating", ":nobutton")]
public sealed class SemanticZoom : TemplatedControl
{
    /// <summary>Defines the <see cref="ZoomedInView"/> property.</summary>
    public static readonly StyledProperty<Control?> ZoomedInViewProperty = AvaloniaProperty.Register<SemanticZoom, Control?>(nameof(ZoomedInView));

    /// <summary>Defines the <see cref="ZoomedOutView"/> property.</summary>
    public static readonly StyledProperty<Control?> ZoomedOutViewProperty = AvaloniaProperty.Register<SemanticZoom, Control?>(nameof(ZoomedOutView));

    /// <summary>Defines the <see cref="IsZoomedOut"/> property.</summary>
    public static readonly StyledProperty<bool> IsZoomedOutProperty = AvaloniaProperty.Register<SemanticZoom, bool>(nameof(IsZoomedOut), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="ZoomFactor"/> property.</summary>
    public static readonly StyledProperty<double> ZoomFactorProperty = AvaloniaProperty.Register<SemanticZoom, double>(nameof(ZoomFactor), 0.65, coerce: (_, v) => Math.Clamp(v, 0.2, 0.85));

    /// <summary>Defines the <see cref="IsLocked"/> property.</summary>
    public static readonly StyledProperty<bool> IsLockedProperty = AvaloniaProperty.Register<SemanticZoom, bool>(nameof(IsLocked));

    /// <summary>Defines the <see cref="EnableButton"/> property.</summary>
    public static readonly StyledProperty<bool> EnableButtonProperty = AvaloniaProperty.Register<SemanticZoom, bool>(nameof(EnableButton), true);

    /// <summary>Defines the <see cref="ZoomedInItem"/> property.</summary>
    public static readonly StyledProperty<Func<object?, object?>?> ZoomedInItemProperty = AvaloniaProperty.Register<SemanticZoom, Func<object?, object?>?>(nameof(ZoomedInItem));

    /// <summary>Defines the <see cref="ZoomedOutItem"/> property.</summary>
    public static readonly StyledProperty<Func<object?, object?>?> ZoomedOutItemProperty = AvaloniaProperty.Register<SemanticZoom, Func<object?, object?>?>(nameof(ZoomedOutItem));

    /// <summary>Defines the <see cref="ZoomChanged"/> event.</summary>
    public static readonly RoutedEvent<SemanticZoomChangedEventArgs> ZoomChangedEvent = RoutedEvent.Register<SemanticZoom, SemanticZoomChangedEventArgs>(nameof(ZoomChanged), RoutingStrategies.Bubble);

    private Panel? _inHost;
    private Panel? _outHost;
    private Button? _button;
    private int _generation;
    private bool _reverting;
    private double _pinchStartScale = -1;

    static SemanticZoom()
    {
        ZoomedInViewProperty.Changed.AddClassHandler<SemanticZoom>((z, e) => z.OnViewChanged(e.OldValue as Control, e.NewValue as Control, zoomedOut: false));
        ZoomedOutViewProperty.Changed.AddClassHandler<SemanticZoom>((z, e) => z.OnViewChanged(e.OldValue as Control, e.NewValue as Control, zoomedOut: true));
        IsZoomedOutProperty.Changed.AddClassHandler<SemanticZoom>((z, e) => z.OnIsZoomedOutChanged((bool)e.NewValue!));
        IsLockedProperty.Changed.AddClassHandler<SemanticZoom>((z, _) => z.UpdatePseudoClasses());
        EnableButtonProperty.Changed.AddClassHandler<SemanticZoom>((z, _) => z.UpdatePseudoClasses());
        FocusableProperty.OverrideDefaultValue<SemanticZoom>(false);
    }

    /// <summary>Initializes a new instance.</summary>
    public SemanticZoom()
    {
        UpdatePseudoClasses();
        GestureRecognizers.Add(new PinchGestureRecognizer());
        AddHandler(PinchEvent, OnPinch);
        AddHandler(PinchEndedEvent, OnPinchEnded);
        AddHandler(PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
    }

    /// <summary>The detailed view. It must implement <see cref="IZoomableView"/>.</summary>
    public Control? ZoomedInView { get => GetValue(ZoomedInViewProperty); set => SetValue(ZoomedInViewProperty, value); }

    /// <summary>The overview. It must implement <see cref="IZoomableView"/>.</summary>
    public Control? ZoomedOutView { get => GetValue(ZoomedOutViewProperty); set => SetValue(ZoomedOutViewProperty, value); }

    /// <summary>Whether the overview is shown. Two-way.</summary>
    public bool IsZoomedOut { get => GetValue(IsZoomedOutProperty); set => SetValue(IsZoomedOutProperty, value); }

    /// <summary>How far the zoomed-in view shrinks during the cross-fade, from 0.2 to 0.85.</summary>
    public double ZoomFactor { get => GetValue(ZoomFactorProperty); set => SetValue(ZoomFactorProperty, value); }

    /// <summary>Whether zooming is disabled.</summary>
    public bool IsLocked { get => GetValue(IsLockedProperty); set => SetValue(IsLockedProperty, value); }

    /// <summary>Whether the corner zoom-out button is shown.</summary>
    public bool EnableButton { get => GetValue(EnableButtonProperty); set => SetValue(EnableButtonProperty, value); }

    /// <summary>Maps an item of the zoomed-out view to the item of the zoomed-in view to position on.</summary>
    public Func<object?, object?>? ZoomedInItem { get => GetValue(ZoomedInItemProperty); set => SetValue(ZoomedInItemProperty, value); }

    /// <summary>Maps an item of the zoomed-in view to the item of the zoomed-out view to position on.</summary>
    public Func<object?, object?>? ZoomedOutItem { get => GetValue(ZoomedOutItemProperty); set => SetValue(ZoomedOutItemProperty, value); }

    /// <summary>Raised when the zoom state changes.</summary>
    public event EventHandler<SemanticZoomChangedEventArgs> ZoomChanged { add => AddHandler(ZoomChangedEvent, value); remove => RemoveHandler(ZoomChangedEvent, value); }

    /// <summary>The item last handed from one view to the other. For tests and diagnostics.</summary>
    public object? LastPositionedItem { get; private set; }

    /// <summary>Switches between the two views.</summary>
    public void Toggle()
    {
        if (!IsLocked)
        {
            SetCurrentValue(IsZoomedOutProperty, !IsZoomedOut);
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_button is { } old)
        {
            old.Click -= OnButtonClick;
        }

        _inHost = e.NameScope.Find<Panel>("PART_ZoomedInHost");
        _outHost = e.NameScope.Find<Panel>("PART_ZoomedOutHost");
        _button = e.NameScope.Find<Button>("PART_ZoomOutButton");
        if (_button is { } b)
        {
            b.Click += OnButtonClick;
        }

        Mount(_inHost, ZoomedInView);
        Mount(_outHost, ZoomedOutView);
        ApplyState(animate: false);
        Configure();
    }

    private static void Mount(Panel? host, Control? view)
    {
        if (host is null)
        {
            return;
        }

        host.Children.Clear();
        if (view is not null)
        {
            if (view.Parent is Panel p)
            {
                p.Children.Remove(view);
            }

            host.Children.Add(view);
        }
    }

    private void OnViewChanged(Control? oldView, Control? newView, bool zoomedOut)
    {
        if (newView is not null and not IZoomableView)
        {
            throw new InvalidOperationException($"{(zoomedOut ? "ZoomedOutView" : "ZoomedInView")} must implement IZoomableView (ListView and Hub do).");
        }

        _ = oldView;
        Mount(zoomedOut ? _outHost : _inHost, newView);
        Configure();
        ApplyState(animate: false);
    }

    private void Configure()
    {
        if (ZoomedInView is IZoomableView zin)
        {
            zin.ConfigureForZoom(isZoomedOut: false, isCurrentView: !IsZoomedOut, triggerZoom: () => SetCurrentValue(IsZoomedOutProperty, true), prefetchedPages: 1);
        }

        if (ZoomedOutView is IZoomableView zout)
        {
            zout.ConfigureForZoom(isZoomedOut: true, isCurrentView: IsZoomedOut, triggerZoom: () => SetCurrentValue(IsZoomedOutProperty, false), prefetchedPages: 1);
        }
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e) => Toggle();

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (IsLocked || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        if (e.Delta.Y < 0 && !IsZoomedOut)
        {
            SetCurrentValue(IsZoomedOutProperty, true);
            e.Handled = true;
        }
        else if (e.Delta.Y > 0 && IsZoomedOut)
        {
            SetCurrentValue(IsZoomedOutProperty, false);
            e.Handled = true;
        }
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (IsLocked || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        if (e.Key is Key.OemMinus or Key.Subtract && !IsZoomedOut)
        {
            SetCurrentValue(IsZoomedOutProperty, true);
            e.Handled = true;
        }
        else if (e.Key is Key.OemPlus or Key.Add && IsZoomedOut)
        {
            SetCurrentValue(IsZoomedOutProperty, false);
            e.Handled = true;
        }
    }

    private void OnPinch(object? sender, PinchEventArgs e)
    {
        if (IsLocked)
        {
            return;
        }

        if (_pinchStartScale < 0)
        {
            _pinchStartScale = e.Scale;
        }

        if (e.Scale < 0.8 && !IsZoomedOut)
        {
            SetCurrentValue(IsZoomedOutProperty, true);
        }
        else if (e.Scale > 1.25 && IsZoomedOut)
        {
            SetCurrentValue(IsZoomedOutProperty, false);
        }
    }

    private void OnPinchEnded(object? sender, PinchEndedEventArgs e) => _pinchStartScale = -1;

    private void OnIsZoomedOutChanged(bool zoomedOut)
    {
        if (_reverting)
        {
            return;
        }

        if (IsLocked)
        {
            _reverting = true;
            try
            {
                SetCurrentValue(IsZoomedOutProperty, !zoomedOut);
            }
            finally
            {
                _reverting = false;
            }

            return;
        }

        // The incoming view needs a layout pass before it can scroll to the handed-over item.
        var showing = zoomedOut ? _outHost : _inHost;
        if (showing is not null)
        {
            showing.IsVisible = true;
            showing.UpdateLayout();
        }

        HandOff(zoomedOut);
        UpdatePseudoClasses();
        ApplyState(animate: true);
        RaiseEvent(new SemanticZoomChangedEventArgs(ZoomChangedEvent, zoomedOut));
    }

    /// <summary>Passes the current item of the view that is left to the view that is shown.</summary>
    private void HandOff(bool zoomedOut)
    {
        var from = (zoomedOut ? ZoomedInView : ZoomedOutView) as IZoomableView;
        var to = (zoomedOut ? ZoomedOutView : ZoomedInView) as IZoomableView;
        if (from is null || to is null)
        {
            return;
        }

        from.BeginZoom();
        to.BeginZoom();
        var current = from.GetCurrentItem();
        if (current is { } c)
        {
            var mapper = zoomedOut ? ZoomedOutItem : ZoomedInItem;
            var mapped = mapper is not null ? mapper(c.Item) : DefaultMap(c.Item, from, to, zoomedOut);
            LastPositionedItem = mapped;
            to.PositionItem(mapped, c.Position);
        }

        from.EndZoom(isCurrentView: false);
        to.EndZoom(isCurrentView: true);
    }

    /// <summary>The default mapping. An item of a grouped ListView maps to its group by key or header, and back. Otherwise the item maps to itself.</summary>
    private static object? DefaultMap(object? item, IZoomableView from, IZoomableView to, bool zoomedOut)
    {
        if (zoomedOut && from is ListView { Groups: { } groups } zin && item is not null)
        {
            var index = zin.ItemsView.IndexOf(item);
            foreach (var g in groups)
            {
                if (index >= g.Start && index < g.Start + g.Count)
                {
                    return g.Header;
                }
            }
        }

        if (!zoomedOut && to is ListView { Groups: { } inGroups } target)
        {
            foreach (var g in inGroups)
            {
                if (Equals(g.Header, item) || Equals(g.Key, item))
                {
                    return g.Start < target.ItemCount ? target.ItemsView[g.Start] : item;
                }
            }
        }

        return item;
    }

    private async void ApplyState(bool animate)
    {
        if (_inHost is null || _outHost is null)
        {
            return;
        }

        var generation = ++_generation;
        var zoomedOut = IsZoomedOut;
        var showing = zoomedOut ? _outHost : _inHost;
        var hiding = zoomedOut ? _inHost : _outHost;
        showing.IsVisible = true;
        showing.IsHitTestVisible = true;
        hiding.IsHitTestVisible = false;
        if (!animate || !this.IsAttachedToVisualTree())
        {
            hiding.IsVisible = false;
            showing.ClearValue(OpacityProperty);
            showing.ClearValue(RenderTransformProperty);
            return;
        }

        PseudoClasses.Set(":animating", true);
        var factor = ZoomFactor;
        // On zoom out, the detailed view shrinks from 1 to the factor while the overview grows from 1/factor to 1. Zoom in is the reverse.
        var hidingTo = zoomedOut ? AnimationRunner.Scale(factor) : AnimationRunner.Scale(1 / factor);
        var showingFrom = zoomedOut ? AnimationRunner.Scale(1 / factor) : AnimationRunner.Scale(factor);
        showing.RenderTransformOrigin = RelativePoint.Center;
        hiding.RenderTransformOrigin = RelativePoint.Center;
        await Task.WhenAll(
            WinAnimations.CrossFade(showing, hiding),
            AnimationRunner.Run(hiding, AnimationRunner.Transform(TransformOperations.Identity, hidingTo, 0, 367, WinEasing.Standard)),
            AnimationRunner.Run(showing, AnimationRunner.Transform(showingFrom, TransformOperations.Identity, 0, 367, WinEasing.Standard)));
        if (generation != _generation)
        {
            return;
        }

        hiding.IsVisible = false;
        hiding.ClearValue(OpacityProperty);
        hiding.ClearValue(RenderTransformProperty);
        showing.ClearValue(RenderTransformProperty);
        PseudoClasses.Set(":animating", false);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":zoomedout", IsZoomedOut);
        PseudoClasses.Set(":zoomedin", !IsZoomedOut);
        PseudoClasses.Set(":locked", IsLocked);
        PseudoClasses.Set(":nobutton", !EnableButton);
    }
}
