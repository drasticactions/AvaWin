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

public class ListViewDragTests
{
    private static (Window window, ListView list, ObservableCollection<string> source) Make()
    {
        var source = new ObservableCollection<string>(Enumerable.Range(0, 10).Select(i => "Item " + i));
        var list = new ListView { ItemsSource = source, ItemsDraggable = true, ItemsReorderable = true, Width = 300, Height = 400 };
        list.ContentAnimating += (_, e) => e.Cancel = true;
        var window = ThemeTestHelpers.Host(new Grid { Children = { list } }, "Light", Platform.Desktop, 400, 500);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, list, source);
    }

    [AvaloniaFact]
    public void Pointer_Drag_Reorders_End_To_End()
    {
        // Headless uses Avalonia's in-process drag source: pointer moves become DragOver, release becomes Drop.
        var (window, list, source) = Make();
        var started = new List<int>();
        var ended = 0;
        list.ItemDragStart += (_, e) => started.AddRange(e.Indexes);
        list.ItemDragEnd += (_, _) => ended++;
        var item = list.ContainerFromIndex(2)!;
        var p = item.TranslatePoint(new Point(20, 10), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        window.MouseMove(p + new Vector(0, 3), RawInputModifiers.LeftMouseButton);
        Assert.Empty(started);
        window.MouseMove(p + new Vector(0, 30), RawInputModifiers.LeftMouseButton);
        Assert.Equal([2], started);
        Assert.Contains(":dragsource", item.Classes);
        var target = list.ContainerFromIndex(6)!;
        var drop = target.TranslatePoint(new Point(20, target.Bounds.Height - 5), window)!.Value;
        window.MouseMove(drop, RawInputModifiers.LeftMouseButton);
        Assert.Equal(7, list.DropIndex);
        window.MouseUp(drop, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, ended);
        Assert.Equal(["Item 0", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6", "Item 2", "Item 7", "Item 8", "Item 9"], source);
        Assert.DoesNotContain(":dragsource", item.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void DragOver_And_Drop_Reorder_Items()
    {
        var (window, list, source) = Make();
        var between = new List<int>();
        var dropped = new List<int>();
        list.ItemDragBetween += (_, e) => between.Add(e.InsertIndex);
        list.ItemDragDrop += (_, e) => dropped.Add(e.InsertIndex);
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(ListView.DragFormat, new ListViewDragData(list, [1], ["Item 1"])));
        var target = list.ContainerFromIndex(5)!;
        var pTop = target.TranslatePoint(new Point(20, 5), window)!.Value;
        var pBottom = target.TranslatePoint(new Point(20, target.Bounds.Height - 5), window)!.Value;
        window.DragDrop(pTop, RawDragEventType.DragEnter, data, DragDropEffects.Move);
        window.DragDrop(pTop, RawDragEventType.DragOver, data, DragDropEffects.Move);
        Assert.Equal([5], between);
        Assert.Equal(5, list.DropIndex);
        window.DragDrop(pBottom, RawDragEventType.DragOver, data, DragDropEffects.Move);
        Assert.Equal([5, 6], between);
        window.DragDrop(pBottom, RawDragEventType.Drop, data, DragDropEffects.Move);
        Assert.Equal([6], dropped);
        Assert.Equal(-1, list.DropIndex);
        Assert.Equal(["Item 0", "Item 2", "Item 3", "Item 4", "Item 5", "Item 1", "Item 6", "Item 7", "Item 8", "Item 9"], source);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal([5], list.Selection.SelectedIndexes.ToList());
        window.Close();
    }

    [AvaloniaFact]
    public void Drop_Can_Be_Cancelled_And_External_Data_Does_Not_Reorder()
    {
        var (window, list, source) = Make();
        list.ItemDragDrop += (_, e) => e.Cancel = true;
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(ListView.DragFormat, new ListViewDragData(list, [0], ["Item 0"])));
        var target = list.ContainerFromIndex(3)!;
        var p = target.TranslatePoint(new Point(20, target.Bounds.Height - 5), window)!.Value;
        window.DragDrop(p, RawDragEventType.DragOver, data, DragDropEffects.Move);
        window.DragDrop(p, RawDragEventType.Drop, data, DragDropEffects.Move);
        Assert.Equal("Item 0", source[0]);

        var leaves = 0;
        list.ItemDragLeave += (_, _) => leaves++;
        var text = new DataTransfer();
        text.Add(DataTransferItem.CreateText("external"));
        window.DragDrop(p, RawDragEventType.DragOver, text, DragDropEffects.Copy);
        window.DragDrop(p, RawDragEventType.DragLeave, text, DragDropEffects.Copy);
        Assert.Equal(1, leaves);
        Assert.Equal("Item 0", source[0]);
        window.Close();
    }
}
