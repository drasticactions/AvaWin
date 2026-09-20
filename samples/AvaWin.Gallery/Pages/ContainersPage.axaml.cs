using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class ContainersPage : SamplePage
{
    public ContainersPage()
    {
        DataContext = new MusicViewModel();
        InitializeComponent();
    }
}
