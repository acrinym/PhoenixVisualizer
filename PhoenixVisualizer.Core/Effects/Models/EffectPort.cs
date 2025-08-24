using System;

namespace PhoenixVisualizer.Core.Effects.Models
{
    public class EffectPort
    {
        public string Name { get; set; } = string.Empty;
        public Type DataType { get; set; } = default!;
        public bool IsRequired { get; set; }
        public object? DefaultValue { get; set; }
        public string Description { get; set; } = string.Empty;
        public EffectConnection? Connection { get; set; }

        public EffectPort()
        {
        }

        public EffectPort(string name, Type dataType, bool isRequired, object? defaultValue, string description)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            IsRequired = isRequired;
            DefaultValue = defaultValue;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }
}
