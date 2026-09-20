using Avalonia.Interactivity;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class SemanticZoomPage : SamplePage
{
    private readonly PhotosViewModel _vm = new();

    public SemanticZoomPage()
    {
        DataContext = _vm;
        InitializeComponent();
        _vm.Status = "Zoomed in. Pinch, Ctrl+wheel or use the corner button.";
    }

    private void OnZoomChanged(object? sender, SemanticZoomChangedEventArgs e) => _vm.Status = e.IsZoomedOut ? "Zoomed out: pick a letter" : $"Zoomed in on {Zoom.LastPositionedItem}";

    private void OnToggle(object? sender, RoutedEventArgs e) => Zoom.Toggle();
}
