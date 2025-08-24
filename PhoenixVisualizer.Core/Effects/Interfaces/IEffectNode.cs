using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects.Models;

namespace PhoenixVisualizer.Core.Effects.Interfaces
{
    /// <summary>
    /// Core interface for all effect nodes in the Phoenix Visualizer system
    /// Provides a clean, decoupled interface for effect processing
    /// </summary>
    public interface IEffectNode
    {
        /// <summary>
        /// Unique identifier for the node
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Human-readable name for the node
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Description of what the node does
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Category for organizing nodes
        /// </summary>
        string Category { get; }
        
        /// <summary>
        /// Version of the node implementation
        /// </summary>
        Version Version { get; }
        
        /// <summary>
        /// Whether the node is currently enabled
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Input ports for the node
        /// </summary>
        IReadOnlyList<EffectPort> InputPorts { get; }
        
        /// <summary>
        /// Output ports for the node
        /// </summary>
        IReadOnlyList<EffectPort> OutputPorts { get; }
        
        /// <summary>
        /// Process inputs and return output
        /// </summary>
        /// <param name="inputs">Input data dictionary</param>
        /// <param name="audioFeatures">Audio analysis data</param>
        /// <returns>Processed output</returns>
        object Process(Dictionary<string, object> inputs, AudioFeatures audioFeatures);
        
        /// <summary>
        /// Validate the current configuration
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        bool ValidateConfiguration();
        
        /// <summary>
        /// Reset the node to initial state
        /// </summary>
        void Reset();
        
        /// <summary>
        /// Initialize the node
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Get a summary of current settings
        /// </summary>
        /// <returns>Settings summary string</returns>
        string GetSettingsSummary();
        
        /// <summary>
        /// Get default output when processing fails or is disabled
        /// </summary>
        /// <returns>Default output object</returns>
        object GetDefaultOutput();
    }
}
