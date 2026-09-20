using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Controls.Tests;

public class NavBarTests
{
    private static (Window window, NavBarContainer container) Make(int count, double width = 700, bool fixedSize = false, Orientation layout = Orientation.Horizontal, int maxRows = 1)
    {
        var c = new NavBarContainer { FixedSize = fixedSize, Layout = layout, MaxRows = maxRows };
        for (var i = 0; i < count; i++)
        {
            c.Items.Add(new NavBarCommand { Label = "Item " + i, Icon = AppBarIcon.Home, Location = "/page" + i, SplitButton = i % 2 == 1 });
        }

        var window = ThemeTestHelpers.Host(new Grid { Children = { c } }, "Light", Platform.Desktop, width, 400);
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, c);
    }

    [AvaloniaFact]
    public void Horizontal_Layout_Pages_And_Fills_Width()
    {
        var (window, c) = Make(7, 700);
        // usable = 700 - 50 = 650; 210+10 margin = 220 → 2 columns per page → 4 pages
        Assert.Equal(4, c.PageCount);
        Assert.Equal(0, c.CurrentPage);
        Assert.Contains(":paged", c.Classes);
        Assert.Contains(":hasnext", c.Classes);
        Assert.DoesNotContain(":hasprevious", c.Classes);
        var cmds = c.GetVisualDescendants().OfType<NavBarCommand>().ToList();
        Assert.Equal(7, cmds.Count);
        Assert.True(cmds[0].Bounds.Width > 210, "commands stretch when FixedSize is false");
        Assert.Equal(cmds[0].Bounds.Y, cmds[1].Bounds.Y, 0.5);
        Assert.True(cmds[2].Bounds.X >= 700, "third item is on page 2");
        window.Close();
    }

    [AvaloniaFact]
    public void FixedSize_Keeps_Command_Width()
    {
        var (window, c) = Make(3, 700, fixedSize: true);
        var cmds = c.GetVisualDescendants().OfType<NavBarCommand>().ToList();
        Assert.Equal(210, cmds[0].Bounds.Width, 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void MaxRows_Fills_Column_First()
    {
        var (window, c) = Make(6, 700, maxRows: 2);
        Assert.Equal(2, c.PageCount);
        var cmds = c.GetVisualDescendants().OfType<NavBarCommand>().ToList();
        Assert.Equal(cmds[0].Bounds.X, cmds[1].Bounds.X, 0.5);
        Assert.True(cmds[1].Bounds.Y > cmds[0].Bounds.Y);
        Assert.True(cmds[2].Bounds.X > cmds[0].Bounds.X);
        window.Close();
    }

    [AvaloniaFact]
    public void Arrows_And_Indicators_Page()
    {
        var (window, c) = Make(7, 700);
        var viewport = c.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "PART_Viewport");
        c.NextPage();
        window.UpdateLayout();
        Assert.Equal(1, c.CurrentPage);
        Assert.Equal(2, c.CurrentIndex);
        Assert.Equal(700, viewport.Offset.X, 0.5);
        Assert.Contains(":hasprevious", c.Classes);
        var right = c.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_RightArrow");
        right.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Assert.Equal(2, c.CurrentPage);
        var dots = c.GetVisualDescendants().OfType<StackPanel>().Single(s => s.Name == "PART_PageIndicators").Children;
        Assert.Equal(4, dots.Count);
        Assert.Contains("current", dots[2].Classes);
        ((Button)dots[0]).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Assert.Equal(0, c.CurrentPage);
        c.CurrentIndex = 6;
        window.UpdateLayout();
        Assert.Equal(3, c.CurrentPage);
        Assert.DoesNotContain(":hasnext", c.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Wheel_Deltas_Accumulate_To_One_Page_Per_Notch()
    {
        var (window, c) = Make(7, 700);
        var p = c.TranslatePoint(new Point(300, 60), window)!.Value;
        // A trackpad sends many small deltas: five of −0.3 must add up to a single page turn, not five.
        for (var i = 0; i < 5; i++)
        {
            window.MouseWheel(p, new Vector(-0.3, 0));
        }

        window.UpdateLayout();
        Assert.Equal(1, c.CurrentPage);
        // A whole mouse notch pages once more; the opposite direction comes back.
        window.MouseWheel(p, new Vector(0, -1));
        window.UpdateLayout();
        Assert.Equal(2, c.CurrentPage);
        window.MouseWheel(p, new Vector(0, 1));
        window.UpdateLayout();
        Assert.Equal(1, c.CurrentPage);
        window.Close();
    }

    [AvaloniaFact]
    public void Invoked_And_SplitToggle_Carry_Index_And_Data()
    {
        var (window, c) = Make(3);
        var invoked = new List<(int, object?)>();
        var toggled = new List<(int, bool)>();
        c.Invoked += (_, e) => invoked.Add((e.Index, e.Data));
        c.SplitToggle += (_, e) => toggled.Add((e.Index, e.Opened));
        var second = (NavBarCommand)c.Items[1]!;
        var main = second.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Button");
        var split = second.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_SplitButton");
        AppBarTests.Click(window, main);
        Assert.Single(invoked);
        Assert.Equal(1, invoked[0].Item1);
        Assert.Same(second, invoked[0].Item2);
        AppBarTests.Click(window, split);
        Assert.True(second.SplitOpened);
        Assert.Contains(":splitopened", second.Classes);
        Assert.Equal([(1, true)], toggled);
        AppBarTests.Click(window, split);
        Assert.False(second.SplitOpened);
        window.Close();
    }

    [AvaloniaFact]
    public void Data_Items_Get_Commands()
    {
        var c = new NavBarContainer { ItemsSource = new[] { "Alpha", "Beta" } };
        var window = ThemeTestHelpers.Host(new Grid { Children = { c } }, "Light", Platform.Desktop, 700, 400);
        var cmds = c.GetVisualDescendants().OfType<NavBarCommand>().ToList();
        Assert.Equal(2, cmds.Count);
        Assert.Equal("Alpha", cmds[0].Label);
        var invoked = new List<object?>();
        c.Invoked += (_, e) => invoked.Add(e.Data);
        AppBarTests.Click(window, cmds[1].GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Button"));
        Assert.Equal(["Beta"], invoked);
        window.Close();
    }

    [AvaloniaFact]
    public void Vertical_Layout_Stacks()
    {
        var (window, c) = Make(3, 700, layout: Orientation.Vertical);
        Assert.Contains(":vertical", c.Classes);
        Assert.Equal(1, c.PageCount);
        var cmds = c.GetVisualDescendants().OfType<NavBarCommand>().ToList();
        Assert.Equal(cmds[0].Bounds.X, cmds[1].Bounds.X, 0.5);
        Assert.True(cmds[1].Bounds.Y > cmds[0].Bounds.Y);
        Assert.True(cmds[0].Bounds.Width > 300, "vertical commands stretch to the container width");
        window.Close();
    }

    [AvaloniaFact]
    public void NavBar_Defaults_To_Top_Custom_None()
    {
        var nav = new NavBar { Content = new NavBarContainer { Items = { new NavBarCommand { Label = "Home" } } } };
        var window = ThemeTestHelpers.Host(new Grid { Children = { nav } }, "Light", Platform.Desktop);
        Assert.Equal(AppBarPlacement.Top, nav.Placement);
        Assert.Equal(AppBarLayout.Custom, nav.Layout);
        Assert.Equal(AppBarClosedDisplayMode.None, nav.ClosedDisplayMode);
        Assert.False(nav.Bar.IsVisible);
        nav.Open();
        window.UpdateLayout();
        Assert.True(nav.Bar.IsVisible);
        Assert.Equal(0, nav.Bar.Bounds.Y, 0.5);
        Assert.True(nav.Bar.Bounds.Height >= 60);
        Assert.NotEmpty(nav.Bar.GetVisualDescendants().OfType<NavBarCommand>());
        window.Close();
    }
}
