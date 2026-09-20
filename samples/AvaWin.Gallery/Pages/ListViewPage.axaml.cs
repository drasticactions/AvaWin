using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Animations;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class ListViewPage : SamplePage
{
    private static readonly GridItemInfo[] MosaicSizes =
    [
        new(200, 200), new(100, 100), new(100, 100), new(200, 100), new(100, 100), new(100, 300),
    ];

    private readonly ListViewViewModel _vm = new();

    public ListViewPage()
    {
        DataContext = _vm;
        InitializeComponent();
        Mosaic.Layout = new CellSpanningLayout { ItemInfo = i => MosaicSizes[i % MosaicSizes.Length], GroupInfo = _ => new GroupInfo(true, 100, 100) };
        Reorder.ItemDragBetween += (_, e) => _vm.DragStatus = $"ItemDragBetween: insert at {e.InsertIndex}";
        Reorder.ItemDragDrop += (_, e) => _vm.DragStatus = $"ItemDragDrop at {e.InsertIndex}: {string.Join(", ", e.Items)}";
    }

    private void OnInvoked(object? sender, ListViewItemInvokedEventArgs e) => _vm.Status = $"ItemInvoked {e.Index}: {e.Item}";

    private void OnGroupInvoked(object? sender, ListViewGroupHeaderInvokedEventArgs e) => _vm.Status = $"GroupHeaderInvoked {e.GroupIndex}: group “{e.Group.Header}”";

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => _vm.Status = $"{People.Selection.Count} selected: {string.Join(", ", People.Selection.SelectedItems.Take(4))}{(People.Selection.Count > 4 ? "..." : string.Empty)}";

    /// <summary>Pre-select a couple of rows so the chrome is visible before any interaction.</summary>
    private void OnSelectSome(object? sender, RoutedEventArgs e)
    {
        if (sender is ListView lv && lv.Selection.Count == 0)
        {
            lv.Selection.Select(1);
            lv.Selection.Select(3);
        }
    }

    private async void OnInsert(object? sender, RoutedEventArgs e)
    {
        _vm.Editable.Insert(Math.Min(1, _vm.Editable.Count), _vm.NextContact());
        await System.Threading.Tasks.Task.Yield();
        Editable.UpdateLayout();
        if (Editable.ContainerFromIndex(1) is { } c)
        {
            await WinAnimations.CreateAddToListAnimation(c, Editable.RealizedItems.Where(i => !ReferenceEquals(i, c))).ExecuteAsync();
        }
    }

    private async void OnRemove(object? sender, RoutedEventArgs e)
    {
        if (_vm.Editable.Count == 0 || Editable.ContainerFromIndex(0) is not { } c)
        {
            return;
        }

        var anim = WinAnimations.CreateDeleteFromListAnimation(c, Editable.RealizedItems.Where(i => !ReferenceEquals(i, c)));
        await WinAnimations.FadeOut(c);
        _vm.Editable.RemoveAt(0);
        await anim.ExecuteAsync();
    }
}
