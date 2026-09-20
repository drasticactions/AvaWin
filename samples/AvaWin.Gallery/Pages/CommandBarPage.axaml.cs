using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class CommandBarPage : SamplePage
{
    public CommandBarPage()
    {
        DataContext = new ChromeViewModel();
        InitializeComponent();
    }
}
