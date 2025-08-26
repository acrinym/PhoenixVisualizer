using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects.Nodes;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Effects.Models;

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
        public IEffectNode RootInput { get; private set; }

        /// <summary>
        /// Final output node for the graph
        /// </summary>
        public IEffectNode FinalOutput { get; private set; }

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
            _nodes = [];
            _connections = [];
            _nodeLookup = [];
            _graphLock = new object();
            State = EffectGraphState.Initialized;
            Stats = new EffectGraphStats();

            // Create default input and output nodes using factory pattern
            RootInput = CreateInputNode();
            FinalOutput = CreateOutputNode();

            _ = AddNode(RootInput);
            _ = AddNode(FinalOutput);
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Factory method to create input nodes
        /// </summary>
        protected virtual IEffectNode CreateInputNode() => new InputNode();

        /// <summary>
        /// Factory method to create output nodes
        /// </summary>
        protected virtual IEffectNode CreateOutputNode() => new OutputNode();

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
                _ = _connections.RemoveAll(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId);

                // Remove the node
                _ = _nodes.Remove(node);
                _ = _nodeLookup.Remove(nodeId);
                
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

                EffectConnection connection = new EffectConnection
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
                int removed = _connections.RemoveAll(c => 
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
                    // No need to set Graph property - removed circular dependency
                }
                
                _nodes.Clear();
                _nodeLookup.Clear();

                // Re-add default nodes
                _ = AddNode(RootInput);
                _ = AddNode(FinalOutput);

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
            List<IEffectNode> executionOrder = GetExecutionOrder();
            Dictionary<string, object> nodeOutputs = new Dictionary<string, object>();

            // Set input data
            nodeOutputs[RootInput.Id] = inputData;

            // Execute nodes in order
            foreach (IEffectNode node in executionOrder)
            {
                if (node == RootInput)
                    continue;

                Dictionary<string, object> inputs = GatherNodeInputs(node, nodeOutputs);
                object output = await ExecuteNodeAsync(node, inputs, audioFeatures);
                nodeOutputs[node.Id] = output;
            }

            // Return final output
            return nodeOutputs[FinalOutput.Id] as EffectOutput ?? new EffectOutput { Image = new ImageBuffer(640, 480) };
        }

        /// <summary>
        /// Gather all inputs for a specific node
        /// </summary>
        private Dictionary<string, object> GatherNodeInputs(IEffectNode node, Dictionary<string, object> nodeOutputs)
        {
            Dictionary<string, object> inputs = new Dictionary<string, object>();

            IEnumerable<EffectConnection> inputConnections = _connections.Where(c => c.TargetNodeId == node.Id);
            foreach (EffectConnection connection in inputConnections)
            {
                if (nodeOutputs.TryGetValue(connection.SourceNodeId, out object? sourceOutput) && sourceOutput != null)
                {
                    inputs[connection.TargetPort] = sourceOutput;
                }
            }

            return inputs;
        }

        /// <summary>
        /// Execute a single node
        /// </summary>
        private static async Task<object> ExecuteNodeAsync(IEffectNode node, Dictionary<string, object> inputs, AudioFeatures audioFeatures)
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
                // Log error and return default output
                Console.WriteLine($"Error executing node {node.Name}: {ex.Message}");
                return node.GetDefaultOutput();
            }
        }

        /// <summary>
        /// Check if adding a connection would create a cycle
        /// </summary>
        private bool WouldCreateCycle(string sourceId, string targetId)
        {
            // Simple cycle detection - if target can reach source, it would create a cycle
            HashSet<string> visited = new HashSet<string>();
            Stack<string> stack = new Stack<string>();
            
            stack.Push(targetId);
            _ = visited.Add(targetId);

            while (stack.Count > 0)
            {
                string current = stack.Pop();
                
                if (current == sourceId)
                    return true;

                IEnumerable<EffectConnection> outgoingConnections = _connections.Where(c => c.SourceNodeId == current);
                foreach (EffectConnection connection in outgoingConnections)
                {
                    if (!visited.Contains(connection.TargetNodeId))
                    {
                        _ = visited.Add(connection.TargetNodeId);
                        stack.Push(connection.TargetNodeId);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Perform topological sort of nodes
        /// </summary>
        private List<IEffectNode> TopologicalSort()
        {
            List<IEffectNode> result = new List<IEffectNode>();
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> temp = new HashSet<string>();

            foreach (IEffectNode node in _nodes)
            {
                if (!visited.Contains(node.Id))
                {
                    TopologicalSortVisit(node.Id, visited, temp, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Recursive helper for topological sort
        /// </summary>
        private void TopologicalSortVisit(string nodeId, HashSet<string> visited, HashSet<string> temp, List<IEffectNode> result)
        {
            if (temp.Contains(nodeId))
                throw new InvalidOperationException("Graph contains cycles");

            if (visited.Contains(nodeId))
                return;

            _ = temp.Add(nodeId);

            IEnumerable<EffectConnection> outgoingConnections = _connections.Where(c => c.SourceNodeId == nodeId);
            foreach (EffectConnection connection in outgoingConnections)
            {
                TopologicalSortVisit(connection.TargetNodeId, visited, temp, result);
            }

            _ = temp.Remove(nodeId);
            _ = visited.Add(nodeId);

            IEffectNode node = _nodeLookup[nodeId];
            result.Add(node);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Clear all nodes and connections
            Clear();

            // Dispose of nodes that implement IDisposable
            foreach (IEffectNode node in _nodes)
            {
                if (node is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _nodes.Clear();
            _nodeLookup.Clear();
            _connections.Clear();
            
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
    public class EffectGraphExecutionException(string message, Exception innerException) 
        : Exception(message, innerException)
    {
    }

    /// <summary>
    /// Exception thrown during individual node execution
    /// </summary>
    public class EffectNodeExecutionException(string message, Exception innerException) 
        : Exception(message, innerException)
    {
    }
}
