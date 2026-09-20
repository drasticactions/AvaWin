using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using static AvaWin.Animations.AnimationRunner;

namespace AvaWin.Animations;

public static partial class WinAnimations
{
    /// <summary>
    /// <see cref="WinAnimations.EnterContent(IEnumerable{Control}, WinOffset?)"/> with a delay per element. The Hub entrance uses it: 83 ms per section, capped at 333 ms.
    /// </summary>
    internal static Task EnterContentStaggered(IEnumerable<Control> incoming, WinOffset offset, double staggerMs, double capMs = 333)
    {
        return WhenAll(ToList(incoming), (c, i) =>
        {
            var delay = StaggerMs(i, 0, staggerMs, 1, capMs);
            return Run(c,
                Transform(Translate(FlipX(c, offset), offset.Top), Identity, delay, 550, WinEasing.Standard),
                Opacity(0, 1, delay, 170, WinEasing.Standard));
        });
    }
}
