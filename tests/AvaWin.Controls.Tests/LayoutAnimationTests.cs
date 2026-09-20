using System.Collections.Generic;
using System.Collections.ObjectModel;
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

public class LayoutAnimationTests
{
    [AvaloniaFact]
    public async Task AddToList_And_DeleteFromList_Run_To_Completion()
    {
        AvaWin.Animations.WinAnimations.TimeScale = 0;
        var panel = new StackPanel();
        var a = new Border { Height = 20 };
        var b = new Border { Height = 20 };
        panel.Children.Add(a);
        panel.Children.Add(b);
        var window = ThemeTestHelpers.Host(panel, "Light", Platform.Desktop);
        var added = new Border { Height = 20 };
        var anim = AvaWin.Animations.WinAnimations.CreateAddToListAnimation(added, new[] { b });
        panel.Children.Insert(1, added);
        await anim.ExecuteAsync();
        Assert.Equal(1, added.Opacity);
        Assert.Null(added.RenderTransform);
        var del = AvaWin.Animations.WinAnimations.CreateDeleteFromListAnimation(added, new[] { b });
        await del.ExecuteAsync();
        Assert.Equal(0, added.Opacity);
        window.Close();
    }
}
