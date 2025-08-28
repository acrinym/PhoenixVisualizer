using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Graph
{
    /// <summary>
    /// Manages multiple effects graphs and provides common graph operations
    /// </summary>
    public class EffectsGraphManager
    {
        #region Properties

        public string Name { get; set; } = "Effects Graph Manager";
        public bool IsEnabled { get; set; } = true;
        public int ActiveGraphCount => _graphs.Count;
        public int TotalNodeCount => _graphs.Values.Sum(g => g.GetNodes().Count);
        public int TotalConnectionCount => _graphs.Values.Sum(g => g.GetConnections().Count);

        #endregion

        #region Private Fields

        private readonly Dictionary<string, EffectsGraph> _graphs;
        private readonly Dictionary<string, IEffectNode> _availableNodes;
        private readonly object _lock;

        #endregion

        #region Constructor

        public EffectsGraphManager()
        {
            _graphs = new Dictionary<string, EffectsGraph>();
            _availableNodes = new Dictionary<string, IEffectNode>();
            _lock = new object();
        }

        #endregion

        #region Graph Management

        /// <summary>
        /// Create a new effects graph
        /// </summary>
        public EffectsGraph CreateGraph(string name, string description = "")
        {
            lock (_lock)
            {
                var graph = new EffectsGraph
                {
                    Name = name,
                    Description = description
                };

                _graphs[name] = graph;
                return graph;
            }
        }

        /// <summary>
        /// Get a graph by name
        /// </summary>
        public EffectsGraph? GetGraph(string name)
        {
            lock (_lock)
            {
                return _graphs.TryGetValue(name, out var graph) ? graph : null;
            }
        }

        /// <summary>
        /// Remove a graph
        /// </summary>
        public bool RemoveGraph(string name)
        {
            lock (_lock)
            {
                return _graphs.Remove(name);
            }
        }

        /// <summary>
        /// Get all graphs
        /// </summary>
        public IReadOnlyDictionary<string, EffectsGraph> GetAllGraphs()
        {
            lock (_lock)
            {
                return new Dictionary<string, EffectsGraph>(_graphs);
            }
        }

        #endregion

        #region Node Management

        /// <summary>
        /// Register an available node type
        /// </summary>
        public void RegisterNodeType(IEffectNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.Id))
                return;

            lock (_lock)
            {
                _availableNodes[node.Id] = node;
            }
        }

        /// <summary>
        /// Get available node types
        /// </summary>
        public IReadOnlyDictionary<string, IEffectNode> GetAvailableNodeTypes()
        {
            lock (_lock)
            {
                return new Dictionary<string, IEffectNode>(_availableNodes);
            }
        }

        /// <summary>
        /// Get available node types by category
        /// </summary>
        public IEnumerable<IEffectNode> GetAvailableNodeTypesByCategory(string category)
        {
            lock (_lock)
            {
                return _availableNodes.Values.Where(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Create a node instance from a registered type
        /// </summary>
        public IEffectNode? CreateNodeInstance(string nodeTypeId)
        {
            lock (_lock)
            {
                if (!_availableNodes.TryGetValue(nodeTypeId, out var nodeType))
                    return null;

                // For now, return the registered node (in a real implementation, you'd clone it)
                // This is a simplified approach - in practice you'd want proper instantiation
                return nodeType;
            }
        }

        #endregion

        #region Common Graph Patterns

        /// <summary>
        /// Create a simple chain of effects
        /// </summary>
        public EffectsGraph CreateEffectChain(string graphName, params string[] nodeTypeIds)
        {
            var graph = CreateGraph(graphName, $"Chain of {nodeTypeIds.Length} effects");
            
            if (nodeTypeIds.Length < 2)
                return graph;

            var previousNodeId = "";
            
            for (int i = 0; i < nodeTypeIds.Length; i++)
            {
                var nodeTypeId = nodeTypeIds[i];
                var node = CreateNodeInstance(nodeTypeId);
                
                if (node == null)
                    continue;

                var nodeId = $"{nodeTypeId}_{i}";
                graph.AddNode(node);

                if (!string.IsNullOrEmpty(previousNodeId))
                {
                    // Connect to previous node
                    graph.AddConnection(previousNodeId, "Output", nodeId, "Input");
                }

                previousNodeId = nodeId;
            }

            return graph;
        }

        /// <summary>
        /// Create a parallel effects setup
        /// </summary>
        public EffectsGraph CreateParallelEffects(string graphName, string inputNodeTypeId, string outputNodeTypeId, params string[] parallelNodeTypeIds)
        {
            var graph = CreateGraph(graphName, $"Parallel effects with {parallelNodeTypeIds.Length} branches");

            // Create input and output nodes
            var inputNode = CreateNodeInstance(inputNodeTypeId);
            var outputNode = CreateNodeInstance(outputNodeTypeId);

            if (inputNode == null || outputNode == null)
                return graph;

            var inputNodeId = $"{inputNodeTypeId}_input";
            var outputNodeId = $"{outputNodeTypeId}_output";

            graph.AddNode(inputNode);
            graph.AddNode(outputNode);

            // Create parallel effect nodes
            for (int i = 0; i < parallelNodeTypeIds.Length; i++)
            {
                var nodeTypeId = parallelNodeTypeIds[i];
                var node = CreateNodeInstance(nodeTypeId);

                if (node == null)
                    continue;

                var nodeId = $"{nodeTypeId}_{i}";
                graph.AddNode(node);

                // Connect input to parallel node
                graph.AddConnection(inputNodeId, "Output", nodeId, "Input");
                
                // Connect parallel node to output
                graph.AddConnection(nodeId, "Output", outputNodeId, "Input");
            }

            return graph;
        }

        /// <summary>
        /// Create a feedback loop effect
        /// </summary>
        public EffectsGraph CreateFeedbackLoop(string graphName, string effectNodeTypeId, string delayNodeTypeId, int delayFrames = 1)
        {
            var graph = CreateGraph(graphName, $"Feedback loop with {delayFrames} frame delay");

            var effectNode = CreateNodeInstance(effectNodeTypeId);
            var delayNode = CreateNodeInstance(delayNodeTypeId);

            if (effectNode == null || delayNode == null)
                return graph;

            var effectNodeId = $"{effectNodeTypeId}_effect";
            var delayNodeId = $"{delayNodeTypeId}_delay";

            graph.AddNode(effectNode);
            graph.AddNode(delayNode);

            // Create the feedback loop
            graph.AddConnection(effectNodeId, "Output", delayNodeId, "Input");
            graph.AddConnection(delayNodeId, "Output", effectNodeId, "Feedback");

            return graph;
        }

        #endregion

        #region Graph Operations

        /// <summary>
        /// Process all active graphs
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> ProcessAllGraphs(AudioFeatures audioFeatures)
        {
            var results = new Dictionary<string, Dictionary<string, object>>();

            lock (_lock)
            {
                foreach (var graph in _graphs.Values)
                {
                    if (graph.IsEnabled)
                    {
                        try
                        {
                            var graphResults = graph.ProcessGraph(audioFeatures);
                            results[graph.Name] = graphResults;
                        }
                        catch (Exception ex)
                        {
                            // Log error and continue with other graphs
                            System.Diagnostics.Debug.WriteLine($"Error processing graph {graph.Name}: {ex.Message}");
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Validate all graphs
        /// </summary>
        public Dictionary<string, List<string>> ValidateAllGraphs()
        {
            var validationResults = new Dictionary<string, List<string>>();

            lock (_lock)
            {
                foreach (var graph in _graphs.Values)
                {
                    var errors = graph.GetValidationErrors();
                    if (errors.Any())
                    {
                        validationResults[graph.Name] = errors;
                    }
                }
            }

            return validationResults;
        }

        /// <summary>
        /// Get statistics for all graphs
        /// </summary>
        public Dictionary<string, GraphStatistics> GetAllGraphStatistics()
        {
            var statistics = new Dictionary<string, GraphStatistics>();

            lock (_lock)
            {
                foreach (var graph in _graphs.Values)
                {
                    statistics[graph.Name] = graph.GetStatistics();
                }
            }

            return statistics;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Clear all graphs
        /// </summary>
        public void ClearAllGraphs()
        {
            lock (_lock)
            {
                foreach (var graph in _graphs.Values)
                {
                    graph.Clear();
                }
                _graphs.Clear();
            }
        }

        /// <summary>
        /// Get overall manager statistics
        /// </summary>
        public ManagerStatistics GetManagerStatistics()
        {
            lock (_lock)
            {
                return new ManagerStatistics
                {
                    ActiveGraphCount = ActiveGraphCount,
                    TotalNodeCount = TotalNodeCount,
                    TotalConnectionCount = TotalConnectionCount,
                    AvailableNodeTypes = _availableNodes.Count,
                    IsEnabled = IsEnabled
                };
            }
        }

        #endregion
    }

    /// <summary>
    /// Statistics for the graph manager
    /// </summary>
    public class ManagerStatistics
    {
        public int ActiveGraphCount { get; set; }
        public int TotalNodeCount { get; set; }
        public int TotalConnectionCount { get; set; }
        public int AvailableNodeTypes { get; set; }
        public bool IsEnabled { get; set; }

        public override string ToString()
        {
            return $"Manager: {ActiveGraphCount} graphs, {TotalNodeCount} nodes, {TotalConnectionCount} connections, {AvailableNodeTypes} node types, Enabled: {IsEnabled}";
        }
    }
}