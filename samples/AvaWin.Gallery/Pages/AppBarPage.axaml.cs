using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class AppBarPage : SamplePage
{
    private readonly ChromeViewModel _vm = new();

    public AppBarPage()
    {
        DataContext = _vm;
        InitializeComponent();
        _vm.Status = "Select photos to enable Delete and Copy.";
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView lv)
        {
            _vm.SelectedCount = lv.Selection.Count;
            _vm.Status = lv.Selection.Count == 0 ? "Select photos to enable Delete and Copy." : $"{lv.Selection.Count} selected";
        }
    }

    private void OnCommand(object? sender, RoutedEventArgs e)
    {
        if (sender is AppBarCommand c)
        {
            _vm.Status = c.Type == AppBarCommandType.Toggle ? $"{c.Label}: {(c.IsSelected ? "on" : "off")}" : $"{c.Label} invoked";
        }
    }

    private void OnOpenBottom(object? sender, RoutedEventArgs e) => BottomBar.Open();

    private void OnOpenTop(object? sender, RoutedEventArgs e) => TopBar.Open();

    private void OnCloseTop(object? sender, RoutedEventArgs e) => TopBar.Close();
}
