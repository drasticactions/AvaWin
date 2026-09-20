using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Controls.Tests;

public class BackButtonTests
{
    [AvaloniaFact]
    public async Task Tracks_CanGoBack_And_Pops()
    {
        var back = new BackButton();
        var nav = new NavigationPage();
        var root = new ContentPage { Content = new TextBlock { Text = "root" } };
        var second = new ContentPage { Content = new StackPanel { Children = { back } } };
        var window = ThemeTestHelpers.Host(nav, "Light", Platform.Desktop);
        await nav.PushAsync(root);
        await nav.PushAsync(second);
        window.UpdateLayout();
        Assert.Same(nav, back.ResolvedNavigation);
        Assert.True(back.IsEnabled);
        Assert.Contains(":cangoback", back.Classes);
        AppBarTests.Click(window, back);
        await Task.Yield();
        for (var i = 0; i < 20 && nav.StackDepth > 1; i++)
        {
            await Task.Delay(10);
        }

        Assert.Equal(1, nav.StackDepth);
        window.Close();
    }

    [AvaloniaFact]
    public void Disabled_When_No_Navigation_And_Opacity_Zero()
    {
        var back = new BackButton();
        var window = ThemeTestHelpers.Host(back, "Light", Platform.Desktop);
        Assert.False(back.IsEnabled);
        Assert.Equal(0, back.Opacity);
        Assert.Equal(41, back.Bounds.Width, 0.5);
        back.Classes.Add("small");
        window.UpdateLayout();
        Assert.Equal(30, back.Bounds.Width, 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void Manual_Mode_Leaves_IsEnabled_Alone()
    {
        var back = new BackButton { IsAutoEnabled = false };
        var window = ThemeTestHelpers.Host(back, "Light", Platform.Desktop);
        Assert.True(back.IsEnabled);
        var clicked = 0;
        back.Click += (_, _) => clicked++;
        AppBarTests.Click(window, back);
        Assert.Equal(1, clicked);
        window.Close();
    }
}
