using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Controls.Tests;

public class SemanticZoomTests
{
    private sealed record Group(string Key, string Title);

    private static (Window window, SemanticZoom zoom, ListView zin, ListView zout) Make(bool enableButton = true)
    {
        var items = Enumerable.Range(0, 60).Select(i => "G" + (i / 10) + "-" + i).ToList();
        var groups = Enumerable.Range(0, 6).Select(g => new Group("G" + g, "Group " + g)).ToList();
        var zin = new ListView
        {
            ItemsSource = items,
            GroupKeySelector = o => o!.ToString()!.Split('-')[0],
            GroupsSource = groups,
            GroupsKeySelector = g => ((Group)g!).Key,
            Height = 300,
        };
        zin.ContentAnimating += (_, e) => e.Cancel = true;
        var zout = new ListView { ItemsSource = groups, Layout = new GridLayout { ItemInfo = _ => new GridItemInfo(100, 100) }, Height = 300 };
        zout.ContentAnimating += (_, e) => e.Cancel = true;
        var zoom = new SemanticZoom { ZoomedInView = zin, ZoomedOutView = zout, EnableButton = enableButton, Width = 500, Height = 300 };
        var window = ThemeTestHelpers.Host(new Grid { Children = { zoom } }, "Light", Platform.Desktop, 600, 400);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, zoom, zin, zout);
    }

    [AvaloniaFact]
    public void IsZoomedOut_Toggles_Views_And_Button_Follows_EnableButton()
    {
        var (window, zoom, zin, zout) = Make();
        var inHost = zoom.GetVisualDescendants().OfType<Panel>().Single(p => p.Name == "PART_ZoomedInHost");
        var outHost = zoom.GetVisualDescendants().OfType<Panel>().Single(p => p.Name == "PART_ZoomedOutHost");
        var button = zoom.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_ZoomOutButton");
        Assert.True(inHost.IsVisible);
        Assert.False(outHost.IsVisible);
        Assert.True(button.IsVisible);
        Assert.Same(zin, inHost.Children[0]);
        Assert.Same(zout, outHost.Children[0]);

        var changed = new List<bool>();
        zoom.ZoomChanged += (_, e) => changed.Add(e.IsZoomedOut);
        AppBarTests.Click(window, button);
        window.UpdateLayout();
        Assert.True(zoom.IsZoomedOut);
        Assert.Contains(":zoomedout", zoom.Classes);
        Assert.True(outHost.IsVisible);
        Assert.False(inHost.IsVisible);
        Assert.False(button.IsVisible, "button hidden while zoomed out");
        Assert.Equal([true], changed);

        zoom.EnableButton = false;
        zoom.IsZoomedOut = false;
        window.UpdateLayout();
        Assert.False(button.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Zoom_Out_Positions_On_The_Group_And_Zoom_In_Returns_To_Its_First_Item()
    {
        var (window, zoom, zin, zout) = Make();
        zin.EnsureVisible(35);
        zin.CurrentIndex = 35;
        window.UpdateLayout();
        zoom.IsZoomedOut = true;
        window.UpdateLayout();
        Assert.Equal(3, zout.CurrentIndex);
        Assert.IsType<Group>(zoom.LastPositionedItem);
        Assert.Equal("G3", ((Group)zoom.LastPositionedItem!).Key);

        // Choose another group in the zoomed-out view: invoking an item zooms back in on it.
        zout.EnsureVisible(5);
        window.UpdateLayout();
        var item5 = zout.ContainerFromIndex(5)!;
        AppBarTests.Click(window, item5);
        window.UpdateLayout();
        Assert.False(zoom.IsZoomedOut);
        Assert.Equal("G5-50", zoom.LastPositionedItem);
        Assert.Equal(50, zin.CurrentIndex);
        Assert.True(zin.IndexOfFirstVisible >= 40);
        window.Close();
    }

    [AvaloniaFact]
    public void Locked_Ignores_Toggles_And_Ctrl_Wheel_Zooms()
    {
        var (window, zoom, _, _) = Make();
        zoom.IsLocked = true;
        zoom.Toggle();
        Assert.False(zoom.IsZoomedOut);
        zoom.IsZoomedOut = true;
        Assert.False(zoom.IsZoomedOut);
        zoom.IsLocked = false;
        var p = zoom.TranslatePoint(new Point(100, 100), window)!.Value;
        window.MouseWheel(p, new Vector(0, -1), RawInputModifiers.Control);
        Assert.True(zoom.IsZoomedOut);
        window.MouseWheel(p, new Vector(0, 1), RawInputModifiers.Control);
        Assert.False(zoom.IsZoomedOut);
        window.Close();
    }

    [AvaloniaFact]
    public void Non_Zoomable_View_Throws()
    {
        Assert.Throws<System.InvalidOperationException>(() => new SemanticZoom { ZoomedInView = new Border() });
    }
}
