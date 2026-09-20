using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Theme.Tests;

public class ControlThemeTests
{
    private static HashSet<string?> PartNames(Control control) => PartFinder.Collect(control);

    [AvaloniaTheory]
    [MemberData(nameof(ThemedControls.Matrix), MemberType = typeof(ThemedControls))]
    public void Templates_And_Resolves_Keys(string name, string variant, Platform platform)
    {
        var control = ThemedControls.Factories[name]();
        var window = ThemeTestHelpers.Host(control, variant, platform);

        // 1. Non-empty visual tree.
        Assert.True(control.GetVisualChildren().Any() || control is TextBlock,
            $"{name} produced an empty visual tree under {variant}/{platform}.");

        // 2. Required PARTs.
        if (ThemedControls.RequiredParts.TryGetValue(name, out var parts))
        {
            var present = PartNames(control);
            foreach (var part in parts)
            {
                Assert.True(present.Contains(part), $"{name} template is missing {part}.");
            }
        }

        // 3. Every key the theme file references resolves in this variant/platform.
        var theme = ThemeTestHelpers.ToVariant(variant);
        var scoped = ThemedControls.TemplateScopedKeys.TryGetValue(name, out var sk) ? sk : Array.Empty<string>();
        foreach (var key in ThemeTestHelpers.KeysReferencedBy("ThemeXaml", name))
        {
            if (scoped.Contains(key))
            {
                continue;
            }

            Assert.True(control.TryFindResource(key, theme, out var value) && value is not null,
                $"{name}: resource '{key}' did not resolve under {variant}/{platform}.");
        }

        // 4. Background resolves (proof the theme applied).
        if (control is TemplatedControl templated && !ThemedControls.TransparentByDesign.Contains(name))
        {
            Assert.True(templated.Background is not null,
                $"{name}.Background did not resolve under {variant}/{platform}.");
        }

        // 5. State walk.
        control.IsEnabled = false;
        window.UpdateLayout();
        control.IsEnabled = true;
        StateWalk.Apply(control, window);
        Assert.True(control.GetVisualChildren().Any() || control is TextBlock);
        window.Close();
    }
}

/// <summary>Names of every named element in the control's template, including the children of closed popups.</summary>
public static class PartFinder
{
    public static HashSet<string?> Collect(Control control)
    {
        var names = new HashSet<string?>();
        void Walk(StyledElement e)
        {
            names.Add(e.Name);
            if (e is Visual v)
            {
                foreach (var child in v.GetVisualDescendants().OfType<StyledElement>())
                {
                    names.Add(child.Name);
                    if (child is Avalonia.Controls.Primitives.Popup { Child: StyledElement pc })
                    {
                        Walk(pc);
                    }
                }
            }

            foreach (var child in e.GetLogicalDescendants().OfType<StyledElement>())
            {
                names.Add(child.Name);
                if (child is Avalonia.Controls.Primitives.Popup { Child: StyledElement pc })
                {
                    Walk(pc);
                }
            }
        }

        Walk(control);
        return names;
    }
}

/// <summary>Control-specific state toggles.</summary>
public static class StateWalk
{
    /// <summary>Popups need the Window template's OverlayLayer, which the Window theme provides.</summary>
    public static bool PopupsSupported => true;

    public static void Apply(Control control, Window window)
    {
        switch (control)
        {
            case ToggleButton tb:
                tb.IsChecked = true; window.UpdateLayout();
                tb.IsChecked = null; window.UpdateLayout();
                tb.IsChecked = false; window.UpdateLayout();
                break;
            case SelectingItemsControl sic when sic.ItemCount > 0:
                sic.SelectedIndex = 0; window.UpdateLayout();
                break;
        }

        if (control is ListBoxItem lbi)
        {
            lbi.IsSelected = true; window.UpdateLayout();
        }

        switch (control)
        {
            case ComboBoxItem cbi:
                cbi.IsSelected = true; window.UpdateLayout();
                break;
            case ComboBox cb when PopupsSupported:
                cb.IsDropDownOpen = true; window.UpdateLayout();
                cb.IsDropDownOpen = false; window.UpdateLayout();
                break;
            case ProgressBar pb:
                pb.IsIndeterminate = true; window.UpdateLayout();
                pb.Classes.Add("ring"); window.UpdateLayout();
                pb.Classes.Add("large"); window.UpdateLayout();
                break;
            case Slider s:
                s.Orientation = Avalonia.Layout.Orientation.Vertical; window.UpdateLayout();
                s.TickPlacement = TickPlacement.Outside; window.UpdateLayout();
                break;
            case AutoCompleteBox acb when PopupsSupported:
                acb.Text = "a"; window.UpdateLayout();
                acb.IsDropDownOpen = true; window.UpdateLayout();
                acb.IsDropDownOpen = false; window.UpdateLayout();
                break;
            case Expander ex:
                ex.IsExpanded = true; window.UpdateLayout();
                ex.ExpandDirection = ExpandDirection.Left; window.UpdateLayout();
                break;
            case SplitView sv:
                sv.DisplayMode = SplitViewDisplayMode.Overlay; window.UpdateLayout();
                sv.PanePlacement = SplitViewPanePlacement.Right; window.UpdateLayout();
                sv.PanePlacement = SplitViewPanePlacement.Top; window.UpdateLayout();
                sv.PanePlacement = SplitViewPanePlacement.Bottom; window.UpdateLayout();
                break;
            case ScrollBar sb:
                sb.Orientation = Avalonia.Layout.Orientation.Horizontal; window.UpdateLayout();
                break;
            case TreeView { Items: [TreeViewItem tvi] }:
                tvi.IsExpanded = false; window.UpdateLayout();
                tvi.IsSelected = true; window.UpdateLayout();
                break;
            case PipsPager pp:
                pp.Orientation = Avalonia.Layout.Orientation.Vertical; window.UpdateLayout();
                break;
            case CalendarDatePicker cdp:
                cdp.IsDropDownOpen = true; window.UpdateLayout();
                cdp.IsDropDownOpen = false; window.UpdateLayout();
                break;
            case Calendar cal:
                cal.DisplayMode = CalendarMode.Year; window.UpdateLayout();
                cal.DisplayMode = CalendarMode.Decade; window.UpdateLayout();
                break;
            case CommandBar cbar:
                cbar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed; window.UpdateLayout();
                cbar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right; window.UpdateLayout();
                cbar.IsOpen = true; window.UpdateLayout();
                cbar.IsOpen = false; window.UpdateLayout();
                break;
            case MenuItem mi:
                mi.Classes.Add("x"); window.UpdateLayout();
                break;
            case ToolTip tt:
                tt.Classes.Add("x"); window.UpdateLayout();
                break;
            case TextBox tb:
                tb.AcceptsReturn = true; window.UpdateLayout();
                tb.Classes.Add("error"); window.UpdateLayout();
                break;
        }
    }
}
