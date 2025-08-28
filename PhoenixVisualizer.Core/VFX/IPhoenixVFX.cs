using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.VFX
{
    /// <summary>
    /// Interface for all Phoenix VFX effects
    /// Provides modern VFX architecture with PEL integration, GPU acceleration hooks, and automatic parameter discovery
    /// </summary>
    public interface IPhoenixVFX
    {
        /// <summary>
        /// Unique identifier for this VFX effect
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Display name for this VFX effect
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Category this VFX effect belongs to
        /// </summary>
        string Category { get; }
        
        /// <summary>
        /// Whether this VFX effect is currently enabled
        /// </summary>
        bool Enabled { get; set; }
        
        /// <summary>
        /// Opacity/Alpha for the effect output (0.0 to 1.0)
        /// </summary>
        float Opacity { get; set; }
        
        /// <summary>
        /// All discoverable parameters for this VFX
        /// </summary>
        Dictionary<string, VFXParameter> Parameters { get; }
        
        /// <summary>
        /// Performance metrics for this VFX
        /// </summary>
        VFXPerformanceMetrics Performance { get; }
        
        /// <summary>
        /// Initialize the VFX effect with the given context
        /// </summary>
        void Initialize(VFXRenderContext context, AudioFeatures audio);
        
        /// <summary>
        /// Process a single frame with the VFX effect
        /// </summary>
        void ProcessFrame(VFXRenderContext context);
        
        /// <summary>
        /// Clean up resources used by the VFX effect
        /// </summary>
        void Dispose();
    }
}
