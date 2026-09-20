using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaWin.Gallery.ViewModels;

/// <summary>The search screen on the SearchBox page: contacts, filtered by the submitted query.</summary>
public sealed partial class SearchViewModel : ObservableObject
{
    public SearchViewModel()
    {
        Contacts = SampleData.Contacts(60);
        Results = Contacts;
        Status = "Type to see suggestions; Enter or the magnifier submits.";
    }

    public List<Contact> Contacts { get; }

    [ObservableProperty]
    public partial IReadOnlyList<Contact> Results { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; }

    public IEnumerable<Contact> Matches(string query) => string.IsNullOrWhiteSpace(query)
        ? Contacts
        : Contacts.Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || c.City.Contains(query, StringComparison.OrdinalIgnoreCase));
}
