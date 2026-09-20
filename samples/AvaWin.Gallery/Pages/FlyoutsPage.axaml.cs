using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class FlyoutsPage : SamplePage
{
    private readonly ChromeViewModel _vm = new();

    public FlyoutsPage()
    {
        DataContext = _vm;
        InitializeComponent();
        _vm.Status = "Hover the buttons for tooltips; right-click the photo for its menu.";
    }

    private void OnDelete(object? sender, RoutedEventArgs e)
    {
        var yes = new Button { Content = "Delete", Classes = { "accent" } };
        var flyout = new Flyout
        {
            Content = new StackPanel
            {
                Spacing = 10,
                Children = { new TextBlock { Text = "Delete this photo?", Classes = { "win-type-medium" } }, yes },
            },
        };
        yes.Click += (_, _) =>
        {
            flyout.Hide();
            _vm.Status = "Deleted (not really).";
        };
        flyout.ShowAt(DeleteButton, WinFlyoutPlacement.Top);
    }

    private void OnPlace(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } b)
        {
            Confirm().ShowAt(b, Enum.Parse<WinFlyoutPlacement>(tag));
        }
    }

    private void OnAlign(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } b)
        {
            Confirm().ShowAt(b, WinFlyoutPlacement.Bottom, Enum.Parse<WinFlyoutAlignment>(tag));
        }
    }

    private static Flyout Confirm() => new()
    {
        Content = new StackPanel
        {
            Spacing = 10,
            Children = { new TextBlock { Text = "Are you sure?", Classes = { "win-type-medium" } }, new Button { Content = "Yes", Classes = { "accent" } } },
        },
    };
}
