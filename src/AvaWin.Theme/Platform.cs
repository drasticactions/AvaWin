namespace AvaWin;

/// <summary>
/// The metric set the theme applies: desktop or phone.
/// </summary>
public enum Platform
{
    /// <summary>The desktop metrics (default).</summary>
    Desktop,

    /// <summary>The phone metrics: thicker borders, taller controls, the phone type ramp and accent-filled presses.</summary>
    Phone,

    /// <summary>Resolves to <see cref="Phone"/> on Android and iOS, otherwise <see cref="Desktop"/>.</summary>
    Auto,
}
