using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Styling;
using Avalonia.Controls.Primitives;
using Xunit;

namespace AvaWin.Theme.Tests;

/// <summary>
/// One row per themed built-in control.
/// </summary>
public static class ThemedControls
{
    public static readonly Dictionary<string, Func<Control>> Factories = new()
    {
        ["TextBlock"] = () => new TextBlock { Text = "x", Classes = { "win-type-large" } },
        // Base controls
        ["Button"] = () => new Button { Content = "x" },
        ["ToggleButton"] = () => new ToggleButton { Content = "x" },
        ["RepeatButton"] = () => new RepeatButton { Content = "x" },
        ["TextBox"] = () => new TextBox { Text = "x", Classes = { "clearButton" } },
        ["ListBoxItem"] = () => new ListBoxItem { Content = "x" },
        ["HyperlinkButton"] = () => new HyperlinkButton { Content = "x" },
        ["DropDownButton"] = () => new DropDownButton { Content = "x" },
        ["SplitButton"] = () => new SplitButton { Content = "x" },
        ["ListBox"] = () => new ListBox { ItemsSource = new[] { "a", "b", "c" } },
        // Selection and input
        ["CheckBox"] = () => new CheckBox { Content = "x" },
        ["RadioButton"] = () => new RadioButton { Content = "x" },
        ["ToggleSwitch"] = () => new ToggleSwitch { Content = "x", OnContent = "On", OffContent = "Off" },
        ["Slider"] = () => new Slider { Value = 40 },
        ["ProgressBar"] = () => new ProgressBar { Value = 40 },
        ["ComboBox"] = () => new ComboBox { ItemsSource = new[] { "a", "b" }, SelectedIndex = 0 },
        ["ComboBoxItem"] = () => new ComboBoxItem { Content = "x" },
        ["NumericUpDown"] = () => new NumericUpDown { Value = 1 },
        ["ButtonSpinner"] = () => new ButtonSpinner { Content = "x" },
        ["AutoCompleteBox"] = () => new AutoCompleteBox { ItemsSource = new[] { "a", "b" } },
        ["SelectableTextBlock"] = () => new SelectableTextBlock { Text = "x" },
        ["Label"] = () => new Label { Content = "x" },
        ["PathIcon"] = () => new PathIcon { Data = Avalonia.Media.StreamGeometry.Parse("M0,0 L10,0 10,10 Z") },
        // Overlays, menus and roots
        ["ToolTip"] = () => new ToolTip { Content = "x" },
        ["FlyoutPresenter"] = () => new FlyoutPresenter { Content = "x" },
        ["MenuFlyoutPresenter"] = () => new MenuFlyoutPresenter { ItemsSource = new[] { new MenuItem { Header = "a" } } },
        ["Menu"] = () => new Menu { ItemsSource = new[] { new MenuItem { Header = "File", ItemsSource = new[] { new MenuItem { Header = "New" } } } } },
        ["MenuItem"] = () => new MenuItem { Header = "x", InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.S, Avalonia.Input.KeyModifiers.Control), ItemsSource = new[] { new MenuItem { Header = "child" } } },
        ["ContextMenu"] = () => new ContextMenu { ItemsSource = new[] { new MenuItem { Header = "a" } } },
        ["MenuScrollViewer"] = () => new ScrollViewer { Content = new TextBlock { Text = "x" }, Theme = (ControlTheme)Application.Current!.FindResource("WinMenuScrollViewer")! },
        ["Separator"] = () => new Separator(),
        ["ThemeVariantScope"] = () => new ThemeVariantScope { Child = new TextBlock { Text = "x" } },
        ["TextSelectionHandle"] = () => new TextSelectionHandle(),
        // Containers and navigation
        ["ScrollViewer"] = () => new ScrollViewer { Content = new Border { Width = 1000, Height = 1000 }, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto },
        ["ScrollBar"] = () => new ScrollBar { Maximum = 100, ViewportSize = 10 },
        ["ItemsControl"] = () => new ItemsControl { ItemsSource = new[] { "a" } },
        ["HeaderedContentControl"] = () => new HeaderedContentControl { Header = "h", Content = "c" },
        ["GridSplitter"] = () => new GridSplitter(),
        ["GroupBox"] = () => new GroupBox { Header = "h", Content = "c" },
        ["Expander"] = () => new Expander { Header = "h", Content = "c" },
        ["SplitView"] = () => new SplitView { Pane = new Border(), Content = new Border(), IsPaneOpen = true },
        ["TabControl"] = () => new TabControl { ItemsSource = new[] { new TabItem { Header = "a", Content = "x" }, new TabItem { Header = "b" } } },
        ["TabItem"] = () => new TabItem { Header = "h", Content = "c" },
        ["TabStrip"] = () => new TabStrip { ItemsSource = new[] { "a", "b" } },
        ["TabStripItem"] = () => new TabStripItem { Content = "x" },
        ["TransitioningContentControl"] = () => new TransitioningContentControl { Content = "x" },
        ["Carousel"] = () => new Carousel { ItemsSource = new[] { "a", "b" } },
        ["PipsPager"] = () => new PipsPager { NumberOfPages = 3 },
        ["RefreshContainer"] = () => new RefreshContainer { Content = new Border() },
        ["RefreshVisualizer"] = () => new RefreshVisualizer(),
        ["TreeView"] = () => new TreeView { ItemsSource = new[] { "a" } },
        ["TreeViewItem"] = () => new TreeView { Items = { new TreeViewItem { Header = "h", ItemsSource = new[] { "child" }, IsExpanded = true } } },
        ["DataValidationErrors"] = () => new DataValidationErrors { Content = new TextBox() },
        // Date and time
        ["DatePicker"] = () => new DatePicker { SelectedDate = new System.DateTimeOffset(2014, 4, 8, 0, 0, 0, System.TimeSpan.Zero) },
        ["TimePicker"] = () => new TimePicker { SelectedTime = new System.TimeSpan(9, 30, 0) },
        ["DateTimePickerShared"] = () => new ListBoxItem { Content = "x", Theme = (ControlTheme)Application.Current!.FindResource("WinDateTimePickerItem")! },
        ["Calendar"] = () => new Calendar { DisplayDate = new System.DateTime(2014, 4, 8), SelectedDate = new System.DateTime(2014, 4, 8) },
        ["CalendarItem"] = () => new Calendar { DisplayDate = new System.DateTime(2014, 4, 8) },
        ["CalendarButton"] = () => new CalendarButton { Content = "Apr" },
        ["CalendarDayButton"] = () => new CalendarDayButton { Content = "8" },
        ["CalendarDatePicker"] = () => new CalendarDatePicker { SelectedDate = new System.DateTime(2014, 4, 8) },
        // Commands, pages, notifications, tables
        ["CommandBar"] = () => new CommandBar { PrimaryCommands = { new CommandBarButton { Label = "Add", Icon = "\uE109" }, new CommandBarToggleButton { Label = "Fav", Icon = "\uE113", IsChecked = true }, new CommandBarSeparator(), new CommandBarButton { Label = "Del" } }, SecondaryCommands = { new CommandBarButton { Label = "More" } } },
        ["ContentPage"] = () => new ContentPage { Content = "x" },
        ["NavigationPage"] = () => { var n = new NavigationPage(); n.PushAsync(new ContentPage { Header = "Root", Content = "x" }); n.PushAsync(new ContentPage { Header = "Page", Content = "y" }); return n; },
        ["DrawerPage"] = () => new DrawerPage { Drawer = new ContentPage { Content = "d" }, Content = new ContentPage { Content = "x" } },
        ["TabbedPage"] = () => new TabbedPage { Pages = new[] { new ContentPage { Header = "a", Content = "x" }, new ContentPage { Header = "b" } } },
        ["CarouselPage"] = () => new CarouselPage { Pages = new[] { new ContentPage { Content = "x" }, new ContentPage { Content = "y" } } },
        ["NotificationCard"] = () => new NotificationCard { Content = "x" },
        ["WindowNotificationManager"] = () => new WindowNotificationManager(),
        ["ManagedFileChooser"] = () => new Avalonia.Dialogs.ManagedFileChooser(),
        ["TableView"] = () => new TableView { Columns = new Avalonia.Collections.AvaloniaList<TableViewColumn> { new TableViewColumn { Header = "Name" } }, ItemsSource = new[] { "a", "b" } },
        ["TableViewRow"] = () => new TableViewRow { Content = "x" },
        ["TableViewCell"] = () => new TableViewCell { Content = "x" },
        ["TableViewColumnHeader"] = () => new TableViewColumnHeader { Content = "x" },
    };

    /// <summary>Required PART names per control (from the skill's control-checklist.md).</summary>
    public static readonly Dictionary<string, string[]> RequiredParts = new()
    {
        ["Button"] = ["PART_ContentPresenter", "PART_FocusVisual"],
        ["ToggleButton"] = ["PART_ContentPresenter"],
        ["RepeatButton"] = ["PART_ContentPresenter"],
        ["TextBox"] = ["PART_BorderElement", "PART_TextPresenter", "PART_ScrollViewer", "PART_InnerDockPanel", "PART_FloatingPlaceholder"],
        ["ListBoxItem"] = ["PART_ContentPresenter", "PART_HoverOutline", "PART_SelectionBorder", "PART_SelectionCheckmark", "PART_FocusOutline"],
        ["HyperlinkButton"] = ["PART_ContentPresenter"],
        ["DropDownButton"] = ["PART_ContentPresenter"],
        ["SplitButton"] = ["PART_PrimaryButton", "PART_SecondaryButton"],
        ["ListBox"] = ["PART_ScrollViewer", "PART_ItemsPresenter"],
        ["CheckBox"] = ["PART_ContentPresenter", "PART_Border", "NormalRectangle", "CheckGlyph"],
        ["RadioButton"] = ["PART_ContentPresenter", "OuterEllipse", "CheckOuterEllipse", "CheckGlyph"],
        ["ToggleSwitch"] = ["PART_MovingKnobs", "PART_SwitchKnob", "PART_ContentPresenter", "PART_OnContentPresenter", "PART_OffContentPresenter"],
        ["Slider"] = ["PART_Track", "PART_DecreaseButton", "PART_IncreaseButton"],
        ["ProgressBar"] = ["PART_Indicator"],
        ["ComboBox"] = ["PART_Popup", "PART_ItemsPresenter", "PART_EditableTextBox"],
        ["ComboBoxItem"] = ["PART_ContentPresenter"],
        ["NumericUpDown"] = ["PART_Spinner", "PART_TextBox"],
        ["ButtonSpinner"] = ["PART_IncreaseButton", "PART_DecreaseButton", "PART_ContentPresenter", "PART_SpinnerPanel"],
        ["AutoCompleteBox"] = ["PART_TextBox", "PART_Popup", "PART_SelectingItemsControl"],
        ["Label"] = ["PART_ContentPresenter"],
        ["ToolTip"] = ["PART_LayoutRoot", "PART_ContentPresenter"],
        ["FlyoutPresenter"] = ["PART_ContentPresenter"],
        ["MenuFlyoutPresenter"] = ["PART_ItemsPresenter"],
        ["Menu"] = ["PART_ItemsPresenter"],
        ["MenuItem"] = ["PART_LayoutRoot", "PART_HeaderPresenter", "PART_Popup", "PART_ItemsPresenter", "PART_ToggleIconPresenter", "PART_IconPresenter", "PART_InputGestureText", "PART_ChevronPath"],
        ["ContextMenu"] = ["PART_ItemsPresenter"],
        ["TextSelectionHandle"] = ["PART_HandlePathIcon", "PART_Indicator"],
        ["ScrollViewer"] = ["PART_ContentPresenter", "PART_HorizontalScrollBar", "PART_VerticalScrollBar", "PART_ScrollBarsSeparator"],
        ["ScrollBar"] = ["PART_LineUpButton", "PART_LineDownButton", "PART_PageUpButton", "PART_PageDownButton"],
        ["ItemsControl"] = ["PART_ItemsPresenter"],
        ["HeaderedContentControl"] = ["PART_HeaderPresenter", "PART_ContentPresenter"],
        ["GroupBox"] = ["PART_HeaderPresenter", "PART_ContentPresenter"],
        ["Expander"] = ["PART_ContentPresenter", "ExpanderHeader"],
        ["SplitView"] = ["PART_PaneRoot", "PART_PanePresenter", "PART_ContentPresenter"],
        ["TabControl"] = ["PART_ItemsPresenter", "PART_SelectedContentHost", "PART_SelectedContentHost2"],
        ["TabItem"] = ["PART_LayoutRoot", "PART_ContentPresenter", "PART_SelectedPipe"],
        ["TabStrip"] = ["PART_ItemsPresenter"],
        ["TabStripItem"] = ["PART_LayoutRoot", "PART_ContentPresenter"],
        ["TransitioningContentControl"] = ["PART_ContentPresenter", "PART_ContentPresenter2"],
        ["Carousel"] = ["PART_ScrollViewer", "PART_ItemsPresenter"],
        ["PipsPager"] = ["PART_RootPanel", "PART_PreviousButton", "PART_NextButton", "PART_PipsPagerList"],
        ["RefreshContainer"] = ["PART_ContentPresenter", "PART_RefreshVisualizerPresenter"],
        ["RefreshVisualizer"] = ["PART_Root"],
        ["TreeView"] = ["PART_ItemsPresenter"],
        ["TreeViewItem"] = ["PART_LayoutRoot", "PART_Header", "PART_HeaderPresenter", "PART_ItemsPresenter", "PART_ExpandCollapseChevron"],
        ["DataValidationErrors"] = ["PART_ContentPresenter"],
        ["DatePicker"] = ["PART_FlyoutButton", "PART_ButtonContentGrid", "PART_DayTextBlock", "PART_MonthTextBlock", "PART_YearTextBlock", "PART_FirstSpacer", "PART_SecondSpacer", "PART_Popup", "PART_PickerPresenter"],
        ["TimePicker"] = ["PART_FlyoutButton", "PART_FlyoutButtonContentGrid", "PART_HourTextBlock", "PART_MinuteTextBlock", "PART_PeriodTextBlock", "PART_Popup", "PART_PickerPresenter"],
        ["Calendar"] = ["PART_Root", "PART_CalendarItem"],
        ["CalendarItem"] = ["PART_HeaderButton", "PART_PreviousButton", "PART_NextButton", "PART_MonthView", "PART_YearView"],
        ["CalendarButton"] = ["PART_ContentPresenter"],
        ["CalendarDayButton"] = ["PART_ContentPresenter"],
        ["CalendarDatePicker"] = ["PART_TextBox", "PART_Button", "PART_Popup", "PART_Calendar"],
        ["CommandBar"] = ["PART_ContentPresenter", "PART_OverflowButton", "PART_OverflowPopup", "PART_OverflowPresenter", "PART_PrimaryCommands", "PART_Border", "PART_IconPresenter", "PART_Label", "PART_LayoutRoot"],
        ["ContentPage"] = ["PART_TopCommandBar", "PART_BottomCommandBar", "PART_ContentPresenter"],
        ["NavigationPage"] = ["PART_BackButton", "PART_BackButtonContentPresenter", "PART_BackButtonDefaultIcon", "PART_BottomCommandBar", "PART_ContentHost", "PART_ModalBackPresenter", "PART_ModalPresenter", "PART_NavBarShadow", "PART_NavigationBar", "PART_PageBackPresenter", "PART_PagePresenter", "PART_TopCommandBar"],
        ["DrawerPage"] = ["PART_Backdrop", "PART_CompactPaneToggle", "PART_ContentPresenter", "PART_DrawerFooter", "PART_DrawerHeader", "PART_DrawerPresenter", "PART_PaneButton", "PART_SplitView", "PART_TopBar"],
        ["NotificationCard"] = ["PART_LayoutTransformControl", "PART_HeaderBar", "PART_ContentPresenter"],
        ["WindowNotificationManager"] = ["PART_Items"],
        ["ManagedFileChooser"] = ["PART_QuickLinks"],
    };

    /// <summary>Keys declared inside a template's own Resources (not reachable from the control) — skipped by the resolver check.</summary>
    public static readonly Dictionary<string, string[]> TemplateScopedKeys = new()
    {
        ["ManagedFileChooser"] = ["Icons"],
    };

    /// <summary>Controls whose Background is transparent/null by design.</summary>
    public static readonly HashSet<string> TransparentByDesign = new()
    {
        "TextBlock",
        "HyperlinkButton", // transparent: a link has no face
        "ListBox",         // transparent: a list has no chrome
        "CheckBox", "RadioButton", "ToggleSwitch", "Slider", "ComboBoxItem", "SelectableTextBlock", "Label", "PathIcon",
        "MenuItem", "MenuScrollViewer", "ThemeVariantScope", "AdornerLayer",
        "ScrollViewer", "ScrollBar", "ItemsControl", "HeaderedContentControl", "GridSplitter", "GroupBox", "Expander", "SplitView",
        "TabControl", "TabItem", "TabStrip", "TabStripItem", "TransitioningContentControl", "Carousel", "PipsPager",
        "RefreshContainer", "RefreshVisualizer", "TreeView", "TreeViewItem", "DataValidationErrors",
        "DateTimePickerShared", "CalendarItem", "CalendarButton", "CalendarDayButton",
        "ContentPage", "NavigationPage", "DrawerPage", "TabbedPage", "CarouselPage", "WindowNotificationManager", "ManagedFileChooser",
        "TableView", "TableViewRow", "TableViewCell", "TableViewColumnHeader",
    };

    public static TheoryData<string> Names => new(Factories.Keys);

    /// <summary>name × {Light, Dark} × {Desktop, Phone}.</summary>
    public static TheoryData<string, string, Platform> Matrix
    {
        get
        {
            var data = new TheoryData<string, string, Platform>();
            foreach (var name in Factories.Keys)
            {
                foreach (var variant in ThemeTestHelpers.Variants)
                {
                    foreach (var platform in ThemeTestHelpers.Platforms)
                    {
                        data.Add(name, variant, platform);
                    }
                }
            }

            return data;
        }
    }
}
