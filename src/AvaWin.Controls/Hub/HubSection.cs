using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace AvaWin.Controls;

/// <summary>
/// One section of a <see cref="Hub"/>: a header, interactive with a chevron or static, above its content.
/// </summary>
[TemplatePart("PART_HeaderButton", typeof(Button))]
[TemplatePart("PART_Chevron", typeof(TextBlock))]
[TemplatePart("PART_StaticHeader", typeof(ContentPresenter))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter), IsRequired = true)]
[PseudoClasses(":static", ":interactive", ":vertical", ":horizontal")]
public sealed class HubSection : HeaderedContentControl
{
    /// <summary>Defines the <see cref="IsHeaderStatic"/> property.</summary>
    public static readonly StyledProperty<bool> IsHeaderStaticProperty = AvaloniaProperty.Register<HubSection, bool>(nameof(IsHeaderStatic));

    /// <summary>Defines the <see cref="Orientation"/> property (set by the owning <see cref="Hub"/>).</summary>
    public static readonly StyledProperty<Avalonia.Layout.Orientation> OrientationProperty = AvaloniaProperty.Register<HubSection, Avalonia.Layout.Orientation>(nameof(Orientation));

    /// <summary>Defines the <see cref="HeaderInvoked"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> HeaderInvokedEvent = RoutedEvent.Register<HubSection, RoutedEventArgs>(nameof(HeaderInvoked), RoutingStrategies.Bubble);

    private Button? _headerButton;

    static HubSection()
    {
        IsHeaderStaticProperty.Changed.AddClassHandler<HubSection>((s, _) => s.UpdatePseudoClasses());
        OrientationProperty.Changed.AddClassHandler<HubSection>((s, _) => s.UpdatePseudoClasses());
        FocusableProperty.OverrideDefaultValue<HubSection>(false);
    }

    /// <summary>Initializes a new instance.</summary>
    public HubSection()
    {
        UpdatePseudoClasses();
    }

    /// <summary>A static header has no chevron and does not raise <see cref="HeaderInvoked"/>.</summary>
    public bool IsHeaderStatic { get => GetValue(IsHeaderStaticProperty); set => SetValue(IsHeaderStaticProperty, value); }

    /// <summary>The orientation of the owning hub. It sets the padding and margins of the section.</summary>
    public Avalonia.Layout.Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Raised when the interactive header is clicked.</summary>
    public event EventHandler<RoutedEventArgs> HeaderInvoked { add => AddHandler(HeaderInvokedEvent, value); remove => RemoveHandler(HeaderInvokedEvent, value); }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_headerButton is { } old)
        {
            old.Click -= OnHeaderClick;
        }

        _headerButton = e.NameScope.Find<Button>("PART_HeaderButton");
        if (_headerButton is { } b)
        {
            b.Click += OnHeaderClick;
        }
    }

    private void OnHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (IsHeaderStatic)
        {
            return;
        }

        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(HeaderInvokedEvent));
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":static", IsHeaderStatic);
        PseudoClasses.Set(":interactive", !IsHeaderStatic);
        PseudoClasses.Set(":vertical", Orientation == Avalonia.Layout.Orientation.Vertical);
        PseudoClasses.Set(":horizontal", Orientation == Avalonia.Layout.Orientation.Horizontal);
    }
}

/// <summary>Arguments for <see cref="Hub.HeaderInvoked"/>.</summary>
public sealed class HubHeaderInvokedEventArgs : RoutedEventArgs
{
    internal HubHeaderInvokedEventArgs(RoutedEvent routedEvent, int index, HubSection section) : base(routedEvent)
    {
        Index = index;
        Section = section;
    }

    /// <summary>The index of the section in the hub.</summary>
    public int Index { get; }

    /// <summary>The section whose header was invoked.</summary>
    public HubSection Section { get; }
}

/// <summary>The animations a <see cref="Hub"/> runs on its sections.</summary>
public enum HubAnimationType
{
    /// <summary>The first-load entrance animation.</summary>
    Entrance,

    /// <summary>The content transition after the sections change.</summary>
    ContentTransition,

    /// <summary>A section was inserted.</summary>
    Insert,

    /// <summary>A section was removed.</summary>
    Remove,
}

/// <summary>Arguments for <see cref="Hub.ContentAnimating"/> (cancelable).</summary>
public sealed class HubContentAnimatingEventArgs : RoutedEventArgs
{
    internal HubContentAnimatingEventArgs(RoutedEvent routedEvent, HubAnimationType type, int index, HubSection? section) : base(routedEvent)
    {
        Type = type;
        Index = index;
        Section = section;
    }

    /// <summary>The animation about to run.</summary>
    public HubAnimationType Type { get; }

    /// <summary>The section index, or −1 for the entrance.</summary>
    public int Index { get; }

    /// <summary>The section, or null for the entrance.</summary>
    public HubSection? Section { get; }

    /// <summary>Set to true to skip the animation.</summary>
    public bool Cancel { get; set; }
}

/// <summary>How far a <see cref="Hub"/> has loaded.</summary>
public enum HubLoadingState
{
    /// <summary>The sections are laid out.</summary>
    Loading,

    /// <summary>All sections are loaded and the entrance animation has finished.</summary>
    Complete,
}
