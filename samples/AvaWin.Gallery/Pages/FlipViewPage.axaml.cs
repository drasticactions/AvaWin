using Avalonia.Interactivity;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class FlipViewPage : SamplePage
{
    private readonly PhotosViewModel _vm = new();

    public FlipViewPage()
    {
        DataContext = _vm;
        InitializeComponent();
        _vm.Status = "Swipe, use the edge arrows, or press Left and Right.";
    }

    private void OnPageSelected(object? sender, RoutedEventArgs e) => _vm.Status = $"PageSelected: {_vm.Current.Title}";

    private async void OnPrevious(object? sender, RoutedEventArgs e) => await Viewer.PreviousAsync();

    private async void OnNext(object? sender, RoutedEventArgs e) => await Viewer.NextAsync();

    private void OnFirst(object? sender, RoutedEventArgs e) => Viewer.CurrentPage = 0;

    private void OnMiddle(object? sender, RoutedEventArgs e) => Viewer.CurrentPage = Viewer.Count / 2;

    private void OnLast(object? sender, RoutedEventArgs e) => Viewer.CurrentPage = Viewer.Count - 1;
}
