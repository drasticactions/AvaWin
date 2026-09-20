using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaWin.Animations;
using Xunit;

namespace AvaWin.Controls.Tests;

public class ContentDialogTests
{
    private static (Window window, ContentDialog dialog) Make(string? primary = "Delete", string? secondary = "Cancel", double width = 600, double height = 400)
    {
        WinAnimations.TimeScale = 0;
        var dialog = new ContentDialog { Title = "Delete this album?", Content = new TextBlock { Text = "It will be removed from every device." }, PrimaryCommandText = primary, SecondaryCommandText = secondary };
        var window = ThemeTestHelpers.Host(new Grid { Children = { new Button { Content = "behind" }, dialog } }, "Light", Platform.Desktop, width, height);
        return (window, dialog);
    }

    private static Button Command(ContentDialog d, string name) => d.Dialog.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name);

    private static Border Panel(ContentDialog d) => d.Dialog.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Dialog");

    [AvaloniaFact]
    public async Task Show_Then_Primary_Command_Completes_With_Primary()
    {
        var (window, dialog) = Make();
        var events = new List<string>();
        dialog.BeforeShow += (_, _) => events.Add("beforeshow");
        dialog.AfterShow += (_, _) => events.Add("aftershow");
        dialog.BeforeHide += (_, e) => events.Add("beforehide:" + e.Result);
        dialog.AfterHide += (_, e) => events.Add("afterhide:" + e.Result);
        Assert.False(dialog.Dialog.IsVisible);

        var task = dialog.ShowAsync();
        window.UpdateLayout();
        Assert.True(dialog.IsOpen);
        Assert.True(dialog.Dialog.IsVisible);
        Assert.Equal(window.Bounds.Size, dialog.Dialog.Bounds.Size);
        await Assert.ThrowsAsync<System.InvalidOperationException>(() => dialog.ShowAsync());

        Command(dialog, "PART_PrimaryCommand").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(ContentDialogResult.Primary, await task);
        Assert.False(dialog.IsOpen);
        Assert.False(dialog.Dialog.IsVisible);
        Assert.Equal(["beforeshow", "aftershow", "beforehide:Primary", "afterhide:Primary"], events);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Escape_Dismisses_With_None_And_BeforeHide_Can_Cancel()
    {
        var (window, dialog) = Make();
        var task = dialog.ShowAsync();
        window.UpdateLayout();
        var block = true;
        dialog.BeforeHide += (_, e) => e.Cancel = block;
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        await Task.Delay(50);
        Assert.True(dialog.IsOpen);
        block = false;
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.Equal(ContentDialogResult.None, await task);
        window.Close();
    }

    [AvaloniaFact]
    public async Task BeforeShow_Cancel_Never_Shows()
    {
        var (window, dialog) = Make();
        dialog.BeforeShow += (_, e) => e.Cancel = true;
        Assert.Equal(ContentDialogResult.None, await dialog.ShowAsync());
        Assert.False(dialog.IsOpen);
        Assert.False(dialog.Dialog.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Layout_Follows_The_WinJS_Rules()
    {
        // 600×400: too short to center vertically → attached to the top; width sized to the window, clamped to 456.
        var (window, dialog) = Make();
        _ = dialog.ShowAsync();
        window.UpdateLayout();
        var panel = Panel(dialog);
        Assert.Equal(456, panel.Bounds.Width, 0.5);
        Assert.Equal((600 - 456) / 2.0, panel.Bounds.X, 0.5);
        Assert.Equal(0, panel.Bounds.Y, 0.5);
        Assert.True(panel.Bounds.Height >= 184);
        // Two commands share the row; a lone primary command sits in the right half.
        var primary = Command(dialog, "PART_PrimaryCommand");
        var secondary = Command(dialog, "PART_SecondaryCommand");
        Assert.True(secondary.IsVisible);
        Assert.Equal(primary.Bounds.Width, secondary.Bounds.Width, 0.5);
        Assert.True(secondary.Bounds.X > primary.Bounds.X);
        dialog.SecondaryCommandText = null;
        window.UpdateLayout();
        Assert.False(secondary.IsVisible);
        Assert.Equal(1, Grid.GetColumn(primary));
        window.Close();

        // 800×700: tall enough to center.
        var (tall, dialog2) = Make(width: 800, height: 700);
        _ = dialog2.ShowAsync();
        tall.UpdateLayout();
        var panel2 = Panel(dialog2);
        Assert.True(panel2.Bounds.Y > 100);
        Assert.Equal((700 - panel2.Bounds.Height) / 2, panel2.Bounds.Y, 1);
        tall.Close();
    }

    [AvaloniaFact]
    public void Focus_Moves_Into_The_Dialog_And_Back()
    {
        var (window, dialog) = Make();
        var behind = window.GetVisualDescendants().OfType<Button>().First(b => Equals(b.Content, "behind"));
        behind.Focus();
        _ = dialog.ShowAsync();
        window.UpdateLayout();
        var focused = window.FocusManager!.GetFocusedElement() as Visual;
        Assert.NotNull(focused);
        Assert.True(dialog.Dialog.IsVisualAncestorOf(focused!), "focus is inside the dialog");
        dialog.Hide(ContentDialogResult.Secondary);
        window.UpdateLayout();
        Assert.Same(behind, window.FocusManager!.GetFocusedElement());
        window.Close();
    }
}
