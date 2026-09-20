// Derived from Avalonia.Themes.Fluent/ColorPaletteResources.cs (MIT, see NOTICE.md).
﻿using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;


using Avalonia;

namespace AvaWin;

/// <summary>
/// Represents a specialized resource dictionary that contains color resources used by AvaWinTheme elements.
/// </summary>
/// <remarks>
/// This class can only be used in <see cref="AvaWinTheme.Palettes"/>.
/// </remarks>
public partial class ColorPaletteResources : ResourceProvider
{
    private readonly Dictionary<string, Color> _colors = new(StringComparer.InvariantCulture);

    /// <inheritdoc/>
    public override bool HasResources => _hasAccentColor || _colors.Count > 0;

    /// <inheritdoc/>
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is string strKey)
        {
            if (strKey.Equals(AccentColors.AccentKey, StringComparison.InvariantCulture))
            {
                value = _accentColor;
                return _hasAccentColor;
            }

            if (strKey.Equals(AccentColors.AccentDark1Key, StringComparison.InvariantCulture))
            {
                value = _accentColorDark1;
                return _hasAccentColor;
            }

            if (strKey.Equals(AccentColors.AccentDark2Key, StringComparison.InvariantCulture))
            {
                value = _accentColorDark2;
                return _hasAccentColor;
            }

            if (strKey.Equals(AccentColors.AccentDark3Key, StringComparison.InvariantCulture))
            {
                value = _accentColorDark3;
                return _hasAccentColor;
            }

            if (strKey.Equals(AccentColors.AccentLight1Key, StringComparison.InvariantCulture))
            {
                value = _accentColorLight1;
                return _hasAccentColor;
            }

            if (strKey.Equals(AccentColors.AccentLight2Key, StringComparison.InvariantCulture))
            {
                value = _accentColorLight2;
                return _hasAccentColor;
            }

            if (strKey.Equals(AccentColors.AccentLight3Key, StringComparison.InvariantCulture))
            {
                value = _accentColorLight3;
                return _hasAccentColor;
            }

            if (_colors.TryGetValue(strKey, out var color))
            {
                value = color;
                return true;
            }
        }

        value = null;
        return false;
    }

    private Color GetColor(string key)
    {
        if (_colors.TryGetValue(key, out var color))
        {
            return color;
        }

        return default;
    }

    private void SetColor(string key, Color value)
    {
        if (value == default)
        {
            _colors.Remove(key);
        }
        else
        {
            _colors[key] = value;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AccentProperty)
        {
            _hasAccentColor = _accentColor != default;

            if (_hasAccentColor)
            {
                (_accentColorDark1, _accentColorDark2, _accentColorDark3,
                        _accentColorLight1, _accentColorLight2, _accentColorLight3) =
                    AccentColors.CalculateAccentShades(_accentColor);
            }
            RaiseResourcesChanged();
        }
    }
}
