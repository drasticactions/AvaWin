using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace AvaWin.Theme.Tests;

public class TypographyClipTests
{
    /// <summary>The descenders of Selawik reach below its hhea descent, and the display line heights are shorter than
    /// the natural line box. Text must draw outside its bounds, like a browser does. Only xx-large (56px type in a
    /// 64px line) overhangs by whole pixels; at x-large and below the overhang is sub-pixel and leaves no ink.</summary>
    [AvaloniaTheory]
    [InlineData("win-type-xx-large")]
    public void Text_Draws_Its_Descenders_Below_The_Line_Box(string cls)
    {
        var text = new TextBlock { Text = "ggg", Classes = { cls }, TextTrimming = TextTrimming.CharacterEllipsis, Background = Brushes.White, Foreground = Brushes.Black, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        var panel = new Border { Background = Brushes.White, Child = text, Padding = new Thickness(0, 0, 0, 40) };
        var window = ThemeTestHelpers.Host(panel, "Light", Platform.Desktop, 300, 200);
        Assert.False(text.ClipToBounds);
        var bounds = text.Bounds;
        var frame = window.CaptureRenderedFrame()!;
        // Any inked pixel below the TextBlock bounds is a descender that was not clipped (sub-pixel at body sizes).
        var dark = 0;
        using (var fb = frame.Lock())
        {
            var row = new byte[fb.RowBytes];
            for (var y = (int)System.Math.Ceiling(bounds.Bottom); y < (int)bounds.Bottom + 12; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(fb.Address + y * fb.RowBytes, row, 0, fb.RowBytes);
                for (var x = 0; x < 200; x++)
                {
                    if (row[x * 4] < 235) dark++;
                }
            }
        }

        Assert.True(dark > 0, $"bounds {bounds}, no ink below the line box");
        window.Close();
    }
}
