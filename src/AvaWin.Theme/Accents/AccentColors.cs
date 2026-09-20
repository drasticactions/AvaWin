// The HSL ramp is a port of Avalonia.Themes.Fluent/Accents/SystemAccentColors.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvaWin;

/// <summary>
/// Serves <c>SystemAccentColor</c> and its ramp, plus the "on dark" accent family and the link colors. The accent
/// is <see cref="AvaWinTheme.AccentColor"/>, else the platform accent, else the default purple <c>#4617B4</c>.
/// </summary>
internal sealed class AccentColors : ResourceProvider
{
    public const string AccentKey = "SystemAccentColor";
    public const string AccentDark1Key = "SystemAccentColorDark1";
    public const string AccentDark2Key = "SystemAccentColorDark2";
    public const string AccentDark3Key = "SystemAccentColorDark3";
    public const string AccentLight1Key = "SystemAccentColorLight1";
    public const string AccentLight2Key = "SystemAccentColorLight2";
    public const string AccentLight3Key = "SystemAccentColorLight3";
    public const string AccentOnDarkKey = "WinAccentOnDarkColor";
    public const string AccentOnDarkHoverKey = "WinAccentOnDarkHoverColor";
    public const string AccentOnDarkPressedKey = "WinAccentOnDarkPressedColor";
    public const string AccentOnDarkVividKey = "WinAccentOnDarkVividColor";
    public const string LinkKey = "WinLinkColor";
    public const string LinkOnDarkKey = "WinLinkOnDarkColor";
    public const string TextSelectionKey = "WinTextSelectionColor";

    /// <summary>The default accent, purple rgb(70, 23, 180).</summary>
    public static readonly Color WinJsAccent = Color.FromRgb(0x46, 0x17, 0xB4);

    /// <summary>The hand-tuned ramp used when the accent is <see cref="WinJsAccent"/>.</summary>
    public static readonly AccentRamp WinJsRamp = new(
        Accent: WinJsAccent,
        Light1: Color.Parse("#FF5F37BE"),
        Light2: Color.Parse("#FF7241E4"),
        Light3: Color.Parse("#FF8152EF"),
        Dark1: Color.Parse("#FF3E1499"),
        Dark2: Color.Parse("#FF35117F"),
        Dark3: Color.Parse("#FF2B0E66"),
        OnDark: Color.Parse("#FF5B2EC5"),
        OnDarkHover: Color.Parse("#FF724BCD"),
        OnDarkPressed: Color.Parse("#FF7E4FEC"),
        OnDarkVivid: Color.Parse("#FF8A57FF"),
        Link: Color.Parse("#FF4F1ACB"),
        LinkOnDark: Color.Parse("#FF9C72FF"),
        TextSelection: Color.Parse("#FF5729C1"));

    private bool _invalidate = true;
    private Color? _override;
    private AccentRamp _ramp = WinJsRamp;

    /// <summary>The explicit accent from <see cref="AvaWinTheme.AccentColor"/>. <see langword="null"/> follows the platform.</summary>
    public Color? Override
    {
        get => _override;
        set
        {
            if (_override != value)
            {
                _override = value;
                _invalidate = true;
                RaiseResourcesChanged();
            }
        }
    }

    public override bool HasResources => true;

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is string s)
        {
            switch (s)
            {
                case AccentKey: value = Ramp.Accent; return true;
                case AccentLight1Key: value = Ramp.Light1; return true;
                case AccentLight2Key: value = Ramp.Light2; return true;
                case AccentLight3Key: value = Ramp.Light3; return true;
                case AccentDark1Key: value = Ramp.Dark1; return true;
                case AccentDark2Key: value = Ramp.Dark2; return true;
                case AccentDark3Key: value = Ramp.Dark3; return true;
                case AccentOnDarkKey: value = Ramp.OnDark; return true;
                case AccentOnDarkHoverKey: value = Ramp.OnDarkHover; return true;
                case AccentOnDarkPressedKey: value = Ramp.OnDarkPressed; return true;
                case AccentOnDarkVividKey: value = Ramp.OnDarkVivid; return true;
                case LinkKey: value = Ramp.Link; return true;
                case LinkOnDarkKey: value = Ramp.LinkOnDark; return true;
                case TextSelectionKey: value = Ramp.TextSelection; return true;
            }
        }

        value = null;
        return false;
    }

    private AccentRamp Ramp
    {
        get
        {
            if (_invalidate)
            {
                _invalidate = false;
                var platform = GetPlatformSettings(Owner);
                var accent = _override ?? platform?.GetColorValues().AccentColor1 ?? WinJsAccent;
                _ramp = Compute(accent);
            }

            return _ramp;
        }
    }

    protected override void OnAddOwner(IResourceHost owner)
    {
        if (GetPlatformSettings(owner) is { } ps)
        {
            ps.ColorValuesChanged += OnColorValuesChanged;
        }

        _invalidate = true;
    }

    protected override void OnRemoveOwner(IResourceHost owner)
    {
        if (GetPlatformSettings(owner) is { } ps)
        {
            ps.ColorValuesChanged -= OnColorValuesChanged;
        }

        _invalidate = true;
    }

    private void OnColorValuesChanged(object? sender, PlatformColorValues e)
    {
        _invalidate = true;
        Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
    }

    private static IPlatformSettings? GetPlatformSettings(IResourceHost? owner) => owner switch
    {
        Application app => app.PlatformSettings,
        Visual visual => visual.GetPlatformSettings(),
        _ => null,
    };

    /// <summary>
    /// Computes the full ramp. The default accent gets the hand-tuned table. Any other accent goes through the
    /// HSL algorithm plus the "on dark" lightness offsets.
    /// </summary>
    public static AccentRamp Compute(Color accent)
    {
        if (accent == WinJsAccent)
        {
            return WinJsRamp;
        }

        var (d1, d2, d3, l1, l2, l3) = CalculateAccentShades(accent);
        var onDark = Lighten(accent, 0.06);
        var onDarkVivid = Lighten(accent, 0.30);
        return new AccentRamp(
            Accent: accent,
            Light1: l1, Light2: l2, Light3: l3,
            Dark1: d1, Dark2: d2, Dark3: d3,
            OnDark: onDark,
            OnDarkHover: Lighten(accent, 0.14),
            OnDarkPressed: Lighten(accent, 0.22),
            OnDarkVivid: onDarkVivid,
            Link: accent,
            LinkOnDark: onDarkVivid,
            TextSelection: onDark);
    }

    /// <summary>The accent ramp: fixed HSL lightness steps on either side of the accent.</summary>
    public static (Color d1, Color d2, Color d3, Color l1, Color l2, Color l3) CalculateAccentShades(Color accentColor)
    {
        // dark1step = (hslAccent.L - SystemAccentColorDark1.L) * 255
        const double dark1step = 28.5 / 255d;
        const double dark2step = 49 / 255d;
        const double dark3step = 74.5 / 255d;
        // light1step = (SystemAccentColorLight1.L - hslAccent.L) * 255
        const double light1step = 39 / 255d;
        const double light2step = 70 / 255d;
        const double light3step = 103 / 255d;

        var hsl = accentColor.ToHsl();

        return (
            new HslColor(hsl.A, hsl.H, hsl.S, hsl.L - dark1step).ToRgb(),
            new HslColor(hsl.A, hsl.H, hsl.S, hsl.L - dark2step).ToRgb(),
            new HslColor(hsl.A, hsl.H, hsl.S, hsl.L - dark3step).ToRgb(),
            new HslColor(hsl.A, hsl.H, hsl.S, hsl.L + light1step).ToRgb(),
            new HslColor(hsl.A, hsl.H, hsl.S, hsl.L + light2step).ToRgb(),
            new HslColor(hsl.A, hsl.H, hsl.S, hsl.L + light3step).ToRgb());
    }

    private static Color Lighten(Color c, double amount)
    {
        var hsl = c.ToHsl();
        return new HslColor(hsl.A, hsl.H, hsl.S, Math.Clamp(hsl.L + amount, 0, 1)).ToRgb();
    }
}

/// <summary>The accent color and every color derived from it.</summary>
public readonly record struct AccentRamp(
    Color Accent,
    Color Light1, Color Light2, Color Light3,
    Color Dark1, Color Dark2, Color Dark3,
    Color OnDark, Color OnDarkHover, Color OnDarkPressed, Color OnDarkVivid,
    Color Link, Color LinkOnDark, Color TextSelection);
