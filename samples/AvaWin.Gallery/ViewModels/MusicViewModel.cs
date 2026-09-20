using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaWin.Gallery.ViewModels;

/// <summary>A song row in the music demos: a track with its album.</summary>
public sealed record Song(Album Album, Track Track)
{
    public string Title => Track.Title;

    public string Subtitle => $"{Album.Artist} · {Album.Title}";

    public string Duration => Track.Duration;
}

/// <summary>The music-app hero shared by the Pivot and Pages demos.</summary>
public sealed partial class MusicViewModel : ObservableObject
{
    public MusicViewModel()
    {
        Status = string.Empty;
    }

    public IReadOnlyList<Album> Albums { get; } = SampleData.Albums;

    public IReadOnlyList<string> Artists { get; } = SampleData.Albums.Select(a => a.Artist).Distinct().OrderBy(a => a).ToList();

    public IReadOnlyList<Song> Songs { get; } = SampleData.Albums.SelectMany(a => a.Tracks.Select(t => new Song(a, t))).OrderBy(s => s.Title).ToList();

    public IReadOnlyList<string> Playlists { get; } = ["Morning commute", "Focus", "Weekend", "Late night"];

    [ObservableProperty]
    public partial Album? SelectedAlbum { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; }

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }
}
