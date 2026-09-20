using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaWin.Gallery.ViewModels;

/// <summary>Data for the ListView page: a people app plus the smaller scenario lists.</summary>
public sealed partial class ListViewViewModel : ObservableObject
{
    private int _added = 1;

    public ListViewViewModel()
    {
        Contacts = SampleData.Contacts(80);
        Groups = SampleData.Groups(Contacts);
        Reorderable = new ObservableCollection<Contact>(SampleData.Contacts(8));
        Editable = new ObservableCollection<Contact>(SampleData.Contacts(6));
        Incremental = new SampleData.IncrementalContacts(20, 200);
        Incremental.CollectionChanged += (_, _) => IncrementalStatus = $"{Incremental.Count} of 200 loaded in {Incremental.LoadCalls} page loads";
        Status = "Tap a person to invoke; tap again or press Space to select.";
        DragStatus = "Drag a row to reorder the list.";
        IncrementalStatus = "20 of 200 loaded — scroll to the end to load more";
    }

    public List<Contact> Contacts { get; }

    public List<ContactGroup> Groups { get; }

    /// <summary>The group key of an item.</summary>
    public Func<object?, object?> GroupKey { get; } = o => ((Contact)o!).Group;

    /// <summary>The key of a group item.</summary>
    public Func<object?, object?> GroupsKey { get; } = g => ((ContactGroup)g!).Key;

    public IReadOnlyList<Photo> Photos { get; } = SampleData.Photos;

    public ObservableCollection<Contact> Reorderable { get; }

    public ObservableCollection<Contact> Editable { get; }

    public SampleData.IncrementalContacts Incremental { get; }

    [ObservableProperty]
    public partial string Status { get; set; }

    [ObservableProperty]
    public partial string DragStatus { get; set; }

    [ObservableProperty]
    public partial string IncrementalStatus { get; set; }

    [ObservableProperty]
    public partial bool IsSelectionModeActive { get; set; } = true;

    /// <summary>A new contact for the add/remove scenario.</summary>
    public Contact NextContact() => new("New", "Contact " + _added++, "Nowhere", SampleData.Palette[3]);

    [RelayCommand]
    private void ClearReorder() => DragStatus = "Drag a row to reorder the list.";
}
