using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Effects.Nodes;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Graph
{
    /// <summary>
    /// EffectsGraph manages a collection of effect nodes and their connections
    /// Provides a visual programming interface for composing effects
    /// </summary>
    public class EffectsGraph
    {
        #region Events

        public event EventHandler<EffectNodeEventArgs>? NodeAdded;
        public event EventHandler<EffectNodeEventArgs>? NodeRemoved;
        public event EventHandler<EffectConnectionEventArgs>? ConnectionAdded;
        public event EventHandler<EffectConnectionEventArgs>? ConnectionRemoved;
        public event EventHandler<EffectProcessingEventArgs>? ProcessingStarted;
        public event EventHandler<EffectProcessingEventArgs>? ProcessingCompleted;

        #endregion

        #region Properties

        public string Name { get; set; } = "Effects Graph";
        public string Description { get; set; } = "Visual effects composition graph";
        public bool IsEnabled { get; set; } = true;
        public bool IsProcessing { get; private set; }
        public DateTime LastProcessed { get; private set; }
        public TimeSpan ProcessingTime { get; private set; }

        #endregion

        #region Private Fields

        private readonly Dictionary<string, IEffectNode> _nodes;
        private readonly Dictionary<string, EffectConnection> _connections;
        private readonly Dictionary<string, object> _globalData;
        private readonly object _processingLock;
        private readonly PhoenixExpressionEngine _expressionEngine;

        #endregion

        #region Constructor

        public EffectsGraph()
        {
            _nodes = new Dictionary<string, IEffectNode>();
            _connections = new Dictionary<string, EffectConnection>();
            _globalData = new Dictionary<string, object>();
            _processingLock = new object();
            _expressionEngine = new PhoenixExpressionEngine();
        }

        #endregion

        #region Node Management

        /// <summary>
        /// Add a node to the graph
        /// </summary>
        public bool AddNode(IEffectNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.Id))
                return false;

            lock (_processingLock)
            {
                if (_nodes.ContainsKey(node.Id))
                    return false;

                _nodes[node.Id] = node;
                
                // Bind the expression engine to the node
                if (node is BaseEffectNode baseNode)
                {
                    baseNode.BindExpressionEngine(_expressionEngine);
                }

                NodeAdded?.Invoke(this, new EffectNodeEventArgs(node));
                return true;
            }
        }

        /// <summary>
        /// Remove a node from the graph
        /// </summary>
        public bool RemoveNode(string nodeId)
        {
            lock (_processingLock)
            {
                if (!_nodes.TryGetValue(nodeId, out var node))
                    return false;

                // Remove all connections involving this node
                var connectionsToRemove = _connections.Values
                    .Where(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId)
                    .ToList();

                foreach (var connection in connectionsToRemove)
                {
                    RemoveConnection(connection.Id);
                }

                _nodes.Remove(nodeId);
                NodeRemoved?.Invoke(this, new EffectNodeEventArgs(node));
                return true;
            }
        }

        /// <summary>
        /// Get a node by ID
        /// </summary>
        public IEffectNode? GetNode(string nodeId)
        {
            lock (_processingLock)
            {
                return _nodes.TryGetValue(nodeId, out var node) ? node : null;
            }
        }

        /// <summary>
        /// Get all nodes in the graph
        /// </summary>
        public IReadOnlyDictionary<string, IEffectNode> GetNodes()
        {
            lock (_processingLock)
            {
                return new Dictionary<string, IEffectNode>(_nodes);
            }
        }

        /// <summary>
        /// Get nodes by category
        /// </summary>
        public IEnumerable<IEffectNode> GetNodesByCategory(string category)
        {
            lock (_processingLock)
            {
                return _nodes.Values.Where(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Add a connection between two nodes
        /// </summary>
        public bool AddConnection(string sourceNodeId, string sourcePortName, string targetNodeId, string targetPortName)
        {
            lock (_processingLock)
            {
                if (!_nodes.ContainsKey(sourceNodeId) || !_nodes.ContainsKey(targetNodeId))
                    return false;

                var sourceNode = _nodes[sourceNodeId];
                var targetNode = _nodes[targetNodeId];

                // Validate ports exist
                var sourcePort = sourceNode.OutputPorts.FirstOrDefault(p => p.Name == sourcePortName);
                var targetPort = targetNode.InputPorts.FirstOrDefault(p => p.Name == targetPortName);

                if (sourcePort == null || targetPort == null)
                    return false;

                // Check for circular connections
                if (WouldCreateCycle(sourceNodeId, targetNodeId))
                    return false;

                var connection = new EffectConnection
                {
                    Id = Guid.NewGuid().ToString(),
                    SourceNodeId = sourceNodeId,
                    SourcePortName = sourcePortName,
                    TargetNodeId = targetNodeId,
                    TargetPortName = targetPortName,
                    DataType = sourcePort.DataType
                };

                _connections[connection.Id] = connection;
                ConnectionAdded?.Invoke(this, new EffectConnectionEventArgs(connection));
                return true;
            }
        }

        /// <summary>
        /// Remove a connection
        /// </summary>
        public bool RemoveConnection(string connectionId)
        {
            lock (_processingLock)
            {
                if (!_connections.TryGetValue(connectionId, out var connection))
                    return false;

                _connections.Remove(connectionId);
                ConnectionRemoved?.Invoke(this, new EffectConnectionEventArgs(connection));
                return true;
            }
        }

        /// <summary>
        /// Get all connections
        /// </summary>
        public IReadOnlyDictionary<string, EffectConnection> GetConnections()
        {
            lock (_processingLock)
            {
                return new Dictionary<string, EffectConnection>(_connections);
            }
        }

        /// <summary>
        /// Get connections for a specific node
        /// </summary>
        public IEnumerable<EffectConnection> GetConnectionsForNode(string nodeId)
        {
            lock (_processingLock)
            {
                return _connections.Values.Where(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId);
            }
        }

        #endregion

        #region Graph Processing

        /// <summary>
        /// Process the entire graph
        /// </summary>
        public Dictionary<string, object> ProcessGraph(AudioFeatures audioFeatures)
        {
            if (!IsEnabled || IsProcessing)
                return new Dictionary<string, object>();

            lock (_processingLock)
            {
                try
                {
                    IsProcessing = true;
                    var startTime = DateTime.UtcNow;
                    
                    ProcessingStarted?.Invoke(this, new EffectProcessingEventArgs(this));

                    // Topological sort to determine processing order
                    var processingOrder = GetTopologicalOrder();
                    var results = new Dictionary<string, object>();

                    // Process nodes in order
                    foreach (var nodeId in processingOrder)
                    {
                        if (!_nodes.TryGetValue(nodeId, out var node))
                            continue;

                        var inputs = GatherNodeInputs(nodeId, results);
                        var output = node.Process(inputs, audioFeatures);
                        
                        if (output != null)
                        {
                            results[nodeId] = output;
                        }
                    }

                    var endTime = DateTime.UtcNow;
                    ProcessingTime = endTime - startTime;
                    LastProcessed = endTime;

                    ProcessingCompleted?.Invoke(this, new EffectProcessingEventArgs(this));
                    return results;
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        }

        /// <summary>
        /// Process a specific node and its dependencies
        /// </summary>
        public object? ProcessNode(string nodeId, AudioFeatures audioFeatures)
        {
            lock (_processingLock)
            {
                if (!_nodes.TryGetValue(nodeId, out var node))
                    return null;

                var inputs = GatherNodeInputs(nodeId, new Dictionary<string, object>());
                return node.Process(inputs, audioFeatures);
            }
        }

        #endregion

        #region Graph Analysis

        /// <summary>
        /// Get topological order for processing
        /// </summary>
        private List<string> GetTopologicalOrder()
        {
            var visited = new HashSet<string>();
            var tempVisited = new HashSet<string>();
            var order = new List<string>();

            foreach (var nodeId in _nodes.Keys)
            {
                if (!visited.Contains(nodeId))
                {
                    TopologicalSort(nodeId, visited, tempVisited, order);
                }
            }

            order.Reverse();
            return order;
        }

        /// <summary>
        /// Topological sort using DFS
        /// </summary>
        private void TopologicalSort(string nodeId, HashSet<string> visited, HashSet<string> tempVisited, List<string> order)
        {
            if (tempVisited.Contains(nodeId))
                throw new InvalidOperationException("Circular dependency detected in effects graph");

            if (visited.Contains(nodeId))
                return;

            tempVisited.Add(nodeId);

            var outgoingConnections = _connections.Values.Where(c => c.SourceNodeId == nodeId);
            foreach (var connection in outgoingConnections)
            {
                TopologicalSort(connection.TargetNodeId, visited, tempVisited, order);
            }

            tempVisited.Remove(nodeId);
            visited.Add(nodeId);
            order.Add(nodeId);
        }

        /// <summary>
        /// Check if adding a connection would create a cycle
        /// </summary>
        private bool WouldCreateCycle(string sourceNodeId, string targetNodeId)
        {
            if (sourceNodeId == targetNodeId)
                return true;

            var visited = new HashSet<string>();
            var tempVisited = new HashSet<string>();

            return HasCycle(targetNodeId, sourceNodeId, visited, tempVisited);
        }

        /// <summary>
        /// Check for cycles using DFS
        /// </summary>
        private bool HasCycle(string currentNodeId, string targetNodeId, HashSet<string> visited, HashSet<string> tempVisited)
        {
            if (currentNodeId == targetNodeId)
                return true;

            if (tempVisited.Contains(currentNodeId))
                return false;

            if (visited.Contains(currentNodeId))
                return false;

            tempVisited.Add(currentNodeId);

            var outgoingConnections = _connections.Values.Where(c => c.SourceNodeId == currentNodeId);
            foreach (var connection in outgoingConnections)
            {
                if (HasCycle(connection.TargetNodeId, targetNodeId, visited, tempVisited))
                    return true;
            }

            tempVisited.Remove(currentNodeId);
            visited.Add(currentNodeId);
            return false;
        }

        #endregion

        #region Input Gathering

        /// <summary>
        /// Gather inputs for a specific node
        /// </summary>
        private Dictionary<string, object> GatherNodeInputs(string nodeId, Dictionary<string, object> nodeResults)
        {
            var inputs = new Dictionary<string, object>();

            var incomingConnections = _connections.Values.Where(c => c.TargetNodeId == nodeId);
            foreach (var connection in incomingConnections)
            {
                if (nodeResults.TryGetValue(connection.SourceNodeId, out var sourceOutput))
                {
                    inputs[connection.TargetPortName] = sourceOutput;
                }
            }

            return inputs;
        }

        #endregion

        #region Global Data Management

        /// <summary>
        /// Set global data that can be accessed by all nodes
        /// </summary>
        public void SetGlobalData(string key, object value)
        {
            lock (_processingLock)
            {
                _globalData[key] = value;
            }
        }

        /// <summary>
        /// Get global data
        /// </summary>
        public object? GetGlobalData(string key)
        {
            lock (_processingLock)
            {
                return _globalData.TryGetValue(key, out var value) ? value : null;
            }
        }

        /// <summary>
        /// Clear global data
        /// </summary>
        public void ClearGlobalData()
        {
            lock (_processingLock)
            {
                _globalData.Clear();
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate the entire graph
        /// </summary>
        public bool ValidateGraph()
        {
            lock (_processingLock)
            {
                // Check all nodes are valid
                foreach (var node in _nodes.Values)
                {
                    if (!node.ValidateConfiguration())
                        return false;
                }

                // Check for cycles
                try
                {
                    GetTopologicalOrder();
                }
                catch (InvalidOperationException)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Get validation errors
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            lock (_processingLock)
            {
                // Check node configurations
                foreach (var node in _nodes.Values)
                {
                    if (!node.ValidateConfiguration())
                    {
                        errors.Add($"Node '{node.Name}' has invalid configuration");
                    }
                }

                // Check for cycles
                try
                {
                    GetTopologicalOrder();
                }
                catch (InvalidOperationException)
                {
                    errors.Add("Graph contains circular dependencies");
                }

                // Check for orphaned connections
                foreach (var connection in _connections.Values)
                {
                    if (!_nodes.ContainsKey(connection.SourceNodeId))
                    {
                        errors.Add($"Connection references non-existent source node: {connection.SourceNodeId}");
                    }
                    if (!_nodes.ContainsKey(connection.TargetNodeId))
                    {
                        errors.Add($"Connection references non-existent target node: {connection.TargetNodeId}");
                    }
                }
            }

            return errors;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Clear the entire graph
        /// </summary>
        public void Clear()
        {
            lock (_processingLock)
            {
                _nodes.Clear();
                _connections.Clear();
                _globalData.Clear();
            }
        }

        /// <summary>
        /// Get graph statistics
        /// </summary>
        public GraphStatistics GetStatistics()
        {
            lock (_processingLock)
            {
                return new GraphStatistics
                {
                    NodeCount = _nodes.Count,
                    ConnectionCount = _connections.Count,
                    Categories = _nodes.Values.Select(n => n.Category).Distinct().Count(),
                    IsValid = ValidateGraph(),
                    LastProcessed = LastProcessed,
                    ProcessingTime = ProcessingTime
                };
            }
        }

        #endregion
    }
}