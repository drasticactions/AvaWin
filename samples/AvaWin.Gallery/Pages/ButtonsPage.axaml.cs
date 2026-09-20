using System;
using Avalonia.Interactivity;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class ButtonsPage : SamplePage
{
    private readonly FormsViewModel _vm = new();

    public ButtonsPage()
    {
        DataContext = _vm;
        InitializeComponent();
    }

    private void OnMore(object? sender, RoutedEventArgs e) => _vm.Copies = Math.Min(99, _vm.Copies + 1);

    private void OnFewer(object? sender, RoutedEventArgs e) => _vm.Copies = Math.Max(1, _vm.Copies - 1);
}
