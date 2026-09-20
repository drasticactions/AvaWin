using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaWin.Controls;

/// <summary>The header of a group. It is focusable and raises <see cref="ListView.GroupHeaderInvoked"/> when tapped.</summary>
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter), IsRequired = true)]
[TemplatePart("PART_FocusOutline", typeof(Border))]
[PseudoClasses(":pressed")]
public sealed class ListViewGroupHeader : ContentControl
{
    /// <summary>Defines the <see cref="Group"/> property.</summary>
    public static readonly DirectProperty<ListViewGroupHeader, ListViewGroupInfo?> GroupProperty = AvaloniaProperty.RegisterDirect<ListViewGroupHeader, ListViewGroupInfo?>(nameof(Group), o => o.Group);

    /// <summary>Defines the <see cref="Invoked"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> InvokedEvent = RoutedEvent.Register<ListViewGroupHeader, RoutedEventArgs>(nameof(Invoked), RoutingStrategies.Bubble);

    private ListViewGroupInfo? _group;

    static ListViewGroupHeader()
    {
        PressedMixin.Attach<ListViewGroupHeader>();
        FocusableProperty.OverrideDefaultValue<ListViewGroupHeader>(true);
    }

    /// <summary>The group this header belongs to.</summary>
    public ListViewGroupInfo? Group { get => _group; private set => SetAndRaise(GroupProperty, ref _group, value); }

    /// <summary>The index of the group.</summary>
    public int GroupIndex { get; private set; } = -1;

    /// <summary>Raised on a tap, Enter or Space.</summary>
    public event System.EventHandler<RoutedEventArgs> Invoked { add => AddHandler(InvokedEvent, value); remove => RemoveHandler(InvokedEvent, value); }

    internal void Bind(ListViewGroupInfo group, int index)
    {
        Group = group;
        GroupIndex = index;
        Content = group.Header;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton == MouseButton.Left && !e.Handled)
        {
            RaiseEvent(new RoutedEventArgs(InvokedEvent));
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key is Key.Enter or Key.Space)
        {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(InvokedEvent));
        }
    }
}
