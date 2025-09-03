using Avalonia.Data.Converters;
using AvaloniaEdit.Document;
using System;
using System.Globalization;

namespace PhoenixVisualizer.Editor.Converters;

public sealed class StringToDocumentConverter : IValueConverter
{
    public static readonly StringToDocumentConverter Instance = new();
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return new TextDocument(text);
        }
        return new TextDocument();
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TextDocument document)
        {
            return document.Text;
        }
        return string.Empty;
    }
}
