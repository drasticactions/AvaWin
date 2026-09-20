using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace AvaWin.Controls;

/// <summary>
/// A 41×41 circular button with the back glyph. It pops the <see cref="INavigation"/> stack of the nearest
/// <see cref="Page"/>. When nothing can be popped it is disabled and invisible, but keeps its space. The
/// <c>small</c> style class gives the 30×30 variant of the SettingsFlyout header.
/// </summary>
[PseudoClasses(":cangoback")]
public class BackButton : Button
{
    /// <summary>Defines the <see cref="Navigation"/> property.</summary>
    public static readonly StyledProperty<INavigation?> NavigationProperty =
        AvaloniaProperty.Register<BackButton, INavigation?>(nameof(Navigation));

    /// <summary>Defines the <see cref="IsAutoEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsAutoEnabledProperty =
        AvaloniaProperty.Register<BackButton, bool>(nameof(IsAutoEnabled), true);

    private INavigation? _resolved;
    private NavigationPage? _observed;
    private Page? _page;

    static BackButton()
    {
        NavigationProperty.Changed.AddClassHandler<BackButton>((b, _) => b.Resolve());
        IsAutoEnabledProperty.Changed.AddClassHandler<BackButton>((b, _) => b.UpdateState());
    }

    /// <summary>Initializes a new instance.</summary>
    public BackButton()
    {
        Content = "\uE0D5";
    }

    /// <summary>
    /// The navigation to pop. When null, the button looks up its logical tree for a <see cref="Page"/> and uses
    /// its <see cref="Page.Navigation"/>.
    /// </summary>
    public INavigation? Navigation { get => GetValue(NavigationProperty); set => SetValue(NavigationProperty, value); }

    /// <summary>
    /// When true (default), <c>IsEnabled</c> follows <see cref="INavigation.CanGoBack"/>. Set it to false to control
    /// the button yourself, for example with a Command.
    /// </summary>
    public bool IsAutoEnabled { get => GetValue(IsAutoEnabledProperty); set => SetValue(IsAutoEnabledProperty, value); }

    /// <summary>The navigation that the button pops: <see cref="Navigation"/>, or the one of the ancestor page.</summary>
    public INavigation? ResolvedNavigation => _resolved;

    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        Resolve();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        ObservePage(null);
        Observe(null);
    }

    /// <inheritdoc/>
    protected override void OnClick()
    {
        base.OnClick();
        if (Command is null && _resolved is { CanGoBack: true } nav)
        {
            _ = nav.PopAsync();
        }
    }

    /// <summary>Finds the navigation target again and updates the enabled state.</summary>
    public void Refresh() => Resolve();

    private void Resolve()
    {
        _resolved = Navigation;
        if (_resolved is null)
        {
            // NavigationPage adds the page to its logical children before it assigns Page.Navigation, so the button
            // watches the nearest page for that assignment.
            var page = this.FindLogicalAncestorOfType<Page>();
            ObservePage(page);
            while (page is not null && page.Navigation is null)
            {
                page = page.FindLogicalAncestorOfType<Page>();
            }

            _resolved = page?.Navigation;
        }
        else
        {
            ObservePage(null);
        }

        Observe(_resolved as NavigationPage);
        UpdateState();
    }

    private void Observe(NavigationPage? nav)
    {
        if (_observed == nav)
        {
            return;
        }

        if (_observed is { } old)
        {
            old.Pushed -= OnStackChanged;
            old.Popped -= OnStackChanged;
            old.PoppedToRoot -= OnStackChanged;
        }

        _observed = nav;
        if (nav is not null)
        {
            nav.Pushed += OnStackChanged;
            nav.Popped += OnStackChanged;
            nav.PoppedToRoot += OnStackChanged;
        }
    }

    private void ObservePage(Page? page)
    {
        if (_page == page)
        {
            return;
        }

        if (_page is { } old)
        {
            old.PropertyChanged -= OnPagePropertyChanged;
        }

        _page = page;
        if (page is not null)
        {
            page.PropertyChanged += OnPagePropertyChanged;
        }
    }

    private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Page.NavigationProperty)
        {
            Resolve();
        }
    }

    private void OnStackChanged(object? sender, NavigationEventArgs e) => UpdateState();

    private void UpdateState()
    {
        var can = _resolved?.CanGoBack == true;
        PseudoClasses.Set(":cangoback", can);
        if (IsAutoEnabled && Command is null)
        {
            SetCurrentValue(IsEnabledProperty, can);
        }
    }
}
