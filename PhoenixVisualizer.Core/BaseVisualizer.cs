using System;
using PhoenixVisualizer.Core.Models;
using SkiaSharp;

namespace PhoenixVisualizer.Core
{
    /// <summary>
    /// Base class for all visualizers in Phoenix Visualizer
    /// Provides common functionality and interface for audio-reactive visualizations
    /// </summary>
    public abstract class BaseVisualizer
    {
        /// <summary>
        /// Unique identifier for this visualizer
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// Display name for this visualizer
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Description of what this visualizer does
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Render the visualizer to the specified canvas
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to render to</param>
        /// <param name="width">Width of the render area</param>
        /// <param name="height">Height of the render area</param>
        /// <param name="audioFeatures">Current audio features for reactivity</param>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        public abstract void Render(SKCanvas canvas, int width, int height, AudioFeatures audioFeatures, float deltaTime);

        /// <summary>
        /// Initialize the visualizer with default settings
        /// </summary>
        public virtual void Initialize()
        {
            // Default implementation - override in derived classes if needed
        }

        /// <summary>
        /// Clean up resources used by the visualizer
        /// </summary>
        public virtual void Dispose()
        {
            // Default implementation - override in derived classes if needed
        }

        /// <summary>
        /// Reset the visualizer to its initial state
        /// </summary>
        public virtual void Reset()
        {
            // Default implementation - override in derived classes if needed
        }

        /// <summary>
        /// Get the current settings/configuration of the visualizer
        /// </summary>
        /// <returns>Dictionary of setting names and values</returns>
        public virtual System.Collections.Generic.Dictionary<string, object> GetSettings()
        {
            return new System.Collections.Generic.Dictionary<string, object>();
        }

        /// <summary>
        /// Apply settings/configuration to the visualizer
        /// </summary>
        /// <param name="settings">Dictionary of setting names and values</param>
        public virtual void ApplySettings(System.Collections.Generic.Dictionary<string, object> settings)
        {
            // Default implementation - override in derived classes if needed
        }
    }
}
