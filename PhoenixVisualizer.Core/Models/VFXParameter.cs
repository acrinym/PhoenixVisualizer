using System;

namespace PhoenixVisualizer.Core.Models
{
    /// <summary>
    /// Represents a configurable parameter for a VFX effect
    /// </summary>
    public class VFXParameter
    {
        /// <summary>
        /// Unique identifier for the parameter
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Display name for the parameter
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of what the parameter does
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Current value of the parameter
        /// </summary>
        public object? Value { get; set; }
        
        /// <summary>
        /// Default value for the parameter
        /// </summary>
        public object? DefaultValue { get; set; }
        
        /// <summary>
        /// Minimum value (for numeric parameters)
        /// </summary>
        public object? MinValue { get; set; }
        
        /// <summary>
        /// Maximum value (for numeric parameters)
        /// </summary>
        public object? MaxValue { get; set; }
        
        /// <summary>
        /// Type of the parameter value
        /// </summary>
        public Type ParameterType { get; set; } = typeof(object);
        
        /// <summary>
        /// Whether the parameter can be animated
        /// </summary>
        public bool IsAnimatable { get; set; } = true;
        
        /// <summary>
        /// Whether the parameter is currently visible in the UI
        /// </summary>
        public bool IsVisible { get; set; } = true;
        
        /// <summary>
        /// Category for grouping related parameters
        /// </summary>
        public string Category { get; set; } = "General";
        
        /// <summary>
        /// Order for UI display (lower numbers appear first)
        /// </summary>
        public int Order { get; set; } = 0;
        
        /// <summary>
        /// Reset the parameter to its default value
        /// </summary>
        public void ResetToDefault()
        {
            Value = DefaultValue;
        }
        
        /// <summary>
        /// Get the parameter value as a specific type
        /// </summary>
        public T? GetValue<T>()
        {
            if (Value is T typedValue)
                return typedValue;
            
            if (Value != null && typeof(T).IsAssignableFrom(Value.GetType()))
                return (T)Value;
                
            return default;
        }
        
        /// <summary>
        /// Set the parameter value with type safety
        /// </summary>
        public void SetValue<T>(T value)
        {
            if (typeof(T).IsAssignableFrom(ParameterType))
            {
                Value = value;
            }
            else
            {
                throw new ArgumentException($"Cannot assign value of type {typeof(T)} to parameter of type {ParameterType}");
            }
        }
    }
}