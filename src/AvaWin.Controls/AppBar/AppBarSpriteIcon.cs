using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AvaWin.Controls;

/// <summary>
/// Shows one 40×40 cell of a 160×80 icon sprite. The columns are the pointer states: rest, hover, pressed and
/// disabled. The rows are normal and selected.
/// </summary>
public sealed class AppBarSpriteIcon : Control
{
    /// <summary>Defines the <see cref="Source"/> property.</summary>
    public static readonly StyledProperty<IImage?> SourceProperty = AvaloniaProperty.Register<AppBarSpriteIcon, IImage?>(nameof(Source));

    /// <summary>The sprite image.</summary>
    public IImage? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    /// <summary>The command whose state selects the sprite cell.</summary>
    public AppBarCommand? Owner { get; set; }

    static AppBarSpriteIcon()
    {
        AffectsRender<AppBarSpriteIcon>(SourceProperty);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Owner is { } o)
        {
            o.PropertyChanged += OnOwnerChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Owner is { } o)
        {
            o.PropertyChanged -= OnOwnerChanged;
        }
    }

    private void OnOwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Button.IsPressedProperty || e.Property == InputElement.IsPointerOverProperty ||
            e.Property == InputElement.IsEnabledProperty || e.Property == AppBarCommand.IsSelectedProperty)
        {
            InvalidateVisual();
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) => new(40, 40);

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (Source is not { } image)
        {
            return;
        }

        var column = Owner switch
        {
            { IsEnabled: false } => 3,
            { IsPressed: true } => 2,
            { IsPointerOver: true } => 1,
            _ => 0,
        };
        var row = Owner is { IsSelected: true } ? 1 : 0;
        var cell = new Rect(column * 40, row * 40, 40, 40);
        var dest = new Rect(0, 0, 40, 40);
        context.DrawImage(image, cell, dest);
    }
}
