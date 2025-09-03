using Avalonia.Data.Converters;
using Avalonia;
using Avalonia.Controls;
using System;
using System.Globalization;

namespace PhoenixVisualizer.Editor.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? (object)1 : (object)0;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
