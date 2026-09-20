namespace AvaWin;

/// <summary>
/// The Segoe UI Symbol / <c>Symbols.ttf</c> codepoints the control templates use, as single-character strings.
/// Render them with <c>FontFamily="{DynamicResource WinSymbolFontFamily}"</c>.
/// </summary>
public static class WinSymbol
{
    /// <summary>U+E0D5: BackButton, SettingsFlyout header (styles-backbutton.less).</summary>
    public const string Back = "\uE0D5";

    /// <summary>U+E0AE: BackButton under right-to-left flow.</summary>
    public const string BackRtl = "\uE0AE";

    /// <summary>U+E081: ListView / ItemContainer selection check, phone selection checkbox.</summary>
    public const string Checkmark = "\uE081";

    /// <summary>U+E082: Rating.</summary>
    public const string Star = "\uE082";

    /// <summary>U+E094: SearchBox button.</summary>
    public const string Search = "\uE094";

    /// <summary>U+E0B8: SemanticZoom button.</summary>
    public const string ZoomOut = "\uE0B8";

    /// <summary>U+E0E2: Pivot prev, NavBar left arrow, FlipView prev, ScrollBar line-left.</summary>
    public const string ChevronLeft = "\uE0E2";

    /// <summary>U+E0E3: Pivot next, NavBar right arrow, FlipView next, ScrollBar line-right, submenu.</summary>
    public const string ChevronRight = "\uE0E3";

    /// <summary>U+E0E4: FlipView prev (vertical), spinner up, ScrollBar line-up.</summary>
    public const string ChevronUp = "\uE0E4";

    /// <summary>U+E0E5: FlipView next (vertical), spinner down, ScrollBar line-down, ComboBox.</summary>
    public const string ChevronDown = "\uE0E5";

    /// <summary>U+E0E7: Menu toggle command check mark.</summary>
    public const string MenuCheck = "\uE0E7";

    /// <summary>U+E10C: AppBar minimal-mode invoke button, CommandBar overflow.</summary>
    public const string Ellipsis = "\uE10C";

    /// <summary>U+E10A: TextBox clear button, NotificationCard close (AppBarIcon.Cancel).</summary>
    public const string Cancel = "\uE10A";

    /// <summary>U+E163: CalendarDatePicker button (AppBarIcon.Calendar).</summary>
    public const string Calendar = "\uE163";

    /// <summary>U+E26B: Hub interactive section header.</summary>
    public const string HubChevron = "\uE26B";

    /// <summary>U+E26C: Hub interactive section header under right-to-left flow.</summary>
    public const string HubChevronRtl = "\uE26C";

    /// <summary>U+E019: NavBarCommand split button (closed).</summary>
    public const string SplitOpen = "\uE019";

    /// <summary>U+E018: NavBarCommand split button (open).</summary>
    public const string SplitClose = "\uE018";
}
