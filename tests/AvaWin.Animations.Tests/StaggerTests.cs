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

public class StaggerTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 83)]
    [InlineData(2, 166)]
    [InlineData(3, 249)]
    [InlineData(4, 332)]
    [InlineData(5, 333)]
    [InlineData(6, 333)]
    [InlineData(50, 333)]
    public void EnterPage_Stagger_Matches_WinJS(int index, double expectedMs)
    {
        Assert.Equal(expectedMs, AnimationRunner.StaggerMs(index, 0, 83, 1, 333));
    }

    [Fact]
    public void Stagger_Without_Cap_Grows_Linearly()
    {
        Assert.Equal(0, AnimationRunner.StaggerMs(0, 0, 50, 1, null));
        Assert.Equal(150, AnimationRunner.StaggerMs(3, 0, 50, 1, null));
    }

    [Fact]
    public void Stagger_Applies_Factor()
    {
        // The factor applies before the add: 0 +5 +2.5 +1.25 = 8.75
        Assert.Equal(8.75, AnimationRunner.StaggerMs(3, 0, 10, 0.5, null));
    }

    [Fact]
    public void TimeScale_Scales_Durations()
    {
        var old = WinAnimations.TimeScale;
        try
        {
            WinAnimations.TimeScale = 2;
            Assert.Equal(TimeSpan.FromMilliseconds(200), AnimationRunner.Ms(100));
        }
        finally
        {
            WinAnimations.TimeScale = old;
        }
    }
}
