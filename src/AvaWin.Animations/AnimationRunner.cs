using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvaWin.Animations;

/// <summary>
/// One animated property on one control: from → to over a duration after a delay.
/// </summary>
internal readonly record struct Track(
    AvaloniaProperty Property,
    object From,
    object To,
    TimeSpan Delay,
    TimeSpan Duration,
    Easing Easing);

/// <summary>
/// Runs <see cref="Track"/>s as Avalonia <see cref="Animation"/>s. Afterwards the control has plain property
/// values and no live animation.
/// </summary>
internal static class AnimationRunner
{
    public static readonly TransformOperations Identity = TransformOperations.Identity;

    static AnimationRunner()
    {
        // Avalonia 12.1 has no keyframe animator for ITransform (only TransformOperationsTransition). Without this
        // registration, RenderTransform keyframes throw "No animator registered".
        Animation.RegisterCustomAnimator<ITransform?, TransformInterpolator>();
    }

    private sealed class TransformInterpolator : InterpolatingAnimator<ITransform?>
    {
        public override ITransform? Interpolate(double progress, ITransform? oldValue, ITransform? newValue) =>
            TransformOperations.Interpolate(
                oldValue as TransformOperations ?? TransformOperations.Identity,
                newValue as TransformOperations ?? TransformOperations.Identity,
                progress);
    }

    public static TransformOperations Transform(string css) => TransformOperations.Parse(css);

    public static TransformOperations Translate(double x, double y)
    {
        var b = TransformOperations.CreateBuilder(1);
        b.AppendTranslate(x, y);
        return b.Build();
    }

    public static TransformOperations Scale(double s)
    {
        var b = TransformOperations.CreateBuilder(1);
        b.AppendScale(s, s);
        return b.Build();
    }

    public static TransformOperations TranslateScale(double x, double y, double s)
    {
        var b = TransformOperations.CreateBuilder(2);
        b.AppendTranslate(x, y);
        b.AppendScale(s, s);
        return b.Build();
    }

    public static TimeSpan Ms(double ms) => TimeSpan.FromMilliseconds(ms * WinAnimations.TimeScale);

    /// <summary>
    /// The stagger delay of element <paramref name="index"/>: <paramref name="initialMs"/>, plus
    /// <paramref name="extraMs"/> shrunk by <paramref name="factor"/> for each element before it, capped
    /// at <paramref name="capMs"/>. Returns unscaled milliseconds.
    /// </summary>
    internal static double StaggerMs(int index, double initialMs, double extraMs, double factor, double? capMs)
    {
        var ret = initialMs;
        var extra = extraMs;
        for (var j = 0; j < index; j++)
        {
            extra *= factor;
            ret += extra;
        }

        if (capMs is { } cap)
        {
            ret = Math.Min(ret, cap);
        }

        return ret;
    }

    /// <summary>The stagger delay as a <see cref="TimeSpan"/>, scaled by <see cref="WinAnimations.TimeScale"/>.</summary>
    internal static TimeSpan Stagger(int index, double initialMs, double extraMs, double factor, double? capMs) =>
        Ms(StaggerMs(index, initialMs, extraMs, factor, capMs));

    public static bool ShouldSkip(Control control) =>
        !WinAnimations.IsEnabled || WinAnimations.TimeScale <= 0 || !control.IsAttachedToVisualTree();

    public static double FlipX(Control control, WinOffset offset) =>
        offset.RtlFlip && control.FlowDirection == FlowDirection.RightToLeft ? -offset.Left : offset.Left;

    /// <summary>
    /// Runs every track on the control at the same time, then writes the final values. Afterwards the element has
    /// plain property values and no live animation.
    /// </summary>
    public static Task Run(Control control, IReadOnlyList<Track> tracks) => Run(control, tracks, CancellationToken.None);

    /// <summary>
    /// <see cref="Run(Control, IReadOnlyList{Track})"/> with cancellation. A canceled run stops the live animations
    /// at once and does not write the end values of the tracks. The caller decides where to leave the control.
    /// Returns true when the run completed and false when it was canceled.
    /// </summary>
    public static async Task<bool> Run(Control control, IReadOnlyList<Track> tracks, CancellationToken cancellationToken)
    {
        if (tracks.Count == 0)
        {
            return true;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (ShouldSkip(control))
        {
            foreach (var t in tracks)
            {
                ApplyFinal(control, t);
            }

            return true;
        }

        // Write the end values as the base values first. Avalonia disposes a finished animation before this method
        // resumes, and a render can run in between. With the base already at the end state, that frame is the same
        // as the last keyframe instead of a flash of the pre-animation look.
        foreach (var t in tracks)
        {
            ApplyFinal(control, t);
        }

        var runs = new Task[tracks.Count];
        for (var i = 0; i < tracks.Count; i++)
        {
            var t = tracks[i];
            var animation = new Animation
            {
                Delay = t.Delay,
                Duration = t.Duration,
                Easing = t.Easing,
                FillMode = FillMode.Both,
            };
            animation.Children.Add(new KeyFrame { Cue = new Cue(0), Setters = { new Setter(t.Property, t.From) } });
            animation.Children.Add(new KeyFrame { Cue = new Cue(1), Setters = { new Setter(t.Property, t.To) } });
            runs[i] = animation.RunAsync(control, cancellationToken);
        }

        await Task.WhenAll(runs).ConfigureAwait(true);
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        foreach (var t in tracks)
        {
            ApplyFinal(control, t);
        }

        return true;
    }

    public static Task Run(Control control, params Track[] tracks) => Run(control, (IReadOnlyList<Track>)tracks);

    /// <summary>
    /// Writes the end value of a track. An identity transform or opacity 1 clears the local value, so styles and
    /// the theme's <c>RenderTransform="none"</c> setters apply again.
    /// </summary>
    public static void ApplyFinal(Control control, Track t)
    {
        if (typeof(Transform).IsAssignableFrom(t.Property.OwnerType))
        {
            // A 3D / group track: write the end value onto the matching transform inside the group.
            if (control.RenderTransform is TransformGroup group)
            {
                foreach (var child in group.Children)
                {
                    if (child.GetType() == t.Property.OwnerType)
                    {
                        child.SetValue(t.Property, t.To);
                    }
                }
            }

            return;
        }

        if (t.Property == Visual.RenderTransformProperty)
        {
            if (t.To is TransformOperations { IsIdentity: true })
            {
                control.ClearValue(Visual.RenderTransformProperty);
            }
            else
            {
                control.SetValue(Visual.RenderTransformProperty, (ITransform?)t.To);
            }
        }
        else if (t.Property == Visual.OpacityProperty)
        {
            var v = (double)t.To;
            if (v >= 1)
            {
                control.ClearValue(Visual.OpacityProperty);
            }
            else
            {
                control.SetValue(Visual.OpacityProperty, v);
            }
        }
        else
        {
            control.SetValue(t.Property, t.To);
        }
    }

    /// <summary>
    /// Keeps an eased opacity inside 0..1. The turnstile and slide opacity curves overshoot on purpose (a near-step
    /// through <c>cubic-bezier(0,2,0,2)</c>). Avalonia renders an opacity above 1 as wrapped alpha, so it is clamped.
    /// </summary>
    private sealed class ClampedEasing(Easing inner) : Easing
    {
        public override double Ease(double progress) => Math.Clamp(inner.Ease(progress), 0, 1);
    }

    public static Track Opacity(double from, double to, double delayMs, double durationMs, Easing easing) =>
        new(Visual.OpacityProperty, from, to, Ms(delayMs), Ms(durationMs), new ClampedEasing(easing));

    public static Track Transform(TransformOperations from, TransformOperations to, double delayMs, double durationMs, Easing easing) =>
        new(Visual.RenderTransformProperty, from, to, Ms(delayMs), Ms(durationMs), easing);

    /// <summary>
    /// Replaces the RenderTransform of the control with a Scale/Translate/Rotate3D group, so the transform tracks
    /// (<see cref="Angle"/>, <see cref="ScaleXY"/>, <see cref="TranslateXY"/>) can animate it.
    /// <paramref name="depth"/> is the 3D perspective. The center is in element coordinates.
    /// </summary>
    public static TransformGroup Prepare3D(Control control, double depth, double centerX, double centerY)
    {
        var group = new TransformGroup();
        group.Children.Add(new ScaleTransform());
        group.Children.Add(new TranslateTransform());
        group.Children.Add(new Rotate3DTransform { Depth = depth });
        // The whole transform group, perspective included, turns about this point. Avalonia applies RenderTransform
        // about RenderTransformOrigin, so the origin goes there (in element pixels). Rotate3DTransform.CenterX/Y
        // would move only the rotation and leave the perspective at the middle of the element.
        control.RenderTransformOrigin = new RelativePoint(centerX, centerY, RelativeUnit.Absolute);
        control.RenderTransform = group;
        return group;
    }

    /// <summary>Removes the group and the origin that <see cref="Prepare3D"/> set.</summary>
    public static void Clear3D(Control control)
    {
        if (control.RenderTransform is TransformGroup)
        {
            control.ClearValue(Visual.RenderTransformProperty);
            control.ClearValue(Visual.RenderTransformOriginProperty);
        }
    }

    /// <summary>A Rotate3DTransform angle track (AngleX or AngleY).</summary>
    public static Track Angle(StyledProperty<double> angleProperty, double from, double to, double delayMs, double durationMs, Easing easing) =>
        new(angleProperty, from, to, Ms(delayMs), Ms(durationMs), easing);

    /// <summary>Uniform ScaleTransform tracks (X and Y).</summary>
    public static Track[] ScaleXY(double from, double to, double delayMs, double durationMs, Easing easing) =>
    [
        new(ScaleTransform.ScaleXProperty, from, to, Ms(delayMs), Ms(durationMs), easing),
        new(ScaleTransform.ScaleYProperty, from, to, Ms(delayMs), Ms(durationMs), easing),
    ];

    /// <summary>TranslateTransform tracks.</summary>
    public static Track[] TranslateXY(double fromX, double fromY, double toX, double toY, double delayMs, double durationMs, Easing easing) =>
    [
        new(TranslateTransform.XProperty, fromX, toX, Ms(delayMs), Ms(durationMs), easing),
        new(TranslateTransform.YProperty, fromY, toY, Ms(delayMs), Ms(durationMs), easing),
    ];

    /// <summary>The current <see cref="TransformOperations"/> of the control, or identity when it has none.</summary>
    public static TransformOperations Current(Control control) =>
        control.RenderTransform as TransformOperations ?? Identity;

    public static IReadOnlyList<Control> ToList(IEnumerable<Control>? controls)
    {
        if (controls is null)
        {
            return Array.Empty<Control>();
        }

        if (controls is IReadOnlyList<Control> list)
        {
            return list;
        }

        return new List<Control>(controls);
    }

    public static IReadOnlyList<WinOffset> ToOffsets(IEnumerable<WinOffset>? offsets, WinOffset fallback)
    {
        if (offsets is null)
        {
            return new[] { fallback };
        }

        var list = new List<WinOffset>(offsets);
        if (list.Count == 0)
        {
            list.Add(fallback);
        }

        return list;
    }

    /// <summary>The offset of element <paramref name="index"/>. The last offset repeats for the remaining elements.</summary>
    public static WinOffset OffsetAt(IReadOnlyList<WinOffset> offsets, int index) =>
        offsets[Math.Min(index, offsets.Count - 1)];

    /// <summary>Runs one task per control and awaits them all.</summary>
    public static Task WhenAll(IReadOnlyList<Control> controls, Func<Control, int, Task> run)
    {
        if (controls.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (controls.Count == 1)
        {
            return run(controls[0], 0);
        }

        var tasks = new Task[controls.Count];
        for (var i = 0; i < controls.Count; i++)
        {
            tasks[i] = run(controls[i], i);
        }

        return Task.WhenAll(tasks);
    }
}
