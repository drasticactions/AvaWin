namespace AvaWin.Controls;

/// <summary>The edge an <see cref="AppBar"/> docks to.</summary>
public enum AppBarPlacement
{
    /// <summary>Docked to the top edge.</summary>
    Top,
    /// <summary>Docked to the bottom edge (default).</summary>
    Bottom,
}

/// <summary>What an <see cref="AppBar"/> shows.</summary>
public enum AppBarLayout
{
    /// <summary><see cref="AppBar.Commands"/> in two groups: selection on the left, global on the right (default).</summary>
    Commands,
    /// <summary><see cref="AppBar.Content"/> is shown as it is.</summary>
    Custom,
}

/// <summary>What stays visible while an <see cref="AppBar"/> is closed.</summary>
public enum AppBarClosedDisplayMode
{
    /// <summary>Nothing is visible when closed.</summary>
    None,
    /// <summary>A 25px strip with the ellipsis button that opens the bar (default).</summary>
    Minimal,
    /// <summary>A 60px strip that shows the command icons without labels.</summary>
    Compact,
}

/// <summary>The kinds of <see cref="AppBarCommand"/>.</summary>
public enum AppBarCommandType
{
    /// <summary>A button (default).</summary>
    Button,
    /// <summary>A toggle button. <see cref="AppBarCommand.IsSelected"/> is its state.</summary>
    Toggle,
    /// <summary>A button that opens its <c>Flyout</c>.</summary>
    Flyout,
    /// <summary>A vertical separator.</summary>
    Separator,
    /// <summary>Custom content.</summary>
    Content,
}

/// <summary>The group of an <see cref="AppBarCommand"/>.</summary>
public enum AppBarCommandSection
{
    /// <summary>The right-aligned group (default).</summary>
    Global,
    /// <summary>The left-aligned group.</summary>
    Selection,
}
