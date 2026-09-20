using System;
using System.Collections.Generic;
using Avalonia.Controls;
using AvaWin.Gallery.Pages;

namespace AvaWin.Gallery;

/// <summary>One entry in the gallery: where it lives on the Home hub and NavBar, and how to build it.</summary>
public sealed record GalleryPage(string Group, string Name, string Summary, AppBarIcon Icon, int Photo, Func<Control> Create)
{
    public override string ToString() => Name;
}

public static class GalleryPages
{
    public static readonly IReadOnlyList<string> Groups = ["Navigation", "Data", "Chrome", "Input", "Theme", "Animations"];

    public static readonly IReadOnlyList<GalleryPage> All =
    [
        new("Navigation", "Hub", "The panorama start page: sections you pan across", AppBarIcon.View, 1, () => new HubPage()),
        new("Navigation", "Pivot", "Swipe between headed views on the phone", AppBarIcon.Switch, 11, () => new PivotPage()),
        new("Navigation", "Pages", "Navigation, tabbed and drawer pages; SplitView", AppBarIcon.Document, 9, () => new PagesPage()),
        new("Navigation", "Containers", "Expander, GroupBox, ScrollViewer, TabControl", AppBarIcon.Folder, 14, () => new ContainersPage()),

        new("Data", "ListView", "Lists and grids with selection, groups and drag", AppBarIcon.List, 6, () => new ListViewPage()),
        new("Data", "FlipView", "One item at a time, with paging arrows", AppBarIcon.Pictures, 12, () => new FlipViewPage()),
        new("Data", "SemanticZoom", "Pinch or click out to jump between groups", AppBarIcon.ZoomOut, 17, () => new SemanticZoomPage()),
        new("Data", "Collections", "ListBox, TreeView, TableView, Carousel", AppBarIcon.ViewAll, 19, () => new CollectionsPage()),

        new("Chrome", "AppBar", "Commands docked to the bottom or top edge", AppBarIcon.More, 8, () => new AppBarPage()),
        new("Chrome", "NavBar", "Top-edge navigation with paged rows of commands", AppBarIcon.AllApps, 15, () => new NavBarPage()),
        new("Chrome", "SettingsFlyout", "The settings pane that slides in from the right", AppBarIcon.Settings, 22, () => new SettingsFlyoutPage()),
        new("Chrome", "CommandBar", "An in-page command bar with overflow", AppBarIcon.Edit, 5, () => new CommandBarPage()),
        new("Chrome", "Flyouts", "Flyout, MenuFlyout, ContextMenu, Menu and ToolTip", AppBarIcon.OpenPane, 16, () => new FlyoutsPage()),
        new("Chrome", "ContentDialog", "A modal dialog with a title, content and two commands", AppBarIcon.Comment, 21, () => new ContentDialogPage()),

        new("Input", "Buttons", "Button, toggle, repeat, hyperlink, drop-down and split", AppBarIcon.Accept, 2, () => new ButtonsPage()),
        new("Input", "Text", "TextBox, AutoCompleteBox, NumericUpDown and text blocks", AppBarIcon.Font, 3, () => new TextInputPage()),
        new("Input", "Selection", "CheckBox, RadioButton, ToggleSwitch, ComboBox, Slider", AppBarIcon.SelectAll, 7, () => new SelectionPage()),
        new("Input", "Date and time", "DatePicker, TimePicker, CalendarDatePicker, Calendar", AppBarIcon.Calendar, 10, () => new DateTimePage()),
        new("Input", "Rating", "Stars with tentative, user and average values", AppBarIcon.Favorite, 20, () => new RatingPage()),
        new("Input", "SearchBox", "Query suggestions, separators and results", AppBarIcon.Find, 4, () => new SearchBoxPage()),
        new("Input", "Feedback", "ProgressBar, NotificationCard, validation, pull to refresh", AppBarIcon.Important, 18, () => new FeedbackPage()),

        new("Theme", "Typography", "The type ramp and text classes", AppBarIcon.Font, 21, () => new TypographyPage()),
        new("Theme", "Colors", "Palette colors and semantic brushes", AppBarIcon.Highlight, 13, () => new ColorsPage()),
        new("Theme", "Icons", "Every Symbols.ttf glyph behind AppBarIcon", AppBarIcon.Emoji, 23, () => new IconsPage()),

        new("Animations", "Animations", "Animation samples", AppBarIcon.Play, 24, () => new AnimationsPage()),
    ];
}
