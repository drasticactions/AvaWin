using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvaWin.Animations;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class AnimationsPage : SamplePage
{
    private readonly ChromeViewModel _vm = new();
    private readonly List<Control> _tiles;
    private int _screen;
    private int _row = 5;
    private bool _collapsed;

    public AnimationsPage()
    {
        DataContext = _vm;
        InitializeComponent();
        _vm.Status = "Pick a transition, then Next page.";
        Screens.Content = SampleData.Photos[0];
        Transition.SelectionChanged += (_, _) => Screens.PageTransition = new WinPageTransition(Enum.Parse<PageNavigation>(((ComboBoxItem)Transition.SelectedItem!).Content!.ToString()!));
        Screens.PageTransition = new WinPageTransition();

        _tiles = Enumerable.Range(0, 4).Select(i => (Control)Tile("Tile " + (i + 1), SampleData.Palette[i], 120, 120)).ToList();
        foreach (var t in _tiles)
        {
            Tiles.Children.Add(t);
        }

        var tiles = _tiles;
        Discrete("EnterPage", () => WinAnimations.EnterPage(tiles));
        Discrete("ExitPage", () => WinAnimations.ExitPage(tiles));
        Discrete("EnterContent", () => WinAnimations.EnterContent(tiles));
        Discrete("ExitContent", () => WinAnimations.ExitContent(tiles));
        Discrete("FadeIn", () => WinAnimations.FadeIn(tiles));
        Discrete("FadeOut", () => WinAnimations.FadeOut(tiles));
        Discrete("CrossFade", () => WinAnimations.CrossFade(tiles.Take(2), tiles.Skip(2)));
        Discrete("ShowPopup", () => WinAnimations.ShowPopup(tiles));
        Discrete("HidePopup", () => WinAnimations.HidePopup(tiles));
        Discrete("ShowEdgeUI", () => WinAnimations.ShowEdgeUI(tiles));
        Discrete("HideEdgeUI", () => WinAnimations.HideEdgeUI(tiles));
        Discrete("ShowPanel", () => WinAnimations.ShowPanel(tiles));
        Discrete("HidePanel", () => WinAnimations.HidePanel(tiles));
        Discrete("PointerDown", () => WinAnimations.PointerDown(tiles));
        Discrete("PointerUp", () => WinAnimations.PointerUp(tiles));
        Discrete("DragSourceStart", () => WinAnimations.DragSourceStart(tiles.Take(1), tiles.Skip(1)));
        Discrete("DragSourceEnd", () => WinAnimations.DragSourceEnd(tiles.Take(1), null, tiles.Skip(1)));
        Discrete("DragBetweenEnter", () => WinAnimations.DragBetweenEnter(tiles.Take(2)));
        Discrete("DragBetweenLeave", () => WinAnimations.DragBetweenLeave(tiles.Take(2)));
        Discrete("SwipeSelect", () => WinAnimations.SwipeSelect(tiles.Take(1), tiles.Skip(1).Take(1)));
        Discrete("SwipeDeselect", () => WinAnimations.SwipeDeselect(tiles.Take(1), tiles.Skip(1).Take(1)));
        Discrete("SwipeReveal", () => WinAnimations.SwipeReveal(tiles.Take(1)));
        Discrete("UpdateBadge", () => WinAnimations.UpdateBadge(tiles));
        Discrete("TurnstileForwardIn", () => WinAnimations.TurnstileForwardIn(tiles));
        Discrete("TurnstileForwardOut", () => WinAnimations.TurnstileForwardOut(tiles));
        Discrete("TurnstileBackwardIn", () => WinAnimations.TurnstileBackwardIn(tiles));
        Discrete("TurnstileBackwardOut", () => WinAnimations.TurnstileBackwardOut(tiles));
        Discrete("SlideDown", () => WinAnimations.SlideDown(tiles));
        Discrete("SlideUp", () => WinAnimations.SlideUp(tiles));
        Discrete("SlideRightIn", () => WinAnimations.SlideRightIn(null, tiles.Take(1), tiles.Skip(1).Take(1), tiles.Skip(2)));
        Discrete("SlideRightOut", () => WinAnimations.SlideRightOut(null, tiles.Take(1), tiles.Skip(1).Take(1), tiles.Skip(2)));
        Discrete("SlideLeftIn", () => WinAnimations.SlideLeftIn(null, tiles.Take(1), tiles.Skip(1).Take(1), tiles.Skip(2)));
        Discrete("SlideLeftOut", () => WinAnimations.SlideLeftOut(null, tiles.Take(1), tiles.Skip(1).Take(1), tiles.Skip(2)));
        var reset = new Button { Content = "Reset tiles" };
        reset.Click += (_, _) => Reset(tiles);
        DiscreteButtons.Children.Add(reset);

        var roles = new Control[] { ContinuumPage, ContinuumItem, ContinuumContent };
        Continuum("ContinuumForwardIn (page, item, content)", () => WinAnimations.ContinuumForwardIn(ContinuumPage, ContinuumItem, ContinuumContent));
        Continuum("ContinuumForwardOut (page, item)", () => WinAnimations.ContinuumForwardOut(ContinuumPage, ContinuumItem));
        Continuum("ContinuumBackwardIn (page, item)", () => WinAnimations.ContinuumBackwardIn(ContinuumPage, ContinuumItem));
        Continuum("ContinuumBackwardOut (page)", () => WinAnimations.ContinuumBackwardOut(ContinuumPage));
        var resetRoles = new Button { Content = "Reset" };
        resetRoles.Click += (_, _) => Reset(roles);
        ContinuumButtons.Children.Add(resetRoles);

        for (var i = 0; i < 4; i++)
        {
            Rows.Children.Add(Tile("Row " + (i + 1), SampleData.Palette[i % SampleData.Palette.Length], 300, 36));
        }
    }

    private void Discrete(string name, Func<Task> run)
    {
        var b = new Button { Name = "Anim_" + name, Content = name };
        b.Click += async (_, _) =>
        {
            Reset(_tiles);
            await run();
        };
        DiscreteButtons.Children.Add(b);
    }

    private void Continuum(string label, Func<Task> run)
    {
        var b = new Button { Content = label };
        b.Click += async (_, _) =>
        {
            Reset([ContinuumPage, ContinuumItem, ContinuumContent]);
            await run();
        };
        ContinuumButtons.Children.Add(b);
    }

    private void OnNextPage(object? sender, RoutedEventArgs e)
    {
        _screen = (_screen + 1) % SampleData.Photos.Count;
        Screens.Content = SampleData.Photos[_screen];
        _vm.Status = $"{((ComboBoxItem)Transition.SelectedItem!).Content} → {SampleData.Photos[_screen].Title}";
    }

    private async void OnAddToList(object? sender, RoutedEventArgs e)
    {
        var tile = Tile("Row " + _row++, SampleData.Palette[_row % SampleData.Palette.Length], 300, 36);
        var affected = Rows.Children.Skip(1).ToList();
        var anim = WinAnimations.CreateAddToListAnimation(tile, affected);
        Rows.Children.Insert(1, tile);
        await anim.ExecuteAsync();
    }

    private async void OnDeleteFromList(object? sender, RoutedEventArgs e)
    {
        if (Rows.Children.Count < 2)
        {
            return;
        }

        var tile = Rows.Children[1];
        var anim = WinAnimations.CreateDeleteFromListAnimation(tile, Rows.Children.Skip(2).ToList());
        await anim.ExecuteAsync();
        Rows.Children.Remove(tile);
    }

    private async void OnReposition(object? sender, RoutedEventArgs e)
    {
        var anim = WinAnimations.CreateRepositionAnimation(Rows.Children.ToList());
        Rows.Children.Move(0, Rows.Children.Count - 1);
        await anim.ExecuteAsync();
    }

    private async void OnCollapse(object? sender, RoutedEventArgs e)
    {
        if (Rows.Children.Count < 3)
        {
            return;
        }

        var target = Rows.Children[1];
        var affected = Rows.Children.Skip(2).ToList();
        if (!_collapsed)
        {
            var anim = WinAnimations.CreateCollapseAnimation(target, affected);
            target.IsVisible = false;
            await anim.ExecuteAsync();
        }
        else
        {
            var anim = WinAnimations.CreateExpandAnimation(target, affected);
            target.IsVisible = true;
            await anim.ExecuteAsync();
        }

        _collapsed = !_collapsed;
    }

    private static Border Tile(string text, Color color, double width, double height) => new()
    {
        Width = width,
        Height = height,
        Background = new SolidColorBrush(color),
        Child = new TextBlock { Text = text, Foreground = Brushes.White, Margin = new Thickness(8), VerticalAlignment = VerticalAlignment.Bottom },
    };

    private static void Reset(IEnumerable<Control> controls)
    {
        foreach (var c in controls)
        {
            c.ClearValue(Visual.OpacityProperty);
            c.ClearValue(Visual.RenderTransformProperty);
            c.ClearValue(Visual.RenderTransformOriginProperty);
        }
    }
}
