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

public class SettingsFlyoutTests
{
    [AvaloniaFact]
    public void Safe_Area_Pads_The_Root_And_Keeps_The_Pane_Width()
    {
        var flyout = new SettingsFlyout { Header = "Options", Content = new TextBlock { Text = "c" } };
        var window = ThemeTestHelpers.Host(new Grid { Children = { flyout } }, "Light", Platform.Desktop);
        flyout.Show();
        window.UpdateLayout();
        // Headless has no InsetsManager: set what the host would.
        flyout.Pane.SafeAreaPadding = new Thickness(0, 20, 44, 34);
        window.UpdateLayout();

        var root = flyout.Pane.GetVisualChildren().OfType<Border>().Single();
        var body = flyout.Pane.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Body");
        Assert.Equal(new Thickness(0, 20, 44, 34), root.Padding);
        Assert.Equal(345, body.Bounds.Width, 0.5);
        Assert.Equal(345 + 44, flyout.Pane.Bounds.Width, 0.5);
        Assert.Equal(window.Bounds.Width, flyout.Pane.Bounds.Right, 0.5);
        var bodyRight = body.TranslatePoint(new Point(body.Bounds.Width, 0), window)!.Value.X;
        Assert.Equal(window.Bounds.Width - 44, bodyRight, 0.5);

        flyout.IgnoreSafeArea = true;
        Assert.False(flyout.Host.RespectsSafeArea);
        window.Close();
    }

    [AvaloniaFact]
    public void Show_Hide_Escape_And_Widths()
    {
        var flyout = new SettingsFlyout { Header = "Options", Content = new TextBlock { Text = "c" } };
        var window = ThemeTestHelpers.Host(new Grid { Children = { flyout } }, "Light", Platform.Desktop);
        var events = new System.Collections.Generic.List<string>();
        flyout.Opened += (_, _) => events.Add("opened");
        flyout.Closed += (_, _) => events.Add("closed");
        Assert.False(flyout.Pane.IsVisible);
        flyout.Show();
        window.UpdateLayout();
        Assert.True(flyout.Pane.IsVisible);
        Assert.Equal(345, flyout.Pane.Bounds.Width, 0.5);
        Assert.Equal(window.Bounds.Width, flyout.Pane.Bounds.Right, 0.5);
        Assert.Equal(window.Bounds.Height, flyout.Pane.Bounds.Height, 0.5);
        Assert.True(flyout.Host.IsLightDismissEnabled);

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        window.UpdateLayout();
        Assert.False(flyout.IsOpen);
        Assert.False(flyout.Pane.IsVisible);
        Assert.Equal(["opened", "closed"], events);

        flyout.PaneWidth = SettingsFlyoutWidth.Wide;
        flyout.Show();
        window.UpdateLayout();
        Assert.Equal(645, flyout.Pane.Bounds.Width, 0.5);
        window.MouseDown(new Point(10, 10), MouseButton.Left);
        window.MouseUp(new Point(10, 10), MouseButton.Left);
        Assert.False(flyout.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Header_Back_Button_Hides_The_Pane()
    {
        var flyout = new SettingsFlyout { Header = "Options" };
        var window = ThemeTestHelpers.Host(new Grid { Children = { flyout } }, "Light", Platform.Desktop);
        flyout.Show();
        window.UpdateLayout();
        var back = flyout.Pane.GetVisualDescendants().OfType<BackButton>().Single();
        Assert.True(back.IsEnabled);
        AppBarTests.Click(window, back);
        Assert.False(flyout.IsOpen);
        window.Close();
    }
}
