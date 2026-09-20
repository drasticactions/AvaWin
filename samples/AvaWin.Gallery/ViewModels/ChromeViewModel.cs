using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaWin.Gallery.ViewModels;

/// <summary>Data behind the chrome pages (AppBar, NavBar, SettingsFlyout, CommandBar, Flyouts): a photo app.</summary>
public sealed partial class ChromeViewModel : ObservableObject
{
    public ChromeViewModel()
    {
        Status = string.Empty;
        Current = SampleData.Photos[0];
        Note = "Harbor at dusk\n\nThe light was perfect around six. Wider walkways, a new tram stop and space for the weekend market — worth a second visit once the pier reopens.";
    }

    public IReadOnlyList<Photo> Photos { get; } = SampleData.Photos;

    public IReadOnlyList<Photo> Recent { get; } = SampleData.Photos.Take(8).ToList();

    [ObservableProperty]
    public partial string Status { get; set; }

    [ObservableProperty]
    public partial int SelectedCount { get; set; }

    [ObservableProperty]
    public partial Photo Current { get; set; }

    [ObservableProperty]
    public partial string Note { get; set; }

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    public bool HasSelection => SelectedCount > 0;

    partial void OnSelectedCountChanged(int value) => OnPropertyChanged(nameof(HasSelection));
}
