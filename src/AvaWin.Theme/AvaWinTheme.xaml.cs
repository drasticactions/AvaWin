using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace AvaWin;

/// <summary>
/// Adds the AvaWin theme to an application.
/// </summary>
/// <remarks>
/// <code>
/// &lt;Application.Styles&gt;
///   &lt;win:AvaWinTheme Platform="Auto" /&gt;
///   &lt;win:AvaWinControlsTheme /&gt;   &lt;!-- from AvaWin.Controls, optional --&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// Call <see cref="AppBuilderExtensions.WithAvaWinFonts"/> on the <c>AppBuilder</c>, so Selawik and the symbol font load.
/// </remarks>
public class AvaWinTheme : Styles, IResourceNode
{
    private readonly IResourceProvider _phoneMetrics;
    private readonly List<IResourceProvider> _overlays = new();
    private readonly AccentColors _accentColors;
    private Platform _platform = Platform.Desktop;
    private Platform _actualPlatform = Platform.Desktop;
    private Color? _accentColor;

    /// <summary>Initializes a new instance of the <see cref="AvaWinTheme"/> class.</summary>
    /// <param name="sp">The service provider of the parent.</param>
    public AvaWinTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);

        _phoneMetrics = (IResourceProvider)GetAndRemove("PhoneMetrics");
        _overlays.Add(_phoneMetrics);

        _accentColors = Resources.MergedDictionaries.OfType<AccentColors>().FirstOrDefault()
            ?? throw new InvalidOperationException("AvaWinTheme was initialized with a missing AccentColors provider.");
        Palettes = Resources.MergedDictionaries.OfType<ColorPaletteResourcesCollection>().FirstOrDefault()
            ?? throw new InvalidOperationException("AvaWinTheme was initialized with a missing ColorPaletteResourcesCollection.");

        OwnerChanged += (_, _) => SyncOverlayOwners();
        ResolvePlatform();

        object GetAndRemove(string key)
        {
            var val = Resources[key] ?? throw new KeyNotFoundException($"Key {key} was not found in the resources");
            Resources.Remove(key);
            return val;
        }
    }

    /// <summary>Defines the <see cref="Platform"/> property.</summary>
    public static readonly DirectProperty<AvaWinTheme, Platform> PlatformProperty =
        AvaloniaProperty.RegisterDirect<AvaWinTheme, Platform>(nameof(Platform), o => o.Platform, (o, v) => o.Platform = v);

    /// <summary>Defines the <see cref="AccentColor"/> property.</summary>
    public static readonly DirectProperty<AvaWinTheme, Color?> AccentColorProperty =
        AvaloniaProperty.RegisterDirect<AvaWinTheme, Color?>(nameof(AccentColor), o => o.AccentColor, (o, v) => o.AccentColor = v);

    /// <summary>
    /// The metric set: <see cref="AvaWin.Platform.Desktop"/> (default), <see cref="AvaWin.Platform.Phone"/> or
    /// <see cref="AvaWin.Platform.Auto"/> (Phone on Android and iOS, otherwise Desktop).
    /// </summary>
    public Platform Platform
    {
        get => _platform;
        set => SetAndRaise(PlatformProperty, ref _platform, value);
    }

    /// <summary>The platform actually in effect after <see cref="AvaWin.Platform.Auto"/> resolution.</summary>
    public Platform ActualPlatform => _actualPlatform;

    /// <summary>
    /// The accent color. <see langword="null"/> (default) follows the OS accent when the platform reports one.
    /// Otherwise the default purple <c>#4617B4</c> applies.
    /// </summary>
    public Color? AccentColor
    {
        get => _accentColor;
        set => SetAndRaise(AccentColorProperty, ref _accentColor, value);
    }

    /// <summary>
    /// Palette overrides per variant: a <see cref="ColorPaletteResources"/> keyed by
    /// <see cref="ThemeVariant.Light"/> or <see cref="ThemeVariant.Dark"/>.
    /// </summary>
    public IDictionary<ThemeVariant, ColorPaletteResources> Palettes { get; }

    /// <summary>
    /// Registers one more platform overlay dictionary. <c>AvaWin.Controls</c> uses it for its own phone keys. When
    /// <see cref="ActualPlatform"/> is <see cref="AvaWin.Platform.Phone"/>, the overlays are searched in
    /// registration order, before the resources of the theme.
    /// </summary>
    internal void RegisterPlatformOverlay(IResourceProvider overlay)
    {
        if (_overlays.Contains(overlay))
        {
            return;
        }

        _overlays.Add(overlay);
        if (Owner is { } owner)
        {
            overlay.AddOwner(owner);
        }

        NotifyChanged();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PlatformProperty)
        {
            ResolvePlatform();
            NotifyChanged();
        }
        else if (change.Property == AccentColorProperty)
        {
            _accentColors.Override = _accentColor;
            NotifyChanged();
        }
    }

    private void ResolvePlatform()
    {
        _actualPlatform = _platform switch
        {
            Platform.Auto => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ? Platform.Phone : Platform.Desktop,
            var p => p,
        };
    }

    private void NotifyChanged() => Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());

    private void SyncOverlayOwners()
    {
        // The overlays are not in Resources, so they are not always active. But their DynamicResource colors need
        // a host to resolve against, so they get the same owner as the theme.
        foreach (var overlay in _overlays)
        {
            if (overlay.Owner is { } old && old != Owner)
            {
                overlay.RemoveOwner(old);
            }

            if (Owner is { } owner && overlay.Owner != owner)
            {
                overlay.AddOwner(owner);
            }
        }
    }

    bool IResourceNode.TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        return TryGetResourceCore(key, theme, out value, 0);
    }

    private bool TryGetResourceCore(object key, ThemeVariant? theme, out object? value, int depth)
    {
        if (_actualPlatform == Platform.Phone)
        {
            for (var i = _overlays.Count - 1; i >= 0; --i)
            {
                if (_overlays[i].TryGetResource(key, theme, out value))
                {
                    return Unalias(theme, ref value, depth);
                }
            }
        }

        if (base.TryGetResource(key, theme, out value))
        {
            return Unalias(theme, ref value, depth);
        }

        return false;
    }

    private bool Unalias(ThemeVariant? theme, ref object? value, int depth)
    {
        if (value is ResourceAlias alias)
        {
            if (depth > 8)
            {
                throw new InvalidOperationException($"Resource alias chain too deep at '{alias.Target}'.");
            }

            return TryGetResourceCore(alias.TargetFor(theme), theme, out value, depth + 1);
        }

        return true;
    }
}
