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

public class AppBarTests
{
    private static (Window window, AppBar bar) Make(bool sticky = false, AppBarClosedDisplayMode mode = AppBarClosedDisplayMode.Minimal)
    {
        var bar = new AppBar { IsSticky = sticky, ClosedDisplayMode = mode };
        bar.Commands.Add(new AppBarCommand { Id = "add", Label = "Add", Icon = AppBarIcon.Add });
        bar.Commands.Add(new AppBarCommand { Id = "del", Label = "Delete", Icon = AppBarIcon.Delete, Section = AppBarCommandSection.Selection });
        var page = new Grid { Children = { new TextBlock { Text = "page" }, bar } };
        var window = ThemeTestHelpers.Host(page, "Light", Platform.Desktop);
        return (window, bar);
    }

    [AvaloniaFact]
    public void Presenter_Is_Hosted_In_OverlayLayer_And_Commands_Are_Laid_Out()
    {
        var (window, bar) = Make();
        Assert.NotNull(bar.Host.Layer);
        Assert.Same(OverlayLayer.GetOverlayLayer(bar), bar.Host.Layer);
        Assert.Equal(0, bar.DesiredSize.Height);
        bar.Open();
        window.UpdateLayout();
        var panel = bar.Bar.GetVisualDescendants().OfType<AppBarCommandsPanel>().Single();
        Assert.Equal(2, panel.Children.Count);
        var add = bar.GetCommandById("add")!;
        var del = bar.GetCommandById("del")!;
        Assert.True(add.Bounds.X > del.Bounds.X, "global commands sit to the right of selection commands");
        Assert.True(bar.Bar.Bounds.Height >= 80);
        Assert.Equal(window.Bounds.Height, bar.Bar.Bounds.Bottom, 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void Open_And_Close_Raise_Events_And_Cancel_Works()
    {
        var (window, bar) = Make();
        var log = new System.Collections.Generic.List<string>();
        bar.Opening += (_, _) => log.Add("opening");
        bar.Opened += (_, _) => log.Add("opened");
        bar.Closing += (_, e) => { log.Add("closing"); if (log.Count == 3) e.Cancel = true; };
        bar.Closed += (_, _) => log.Add("closed");
        bar.Open();
        window.UpdateLayout();
        Assert.True(bar.IsOpen);
        Assert.Contains(":open", bar.Classes);
        bar.Close();
        Assert.True(bar.IsOpen, "canceled Closing keeps the bar open");
        bar.Close();
        window.UpdateLayout();
        Assert.False(bar.IsOpen);
        Assert.Equal(["opening", "opened", "closing", "closing", "closed"], log);
        window.Close();
    }

    [AvaloniaFact]
    public void Escape_Closes_And_Light_Dismiss_Respects_Sticky()
    {
        var (window, bar) = Make();
        bar.Open();
        window.UpdateLayout();
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.False(bar.IsOpen);

        bar.Open();
        window.UpdateLayout();
        Assert.True(bar.Host.IsLightDismissEnabled);
        window.MouseDown(new Point(10, 10), MouseButton.Left);
        window.MouseUp(new Point(10, 10), MouseButton.Left);
        Assert.False(bar.IsOpen);

        bar.IsSticky = true;
        bar.Open();
        window.UpdateLayout();
        Assert.False(bar.Host.IsLightDismissEnabled);
        window.MouseDown(new Point(10, 10), MouseButton.Left);
        window.MouseUp(new Point(10, 10), MouseButton.Left);
        Assert.True(bar.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Right_Click_Toggles_Non_Sticky_Bar()
    {
        var (window, bar) = Make();
        window.MouseDown(new Point(10, 10), MouseButton.Right);
        window.MouseUp(new Point(10, 10), MouseButton.Right);
        Assert.True(bar.IsOpen);
        window.MouseDown(new Point(10, 10), MouseButton.Right);
        window.MouseUp(new Point(10, 10), MouseButton.Right);
        Assert.False(bar.IsOpen);
        bar.IsRightClickToggleEnabled = false;
        window.MouseDown(new Point(10, 10), MouseButton.Right);
        window.MouseUp(new Point(10, 10), MouseButton.Right);
        Assert.False(bar.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Closing_Slides_The_Open_Bar_Out_Before_It_Becomes_The_Strip()
    {
        var (window, bar) = Make(mode: AppBarClosedDisplayMode.Minimal);
        AvaWin.Animations.WinAnimations.TimeScale = 1;
        try
        {
            bar.Open();
            window.UpdateLayout();
            var closed = 0;
            bar.Closed += (_, _) => closed++;
            var openHeight = bar.Bar.Bounds.Height;
            Assert.True(openHeight >= 80);
            bar.Close();
            window.UpdateLayout();
            // The bar that slides out is the open one: still the open layout, still visible, moving toward the strip.
            Assert.False(bar.IsOpen);
            Assert.True(bar.Bar.IsOpen, "the presenter keeps its open layout while closing");
            Assert.True(bar.Bar.IsVisible);
            Assert.Contains(":closing", bar.Bar.Classes);
            Assert.Equal(openHeight, bar.Bar.Bounds.Height, 0.5);
            Assert.Equal(0, closed);

            for (var i = 0; i < 120 && closed == 0; i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                await Task.Delay(16);
            }

            window.UpdateLayout();
            Assert.Equal(1, closed);
            Assert.False(bar.Bar.IsOpen);
            Assert.DoesNotContain(":closing", bar.Bar.Classes);
            Assert.True(bar.Bar.IsVisible);
            Assert.Equal(25, bar.Bar.Bounds.Height, 0.5);
            Assert.Equal(window.Bounds.Height, bar.Bar.Bounds.Bottom, 0.5);
        }
        finally
        {
            AvaWin.Animations.WinAnimations.TimeScale = 0;
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Closing_A_Bar_With_No_Strip_Hides_It_After_The_Slide()
    {
        var (window, bar) = Make(mode: AppBarClosedDisplayMode.None);
        AvaWin.Animations.WinAnimations.TimeScale = 1;
        try
        {
            bar.Open();
            window.UpdateLayout();
            var closed = 0;
            bar.Closed += (_, _) => closed++;
            bar.Close();
            window.UpdateLayout();
            Assert.True(bar.Bar.IsVisible, "the bar stays on screen for its slide out");
            Assert.True(bar.Bar.IsOpen);

            for (var i = 0; i < 120 && closed == 0; i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                await Task.Delay(16);
            }

            Assert.Equal(1, closed);
            Assert.False(bar.Bar.IsVisible);
            Assert.False(bar.Bar.IsOpen);
        }
        finally
        {
            AvaWin.Animations.WinAnimations.TimeScale = 0;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Closed_Display_Modes_Size_The_Strip()
    {
        var (window, bar) = Make(mode: AppBarClosedDisplayMode.Minimal);
        window.UpdateLayout();
        Assert.True(bar.Bar.IsVisible);
        Assert.Equal(25, bar.Bar.Bounds.Height, 0.5);
        var invoke = bar.Bar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_InvokeButton");
        Assert.True(invoke.IsVisible);

        bar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
        window.UpdateLayout();
        Assert.Equal(60, bar.Bar.Bounds.Height, 0.5);

        bar.ClosedDisplayMode = AppBarClosedDisplayMode.None;
        window.UpdateLayout();
        Assert.False(bar.Bar.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Safe_Area_Pads_The_Root_And_Keeps_The_Ellipsis_Inside_It()
    {
        var (window, bar) = Make(mode: AppBarClosedDisplayMode.Minimal);
        window.UpdateLayout();
        // Headless has no InsetsManager: set what the host would.
        Assert.Equal(default, bar.Host.SafeAreaPadding);
        bar.Bar.SafeAreaPadding = new Thickness(10, 0, 10, 34);
        window.UpdateLayout();

        var root = bar.Bar.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Root");
        var body = bar.Bar.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_Body");
        Assert.Equal(new Thickness(10, 0, 10, 34), root.Padding);
        Assert.Equal(25, body.Bounds.Height, 0.5);
        Assert.Equal(25 + 34, bar.Bar.Bounds.Height, 0.5);
        Assert.Equal(window.Bounds.Height, bar.Bar.Bounds.Bottom, 0.5);

        var invoke = bar.Bar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_InvokeButton");
        var invokeRect = invoke.TranslatePoint(default, window)!.Value;
        Assert.True(invokeRect.Y + invoke.Bounds.Height <= window.Bounds.Height - 34 + 0.5, "ellipsis sits above the home indicator");
        Assert.True(invokeRect.X + invoke.Bounds.Width <= window.Bounds.Width - 10 + 0.5, "ellipsis sits inside the horizontal inset");

        bar.Open();
        window.UpdateLayout();
        var panel = bar.Bar.GetVisualDescendants().OfType<AppBarCommandsPanel>().Single();
        var panelBottom = panel.TranslatePoint(new Point(0, panel.Bounds.Height), window)!.Value.Y;
        Assert.True(panelBottom <= window.Bounds.Height - 34 + 0.5, "commands sit above the home indicator");
        window.Close();
    }

    [AvaloniaFact]
    public void Ignore_Safe_Area_Turns_The_Host_Inset_Off()
    {
        var (window, bar) = Make();
        Assert.True(bar.Host.RespectsSafeArea);
        bar.IgnoreSafeArea = true;
        Assert.False(bar.Host.RespectsSafeArea);
        Assert.Equal(default, bar.Host.SafeAreaPadding);
        window.Close();
    }

    [Fact]
    public void Inset_For_Edge_Keeps_The_Docked_Edge_And_Its_Neighbours()
    {
        var safe = new Thickness(1, 2, 3, 4);
        Assert.Equal(new Thickness(1, 0, 3, 4), AvaWin.Controls.Primitives.EdgeOverlayHost.InsetForEdge(safe, Dock.Bottom));
        Assert.Equal(new Thickness(1, 2, 3, 0), AvaWin.Controls.Primitives.EdgeOverlayHost.InsetForEdge(safe, Dock.Top));
        Assert.Equal(new Thickness(1, 2, 0, 4), AvaWin.Controls.Primitives.EdgeOverlayHost.InsetForEdge(safe, Dock.Left));
        Assert.Equal(new Thickness(0, 2, 3, 4), AvaWin.Controls.Primitives.EdgeOverlayHost.InsetForEdge(safe, Dock.Right));
    }

    [AvaloniaFact]
    public void Invoke_Button_Opens_The_Bar()
    {
        var (window, bar) = Make();
        window.UpdateLayout();
        var invoke = bar.Bar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_InvokeButton");
        var p = invoke.TranslatePoint(new Point(invoke.Bounds.Width / 2, invoke.Bounds.Height / 2), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Assert.True(bar.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Reduced_Mode_Engages_When_Commands_Do_Not_Fit()
    {
        static AppBar MakeBar()
        {
            var bar = new AppBar();
            for (var i = 0; i < 12; i++)
            {
                bar.Commands.Add(new AppBarCommand { Label = "Cmd " + i, Icon = AppBarIcon.Add });
            }

            return bar;
        }

        var wide = MakeBar();
        var wideWindow = ThemeTestHelpers.Host(new Grid { Children = { wide } }, "Light", Platform.Desktop, width: 1400);
        wide.Open();
        wideWindow.UpdateLayout();
        Assert.False(wide.IsReduced);
        Assert.DoesNotContain(":reduced", wide.Commands[0].Classes);
        Assert.True(wide.Commands[0].GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Label").IsVisible);
        wideWindow.Close();

        var narrow = MakeBar();
        var narrowWindow = ThemeTestHelpers.Host(new Grid { Children = { narrow } }, "Light", Platform.Desktop, width: 500);
        narrow.Open();
        narrowWindow.UpdateLayout();
        Assert.True(narrow.IsReduced);
        Assert.Contains(":reduced", narrow.Commands[0].Classes);
        Assert.Contains(":reduced", narrow.Bar.Classes);
        Assert.False(narrow.Commands[0].GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Label").IsVisible);
        Assert.True(narrow.Bar.Bounds.Height >= 60);
        narrowWindow.Close();
    }

    [AvaloniaFact]
    public void Show_Hide_ShowOnly_Toggle_Visibility()
    {
        var (window, bar) = Make();
        var add = bar.GetCommandById("add")!;
        var del = bar.GetCommandById("del")!;
        bar.HideCommands(add);
        Assert.False(add.IsVisible);
        bar.ShowCommands(add);
        Assert.True(add.IsVisible);
        bar.ShowOnlyCommands(del);
        Assert.False(add.IsVisible);
        Assert.True(del.IsVisible);
        Assert.Null(bar.GetCommandById("nope"));
        window.Close();
    }

    [AvaloniaFact]
    public void Toggle_Command_Flips_IsSelected_And_Flyout_Command_Opens_Flyout()
    {
        var toggle = new AppBarCommand { Type = AppBarCommandType.Toggle, Label = "T", Icon = AppBarIcon.Favorite };
        var flyout = new Flyout { Content = new TextBlock { Text = "fly" } };
        var fly = new AppBarCommand { Type = AppBarCommandType.Flyout, Label = "F", Icon = AppBarIcon.More, Flyout = flyout };
        var window = ThemeTestHelpers.Host(new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { toggle, fly } }, "Light", Platform.Desktop);
        Click(window, toggle);
        Assert.True(toggle.IsSelected);
        Assert.Contains(":selected", toggle.Classes);
        Click(window, toggle);
        Assert.False(toggle.IsSelected);
        Click(window, fly);
        Assert.True(flyout.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Custom_Layout_Shows_Content()
    {
        var bar = new AppBar { Layout = AppBarLayout.Custom, Content = new TextBlock { Text = "custom", Name = "Custom" } };
        var window = ThemeTestHelpers.Host(new Grid { Children = { bar } }, "Light", Platform.Desktop);
        bar.Open();
        window.UpdateLayout();
        var text = bar.Bar.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "Custom");
        Assert.True(text.IsEffectivelyVisible);
        Assert.False(bar.Bar.GetVisualDescendants().OfType<AppBarCommandsPanel>().Single().IsVisible);
        window.Close();
    }

    internal static void Click(Window window, Control control)
    {
        var p = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
    }
}
