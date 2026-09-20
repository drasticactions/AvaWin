using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Theme.Tests;

public class ListBoxHoverTests
{
    /// <summary>The 3px hover outline overhangs the item; it must show on all four sides, over the neighbors and at the list edges.</summary>
    [AvaloniaFact]
    public void Hover_Outline_Is_Drawn_On_All_Sides()
    {
        var list = new ListBox { Width = 200, Background = Brushes.White };
        for (var i = 0; i < 3; i++)
        {
            list.Items.Add(new ListBoxItem { Content = "Item " + i, Height = 40 });
        }

        var window = ThemeTestHelpers.Host(new Border { Background = Brushes.White, Padding = new Thickness(20), Child = list }, "Light", Platform.Desktop, 300, 300);
        var item = list.GetVisualDescendants().OfType<ListBoxItem>().ElementAt(1);
        var center = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center);
        window.UpdateLayout();
        Assert.Contains(":pointerover", item.Classes);

        var topLeft = item.TranslatePoint(new Point(0, 0), window)!.Value;
        var frame = window.CaptureRenderedFrame()!;
        using var fb = frame.Lock();
        var row = new byte[fb.RowBytes];
        byte Red(int x, int y)
        {
            System.Runtime.InteropServices.Marshal.Copy(fb.Address + y * fb.RowBytes, row, 0, fb.RowBytes);
            return row[x * 4 + 2];
        }

        var midX = (int)(topLeft.X + item.Bounds.Width / 2);
        var midY = (int)(topLeft.Y + item.Bounds.Height / 2);
        // 2px outside each edge: inside the 3px outline, over the neighbor item above/below and past the left/right item edge.
        Assert.True(Red(midX, (int)topLeft.Y - 2) < 235, "top edge");
        Assert.True(Red(midX, (int)(topLeft.Y + item.Bounds.Height) + 1) < 235, "bottom edge (over the next item)");
        Assert.True(Red((int)topLeft.X - 2, midY) < 235, "left edge (inside the scroll clip)");
        Assert.True(Red((int)(topLeft.X + item.Bounds.Width) + 1, midY) < 235, "right edge");
        window.Close();
    }
}
