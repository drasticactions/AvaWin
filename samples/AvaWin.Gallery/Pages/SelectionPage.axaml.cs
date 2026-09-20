using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class SelectionPage : SamplePage
{
    public SelectionPage()
    {
        DataContext = new FormsViewModel();
        InitializeComponent();
    }
}
