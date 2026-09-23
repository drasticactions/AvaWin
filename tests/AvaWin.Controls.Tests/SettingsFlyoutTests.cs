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

    [AvaloniaFact]
    public void Inline_Pane_Lays_Out_In_Place_At_Both_Widths()
    {
        var flyout = new SettingsFlyout { Header = "Options", Content = new TextBlock { Text = "c" }, IsInline = true, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var window = ThemeTestHelpers.Host(new Grid { Children = { flyout } }, "Dark", Platform.Desktop);

        Assert.True(flyout.Pane.IsVisible);
        Assert.Same(flyout, flyout.Pane.GetVisualParent());
        Assert.Equal(345, flyout.Bounds.Width, 0.5);
        Assert.Equal(window.Bounds.Height, flyout.Bounds.Height, 0.5);
        Assert.Equal(0, flyout.Pane.TranslatePoint(default, window)!.Value.X, 0.5);

        flyout.PaneWidth = SettingsFlyoutWidth.Wide;
        window.UpdateLayout();
        Assert.Equal(645, flyout.Bounds.Width, 0.5);
        Assert.Equal(645, flyout.Pane.Bounds.Width, 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void Inline_Pane_Adds_No_Overlay_Child()
    {
        var flyout = new SettingsFlyout { Header = "Options", IsInline = true };
        var window = ThemeTestHelpers.Host(new Grid { Children = { flyout } }, "Light", Platform.Desktop);
        flyout.Show();
        window.UpdateLayout();

        var layer = OverlayLayer.GetOverlayLayer(window)!;
        Assert.DoesNotContain(layer.Children, c => c == flyout.Host);
        Assert.Null(flyout.Host.Layer);
        Assert.False(flyout.Host.IsLightDismissEnabled);
        window.Close();
    }

    [AvaloniaFact]
    public void Inline_Pane_Skips_The_Entrance_And_Raises_Its_Events()
    {
        var flyout = new SettingsFlyout { Header = "Options", IsInline = true };
        var window = ThemeTestHelpers.Host(new Grid { Children = { flyout } }, "Light", Platform.Desktop);
        var events = new System.Collections.Generic.List<string>();
        flyout.Opened += (_, _) => events.Add("opened");
        flyout.Closed += (_, _) => events.Add("closed");
        var scale = Animations.WinAnimations.TimeScale;
        Animations.WinAnimations.TimeScale = 1;
        try
        {
            flyout.Show();
            Assert.Equal(["opened"], events);
            Assert.Null(flyout.Pane.RenderTransform);
            Assert.Equal(1, flyout.Pane.Opacity);

            var back = flyout.Pane.GetVisualDescendants().OfType<BackButton>().Single();
            AppBarTests.Click(window, back);
            Assert.False(flyout.IsOpen);
            Assert.Equal(["opened", "closed"], events);
            Assert.True(flyout.Pane.IsVisible);
        }
        finally
        {
            Animations.WinAnimations.TimeScale = scale;
        }

        window.Close();
    }

    [AvaloniaFact]
    public void Inline_Can_Be_Turned_Off_Again()
    {
        var flyout = new SettingsFlyout { Header = "Options", IsInline = true };
        var window = ThemeTestHelpers.Host(new Grid { Children = { flyout } }, "Light", Platform.Desktop);
        flyout.IsInline = false;
        window.UpdateLayout();
        Assert.Same(flyout.Host, flyout.Pane.GetVisualParent());
        Assert.False(flyout.Pane.IsVisible);
        Assert.NotNull(flyout.Host.Layer);

        flyout.Show();
        window.UpdateLayout();
        Assert.True(flyout.Pane.IsVisible);
        Assert.Equal(window.Bounds.Width, flyout.Pane.Bounds.Right, 0.5);
        window.Close();
    }
}
