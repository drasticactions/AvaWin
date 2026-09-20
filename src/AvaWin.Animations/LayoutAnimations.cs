using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using static AvaWin.Animations.AnimationRunner;

namespace AvaWin.Animations;

/// <summary>
/// A two-phase layout animation. The positions are captured at creation. The caller changes the layout. Then
/// <see cref="ExecuteAsync"/> waits for one layout pass and animates each affected element from its old position to
/// its new one, together with the enter and exit tracks.
/// </summary>
internal sealed class LayoutAnimation : ILayoutAnimation
{
    private readonly IReadOnlyList<Control> _primary;
    private readonly IReadOnlyList<Control> _affected;
    private readonly Dictionary<Control, Point> _before = new();
    private readonly Func<Control, int, Task> _primaryRun;
    private readonly Func<int, double> _repositionDelay;
    private readonly double _repositionMs;
    private readonly Easing _repositionEasing;
    private bool _executed;

    public LayoutAnimation(
        IEnumerable<Control>? primary,
        IEnumerable<Control>? affected,
        Func<Control, int, Task> primaryRun,
        Func<int, double> repositionDelay,
        double repositionMs,
        Easing? repositionEasing = null)
    {
        _primary = ToList(primary);
        _affected = ToList(affected);
        _primaryRun = primaryRun;
        _repositionDelay = repositionDelay;
        _repositionMs = repositionMs;
        _repositionEasing = repositionEasing ?? WinEasing.Standard;
        foreach (var c in _affected)
        {
            _before[c] = PositionOf(c);
        }
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync()
    {
        if (_executed)
        {
            return;
        }

        _executed = true;
        // Wait for the layout change of the caller, so the new positions are known.
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        foreach (var c in _affected)
        {
            (c.GetVisualParent() as Layoutable)?.UpdateLayout();
            c.UpdateLayout();
        }

        var tasks = new List<Task>(_primary.Count + _affected.Count);
        for (var i = 0; i < _primary.Count; i++)
        {
            tasks.Add(_primaryRun(_primary[i], i));
        }

        for (var i = 0; i < _affected.Count; i++)
        {
            var c = _affected[i];
            var after = PositionOf(c);
            var before = _before[c];
            var dx = before.X - after.X;
            var dy = before.Y - after.Y;
            if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5)
            {
                continue;
            }

            tasks.Add(Run(c, Transform(Translate(dx, dy), Identity, _repositionDelay(i), _repositionMs, _repositionEasing)));
        }

        await Task.WhenAll(tasks).ConfigureAwait(true);
    }

    private static Point PositionOf(Control c) => c.Bounds.Position;
}

file static class VisualExt
{
    public static Visual? GetVisualParent(this Visual v) => Avalonia.VisualTree.VisualExtensions.GetVisualParent(v);
}

public static partial class WinAnimations
{
    /// <summary>
    /// The <paramref name="added"/> elements scale 0.85→1 and fade in over 120 ms, after a 240 ms delay when there
    /// are affected items. The <paramref name="affected"/> elements move to their new positions over 400 ms.
    /// </summary>
    public static ILayoutAnimation CreateAddToListAnimation(IEnumerable<Control> added, IEnumerable<Control>? affected = null)
    {
        var affectedList = ToList(affected);
        var delay = affectedList.Count > 0 ? 240 : 0;
        return new LayoutAnimation(added, affectedList,
            (c, _) => Run(c,
                Transform(Scale(0.85), Identity, delay, 120, WinEasing.Standard),
                Opacity(0, 1, delay, 120, WinEasing.Linear)),
            _ => 0, 400);
    }

    /// <summary>
    /// The <paramref name="deleted"/> elements scale 1→0.85 and fade out over 120 ms. The <paramref name="remaining"/>
    /// elements move to their new positions over 400 ms, after a 60 ms delay when something was deleted. The deleted
    /// elements must stay in the tree until the returned task completes.
    /// </summary>
    public static ILayoutAnimation CreateDeleteFromListAnimation(IEnumerable<Control> deleted, IEnumerable<Control>? remaining = null)
    {
        var deletedList = ToList(deleted);
        var delay = deletedList.Count > 0 ? 60 : 0;
        return new LayoutAnimation(deletedList, remaining,
            (c, _) => Run(c,
                Transform(Identity, Scale(0.85), 0, 120, WinEasing.Reposition),
                Opacity(1, 0, 0, 120, WinEasing.Linear)),
            _ => delay, 400);
    }

    /// <summary>Reposition over 367 ms, staggered 33 ms per element (cap 250 ms).</summary>
    public static ILayoutAnimation CreateRepositionAnimation(IEnumerable<Control> elements) =>
        new LayoutAnimation(null, elements, (_, _) => Task.CompletedTask, i => StaggerMs(i, 0, 33, 1, 250), 367);

    /// <summary>The added elements fade in over 117 ms, after a 240 ms delay when there are affected items. The affected elements move over 400 ms.</summary>
    public static ILayoutAnimation CreateAddToSearchListAnimation(IEnumerable<Control> added, IEnumerable<Control>? affected = null)
    {
        var affectedList = ToList(affected);
        var delay = affectedList.Count > 0 ? 240 : 0;
        return new LayoutAnimation(added, affectedList, (c, _) => Run(c, Opacity(0, 1, delay, 117, WinEasing.Linear)), _ => 0, 400);
    }

    /// <summary>The deleted elements fade out over 93 ms. The remaining elements move over 400 ms, after a 60 ms delay when something was deleted.</summary>
    public static ILayoutAnimation CreateDeleteFromSearchListAnimation(IEnumerable<Control> deleted, IEnumerable<Control>? remaining = null)
    {
        var deletedList = ToList(deleted);
        var delay = deletedList.Count > 0 ? 60 : 0;
        return new LayoutAnimation(deletedList, remaining, (c, _) => Run(c, Opacity(1, 0, 0, 93, WinEasing.Linear)), _ => delay, 400);
    }

    /// <inheritdoc cref="CreateAddToListAnimation(IEnumerable{Control}, IEnumerable{Control}?)"/>
    public static ILayoutAnimation CreateAddToListAnimation(Control added, IEnumerable<Control>? affected = null) => CreateAddToListAnimation(new[] { added }, affected);

    /// <inheritdoc cref="CreateDeleteFromListAnimation(IEnumerable{Control}, IEnumerable{Control}?)"/>
    public static ILayoutAnimation CreateDeleteFromListAnimation(Control deleted, IEnumerable<Control>? remaining = null) => CreateDeleteFromListAnimation(new[] { deleted }, remaining);

    /// <inheritdoc cref="CreateRepositionAnimation(IEnumerable{Control})"/>
    public static ILayoutAnimation CreateRepositionAnimation(Control element) => CreateRepositionAnimation(new[] { element });
}
