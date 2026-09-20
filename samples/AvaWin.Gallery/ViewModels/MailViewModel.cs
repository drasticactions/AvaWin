using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaWin.Gallery.ViewModels;

public sealed record Message(Contact From, string Subject, string Preview, DateTime Received, bool IsUnread)
{
    public string When => Received.Date == new DateTime(2014, 4, 8) ? Received.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture) : Received.ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture);
}

public sealed record Folder(string Name, int Unread, IReadOnlyList<Folder> Children)
{
    public string Label => Unread > 0 ? $"{Name} ({Unread})" : Name;
}

/// <summary>The mail-app hero on the Collections page.</summary>
public sealed partial class MailViewModel : ObservableObject
{
    private static readonly string[] Subjects =
    [
        "Re: harbor photos", "Trip dates?", "Lunch on Thursday", "Slides for the review", "The green ridge trail", "Your order has shipped",
        "Invitation: album launch", "Weekend plan", "Re: Re: tide pools", "Draft two attached", "Concert tickets", "Recipe you asked for",
    ];

    private static readonly string[] Previews =
    [
        "Here are the ones from the pier, the light was perfect around six.", "Does the second week of May still work for everyone?",
        "The place on the corner has a new menu, want to try it?", "Attached, still missing the numbers for Q2.",
        "We could do the loop in about four hours if we start early.", "Track your package with the link below.",
        "Doors at seven, bring whoever you like.", "Thinking of the coast if the weather holds.", "Low tide is at 9:40, meet at the car park?",
        "Changed the ending, let me know what you think.", "Got two extra for Saturday.", "Halve the salt, double the lemon.",
    ];

    public MailViewModel()
    {
        var people = SampleData.Contacts(12);
        var start = new DateTime(2014, 4, 8, 17, 30, 0);
        Messages = Enumerable.Range(0, 12).Select(i => new Message(people[i], Subjects[i], Previews[i], start.AddHours(-i * 5.5), i % 4 == 0)).ToList();
        SelectedMessage = Messages[0];
        Folders =
        [
            new("Inbox", 3, [new("Family", 1, []), new("Work", 2, []), new("Receipts", 0, [])]),
            new("Drafts", 0, []),
            new("Sent", 0, []),
            new("Archive", 0, [new("2013", 0, []), new("2014", 0, [])]),
        ];
        Contacts = SampleData.Contacts(24);
    }

    public List<Message> Messages { get; }

    public IReadOnlyList<Folder> Folders { get; }

    public List<Contact> Contacts { get; }

    public IReadOnlyList<Photo> Photos { get; } = SampleData.Photos.Take(5).ToList();

    [ObservableProperty]
    public partial Message? SelectedMessage { get; set; }
}
