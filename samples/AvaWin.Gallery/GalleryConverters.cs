using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaWin.Gallery.ViewModels;

namespace AvaWin.Gallery;

public static class GalleryConverters
{
    /// <summary>A 1-based photo index (see <see cref="SampleData.Photos"/>) to its bitmap.</summary>
    /// <summary>Unread mail rows are bold.</summary>
    public static readonly IValueConverter UnreadWeight = new FuncValueConverter<bool, FontWeight>(unread => unread ? FontWeight.SemiBold : FontWeight.Normal);

    public static readonly IValueConverter PhotoImage = new FuncValueConverter<int, Bitmap?>(i => i >= 1 && i <= SampleData.Photos.Count ? SampleData.Photos[i - 1].Image : null);
}
