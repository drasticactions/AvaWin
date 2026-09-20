using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AvaWin.Controls;

/// <summary>A TextBlock that colors every occurrence of <see cref="Highlight"/> with <see cref="HighlightBrush"/>.</summary>
public sealed class HighlightTextBlock : TextBlock
{
    /// <summary>Defines the <see cref="Highlight"/> property.</summary>
    public static readonly StyledProperty<string?> HighlightProperty = AvaloniaProperty.Register<HighlightTextBlock, string?>(nameof(Highlight));

    /// <summary>Defines the <see cref="HighlightBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> HighlightBrushProperty = AvaloniaProperty.Register<HighlightTextBlock, IBrush?>(nameof(HighlightBrush));

    /// <summary>Defines the <see cref="FullText"/> property.</summary>
    public static readonly StyledProperty<string?> FullTextProperty = AvaloniaProperty.Register<HighlightTextBlock, string?>(nameof(FullText));

    static HighlightTextBlock()
    {
        HighlightProperty.Changed.AddClassHandler<HighlightTextBlock>((t, _) => t.Rebuild());
        HighlightBrushProperty.Changed.AddClassHandler<HighlightTextBlock>((t, _) => t.Rebuild());
        FullTextProperty.Changed.AddClassHandler<HighlightTextBlock>((t, _) => t.Rebuild());
    }

    /// <summary>The substring to highlight (case-insensitive).</summary>
    public string? Highlight { get => GetValue(HighlightProperty); set => SetValue(HighlightProperty, value); }

    /// <summary>Brush for the highlighted runs.</summary>
    public IBrush? HighlightBrush { get => GetValue(HighlightBrushProperty); set => SetValue(HighlightBrushProperty, value); }

    /// <summary>The whole text (use instead of <see cref="TextBlock.Text"/>).</summary>
    public string? FullText { get => GetValue(FullTextProperty); set => SetValue(FullTextProperty, value); }

    private void Rebuild()
    {
        var text = FullText ?? string.Empty;
        var hl = Highlight;
        var inlines = new InlineCollection();
        if (string.IsNullOrEmpty(hl) || HighlightBrush is null)
        {
            inlines.Add(new Run(text));
        }
        else
        {
            var pos = 0;
            while (pos < text.Length)
            {
                var idx = text.IndexOf(hl, pos, StringComparison.CurrentCultureIgnoreCase);
                if (idx < 0)
                {
                    inlines.Add(new Run(text[pos..]));
                    break;
                }

                if (idx > pos)
                {
                    inlines.Add(new Run(text[pos..idx]));
                }

                inlines.Add(new Run(text.Substring(idx, hl.Length)) { Foreground = HighlightBrush });
                pos = idx + hl.Length;
            }
        }

        Inlines = inlines;
    }
}
