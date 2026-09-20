using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaWin.Gallery.ViewModels;

/// <summary>The photo-viewer hero on the FlipView page and the contacts for SemanticZoom.</summary>
public sealed partial class PhotosViewModel : ObservableObject
{
    public PhotosViewModel()
    {
        Contacts = SampleData.Contacts(80);
        Groups = SampleData.Groups(Contacts);
        Status = string.Empty;
    }

    public IReadOnlyList<Photo> Photos { get; } = SampleData.Photos;

    public IReadOnlyList<Photo> Featured { get; } = SampleData.Photos.Take(6).ToList();

    public List<Contact> Contacts { get; }

    public List<ContactGroup> Groups { get; }

    public System.Func<object?, object?> GroupKey { get; } = o => ((Contact)o!).Group;

    public System.Func<object?, object?> GroupsKey { get; } = g => ((ContactGroup)g!).Key;

    [ObservableProperty]
    public partial int CurrentPage { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; }

    public string Position => $"{CurrentPage + 1} of {Photos.Count}";

    public Photo Current => Photos[CurrentPage < 0 ? 0 : CurrentPage % Photos.Count];

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(Current));
    }
}
