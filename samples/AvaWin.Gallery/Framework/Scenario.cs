using Avalonia;
using Avalonia.Controls;

namespace AvaWin.Gallery.Framework;

/// <summary>One named section of a <see cref="SamplePage"/>: a title, a sentence on what it shows, and the demo.</summary>
public class Scenario : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<Scenario, string?>(nameof(Title));
    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<Scenario, string?>(nameof(Description));

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
}
