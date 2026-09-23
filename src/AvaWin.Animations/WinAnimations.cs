using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using static AvaWin.Animations.AnimationRunner;

namespace AvaWin.Animations;

/// <summary>
/// The animation library as a static API. Each method animates the <c>Opacity</c> and
/// <c>RenderTransform</c> of the given controls and returns a <see cref="Task"/> that completes when the
/// animation is done.
/// </summary>
/// <remarks>
/// When <see cref="IsEnabled"/> is <see langword="false"/>, <see cref="TimeScale"/> is 0, or the control is not
/// in a visual tree, every method sets the final values immediately and completes synchronously.
/// </remarks>
public static partial class WinAnimations
{
    /// <summary>
    /// Global switch. When <see langword="false"/>, every method sets the final values and completes at once.
    /// </summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Multiplier for all durations and delays. Tests set 0.
    /// </summary>
    public static double TimeScale { get; set; } = 1.0;

    // -------------------------------------------------------------------------------------------------
    // Page and content
    // -------------------------------------------------------------------------------------------------

    /// <summary>Default offset for <see cref="EnterPage(IEnumerable{Control}, WinOffset?)"/>: 100px from the right.</summary>
    public static readonly WinOffset EnterPageOffset = new(100, 0);

    /// <summary>Default offset for <see cref="EnterContent(IEnumerable{Control}, WinOffset?)"/>: 40px from the right.</summary>
    public static readonly WinOffset EnterContentOffset = new(40, 0);

    /// <summary>Slide in from <paramref name="offset"/> over 1000 ms, fade in over 170 ms, staggered 83 ms per element (cap 333 ms).</summary>
    public static Task EnterPage(IEnumerable<Control> elements, WinOffset? offset = null) =>
        EnterPage(elements, offset is { } o ? new[] { o } : null);

    /// <summary>Slide in and fade in with one offset per element. The last offset repeats for the remaining elements.</summary>
    public static Task EnterPage(IEnumerable<Control> elements, IEnumerable<WinOffset>? offsets)
    {
        var list = ToList(elements);
        var offs = ToOffsets(offsets, EnterPageOffset);
        return WhenAll(list, (c, i) =>
        {
            var o = OffsetAt(offs, i);
            var delay = StaggerMs(i, 0, 83, 1, 333);
            return Run(c,
                Transform(Translate(FlipX(c, o), o.Top), Identity, delay, 1000, WinEasing.Standard),
                Opacity(0, 1, delay, 170, WinEasing.Standard));
        });
    }

    /// <summary>Fade out over 117 ms (no movement).</summary>
    public static Task ExitPage(IEnumerable<Control> elements, WinOffset? offset = null)
    {
        _ = offset;
        return WhenAll(ToList(elements), (c, _) => Run(c, Opacity(1, 0, 0, 117, WinEasing.Linear)));
    }

    /// <summary>Slide in from <paramref name="offset"/> over 550 ms, fade in over 170 ms.</summary>
    public static Task EnterContent(IEnumerable<Control> incoming, WinOffset? offset = null) =>
        EnterContent(incoming, offset is { } o ? new[] { o } : null);

    /// <summary>Slide in and fade in with one offset per element.</summary>
    public static Task EnterContent(IEnumerable<Control> incoming, IEnumerable<WinOffset>? offsets)
    {
        var offs = ToOffsets(offsets, EnterContentOffset);
        return WhenAll(ToList(incoming), (c, i) =>
        {
            var o = OffsetAt(offs, i);
            return Run(c,
                Transform(Translate(FlipX(c, o), o.Top), Identity, 0, 550, WinEasing.Standard),
                Opacity(0, 1, 0, 170, WinEasing.Standard));
        });
    }

    /// <summary>Fade out over 117 ms.</summary>
    public static Task ExitContent(IEnumerable<Control> outgoing, WinOffset? offset = null)
    {
        _ = offset;
        return WhenAll(ToList(outgoing), (c, _) => Run(c, Opacity(1, 0, 0, 117, WinEasing.Linear)));
    }

    // -------------------------------------------------------------------------------------------------
    // Fades
    // -------------------------------------------------------------------------------------------------

    /// <summary>Opacity 0 → 1 over 250 ms, linear.</summary>
    public static Task FadeIn(IEnumerable<Control> shown) =>
        WhenAll(ToList(shown), (c, _) => Run(c, Opacity(0, 1, 0, 250, WinEasing.Linear)));

    /// <summary>Opacity 1 → 0 over 167 ms, linear.</summary>
    public static Task FadeOut(IEnumerable<Control> hidden) =>
        WhenAll(ToList(hidden), (c, _) => Run(c, Opacity(1, 0, 0, 167, WinEasing.Linear)));

    /// <summary>Incoming 0 → 1 and outgoing 1 → 0 over 167 ms.</summary>
    public static Task CrossFade(IEnumerable<Control> incoming, IEnumerable<Control> outgoing) =>
        Task.WhenAll(
            WhenAll(ToList(incoming), (c, _) => Run(c, Opacity(0, 1, 0, 167, WinEasing.Linear))),
            WhenAll(ToList(outgoing), (c, _) => Run(c, Opacity(1, 0, 0, 167, WinEasing.Linear))));

    // -------------------------------------------------------------------------------------------------
    // Popups, edge UI, panels
    // -------------------------------------------------------------------------------------------------

    /// <summary>Default offset for <see cref="ShowPopup(IEnumerable{Control}, WinOffset?)"/>: 50px from below.</summary>
    public static readonly WinOffset ShowPopupOffset = new(0, 50, false);

    /// <summary>Default offset for <see cref="ShowEdgeUI(IEnumerable{Control}, WinOffset?)"/>: 70px from above (a top bar).</summary>
    public static readonly WinOffset EdgeUIOffset = new(0, -70, false);

    /// <summary>Default offset for <see cref="ShowPanel(IEnumerable{Control}, WinOffset?)"/>: 364px from the right.</summary>
    public static readonly WinOffset PanelOffset = new(364, 0);

    /// <summary>Translate from <paramref name="offset"/> over 367 ms. The opacity goes 0 → 1 over 83 ms after an 83 ms delay.</summary>
    public static Task ShowPopup(IEnumerable<Control> elements, WinOffset? offset = null)
    {
        var o = offset ?? ShowPopupOffset;
        return WhenAll(ToList(elements), (c, _) => Run(c,
            Transform(Translate(FlipX(c, o), o.Top), Identity, 0, 367, WinEasing.Standard),
            Opacity(0, 1, 83, 83, WinEasing.Linear)));
    }

    /// <summary>Opacity 1 → 0 over 83 ms.</summary>
    public static Task HidePopup(IEnumerable<Control> elements) =>
        WhenAll(ToList(elements), (c, _) => Run(c, Opacity(1, 0, 0, 83, WinEasing.Linear)));

    /// <summary>Translate from <paramref name="offset"/> (default 70px above) to rest over 367 ms.</summary>
    public static Task ShowEdgeUI(IEnumerable<Control> elements, WinOffset? offset = null)
    {
        var o = offset ?? EdgeUIOffset;
        return WhenAll(ToList(elements), (c, _) => Run(c,
            Transform(Translate(FlipX(c, o), o.Top), Identity, 0, 367, WinEasing.Standard)));
    }

    /// <summary>Translate from rest to <paramref name="offset"/> over 367 ms.</summary>
    public static Task HideEdgeUI(IEnumerable<Control> elements, WinOffset? offset = null)
    {
        var o = offset ?? EdgeUIOffset;
        return WhenAll(ToList(elements), (c, _) => Run(c,
            Transform(Identity, Translate(FlipX(c, o), o.Top), 0, 367, WinEasing.Standard)));
    }

    /// <summary>Translate from <paramref name="offset"/> (default 364px right, RTL-flipped) over 550 ms.</summary>
    public static Task ShowPanel(IEnumerable<Control> elements, WinOffset? offset = null)
    {
        var o = offset ?? PanelOffset;
        return WhenAll(ToList(elements), (c, _) => Run(c,
            Transform(Translate(FlipX(c, o), o.Top), Identity, 0, 550, WinEasing.Standard)));
    }

    /// <summary>Translate from rest to <paramref name="offset"/> over 550 ms.</summary>
    public static Task HidePanel(IEnumerable<Control> elements, WinOffset? offset = null)
    {
        var o = offset ?? PanelOffset;
        return WhenAll(ToList(elements), (c, _) => Run(c,
            Transform(Identity, Translate(FlipX(c, o), o.Top), 0, 550, WinEasing.Standard)));
    }

    // -------------------------------------------------------------------------------------------------
    // Pointer feedback
    // -------------------------------------------------------------------------------------------------

    /// <summary>The scale of a pressed element (0.975).</summary>
    public const double PointerDownScale = 0.975;

    /// <summary>Scale to 0.975 over 167 ms.</summary>
    public static Task PointerDown(IEnumerable<Control> elements) =>
        WhenAll(ToList(elements), (c, _) => Run(c,
            Transform(Current(c), Scale(PointerDownScale), 0, 167, WinEasing.Standard)));

    /// <summary>Scale back to rest over 167 ms.</summary>
    public static Task PointerUp(IEnumerable<Control> elements) =>
        WhenAll(ToList(elements), (c, _) => Run(c,
            Transform(Current(c), Identity, 0, 167, WinEasing.Standard)));

    /// <summary>How far the corner nearest a tilted press recedes, as a fraction of the element size (0.075).</summary>
    public const double PointerTiltMaxRecess = 0.075;

    /// <summary>
    /// Tilts the element toward <paramref name="contact"/> (element coordinates) and scales it to 0.975 over 167 ms.
    /// The corner nearest the contact recedes furthest, up to <see cref="PointerTiltMaxRecess"/>. Call it again as the
    /// contact moves: the new run starts from the tilt showing.
    /// </summary>
    public static Task PointerDownTilt(Control element, Point contact)
    {
        var (angleX, angleY, depth) = TiltFor(element.Bounds.Size, contact);
        if (element.RenderTransform is not TransformGroup)
        {
            Prepare3D(element, depth, element.Bounds.Width / 2, element.Bounds.Height / 2);
        }

        return Run(element,
        [
            Angle(Rotate3DTransform.AngleXProperty, 0, angleX, 0, 167, WinEasing.Standard),
            Angle(Rotate3DTransform.AngleYProperty, 0, angleY, 0, 167, WinEasing.Standard),
            .. ScaleXY(1, PointerDownScale, 0, 167, WinEasing.Standard),
        ]);
    }

    /// <summary>Returns a tilted element to rest over 167 ms, then removes the tilt transform.</summary>
    public static async Task PointerUpTilt(Control element)
    {
        if (element.RenderTransform is not TransformGroup group)
        {
            return;
        }

        var done = await Run(element,
        [
            Angle(Rotate3DTransform.AngleXProperty, 0, 0, 0, 167, WinEasing.Standard),
            Angle(Rotate3DTransform.AngleYProperty, 0, 0, 0, 167, WinEasing.Standard),
            .. ScaleXY(PointerDownScale, 1, 0, 167, WinEasing.Standard),
        ], System.Threading.CancellationToken.None).ConfigureAwait(true);
        if (done && ReferenceEquals(element.RenderTransform, group))
        {
            Clear3D(element);
        }
    }

    /// <summary>The rotation, in degrees about X and Y, and the perspective depth that tilt an element of <paramref name="size"/> toward <paramref name="contact"/>.</summary>
    public static (double AngleX, double AngleY, double Depth) TiltFor(Size size, Point contact)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            return (0, 0, 1);
        }

        var depth = 2 * Math.Max(size.Width, size.Height);
        var u = Math.Clamp(contact.X / size.Width * 2 - 1, -1, 1);
        var v = Math.Clamp(contact.Y / size.Height * 2 - 1, -1, 1);
        var edge = 1 - (1 - PointerTiltMaxRecess) / PointerDownScale;
        double MaxAngle(double half) => Math.Asin(Math.Min(1, depth * (1 / (1 - edge) - 1) / 2 / half)) * 180 / Math.PI;
        return (-v * MaxAngle(size.Height / 2), u * MaxAngle(size.Width / 2), depth);
    }

    // -------------------------------------------------------------------------------------------------
    // Drag
    // -------------------------------------------------------------------------------------------------

    /// <summary>Source scales to 1.05 and fades to 0.65, affected items scale to 0.95, over 240 ms.</summary>
    public static Task DragSourceStart(IEnumerable<Control> dragSource, IEnumerable<Control>? affected = null) =>
        Task.WhenAll(
            WhenAll(ToList(dragSource), (c, _) => Run(c,
                Transform(Current(c), Scale(1.05), 0, 240, WinEasing.Standard),
                Opacity(1, 0.65, 0, 240, WinEasing.Standard))),
            WhenAll(ToList(affected), (c, _) => Run(c,
                Transform(Current(c), Scale(0.95), 0, 240, WinEasing.Standard))));

    /// <summary>The source returns from translate(offset), scale 1.05 and opacity 0.65 to rest over 500 ms. The affected items move to their new positions.</summary>
    public static Task DragSourceEnd(IEnumerable<Control> dragSource, WinOffset? offset = null, IEnumerable<Control>? affected = null)
    {
        var o = offset ?? new WinOffset(11, 0);
        return Task.WhenAll(
            WhenAll(ToList(dragSource), (c, _) => Run(c,
                Transform(TranslateScale(FlipX(c, o), o.Top, 1.05), Identity, 0, 500, WinEasing.Standard),
                Opacity(0.65, 1, 0, 500, WinEasing.Standard))),
            WhenAll(ToList(affected), (c, _) => Run(c,
                Transform(Current(c), Identity, 0, 500, WinEasing.Standard))));
    }

    /// <summary>Targets move apart by <paramref name="offset"/> (default ∓40px) and scale to 0.95 over 200 ms.</summary>
    public static Task DragBetweenEnter(IEnumerable<Control> target, WinOffset? offset = null)
    {
        var list = ToList(target);
        var offs = offset is { } o
            ? new[] { o }
            : new[] { new WinOffset(-40, 0), new WinOffset(40, 0) };
        return WhenAll(list, (c, i) =>
        {
            var off = OffsetAt(offs, i);
            return Run(c, Transform(Current(c), TranslateScale(FlipX(c, off), off.Top, 0.95), 0, 200, WinEasing.Standard));
        });
    }

    /// <summary>Targets return to the 0.95 drag-affected scale over 200 ms.</summary>
    public static Task DragBetweenLeave(IEnumerable<Control> target) =>
        WhenAll(ToList(target), (c, _) => Run(c,
            Transform(Current(c), Scale(0.95), 0, 200, WinEasing.Standard)));

    // -------------------------------------------------------------------------------------------------
    // Swipe select
    // -------------------------------------------------------------------------------------------------

    /// <summary>Selected items reposition and the selection visual fades in over 300 ms.</summary>
    public static Task SwipeSelect(IEnumerable<Control> selected, IEnumerable<Control> selection) =>
        Task.WhenAll(
            WhenAll(ToList(selected), (c, _) => Run(c, Transform(Current(c), Identity, 0, 300, WinEasing.Standard))),
            WhenAll(ToList(selection), (c, _) => Run(c, Opacity(0, 1, 0, 300, WinEasing.Standard))));

    /// <summary>Deselected items reposition and the selection visual fades out over 300 ms.</summary>
    public static Task SwipeDeselect(IEnumerable<Control> deselected, IEnumerable<Control> selection) =>
        Task.WhenAll(
            WhenAll(ToList(deselected), (c, _) => Run(c, Transform(Current(c), Identity, 0, 300, WinEasing.Standard))),
            WhenAll(ToList(selection), (c, _) => Run(c, Opacity(1, 0, 0, 300, WinEasing.Standard))));

    /// <summary>Move to <paramref name="offset"/> (default 25px down) and back, 300 ms each way.</summary>
    public static async Task SwipeReveal(IEnumerable<Control> target, WinOffset? offset = null)
    {
        var o = offset ?? new WinOffset(0, 25, false);
        var list = ToList(target);
        await WhenAll(list, (c, _) => Run(c,
            Transform(Identity, Translate(FlipX(c, o), o.Top), 0, 300, WinEasing.Standard))).ConfigureAwait(true);
        await WhenAll(list, (c, _) => Run(c,
            Transform(Translate(FlipX(c, o), o.Top), Identity, 0, 300, WinEasing.Standard))).ConfigureAwait(true);
    }

    /// <summary>Fade in over 367 ms and slide from <paramref name="offset"/> (default 24px below) over 1333 ms.</summary>
    public static Task UpdateBadge(IEnumerable<Control> incoming, WinOffset? offset = null)
    {
        var o = offset ?? new WinOffset(0, 24, false);
        return WhenAll(ToList(incoming), (c, _) => Run(c,
            Opacity(0, 1, 0, 367, WinEasing.Standard),
            Transform(Translate(FlipX(c, o), o.Top), Identity, 0, 1333, WinEasing.Standard)));
    }

    // -------------------------------------------------------------------------------------------------
    // Phone slide up / down
    // -------------------------------------------------------------------------------------------------

    /// <summary>Translate 200px down and fade out over 250 ms.</summary>
    public static Task SlideDown(IEnumerable<Control> outgoing) =>
        WhenAll(ToList(outgoing), (c, _) => Run(c,
            Transform(Identity, Translate(0, 200), 0, 250, WinEasing.SlideDown),
            Opacity(1, 0, 0, 250, WinEasing.TurnstileOpacityOut)));

    /// <summary>Translate from 200px below over 350 ms, fade in staggered 34 ms per element.</summary>
    public static Task SlideUp(IEnumerable<Control> incoming) =>
        WhenAll(ToList(incoming), (c, i) => Run(c,
            Transform(Translate(0, 200), Identity, 0, 350, WinEasing.Snap),
            Opacity(0, 1, StaggerMs(i, 0, 34, 1, 1000), 350, WinEasing.TurnstileOpacityIn)));
}
