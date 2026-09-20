using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace AvaWin.Animations;

/// <summary>Single-control overloads.</summary>
public static partial class WinAnimations
{
    private static IEnumerable<Control> One(Control c) => new[] { c };

    /// <inheritdoc cref="EnterPage(IEnumerable{Control}, WinOffset?)"/>
    public static Task EnterPage(Control element, WinOffset? offset = null) => EnterPage(One(element), offset);
    /// <inheritdoc cref="ExitPage(IEnumerable{Control}, WinOffset?)"/>
    public static Task ExitPage(Control element, WinOffset? offset = null) => ExitPage(One(element), offset);
    /// <inheritdoc cref="EnterContent(IEnumerable{Control}, WinOffset?)"/>
    public static Task EnterContent(Control incoming, WinOffset? offset = null) => EnterContent(One(incoming), offset);
    /// <inheritdoc cref="ExitContent(IEnumerable{Control}, WinOffset?)"/>
    public static Task ExitContent(Control outgoing, WinOffset? offset = null) => ExitContent(One(outgoing), offset);
    /// <inheritdoc cref="FadeIn(IEnumerable{Control})"/>
    public static Task FadeIn(Control shown) => FadeIn(One(shown));
    /// <inheritdoc cref="FadeOut(IEnumerable{Control})"/>
    public static Task FadeOut(Control hidden) => FadeOut(One(hidden));
    /// <inheritdoc cref="CrossFade(IEnumerable{Control}, IEnumerable{Control})"/>
    public static Task CrossFade(Control incoming, Control outgoing) => CrossFade(One(incoming), One(outgoing));
    /// <inheritdoc cref="ShowPopup(IEnumerable{Control}, WinOffset?)"/>
    public static Task ShowPopup(Control element, WinOffset? offset = null) => ShowPopup(One(element), offset);
    /// <inheritdoc cref="HidePopup(IEnumerable{Control})"/>
    public static Task HidePopup(Control element) => HidePopup(One(element));
    /// <inheritdoc cref="ShowEdgeUI(IEnumerable{Control}, WinOffset?)"/>
    public static Task ShowEdgeUI(Control element, WinOffset? offset = null) => ShowEdgeUI(One(element), offset);
    /// <inheritdoc cref="HideEdgeUI(IEnumerable{Control}, WinOffset?)"/>
    public static Task HideEdgeUI(Control element, WinOffset? offset = null) => HideEdgeUI(One(element), offset);
    /// <inheritdoc cref="ShowPanel(IEnumerable{Control}, WinOffset?)"/>
    public static Task ShowPanel(Control element, WinOffset? offset = null) => ShowPanel(One(element), offset);
    /// <inheritdoc cref="HidePanel(IEnumerable{Control}, WinOffset?)"/>
    public static Task HidePanel(Control element, WinOffset? offset = null) => HidePanel(One(element), offset);
    /// <inheritdoc cref="PointerDown(IEnumerable{Control})"/>
    public static Task PointerDown(Control element) => PointerDown(One(element));
    /// <inheritdoc cref="PointerUp(IEnumerable{Control})"/>
    public static Task PointerUp(Control element) => PointerUp(One(element));
    /// <inheritdoc cref="DragSourceStart(IEnumerable{Control}, IEnumerable{Control}?)"/>
    public static Task DragSourceStart(Control dragSource, IEnumerable<Control>? affected = null) => DragSourceStart(One(dragSource), affected);
    /// <inheritdoc cref="DragSourceEnd(IEnumerable{Control}, WinOffset?, IEnumerable{Control}?)"/>
    public static Task DragSourceEnd(Control dragSource, WinOffset? offset = null, IEnumerable<Control>? affected = null) => DragSourceEnd(One(dragSource), offset, affected);
    /// <inheritdoc cref="DragBetweenEnter(IEnumerable{Control}, WinOffset?)"/>
    public static Task DragBetweenEnter(Control target, WinOffset? offset = null) => DragBetweenEnter(One(target), offset);
    /// <inheritdoc cref="DragBetweenLeave(IEnumerable{Control})"/>
    public static Task DragBetweenLeave(Control target) => DragBetweenLeave(One(target));
    /// <inheritdoc cref="SwipeSelect(IEnumerable{Control}, IEnumerable{Control})"/>
    public static Task SwipeSelect(Control selected, Control selection) => SwipeSelect(One(selected), One(selection));
    /// <inheritdoc cref="SwipeDeselect(IEnumerable{Control}, IEnumerable{Control})"/>
    public static Task SwipeDeselect(Control deselected, Control selection) => SwipeDeselect(One(deselected), One(selection));
    /// <inheritdoc cref="SwipeReveal(IEnumerable{Control}, WinOffset?)"/>
    public static Task SwipeReveal(Control target, WinOffset? offset = null) => SwipeReveal(One(target), offset);
    /// <inheritdoc cref="UpdateBadge(IEnumerable{Control}, WinOffset?)"/>
    public static Task UpdateBadge(Control incoming, WinOffset? offset = null) => UpdateBadge(One(incoming), offset);
    /// <inheritdoc cref="SlideDown(IEnumerable{Control})"/>
    public static Task SlideDown(Control outgoing) => SlideDown(One(outgoing));
    /// <inheritdoc cref="SlideUp(IEnumerable{Control})"/>
    public static Task SlideUp(Control incoming) => SlideUp(One(incoming));
}
