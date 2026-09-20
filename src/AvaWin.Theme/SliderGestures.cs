using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaWin;

/// <summary>
/// Keeps a touch drag on a <see cref="Slider"/> from being taken over by a scrolling ancestor. Thumb already opts
/// out of gesture recognition; a press on the track does not, so a wobbling finger let the ScrollViewer capture it.
/// </summary>
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
