using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using AvaWin.Animations;
using Xunit;

namespace AvaWin.Animations.Tests;

public class PageAnimationTests
{
    private static (Window window, Border tile) Host()
    {
        var tile = new Border { Width = 100, Height = 60 };
        var window = new Window { Content = new Grid { Children = { tile } }, Width = 400, Height = 300 };
        window.Show();
        window.UpdateLayout();
        return (window, tile);
    }

    [AvaloniaFact]
    public void Every_Turnstile_Ends_Clean_Like_WinJS()
    {
        // Every turnstile, in or out, resets the transform, the origin and the opacity when it is done.
        WinAnimations.TimeScale = 0;
        var (_, tile) = Host();
        foreach (var run in new Func<Control, Task>[] { WinAnimations.TurnstileForwardIn, WinAnimations.TurnstileForwardOut, WinAnimations.TurnstileBackwardIn, WinAnimations.TurnstileBackwardOut })
        {
            Assert.True(run(tile).IsCompletedSuccessfully);
            Assert.Null(tile.RenderTransform);
            Assert.Equal(1, tile.Opacity);
        }
    }

    [AvaloniaFact]
    public async Task Turnstile_Live_Run_Completes()
    {
        WinAnimations.TimeScale = 1;
        try
        {
            var (_, tile) = Host();
            var task = WinAnimations.TurnstileForwardOut(tile);
            for (var i = 0; i < 60 && !task.IsCompleted; i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                await Task.Delay(16);
            }

            await task;
            Assert.Null(tile.RenderTransform);
            Assert.Equal(1, tile.Opacity);
        }
        finally
        {
            WinAnimations.TimeScale = 0;
        }
    }

    [AvaloniaFact]
    public void Slides_Move_Page_And_Groups()
    {
        WinAnimations.TimeScale = 0;
        var (window, tile) = Host();
        var a = new Border { Width = 10, Height = 10 };
        var b = new Border { Width = 10, Height = 10 };
        ((Grid)window.Content!).Children.Add(a);
        ((Grid)window.Content!).Children.Add(b);
        window.UpdateLayout();
        Assert.True(WinAnimations.SlideRightIn(tile, new[] { a }, new[] { b }).IsCompletedSuccessfully);
        Assert.Null(tile.RenderTransform);
        Assert.Null(a.RenderTransform);
        WinAnimations.SlideLeftOut(tile, new[] { a }, new[] { b });
        Assert.Equal(-400, ((TransformOperations)tile.RenderTransform!).Value.M31);
        Assert.Equal(-200, ((TransformOperations)a.RenderTransform!).Value.M31);
        Assert.Equal(-400, ((TransformOperations)b.RenderTransform!).Value.M31);
        Assert.Equal(0, tile.Opacity);
        WinAnimations.SlideRightOut(tile);
        Assert.Equal(400, ((TransformOperations)tile.RenderTransform!).Value.M31);
    }

    [AvaloniaFact]
    public void Continuum_Forward_And_Backward_End_States()
    {
        WinAnimations.TimeScale = 0;
        var (window, page) = Host();
        var root = new Border { Width = 50, Height = 20 };
        var content = new Border { Width = 50, Height = 20 };
        ((Grid)window.Content!).Children.Add(root);
        ((Grid)window.Content!).Children.Add(content);
        window.UpdateLayout();
        Assert.True(WinAnimations.ContinuumForwardIn(page, root, content).IsCompletedSuccessfully);
        Assert.Null(page.RenderTransform);
        Assert.Null(root.RenderTransform);
        Assert.Null(content.RenderTransform);
        WinAnimations.ContinuumForwardOut(page, root);
        var g = Assert.IsType<Avalonia.Media.TransformGroup>(page.RenderTransform);
        Assert.Equal(1.1, g.Children.OfType<Avalonia.Media.ScaleTransform>().Single().ScaleX, 3);
        Assert.Equal(0, page.Opacity);
        var item = (Avalonia.Media.TransformGroup)root.RenderTransform!;
        Assert.Equal(80, item.Children.OfType<Avalonia.Media.Rotate3DTransform>().Single().AngleX);
        Assert.Equal(150, item.Children.OfType<Avalonia.Media.TranslateTransform>().Single().Y);
        WinAnimations.ContinuumBackwardIn(page, root);
        Assert.Null(page.RenderTransform);
        Assert.Equal(1, page.Opacity);
        WinAnimations.ContinuumBackwardOut(page);
        Assert.Equal(0.5, ((Avalonia.Media.TransformGroup)page.RenderTransform!).Children.OfType<Avalonia.Media.ScaleTransform>().Single().ScaleY, 3);
    }

    [AvaloniaFact]
    public void PageNavigationAnimations_Follow_WinJS_Rules()
    {
        static string Name(Func<System.Collections.Generic.IEnumerable<Control>, Task> f) => f.Method.Name;
        var desktop = WinAnimations.CreatePageNavigationAnimations(PageNavigation.Turnstile, PageNavigation.Turnstile, false);
        Assert.Equal("EnterPageDefault", Name(desktop.Entrance));
        var slideForward = WinAnimations.CreatePageNavigationAnimations(null, PageNavigation.Slide, false, isPhone: true);
        Assert.Equal("SlideUp", Name(slideForward.Entrance));
        var slideBack = WinAnimations.CreatePageNavigationAnimations(PageNavigation.Slide, PageNavigation.Turnstile, true, isPhone: true);
        Assert.Equal("SlideDown", Name(slideBack.Exit));
        var turnstileBack = WinAnimations.CreatePageNavigationAnimations(null, null, true, isPhone: true);
        Assert.Equal("TurnstileBackwardIn", Name(turnstileBack.Entrance));
        Assert.Equal("TurnstileBackwardOut", Name(turnstileBack.Exit));
    }

    [AvaloniaFact]
    public async Task WinPageTransition_Swaps_Visibility()
    {
        WinAnimations.TimeScale = 0;
        var (window, from) = Host();
        var to = new Border { Width = 100, Height = 60, IsVisible = false };
        ((Grid)window.Content!).Children.Add(to);
        window.UpdateLayout();
        await new WinPageTransition().Start(from, to, true, System.Threading.CancellationToken.None);
        Assert.False(from.IsVisible);
        Assert.True(to.IsVisible);
        Assert.Null(to.RenderTransform);
        Assert.Equal(1, to.Opacity);
        await new WinPageTransition(PageNavigation.Turnstile).Start(to, from, false, System.Threading.CancellationToken.None);
        Assert.True(from.IsVisible);
        Assert.False(to.IsVisible);
        Assert.Equal(1, from.Opacity);
    }

    [AvaloniaFact]
    public async Task Continuum_Transition_Leaves_The_Page_Clean()
    {
        // One element plays only the page role: no item animations fight it, no transform is left behind.
        WinAnimations.TimeScale = 0;
        var (window, from) = Host();
        var to = new Border { Width = 100, Height = 60, IsVisible = false };
        ((Grid)window.Content!).Children.Add(to);
        window.UpdateLayout();
        await new WinPageTransition(PageNavigation.Continuum).Start(from, to, true, System.Threading.CancellationToken.None);
        Assert.True(to.IsVisible);
        Assert.Equal(1, to.Opacity);
        Assert.Null(to.RenderTransform);
        await new WinPageTransition(PageNavigation.Continuum).Start(to, from, false, System.Threading.CancellationToken.None);
        Assert.True(from.IsVisible);
        Assert.Equal(1, from.Opacity);
        Assert.Null(from.RenderTransform);
        window.Close();
    }

    [AvaloniaFact]
    public async Task WinPageTransition_Hides_The_Incoming_Page_Until_The_Exit_Is_Done()
    {
        WinAnimations.TimeScale = 1;
        WinAnimations.IsEnabled = true;
        var (window, from) = Host();
        var to = new Border { Width = 100, Height = 60 };
        ((Grid)window.Content!).Children.Add(to);
        window.UpdateLayout();
        var run = new WinPageTransition().Start(from, to, true, System.Threading.CancellationToken.None);
        // While the outgoing page is still fading, the incoming one must not be drawn over it.
        Assert.Equal(0, to.Opacity);
        await run;
        Assert.Equal(1, to.Opacity);
        Assert.False(from.IsVisible);
        WinAnimations.TimeScale = 0;
    }

    [AvaloniaFact]
    public async Task Expand_And_Collapse_Run()
    {
        WinAnimations.TimeScale = 0;
        var panel = new StackPanel();
        var a = new Border { Height = 20 };
        var hidden = new Border { Height = 20, IsVisible = false };
        var b = new Border { Height = 20 };
        panel.Children.Add(a);
        panel.Children.Add(hidden);
        panel.Children.Add(b);
        var window = new Window { Content = panel, Width = 300, Height = 300 };
        window.Show();
        window.UpdateLayout();
        var expand = WinAnimations.CreateExpandAnimation(hidden, new[] { b });
        hidden.IsVisible = true;
        await expand.ExecuteAsync();
        Assert.Equal(1, hidden.Opacity);
        Assert.Null(b.RenderTransform);
        var collapse = WinAnimations.CreateCollapseAnimation(hidden, new[] { b });
        await collapse.ExecuteAsync();
        Assert.Equal(0, hidden.Opacity);
    }
}
