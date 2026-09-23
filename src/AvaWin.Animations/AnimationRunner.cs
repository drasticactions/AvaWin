using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    /// <remarks>
    /// A control has one live run per property. Starting a run on a property that is still animating supersedes the
    /// earlier run (it is canceled and reports false), and the new run starts from the value on screen at that
    /// moment instead of its own From, as a CSS transition would. Without this, the runs would stack: Avalonia
    /// layers animations on a property, and once the top one finished the ones beneath would show through and
    /// replay until the last of them ended.
    /// </remarks>
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

        var run = Supersede(control, tracks);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, run.Cancellation.Token);
        var token = cts.Token;

        // Write the end values as the base values first. Avalonia disposes a finished animation before this method
        // resumes, and a render can run in between. With the base already at the end state, that frame is the same
        // as the last keyframe instead of a flash of the pre-animation look.
        foreach (var t in run.Tracks)
        {
            ApplyFinal(control, t);
        }

        var runs = new Task[run.Tracks.Length];
        for (var i = 0; i < run.Tracks.Length; i++)
        {
            var t = run.Tracks[i];
            var animation = new Animation
            {
                Delay = t.Delay,
                Duration = t.Duration,
                Easing = t.Easing,
                FillMode = FillMode.Both,
            };
            animation.Children.Add(new KeyFrame { Cue = new Cue(0), Setters = { new Setter(t.Property, t.From) } });
            animation.Children.Add(new KeyFrame { Cue = new Cue(1), Setters = { new Setter(t.Property, t.To) } });
            runs[i] = animation.RunAsync(control, token);
        }

        try
        {
            await Task.WhenAll(runs).ConfigureAwait(true);
        }
        finally
        {
            Release(control, run);
        }

        if (token.IsCancellationRequested)
        {
            return false;
        }

        foreach (var t in run.Tracks)
        {
            ApplyFinal(control, t);
        }

        return true;
    }

    /// <summary>The live runs of a control, kept only while they run.</summary>
    private sealed class LiveRun(Track[] tracks)
    {
        public Track[] Tracks { get; } = tracks;
        public CancellationTokenSource Cancellation { get; } = new();
    }

    private static readonly ConditionalWeakTable<Control, List<LiveRun>> s_live = new();

    /// <summary>
    /// Registers a run on the control. Any live run that animates one of the same properties is canceled first, and
    /// the new tracks on those properties pick up the value showing at that moment as their From.
    /// </summary>
    private static LiveRun Supersede(Control control, IReadOnlyList<Track> tracks)
    {
        var live = s_live.GetOrCreateValue(control);
        var resumed = new Track[tracks.Count];
        for (var i = 0; i < resumed.Length; i++)
        {
            resumed[i] = tracks[i];
        }

        for (var i = live.Count - 1; i >= 0; i--)
        {
            var previous = live[i];
            if (!Overlaps(previous.Tracks, tracks))
            {
                continue;
            }

            // The animated value is only readable while the animation is bound, so it is taken before the cancel.
            for (var j = 0; j < resumed.Length; j++)
            {
                var t = resumed[j];
                if (Animates(previous.Tracks, t.Property) && LiveValue(control, t) is { } from)
                {
                    resumed[j] = t with { From = from };
                }
            }

            live.RemoveAt(i);
            previous.Cancellation.Cancel();
        }

        var run = new LiveRun(resumed);
        live.Add(run);
        return run;
    }

    private static void Release(Control control, LiveRun run)
    {
        if (s_live.TryGetValue(control, out var live))
        {
            live.Remove(run);
        }

        run.Cancellation.Dispose();
    }

    private static bool Overlaps(Track[] previous, IReadOnlyList<Track> next)
    {
        for (var i = 0; i < next.Count; i++)
        {
            if (Animates(previous, next[i].Property))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Animates(Track[] tracks, AvaloniaProperty property)
    {
        foreach (var t in tracks)
        {
            if (t.Property == property)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The value a track's property shows right now: the interpolated value of a live animation. Only the tracks
    /// on the control itself resume; a 3D track lives on a child transform and keeps its own From.
    /// </summary>
    private static object? LiveValue(Control control, Track t)
    {
        if (t.Property == Visual.RenderTransformProperty)
        {
            return control.RenderTransform as TransformOperations;
        }

        if (t.Property == Visual.OpacityProperty)
        {
            return control.Opacity;
        }

        if (typeof(Transform).IsAssignableFrom(t.Property.OwnerType) && control.RenderTransform is TransformGroup group)
        {
            foreach (var child in group.Children)
            {
                if (child.GetType() == t.Property.OwnerType)
                {
                    return child.GetValue(t.Property);
                }
            }
        }

        return null;
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
