using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Controls.Tests;

public class ItemContainerTests
{
    [AvaloniaFact]
    public void Tap_Behaviours_And_Selection_Events()
    {
        var container = new ItemContainer { Content = "Item", TapBehavior = TapBehavior.ToggleSelect, Width = 200 };
        var invoked = 0;
        var changed = 0;
        container.Invoked += (_, _) => invoked++;
        container.SelectionChanged += (_, _) => changed++;
        var window = ThemeTestHelpers.Host(container, "Light", Platform.Desktop);
        AppBarTests.Click(window, container);
        Assert.True(container.IsSelected);
        Assert.Equal(1, invoked);
        Assert.Equal(1, changed);
        Assert.Contains(":selected", container.Classes);

        container.SelectionChanging += (_, e) => e.Cancel = true;
        AppBarTests.Click(window, container);
        Assert.True(container.IsSelected, "canceled SelectionChanging keeps the state");
        Assert.Equal(2, invoked);

        container.IsSelectionDisabled = true;
        Assert.Contains(":nonselectable", container.Classes);
        Assert.Equal(0, container.Margin.Left);
        window.Close();
    }

    [AvaloniaFact]
    public void Filled_Style_Uses_Attached_Property()
    {
        var container = new ItemContainer { Content = "Item", SelectionStyle = ListViewSelectionStyle.Filled };
        var window = ThemeTestHelpers.Host(container, "Light", Platform.Desktop);
        Assert.Equal(SelectionStyle.Filled, ListStyle.GetSelectionStyle(container));
        window.Close();
    }
}
