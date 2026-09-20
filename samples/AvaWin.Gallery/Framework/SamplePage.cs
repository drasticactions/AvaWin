using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace AvaWin.Gallery.Framework;

/// <summary>
/// A control page: a one-line description, a hero demo that fills the viewport like a real app screen, then
/// named <see cref="Scenario"/> sections. Sets the <c>:narrow</c> pseudo-class below <see cref="NarrowWidth"/>
/// so page XAML can re-flow with a style selector.
/// </summary>
[PseudoClasses(":narrow")]
public class SamplePage : TemplatedControl
{
    /// <summary>Below this width the page is laid out for a handset.</summary>
    public const double NarrowWidth = 640;

    /// <summary>The hero never gets shorter than this when it fills the viewport.</summary>
    private const double MinHeroHeight = 320;

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<SamplePage, string?>(nameof(Description));
    public static readonly StyledProperty<object?> HeroProperty = AvaloniaProperty.Register<SamplePage, object?>(nameof(Hero));
    public static readonly StyledProperty<bool> HeroFillProperty = AvaloniaProperty.Register<SamplePage, bool>(nameof(HeroFill), true);
    public static readonly StyledProperty<double> HeroHeightProperty = AvaloniaProperty.Register<SamplePage, double>(nameof(HeroHeight), double.NaN);
    public static readonly DirectProperty<SamplePage, bool> IsNarrowProperty = AvaloniaProperty.RegisterDirect<SamplePage, bool>(nameof(IsNarrow), o => o.IsNarrow);
    public static readonly DirectProperty<SamplePage, AvaloniaList<Scenario>> ScenariosProperty = AvaloniaProperty.RegisterDirect<SamplePage, AvaloniaList<Scenario>>(nameof(Scenarios), o => o.Scenarios);

    private ScrollViewer? _scroller;
    private bool _isNarrow;

    public SamplePage()
    {
        SizeChanged += (_, e) => IsNarrow = e.NewSize.Width < NarrowWidth;
    }

    /// <summary>Every page subclass shares the one SamplePage control theme.</summary>
    protected override Type StyleKeyOverride => typeof(SamplePage);

    /// <summary>One or two sentences under the title: what the control is for.</summary>
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>The main demo, shown first at app-screen size.</summary>
    public object? Hero { get => GetValue(HeroProperty); set => SetValue(HeroProperty, value); }

    /// <summary>True (default): the hero is as tall as the page viewport. False: it sizes to its content.</summary>
    public bool HeroFill { get => GetValue(HeroFillProperty); set => SetValue(HeroFillProperty, value); }

    /// <summary>The hero height the template binds to; NaN sizes to content.</summary>
    public double HeroHeight { get => GetValue(HeroHeightProperty); private set => SetValue(HeroHeightProperty, value); }

    /// <summary>The named sections under the hero.</summary>
    [Content]
    public AvaloniaList<Scenario> Scenarios { get; } = new();

    public bool IsNarrow
    {
        get => _isNarrow;
        private set
        {
            if (SetAndRaise(IsNarrowProperty, ref _isNarrow, value))
            {
                PseudoClasses.Set(":narrow", value);
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The page host scrolls, so the hero sees infinite height; size it to the viewport instead. The viewport is
        // read after each layout pass, not from a Viewport change, which arrives mid-arrange: a HeroHeight set there
        // leaves the page host arranged at its old size.
        _scroller = this.FindAncestorOfType<ScrollViewer>();
        EffectiveViewportChanged += OnEffectiveViewportChanged;
        UpdateHeroHeight();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
        _scroller = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e) => UpdateHeroHeight();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HeroFillProperty)
        {
            UpdateHeroHeight();
        }
    }

    private void UpdateHeroHeight()
    {
        if (!HeroFill || _scroller is null)
        {
            HeroHeight = double.NaN;
            return;
        }

        // Hero bottom edge lands on the viewport's bottom edge at scroll offset 0.
        var description = this.FindDescendantOfType<TextBlock>() is { Name: "PART_Description", IsVisible: true } d ? d.Bounds.Height + 16 : 0;
        var margin = ((_scroller.Content as Control)?.Margin.Bottom ?? 0) + _scroller.Padding.Bottom;
        HeroHeight = Math.Max(MinHeroHeight, _scroller.Viewport.Height - description - margin);
    }
}
