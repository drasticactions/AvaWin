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

public class HubTests
{
    private static (Window window, Hub hub) Make(int count = 5, double width = 700, bool cancelEntrance = false)
    {
        var hub = new Hub();
        for (var i = 0; i < count; i++)
        {
            hub.Items.Add(new HubSection { Header = "Section " + i, IsHeaderStatic = i == 1, Content = new Border { Width = 200, Height = 100 } });
        }

        if (cancelEntrance)
        {
            hub.ContentAnimating += (_, e) => e.Cancel = true;
        }

        var window = ThemeTestHelpers.Host(new Grid { Children = { hub } }, "Light", Platform.Desktop, width, 500);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, hub);
    }

    [AvaloniaFact]
    public void Sections_Lay_Out_Horizontally_And_Load_Completes()
    {
        var (window, hub) = Make();
        var sections = hub.Sections.ToList();
        Assert.Equal(5, sections.Count);
        Assert.True(sections[1].Bounds.X > sections[0].Bounds.X);
        Assert.Equal(sections[0].Bounds.Y, sections[1].Bounds.Y, 0.5);
        Assert.Equal(HubLoadingState.Complete, hub.LoadingState);
        Assert.DoesNotContain(":loading", hub.Classes);
        Assert.Equal(0, hub.IndexOfFirstVisible);
        Assert.True(hub.IndexOfLastVisible < 4, "not every section fits in 700px");
        Assert.All(sections, s => Assert.Equal(1, s.Opacity));
        window.Close();
    }

    [AvaloniaFact]
    public void Entrance_Is_Cancelable_And_State_Events_Fire()
    {
        var states = new List<HubLoadingState>();
        var hub = new Hub();
        hub.LoadingStateChanged += (_, _) => states.Add(hub.LoadingState);
        var animating = 0;
        hub.ContentAnimating += (_, e) => { animating++; e.Cancel = true; };
        hub.Items.Add(new HubSection { Header = "A", Content = new Border { Width = 100, Height = 50 } });
        var window = ThemeTestHelpers.Host(new Grid { Children = { hub } }, "Light", Platform.Desktop);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(1, animating);
        Assert.Equal(HubLoadingState.Complete, hub.LoadingState);
        Assert.Contains(HubLoadingState.Complete, states);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderInvoked_Only_For_Interactive_Headers()
    {
        var (window, hub) = Make();
        var invoked = new List<int>();
        hub.HeaderInvoked += (_, e) => invoked.Add(e.Index);
        var sections = hub.Sections.ToList();
        var button0 = sections[0].GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_HeaderButton");
        Assert.True(button0.IsVisible);
        AppBarTests.Click(window, button0);
        Assert.Equal([0], invoked);
        var button1 = sections[1].GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_HeaderButton");
        Assert.False(button1.IsVisible, "static header has no button");
        Assert.Contains(":static", sections[1].Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void SectionOnScreen_Scrolls()
    {
        var (window, hub) = Make();
        hub.SectionOnScreen = 3;
        window.UpdateLayout();
        Assert.True(hub.ScrollPosition > 0);
        Assert.Equal(3, hub.SectionOnScreen);
        Assert.Equal(3, hub.IndexOfFirstVisible);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Proximity_Snapping_Settles_Only_Near_A_Section()
    {
        var (window, hub) = Make();
        var points = hub.SnapPoints;
        Assert.Equal(5, points.Count);
        Assert.True(points[1] > points[0]);

        // Within 120px of a section start: eases onto it once scrolling settles.
        hub.ScrollPosition = points[2] + 40;
        await Task.Delay(300);
        Assert.Equal(points[2], hub.ScrollPosition, 0.5);

        // Far from any section start: left alone (proximity, not mandatory).
        var mid = (points[1] + points[2]) / 2;
        hub.ScrollPosition = mid;
        await Task.Delay(300);
        Assert.Equal(mid, hub.ScrollPosition, 0.5);

        hub.SnapProximity = 0;
        hub.ScrollPosition = points[2] + 40;
        await Task.Delay(300);
        Assert.Equal(points[2] + 40, hub.ScrollPosition, 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void ScrollPosition_Notifies_As_The_Viewport_Scrolls()
    {
        var (window, hub) = Make();
        var seen = new List<double>();
        hub.GetObservable(Hub.ScrollPositionProperty).Subscribe(new Avalonia.Reactive.AnonymousObserver<double>(seen.Add));
        hub.ScrollPosition = 150;
        window.UpdateLayout();
        Assert.Contains(150, seen);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Scrolling_To_The_End_Stays_There()
    {
        var (window, hub) = Make();
        // With every section start "in proximity", the end of the panorama must still be a resting position.
        hub.SnapProximity = 100000;
        hub.ScrollPosition = 100000;
        window.UpdateLayout();
        var end = hub.ScrollPosition;
        Assert.True(end > 0);
        await Task.Delay(300);
        Assert.Equal(end, hub.ScrollPosition, 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void Wheel_Scrolls_The_Panorama_Horizontally()
    {
        var (window, hub) = Make();
        var p = hub.TranslatePoint(new Point(100, 250), window)!.Value;
        // A vertical wheel notch is the only wheel a mouse has: down advances, up comes back.
        window.MouseWheel(p, new Vector(0, -1));
        window.UpdateLayout();
        Assert.True(hub.ScrollPosition > 0, $"position {hub.ScrollPosition}");
        var after = hub.ScrollPosition;
        window.MouseWheel(p, new Vector(0, -1));
        window.UpdateLayout();
        Assert.True(hub.ScrollPosition > after);
        window.MouseWheel(p, new Vector(0, 2));
        window.UpdateLayout();
        Assert.Equal(0, hub.ScrollPosition);
        window.Close();
    }

    [AvaloniaFact]
    public void Wheel_Over_A_Vertically_Scrollable_Section_Scrolls_The_Section()
    {
        var hub = new Hub();
        var list = new ScrollViewer { Width = 200, Height = 100, Content = new Border { Height = 400 } };
        hub.Items.Add(new HubSection { Header = "List", Content = list });
        for (var i = 0; i < 4; i++)
        {
            hub.Items.Add(new HubSection { Header = "S" + i, Content = new Border { Width = 200, Height = 100 } });
        }

        var window = ThemeTestHelpers.Host(new Grid { Children = { hub } }, "Light", Platform.Desktop, 700, 500);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var p = list.TranslatePoint(new Point(50, 50), window)!.Value;
        window.MouseWheel(p, new Vector(0, -1));
        window.UpdateLayout();
        Assert.True(list.Offset.Y > 0, "the section's own list scrolls");
        Assert.Equal(0, hub.ScrollPosition);
        // Wheel up at the top of the list has nothing to scroll there: the panorama takes it and comes back.
        list.Offset = new Vector(0, 0);
        hub.ScrollPosition = 150;
        window.UpdateLayout();
        window.MouseWheel(p, new Vector(0, 1));
        window.UpdateLayout();
        Assert.Equal(0, hub.ScrollPosition);
        window.Close();
    }

    [AvaloniaFact]
    public void Vertical_Orientation_Stacks_Sections()
    {
        var hub = new Hub { Orientation = Orientation.Vertical };
        for (var i = 0; i < 3; i++)
        {
            hub.Items.Add(new HubSection { Header = "S" + i, Content = new Border { Height = 60 } });
        }

        var window = ThemeTestHelpers.Host(new Grid { Children = { hub } }, "Light", Platform.Desktop);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var sections = hub.Sections.ToList();
        Assert.Contains(":vertical", hub.Classes);
        Assert.Contains(":vertical", sections[0].Classes);
        Assert.True(sections[1].Bounds.Y > sections[0].Bounds.Y);
        Assert.Equal(sections[0].Bounds.X, sections[1].Bounds.X, 0.5);
        window.Close();
    }
}
