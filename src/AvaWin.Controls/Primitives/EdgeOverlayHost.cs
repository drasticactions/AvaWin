using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace AvaWin.Controls.Primitives;

/// <summary>
/// Hosts an edge-docked overlay (AppBar, SettingsFlyout) in the <see cref="OverlayLayer"/> of a TopLevel. The host
/// covers the whole overlay layer. A transparent click-eater sits under the docked child and closes it on pointer
/// down when light dismiss is enabled. The overlay layer covers the whole screen, so the host also reports the part
/// of the safe area of the TopLevel that touches its edge (<see cref="SafeAreaPadding"/>).
/// </summary>
internal sealed class EdgeOverlayHost : Panel
{
    private readonly Border _clickEater;
    private readonly Control _child;
    private OverlayLayer? _layer;
    private IInsetsManager? _insets;
    private Dock _edge;
    private bool _respectsSafeArea = true;
    private Thickness _safeAreaPadding;

    public EdgeOverlayHost(Control child, Dock edge)
    {
        _child = child;
        _edge = edge;
        _clickEater = new Border { Background = Avalonia.Media.Brushes.Transparent, IsHitTestVisible = false };
        _clickEater.PointerPressed += OnClickEaterPressed;
        Children.Add(_clickEater);
        Children.Add(child);
        ApplyEdge();
    }

    /// <summary>The edge the child docks to.</summary>
    public Dock Edge
    {
        get => _edge;
        set
        {
            if (_edge != value)
            {
                _edge = value;
                ApplyEdge();
                UpdateSafeArea();
            }
        }
    }

    /// <summary>Whether the child keeps out of the safe area. Default true.</summary>
    public bool RespectsSafeArea
    {
        get => _respectsSafeArea;
        set
        {
            if (_respectsSafeArea != value)
            {
                _respectsSafeArea = value;
                UpdateSafeArea();
            }
        }
    }

    /// <summary>The part of the safe area that touches <see cref="Edge"/>. Zero on desktop.</summary>
    public Thickness SafeAreaPadding => _safeAreaPadding;

    /// <summary>Raised when <see cref="SafeAreaPadding"/> changes.</summary>
    public event EventHandler? SafeAreaPaddingChanged;

    /// <summary>Raised when the click-eater is pressed, to light dismiss the child.</summary>
    public event EventHandler? LightDismissRequested;

    /// <summary>Whether the click-eater intercepts pointer input.</summary>
    public bool IsLightDismissEnabled
    {
        get => _clickEater.IsHitTestVisible;
        set => _clickEater.IsHitTestVisible = value;
    }

    /// <summary>The docked child.</summary>
    public Control Child => _child;

    /// <summary>The overlay layer this host is attached to, if any.</summary>
    public OverlayLayer? Layer => _layer;

    /// <summary>Attaches to the overlay layer of the TopLevel of <paramref name="anchor"/>. Returns false when there is none.</summary>
    public bool Attach(Visual anchor)
    {
        var layer = OverlayLayer.GetOverlayLayer(anchor);
        if (layer is null)
        {
            return false;
        }

        if (_layer == layer)
        {
            return true;
        }

        Detach();
        _layer = layer;
        layer.Children.Add(this);
        _insets = TopLevel.GetTopLevel(anchor)?.InsetsManager;
        if (_insets is { } insets)
        {
            insets.SafeAreaChanged += OnSafeAreaChanged;
        }

        UpdateSafeArea();
        return true;
    }

    /// <summary>Removes the host from its overlay layer.</summary>
    public void Detach()
    {
        if (_layer is null)
        {
            return;
        }

        _layer.Children.Remove(this);
        _layer = null;
        if (_insets is { } insets)
        {
            insets.SafeAreaChanged -= OnSafeAreaChanged;
            _insets = null;
        }

        UpdateSafeArea();
    }

    /// <summary>The inset of the docked edge and its two neighbors. The opposite edge is dropped.</summary>
    public static Thickness InsetForEdge(Thickness safeArea, Dock edge) => edge switch
    {
        Dock.Top => new Thickness(safeArea.Left, safeArea.Top, safeArea.Right, 0),
        Dock.Bottom => new Thickness(safeArea.Left, 0, safeArea.Right, safeArea.Bottom),
        Dock.Left => new Thickness(safeArea.Left, safeArea.Top, 0, safeArea.Bottom),
        _ => new Thickness(0, safeArea.Top, safeArea.Right, safeArea.Bottom),
    };

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => UpdateSafeArea();

    private void UpdateSafeArea()
    {
        var padding = _respectsSafeArea && _insets is { } insets ? InsetForEdge(insets.SafeAreaPadding, _edge) : default;
        if (padding == _safeAreaPadding)
        {
            return;
        }

        _safeAreaPadding = padding;
        SafeAreaPaddingChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The host covers the whole layer. The layer measures its children with its own size and arranges each at its
    /// desired size, so reporting that size back is what fills it. Setting Width and Height from the layer's
    /// SizeChanged instead arrives mid-arrange and, on a maximize, left the host at a stale size.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = base.MeasureOverride(availableSize);
        return new Size(
            double.IsFinite(availableSize.Width) ? availableSize.Width : desired.Width,
            double.IsFinite(availableSize.Height) ? availableSize.Height : desired.Height);
    }

    private void ApplyEdge()
    {
        switch (Edge)
        {
            case Dock.Top:
                _child.VerticalAlignment = VerticalAlignment.Top;
                _child.HorizontalAlignment = HorizontalAlignment.Stretch;
                break;
            case Dock.Bottom:
                _child.VerticalAlignment = VerticalAlignment.Bottom;
                _child.HorizontalAlignment = HorizontalAlignment.Stretch;
                break;
            case Dock.Left:
                _child.HorizontalAlignment = HorizontalAlignment.Left;
                _child.VerticalAlignment = VerticalAlignment.Stretch;
                break;
            case Dock.Right:
                _child.HorizontalAlignment = HorizontalAlignment.Right;
                _child.VerticalAlignment = VerticalAlignment.Stretch;
                break;
        }
    }

    private void OnClickEaterPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            // A right-click is the gesture that toggles the AppBar. It bubbles to the TopLevel handler.
            return;
        }

        e.Handled = true;
        LightDismissRequested?.Invoke(this, EventArgs.Empty);
    }
}
