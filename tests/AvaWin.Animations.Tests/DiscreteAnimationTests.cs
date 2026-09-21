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

public class DiscreteAnimationTests
{
    private static (Window window, Border tile) Host()
    {
        var tile = new Border { Width = 50, Height = 50 };
        var window = new Window { Content = tile, Width = 200, Height = 200 };
        window.Show();
        window.UpdateLayout();
        return (window, tile);
    }

    [AvaloniaFact]
    public void FadeOut_With_TimeScale_Zero_Completes_Synchronously()
    {
        WinAnimations.TimeScale = 0;
        var (_, tile) = Host();
        var task = WinAnimations.FadeOut(tile);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(0, tile.Opacity);
    }

    [AvaloniaFact]
    public void FadeIn_Leaves_Opacity_At_One()
    {
        WinAnimations.TimeScale = 0;
        var (_, tile) = Host();
        tile.Opacity = 0;
        var task = WinAnimations.FadeIn(tile);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, tile.Opacity);
    }

    [AvaloniaFact]
    public void PointerDown_Then_PointerUp_Restores_Transform()
    {
        WinAnimations.TimeScale = 0;
        var (_, tile) = Host();
        WinAnimations.PointerDown(tile);
        var t = Assert.IsType<TransformOperations>(tile.RenderTransform);
        Assert.Equal(new Matrix(0.975, 0, 0, 0.975, 0, 0), t.Value);
        WinAnimations.PointerUp(tile);
        Assert.Null(tile.RenderTransform);
    }

    [AvaloniaFact]
    public void HideEdgeUI_Ends_Translated_And_ShowEdgeUI_Ends_At_Rest()
    {
        WinAnimations.TimeScale = 0;
        var (_, tile) = Host();
        WinAnimations.HideEdgeUI(tile);
        var t = Assert.IsType<TransformOperations>(tile.RenderTransform);
        Assert.Equal(-70, t.Value.M32);
        WinAnimations.ShowEdgeUI(tile);
        Assert.Null(tile.RenderTransform);
    }

    [AvaloniaFact]
    public async Task Live_Transform_Animation_Runs_And_Completes()
    {
        // Avalonia 12.1 has no built-in keyframe animator for ITransform; AnimationRunner registers one.
        WinAnimations.TimeScale = 1;
        var (_, tile) = Host();
        var task = WinAnimations.HideEdgeUI(tile);
        Assert.False(task.IsCompleted);
        for (var i = 0; i < 40 && !task.IsCompleted; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            await Task.Delay(16);
        }

        await task;
        var t = Assert.IsType<TransformOperations>(tile.RenderTransform);
        Assert.Equal(-70, t.Value.M32);
    }

    [AvaloniaFact]
    public async Task New_Run_Supersedes_Live_Run_On_Same_Property()
    {
        // Toggling mid-flight must not stack animations: the earlier run is canceled at once, and the new one
        // resumes from the value on screen rather than replaying from its own start.
        WinAnimations.TimeScale = 1;
        var (_, tile) = Host();
        var hide = WinAnimations.HideEdgeUI(tile);
        for (var i = 0; i < 8; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            await Task.Delay(16);
        }

        var midway = Assert.IsType<TransformOperations>(tile.RenderTransform).Value.M32;
        Assert.True(midway < 0 && midway > -70, $"expected a mid-flight translate, got {midway}");

        var show = WinAnimations.ShowEdgeUI(tile);
        Assert.True(hide.IsCompleted, "the superseded run should finish as soon as the next one starts");
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        await Task.Delay(16);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var resumed = Assert.IsType<TransformOperations>(tile.RenderTransform).Value.M32;
        Assert.True(resumed <= 0 && resumed >= midway - 5, $"expected the show to resume near {midway}, got {resumed}");

        for (var i = 0; i < 40 && !show.IsCompleted; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            await Task.Delay(16);
        }

        await show;
        Assert.Null(tile.RenderTransform);
    }

    [AvaloniaFact]
    public void IsEnabled_False_Short_Circuits()
    {
        WinAnimations.TimeScale = 1;
        WinAnimations.IsEnabled = false;
        try
        {
            var (_, tile) = Host();
            var task = WinAnimations.HidePanel(tile);
            Assert.True(task.IsCompletedSuccessfully);
            var t = Assert.IsType<TransformOperations>(tile.RenderTransform);
            Assert.Equal(364, t.Value.M31);
        }
        finally
        {
            WinAnimations.IsEnabled = true;
        }
    }

    [AvaloniaFact]
    public void HidePanel_Flips_Left_Under_Rtl()
    {
        WinAnimations.TimeScale = 0;
        var (_, tile) = Host();
        tile.FlowDirection = FlowDirection.RightToLeft;
        WinAnimations.HidePanel(tile);
        var t = Assert.IsType<TransformOperations>(tile.RenderTransform);
        Assert.Equal(-364, t.Value.M31);
    }

    [AvaloniaFact]
    public void Detached_Control_Completes_Immediately()
    {
        WinAnimations.TimeScale = 1;
        var tile = new Border();
        var task = WinAnimations.FadeOut(tile);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(0, tile.Opacity);
    }

    [AvaloniaFact]
    public async Task Real_Animation_Completes_And_Cleans_Up()
    {
        WinAnimations.TimeScale = 0.05; // 167 ms -> ~8 ms
        try
        {
            var (_, tile) = Host();
            await WinAnimations.FadeOut(tile).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, tile.Opacity);
        }
        finally
        {
            WinAnimations.TimeScale = 0;
        }
    }
}
