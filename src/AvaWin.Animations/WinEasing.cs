using Avalonia.Animation.Easings;

namespace AvaWin.Animations;

/// <summary>
/// The easing curves of the animation library as Avalonia <see cref="Easing"/> instances. All are cubic-bezier curves.
/// </summary>
public static class WinEasing
{
    /// <summary><c>cubic-bezier(0.1, 0.9, 0.2, 1)</c>: the curve of almost every animation.</summary>
    public static readonly Easing Standard = Bezier(0.1, 0.9, 0.2, 1);

    /// <summary>Linear, for fades and exits.</summary>
    public static readonly Easing Linear = new LinearEasing();

    /// <summary><c>cubic-bezier(0.11, 0.5, 0.24, 0.96)</c>: the delete-from-list scale.</summary>
    public static readonly Easing Reposition = Bezier(0.11, 0.5, 0.24, 0.96);

    /// <summary><c>cubic-bezier(0.17, 0.79, 0.215, 1.0025)</c>: the phone slide-up, Pivot headers and the phone ListView.</summary>
    public static readonly Easing Snap = Bezier(0.17, 0.79, 0.215, 1.0025);

    /// <summary>ListView item box reposition (220 ms). Same curve as <see cref="Standard"/>.</summary>
    public static readonly Easing ItemBox = Standard;

    /// <summary>Turnstile transform in.</summary>
    public static readonly Easing TurnstileIn = Bezier(0.01, 0.975, 0.4775, 0.9775);

    /// <summary>Turnstile transform out.</summary>
    public static readonly Easing TurnstileOut = Bezier(0.4925, 0.01, 0.7675, -0.01);

    /// <summary>Turnstile opacity in.</summary>
    public static readonly Easing TurnstileOpacityIn = Bezier(0, 2, 0, 2);

    /// <summary>Turnstile opacity out.</summary>
    public static readonly Easing TurnstileOpacityOut = Bezier(1, -0.42, 0.995, -0.425);

    /// <summary>Slide-down transform.</summary>
    public static readonly Easing SlideDown = Bezier(0.3825, 0.0025, 0.8775, -0.1075);

    /// <summary>Continuum page scale.</summary>
    public static readonly Easing ContinuumScale = Bezier(0.33, 0.18, 0.11, 1);

    /// <summary>Continuum item root translate.</summary>
    public static readonly Easing ContinuumItem = Bezier(0.24, 1.15, 0.11, 1.1575);

    /// <summary>Continuum item content.</summary>
    public static readonly Easing ContinuumContent = Bezier(0, 0.62, 0.8225, 0.9625);

    /// <summary>The continuum backward-in item curve: <c>cubic-bezier(0.2975, 0.7325, 0.4725, 0.99)</c>.</summary>
    public static readonly Easing ContinuumBackwardItem = Bezier(0.2975, 0.7325, 0.4725, 0.99);

    /// <summary>The paused progress bar fade: <c>cubic-bezier(0.03, 0.76, 0.31, 1)</c>.</summary>
    public static readonly Easing ProgressFade = Bezier(0.03, 0.76, 0.31, 1);

    private static SplineEasing Bezier(double x1, double y1, double x2, double y2) =>
        new() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };
}
