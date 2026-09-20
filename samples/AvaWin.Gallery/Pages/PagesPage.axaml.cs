using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class PagesPage : SamplePage
{
    private readonly MusicViewModel _vm = new();

    public PagesPage()
    {
        DataContext = _vm;
        InitializeComponent();
    }

    private async void OnAlbumInvoked(object? sender, ListViewItemInvokedEventArgs e)
    {
        if (e.Item is not Album album)
        {
            return;
        }

        // The detail page: the cover, the title, then the tracks. The BackButton pops it (IsAutoEnabled).
        var tracks = new ItemsControl { ItemsSource = album.Tracks, ItemTemplate = (Avalonia.Controls.Templates.IDataTemplate)Resources["TrackRow"]!, Margin = new Thickness(0, 16, 0, 0) };
        var body = new StackPanel
        {
            Margin = new Thickness(20, 0),
            Children =
            {
                new Border { Height = 160, ClipToBounds = true, Child = new Image { Source = album.Cover.Image, Stretch = Stretch.UniformToFill } },
                new TextBlock { Text = album.Artist, Classes = { "win-type-small" }, Margin = new Thickness(0, 10, 0, 0) },
                tracks,
            },
        };
        await Nav.PushAsync(new ContentPage { Header = album.Title, Content = new ScrollViewer { Content = body } });
    }
}
