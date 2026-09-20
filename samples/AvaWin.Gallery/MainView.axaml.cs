using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Gallery.Pages;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery;

/// <summary>The title row above a page. It transitions together with the page.</summary>
public sealed record PageHeader(string? Group, string Title);

/// <summary>The gallery chrome and page host. Every platform head shows this control; the Desktop head puts it in a Window.</summary>
public partial class MainView : UserControl
{
    private sealed class RelayCommand(Action action) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => action();
    }

    public MainView()
    {
        InitializeComponent();
        DataContext = SettingsViewModel.Instance;
        BuildNav();
        // Ctrl+, opens the SettingsFlyout, like the settings charm.
        KeyBindings.Add(new Avalonia.Input.KeyBinding { Gesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.OemComma, Avalonia.Input.KeyModifiers.Control), Command = new RelayCommand(() => SettingsPane.IsOpen = !SettingsPane.IsOpen) });
        PageHost.PageTransition = new AvaWin.Animations.WinPageTransition();
        HeaderHost.PageTransition = new AvaWin.Animations.WinPageTransition();
        // The page transition already animates the incoming page; a Hub or ListView on it would run its own
        // entrance animation on top.
        PageHost.AddHandler(AvaWin.Controls.Hub.ContentAnimatingEvent, (_, e) => e.Cancel |= e.Type == AvaWin.Controls.HubAnimationType.Entrance);
        PageHost.AddHandler(AvaWin.Controls.ListView.ContentAnimatingEvent, (_, e) => e.Cancel |= e.Type == "entrance");
        ShowHome();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Phones: draw under the system bars. The TopLevel pads this view by the safe area itself
        // (TopLevel.AutoSafeAreaPadding); the overlay chrome (AppBar, NavBar, SettingsFlyout) insets itself.
        if (TopLevel.GetTopLevel(this)?.InsetsManager is { } insets)
        {
            insets.DisplayEdgeToEdgePreference = true;
        }
    }

    private void BuildNav()
    {
        NavContainer.Items.Add(new AvaWin.Controls.NavBarCommand { Label = "Home", Icon = AppBarIcon.Home, Location = "home" });
        foreach (var page in GalleryPages.All)
        {
            NavContainer.Items.Add(new AvaWin.Controls.NavBarCommand
            {
                Label = page.Name,
                Icon = page.Icon,
                Location = page.Name,
                State = page,
            });
        }
    }

    private void OnNavInvoked(object? sender, AvaWin.Controls.NavBarInvokedEventArgs e)
    {
        NavBar.Close();
        if (e.NavBarCommand.State is GalleryPage page)
        {
            Navigate(page);
        }
        else
        {
            ShowHome();
        }
    }

    private void OnToggleNav(object? sender, RoutedEventArgs e)
    {
        CommandBar.Close();
        NavBar.IsOpen = !NavBar.IsOpen;
    }

    internal void ShowHome()
    {
        PageScroller.Offset = default;
        PageHost.Content = new HomePage(page => Navigate(page));
        BackButton.IsEnabled = false;
        HeaderHost.Content = new PageHeader(string.Empty, "AvaWin");
    }

    internal void Navigate(GalleryPage page)
    {
        PageScroller.Offset = default;
        PageHost.Content = page.Create();
        BackButton.IsEnabled = true;
        HeaderHost.Content = new PageHeader(page.Group.ToUpperInvariant(), page.Name);
    }

    /// <summary>Opens the page called <paramref name="name"/> (case-insensitive); false when there is no such page.</summary>
    public bool TryNavigate(string name)
    {
        if (GalleryPages.All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) is { } page)
        {
            Navigate(page);
            return true;
        }

        return false;
    }

    private void OnBack(object? sender, RoutedEventArgs e) => ShowHome();

    private void OnHome(object? sender, RoutedEventArgs e)
    {
        CommandBar.Close();
        ShowHome();
    }

    private void OnToggleSettings(object? sender, RoutedEventArgs e)
    {
        CommandBar.Close();
        SettingsPane.IsOpen = !SettingsPane.IsOpen;
    }
}
