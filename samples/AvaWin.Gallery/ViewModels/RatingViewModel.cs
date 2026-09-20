using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaWin.Gallery.ViewModels;

public sealed record AlbumRating(Album Album, double Average, int Votes);

/// <summary>The album review hero on the Rating page.</summary>
public sealed partial class RatingViewModel : ObservableObject
{
    public RatingViewModel()
    {
        Album = SampleData.Albums[0];
        Status = "Hover to preview a rating, click to set it, click the same star again to clear.";
    }

    public Album Album { get; }

    public IReadOnlyList<AlbumRating> Others { get; } = SampleData.Albums.Skip(1).Select((a, i) => new AlbumRating(a, 2.5 + (i * 0.7) % 2.5, 12 + i * 37)).ToList();

    [ObservableProperty]
    public partial int UserRating { get; set; } = 4;

    [ObservableProperty]
    public partial string Status { get; set; }
}
