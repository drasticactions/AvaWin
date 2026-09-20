using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class NavBarPage : SamplePage
{
    private static readonly AppBarIcon[] Icons = [AppBarIcon.Home, AppBarIcon.Camera, AppBarIcon.Mail, AppBarIcon.Calendar, AppBarIcon.Contact, AppBarIcon.Map, AppBarIcon.Video, AppBarIcon.Audio, AppBarIcon.Pictures, AppBarIcon.Settings];

    private readonly ChromeViewModel _vm = new();

    public NavBarPage()
    {
        DataContext = _vm;
        InitializeComponent();
        _vm.Status = "Pick a destination in the bar above.";
        for (var i = 0; i < 20; i++)
        {
            Paged.Items.Add(new NavBarCommand { Label = "Command " + (i + 1), Icon = Icons[i % Icons.Length], SplitButton = i % 4 == 3 });
        }

        for (var i = 0; i < 12; i++)
        {
            OverlayNav.Items.Add(new NavBarCommand { Label = "Page " + (i + 1), Icon = Icons[i % Icons.Length] });
        }
    }

    private void OnInvoked(object? sender, NavBarInvokedEventArgs e)
    {
        PageTitle.Text = e.NavBarCommand.Label;
        _vm.Current = SampleData.Photos[e.Index % SampleData.Photos.Count];
        _vm.Status = $"Invoked index {e.Index}, location “{e.NavBarCommand.Location}”";
    }

    private void OnSplitInvoked(object? sender, NavBarInvokedEventArgs e) => SplitStatus.Text = e.Data is Photo p ? $"Data item invoked: {p.Title}" : $"Invoked index {e.Index}: {e.NavBarCommand.Label}";

    private void OnSplitToggle(object? sender, NavBarSplitToggleEventArgs e) => SplitStatus.Text = $"SplitToggle index {e.Index}: opened={e.Opened}";

    private void OnOverlayInvoked(object? sender, NavBarInvokedEventArgs e) => Overlay.Close();

    private void OnOpenOverlay(object? sender, RoutedEventArgs e) => Overlay.Open();
}
