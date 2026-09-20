using Avalonia.Interactivity;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class ContentDialogPage : SamplePage
{
    private readonly ChromeViewModel _vm = new();

    public ContentDialogPage()
    {
        DataContext = _vm;
        InitializeComponent();
        _vm.Status = "Each button opens a dialog.";
        foreach (var d in new[] { DeleteDialog, RenameDialog, InfoDialog })
        {
            d.BeforeShow += (_, _) => _vm.Status = $"{d.Title}: BeforeShow";
            d.AfterShow += (_, _) => _vm.Status = $"{d.Title}: AfterShow";
            d.AfterHide += (_, e) => _vm.Status = $"{d.Title}: AfterHide({e.Result})";
        }
    }

    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        Confirm.IsChecked = false;
        var result = await DeleteDialog.ShowAsync();
        _vm.Status = result == ContentDialogResult.Primary ? "Deleted (not really)." : $"Delete dismissed: {result}";
    }

    private void OnDeleteBeforeHide(object? sender, ContentDialogHideEventArgs e)
    {
        if (e.Result == ContentDialogResult.Primary && Confirm.IsChecked != true)
        {
            e.Cancel = true;
            _vm.Status = "BeforeHide canceled: check the box first.";
        }
    }

    private async void OnRename(object? sender, RoutedEventArgs e)
    {
        NewTitle.Text = _vm.Current.Title;
        var result = await RenameDialog.ShowAsync();
        _vm.Status = result == ContentDialogResult.Primary ? $"Renamed to “{NewTitle.Text}” (not really)." : $"Rename dismissed: {result}";
    }

    private async void OnInfo(object? sender, RoutedEventArgs e)
    {
        var result = await InfoDialog.ShowAsync();
        _vm.Status = $"Details closed: {result}";
    }
}
