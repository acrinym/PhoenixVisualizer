using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects.Nodes;

namespace PhoenixVisualizer.Core.Effects
{
    /// <summary>
    /// EffectGraph - Core node-based effect chaining system for Phoenix Visualizer
    /// Provides a flexible, extensible architecture for creating complex visual effect chains
    /// </summary>
    public class EffectGraph : IDisposable
    {
        #region Properties

        /// <summary>
        /// Collection of all nodes in the graph
        /// </summary>
        public IReadOnlyList<IEffectNode> Nodes => _nodes.AsReadOnly();

        /// <summary>
        /// Collection of all connections between nodes
        /// </summary>
        public IReadOnlyList<EffectConnection> Connections => _connections.AsReadOnly();

        /// <summary>
        /// Root input node for the graph
        /// </summary>
        public InputNode RootInput { get; private set; }

        /// <summary>
        /// Final output node for the graph
        /// </summary>
        public OutputNode FinalOutput { get; private set; }

        /// <summary>
        /// Current processing state of the graph
        /// </summary>
        public EffectGraphState State { get; private set; }

        /// <summary>
        /// Graph execution statistics
        /// </summary>
        public EffectGraphStats Stats { get; private set; }

        /// <summary>
        /// Enable/disable parallel processing
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = true;

        /// <summary>
        /// Maximum number of parallel threads
        /// </summary>
        public int MaxParallelThreads { get; set; } = Environment.ProcessorCount;

        #endregion

        #region Private Fields

        private readonly List<IEffectNode> _nodes;
        private readonly List<EffectConnection> _connections;
        private readonly Dictionary<string, IEffectNode> _nodeLookup;
        private readonly object _graphLock;
        private bool _isDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize a new EffectGraph
        /// </summary>
        public EffectGraph()
        {
            _nodes = new List<IEffectNode>();
            _connections = new List<EffectConnection>();
            _nodeLookup = new Dictionary<string, IEffectNode>();
            _graphLock = new object();
            State = EffectGraphState.Initialized;
            Stats = new EffectGraphStats();

            // Create default input and output nodes
            RootInput = new InputNode();
            FinalOutput = new OutputNode();

            AddNode(RootInput);
            AddNode(FinalOutput);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add a node to the graph
        /// </summary>
        /// <param name="node">Node to add</param>
        /// <returns>True if node was added successfully</returns>
        public bool AddNode(IEffectNode node)
        {
            if (node == null || _isDisposed)
                return false;

            lock (_graphLock)
            {
                if (_nodeLookup.ContainsKey(node.Id))
                    return false;

                _nodes.Add(node);
                _nodeLookup[node.Id] = node;
                node.Graph = this;
                
                State = EffectGraphState.Modified;
                return true;
            }
        }

        /// <summary>
        /// Remove a node from the graph
        /// </summary>
        /// <param name="nodeId">ID of the node to remove</param>
        /// <returns>True if node was removed successfully</returns>
        public bool RemoveNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || _isDisposed)
                return false;

            lock (_graphLock)
            {
                if (!_nodeLookup.TryGetValue(nodeId, out var node))
                    return false;

                // Remove all connections involving this node
                _connections.RemoveAll(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId);

                // Remove the node
                _nodes.Remove(node);
                _nodeLookup.Remove(nodeId);
                node.Graph = null;

                State = EffectGraphState.Modified;
                return true;
            }
        }

        /// <summary>
        /// Connect two nodes
        /// </summary>
        /// <param name="sourceNodeId">Source node ID</param>
        /// <param name="sourcePort">Source port name</param>
        /// <param name="targetNodeId">Target node ID</param>
        /// <param name="targetPort">Target port name</param>
        /// <returns>True if connection was created successfully</returns>
        public bool ConnectNodes(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
        {
            if (string.IsNullOrEmpty(sourceNodeId) || string.IsNullOrEmpty(targetNodeId) || _isDisposed)
                return false;

            lock (_graphLock)
            {
                if (!_nodeLookup.ContainsKey(sourceNodeId) || !_nodeLookup.ContainsKey(targetNodeId))
                    return false;

                // Check for circular connections
                if (WouldCreateCycle(sourceNodeId, targetNodeId))
                    return false;

                var connection = new EffectConnection
                {
                    SourceNodeId = sourceNodeId,
                    SourcePort = sourcePort,
                    TargetNodeId = targetNodeId,
                    TargetPort = targetPort
                };

                _connections.Add(connection);
                State = EffectGraphState.Modified;
                return true;
            }
        }

        /// <summary>
        /// Disconnect nodes
        /// </summary>
        /// <param name="sourceNodeId">Source node ID</param>
        /// <param name="targetNodeId">Target node ID</param>
        /// <returns>True if connection was removed successfully</returns>
        public bool DisconnectNodes(string sourceNodeId, string targetNodeId)
        {
            if (string.IsNullOrEmpty(sourceNodeId) || string.IsNullOrEmpty(targetNodeId) || _isDisposed)
                return false;

            lock (_graphLock)
            {
                var removed = _connections.RemoveAll(c => 
                    c.SourceNodeId == sourceNodeId && c.TargetNodeId == targetNodeId);

                if (removed > 0)
                {
                    State = EffectGraphState.Modified;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Get a node by ID
        /// </summary>
        /// <param name="nodeId">Node ID to find</param>
        /// <returns>Node if found, null otherwise</returns>
        public IEffectNode GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || _isDisposed)
                return null;

            lock (_graphLock)
            {
                return _nodeLookup.TryGetValue(nodeId, out var node) ? node : null;
            }
        }

        /// <summary>
        /// Get all connections for a specific node
        /// </summary>
        /// <param name="nodeId">Node ID</param>
        /// <returns>List of connections involving the node</returns>
        public List<EffectConnection> GetNodeConnections(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || _isDisposed)
                return new List<EffectConnection>();

            lock (_graphLock)
            {
                return _connections.Where(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId).ToList();
            }
        }

        /// <summary>
        /// Execute the effect graph with input data
        /// </summary>
        /// <param name="inputData">Input data to process</param>
        /// <param name="audioFeatures">Audio features for beat-reactive effects</param>
        /// <returns>Processed output data</returns>
        public async Task<EffectOutput> ExecuteAsync(EffectInput inputData, AudioFeatures audioFeatures)
        {
            if (_isDisposed || State == EffectGraphState.Error)
                throw new InvalidOperationException("EffectGraph is not in a valid state for execution");

            try
            {
                State = EffectGraphState.Executing;
                Stats.ExecutionCount++;
                var startTime = DateTime.UtcNow;

                // Validate graph structure
                if (!ValidateGraph())
                {
                    State = EffectGraphState.Error;
                    throw new InvalidOperationException("EffectGraph validation failed");
                }

                // Execute the graph
                var output = await ExecuteGraphAsync(inputData, audioFeatures);

                // Update statistics
                Stats.LastExecutionTime = DateTime.UtcNow - startTime;
                Stats.TotalExecutionTime += Stats.LastExecutionTime;
                Stats.AverageExecutionTime = Stats.TotalExecutionTime.TotalMilliseconds / Stats.ExecutionCount;

                State = EffectGraphState.Ready;
                return output;
            }
            catch (Exception ex)
            {
                State = EffectGraphState.Error;
                Stats.ErrorCount++;
                throw new EffectGraphExecutionException("EffectGraph execution failed", ex);
            }
        }

        /// <summary>
        /// Validate the graph structure
        /// </summary>
        /// <returns>True if graph is valid</returns>
        public bool ValidateGraph()
        {
            if (_isDisposed)
                return false;

            lock (_graphLock)
            {
                // Check for orphaned nodes
                var connectedNodeIds = _connections
                    .SelectMany(c => new[] { c.SourceNodeId, c.TargetNodeId })
                    .Distinct()
                    .ToHashSet();

                // Root input and final output should always be connected
                if (!connectedNodeIds.Contains(RootInput.Id) || !connectedNodeIds.Contains(FinalOutput.Id))
                    return false;

                // Check for cycles
                if (HasCycles())
                    return false;

                // Validate node configurations
                foreach (var node in _nodes)
                {
                    if (!node.ValidateConfiguration())
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Reset the graph state
        /// </summary>
        public void Reset()
        {
            if (_isDisposed)
                return;

            lock (_graphLock)
            {
                foreach (var node in _nodes)
                {
                    node.Reset();
                }

                State = EffectGraphState.Ready;
                Stats.Reset();
            }
        }

        /// <summary>
        /// Clear all nodes and connections
        /// </summary>
        public void Clear()
        {
            if (_isDisposed)
                return;

            lock (_graphLock)
            {
                _connections.Clear();
                
                foreach (var node in _nodes)
                {
                    node.Graph = null;
                }
                
                _nodes.Clear();
                _nodeLookup.Clear();

                // Re-add default nodes
                AddNode(RootInput);
                AddNode(FinalOutput);

                State = EffectGraphState.Initialized;
                Stats.Reset();
            }
        }

        /// <summary>
        /// Get a topological sort of the nodes for execution order
        /// </summary>
        /// <returns>Nodes in execution order</returns>
        public List<IEffectNode> GetExecutionOrder()
        {
            if (_isDisposed)
                return new List<IEffectNode>();

            lock (_graphLock)
            {
                return TopologicalSort();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Execute the graph with the given input
        /// </summary>
        private async Task<EffectOutput> ExecuteGraphAsync(EffectInput inputData, AudioFeatures audioFeatures)
        {
            var executionOrder = GetExecutionOrder();
            var nodeOutputs = new Dictionary<string, object>();

            // Set input data
            nodeOutputs[RootInput.Id] = inputData;

            // Execute nodes in order
            foreach (var node in executionOrder)
            {
                if (node == RootInput)
                    continue;

                var inputs = GatherNodeInputs(node, nodeOutputs);
                var output = await ExecuteNodeAsync(node, inputs, audioFeatures);
                nodeOutputs[node.Id] = output;
            }

            // Return final output
            return nodeOutputs[FinalOutput.Id] as EffectOutput ?? new EffectOutput();
        }

        /// <summary>
        /// Gather all inputs for a specific node
        /// </summary>
        private Dictionary<string, object> GatherNodeInputs(IEffectNode node, Dictionary<string, object> nodeOutputs)
        {
            var inputs = new Dictionary<string, object>();

            var inputConnections = _connections.Where(c => c.TargetNodeId == node.Id);
            foreach (var connection in inputConnections)
            {
                if (nodeOutputs.TryGetValue(connection.SourceNodeId, out var sourceOutput))
                {
                    inputs[connection.TargetPort] = sourceOutput;
                }
            }

            return inputs;
        }

        /// <summary>
        /// Execute a single node
        /// </summary>
        private async Task<object> ExecuteNodeAsync(IEffectNode node, Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            try
            {
                if (node is IAsyncEffectNode asyncNode)
                {
                    return await asyncNode.ProcessAsync(inputs, audioFeatures);
                }
                else
                {
                    return node.Process(inputs, audioFeatures);
                }
            }
            catch (Exception ex)
            {
                throw new EffectNodeExecutionException($"Node {node.Id} execution failed", ex);
            }
        }

        /// <summary>
        /// Check if adding a connection would create a cycle
        /// </summary>
        private bool WouldCreateCycle(string sourceNodeId, string targetNodeId)
        {
            // Simple cycle detection - if target can reach source, adding this connection creates a cycle
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            
            stack.Push(targetNodeId);
            visited.Add(targetNodeId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                
                if (current == sourceNodeId)
                    return true;

                var outgoingConnections = _connections.Where(c => c.SourceNodeId == current);
                foreach (var connection in outgoingConnections)
                {
                    if (!visited.Contains(connection.TargetNodeId))
                    {
                        visited.Add(connection.TargetNodeId);
                        stack.Push(connection.TargetNodeId);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the graph has cycles
        /// </summary>
        private bool HasCycles()
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in _nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    if (HasCyclesDFS(node.Id, visited, recursionStack))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Depth-first search for cycle detection
        /// </summary>
        private bool HasCyclesDFS(string nodeId, HashSet<string> visited, HashSet<string> recursionStack)
        {
            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            var outgoingConnections = _connections.Where(c => c.SourceNodeId == nodeId);
            foreach (var connection in outgoingConnections)
            {
                if (!visited.Contains(connection.TargetNodeId))
                {
                    if (HasCyclesDFS(connection.TargetNodeId, visited, recursionStack))
                        return true;
                }
                else if (recursionStack.Contains(connection.TargetNodeId))
                {
                    return true;
                }
            }

            recursionStack.Remove(nodeId);
            return false;
        }

        /// <summary>
        /// Perform topological sort of nodes
        /// </summary>
        private List<IEffectNode> TopologicalSort()
        {
            var result = new List<IEffectNode>();
            var visited = new HashSet<string>();
            var tempVisited = new HashSet<string>();

            foreach (var node in _nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    TopologicalSortDFS(node.Id, visited, tempVisited, result);
                }
            }

            result.Reverse();
            return result;
        }

        /// <summary>
        /// Depth-first search for topological sort
        /// </summary>
        private void TopologicalSortDFS(string nodeId, HashSet<string> visited, HashSet<string> tempVisited, List<IEffectNode> result)
        {
            if (tempVisited.Contains(nodeId))
                throw new InvalidOperationException("Graph contains cycles");

            if (visited.Contains(nodeId))
                return;

            tempVisited.Add(nodeId);

            var outgoingConnections = _connections.Where(c => c.SourceNodeId == nodeId);
            foreach (var connection in outgoingConnections)
            {
                TopologicalSortDFS(connection.TargetNodeId, visited, tempVisited, result);
            }

            tempVisited.Remove(nodeId);
            visited.Add(nodeId);

            if (_nodeLookup.TryGetValue(nodeId, out var node))
            {
                result.Add(node);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;

            lock (_graphLock)
            {
                foreach (var node in _nodes)
                {
                    if (node is IDisposable disposableNode)
                    {
                        disposableNode.Dispose();
                    }
                }

                _nodes.Clear();
                _connections.Clear();
                _nodeLookup.Clear();
                _isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// Effect graph execution state
    /// </summary>
    public enum EffectGraphState
    {
        Initialized,
        Ready,
        Executing,
        Modified,
        Error
    }

    /// <summary>
    /// Effect graph execution statistics
    /// </summary>
    public class EffectGraphStats
    {
        public int ExecutionCount { get; set; }
        public int ErrorCount { get; set; }
        public TimeSpan LastExecutionTime { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public double AverageExecutionTime { get; set; }

        public void Reset()
        {
            ExecutionCount = 0;
            ErrorCount = 0;
            LastExecutionTime = TimeSpan.Zero;
            TotalExecutionTime = TimeSpan.Zero;
            AverageExecutionTime = 0;
        }
    }

    /// <summary>
    /// Exception thrown during effect graph execution
    /// </summary>
    public class EffectGraphExecutionException : Exception
    {
        public EffectGraphExecutionException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown during individual node execution
    /// </summary>
    public class EffectNodeExecutionException : Exception
    {
        public EffectNodeExecutionException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
