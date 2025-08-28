using System;

namespace PhoenixVisualizer.Core.VFX
{
    /// <summary>
    /// Marks a class as a Phoenix VFX effect node with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PhoenixVFXAttribute : Attribute
    {
        public string Id { get; }
        public string Name { get; }
        public string Category { get; }

        public PhoenixVFXAttribute(string id, string name, string category = "General")
        {
            Id = id;
            Name = name;
            Category = category;
        }
    }
}
