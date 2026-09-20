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

public class PivotTests
{
    private static (Window window, Pivot pivot) Make(int count = 4, bool locked = false)
    {
        var pivot = new Pivot { Title = "PIVOT", IsLocked = locked };
        for (var i = 0; i < count; i++)
        {
            pivot.Items.Add(new PivotItem { Header = "item" + i, Content = new TextBlock { Text = "content " + i, Name = "Content" + i } });
        }

        var window = ThemeTestHelpers.Host(new Grid { Children = { pivot } }, "Light", Platform.Desktop, 900, 500);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, pivot);
    }

    private static List<Button> Headers(Pivot p) => p.GetVisualDescendants().OfType<PivotHeadersPanel>().Single().Children.OfType<Button>().ToList();

    [AvaloniaFact]
    public void Selects_First_And_Shows_Its_Content()
    {
        var (window, pivot) = Make();
        Assert.Equal(0, pivot.SelectedIndex);
        var headers = Headers(pivot);
        Assert.Equal(4, headers.Count);
        Assert.Contains("selected", headers[0].Classes);
        Assert.Equal(1, headers[0].Opacity);
        Assert.Equal(0.2, headers[1].Opacity, 0.01);
        Assert.NotNull(pivot.GetVisualDescendants().OfType<TextBlock>().SingleOrDefault(t => t.Name == "Content0"));
        Assert.Null(pivot.GetVisualDescendants().OfType<TextBlock>().SingleOrDefault(t => t.Name == "Content1"));
        Assert.True(pivot.SelectedPivotItem!.IsSelected);
        window.Close();
    }

    [AvaloniaFact]
    public void Header_Click_Selects_And_Rotates_Track()
    {
        var (window, pivot) = Make();
        var events = new List<(int, string?)>();
        pivot.PivotSelectionChanged += (_, e) => events.Add((e.Index, e.Item?.Header as string));
        var headers = Headers(pivot);
        AppBarTests.Click(window, headers[2]);
        window.UpdateLayout();
        Assert.Equal(2, pivot.SelectedIndex);
        Assert.Equal([(2, "item2")], events);
        headers = Headers(pivot);
        Assert.Equal(0, headers[2].Bounds.X, 0.5);
        Assert.True(headers[3].Bounds.X > headers[2].Bounds.X);
        Assert.True(headers[0].Bounds.X > headers[3].Bounds.X, "earlier headers wrap round to the end");
        Assert.NotNull(pivot.GetVisualDescendants().OfType<TextBlock>().SingleOrDefault(t => t.Name == "Content2"));
        window.Close();
    }

    [AvaloniaFact]
    public void Keys_And_Wrap_Around()
    {
        var (window, pivot) = Make(3);
        var headers = Headers(pivot);
        headers[0].Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Assert.Equal(2, pivot.SelectedIndex);
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Equal(0, pivot.SelectedIndex);
        window.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None);
        Assert.Equal(2, pivot.SelectedIndex);
        window.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.None);
        Assert.Equal(0, pivot.SelectedIndex);
        pivot.SelectNext();
        pivot.SelectNext();
        pivot.SelectNext();
        Assert.Equal(0, pivot.SelectedIndex);
        pivot.SelectPrevious();
        Assert.Equal(2, pivot.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void Locked_Hides_Other_Headers_And_Ignores_Swipe()
    {
        var (window, pivot) = Make(3, locked: true);
        Assert.Contains(":locked", pivot.Classes);
        var headers = Headers(pivot);
        Assert.True(headers[0].IsVisible);
        Assert.False(headers[1].IsVisible);
        var viewport = pivot.GetVisualDescendants().OfType<Panel>().Single(p => p.Name == "PART_Viewport");
        var p = viewport.TranslatePoint(new Point(300, 100), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        window.MouseMove(p - new Vector(120, 0));
        window.MouseUp(p - new Vector(120, 0), MouseButton.Left);
        Assert.Equal(0, pivot.SelectedIndex);

        pivot.IsLocked = false;
        window.UpdateLayout();
        Assert.True(Headers(pivot)[1].IsVisible);
        window.MouseDown(p, MouseButton.Left);
        window.MouseMove(p - new Vector(120, 0));
        window.MouseUp(p - new Vector(120, 0), MouseButton.Left);
        Assert.Equal(1, pivot.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Rapid_Selection_Changes_Cancel_The_Previous_Transition()
    {
        var (window, pivot) = Make(5);
        AvaWin.Animations.WinAnimations.TimeScale = 1;
        try
        {
            var starts = 0;
            var ends = 0;
            pivot.ItemAnimationStart += (_, _) => starts++;
            pivot.ItemAnimationEnd += (_, _) => ends++;
            // Click through three headers faster than any transition can finish.
            pivot.SelectNext();
            pivot.SelectNext();
            pivot.SelectNext();
            Assert.Equal(3, pivot.SelectedIndex);
            Assert.Equal(3, starts);
            Assert.Contains(":animating", pivot.Classes);

            for (var i = 0; i < 120 && ends == 0; i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                await Task.Delay(16);
            }

            // Only the last transition runs to completion; the canceled ones never report an end.
            Assert.Equal(1, ends);
            Assert.DoesNotContain(":animating", pivot.Classes);
            var shown = pivot.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name?.StartsWith("Content") == true && t.IsEffectivelyVisible);
            Assert.Equal("Content3", shown.Name);
            Assert.Null(pivot.SelectedPivotItem!.RenderTransform);
            Assert.Equal(1, pivot.SelectedPivotItem.Opacity);
            var headers = pivot.GetVisualDescendants().OfType<PivotHeadersPanel>().Single();
            Assert.Null(headers.RenderTransform);
            Assert.Equal(0, Headers(pivot)[3].Bounds.X, 0.5);
        }
        finally
        {
            AvaWin.Animations.WinAnimations.TimeScale = 0;
        }

        window.Close();
    }

    [AvaloniaFact]
    public void Data_Items_Become_PivotItems()
    {
        var pivot = new Pivot { ItemsSource = new[] { "alpha", "beta" } };
        var window = ThemeTestHelpers.Host(new Grid { Children = { pivot } }, "Light", Platform.Desktop);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(2, Headers(pivot).Count);
        Assert.Equal("alpha", pivot.SelectedPivotItem!.Header);
        Assert.Equal("alpha", pivot.SelectedItem);
        window.Close();
    }
}
