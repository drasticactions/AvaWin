using System.Collections.Generic;
using System.Collections.ObjectModel;
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

public class ListViewTests
{
    private static (Window window, ListView list) Make(int count = 100, ListSelectionMode mode = ListSelectionMode.Multi, TapBehavior tap = TapBehavior.InvokeOnly, double height = 400, System.Collections.IEnumerable? source = null)
    {
        var list = new ListView
        {
            ItemsSource = source ?? Enumerable.Range(0, count).Select(i => "Item " + i).ToList(),
            SelectionMode = mode,
            TapBehavior = tap,
            Height = height,
            Width = 300,
        };
        list.ContentAnimating += (_, e) => e.Cancel = true;
        var window = ThemeTestHelpers.Host(new Grid { Children = { list } }, "Light", Platform.Desktop, 400, 500);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, list);
    }

    private static ListViewItem Item(ListView list, int index)
    {
        list.EnsureVisible(index);
        list.UpdateLayout();
        return (ListViewItem)list.ContainerFromIndex(index)!;
    }

    private static void Click(Window window, Control c, KeyModifiers mods = KeyModifiers.None, MouseButton button = MouseButton.Left)
    {
        var p = c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), window)!.Value;
        var raw = (mods.HasFlag(KeyModifiers.Control) ? RawInputModifiers.Control : RawInputModifiers.None)
                  | (mods.HasFlag(KeyModifiers.Shift) ? RawInputModifiers.Shift : RawInputModifiers.None);
        window.MouseDown(p, button, raw);
        window.MouseUp(p, button, raw);
    }

    [AvaloniaFact]
    public void Virtualizes_Large_Sources()
    {
        var (window, list) = Make(10_000);
        var panel = list.GetVisualDescendants().OfType<ListViewPanel>().Single();
        Assert.True(panel.RealizedCount > 0);
        Assert.True(panel.RealizedCount < 200, $"realized {panel.RealizedCount}");
        Assert.Equal(ListViewLoadingState.Complete, list.LoadingState);
        list.EnsureVisible(9_000);
        window.UpdateLayout();
        Assert.NotNull(list.ContainerFromIndex(9_000));
        Assert.True(panel.RealizedCount < 200);
        Assert.True(list.IndexOfFirstVisible >= 8_900);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(TapBehavior.DirectSelect, ListSelectionMode.Multi)]
    [InlineData(TapBehavior.DirectSelect, ListSelectionMode.Single)]
    [InlineData(TapBehavior.DirectSelect, ListSelectionMode.None)]
    [InlineData(TapBehavior.ToggleSelect, ListSelectionMode.Multi)]
    [InlineData(TapBehavior.ToggleSelect, ListSelectionMode.Single)]
    [InlineData(TapBehavior.ToggleSelect, ListSelectionMode.None)]
    [InlineData(TapBehavior.InvokeOnly, ListSelectionMode.Multi)]
    [InlineData(TapBehavior.InvokeOnly, ListSelectionMode.Single)]
    [InlineData(TapBehavior.InvokeOnly, ListSelectionMode.None)]
    [InlineData(TapBehavior.None, ListSelectionMode.Multi)]
    [InlineData(TapBehavior.None, ListSelectionMode.Single)]
    [InlineData(TapBehavior.None, ListSelectionMode.None)]
    public void Tap_Behaviour_Matrix(TapBehavior tap, ListSelectionMode mode)
    {
        var (window, list) = Make(100, mode, tap);
        var invoked = new List<int>();
        list.ItemInvoked += (_, e) => invoked.Add(e.Index);
        Click(window, Item(list, 2));
        Click(window, Item(list, 4));
        var selected = list.Selection.SelectedIndexes.ToList();
        switch (tap)
        {
            case TapBehavior.None:
                Assert.Empty(invoked);
                Assert.Empty(selected);
                break;
            case TapBehavior.InvokeOnly:
                Assert.Equal([2, 4], invoked);
                Assert.Empty(selected);
                break;
            case TapBehavior.DirectSelect:
                Assert.Equal([2, 4], invoked);
                Assert.Equal(mode == ListSelectionMode.None ? new List<int>() : [4], selected);
                break;
            case TapBehavior.ToggleSelect:
                Assert.Equal([2, 4], invoked);
                Assert.Equal(mode switch { ListSelectionMode.None => new List<int>(), ListSelectionMode.Single => [4], _ => [2, 4] }, selected);
                if (mode == ListSelectionMode.Multi)
                {
                    Click(window, Item(list, 2));
                    Assert.Equal([4], list.Selection.SelectedIndexes.ToList());
                }

                break;
        }

        window.Close();
    }

    [AvaloniaFact]
    public void DirectSelect_Modifiers_And_Right_Click()
    {
        var (window, list) = Make(100, ListSelectionMode.Multi, TapBehavior.DirectSelect);
        Click(window, Item(list, 1));
        Click(window, Item(list, 3), KeyModifiers.Control);
        Assert.Equal([1, 3], list.Selection.SelectedIndexes.OrderBy(i => i).ToList());
        Click(window, Item(list, 6), KeyModifiers.Shift);
        Assert.Equal([3, 4, 5, 6], list.Selection.SelectedIndexes.OrderBy(i => i).ToList());
        Click(window, Item(list, 0), button: MouseButton.Right);
        Assert.Contains(0, list.Selection.SelectedIndexes);
        Assert.Contains(":selected", Item(list, 0).Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void SelectionChanging_Can_Cancel()
    {
        var (window, list) = Make(20, ListSelectionMode.Multi, TapBehavior.ToggleSelect);
        var seen = new List<(int old, int @new)>();
        list.SelectionChanging += (_, e) =>
        {
            seen.Add((e.OldSelection.Count, e.NewSelection.Count));
            e.Cancel = e.NewSelection.Contains(5);
        };
        Click(window, Item(list, 2));
        Click(window, Item(list, 5));
        Assert.Equal([2], list.Selection.SelectedIndexes.ToList());
        Assert.Equal(2, seen.Count);
        window.Close();
    }

    [AvaloniaFact]
    public void Keyboard_Navigation_Space_Enter_CtrlA()
    {
        var (window, list) = Make(100, ListSelectionMode.Multi, TapBehavior.DirectSelect);
        var invoked = new List<int>();
        list.ItemInvoked += (_, e) => invoked.Add(e.Index);
        var navigations = new List<(int, int)>();
        list.KeyboardNavigating += (_, e) => navigations.Add((e.OldFocus, e.NewFocus));
        Item(list, 0).Focus();
        Assert.Equal(0, list.CurrentIndex);
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.Equal(1, list.CurrentIndex);
        Assert.Equal([1], list.Selection.SelectedIndexes.ToList());
        Assert.Equal([(0, 1)], navigations);
        window.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None);
        Assert.Equal(99, list.CurrentIndex);
        Assert.True(list.IndexOfLastVisible >= 99);
        window.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.None);
        Assert.Equal(0, list.CurrentIndex);
        window.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.None);
        Assert.True(list.CurrentIndex > 1);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal([list.CurrentIndex], invoked);
        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);
        Assert.Equal(100, list.Selection.Count);
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Equal(99, list.Selection.Count);
        window.Close();
    }

    [AvaloniaFact]
    public void ToggleSelect_Keyboard_Does_Not_Select_On_Move()
    {
        var (window, list) = Make(20, ListSelectionMode.Multi, TapBehavior.ToggleSelect);
        Item(list, 0).Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.Equal(1, list.CurrentIndex);
        Assert.Empty(list.Selection.SelectedIndexes);
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Equal([1], list.Selection.SelectedIndexes.ToList());
        window.Close();
    }

    [AvaloniaFact]
    public void Groups_Render_Headers_Before_Each_Group()
    {
        var items = Enumerable.Range(0, 30).Select(i => "Group" + (i / 10) + "-" + i).ToList();
        var groups = new[] { "Group0", "Group1", "Group2" };
        var list = new ListView
        {
            ItemsSource = items,
            GroupKeySelector = o => o!.ToString()!.Split('-')[0],
            GroupsSource = groups.Select(g => new { Key = g, Title = g.ToUpperInvariant() }).ToList(),
            GroupsKeySelector = g => ((dynamic)g!).Key,
            Height = 400,
            Width = 300,
        };
        list.ContentAnimating += (_, e) => e.Cancel = true;
        var window = ThemeTestHelpers.Host(new Grid { Children = { list } }, "Light", Platform.Desktop, 400, 500);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(3, list.Groups!.Count);
        Assert.Equal(10, list.Groups[1].Start);
        Assert.Contains(":grouped", list.Classes);
        var panel = list.GetVisualDescendants().OfType<ListViewPanel>().Single();
        Assert.True(panel.RealizedHeaders.ContainsKey(0));
        var header0 = panel.RealizedHeaders[0];
        var first = list.ContainerFromIndex(0)!;
        Assert.Equal(0, header0.Bounds.Y, 0.5);
        Assert.True(first.Bounds.Y >= header0.Bounds.Height - 0.5);
        Assert.Equal("GROUP0", ((dynamic)((ListViewGroupHeader)header0).Content!).Title);

        var invoked = new List<int>();
        list.GroupHeaderInvoked += (_, e) => invoked.Add(e.GroupIndex);
        Click(window, header0);
        Assert.Equal([0], invoked);

        list.EnsureVisible(new ListViewEntity(ListViewEntityType.GroupHeader, 2));
        window.UpdateLayout();
        Assert.True(panel.RealizedHeaders.ContainsKey(2));
        var header2 = panel.RealizedHeaders[2];
        var item20 = list.ContainerFromIndex(20)!;
        Assert.True(item20.Bounds.Y > header2.Bounds.Y);
        Assert.True(header2.Bounds.Y - list.ContainerFromIndex(19)!.Bounds.Bottom >= 70 - 1, "70px group leader gap");
        window.Close();
    }

    [AvaloniaFact]
    public void Filled_Style_Sets_Attached_Selection_Style()
    {
        var (window, list) = Make(5, ListSelectionMode.Multi, TapBehavior.ToggleSelect);
        Assert.Equal(SelectionStyle.Bordered, ListStyle.GetSelectionStyle(Item(list, 0)));
        list.SelectionStyle = ListViewSelectionStyle.Filled;
        window.UpdateLayout();
        Assert.Equal(SelectionStyle.Filled, ListStyle.GetSelectionStyle(Item(list, 0)));
        Assert.Contains(":filled", list.Classes);
        window.Close();
    }

    private sealed class IncrementalSource : ObservableCollection<string>, ISupportIncrementalLoading
    {
        public int Calls;

        public bool HasMoreItems => Count < 200;

        public Task<int> LoadMoreItemsAsync(int count)
        {
            Calls++;
            var start = Count;
            for (var i = 0; i < count && Count < 200; i++)
            {
                Add("Item " + (start + i));
            }

            return Task.FromResult(Count - start);
        }
    }

    [AvaloniaFact]
    public void Incremental_Source_Loads_When_Near_The_End()
    {
        var source = new IncrementalSource();
        for (var i = 0; i < 20; i++)
        {
            source.Add("Item " + i);
        }

        // viewport 400 / cell 50 = 8 items per page; threshold 2 pages → fewer than 16 items beyond the viewport triggers a load
        var (window, list) = Make(source: source);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(source.Calls >= 1, "first page requested on load");
        var after = source.Count;
        Assert.True(after > 20);
        list.EnsureVisible(after - 1);
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(source.Count > after);
        window.Close();
    }

    [AvaloniaFact]
    public void Horizontal_List_And_ScrollPosition()
    {
        var list = new ListView { ItemsSource = Enumerable.Range(0, 50).Select(i => "I" + i).ToList(), Layout = new ListLayout { Orientation = Orientation.Horizontal }, Width = 300, Height = 100 };
        list.ContentAnimating += (_, e) => e.Cancel = true;
        var window = ThemeTestHelpers.Host(new Grid { Children = { list } }, "Light", Platform.Desktop, 400, 300);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Contains(":horizontal", list.Classes);
        var a = list.ContainerFromIndex(0)!;
        var b = list.ContainerFromIndex(1)!;
        Assert.True(b.Bounds.X > a.Bounds.X);
        Assert.Equal(a.Bounds.Y, b.Bounds.Y, 0.5);
        list.ScrollPosition = 500;
        window.UpdateLayout();
        Assert.Equal(500, list.ScrollPosition, 0.5);
        Assert.True(list.IndexOfFirstVisible > 0);
        window.Close();
    }

    [AvaloniaFact]
    public void Items_Change_Keeps_Working()
    {
        var source = new ObservableCollection<string>(Enumerable.Range(0, 10).Select(i => "Item " + i));
        var (window, list) = Make(source: source);
        source.RemoveAt(0);
        source.Add("New");
        window.UpdateLayout();
        Assert.Equal(10, list.ItemCount);
        Assert.Equal("Item 1", ((ListViewItem)list.ContainerFromIndex(0)!).Content);
        window.Close();
    }
}
