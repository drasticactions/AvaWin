using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaWin.Gallery.ViewModels;

public sealed partial class Upload : ObservableObject
{
    public Upload(Photo photo, double progress)
    {
        Photo = photo;
        Progress = progress;
    }

    public Photo Photo { get; }

    [ObservableProperty]
    public partial double Progress { get; set; }

    public bool IsDone => Progress >= 100;

    public string State => Progress >= 100 ? "Uploaded" : Progress <= 0 ? "Waiting" : $"{Progress:0}%";

    partial void OnProgressChanged(double value)
    {
        OnPropertyChanged(nameof(IsDone));
        OnPropertyChanged(nameof(State));
    }
}

/// <summary>The upload screen on the Feedback page.</summary>
public sealed partial class UploadViewModel : ObservableObject
{
    private bool _running;

    public UploadViewModel()
    {
        Uploads = new ObservableCollection<Upload>(SampleData.Photos.Take(4).Select((p, i) => new Upload(p, new[] { 100, 65, 20, 0 }[i])));
        Contacts = SampleData.Contacts(8);
        Message = "2 of 4 photos still uploading. This card is a NotificationCard.";
    }

    public ObservableCollection<Upload> Uploads { get; }

    public List<Contact> Contacts { get; }

    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial bool IsPreparing { get; set; } = true;

    [RelayCommand]
    private async Task ResumeAsync()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        IsPreparing = false;
        foreach (var u in Uploads.Where(u => !u.IsDone))
        {
            while (u.Progress < 100)
            {
                await Task.Delay(60);
                u.Progress = System.Math.Min(100, u.Progress + 4);
            }
        }

        Message = "All 4 photos uploaded.";
        _running = false;
    }
}
