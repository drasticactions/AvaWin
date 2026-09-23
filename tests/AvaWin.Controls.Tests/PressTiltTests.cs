using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using AvaWin.Animations;
using Xunit;

namespace AvaWin.Controls.Tests;

public class PressTiltTests
{
    private static (Window window, ItemContainer item) Make(PressFeedback feedback)
    {
        var item = new ItemContainer
        {
            Content = "Tile",
            PressFeedback = feedback,
            Width = 160,
            Height = 160,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var window = ThemeTestHelpers.Host(new Grid { Children = { item } }, "Dark", Platform.Desktop, 400, 400);
        return (window, item);
    }

    private static Point Screen(Control item, Point local)
    {
        var size = item.Bounds.Size;
        var origin = item.RenderTransformOrigin.ToPixels(size);
        var m = Matrix.CreateTranslation(-origin.X, -origin.Y) * item.RenderTransform!.Value * Matrix.CreateTranslation(origin.X, origin.Y);
        return m.Transform(local);
    }

    private static double Reach(Control item, Point corner)
    {
        var centre = new Point(item.Bounds.Width / 2, item.Bounds.Height / 2);
        var moved = Screen(item, corner);
        return Point.Distance(moved, centre) / Point.Distance(corner, centre);
    }

    [AvaloniaTheory]
    [InlineData(0.01, 0.01, 0)]
    [InlineData(0.99, 0.01, 1)]
    [InlineData(0.01, 0.99, 2)]
    [InlineData(0.99, 0.99, 3)]
    public void The_Corner_Nearest_The_Contact_Recedes_Furthest(double fx, double fy, int nearest)
    {
        var (window, item) = Make(PressFeedback.Tilt);
        var p = item.TranslatePoint(new Point(160 * fx, 160 * fy), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        Assert.IsType<TransformGroup>(item.RenderTransform);
        var corners = new[] { new Point(0, 0), new Point(160, 0), new Point(0, 160), new Point(160, 160) };
        var reach = corners.Select(c => Reach(item, c)).ToArray();
        var least = Array.IndexOf(reach, reach.Min());
        Assert.Equal(nearest, least);
        Assert.InRange(reach[nearest], 1 - WinAnimations.PointerTiltMaxRecess - 0.01, 1 - WinAnimations.PointerTiltMaxRecess + 0.01);
        Assert.Equal(3 - nearest, Array.IndexOf(reach, reach.Max()));
        window.MouseUp(p, MouseButton.Left);
        window.Close();
    }

    [AvaloniaFact]
    public void The_Tilt_Follows_The_Contact_While_Pressed()
    {
        var (window, item) = Make(PressFeedback.Tilt);
        var left = item.TranslatePoint(new Point(10, 80), window)!.Value;
        var right = item.TranslatePoint(new Point(150, 80), window)!.Value;
        window.MouseDown(left, MouseButton.Left);
        var rotate = ((TransformGroup)item.RenderTransform!).Children.OfType<Rotate3DTransform>().Single();
        Assert.True(rotate.AngleY < 0);
        window.MouseMove(right, RawInputModifiers.LeftMouseButton);
        Assert.True(rotate.AngleY > 0);
        window.MouseUp(right, MouseButton.Left);
        window.Close();
    }

    [AvaloniaFact]
    public void Release_Restores_Identity()
    {
        var (window, item) = Make(PressFeedback.Tilt);
        var p = item.TranslatePoint(new Point(20, 20), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(item.RenderTransform is TransformOperations { IsIdentity: true });
        Assert.Equal(RelativePoint.Center, item.RenderTransformOrigin);
        Assert.NotNull(item.Transitions);
        window.Close();
    }

    [AvaloniaFact]
    public void The_Default_Is_The_Scale()
    {
        var (window, item) = Make(PressFeedback.Scale);
        Assert.Equal(PressFeedback.Scale, new ItemContainer().PressFeedback);
        var p = item.TranslatePoint(new Point(20, 20), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        Assert.Contains(":pressed", item.Classes);
        Assert.IsType<TransformOperations>(item.RenderTransform);
        window.MouseUp(p, MouseButton.Left);
        window.Close();
    }

    [AvaloniaFact]
    public void None_Does_Not_Move()
    {
        var (window, item) = Make(PressFeedback.None);
        var p = item.TranslatePoint(new Point(20, 20), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        Assert.Contains(":pressed", item.Classes);
        Assert.True(item.RenderTransform is TransformOperations { IsIdentity: true });
        window.MouseUp(p, MouseButton.Left);
        window.Close();
    }

    [AvaloniaFact]
    public void A_ListView_Sets_It_For_Its_Items()
    {
        var list = new ListView
        {
            ItemsSource = new ObservableCollection<string>(["a", "b", "c"]),
            PressFeedback = PressFeedback.Tilt,
            Width = 300,
            Height = 300,
        };
        list.ContentAnimating += (_, e) => e.Cancel = true;
        var window = ThemeTestHelpers.Host(new Grid { Children = { list } }, "Dark", Platform.Desktop);
        Dispatcher.UIThread.RunJobs();
        var item = (ListViewItem)list.ContainerFromIndex(1)!;
        Assert.Equal(PressFeedback.Tilt, item.PressFeedback);
        var p = item.TranslatePoint(new Point(10, 10), window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        Assert.IsType<TransformGroup>(item.RenderTransform);
        window.MouseUp(p, MouseButton.Left);
        window.Close();
    }
}
