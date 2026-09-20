using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AvaWin.Gallery.Framework;

public static class Converters
{
    /// <summary>Two-way: true when the value equals the parameter; setting true writes the parameter back (radio groups over a string).</summary>
    public static readonly IValueConverter IsEqual = new EqualsConverter();

    /// <summary>A bitmap to an ImageBrush that fills its control.</summary>
    public static readonly IValueConverter ImageBrush = new FuncValueConverter<Avalonia.Media.Imaging.Bitmap?, Avalonia.Media.IBrush?>(b => b is null ? null : new Avalonia.Media.ImageBrush(b) { Stretch = Avalonia.Media.Stretch.UniformToFill });

    private sealed class EqualsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? parameter : BindingOperations.DoNothing;
    }
}
