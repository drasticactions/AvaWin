using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace AvaWin.Controls;

/// <summary>Where a flyout opens relative to its anchor.</summary>
public enum WinFlyoutPlacement
{
    /// <summary>Above, below, left or right, whichever has room, in that order (default).</summary>
    Auto,
    /// <summary>Above the anchor.</summary>
    Top,
    /// <summary>Below the anchor.</summary>
    Bottom,
    /// <summary>Left of the anchor.</summary>
    Left,
    /// <summary>Right of the anchor.</summary>
    Right,
}

/// <summary>How a flyout above or below its anchor aligns with it.</summary>
public enum WinFlyoutAlignment
{
    /// <summary>Centered on the anchor (default).</summary>
    Center,
    /// <summary>The left edges align.</summary>
    Left,
    /// <summary>The right edges align.</summary>
    Right,
}

/// <summary>
/// Placement and alignment for Avalonia's <see cref="FlyoutBase"/>. The methods map them to a
/// <see cref="PlacementMode"/>, apply the 5px margin, and tag the presenter with a <c>win-place-*</c> class, so the
/// entrance animation of the theme slides in from the correct side.
/// </summary>
public static class WinFlyoutExtensions
{
    /// <summary>The gap between a flyout and its anchor.</summary>
    public const double Offset = 5;

    /// <summary>Shows <paramref name="flyout"/> at <paramref name="anchor"/> with the given placement and alignment.</summary>
    public static void ShowAt(this FlyoutBase flyout, Control anchor, WinFlyoutPlacement placement = WinFlyoutPlacement.Auto, WinFlyoutAlignment alignment = WinFlyoutAlignment.Center)
    {
        ArgumentNullException.ThrowIfNull(flyout);
        ArgumentNullException.ThrowIfNull(anchor);

        var resolved = placement == WinFlyoutPlacement.Auto ? ResolveAuto(anchor) : placement;
        if (flyout is PopupFlyoutBase popupFlyout)
        {
            Apply(popupFlyout, resolved, alignment);
        }

        var classes = flyout switch
        {
            Flyout f => f.FlyoutPresenterClasses,
            MenuFlyout m => m.FlyoutPresenterClasses,
            _ => null,
        };
        if (classes is not null)
        {
            classes.Remove("win-place-top");
            classes.Remove("win-place-bottom");
            classes.Remove("win-place-left");
            classes.Remove("win-place-right");
            classes.Add("win-place-" + resolved.ToString().ToLowerInvariant());
        }

        flyout.ShowAt(anchor);
    }

    /// <summary>
    /// Picks above the anchor when there is more room there than below, then below, then the side with more room.
    /// The flyout size is not known before it is shown, so the position of the anchor in the window decides.
    /// </summary>
    public static WinFlyoutPlacement ResolveAuto(Control anchor)
    {
        if (TopLevel.GetTopLevel(anchor) is { } root && anchor.TranslatePoint(new Point(0, 0), root) is { } p)
        {
            var above = p.Y;
            var below = root.Bounds.Height - (p.Y + anchor.Bounds.Height);
            var left = p.X;
            var right = root.Bounds.Width - (p.X + anchor.Bounds.Width);
            const double minimum = 80;
            if (above >= minimum && above >= below)
            {
                return WinFlyoutPlacement.Top;
            }

            if (below >= minimum)
            {
                return WinFlyoutPlacement.Bottom;
            }

            return left >= right ? WinFlyoutPlacement.Left : WinFlyoutPlacement.Right;
        }

        return WinFlyoutPlacement.Top;
    }

    private static void Apply(PopupFlyoutBase flyout, WinFlyoutPlacement placement, WinFlyoutAlignment alignment)
    {
        flyout.HorizontalOffset = 0;
        flyout.VerticalOffset = 0;
        switch (placement)
        {
            case WinFlyoutPlacement.Top:
                flyout.Placement = alignment switch
                {
                    WinFlyoutAlignment.Left => PlacementMode.TopEdgeAlignedLeft,
                    WinFlyoutAlignment.Right => PlacementMode.TopEdgeAlignedRight,
                    _ => PlacementMode.Top,
                };
                flyout.VerticalOffset = -Offset;
                break;
            case WinFlyoutPlacement.Bottom:
                flyout.Placement = alignment switch
                {
                    WinFlyoutAlignment.Left => PlacementMode.BottomEdgeAlignedLeft,
                    WinFlyoutAlignment.Right => PlacementMode.BottomEdgeAlignedRight,
                    _ => PlacementMode.Bottom,
                };
                flyout.VerticalOffset = Offset;
                break;
            case WinFlyoutPlacement.Left:
                flyout.Placement = PlacementMode.Left;
                flyout.HorizontalOffset = -Offset;
                break;
            case WinFlyoutPlacement.Right:
                flyout.Placement = PlacementMode.Right;
                flyout.HorizontalOffset = Offset;
                break;
        }
    }
}
