namespace PhoenixVisualizer.Core.VFX;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class VFXParameterAttribute : Attribute
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public object MinValue { get; }
    public object MaxValue { get; }
    public object DefaultValue { get; }
    public string[]? EnumValues { get; }

    public VFXParameterAttribute(string id, string name, string description, object minValue, object maxValue, object defaultValue, string[]? enumValues = null)
    {
        Id = id;
        Name = name;
        Description = description;
        MinValue = minValue;
        MaxValue = maxValue;
        DefaultValue = defaultValue;
        EnumValues = enumValues;
    }

    public VFXParameterAttribute(string displayName)
    {
        Id = displayName.ToLowerInvariant();
        Name = displayName;
        Description = $"{displayName} parameter";
        MinValue = 0;
        MaxValue = 1;
        DefaultValue = 0;
    }
}
