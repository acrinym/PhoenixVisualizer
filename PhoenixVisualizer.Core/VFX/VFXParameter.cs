using System.Reflection;

namespace PhoenixVisualizer.Core.VFX;

public class VFXParameter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(object);
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public object? DefaultValue { get; set; }
    public PropertyInfo Property { get; set; } = null!;
    public string[]? EnumValues { get; set; }
}
