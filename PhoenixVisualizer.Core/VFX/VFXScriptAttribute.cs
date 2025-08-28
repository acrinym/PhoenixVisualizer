using System;

namespace PhoenixVisualizer.Core.VFX
{
    /// <summary>
    /// Marks a method as a VFX script entry point.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class VFXScriptAttribute : Attribute
    {
        public string Name { get; }

        public VFXScriptAttribute(string name)
        {
            Name = name;
        }
    }
}
