using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using AvaWin.Animations;

namespace AvaWin.Gallery.ViewModels;

/// <summary>The gallery's global toggles: variant, platform, accent, animations, disable-all.</summary>
public sealed class SettingsViewModel : INotifyPropertyChanged
{
    public static SettingsViewModel Instance { get; } = new();

    public static readonly IReadOnlyList<AccentSwatch> Accents =
    [
        new("Purple", Color.Parse("#4617B4")),
        new("Blue", Color.Parse("#2672EC")),
        new("Green", Color.Parse("#00A600")),
        new("Orange", Color.Parse("#D24726")),
        new("Magenta", Color.Parse("#AA40FF")),
        new("Teal", Color.Parse("#008299")),
        new("Lime", Color.Parse("#82BA00")),
        new("Red", Color.Parse("#EE1111")),
    ];

    private string _variant = "System";
    private Platform _platform = Platform.Auto;
    private AccentSwatch? _accent;
    private bool _useSystemAccent;
    private bool _animationsEnabled = true;
    private bool _slowAnimations;
    private bool _disableAll;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> Variants { get; } = ["Light", "Dark", "System"];
    public IReadOnlyList<Platform> Platforms { get; } = [Platform.Desktop, Platform.Phone, Platform.Auto];
    public IReadOnlyList<AccentSwatch> AccentSwatches => Accents;

    public string Variant
    {
        get => _variant;
        set
        {
            if (Set(ref _variant, value) && Application.Current is { } app)
            {
                app.RequestedThemeVariant = value switch
                {
                    "Dark" => ThemeVariant.Dark,
                    "System" => ThemeVariant.Default,
                    _ => ThemeVariant.Light,
                };
            }
        }
    }

    public Platform Platform
    {
        get => _platform;
        set
        {
            if (Set(ref _platform, value))
            {
                App.Theme.Platform = value;
                PlatformChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? PlatformChanged;

    public AccentSwatch? Accent
    {
        get => _accent;
        set
        {
            if (Set(ref _accent, value))
            {
                _useSystemAccent = value is null;
                OnPropertyChanged(nameof(UseSystemAccent));
                App.Theme.AccentColor = value?.Color;
            }
        }
    }

    public bool UseSystemAccent
    {
        get => _useSystemAccent;
        set
        {
            if (Set(ref _useSystemAccent, value) && value)
            {
                Accent = null;
            }
        }
    }

    public bool AnimationsEnabled
    {
        get => _animationsEnabled;
        set
        {
            if (Set(ref _animationsEnabled, value))
            {
                WinAnimations.IsEnabled = value;
            }
        }
    }

    public bool SlowAnimations
    {
        get => _slowAnimations;
        set
        {
            if (Set(ref _slowAnimations, value))
            {
                WinAnimations.TimeScale = value ? 5 : 1;
            }
        }
    }

    /// <summary>Disables every control on the current page (visual review of the disabled state).</summary>
    public bool DisableAll
    {
        get => _disableAll;
        set => Set(ref _disableAll, value);
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed record AccentSwatch(string Name, Color Color)
{
    public IBrush Brush { get; } = new SolidColorBrush(Color);
}
