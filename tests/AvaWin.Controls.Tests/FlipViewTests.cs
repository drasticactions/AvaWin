using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Controls.Tests;

public class FlipViewTests
{
    private static (Window window, FlipView flip) Make(Orientation orientation = Orientation.Horizontal)
    {
        var flip = new FlipView { ItemsSource = new[] { "one", "two", "three" }, Orientation = orientation, Width = 400, Height = 300 };
        var window = ThemeTestHelpers.Host(new Grid { Children = { flip } }, "Light", Platform.Desktop, 500, 400);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, flip);
    }

    [AvaloniaFact]
    public async Task Next_And_Previous_Stop_At_The_Ends()
    {
        var (window, flip) = Make();
        var selected = 0;
        var visibility = new List<(bool, int)>();
        flip.PageSelected += (_, _) => selected++;
        flip.PageVisibilityChanged += (_, e) => visibility.Add((e.Visible, e.Index));
        Assert.Equal(0, flip.CurrentPage);
        Assert.Equal(3, flip.Count);
        Assert.Contains(":cannext", flip.Classes);
        Assert.DoesNotContain(":canprevious", flip.Classes);
        Assert.False(await flip.PreviousAsync());
        Assert.True(await flip.NextAsync());
        Assert.Equal(1, flip.CurrentPage);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(await flip.NextAsync());
        Assert.False(await flip.NextAsync());
        Assert.Equal(2, flip.CurrentPage);
        Assert.DoesNotContain(":cannext", flip.Classes);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, selected);
        Assert.Contains((true, 2), visibility);
        window.UpdateLayout();
        var text = flip.GetVisualDescendants().OfType<TextBlock>().Single(t => t.IsEffectivelyVisible);
        Assert.Equal("three", text.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Nav_Buttons_Follow_Orientation_And_Availability()
    {
        var (window, flip) = Make(Orientation.Vertical);
        Assert.Contains(":vertical", flip.Classes);
        var next = flip.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_NextButton");
        var prev = flip.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_PreviousButton");
        Assert.Equal("", next.Content);
        Assert.Equal("", prev.Content);
        var p = flip.TranslatePoint(new Point(200, 150), window)!.Value;
        window.MouseMove(p);
        Assert.True(next.IsVisible);
        Assert.False(prev.IsVisible, "no previous page at the start");
        AppBarTests.Click(window, next);
        Assert.Equal(1, flip.CurrentPage);
        Assert.True(prev.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Swipe_And_Keys_Change_Page()
    {
        var (window, flip) = Make();
        var p = flip.TranslatePoint(new Point(300, 150), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        window.MouseMove(p - new Vector(120, 0), RawInputModifiers.LeftMouseButton);
        window.MouseUp(p - new Vector(120, 0), MouseButton.Left);
        Assert.Equal(1, flip.CurrentPage);
        flip.Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Assert.Equal(0, flip.CurrentPage);
        window.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None);
        Assert.Equal(2, flip.CurrentPage);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Custom_Animations_Are_Used()
    {
        var (window, flip) = Make();
        var calls = new List<string>();
        flip.SetCustomAnimations(new FlipViewAnimations(
            Next: (_, _, _) => { calls.Add("next"); return Task.CompletedTask; },
            Previous: (_, _, _) => { calls.Add("prev"); return Task.CompletedTask; },
            Jump: (_, _, _) => { calls.Add("jump"); return Task.CompletedTask; }));
        AvaWin.Animations.WinAnimations.TimeScale = 1;
        await flip.NextAsync();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        flip.CurrentPage = 0;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        flip.CurrentPage = 2;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(["next", "prev", "jump"], calls);
        AvaWin.Animations.WinAnimations.TimeScale = 0;
        window.Close();
    }

    /// <summary>A second move during the slide cancels it: one page ends up visible, at rest, with paired visibility events.</summary>
    [AvaloniaFact]
    public async Task Quick_Moves_Cancel_The_Running_Slide()
    {
        var (window, flip) = Make();
        var visibility = new List<(bool, int)>();
        flip.PageVisibilityChanged += (_, e) => visibility.Add((e.Visible, e.Index));
        AvaWin.Animations.WinAnimations.TimeScale = 1;
        try
        {
            await flip.NextAsync();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            await Task.Delay(50);
            Assert.Contains(":animating", flip.Classes);
            await flip.NextAsync();
            var completed = 0;
            flip.PageCompleted += (_, _) => completed++;
            for (var i = 0; i < 120 && flip.Classes.Contains(":animating"); i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                await Task.Delay(16);
            }

            window.UpdateLayout();
            Assert.Equal(2, flip.CurrentPage);
            Assert.Equal(1, completed);
            var pages = flip.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().Where(p => p.Name is "PART_PageA" or "PART_PageB").ToList();
            var shown = Assert.Single(pages, p => p.IsVisible);
            Assert.Equal("three", (shown.Child as TextBlock)?.Text);
            Assert.Null(shown.RenderTransform);
            Assert.Equal(1, shown.Opacity);
            Assert.Equal([(true, 1), (false, 0), (true, 2), (false, 1)], visibility);
        }
        finally
        {
            AvaWin.Animations.WinAnimations.TimeScale = 0;
            window.Close();
        }
    }

    /// <summary>A custom animation that throws on its token when interrupted is a canceled slide, not an error.</summary>
    [AvaloniaFact]
    public async Task Interrupted_Custom_Animation_Cancels_Cleanly()
    {
        var (window, flip) = Make();
        var canceled = 0;
        flip.SetCustomAnimations(new FlipViewAnimations(Next: async (_, _, token) =>
        {
            try
            {
                await Task.Delay(10_000, token);
            }
            catch (OperationCanceledException)
            {
                canceled++;
                throw;
            }
        }));
        AvaWin.Animations.WinAnimations.TimeScale = 1;
        try
        {
            await flip.NextAsync();
            Assert.Contains(":animating", flip.Classes);
            await flip.NextAsync();
            for (var i = 0; i < 120 && flip.Classes.Contains(":animating"); i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                await Task.Delay(16);
            }

            Assert.Equal(1, canceled);
            Assert.Contains(":animating", flip.Classes);
            flip.CurrentPage = 0;
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, canceled);
            Assert.Equal(0, flip.CurrentPage);
        }
        finally
        {
            AvaWin.Animations.WinAnimations.TimeScale = 0;
            window.Close();
        }
    }

    private sealed class Model
    {
        public int Page { get; set; }
    }

    /// <summary>PageSelected fires once the page has settled, after a two-way SelectedIndex binding has updated its source.</summary>
    [AvaloniaFact]
    public async Task PageSelected_Fires_After_The_Binding_Source_Is_Updated()
    {
        var model = new Model();
        var flip = new FlipView { ItemsSource = new[] { "one", "two", "three" }, Width = 400, Height = 300, DataContext = model };
        flip.Bind(FlipView.SelectedIndexProperty, new Avalonia.Data.Binding(nameof(Model.Page)) { Mode = Avalonia.Data.BindingMode.TwoWay });
        var window = ThemeTestHelpers.Host(new Grid { Children = { flip } }, "Light", Platform.Desktop, 500, 400);
        var seen = new List<int>();
        flip.PageSelected += (_, _) => seen.Add(model.Page);
        await flip.NextAsync();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        await flip.NextAsync();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal([1, 2], seen);
        window.Close();
    }
}
