namespace AvaWin.Animations;

/// <summary>
/// An animation offset in device-independent pixels.
/// </summary>
/// <param name="Left">Horizontal offset. Negated under right-to-left flow when <paramref name="RtlFlip"/> is set.</param>
/// <param name="Top">Vertical offset.</param>
/// <param name="RtlFlip">Whether <paramref name="Left"/> is mirrored when the control's <c>FlowDirection</c> is <c>RightToLeft</c>.</param>
public readonly record struct WinOffset(double Left, double Top, bool RtlFlip = true)
{
    /// <summary>No offset.</summary>
    public static readonly WinOffset None = new(0, 0, false);
}
