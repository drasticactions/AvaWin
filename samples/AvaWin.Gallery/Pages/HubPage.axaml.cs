using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class HubPage : SamplePage
{
    private readonly HubViewModel _vm = new();

    /// <summary>The background moves at this fraction of the hub's scroll.</summary>
    private const double ParallaxRate = 0.4;

    public HubPage()
    {
        DataContext = _vm;
        InitializeComponent();
        // Hub.ScrollPosition notifies on every pan; the scene shifts at a fraction of it.
        ParallaxHub.GetObservable(Hub.ScrollPositionProperty).Subscribe(new Avalonia.Reactive.AnonymousObserver<double>(x => ParallaxScene.Offset = x * ParallaxRate));
    }

    private void OnHeaderInvoked(object? sender, HubHeaderInvokedEventArgs e) => _vm.Status = $"HeaderInvoked: section {e.Index} “{e.Section.Header}”";

    private void OnJump(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            NewsHub.SectionOnScreen = int.Parse(tag, System.Globalization.CultureInfo.InvariantCulture);
            JumpStatus.Text = $"SectionOnScreen: {NewsHub.SectionOnScreen}";
        }
    }
}
