using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using static AvaWin.Animations.AnimationRunner;

namespace AvaWin.Animations;

/// <summary>The exit/entrance pair returned by <see cref="WinAnimations.CreatePageNavigationAnimations"/>.</summary>
/// <param name="Exit">Runs on the elements of the outgoing page.</param>
/// <param name="Entrance">Runs on the elements of the incoming page.</param>
public sealed record PageNavigationAnimations(Func<IEnumerable<Control>, Task> Exit, Func<IEnumerable<Control>, Task> Entrance);

public static partial class WinAnimations
{
    // -------------------------------------------------------------------------------------------------
    // Turnstile: a 3D rotation about an axis 40px left of the window, at the vertical center of the window
    // -------------------------------------------------------------------------------------------------

    /// <summary>RotateY 80° → 0 over 300 ms (perspective 600) and fade in, staggered 50 ms.</summary>
    public static Task TurnstileForwardIn(IEnumerable<Control> incoming) => Turnstile(incoming, 600, 80, 0, 300, WinEasing.TurnstileIn, fadeIn: true);

    /// <summary>RotateY 0 → −50° over 128 ms and fade out, staggered 50 ms. The elements are visible and untransformed when the task completes.</summary>
    public static Task TurnstileForwardOut(IEnumerable<Control> outgoing) => Turnstile(outgoing, 600, 0, -50, 128, WinEasing.TurnstileOut, fadeIn: false);

    /// <summary>RotateY −50° → 0 over 300 ms and fade in.</summary>
    public static Task TurnstileBackwardIn(IEnumerable<Control> incoming) => Turnstile(incoming, 600, -50, 0, 300, WinEasing.TurnstileIn, fadeIn: true);

    /// <summary>RotateY 0 → 80° over 128 ms (perspective 800) and fade out. The elements are visible and untransformed when the task completes.</summary>
    public static Task TurnstileBackwardOut(IEnumerable<Control> outgoing) => Turnstile(outgoing, 800, 0, 80, 128, WinEasing.TurnstileOut, fadeIn: false);

    private static async Task Turnstile(IEnumerable<Control> elements, double depth, double fromAngle, double toAngle, double durationMs, Easing easing, bool fadeIn)
    {
        var list = ToList(elements);
        await WhenAll(list, (c, i) =>
        {
            var delay = StaggerMs(i, 0, 50, 1, 1000);
            var (cx, cy) = TurnstileOrigin(c);
            Prepare3D(c, depth, cx, cy);
            return Run(c,
            [
                Angle(Rotate3DTransform.AngleYProperty, fromAngle, toAngle, delay, durationMs, easing),
                fadeIn
                    ? Opacity(0, 1, delay, durationMs, WinEasing.TurnstileOpacityIn)
                    : Opacity(1, 0, delay, durationMs, WinEasing.TurnstileOpacityOut),
            ]);
        });

        // When the whole transition is done, every element (in or out) has its transform origin, transform and
        // opacity cleared. The navigation that follows is expected to remove an outgoing page.
        foreach (var c in list)
        {
            Clear3D(c);
            c.ClearValue(Visual.OpacityProperty);
        }
    }

    /// <summary>40px left of the window edge, at the vertical center of the window.</summary>
    private static (double x, double y) TurnstileOrigin(Control c)
    {
        var top = TopLevel.GetTopLevel(c);
        if (top is null)
        {
            return (-40, c.Bounds.Height / 2);
        }

        var p = c.TranslatePoint(new Point(0, 0), top) ?? default;
        var rtl = c.FlowDirection == FlowDirection.RightToLeft;
        var x = rtl ? 40 + (top.Bounds.Width - (p.X + c.Bounds.Width)) + c.Bounds.Width : -(40 + p.X);
        var y = top.Bounds.Height / 2 - p.Y;
        return (x, y);
    }

    // -------------------------------------------------------------------------------------------------
    // Slide: the page and up to three staggered groups across the viewport width
    // -------------------------------------------------------------------------------------------------

    /// <summary>Slide in from the left over 350 ms. The groups start 200, 400 and 600 px further out.</summary>
    public static Task SlideRightIn(Control? page, params IEnumerable<Control>[] groups) => StaggeredSlide(WinEasing.Snap, -1, 0, true, page, groups);

    /// <summary>Slide out to the right over 350 ms.</summary>
    public static Task SlideRightOut(Control? page, params IEnumerable<Control>[] groups) => StaggeredSlide(WinEasing.SlideDown, 0, 1, false, page, groups);

    /// <summary>Slide in from the right over 350 ms.</summary>
    public static Task SlideLeftIn(Control? page, params IEnumerable<Control>[] groups) => StaggeredSlide(WinEasing.Snap, 1, 0, true, page, groups);

    /// <summary>Slide out to the left over 350 ms.</summary>
    public static Task SlideLeftOut(Control? page, params IEnumerable<Control>[] groups) => StaggeredSlide(WinEasing.SlideDown, 0, -1, false, page, groups);

    private static Task StaggeredSlide(Easing easing, int startSign, int endSign, bool fadeIn, Control? page, IEnumerable<Control>[] groups)
    {
        var tasks = new List<Task>();
        var width = page is not null ? ViewportWidth(page) : groups.SelectMany(g => g).Select(ViewportWidth).DefaultIfEmpty(1000).First();
        const double step = 200;
        if (page is not null)
        {
            tasks.Add(SlideOne(page, startSign * width, endSign * width, easing, fadeIn));
        }

        for (var g = 0; g < groups.Length && g < 3; g++)
        {
            var offset = step * (g + 1);
            foreach (var c in groups[g] ?? Array.Empty<Control>())
            {
                tasks.Add(SlideOne(c, startSign * offset, endSign * offset, easing, fadeIn));
            }
        }

        return Task.WhenAll(tasks);
    }

    private static async Task SlideOne(Control c, double from, double to, Easing easing, bool fadeIn)
    {
        // A step curve: the incoming elements become visible at once, the outgoing ones disappear at the end.
        if (fadeIn)
        {
            c.ClearValue(Visual.OpacityProperty);
        }

        await Run(c, Transform(Translate(FlipX(c, new WinOffset(from, 0)), 0), Translate(FlipX(c, new WinOffset(to, 0)), 0), 0, 350, easing));
        if (!fadeIn)
        {
            c.SetValue(Visual.OpacityProperty, 0d);
        }
    }

    private static double ViewportWidth(Control c) => TopLevel.GetTopLevel(c)?.Bounds.Width is { } w && w > 0 ? w : Math.Max(1, c.Bounds.Width);

    // -------------------------------------------------------------------------------------------------
    // Continuum
    // -------------------------------------------------------------------------------------------------

    /// <summary>The page scales 0.5 → 1 and fades in. The item root slides up 225 px. The item content rotates in from 80° at 1.5×.
    /// The item roles are optional. A role that repeats the element of another role is ignored, because one element cannot play two roles.</summary>
    public static Task ContinuumForwardIn(Control incomingPage, Control? incomingItemRoot = null, Control? incomingItemContent = null)
    {
        incomingItemRoot = Distinct(incomingItemRoot, incomingPage);
        incomingItemContent = Distinct(incomingItemContent, incomingPage, incomingItemRoot);
        Prepare3D(incomingPage, 0, incomingPage.Bounds.Width / 2, incomingPage.Bounds.Height / 2);
        var pageTracks = new List<Track>(ScaleXY(0.5, 1, 0, 350, WinEasing.ContinuumScale)) { Opacity(0, 1, 0, 350, WinEasing.TurnstileOpacityIn) };
        var runs = new List<Task> { RunThenClear(incomingPage, pageTracks) };
        if (incomingItemRoot is not null)
        {
            Prepare3D(incomingItemRoot, 0, 0, 0);
            var rootTracks = new List<Track>(TranslateXY(0, 225, 0, 0, 0, 350, WinEasing.ContinuumItem)) { Opacity(0, 1, 0, 350, WinEasing.TurnstileOpacityIn) };
            runs.Add(RunThenClear(incomingItemRoot, rootTracks));
        }

        if (incomingItemContent is not null)
        {
            Prepare3D(incomingItemContent, 600, FlipOrigin(incomingItemContent, 0), incomingItemContent.Bounds.Height / 2);
            var contentTracks = new List<Track>(ScaleXY(1.5, 1, 0, 350, WinEasing.ContinuumContent))
            {
                Angle(Rotate3DTransform.AngleXProperty, 80, 0, 0, 350, WinEasing.ContinuumContent),
                Opacity(0, 1, 0, 350, WinEasing.TurnstileOpacityIn),
            };
            runs.Add(RunThenClear(incomingItemContent, contentTracks));
        }

        return Task.WhenAll(runs);
    }

    /// <summary>The page scales to 1.1 and fades out over 120 ms. The optional item rotates to 80° at 1.5× and drops 150 px over 152 ms.</summary>
    public static Task ContinuumForwardOut(Control outgoingPage, Control? outgoingItem = null)
    {
        outgoingItem = Distinct(outgoingItem, outgoingPage);
        Prepare3D(outgoingPage, 0, outgoingPage.Bounds.Width / 2, outgoingPage.Bounds.Height / 2);
        var pageTracks = new List<Track>(ScaleXY(1, 1.1, 0, 120, WinEasing.SlideDown)) { Opacity(1, 0, 0, 120, WinEasing.TurnstileOpacityOut) };
        if (outgoingItem is null)
        {
            return Run(outgoingPage, pageTracks);
        }

        Prepare3D(outgoingItem, 600, FlipOrigin(outgoingItem, 0), outgoingItem.Bounds.Height);
        var itemTracks = new List<Track>(ScaleXY(1, 1.5, 0, 152, WinEasing.SlideDown))
        {
            Angle(Rotate3DTransform.AngleXProperty, 0, 80, 0, 152, WinEasing.SlideDown),
            Opacity(1, 0, 0, 152, WinEasing.TurnstileOpacityOut),
        };
        itemTracks.AddRange(TranslateXY(0, 0, 0, 150, 0, 152, WinEasing.SlideDown));
        return Task.WhenAll(Run(outgoingPage, pageTracks), Run(outgoingItem, itemTracks));
    }

    /// <summary>The page scales 1.25 → 1 over 200 ms. The optional item rotates in from 80° and rises 100 px over 250 ms.</summary>
    public static Task ContinuumBackwardIn(Control incomingPage, Control? incomingItem = null)
    {
        incomingItem = Distinct(incomingItem, incomingPage);
        Prepare3D(incomingPage, 0, incomingPage.Bounds.Width / 2, incomingPage.Bounds.Height / 2);
        var pageTracks = new List<Track>(ScaleXY(1.25, 1, 0, 200, WinEasing.ContinuumScale)) { Opacity(0, 1, 0, 200, WinEasing.TurnstileOpacityIn) };
        if (incomingItem is null)
        {
            return RunThenClear(incomingPage, pageTracks);
        }

        Prepare3D(incomingItem, 600, FlipOrigin(incomingItem, 0), incomingItem.Bounds.Height / 2);
        var itemTracks = new List<Track>(TranslateXY(0, -100, 0, 0, 0, 250, WinEasing.ContinuumBackwardItem))
        {
            Angle(Rotate3DTransform.AngleXProperty, 80, 0, 0, 250, WinEasing.ContinuumBackwardItem),
            Opacity(0, 1, 0, 250, WinEasing.TurnstileOpacityIn),
        };
        return Task.WhenAll(RunThenClear(incomingPage, pageTracks), RunThenClear(incomingItem, itemTracks));
    }

    /// <summary>A role element, or null when it is missing or already plays one of <paramref name="others"/>.</summary>
    private static Control? Distinct(Control? role, params Control?[] others) => role is null || Array.Exists(others, o => ReferenceEquals(o, role)) ? null : role;

    /// <summary>The page scales 1 → 0.5 and fades out over 167 ms.</summary>
    public static Task ContinuumBackwardOut(Control outgoingPage)
    {
        Prepare3D(outgoingPage, 0, outgoingPage.Bounds.Width / 2, outgoingPage.Bounds.Height / 2);
        var tracks = new List<Track>(ScaleXY(1, 0.5, 0, 167, WinEasing.SlideDown)) { Opacity(1, 0, 0, 167, WinEasing.TurnstileOpacityOut) };
        return Run(outgoingPage, tracks);
    }

    private static async Task RunThenClear(Control c, IReadOnlyList<Track> tracks)
    {
        await Run(c, tracks);
        Clear3D(c);
    }

    private static double FlipOrigin(Control c, double ltr) => c.FlowDirection == FlowDirection.RightToLeft ? c.Bounds.Width - ltr : ltr;

    // -------------------------------------------------------------------------------------------------
    // Expand / collapse / peek
    // -------------------------------------------------------------------------------------------------

    /// <summary>The revealed elements fade in over 167 ms, after a 200 ms delay when there are affected items. The affected elements move over 367 ms.</summary>
    public static ILayoutAnimation CreateExpandAnimation(IEnumerable<Control> revealed, IEnumerable<Control>? affected = null)
    {
        var affectedList = ToList(affected);
        var delay = affectedList.Count > 0 ? 200 : 0;
        return new LayoutAnimation(revealed, affectedList, (c, _) => Run(c, Opacity(0, 1, delay, 167, WinEasing.Standard)), _ => 0, 367);
    }

    /// <summary>The hidden elements fade out over 167 ms. The affected elements move over 367 ms, after a 167 ms delay when something was hidden. The hidden elements must stay in the tree until the task completes.</summary>
    public static ILayoutAnimation CreateCollapseAnimation(IEnumerable<Control> hidden, IEnumerable<Control>? affected = null)
    {
        var hiddenList = ToList(hidden);
        var delay = hiddenList.Count > 0 ? 167 : 0;
        return new LayoutAnimation(hiddenList, affected, (c, _) => Run(c, Opacity(1, 0, 0, 167, WinEasing.Standard)), _ => delay, 367);
    }

    /// <summary>The elements move to their new positions over 2000 ms (the Hub and tile peek).</summary>
    public static ILayoutAnimation CreatePeekAnimation(IEnumerable<Control> elements) =>
        new LayoutAnimation(null, elements, (_, _) => Task.CompletedTask, _ => 0, 2000);

    /// <inheritdoc cref="CreateExpandAnimation(IEnumerable{Control}, IEnumerable{Control}?)"/>
    public static ILayoutAnimation CreateExpandAnimation(Control revealed, IEnumerable<Control>? affected = null) => CreateExpandAnimation(new[] { revealed }, affected);

    /// <inheritdoc cref="CreateCollapseAnimation(IEnumerable{Control}, IEnumerable{Control}?)"/>
    public static ILayoutAnimation CreateCollapseAnimation(Control hidden, IEnumerable<Control>? affected = null) => CreateCollapseAnimation(new[] { hidden }, affected);

    /// <inheritdoc cref="CreatePeekAnimation(IEnumerable{Control})"/>
    public static ILayoutAnimation CreatePeekAnimation(Control element) => CreatePeekAnimation(new[] { element });

    // -------------------------------------------------------------------------------------------------
    // Page navigation
    // -------------------------------------------------------------------------------------------------

    /// <summary>
    /// Picks the exit and entrance pair from the preferences of the two pages and the direction. Desktop, and any
    /// <see cref="PageNavigation.EnterPage"/> preference, gets EnterPage only. Slide gets SlideUp and SlideDown.
    /// Otherwise the preference of the incoming page decides, with Turnstile as the default.
    /// </summary>
    public static PageNavigationAnimations CreatePageNavigationAnimations(PageNavigation? current, PageNavigation? next, bool movingBackwards, bool isPhone = false)
    {
        static Task Empty(IEnumerable<Control> _) => Task.CompletedTask;
        if (!isPhone || current == PageNavigation.EnterPage || next == PageNavigation.EnterPage)
        {
            return new PageNavigationAnimations(Empty, EnterPageDefault);
        }

        next ??= PageNavigation.Turnstile;
        if ((current == PageNavigation.Slide && movingBackwards) || (next == PageNavigation.Slide && !movingBackwards))
        {
            return movingBackwards
                ? new PageNavigationAnimations(SlideDown, Empty)
                : new PageNavigationAnimations(Empty, SlideUp);
        }

        return next switch
        {
            // Elements are the roles in order: page, then the item root and item content when the caller has them.
            PageNavigation.Continuum => movingBackwards
                ? new PageNavigationAnimations(e => ContinuumBackwardOut(e.First()), e => ContinuumBackwardIn(e.First(), e.ElementAtOrDefault(1)))
                : new PageNavigationAnimations(e => ContinuumForwardOut(e.First(), e.ElementAtOrDefault(1)), e => ContinuumForwardIn(e.First(), e.ElementAtOrDefault(1), e.ElementAtOrDefault(2))),
            _ => movingBackwards
                ? new PageNavigationAnimations(TurnstileBackwardOut, TurnstileBackwardIn)
                : new PageNavigationAnimations(TurnstileForwardOut, TurnstileForwardIn),
        };
    }

    private static Task EnterPageDefault(IEnumerable<Control> elements) => EnterPage(elements);

    /// <inheritdoc cref="TurnstileForwardIn(IEnumerable{Control})"/>
    public static Task TurnstileForwardIn(Control element) => TurnstileForwardIn(new[] { element });

    /// <inheritdoc cref="TurnstileForwardOut(IEnumerable{Control})"/>
    public static Task TurnstileForwardOut(Control element) => TurnstileForwardOut(new[] { element });

    /// <inheritdoc cref="TurnstileBackwardIn(IEnumerable{Control})"/>
    public static Task TurnstileBackwardIn(Control element) => TurnstileBackwardIn(new[] { element });

    /// <inheritdoc cref="TurnstileBackwardOut(IEnumerable{Control})"/>
    public static Task TurnstileBackwardOut(Control element) => TurnstileBackwardOut(new[] { element });
}

/// <summary>
/// An <see cref="IPageTransition"/> for <c>TransitioningContentControl</c>, <c>Carousel</c> and the page framework.
/// By default it runs <c>ExitPage</c> on the outgoing page and then <c>EnterPage</c> on the incoming page.
/// <see cref="Navigation"/> selects the Turnstile, Slide or Continuum family instead.
/// </summary>
public sealed class WinPageTransition : IPageTransition
{
    /// <summary>Initializes a transition using <see cref="PageNavigation.EnterPage"/>.</summary>
    public WinPageTransition()
    {
    }

    /// <summary>Initializes a transition of the given family.</summary>
    public WinPageTransition(PageNavigation navigation)
    {
        Navigation = navigation;
    }

    /// <summary>The animation family.</summary>
    public PageNavigation Navigation { get; set; } = PageNavigation.EnterPage;

    /// <inheritdoc/>
    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        var outgoing = from as Control;
        var incoming = to as Control;
        var pair = WinAnimations.CreatePageNavigationAnimations(Navigation, Navigation, !forward, isPhone: Navigation != PageNavigation.EnterPage);
        // The host shows both pages as soon as the transition starts. The incoming page stays invisible until the
        // outgoing one has left. Otherwise it sits over the exit and then flashes when its entrance starts from opacity 0.
        incoming?.SetValue(Visual.OpacityProperty, 0d);
        if (outgoing is not null)
        {
            await pair.Exit(new[] { outgoing });
            if (cancellationToken.IsCancellationRequested)
            {
                incoming?.ClearValue(Visual.OpacityProperty);
                return;
            }

            if (Navigation == PageNavigation.EnterPage)
            {
                await WinAnimations.ExitPage(new[] { outgoing });
            }

            outgoing.IsVisible = false;
        }

        if (incoming is not null)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                incoming.ClearValue(Visual.OpacityProperty);
            }
            else
            {
                incoming.IsVisible = true;
                await pair.Entrance(new[] { incoming });
                incoming.ClearValue(Visual.OpacityProperty);
            }
        }

        if (outgoing is not null)
        {
            outgoing.ClearValue(Visual.OpacityProperty);
            outgoing.ClearValue(Visual.RenderTransformProperty);
        }
    }
}
