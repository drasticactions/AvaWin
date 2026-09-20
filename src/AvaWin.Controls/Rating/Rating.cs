using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaWin.Controls;

/// <summary>Arguments for <see cref="Rating.PreviewChange"/> and <see cref="Rating.Change"/>.</summary>
public sealed class RatingChangeEventArgs : RoutedEventArgs
{
    internal RatingChangeEventArgs(RoutedEvent routedEvent, int tentativeRating, int userRating, double averageRating) : base(routedEvent)
    {
        TentativeRating = tentativeRating;
        UserRating = userRating;
        AverageRating = averageRating;
    }

    /// <summary>The rating under the pointer, or chosen with the keyboard.</summary>
    public int TentativeRating { get; }

    /// <summary>The committed user rating.</summary>
    public int UserRating { get; }

    /// <summary>The average rating.</summary>
    public double AverageRating { get; }
}

/// <summary>The visual state of one star.</summary>
public enum RatingStarState
{
    /// <summary>Part of the user rating, full.</summary>
    UserFull,

    /// <summary>Part of the user rating, empty.</summary>
    UserEmpty,

    /// <summary>Part of the tentative rating (hover or keyboard), full.</summary>
    TentativeFull,

    /// <summary>Part of the tentative rating, empty.</summary>
    TentativeEmpty,

    /// <summary>Part of the average rating, full.</summary>
    AverageFull,

    /// <summary>Part of the average rating, empty.</summary>
    AverageEmpty,

    /// <summary>Part of the average rating, partly filled. <see cref="RatingStar.Fraction"/> is the fill.</summary>
    AverageFractional,
}

/// <summary>One star of a <see cref="Rating"/>. Its state sets the color and the fractional clip.</summary>
[PseudoClasses(":user-full", ":user-empty", ":tentative-full", ":tentative-empty", ":average-full", ":average-empty", ":average-fractional")]
[TemplatePart("PART_Empty", typeof(TextBlock))]
[TemplatePart("PART_Full", typeof(TextBlock))]
[TemplatePart("PART_Clip", typeof(Border))]
public sealed class RatingStar : TemplatedControl
{
    /// <summary>Defines the <see cref="State"/> property.</summary>
    public static readonly StyledProperty<RatingStarState> StateProperty = AvaloniaProperty.Register<RatingStar, RatingStarState>(nameof(State));

    /// <summary>Defines the <see cref="Fraction"/> property.</summary>
    public static readonly StyledProperty<double> FractionProperty = AvaloniaProperty.Register<RatingStar, double>(nameof(Fraction), 1);

    private Border? _clip;

    static RatingStar()
    {
        StateProperty.Changed.AddClassHandler<RatingStar>((s, _) => s.UpdatePseudoClasses());
        FractionProperty.Changed.AddClassHandler<RatingStar>((s, _) => s.UpdateClip());
        FocusableProperty.OverrideDefaultValue<RatingStar>(false);
    }

    /// <summary>The state of the star.</summary>
    public RatingStarState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }

    /// <summary>The filled fraction, from 0 to 1, for <see cref="RatingStarState.AverageFractional"/>.</summary>
    public double Fraction { get => GetValue(FractionProperty); set => SetValue(FractionProperty, value); }

    /// <summary>The index of the star, starting at 1.</summary>
    public int Index { get; internal set; }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _clip = e.NameScope.Find<Border>("PART_Clip");
        UpdatePseudoClasses();
        UpdateClip();
    }

    private Size _lastSize;

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        _lastSize = finalSize;
        UpdateClip();
        return base.ArrangeOverride(finalSize);
    }

    private void UpdateClip()
    {
        if (_clip is null)
        {
            return;
        }

        var fraction = State == RatingStarState.AverageFractional ? Math.Clamp(Fraction, 0, 1) : 1;
        var inner = Math.Max(0, _lastSize.Width - Padding.Left - Padding.Right);
        _clip.Width = Math.Max(0, inner * fraction);
    }

    private void UpdatePseudoClasses()
    {
        var s = State;
        PseudoClasses.Set(":user-full", s == RatingStarState.UserFull);
        PseudoClasses.Set(":user-empty", s == RatingStarState.UserEmpty);
        PseudoClasses.Set(":tentative-full", s == RatingStarState.TentativeFull);
        PseudoClasses.Set(":tentative-empty", s == RatingStarState.TentativeEmpty);
        PseudoClasses.Set(":average-full", s == RatingStarState.AverageFull);
        PseudoClasses.Set(":average-empty", s == RatingStarState.AverageEmpty);
        PseudoClasses.Set(":average-fractional", s == RatingStarState.AverageFractional);
        UpdateClip();
    }
}

/// <summary>
/// A row of <see cref="MaxRating"/> stars, 28px each or 14px with the <c>small</c> class. It shows the user rating,
/// or the average rating with a fractional star when there is none. A hover or drag sets a tentative rating and
/// raises <see cref="PreviewChange"/>. A release commits it and raises <see cref="Change"/>. Leaving cancels. The
/// arrow keys move the tentative rating, Enter commits it and Escape cancels.
/// </summary>
[TemplatePart("PART_Stars", typeof(StackPanel), IsRequired = true)]
[PseudoClasses(":tentative", ":average", ":user")]
public sealed class Rating : TemplatedControl
{
    /// <summary>Defines the <see cref="MaxRating"/> property.</summary>
    public static readonly StyledProperty<int> MaxRatingProperty = AvaloniaProperty.Register<Rating, int>(nameof(MaxRating), 5, coerce: (_, v) => Math.Max(1, v));

    /// <summary>Defines the <see cref="UserRating"/> property.</summary>
    public static readonly StyledProperty<int> UserRatingProperty = AvaloniaProperty.Register<Rating, int>(nameof(UserRating), 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay, coerce: (o, v) => Math.Clamp(v, 0, ((Rating)o).MaxRating));

    /// <summary>Defines the <see cref="AverageRating"/> property.</summary>
    public static readonly StyledProperty<double> AverageRatingProperty = AvaloniaProperty.Register<Rating, double>(nameof(AverageRating), 0, coerce: (o, v) => v < 1 ? 0 : Math.Min(v, ((Rating)o).MaxRating));

    /// <summary>Defines the <see cref="EnableClear"/> property.</summary>
    public static readonly StyledProperty<bool> EnableClearProperty = AvaloniaProperty.Register<Rating, bool>(nameof(EnableClear), true);

    /// <summary>Defines the <see cref="TooltipStrings"/> property.</summary>
    public static readonly StyledProperty<IList<string>?> TooltipStringsProperty = AvaloniaProperty.Register<Rating, IList<string>?>(nameof(TooltipStrings));

    /// <summary>Defines the <see cref="TentativeRating"/> property.</summary>
    public static readonly DirectProperty<Rating, int> TentativeRatingProperty = AvaloniaProperty.RegisterDirect<Rating, int>(nameof(TentativeRating), o => o.TentativeRating);

    /// <summary>Defines the <see cref="PreviewChange"/> event.</summary>
    public static readonly RoutedEvent<RatingChangeEventArgs> PreviewChangeEvent = RoutedEvent.Register<Rating, RatingChangeEventArgs>(nameof(PreviewChange), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="Change"/> event.</summary>
    public static readonly RoutedEvent<RatingChangeEventArgs> ChangeEvent = RoutedEvent.Register<Rating, RatingChangeEventArgs>(nameof(Change), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="Cancel"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CancelEvent = RoutedEvent.Register<Rating, RoutedEventArgs>(nameof(Cancel), RoutingStrategies.Bubble);

    private readonly List<RatingStar> _stars = new();
    private StackPanel? _panel;
    private int _tentative = -1;
    private bool _pointerDown;

    static Rating()
    {
        MaxRatingProperty.Changed.AddClassHandler<Rating>((r, _) => r.RebuildStars());
        UserRatingProperty.Changed.AddClassHandler<Rating>((r, _) => r.UpdateStars());
        AverageRatingProperty.Changed.AddClassHandler<Rating>((r, _) => r.UpdateStars());
        TooltipStringsProperty.Changed.AddClassHandler<Rating>((r, _) => r.UpdateTooltips());
        IsEnabledProperty.Changed.AddClassHandler<Rating>((r, _) => r.UpdateStars());
        FocusableProperty.OverrideDefaultValue<Rating>(true);
    }

    /// <summary>The number of stars. Default 5.</summary>
    public int MaxRating { get => GetValue(MaxRatingProperty); set => SetValue(MaxRatingProperty, value); }

    /// <summary>The rating the user gave. 0 means none.</summary>
    public int UserRating { get => GetValue(UserRatingProperty); set => SetValue(UserRatingProperty, value); }

    /// <summary>The average rating, shown while there is no user rating. It can be fractional.</summary>
    public double AverageRating { get => GetValue(AverageRatingProperty); set => SetValue(AverageRatingProperty, value); }

    /// <summary>Whether a press or drag left of the first star clears the rating to 0.</summary>
    public bool EnableClear { get => GetValue(EnableClearProperty); set => SetValue(EnableClearProperty, value); }

    /// <summary>The tooltip of each star, and one more for "clear" when it is enabled.</summary>
    public IList<string>? TooltipStrings { get => GetValue(TooltipStringsProperty); set => SetValue(TooltipStringsProperty, value); }

    /// <summary>The rating in preview now, or −1.</summary>
    public int TentativeRating { get => _tentative; private set => SetAndRaise(TentativeRatingProperty, ref _tentative, value); }

    /// <summary>Raised when the tentative rating changes.</summary>
    public event EventHandler<RatingChangeEventArgs> PreviewChange { add => AddHandler(PreviewChangeEvent, value); remove => RemoveHandler(PreviewChangeEvent, value); }

    /// <summary>Raised when the user commits a rating.</summary>
    public event EventHandler<RatingChangeEventArgs> Change { add => AddHandler(ChangeEvent, value); remove => RemoveHandler(ChangeEvent, value); }

    /// <summary>Raised when the user leaves without a commit.</summary>
    public event EventHandler<RoutedEventArgs> Cancel { add => AddHandler(CancelEvent, value); remove => RemoveHandler(CancelEvent, value); }

    /// <summary>The star controls.</summary>
    public IReadOnlyList<RatingStar> Stars => _stars;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _panel = e.NameScope.Find<StackPanel>("PART_Stars");
        RebuildStars();
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!IsEnabled)
        {
            return;
        }

        SetTentative(RatingAt(e.GetPosition(this)));
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _pointerDown = true;
        Focus();
        SetTentative(RatingAt(e.GetPosition(this)));
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_pointerDown)
        {
            return;
        }

        _pointerDown = false;
        Commit();
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (!_pointerDown)
        {
            CancelTentative();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pointerDown = false;
        CancelTentative();
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        CancelTentative();
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || !IsEnabled)
        {
            return;
        }

        var current = _tentative >= 0 ? _tentative : UserRating;
        var min = EnableClear ? 0 : 1;
        switch (e.Key)
        {
            case Key.Right:
            case Key.Up:
                SetTentative(Math.Min(MaxRating, current + 1));
                e.Handled = true;
                break;
            case Key.Left:
            case Key.Down:
                SetTentative(Math.Max(min, current - 1));
                e.Handled = true;
                break;
            case Key.Home:
                SetTentative(min);
                e.Handled = true;
                break;
            case Key.End:
                SetTentative(MaxRating);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Space:
                Commit();
                e.Handled = true;
                break;
            case Key.Escape:
                CancelTentative();
                e.Handled = true;
                break;
        }
    }

    private int RatingAt(Point p)
    {
        if (_stars.Count == 0)
        {
            return EnableClear ? 0 : 1;
        }

        var rating = EnableClear ? 0 : 1;
        for (var i = 0; i < _stars.Count; i++)
        {
            var star = _stars[i];
            var left = star.TranslatePoint(new Point(0, 0), this)?.X ?? 0;
            var right = left + star.Bounds.Width;
            var mid = left + star.Bounds.Width * 0.5;
            if (p.X >= mid || (i == 0 && !EnableClear && p.X < right))
            {
                rating = i + 1;
            }
        }

        // The star under the pointer counts once the pointer passes its left padding.
        for (var i = 0; i < _stars.Count; i++)
        {
            var star = _stars[i];
            var left = star.TranslatePoint(new Point(0, 0), this)?.X ?? 0;
            if (p.X >= left + 2)
            {
                rating = Math.Max(rating, i + 1);
            }
        }

        if (!EnableClear)
        {
            rating = Math.Max(1, rating);
        }

        return Math.Clamp(rating, 0, MaxRating);
    }

    private void SetTentative(int rating)
    {
        rating = Math.Clamp(rating, EnableClear ? 0 : 1, MaxRating);
        if (rating == _tentative)
        {
            return;
        }

        TentativeRating = rating;
        UpdateStars();
        RaiseEvent(new RatingChangeEventArgs(PreviewChangeEvent, rating, UserRating, AverageRating));
    }

    private void Commit()
    {
        if (_tentative < 0)
        {
            return;
        }

        var value = _tentative;
        TentativeRating = -1;
        if (value != UserRating)
        {
            SetCurrentValue(UserRatingProperty, value);
            RaiseEvent(new RatingChangeEventArgs(ChangeEvent, value, UserRating, AverageRating));
        }

        UpdateStars();
    }

    private void CancelTentative()
    {
        if (_tentative < 0)
        {
            return;
        }

        TentativeRating = -1;
        UpdateStars();
        RaiseEvent(new RoutedEventArgs(CancelEvent));
    }

    private void RebuildStars()
    {
        if (_panel is null)
        {
            return;
        }

        _panel.Children.Clear();
        _stars.Clear();
        for (var i = 0; i < MaxRating; i++)
        {
            var star = new RatingStar { Index = i + 1 };
            _stars.Add(star);
            _panel.Children.Add(star);
        }

        UpdateTooltips();
        UpdateStars();
    }

    private void UpdateTooltips()
    {
        var strings = TooltipStrings;
        for (var i = 0; i < _stars.Count; i++)
        {
            var tip = strings is not null && i < strings.Count ? strings[i] : (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            ToolTip.SetTip(_stars[i], tip);
        }
    }

    private void UpdateStars()
    {
        var tentative = _tentative;
        var user = UserRating;
        var average = AverageRating;
        var showAverage = tentative < 0 && user == 0 && average > 0;
        PseudoClasses.Set(":tentative", tentative >= 0);
        PseudoClasses.Set(":average", showAverage);
        PseudoClasses.Set(":user", tentative < 0 && user > 0);
        for (var i = 0; i < _stars.Count; i++)
        {
            var star = _stars[i];
            var n = i + 1;
            if (tentative >= 0)
            {
                star.State = n <= tentative ? RatingStarState.TentativeFull : RatingStarState.TentativeEmpty;
                star.Fraction = 1;
            }
            else if (user > 0)
            {
                star.State = n <= user ? RatingStarState.UserFull : RatingStarState.UserEmpty;
                star.Fraction = 1;
            }
            else if (average > 0)
            {
                if (n <= Math.Floor(average))
                {
                    star.State = RatingStarState.AverageFull;
                    star.Fraction = 1;
                }
                else if (n - 1 < average)
                {
                    star.State = RatingStarState.AverageFractional;
                    star.Fraction = average - Math.Floor(average);
                }
                else
                {
                    star.State = RatingStarState.AverageEmpty;
                    star.Fraction = 1;
                }
            }
            else
            {
                star.State = RatingStarState.UserEmpty;
                star.Fraction = 1;
            }
        }
    }
}
