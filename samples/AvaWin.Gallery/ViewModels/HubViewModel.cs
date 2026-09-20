using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaWin.Gallery.ViewModels;

/// <summary>The news-app hero on the Hub page: one section per article category.</summary>
public sealed partial class HubViewModel : ObservableObject
{
    public Article Lead { get; } = SampleData.Articles[0];

    public IReadOnlyList<Article> TopStories { get; } = SampleData.Articles.Where(a => a.Section == "Top stories").Skip(1).ToList();

    public IReadOnlyList<Article> Travel { get; } = Section("Travel");

    public IReadOnlyList<Article> Science { get; } = Section("Science");

    public IReadOnlyList<Article> Culture { get; } = Section("Culture");

    [ObservableProperty]
    public partial string Status { get; set; }

    public HubViewModel()
    {
        Status = "Click a section header (the ones with a chevron) to see HeaderInvoked.";
    }

    private static List<Article> Section(string name) => SampleData.Articles.Where(a => a.Section == name).ToList();
}
