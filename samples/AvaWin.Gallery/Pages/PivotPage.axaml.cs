using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class PivotPage : SamplePage
{
    private readonly MusicViewModel _vm = new();

    public PivotPage()
    {
        DataContext = _vm;
        InitializeComponent();
        _vm.Status = "Swipe the content or tap a header. PivotSelectionChanged: 0 (albums)";
    }

    private void OnSelectionChanged(object? sender, PivotSelectionChangedEventArgs e) => _vm.Status = $"PivotSelectionChanged: {e.Index} ({e.Item?.Header})";

    private void OnSelect(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            _vm.SelectedIndex = int.Parse(tag, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
