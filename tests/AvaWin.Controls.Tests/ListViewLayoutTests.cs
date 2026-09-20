using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Controls.Tests;

public class ListViewLayoutTests
{
    private static (Window window, ListView list) Host(ListView list, double width = 500, double height = 400)
    {
        list.ContentAnimating += (_, e) => e.Cancel = true;
        var window = ThemeTestHelpers.Host(new Grid { Children = { list } }, "Light", Platform.Desktop, width, height);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, list);
    }

    private static ListViewPanel Panel(ListView list) => list.GetVisualDescendants().OfType<ListViewPanel>().Single();

    [AvaloniaFact]
    public void GridLayout_Wraps_Across_And_Scrolls_Along_Orientation()
    {
        var list = new ListView
        {
            ItemsSource = Enumerable.Range(0, 100).Select(i => "I" + i).ToList(),
            Layout = new GridLayout { ItemInfo = _ => new GridItemInfo(100, 100) },
            Width = 480,
            Height = 330,
        };
        var (window, _) = Host(list);
        var panel = Panel(list);
        Assert.Contains("win-grid", list.Classes);
        Assert.Equal(3, panel.ItemsPerLine); // 330 / (100 + 10 margin) = 3 rows
        var a = list.ContainerFromIndex(0)!;
        var b = list.ContainerFromIndex(1)!;
        var c = list.ContainerFromIndex(3)!;
        Assert.Equal(a.Bounds.X, b.Bounds.X, 0.5);
        Assert.True(b.Bounds.Y > a.Bounds.Y, "second item is in the next row of the same column");
        Assert.True(c.Bounds.X > a.Bounds.X, "fourth item starts the next column");
        Assert.True(panel.RealizedCount < 40);

        // Keyboard: Right moves a column (3 items), Down moves one row.
        list.EnsureVisible(0);
        list.ContainerFromIndex(0)!.Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Equal(3, list.CurrentIndex);
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.Equal(4, list.CurrentIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void GridLayout_MaximumRowsOrColumns_Caps_Lines()
    {
        var list = new ListView
        {
            ItemsSource = Enumerable.Range(0, 20).Select(i => "I" + i).ToList(),
            Layout = new GridLayout { ItemInfo = _ => new GridItemInfo(60, 60), MaximumRowsOrColumns = 2, Orientation = Orientation.Vertical },
            Width = 400,
            Height = 300,
        };
        var (window, _) = Host(list);
        Assert.Equal(2, Panel(list).ItemsPerLine);
        var a = list.ContainerFromIndex(0)!;
        var b = list.ContainerFromIndex(1)!;
        var c = list.ContainerFromIndex(2)!;
        Assert.Equal(a.Bounds.Y, b.Bounds.Y, 0.5);
        Assert.True(c.Bounds.Y > a.Bounds.Y);
        window.Close();
    }

    [AvaloniaFact]
    public void CellSpanningLayout_Packs_With_Occupancy_Map()
    {
        // cell 100×100, 3 rows; item 0 spans 2×2, then 1×1 items fill the remaining row of the first columns.
        var sizes = new[] { new GridItemInfo(200, 200), new GridItemInfo(100, 100), new GridItemInfo(100, 100), new GridItemInfo(100, 100), new GridItemInfo(100, 300) };
        var list = new ListView
        {
            ItemsSource = Enumerable.Range(0, sizes.Length).Select(i => "I" + i).ToList(),
            Layout = new CellSpanningLayout { ItemInfo = i => sizes[i], GroupInfo = _ => new GroupInfo(true, 100, 100) },
            Width = 600,
            Height = 300,
        };
        var (window, _) = Host(list);
        var panel = Panel(list);
        Assert.True(panel.IsCellSpanning);
        var r0 = panel.RectOf(0);
        var r1 = panel.RectOf(1);
        var r2 = panel.RectOf(2);
        var r3 = panel.RectOf(3);
        var r4 = panel.RectOf(4);
        Assert.Equal(new Rect(0, 0, 200, 200), r0);
        Assert.Equal(new Rect(0, 200, 100, 100), r1);   // under the big item, first column
        Assert.Equal(new Rect(100, 200, 100, 100), r2); // under the big item, second column
        Assert.Equal(new Rect(200, 0, 100, 100), r3);   // next free column
        Assert.Equal(new Rect(300, 0, 100, 300), r4);   // full-height item needs an empty column
        Assert.Equal(400, panel.DesiredSize.Width, 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void GridLayout_With_Spanning_GroupInfo_Uses_Occupancy_Map()
    {
        var list = new ListView
        {
            ItemsSource = Enumerable.Range(0, 6).Select(i => "I" + i).ToList(),
            Layout = new GridLayout { ItemInfo = i => i == 0 ? new GridItemInfo(200, 100) : new GridItemInfo(100, 100), GroupInfo = _ => new GroupInfo(true, 100, 100) },
            Width = 600,
            Height = 200,
        };
        var (window, _) = Host(list);
        Assert.True(Panel(list).IsCellSpanning);
        Assert.Equal(200, Panel(list).RectOf(0).Width, 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void GroupHeaderPosition_Left_Puts_Header_Before_Group_In_Horizontal_Grid()
    {
        var items = Enumerable.Range(0, 12).Select(i => "G" + (i / 6) + "-" + i).ToList();
        var list = new ListView
        {
            ItemsSource = items,
            GroupKeySelector = o => o!.ToString()!.Split('-')[0],
            Layout = new GridLayout { ItemInfo = _ => new GridItemInfo(80, 80), GroupHeaderPosition = GroupHeaderPosition.Left },
            Width = 700,
            Height = 300,
        };
        var (window, _) = Host(list);
        var panel = Panel(list);
        var header = panel.RealizedHeaders[0];
        var first = list.ContainerFromIndex(0)!;
        Assert.True(first.Bounds.X >= header.Bounds.Right - 0.5, "header runs along the scroll axis before the group");
        Assert.True(first.Bounds.Y < 10, "first row starts at the top (item margin only)");

        list.Layout = new GridLayout { ItemInfo = _ => new GridItemInfo(80, 80), GroupHeaderPosition = GroupHeaderPosition.Top };
        window.UpdateLayout();
        panel = Panel(list);
        header = panel.RealizedHeaders[0];
        first = list.ContainerFromIndex(0)!;
        Assert.True(first.Bounds.Y >= header.Bounds.Bottom - 0.5, "Top header is a band above the group's rows");
        Assert.True(first.Bounds.X < 10, "first column starts at the left (item margin only)");
        window.Close();
    }

    [AvaloniaFact]
    public void Phone_Selection_Mode_Shifts_Vertical_List_Items()
    {
        var list = new ListView { ItemsSource = Enumerable.Range(0, 10).Select(i => "I" + i).ToList(), Width = 300, Height = 300 };
        var (window, _) = Host(list);
        var item = (ListViewItem)list.ContainerFromIndex(0)!;
        Assert.Equal(7, item.Margin.Left, 0.5);
        list.IsSelectionModeActive = true;
        window.UpdateLayout();
        Assert.Contains(":selectionmode", list.Classes);
        Assert.Contains(":selectionmode", item.Classes);
        var check = item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_PhoneCheck");
        Assert.True(check.IsVisible);
        list.IsSelectionModeActive = false;
        window.UpdateLayout();
        Assert.False(check.IsVisible);
        window.Close();
    }
}
