using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace AvaWin.Gallery.Android;

[Activity(
    Label = "AvaWin Gallery",
    Theme = "@style/AvaWinTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
