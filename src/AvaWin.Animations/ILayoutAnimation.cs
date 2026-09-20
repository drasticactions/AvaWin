using System.Threading.Tasks;

namespace AvaWin.Animations;

/// <summary>
/// A two-phase layout animation. Create it, change the layout, then call <see cref="ExecuteAsync"/>.
/// </summary>
public interface ILayoutAnimation
{
    /// <summary>Runs the animation from the positions captured at creation to the current layout.</summary>
    Task ExecuteAsync();
}

/// <summary>The page navigation animation families.</summary>
public enum PageNavigation
{
    /// <summary>Turnstile rotation.</summary>
    Turnstile,
    /// <summary>Horizontal slide.</summary>
    Slide,
    /// <summary>Enter-page slide + fade.</summary>
    EnterPage,
    /// <summary>Continuum scale + item hand-off.</summary>
    Continuum,
}
