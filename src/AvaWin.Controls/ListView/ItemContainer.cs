using System;
using Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AvaWin.Controls;

/// <summary>
/// One item with the ListView chrome (selection border or fill, checkmark, hover and focus outlines), used
/// outside a ListView. It shares the <see cref="ListViewItem"/> theme without the container margins.
/// </summary>
[PseudoClasses(":selected", ":pressed", ":selectionmode")]
public sealed class ItemContainer : ListViewItem
{
    /// <summary>Defines the <see cref="IsSelectionDisabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectionDisabledProperty = AvaloniaProperty.Register<ItemContainer, bool>(nameof(IsSelectionDisabled));

    /// <summary>Defines the <see cref="TapBehavior"/> property.</summary>
    public static readonly StyledProperty<TapBehavior> TapBehaviorProperty = AvaloniaProperty.Register<ItemContainer, TapBehavior>(nameof(TapBehavior), TapBehavior.InvokeOnly);

    /// <summary>Defines the <see cref="SwipeBehavior"/> property.</summary>
    public static readonly StyledProperty<SwipeBehavior> SwipeBehaviorProperty = AvaloniaProperty.Register<ItemContainer, SwipeBehavior>(nameof(SwipeBehavior), SwipeBehavior.Select);

    /// <summary>Defines the <see cref="SwipeOrientation"/> property.</summary>
    public static readonly StyledProperty<Orientation> SwipeOrientationProperty = AvaloniaProperty.Register<ItemContainer, Orientation>(nameof(SwipeOrientation), Orientation.Vertical);

    /// <summary>Defines the <see cref="IsDraggable"/> property.</summary>
    public static readonly StyledProperty<bool> IsDraggableProperty = AvaloniaProperty.Register<ItemContainer, bool>(nameof(IsDraggable));

    /// <summary>Defines the <see cref="SelectionStyle"/> property.</summary>
    public static readonly StyledProperty<ListViewSelectionStyle> SelectionStyleProperty = AvaloniaProperty.Register<ItemContainer, ListViewSelectionStyle>(nameof(SelectionStyle), ListViewSelectionStyle.Bordered);

    /// <summary>Defines the <see cref="Invoked"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> InvokedEvent = RoutedEvent.Register<ItemContainer, RoutedEventArgs>(nameof(Invoked), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="SelectionChanging"/> event.</summary>
    public static readonly RoutedEvent<CancelRoutedEventArgs> SelectionChangingEvent = RoutedEvent.Register<ItemContainer, CancelRoutedEventArgs>(nameof(SelectionChanging), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="SelectionChanged"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> SelectionChangedEvent = RoutedEvent.Register<ItemContainer, RoutedEventArgs>(nameof(SelectionChanged), RoutingStrategies.Bubble);

    private bool _pressed;
    private bool _reverting;

    static ItemContainer()
    {
        SelectionStyleProperty.Changed.AddClassHandler<ItemContainer>((c, _) => c.ApplySelectionStyle());
        IsSelectedProperty.Changed.AddClassHandler<ItemContainer>((c, e) => c.OnIsSelectedChanged((bool)e.OldValue!, (bool)e.NewValue!));
        IsSelectionDisabledProperty.Changed.AddClassHandler<ItemContainer>((c, e) => c.IsSelectable = !(bool)e.NewValue!);
    }

    /// <summary>Initializes a new instance.</summary>
    public ItemContainer()
    {
        ApplySelectionStyle();
    }

    /// <summary>Whether the item can be selected. When true, a tap only invokes.</summary>
    public bool IsSelectionDisabled { get => GetValue(IsSelectionDisabledProperty); set => SetValue(IsSelectionDisabledProperty, value); }

    /// <summary>What a tap does: invoke only (default), invoke and select, or invoke and toggle.</summary>
    public TapBehavior TapBehavior { get => GetValue(TapBehaviorProperty); set => SetValue(TapBehaviorProperty, value); }

    /// <summary>Whether a cross-axis swipe selects the item. Kept for API compatibility. It has no effect.</summary>
    public SwipeBehavior SwipeBehavior { get => GetValue(SwipeBehaviorProperty); set => SetValue(SwipeBehaviorProperty, value); }

    /// <summary>The axis of the swipe gesture. Kept for API compatibility. It has no effect.</summary>
    public Orientation SwipeOrientation { get => GetValue(SwipeOrientationProperty); set => SetValue(SwipeOrientationProperty, value); }

    /// <summary>Whether the item is a drag source. The consumer starts the drag itself.</summary>
    public bool IsDraggable { get => GetValue(IsDraggableProperty); set => SetValue(IsDraggableProperty, value); }

    /// <summary>The selection chrome: an accent border with a corner checkmark (default), or an accent fill.</summary>
    public ListViewSelectionStyle SelectionStyle { get => GetValue(SelectionStyleProperty); set => SetValue(SelectionStyleProperty, value); }

    /// <summary>Raised when the user taps the item, clicks it, or presses Enter on it.</summary>
    public event EventHandler<RoutedEventArgs> Invoked { add => AddHandler(InvokedEvent, value); remove => RemoveHandler(InvokedEvent, value); }

    /// <summary>Raised before the user changes the selection. Cancelable.</summary>
    public event EventHandler<CancelRoutedEventArgs> SelectionChanging { add => AddHandler(SelectionChangingEvent, value); remove => RemoveHandler(SelectionChangingEvent, value); }

    /// <summary>Raised after the selection changed.</summary>
    public event EventHandler<RoutedEventArgs> SelectionChanged { add => AddHandler(SelectionChangedEvent, value); remove => RemoveHandler(SelectionChangedEvent, value); }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pressed = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_pressed)
        {
            return;
        }

        _pressed = false;
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            if (!IsSelectionDisabled)
            {
                SetCurrentValue(IsSelectedProperty, !IsSelected);
            }

            return;
        }

        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        Focus();
        switch (TapBehavior)
        {
            case TapBehavior.DirectSelect:
                if (!IsSelectionDisabled)
                {
                    SetCurrentValue(IsSelectedProperty, true);
                }

                RaiseEvent(new RoutedEventArgs(InvokedEvent));
                break;
            case TapBehavior.ToggleSelect:
                if (!IsSelectionDisabled)
                {
                    SetCurrentValue(IsSelectedProperty, !IsSelected);
                }

                RaiseEvent(new RoutedEventArgs(InvokedEvent));
                break;
            case TapBehavior.InvokeOnly:
                RaiseEvent(new RoutedEventArgs(InvokedEvent));
                break;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            RaiseEvent(new RoutedEventArgs(InvokedEvent));
            e.Handled = true;
        }
        else if (e.Key == Key.Space && !IsSelectionDisabled)
        {
            SetCurrentValue(IsSelectedProperty, !IsSelected);
            e.Handled = true;
        }
    }

    private void OnIsSelectedChanged(bool oldValue, bool newValue)
    {
        if (_reverting)
        {
            return;
        }

        var args = new CancelRoutedEventArgs(SelectionChangingEvent);
        RaiseEvent(args);
        if (args.Cancel)
        {
            _reverting = true;
            try
            {
                SetCurrentValue(IsSelectedProperty, oldValue);
            }
            finally
            {
                _reverting = false;
            }

            return;
        }

        RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
    }

    private void ApplySelectionStyle() =>
        ListStyle.SetSelectionStyle(this, SelectionStyle == ListViewSelectionStyle.Filled ? AvaWin.SelectionStyle.Filled : AvaWin.SelectionStyle.Bordered);
}
