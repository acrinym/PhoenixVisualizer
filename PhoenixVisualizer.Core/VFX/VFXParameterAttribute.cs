using System;

namespace PhoenixVisualizer.Core.VFX
{
    /// <summary>
    /// Attribute for marking VFX effect parameters for automatic discovery
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class VFXParameterAttribute : Attribute
    {
        /// <summary>
        /// Unique identifier for the parameter
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// Display name for the parameter
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Description of what the parameter does
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Minimum value (for numeric parameters)
        /// </summary>
        public object? MinValue { get; }
        
        /// <summary>
        /// Maximum value (for numeric parameters)
        /// </summary>
        public object? MaxValue { get; }
        
        /// <summary>
        /// Default value for the parameter
        /// </summary>
        public object? DefaultValue { get; }
        
        /// <summary>
        /// Whether the parameter can be animated
        /// </summary>
        public bool IsAnimatable { get; }
        
        /// <summary>
        /// Whether the parameter is visible in the UI
        /// </summary>
        public bool IsVisible { get; }
        
        /// <summary>
        /// Category for grouping related parameters
        /// </summary>
        public string Category { get; }
        
        /// <summary>
        /// Order for UI display (lower numbers appear first)
        /// </summary>
        public int Order { get; }
        
        /// <summary>
        /// Create a new VFX parameter attribute
        /// </summary>
        public VFXParameterAttribute(
            string id,
            string name = "",
            string description = "",
            object? minValue = null,
            object? maxValue = null,
            object? defaultValue = null,
            bool isAnimatable = true,
            bool isVisible = true,
            string category = "General",
            int order = 0)
        {
            Id = id;
            Name = string.IsNullOrEmpty(name) ? id : name;
            Description = description;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultValue = defaultValue;
            IsAnimatable = isAnimatable;
            IsVisible = isVisible;
            Category = category;
            Order = order;
        }
        
        /// <summary>
        /// Create a simple VFX parameter attribute with just an ID
        /// </summary>
        public VFXParameterAttribute(string id)
        {
            Id = id;
            Name = id;
            Description = string.Empty;
            MinValue = null;
            MaxValue = null;
            DefaultValue = null;
            IsAnimatable = true;
            IsVisible = true;
            Category = "General";
            Order = 0;
        }
        
        /// <summary>
        /// Create a VFX parameter attribute with ID and name
        /// </summary>
        public VFXParameterAttribute(string id, string name)
        {
            Id = id;
            Name = name;
            Description = string.Empty;
            MinValue = null;
            MaxValue = null;
            DefaultValue = null;
            IsAnimatable = true;
            IsVisible = true;
            Category = "General";
            Order = 0;
        }
        
        /// <summary>
        /// Create a VFX parameter attribute with ID, name, and description
        /// </summary>
        public VFXParameterAttribute(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
            MinValue = null;
            MaxValue = null;
            DefaultValue = null;
            IsAnimatable = true;
            IsVisible = true;
            Category = "General";
            Order = 0;
        }
    }
}
