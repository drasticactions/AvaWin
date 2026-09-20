using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Xunit;

namespace AvaWin.Controls.Tests;

/// <summary>One row per custom control; key = theme file name under Themes/.</summary>
public static class ThemedControls
{
    public static readonly Dictionary<string, Func<Control>> Factories = new()
    {
        ["SymbolIcon"] = () => new SymbolIcon { Icon = AppBarIcon.Accept },
        ["AppBar"] = () =>
        {
            var presenter = new AppBarPresenter();
            presenter.Commands.AddRange(new[]
            {
                new AppBarCommand { Label = "Add", Icon = AppBarIcon.Add },
                new AppBarCommand { Label = "Fav", Icon = AppBarIcon.Favorite, Type = AppBarCommandType.Toggle, IsSelected = true },
                new AppBarCommand { Type = AppBarCommandType.Separator },
                new AppBarCommand { Label = "Del", Icon = AppBarIcon.Delete, Section = AppBarCommandSection.Selection },
            });
            return presenter;
        },
        ["AppBarCommand"] = () => new AppBarCommand { Label = "Add", Icon = AppBarIcon.Add },
        ["BackButton"] = () => new BackButton { IsAutoEnabled = false },
        ["SettingsFlyout"] = () => new SettingsFlyoutPresenter { Header = "Options", Content = new TextBlock { Text = "x" } },
        ["ContentDialog"] = () => new ContentDialogPresenter { Header = "Delete?", Content = new TextBlock { Text = "x" }, PrimaryCommandText = "Delete", SecondaryCommandText = "Cancel" },
        ["NavBar"] = () =>
        {
            var c = new NavBarContainer();
            c.Items.Add(new NavBarCommand { Label = "Home", Icon = AppBarIcon.Home });
            c.Items.Add(new NavBarCommand { Label = "Split", Icon = AppBarIcon.Favorite, SplitButton = true, SplitOpened = true });
            return c;
        },
        ["NavBarCommand"] = () => new NavBarCommand { Label = "Home", Icon = AppBarIcon.Home, SplitButton = true },
        ["NavBarPresenter"] = () => new NavBarPresenter { Content = new TextBlock { Text = "nav" } },
        ["Hub"] = () => new Hub { Items = { new HubSection { Header = "One", Content = new TextBlock { Text = "a" }, Width = 200 }, new HubSection { Header = "Two", IsHeaderStatic = true, Content = new TextBlock { Text = "b" }, Width = 200 } } },
        ["HubSection"] = () => new HubSection { Header = "One", Content = new TextBlock { Text = "a" } },
        ["Pivot"] = () => new Pivot { Title = "PIVOT", Items = { new PivotItem { Header = "first", Content = new TextBlock { Text = "a" } }, new PivotItem { Header = "second", Content = new TextBlock { Text = "b" } } } },
        ["PivotItem"] = () => new PivotItem { Header = "first", Content = new TextBlock { Text = "a" } },
        ["ListView"] = () =>
        {
            var lv = new ListView { ItemsSource = new[] { "Apple", "Banana", "Cherry" }, GroupKeySelector = o => o?.ToString()?[0].ToString(), Height = 300 };
            lv.ContentAnimating += (_, e) => e.Cancel = true;
            return lv;
        },
        ["ListViewItem"] = () => new ListViewItem { Content = "Item", IsSelected = true },
        ["ListViewGroupHeader"] = () => new ListViewGroupHeader { Content = "A" },
        ["FlipView"] = () => new FlipView { ItemsSource = new[] { "one", "two" }, Height = 200 },
        ["ItemContainer"] = () => new ItemContainer { Content = "Item", IsSelected = true },
        ["Rating"] = () => new Rating { UserRating = 3 },
        ["RatingStar"] = () => new RatingStar { State = RatingStarState.AverageFractional, Fraction = 0.5 },
        ["SearchBox"] = () => new SearchBox { PlaceholderText = "Search" },
        ["SearchSuggestionItem"] = () => new SearchSuggestionItem { Suggestion = new SearchSuggestion(SearchSuggestionKind.Result, "Result", "detail") },
        ["SemanticZoom"] = () => new SemanticZoom
        {
            ZoomedInView = new ListView { ItemsSource = new[] { "a", "b" }, Height = 200 },
            ZoomedOutView = new ListView { ItemsSource = new[] { "A" }, Height = 200 },
            Height = 200,
        },
    };

    public static readonly Dictionary<string, string[]> RequiredParts = new()
    {
        ["SymbolIcon"] = ["PART_Glyph"],
        ["AppBar"] = ["PART_Root", "PART_Body", "PART_CommandsHost", "PART_ContentPresenter", "PART_InvokeButton"],
        ["AppBarCommand"] = ["PART_Root", "PART_Icon", "PART_Label"],
        ["BackButton"] = ["PART_Root", "PART_ContentPresenter"],
        ["SettingsFlyout"] = ["PART_Root", "PART_Body", "PART_Header", "PART_BackButton", "PART_HeaderPresenter", "PART_Content", "PART_ContentPresenter"],
        ["NavBar"] = ["PART_PageIndicators", "PART_Viewport", "PART_LeftArrow", "PART_RightArrow"],
        ["NavBarCommand"] = ["PART_Button", "PART_SplitButton", "PART_Icon", "PART_Label"],
        ["NavBarPresenter"] = ["PART_Root", "PART_Body", "PART_ContentPresenter", "PART_InvokeButton"],
        ["Hub"] = ["PART_Viewport", "PART_Progress", "PART_HeaderButton", "PART_Chevron", "PART_StaticHeader"],
        ["HubSection"] = ["PART_HeaderButton", "PART_Chevron", "PART_StaticHeader", "PART_ContentPresenter"],
        ["Pivot"] = ["PART_Title", "PART_Headers", "PART_HeadersPanel", "PART_PrevButton", "PART_NextButton", "PART_Viewport", "PART_ItemHost"],
        ["PivotItem"] = ["PART_ContentPresenter"],
        ["ListView"] = ["PART_ScrollViewer", "PART_ItemsPresenter", "PART_Progress", "PART_Container", "PART_SelectionBorder", "PART_Checkmark", "PART_HoverOutline", "PART_FocusOutline"],
        ["ListViewItem"] = ["PART_Container", "PART_SelectionBackground", "PART_ContentPresenter", "PART_SelectionBorder", "PART_Checkmark", "PART_HoverOutline", "PART_FocusOutline"],
        ["ListViewGroupHeader"] = ["PART_ContentPresenter", "PART_FocusOutline"],
        ["FlipView"] = ["PART_Viewport", "PART_PageA", "PART_PageB", "PART_PreviousButton", "PART_NextButton"],
        ["ItemContainer"] = ["PART_Container", "PART_ContentPresenter", "PART_SelectionBorder", "PART_Checkmark"],
        ["SemanticZoom"] = ["PART_ZoomedInHost", "PART_ZoomedOutHost", "PART_ZoomOutButton"],
        ["Rating"] = ["PART_Stars", "PART_Empty", "PART_Full", "PART_Clip"],
        ["RatingStar"] = ["PART_Empty", "PART_Full", "PART_Clip"],
        ["SearchBox"] = ["PART_Input", "PART_Button", "PART_Flyout", "PART_Suggestions"],
        ["SearchSuggestionItem"] = ["PART_Root", "PART_Query", "PART_Result", "PART_Separator"],
    };

    public static readonly HashSet<string> TransparentByDesign = new() { "SymbolIcon", "AppBarCommand", "NavBarCommand", "HubSection", "PivotItem", "ListViewGroupHeader", "RatingStar", "SearchSuggestionItem" };

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
