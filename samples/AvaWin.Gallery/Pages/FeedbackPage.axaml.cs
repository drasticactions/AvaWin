using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class FeedbackPage : SamplePage
{
    public FeedbackPage()
    {
        DataContext = new UploadViewModel();
        InitializeComponent();
        DataValidationErrors.SetError(Invalid, new Exception("Enter an email address."));
        Refresher.RefreshRequested += async (_, e) =>
        {
            var deferral = e.GetDeferral();
            RefreshStatus.Text = "Refreshing...";
            await Task.Delay(1200);
            RefreshStatus.Text = $"Refreshed at {DateTime.Now:HH:mm:ss}";
            deferral.Complete();
        };
    }

    private void OnRefresh(object? sender, RoutedEventArgs e) => Refresher.RequestRefresh();
}
