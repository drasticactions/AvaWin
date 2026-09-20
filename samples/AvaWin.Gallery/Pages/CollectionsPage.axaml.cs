using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class CollectionsPage : SamplePage
{
    public CollectionsPage()
    {
        DataContext = new MailViewModel();
        InitializeComponent();
    }

    /// <summary>Open the Inbox node so the tree shows its chevrons and nesting on first sight.</summary>
    private void OnTreeLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TreeView tree && tree.ContainerFromIndex(0) is TreeViewItem inbox)
        {
            inbox.IsExpanded = true;
            inbox.IsSelected = true;
        }
    }
}
