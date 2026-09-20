using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaWin;

/// <summary>
/// Keeps a touch drag on a <see cref="Slider"/> from being taken over by a scrolling ancestor.
/// </summary>
/// <remarks>
/// <see cref="Avalonia.Controls.Primitives.Thumb"/> already opts its pointer out of gesture recognition, but a press on
/// the track (which Slider handles itself) does not. On a phone the thumb is small, so most touches land on the track;
/// once the finger wobbles past the scroll start distance the ScrollViewer's ScrollGestureRecognizer captures the
/// pointer, the slider stops following and the page scrolls instead.
/// </remarks>
internal static class SliderGestures
{
    private static bool s_registered;

    /// <summary>Registers the class handler once per process.</summary>
    public static void EnsureRegistered()
    {
        if (s_registered)
        {
            return;
        }

        s_registered = true;
        InputElement.PointerPressedEvent.AddClassHandler<Slider>(OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private static void OnPointerPressed(Slider slider, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(slider).Properties.IsLeftButtonPressed)
        {
            e.PreventGestureRecognition();
        }
    }
}
