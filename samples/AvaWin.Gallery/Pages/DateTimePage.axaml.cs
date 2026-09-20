using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class DateTimePage : SamplePage
{
    public DateTimePage()
    {
        DataContext = new FormsViewModel();
        InitializeComponent();
    }

    /// <summary>Block a long weekend so the blackout style is visible.</summary>
    private void OnCalendarLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Calendar c && c.BlackoutDates.Count == 0)
        {
            c.BlackoutDates.Add(new CalendarDateRange(new DateTime(2014, 4, 18), new DateTime(2014, 4, 21)));
        }
    }
}
