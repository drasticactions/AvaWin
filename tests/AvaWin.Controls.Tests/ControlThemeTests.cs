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

namespace AvaWin.Controls.Tests;

public class ControlThemeTests
{
    private static HashSet<string?> PartNames(Control control) => PartFinder.Collect(control);

    [AvaloniaTheory]
    [MemberData(nameof(ThemedControls.Matrix), MemberType = typeof(ThemedControls))]
    public void Templates_And_Resolves_Keys(string name, string variant, Platform platform)
    {
        var control = ThemedControls.Factories[name]();
        var window = ThemeTestHelpers.Host(control, variant, platform);

        Assert.True(control.GetVisualChildren().Any(), $"{name} produced an empty visual tree under {variant}/{platform}.");

        if (ThemedControls.RequiredParts.TryGetValue(name, out var parts))
        {
            var present = PartNames(control);
            foreach (var part in parts)
            {
                Assert.True(present.Contains(part), $"{name} template is missing {part}.");
            }
        }

        var theme = ThemeTestHelpers.ToVariant(variant);
        foreach (var key in ThemeTestHelpers.KeysReferencedBy(name))
        {
            Assert.True(control.TryFindResource(key, theme, out var value) && value is not null,
                $"{name}: resource '{key}' did not resolve under {variant}/{platform}.");
        }

        if (control is TemplatedControl templated && !ThemedControls.TransparentByDesign.Contains(name))
        {
            Assert.True(templated.Background is not null, $"{name}.Background did not resolve under {variant}/{platform}.");
        }

        control.IsEnabled = false;
        window.UpdateLayout();
        control.IsEnabled = true;
        window.UpdateLayout();
        Assert.True(control.GetVisualChildren().Any());
        window.Close();
    }
}

public class ControlsThemeTests
{
    [AvaloniaFact]
    public void Controls_Theme_Registers_Overlay_With_Theme()
    {
        var controls = Application.Current!.Styles.OfType<AvaWinControlsTheme>().Single();
        Assert.Same(TestApplication.Instance.Theme, controls.Theme);
    }

    [AvaloniaFact]
    public void SymbolIcon_Resolves_Glyph_From_Icon_And_String()
    {
        var icon = new SymbolIcon { Icon = AppBarIcon.MailReplyAll };
        Assert.Equal("\uE165", icon.ActualGlyph);
        icon.Icon = null;
        icon.Glyph = WinSymbol.Back;
        Assert.Equal("\uE0D5", icon.ActualGlyph);
        var window = ThemeTestHelpers.Host(icon, "Light", Platform.Desktop);
        var text = icon.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("\uE0D5", text.Text);
        Assert.True(text.Bounds.Width > 0);
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
