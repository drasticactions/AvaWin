using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWin.Controls.Tests;

public class RatingTests
{
    private static (Window window, Rating rating) Make(int user = 0, double average = 0, bool enableClear = true)
    {
        var rating = new Rating { UserRating = user, AverageRating = average, EnableClear = enableClear };
        var window = ThemeTestHelpers.Host(new Border { Padding = new Thickness(20), Child = rating }, "Light", Platform.Desktop);
        return (window, rating);
    }

    private static Point StarCenter(Window window, Rating rating, int index)
    {
        var star = rating.Stars[index];
        return star.TranslatePoint(new Point(star.Bounds.Width * 0.75, star.Bounds.Height / 2), window)!.Value;
    }

    [AvaloniaFact]
    public void Hover_Sets_Tentative_Release_Commits_Leave_Cancels()
    {
        var (window, rating) = Make();
        var preview = new List<int>();
        var changes = new List<int>();
        var cancels = 0;
        rating.PreviewChange += (_, e) => preview.Add(e.TentativeRating);
        rating.Change += (_, e) => changes.Add(e.UserRating);
        rating.Cancel += (_, _) => cancels++;
        Assert.Equal(5, rating.Stars.Count);
        Assert.Equal(40, rating.Stars[0].Bounds.Width, 0.5);

        window.MouseMove(StarCenter(window, rating, 2));
        Assert.Equal(3, rating.TentativeRating);
        Assert.Equal([3], preview);
        Assert.Contains(":tentative-full", rating.Stars[2].Classes);
        Assert.Contains(":tentative-empty", rating.Stars[3].Classes);

        window.MouseMove(new Point(0, 0));
        Assert.Equal(-1, rating.TentativeRating);
        Assert.Equal(1, cancels);
        Assert.Equal(0, rating.UserRating);

        var p = StarCenter(window, rating, 3);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Assert.Equal(4, rating.UserRating);
        Assert.Equal([4], changes);
        Assert.Contains(":user-full", rating.Stars[3].Classes);
        Assert.Contains(":user-empty", rating.Stars[4].Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void EnableClear_False_Clamps_At_One_And_Keys_Work()
    {
        var (window, rating) = Make(user: 2, enableClear: false);
        rating.Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Assert.Equal(1, rating.TentativeRating);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(1, rating.UserRating);

        var left = rating.Stars[0].TranslatePoint(new Point(1, 10), window)!.Value;
        window.MouseMove(left);
        Assert.True(rating.TentativeRating >= 1);

        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.Equal(-1, rating.TentativeRating);
        Assert.Equal(1, rating.UserRating);
        window.Close();
    }

    [AvaloniaFact]
    public void Clear_Allowed_Sets_Zero_From_Keyboard()
    {
        var (window, rating) = Make(user: 1);
        rating.Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Assert.Equal(0, rating.TentativeRating);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(0, rating.UserRating);
        window.Close();
    }

    [AvaloniaFact]
    public void Average_Renders_Fractional_Star()
    {
        var (window, rating) = Make(average: 3.5);
        Assert.Contains(":average", rating.Classes);
        Assert.Contains(":average-full", rating.Stars[2].Classes);
        Assert.Contains(":average-fractional", rating.Stars[3].Classes);
        Assert.Contains(":average-empty", rating.Stars[4].Classes);
        var clip = rating.Stars[3].GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Clip");
        Assert.Equal(28 * 0.5, clip.Width, 0.5);
        rating.UserRating = 2;
        window.UpdateLayout();
        Assert.Contains(":user-full", rating.Stars[1].Classes);
        Assert.DoesNotContain(":average", rating.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Small_Class_And_Tooltips()
    {
        var rating = new Rating { Classes = { "small" }, TooltipStrings = new[] { "Bad", "Poor", "OK", "Good", "Great" } };
        var window = ThemeTestHelpers.Host(rating, "Light", Platform.Desktop);
        Assert.Equal(20, rating.Stars[0].Bounds.Width, 0.5);
        Assert.Equal("OK", ToolTip.GetTip(rating.Stars[2]));
        window.Close();
    }
}
