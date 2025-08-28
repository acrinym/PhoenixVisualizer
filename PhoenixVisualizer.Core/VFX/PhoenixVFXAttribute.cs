using System;

namespace PhoenixVisualizer.Core.VFX
{
    /// <summary>
    /// Attribute for marking classes as Phoenix VFX effects
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PhoenixVFXAttribute : Attribute
    {
        /// <summary>
        /// Unique identifier for the VFX effect
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Display name for the VFX effect
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Category this VFX effect belongs to
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// Version of the VFX effect
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Author of the VFX effect
        /// </summary>
        public string Author { get; set; }
        
        /// <summary>
        /// Description of what the VFX effect does
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Whether the VFX effect requires GPU acceleration
        /// </summary>
        public bool RequiresGPU { get; set; }
        
        /// <summary>
        /// Whether the VFX effect is experimental
        /// </summary>
        public bool IsExperimental { get; set; }
        
        /// <summary>
        /// Create a default Phoenix VFX attribute
        /// </summary>
        public PhoenixVFXAttribute()
        {
            Id = string.Empty;
            Name = string.Empty;
            Category = "General";
            Version = "1.0";
            Author = "Phoenix Visualizer Team";
            Description = string.Empty;
            RequiresGPU = false;
            IsExperimental = false;
        }
        
        /// <summary>
        /// Create a new Phoenix VFX attribute
        /// </summary>
        public PhoenixVFXAttribute(
            string id,
            string name = "",
            string category = "General",
            string version = "1.0",
            string author = "Phoenix Visualizer Team",
            string description = "",
            bool requiresGPU = false,
            bool isExperimental = false)
        {
            Id = id;
            Name = string.IsNullOrEmpty(name) ? id : name;
            Category = category;
            Version = version;
            Author = author;
            Description = description;
            RequiresGPU = requiresGPU;
            IsExperimental = isExperimental;
        }
        
        /// <summary>
        /// Create a simple Phoenix VFX attribute with just an ID
        /// </summary>
        public PhoenixVFXAttribute(string id)
        {
            Id = id;
            Name = id;
            Category = "General";
            Version = "1.0";
            Author = "Phoenix Visualizer Team";
            Description = string.Empty;
            RequiresGPU = false;
            IsExperimental = false;
        }
        
        /// <summary>
        /// Create a Phoenix VFX attribute with ID and name
        /// </summary>
        public PhoenixVFXAttribute(string id, string name)
        {
            Id = id;
            Name = name;
            Category = "General";
            Version = "1.0";
            Author = "Phoenix Visualizer Team";
            Description = string.Empty;
            RequiresGPU = false;
            IsExperimental = false;
        }
        
        /// <summary>
        /// Create a Phoenix VFX attribute with ID, name, and category
        /// </summary>
        public PhoenixVFXAttribute(string id, string name, string category)
        {
            Id = id;
            Name = name;
            Category = category;
            Version = "1.0";
            Author = "Phoenix Visualizer Team";
            Description = string.Empty;
            RequiresGPU = false;
            IsExperimental = false;
        }
    }
}
