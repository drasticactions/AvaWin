using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Media;
using AvaWin.Animations;

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
[PseudoClasses(":selected", ":pressed", ":backdrop", ":nonselectable", ":dragsource", ":dragover", ":selectionmode", ":swipeselect")]
public class ListViewItem : ContentControl, ISelectable
{
    /// <summary>Defines the <see cref="IsSelected"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty = Avalonia.Controls.Primitives.SelectingItemsControl.IsSelectedProperty.AddOwner<ListViewItem>();

    /// <summary>Defines the <see cref="IsSelectable"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectableProperty = AvaloniaProperty.Register<ListViewItem, bool>(nameof(IsSelectable), true);

    /// <summary>Defines the <see cref="IsBackdrop"/> property.</summary>
    public static readonly StyledProperty<bool> IsBackdropProperty = AvaloniaProperty.Register<ListViewItem, bool>(nameof(IsBackdrop));

    /// <summary>Defines the <see cref="PressFeedback"/> property. It is inherited, so a <see cref="ListView"/> sets it for its items.</summary>
    public static readonly AttachedProperty<PressFeedback> PressFeedbackProperty =
        AvaloniaProperty.RegisterAttached<ListViewItem, Control, PressFeedback>(nameof(PressFeedback), PressFeedback.Scale, inherits: true);

    private IPointer? _tiltPointer;
    private int _tiltGeneration;

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

    /// <summary>How the item answers a press: scale (default), tilt toward the contact, or nothing.</summary>
    public PressFeedback PressFeedback { get => GetValue(PressFeedbackProperty); set => SetValue(PressFeedbackProperty, value); }

    /// <summary>Gets the press feedback of the items under <paramref name="element"/>.</summary>
    public static PressFeedback GetPressFeedback(Control element) => element.GetValue(PressFeedbackProperty);

    /// <summary>Sets the press feedback of the items under <paramref name="element"/>.</summary>
    public static void SetPressFeedback(Control element, PressFeedback value) => element.SetValue(PressFeedbackProperty, value);

    /// <summary>Whether the item shows the placeholder look while its content loads.</summary>
    public bool IsBackdrop { get => GetValue(IsBackdropProperty); set => SetValue(IsBackdropProperty, value); }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (PressFeedback == PressFeedback.Tilt && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _tiltPointer = e.Pointer;
            _tiltGeneration++;
            Transitions = null;
            _ = WinAnimations.PointerDownTilt(this, e.GetPosition(this));
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_tiltPointer is not null && e.Pointer == _tiltPointer && RenderTransform is TransformGroup)
        {
            _ = WinAnimations.PointerDownTilt(this, e.GetPosition(this));
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer == _tiltPointer)
        {
            ReleaseTilt();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (e.Pointer == _tiltPointer)
        {
            ReleaseTilt();
        }
    }

    private async void ReleaseTilt()
    {
        _tiltPointer = null;
        var generation = _tiltGeneration;
        await WinAnimations.PointerUpTilt(this);
        if (generation == _tiltGeneration && !IsSwiping)
        {
            if (RenderTransform is not TransformGroup)
            {
                ClearValue(RenderTransformOriginProperty);
            }

            ClearValue(TransitionsProperty);
        }
    }

    internal void SetDragState(bool source, bool over)
    {
        PseudoClasses.Set(":dragsource", source);
        PseudoClasses.Set(":dragover", over);
    }

    internal bool IsSwiping { get; set; }

    internal void SetSwipeState(bool select) => PseudoClasses.Set(":swipeselect", select);

    internal void SetSelectionModeActive(bool active) => PseudoClasses.Set(":selectionmode", active);
}
