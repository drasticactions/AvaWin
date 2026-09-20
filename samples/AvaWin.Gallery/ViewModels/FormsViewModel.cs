using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaWin.Gallery.ViewModels;

/// <summary>State for the input pages' form heroes (share, new contact, photo settings, new event).</summary>
public sealed partial class FormsViewModel : ObservableObject
{
    public FormsViewModel()
    {
        Status = string.Empty;
        Name = "Ava Adler";
        Email = "ava.adler@example.com";
        City = "Seattle";
        Notes = string.Empty;
        EventTitle = "Album launch";
        EventDate = new DateTimeOffset(2014, 4, 8, 0, 0, 0, TimeSpan.Zero);
        Start = new TimeSpan(19, 0, 0);
        End = new TimeSpan(21, 30, 0);
        Reminder = new DateTime(2014, 4, 7);
        Quality = "High";
        Album = "Camera roll";
        Copies = 1;
    }

    public IReadOnlyList<string> Cities { get; } = ["Berlin", "Dublin", "Lisbon", "Mumbai", "Nairobi", "Oslo", "Prague", "São Paulo", "Seattle", "Sydney", "Tokyo", "Toronto"];

    public IReadOnlyList<string> Albums { get; } = ["Camera roll", "Harbor", "Trips", "Screenshots", "Shared with me"];

    public IReadOnlyList<Photo> Photos { get; } = SampleData.Photos.Take(4).ToList();

    [ObservableProperty]
    public partial string Status { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; }

    [ObservableProperty]
    public partial string City { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; }

    [ObservableProperty]
    public partial decimal? Age { get; set; } = 31;

    [ObservableProperty]
    public partial int Copies { get; set; }

    [ObservableProperty]
    public partial bool UploadAutomatically { get; set; } = true;

    [ObservableProperty]
    public partial bool IncludeLocation { get; set; }

    [ObservableProperty]
    public partial bool WifiOnly { get; set; } = true;

    [ObservableProperty]
    public partial string Quality { get; set; }

    [ObservableProperty]
    public partial string Album { get; set; }

    [ObservableProperty]
    public partial double JpegQuality { get; set; } = 85;

    [ObservableProperty]
    public partial string EventTitle { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? EventDate { get; set; }

    [ObservableProperty]
    public partial TimeSpan? Start { get; set; }

    [ObservableProperty]
    public partial TimeSpan? End { get; set; }

    [ObservableProperty]
    public partial DateTime? Reminder { get; set; }

    [ObservableProperty]
    public partial bool AllDay { get; set; }

    public string Summary => $"{(UploadAutomatically ? "Uploads automatically" : "Manual upload")}{(WifiOnly ? " on Wi-Fi" : string.Empty)}, {Quality.ToLowerInvariant()} quality ({JpegQuality:0}%) to “{Album}”{(IncludeLocation ? ", with location" : string.Empty)}.";

    public string EventSummary => EventDate is { } d ? $"{EventTitle}, {d.ToString("MMMM d", System.Globalization.CultureInfo.InvariantCulture)} {(AllDay ? "all day" : $"{Format(Start)}–{Format(End)}")}" : EventTitle;

    partial void OnUploadAutomaticallyChanged(bool value) => OnPropertyChanged(nameof(Summary));

    partial void OnIncludeLocationChanged(bool value) => OnPropertyChanged(nameof(Summary));

    partial void OnWifiOnlyChanged(bool value) => OnPropertyChanged(nameof(Summary));

    partial void OnQualityChanged(string value) => OnPropertyChanged(nameof(Summary));

    partial void OnAlbumChanged(string value) => OnPropertyChanged(nameof(Summary));

    partial void OnJpegQualityChanged(double value) => OnPropertyChanged(nameof(Summary));

    partial void OnEventTitleChanged(string value) => OnPropertyChanged(nameof(EventSummary));

    partial void OnEventDateChanged(DateTimeOffset? value) => OnPropertyChanged(nameof(EventSummary));

    partial void OnStartChanged(TimeSpan? value) => OnPropertyChanged(nameof(EventSummary));

    partial void OnEndChanged(TimeSpan? value) => OnPropertyChanged(nameof(EventSummary));

    partial void OnAllDayChanged(bool value) => OnPropertyChanged(nameof(EventSummary));

    [RelayCommand]
    private void Send() => Status = $"Sent {Copies} cop{(Copies == 1 ? "y" : "ies")} to {Email}.";

    [RelayCommand]
    private void Cancel() => Status = "Canceled.";

    [RelayCommand]
    private void Save() => Status = $"Saved {Name} ({Email}), {City}.";

    private static string Format(TimeSpan? t) => t is { } v ? new DateTime(2014, 1, 1).Add(v).ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture) : "?";
}
