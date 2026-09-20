using Avalonia.Styling;

namespace AvaWin;

/// <summary>
/// A resource entry that resolves to another key at lookup time. Layer 3 (the per-control keys)
/// is expressed with these so that the Phone overlay and palette overrides, which redefine the Layer 2 target,
/// are honored — a <c>StaticResource</c> alias would capture the Desktop object at load.
/// </summary>
/// <remarks>
/// Only <see cref="AvaWinTheme"/> resolves aliases. A consumer who copies one into their own dictionary gets the
/// alias object itself. Use it in <c>ControlResources.xaml</c> files merged into the theme only.
/// </remarks>
public sealed class ResourceAlias
{
    /// <summary>Initializes an empty alias.</summary>
    public ResourceAlias()
    {
    }

    /// <summary>Initializes an alias to <paramref name="target"/>.</summary>
    public ResourceAlias(string target)
    {
        Target = target;
    }

    /// <summary>The key this alias resolves to.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Optional: when set, the alias resolves to this key instead of <see cref="Target"/> under the Dark variant.
    /// </summary>
    public string? DarkTarget { get; set; }

    internal string TargetFor(ThemeVariant? theme) =>
        DarkTarget is not null && theme == ThemeVariant.Dark ? DarkTarget : Target;
}
