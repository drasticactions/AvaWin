using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;

namespace AvaWin.Gallery.Pages;

public sealed record IconEntry(AppBarIcon Icon, string Name);

public partial class IconsPage : SamplePage
{
    private static readonly List<IconEntry> All = AppBarIcons.All.Select(i => new IconEntry(i, i.ToString())).ToList();

    public IconsPage()
    {
        InitializeComponent();
        Apply(string.Empty);
        Filter.QueryChanged += (_, e) => Apply(e.QueryText);
    }

    private void Apply(string query)
    {
        var list = string.IsNullOrWhiteSpace(query) ? All : All.Where(i => i.Name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        Icons.ItemsSource = list;
        Count.Text = list.Count == All.Count ? $"{All.Count} icons" : $"{list.Count} of {All.Count} icons";
    }
}
