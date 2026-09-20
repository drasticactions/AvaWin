using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvaWin.Gallery;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public static AvaWinTheme Theme => Current!.Styles.OfType<AvaWinTheme>().First();

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            // Desktop: the head owns the Window (size switches, screenshot mode) and hosts MainView in it.
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = DesktopWindowFactory?.Invoke();
                break;
            // Android: one view per activity.
            case IActivityApplicationLifetime activity:
                activity.MainViewFactory = () => new MainView();
                break;
            // iOS and Browser: one view for the whole app.
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Set by the Desktop head before <c>StartWithClassicDesktopLifetime</c>; creates the window that hosts <see cref="MainView"/>.</summary>
    public static System.Func<Avalonia.Controls.Window>? DesktopWindowFactory { get; set; }
}
