using Avalonia;
using Avalonia.Controls;

namespace AvaWin.Controls;

/// <summary>
/// An <see cref="AppBar"/> docked to the top edge, with custom content (normally a <see cref="NavBarContainer"/>),
/// a 60px minimum height, and nothing visible when closed.
/// </summary>
public class NavBar : AppBar
{
    static NavBar()
    {
        PlacementProperty.OverrideDefaultValue<NavBar>(AppBarPlacement.Top);
        LayoutProperty.OverrideDefaultValue<NavBar>(AppBarLayout.Custom);
        ClosedDisplayModeProperty.OverrideDefaultValue<NavBar>(AppBarClosedDisplayMode.None);
    }

    /// <summary>The content of the bar, normally a <see cref="NavBarContainer"/>. This is the XAML content property of NavBar.</summary>
    [Avalonia.Metadata.Content]
    public new object? Content { get => base.Content; set => base.Content = value; }

    /// <inheritdoc/>
    protected override AppBarPresenter CreatePresenter() => new NavBarPresenter();
}

/// <summary>The visible part of a <see cref="NavBar"/>: the <see cref="AppBarPresenter"/> template with the NavBar minimum height.</summary>
public class NavBarPresenter : AppBarPresenter
{
    static NavBarPresenter()
    {
        PlacementProperty.OverrideDefaultValue<NavBarPresenter>(AppBarPlacement.Top);
        LayoutProperty.OverrideDefaultValue<NavBarPresenter>(AppBarLayout.Custom);
        ClosedDisplayModeProperty.OverrideDefaultValue<NavBarPresenter>(AppBarClosedDisplayMode.None);
    }
}
