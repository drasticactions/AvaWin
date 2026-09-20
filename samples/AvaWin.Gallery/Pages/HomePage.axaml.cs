using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaWin.Controls;
using AvaWin.Gallery.Framework;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery.Pages;

public partial class HomePage : UserControl
{
    /// <summary>The horizontal panorama never gets shorter than this; otherwise it fills the page viewport.</summary>
    private const double MinPanoramaHeight = 400;

    private readonly List<ListView> _tiles = new();
    private ScrollViewer? _pageScroller;
    private bool _narrow;

    public HomePage() : this(_ => { })
    {
    }

    public HomePage(Action<GalleryPage> navigate)
    {
        InitializeComponent();
        IntroImage.Source = SampleData.Photos[0].Image;
        foreach (var group in GalleryPages.Groups)
        {
            var pages = GalleryPages.All.Where(p => p.Group == group).ToList();
            if (pages.Count == 0)
            {
                continue;
            }

            var tiles = new ListView { ItemsSource = pages, Classes = { "tiles" } };
            tiles.ItemInvoked += (_, e) => navigate((GalleryPage)e.Item!);
            _tiles.Add(tiles);
            // Static headers: the tiles are the navigation, and a group has no page of its own for a chevron to open.
            HubControl.Items.Add(new HubSection { Header = group, Content = tiles, IsHeaderStatic = true });
        }

        ApplyLayout(narrow: false, force: true);
        SizeChanged += (_, e) => ApplyLayout(e.NewSize.Width < SamplePage.NarrowWidth, force: false);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The page sits in a ScrollViewer, so the Hub sees infinite height; size the panorama to the viewport instead.
        // The viewport is read after each layout pass, not from a Viewport change, which arrives mid-arrange: a
        // Height set there leaves the page host arranged at its old size.
        _pageScroller = this.FindAncestorOfType<ScrollViewer>();
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
        _pageScroller = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e) => ApplyPanoramaHeight();

    private void ApplyPanoramaHeight()
    {
        if (_narrow)
        {
            HubControl.Height = double.NaN;
            return;
        }

        var available = _pageScroller?.Viewport.Height ?? 0;
        var margin = (_pageScroller?.Content as Control)?.Margin.Bottom ?? 0;
        HubControl.Height = Math.Max(MinPanoramaHeight, available - margin);
        // Tall windows get a third row of tiles.
        var rows = HubControl.Height >= 600 ? 3 : 2;
        foreach (var tiles in _tiles)
        {
            if (tiles.Layout is GridLayout { Orientation: Orientation.Horizontal } grid && grid.MaximumRowsOrColumns != rows)
            {
                tiles.Layout = new GridLayout { Orientation = Orientation.Horizontal, MaximumRowsOrColumns = rows };
            }
        }
    }

    private void ApplyLayout(bool narrow, bool force)
    {
        if (!force && narrow == _narrow)
        {
            return;
        }

        _narrow = narrow;
        HubControl.Orientation = narrow ? Orientation.Vertical : Orientation.Horizontal;
        ApplyPanoramaHeight();
        Intro.MaxWidth = narrow ? double.PositiveInfinity : 420;
        foreach (var tiles in _tiles)
        {
            // Wide: two rows of tiles that extend the panorama sideways. Narrow: a wrapping vertical grid.
            tiles.Layout = narrow
                ? new GridLayout { Orientation = Orientation.Vertical }
                : new GridLayout { Orientation = Orientation.Horizontal, MaximumRowsOrColumns = HubControl.Height >= 600 ? 3 : 2 };
        }
    }
}
