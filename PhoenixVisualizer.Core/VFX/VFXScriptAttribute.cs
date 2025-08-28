using System;

namespace PhoenixVisualizer.Core.VFX
{
    /// <summary>
    /// Marks a property as a VFX script entry point.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class VFXScriptAttribute : Attribute
    {
        public string Type { get; }
        public string Name { get; }

        public VFXScriptAttribute(string type, string name)
        {
            Type = type;
            Name = name;
        }

        public VFXScriptAttribute(string type) : this(type, type) { }
    }
}
