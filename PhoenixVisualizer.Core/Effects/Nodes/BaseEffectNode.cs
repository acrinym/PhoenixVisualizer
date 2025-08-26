using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Engine;

namespace PhoenixVisualizer.Core.Effects.Nodes
{
    /// <summary>
    /// Base class for all effect nodes providing common functionality
    /// </summary>
    public abstract class BaseEffectNode : IEffectNode
    {
        #region Properties

        public virtual string Id { get; protected set; } = string.Empty;
        public virtual string Name { get; protected set; } = string.Empty;
        public virtual string Description { get; protected set; } = string.Empty;
        public virtual string Category { get; protected set; } = string.Empty;
        public virtual Version Version { get; protected set; } = new Version(1, 0, 0);
        public virtual bool IsEnabled { get; set; } = true;

        public virtual IReadOnlyList<EffectPort> InputPorts => _inputPorts.AsReadOnly();
        public virtual IReadOnlyList<EffectPort> OutputPorts => _outputPorts.AsReadOnly();

        #endregion

        #region Protected Fields

        protected readonly List<EffectPort> _inputPorts;
        protected readonly List<EffectPort> _outputPorts;
        protected readonly object _processingLock;

        /// <summary>
        /// Shared Phoenix expression engine instance
        /// </summary>
        protected PhoenixExpressionEngine? Engine { get; private set; }

        #endregion

        #region Constructor

        protected BaseEffectNode()
        {
            _inputPorts = new List<EffectPort>();
            _outputPorts = new List<EffectPort>();
            _processingLock = new object();

            // Generate unique ID if not set
            if (string.IsNullOrEmpty(Id))
                Id = Guid.NewGuid().ToString();

            // Initialize ports after setting up collections
            InitializePorts();
        }

        #endregion

        #region Abstract Methods

        protected abstract void InitializePorts();
        protected abstract object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures);

        #endregion

        #region Public Methods

        public virtual object Process(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!IsEnabled)
                return GetDefaultOutput();

            lock (_processingLock)
            {
                try
                {
                    return ProcessCore(inputs, audioFeatures);
                }
                catch (Exception ex)
                {
                    OnProcessingError(ex);
                    return GetDefaultOutput();
                }
            }
        }

        public virtual bool ValidateConfiguration()
        {
            return _inputPorts.All(p => !p.IsRequired || !string.IsNullOrEmpty(p.Name)) &&
                   _outputPorts.All(p => !string.IsNullOrEmpty(p.Name));
        }

        public virtual void Reset()
        {
            lock (_processingLock)
            {
                OnReset();
            }
        }

        public virtual void Initialize()
        {
            lock (_processingLock)
            {
                OnInitialize();
            }
        }

        public virtual string GetSettingsSummary()
        {
            return $"{Name} ({Category}) - Enabled: {IsEnabled}";
        }

        #endregion

        #region Protected Virtual Methods

        protected virtual void OnReset() { }
        protected virtual void OnInitialize() { }
        protected virtual void OnProcessingError(Exception ex) { }
        public virtual object GetDefaultOutput() { return null!; }

        /// <summary>
        /// Bind a global expression engine to this node
        /// </summary>
        public virtual void BindExpressionEngine(PhoenixExpressionEngine engine)
        {
            Engine = engine;
        }

        #endregion
    }
}
