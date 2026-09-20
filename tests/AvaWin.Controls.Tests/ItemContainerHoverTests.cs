using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace AvaWin.Controls.Tests;

public class ItemContainerHoverTests
{
    /// <summary>
    /// The 3px hover outline overhangs the item. A TemplatedControl clips to its bounds by default, so a host that
    /// puts the item at its own edge must turn ClipToBounds off (the gallery page frame does) for the outline to show.
    /// </summary>
    [AvaloniaFact]
    public void Hover_Outline_Is_Drawn_On_All_Sides_At_The_Edge_Of_An_Unclipped_Host()
    {
        var column = new StackPanel { Spacing = 12, Width = 360, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        for (var i = 0; i < 3; i++)
        {
            column.Children.Add(new ItemContainer { Content = new TextBlock { Text = "Item " + i, Margin = new Thickness(10, 7) } });
        }

        var host = new ContentControl { Content = column, Margin = new Thickness(20), ClipToBounds = false };
        var window = ThemeTestHelpers.Host(new Border { Background = Brushes.White, Child = new ScrollViewer { Content = host } }, "Light", Platform.Desktop, 600, 400);
        var item = column.Children.OfType<ItemContainer>().ElementAt(1);
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
        Assert.True(Red((int)topLeft.X - 2, midY) < 235, "left edge (at the host edge)");
        Assert.True(Red((int)(topLeft.X + item.Bounds.Width) + 1, midY) < 235, "right edge");
        window.Close();
    }
}
