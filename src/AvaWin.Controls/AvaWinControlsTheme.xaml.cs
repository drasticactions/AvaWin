using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaWin.Controls;

/// <summary>
/// Includes the AvaWin custom-control themes in an application. Requires an <see cref="AvaWinTheme"/> earlier in
/// <c>Application.Styles</c>. Otherwise it throws, so a wrong setup fails at once instead of showing nothing.
/// </summary>
public class AvaWinControlsTheme : Styles, IResourceNode
{
    private readonly IResourceProvider _phoneMetrics;
    private AvaWinTheme? _theme;

    /// <summary>Initializes a new instance of the <see cref="AvaWinControlsTheme"/> class.</summary>
    /// <param name="sp">The parent's service provider.</param>
    public AvaWinControlsTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);

        _phoneMetrics = (IResourceProvider)(Resources["PhoneMetrics"]
            ?? throw new KeyNotFoundException("Key PhoneMetrics was not found in the resources"));
        Resources.Remove("PhoneMetrics");

        OwnerChanged += (_, _) => AttachToTheme();
    }

    /// <summary>The <see cref="AvaWinTheme"/> this instance registered its phone overlay with, once attached.</summary>
    public AvaWinTheme? Theme => _theme;

    private void AttachToTheme()
    {
        if (Owner is null || _theme is not null)
        {
            return;
        }

        var styles = Owner switch
        {
            Application app => app.Styles,
            IStyleHost host => host.Styles,
            _ => null,
        };

        _theme = FindTheme(styles)
            ?? throw new InvalidOperationException("AvaWinControlsTheme requires AvaWinTheme to be added first.");
        _theme.RegisterPlatformOverlay(_phoneMetrics);
    }

    private static AvaWinTheme? FindTheme(IEnumerable<IStyle>? styles)
    {
        if (styles is null)
        {
            return null;
        }

        foreach (var style in styles)
        {
            switch (style)
            {
                case AvaWinTheme t:
                    return t;
                case AvaWinControlsTheme:
                    continue;
                case Styles nested when FindTheme(nested) is { } t:
                    return t;
            }
        }

        return null;
    }

    bool IResourceNode.TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        // Aliases in this package's ControlResources.xaml resolve through the theme so that the overlay is honored.
        if (base.TryGetResource(key, theme, out value))
        {
            if (value is ResourceAlias alias && _theme is IResourceNode node)
            {
                return node.TryGetResource(alias.TargetFor(theme), theme, out value);
            }

            return true;
        }

        return false;
    }
}
