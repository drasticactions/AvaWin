using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Desktop;

/// <summary>Hosts <see cref="MainView"/>; owns what only a window can do: the size switches and the <c>--screenshot</c> mode.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SettingsViewModel.Instance.PlatformChanged += OnPlatformChanged;
        ApplyLaunchOptions();
    }

    private void ApplyLaunchOptions()
    {
        var o = LaunchOptions.Current;
        Width = o.Width;
        Height = o.Height;
        SettingsViewModel.Instance.Variant = o.Variant;
        SettingsViewModel.Instance.Platform = Enum.Parse<Platform>(o.Platform, true);
        SettingsViewModel.Instance.DisableAll = o.DisableAll;
        if (o.Page is { } name)
        {
            View.TryNavigate(name);
        }

        if (o.Screenshot is { } path)
        {
            Opened += async (_, _) =>
            {
                await System.Threading.Tasks.Task.Delay(LaunchOptions.Current.DelayMs);
                foreach (var state in (o.WindowStates ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    WindowState = Enum.Parse<WindowState>(state, true);
                    await System.Threading.Tasks.Task.Delay(o.DelayMs);
                }

                if (o.Scroll > 0)
                {
                    View.PageScroller.Offset = new Avalonia.Vector(0, o.Scroll);
                    await System.Threading.Tasks.Task.Delay(100);
                }

                foreach (var button in (o.Invoke ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var candidates = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(this).OfType<Button>()
                        .Concat(View.CommandBar.Commands);
                    if (candidates.FirstOrDefault(b => b.Name == button) is { } target)
                    {
                        target.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await System.Threading.Tasks.Task.Delay(o.InvokeDelayMs ?? o.DelayMs);
                    }
                }

                var size = new Avalonia.PixelSize((int)Bounds.Width, (int)Bounds.Height);
                using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size, new Avalonia.Vector(96, 96));
                bitmap.Render(this);
                bitmap.Save(path, new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
                Close();
            };
        }
    }

    private void OnPlatformChanged(object? sender, EventArgs e)
    {
        // Mimic a handset when Phone is chosen; the user can resize afterwards.
        if (App.Theme.ActualPlatform == Platform.Phone)
        {
            Width = 400;
            Height = 760;
        }
    }
}
