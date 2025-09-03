using Avalonia.Data.Converters;
using PhoenixVisualizer.Core.Catalog;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhoenixVisualizer.Editor.Converters;

/// <summary>Extracts unique categories from NodeMeta for the ComboBox.</summary>
public sealed class CatalogCategoryConverter : IValueConverter
{
    public static readonly CatalogCategoryConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable list)
        {
            var cats = new HashSet<string>(StringComparer.OrdinalIgnoreCase){ "All" };
            foreach (var o in list) if (o is NodeMeta m && !string.IsNullOrWhiteSpace(m.Category)) cats.Add(m.Category);
            return cats.OrderBy(x => x).ToList();
        }
        return new []{ "All" };
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
