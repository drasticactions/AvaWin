using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class RatingPage : SamplePage
{
    private readonly RatingViewModel _vm = new();

    public RatingPage()
    {
        DataContext = _vm;
        InitializeComponent();
        Stars.PreviewChange += (_, e) => _vm.Status = $"PreviewChange: {e.TentativeRating}";
        Stars.Change += (_, e) => _vm.Status = $"Change: UserRating {e.UserRating}";
        Stars.Cancel += (_, _) => _vm.Status = $"Cancel: UserRating stays {Stars.UserRating}";
    }
}
