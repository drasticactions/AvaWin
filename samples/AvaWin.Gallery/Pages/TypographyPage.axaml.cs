using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class TypographyPage : SamplePage
{
    public TypographyPage()
    {
        DataContext = new ChromeViewModel();
        InitializeComponent();
    }
}
