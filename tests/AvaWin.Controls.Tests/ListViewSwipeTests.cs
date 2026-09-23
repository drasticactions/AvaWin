using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Xunit;

namespace AvaWin.Controls.Tests;

public class ListViewSwipeTests
{
    private static (Window window, ListView list, ObservableCollection<string> source) Make(bool grouped = false, bool reorderable = true, SwipeBehavior swipe = SwipeBehavior.Select)
    {
        var source = new ObservableCollection<string>(Enumerable.Range(0, 12).Select(i => (i < 6 ? "A" : "B") + i));
        var list = new ListView
        {
            ItemsSource = source,
            SwipeBehavior = swipe,
            ItemsReorderable = reorderable,
            Layout = new CellSpanningLayout { ItemInfo = _ => new GridItemInfo(100, 100), GroupInfo = _ => new GroupInfo(true, 100, 100) },
            Width = 700,
            Height = 320,
        };
        if (grouped)
        {
            list.GroupKeySelector = o => ((string)o!)[..1];
        }

        list.ContentAnimating += (_, e) => e.Cancel = true;
        var window = ThemeTestHelpers.Host(new Grid { Children = { list } }, "Dark", Platform.Desktop, 800, 400);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, list, source);
    }

    private static Point Centre(Window window, ListView list, int index)
    {
        var item = list.ContainerFromIndex(index)!;
        return item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
    }

    private static double TranslateY(Control item) =>
        item.RenderTransform is TransformOperations t ? t.Value.M32 : 0;

    [AvaloniaFact]
    public void Touch_Tap_Invokes()
    {
        var (window, list, _) = Make();
        var invoked = -1;
        list.ItemInvoked += (_, e) => invoked = e.Index;
        var p = Centre(window, list, 2);
        var touch = new TouchInput(window);
        touch.Down(p);
        touch.Up(p);
        Assert.Equal(2, invoked);
        Assert.Empty(list.Selection.SelectedIndexes);
        window.Close();
    }

    [AvaloniaFact]
    public void Release_Short_Of_Select_Does_Nothing()
    {
        var (window, list, _) = Make();
        var invoked = -1;
        list.ItemInvoked += (_, e) => invoked = e.Index;
        var p = Centre(window, list, 1);
        var touch = new TouchInput(window);
        touch.Drag(p, new Vector(0, 30));
        Assert.Equal(ListViewSwipeStage.None, list.SwipeStage);
        Assert.Equal(30, TranslateY(list.ContainerFromIndex(1)!), 0.5);
        touch.Up(p + new Vector(0, 30));
        Assert.Empty(list.Selection.SelectedIndexes);
        Assert.Equal(-1, invoked);
        var item = list.ContainerFromIndex(1)!;
        Assert.NotNull(item.Transitions);
        Assert.True(item.RenderTransform is TransformOperations { IsIdentity: true });
        window.Close();
    }

    [AvaloniaFact]
    public void Past_Select_A_Release_Toggles_The_Selection()
    {
        var (window, list, _) = Make();
        var p = Centre(window, list, 3);
        var touch = new TouchInput(window);
        touch.Drag(p, new Vector(0, 60));
        Assert.Equal(ListViewSwipeStage.Select, list.SwipeStage);
        Assert.Contains(":swipeselect", list.ContainerFromIndex(3)!.Classes);
        touch.Up(p + new Vector(0, 60));
        Assert.Equal([3], list.Selection.SelectedIndexes);
        Assert.DoesNotContain(":swipeselect", list.ContainerFromIndex(3)!.Classes);

        touch.Drag(p, new Vector(0, -60));
        touch.Up(p + new Vector(0, -60));
        Assert.Empty(list.Selection.SelectedIndexes);
        window.Close();
    }

    [AvaloniaFact]
    public void Every_Stage_Is_Reversible()
    {
        var (window, list, source) = Make();
        var starts = 0;
        var leaves = 0;
        var ends = 0;
        list.ItemDragStart += (_, _) => starts++;
        list.ItemDragLeave += (_, _) => leaves++;
        list.ItemDragEnd += (_, _) => ends++;
        var p = Centre(window, list, 2);
        var touch = new TouchInput(window);
        touch.Drag(p, new Vector(0, 130));
        Assert.Equal(ListViewSwipeStage.Drag, list.SwipeStage);
        Assert.Contains(":dragsource", list.ContainerFromIndex(2)!.Classes);
        Assert.Equal(1, starts);

        touch.MoveBy(p + new Vector(0, 130), new Vector(0, -70));
        Assert.Equal(ListViewSwipeStage.Select, list.SwipeStage);
        Assert.DoesNotContain(":dragsource", list.ContainerFromIndex(2)!.Classes);
        Assert.Equal(1, leaves);

        touch.MoveBy(p + new Vector(0, 60), new Vector(0, -40));
        Assert.Equal(ListViewSwipeStage.None, list.SwipeStage);
        touch.Up(p + new Vector(0, 20));
        Assert.Empty(list.Selection.SelectedIndexes);
        Assert.Equal(Enumerable.Range(0, 12).Select(i => (i < 6 ? "A" : "B") + i), source);
        Assert.Equal(1, ends);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Pan_Along_The_Scroll_Axis_Is_Not_A_Swipe()
    {
        var (window, list, _) = Make();
        var p = Centre(window, list, 4);
        var touch = new TouchInput(window);
        touch.Drag(p, new Vector(-80, 20));
        Assert.Equal(ListViewSwipeStage.None, list.SwipeStage);
        Assert.Equal(0, TranslateY(list.ContainerFromIndex(4)!));
        Assert.NotSame(list, touch.Pointer.Captured);
        touch.Up(p + new Vector(-80, 20));
        Assert.Empty(list.Selection.SelectedIndexes);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Swipe_Claims_The_Contact_From_The_Scroller()
    {
        var (window, list, _) = Make();
        var scroller = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(list).OfType<ScrollViewer>().First();
        var p = Centre(window, list, 4);
        var touch = new TouchInput(window);
        touch.Drag(p, new Vector(0, 20), steps: 2);
        Assert.Same(list, touch.Pointer.Captured);
        touch.MoveBy(p + new Vector(0, 20), new Vector(-60, 40));
        Assert.Equal(0, scroller.Offset.X);
        Assert.Equal(ListViewSwipeStage.Select, list.SwipeStage);
        touch.Up(p + new Vector(-60, 60));
        window.Close();
    }

    [AvaloniaFact]
    public void Capture_Loss_Cancels_The_Swipe()
    {
        var (window, list, _) = Make();
        var p = Centre(window, list, 2);
        var touch = new TouchInput(window);
        touch.Drag(p, new Vector(0, 130));
        Assert.Equal(ListViewSwipeStage.Drag, list.SwipeStage);
        touch.Pointer.Capture(null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ListViewSwipeStage.None, list.SwipeStage);
        Assert.DoesNotContain(":dragsource", list.ContainerFromIndex(2)!.Classes);
        Assert.Empty(list.Selection.SelectedIndexes);
        window.Close();
    }

    [AvaloniaFact]
    public void The_Mouse_Does_Not_Swipe()
    {
        var (window, list, _) = Make(reorderable: false);
        var p = Centre(window, list, 2);
        Avalonia.Headless.HeadlessWindowExtensions.MouseDown(window, p, Avalonia.Input.MouseButton.Left);
        Avalonia.Headless.HeadlessWindowExtensions.MouseMove(window, p + new Vector(0, 60), Avalonia.Input.RawInputModifiers.LeftMouseButton);
        Assert.Equal(ListViewSwipeStage.None, list.SwipeStage);
        Avalonia.Headless.HeadlessWindowExtensions.MouseUp(window, p + new Vector(0, 60), Avalonia.Input.MouseButton.Left);
        Assert.Empty(list.Selection.SelectedIndexes);
        window.Close();
    }

    [AvaloniaFact]
    public void SwipeBehavior_None_Leaves_Touch_Alone()
    {
        var (window, list, _) = Make(swipe: SwipeBehavior.None);
        var p = Centre(window, list, 2);
        var touch = new TouchInput(window);
        touch.Drag(p, new Vector(0, 60));
        Assert.Equal(ListViewSwipeStage.None, list.SwipeStage);
        touch.Up(p + new Vector(0, 60));
        Assert.Empty(list.Selection.SelectedIndexes);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Drop_Reorders_Within_The_Group_In_A_Grouped_CellSpanningLayout()
    {
        var (window, list, source) = Make(grouped: true);
        list.Selection.Select(5);
        var between = new List<int>();
        var dropped = new List<int>();
        list.ItemDragBetween += (_, e) => between.Add(e.InsertIndex);
        list.ItemDragDrop += (_, e) => dropped.Add(e.InsertIndex);

        var from = Centre(window, list, 0);
        var target = list.ContainerFromIndex(4)!;
        var over = target.TranslatePoint(new Point(target.Bounds.Width - 10, target.Bounds.Height / 2), window)!.Value;
        var touch = new TouchInput(window);
        touch.Drag(from, new Vector(0, 130));
        Assert.Equal(ListViewSwipeStage.Drag, list.SwipeStage);
        touch.MoveBy(from + new Vector(0, 130), over - (from + new Vector(0, 130)));
        Assert.Equal(5, list.DropIndex);
        touch.Up(over);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([5], dropped);
        Assert.NotEmpty(between);
        Assert.Equal(["A1", "A2", "A3", "A4", "A0", "A5", "B6", "B7", "B8", "B9", "B10", "B11"], source);
        Assert.Equal([5], list.Selection.SelectedIndexes);
        Assert.Equal(-1, list.DropIndex);

        var into = Centre(window, list, 9);
        from = Centre(window, list, 1);
        touch.Drag(from, new Vector(0, 130));
        touch.MoveBy(from + new Vector(0, 130), into - (from + new Vector(0, 130)));
        Assert.InRange(list.DropIndex, 0, 6);
        touch.Up(into);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["A", "A", "A", "A", "A", "A"], source.Take(6).Select(s => s[..1]));
        window.Close();
    }

    [AvaloniaFact]
    public void Without_Reorder_The_Swipe_Stops_At_Select()
    {
        var (window, list, source) = Make(reorderable: false);
        var p = Centre(window, list, 2);
        var touch = new TouchInput(window);
        touch.Drag(p, new Vector(0, 200));
        Assert.Equal(ListViewSwipeStage.Select, list.SwipeStage);
        var y = TranslateY(list.ContainerFromIndex(2)!);
        Assert.Equal(40 + 70 * 0.35, y, 0.5);
        touch.Up(p + new Vector(0, 200));
        Assert.Equal([2], list.Selection.SelectedIndexes);
        Assert.Equal("A2", source[2]);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Touch_That_Travels_Is_Not_A_Tap()
    {
        var (window, list, _) = Make(swipe: SwipeBehavior.None);
        var invoked = -1;
        list.ItemInvoked += (_, e) => invoked = e.Index;
        var p = Centre(window, list, 1);
        var touch = new TouchInput(window);
        touch.Drag(p, new Vector(0, 60));
        touch.Up(p + new Vector(0, 60));
        Assert.Equal(-1, invoked);

        touch.Drag(p, new Vector(2, 1), steps: 1);
        touch.Up(p + new Vector(2, 1));
        Assert.Equal(1, invoked);
        window.Close();
    }
}
