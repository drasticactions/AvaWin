using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class SettingsFlyoutPage : SamplePage
{
    public SettingsFlyoutPage()
    {
        DataContext = new ChromeViewModel();
        InitializeComponent();
    }

    private void OnShowNarrow(object? sender, RoutedEventArgs e) => Narrow.Show();

    private void OnShowWide(object? sender, RoutedEventArgs e) => Wide.Show();

    private async void OnPush(object? sender, RoutedEventArgs e)
    {
        var body = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(20, 0),
            Spacing = 12,
            Children = { new BackButton(), new TextBlock { Text = "This BackButton pops the page", VerticalAlignment = VerticalAlignment.Center } },
        };
        await Nav.PushAsync(new ContentPage { Header = "Details", Content = body });
    }
}
