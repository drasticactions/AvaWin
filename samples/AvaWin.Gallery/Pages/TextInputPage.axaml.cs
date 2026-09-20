using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class TextInputPage : SamplePage
{
    public TextInputPage()
    {
        DataContext = new FormsViewModel();
        InitializeComponent();
    }
}
