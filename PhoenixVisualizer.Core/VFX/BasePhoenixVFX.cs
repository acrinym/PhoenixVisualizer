using System;
using System.Collections.Generic;
using System.Reflection;
using PhoenixVisualizer.Core.Engine;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.VFX
{
    /// <summary>
    /// Base class for all Phoenix VFX effects
    /// Provides modern VFX architecture with PEL integration, GPU acceleration hooks, and automatic parameter discovery
    /// </summary>
    public abstract class BasePhoenixVFX : IPhoenixVFX
    {
        #region Fields

        private readonly PhoenixExpressionEngine _pel;
        protected VFXRenderContext? _context;
        protected AudioFeatures? _audio;
        private Dictionary<string, VFXParameter> _parameters = new();
        private readonly Dictionary<string, PropertyInfo> _parameterProperties = new();
        private bool _initialized;
        private string _vfxId = string.Empty;
        private string _vfxName = string.Empty;
        private string _vfxCategory = string.Empty;
        private string _vfxVersion = string.Empty;
        private string _vfxAuthor = string.Empty;
        private string _vfxDescription = string.Empty;

        #endregion

        #region Properties

        public string Id => _vfxId;
        public string Name => _vfxName;
        public string Category => _vfxCategory;
        public string Version => _vfxVersion;
        public string Author => _vfxAuthor;
        public string Description => _vfxDescription;
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1.0f;
        public VFXPerformanceMetrics Performance { get; } = new();
        public Dictionary<string, VFXParameter> Parameters => _parameters;

        #endregion

        #region Constructor

        protected BasePhoenixVFX()
        {
            _pel = new PhoenixExpressionEngine();
            _parameters = new Dictionary<string, VFXParameter>();
            _parameterProperties = new Dictionary<string, PropertyInfo>();
            
            DiscoverVFXMetadata();
            StoreParameterProperties();
            InitializeVFX();
        }

        #endregion

        #region VFX Metadata Discovery

        private void DiscoverVFXMetadata()
        {
            var vfxAttr = GetType().GetCustomAttribute<PhoenixVFXAttribute>();
            if (vfxAttr != null)
            {
                _vfxId = vfxAttr.Id;
                _vfxName = vfxAttr.Name;
                _vfxCategory = vfxAttr.Category;
                _vfxVersion = vfxAttr.Version;
                _vfxAuthor = vfxAttr.Author;
                _vfxDescription = vfxAttr.Description;
            }
            else
            {
                _vfxId = GetType().Name;
                _vfxName = GetType().Name;
                _vfxCategory = "Uncategorized";
                _vfxVersion = "1.0.0";
                _vfxAuthor = "Unknown";
                _vfxDescription = "No description available";
            }

            _parameters = DiscoverParameters();
        }

        private Dictionary<string, VFXParameter> DiscoverParameters()
        {
            var parameters = new Dictionary<string, VFXParameter>();
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var paramAttr = prop.GetCustomAttribute<VFXParameterAttribute>();
                if (paramAttr != null)
                {
                    var parameter = new VFXParameter
                    {
                        Id = paramAttr.Id,
                        Name = paramAttr.Name ?? prop.Name,
                        Description = paramAttr.Description ?? $"{prop.Name} parameter",
                        ParameterType = prop.PropertyType,
                        MinValue = paramAttr.MinValue,
                        MaxValue = paramAttr.MaxValue,
                        DefaultValue = prop.GetValue(this)
                    };

                    parameters[parameter.Id] = parameter;
                }
            }

            return parameters;
        }
        
        private void StoreParameterProperties()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var paramAttr = prop.GetCustomAttribute<VFXParameterAttribute>();
                if (paramAttr != null)
                {
                    _parameterProperties[paramAttr.Id] = prop;
                }
            }
        }

        protected virtual void InitializeVFX()
        {
            // Override in derived classes for custom initialization
        }

        #endregion

        #region VFX Processing

        /// <summary>
        /// Main processing entry point for the VFX effect
        /// </summary>
        public void ProcessFrame(VFXRenderContext context, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            var startTime = DateTime.UtcNow;

            try
            {
                _context = context;
                _audio = audioFeatures;

                // Initialize on first frame
                if (!_initialized)
                {
                    OnInitialize(context, audioFeatures);
                    _initialized = true;
                }

                // Execute PEL scripts
                ExecuteScripts();

                // Update parameters from PEL variables
                UpdateParametersFromPEL();

                // Choose processing path based on capabilities
                if (SupportsGPUProcessing())
                {
                    ProcessFrameGPU(context);
                }
                else
                {
                    ProcessFrameCPU(context);
                }

                // Apply post-processing effects
                ApplyPostProcessing(context);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                // Update performance metrics
                var endTime = DateTime.UtcNow;
                Performance.UpdateFrameTime((endTime - startTime).TotalMilliseconds);
            }
        }

        /// <summary>
        /// Override for custom initialization logic
        /// </summary>
        protected virtual void OnInitialize(VFXRenderContext context, AudioFeatures audioFeatures) { }

        /// <summary>
        /// Override for custom frame processing logic
        /// </summary>
        protected virtual void OnProcessFrame(VFXRenderContext context) { }

        /// <summary>
        /// Override for custom GPU processing logic
        /// </summary>
        protected virtual void ProcessFrameGPU(VFXRenderContext context)
        {
            // Default GPU implementation falls back to CPU
            ProcessFrameCPU(context);
        }

        /// <summary>
        /// Override for custom CPU processing logic
        /// </summary>
        protected virtual void ProcessFrameCPU(VFXRenderContext context)
        {
            OnProcessFrame(context);
        }

        /// <summary>
        /// Override to indicate GPU processing support
        /// </summary>
        protected virtual bool SupportsGPUProcessing() => false;

        #endregion

        #region PEL Script Execution

        private void ExecuteScripts()
        {
            if (_context == null || _audio == null) return;

            // Set up PEL context variables
            SetupPELContext();

            // Execute any active scripts
            ExecuteActiveScripts();
        }

        private void SetupPELContext()
        {
            if (_context == null || _audio == null) return;

            // Audio variables
            _pel.Set("bass", _audio.Bass);
            _pel.Set("mid", _audio.Mid);
            _pel.Set("treble", _audio.Treble);
            _pel.Set("rms", _audio.RMS);
            _pel.Set("peak", _audio.Peak);

            // Time variables
            _pel.Set("time", _context.Time);
            _pel.Set("frame", _context.FrameNumber);
            _pel.Set("dt", _context.DeltaTime);

            // Canvas dimensions
            _pel.Set("w", _context.Width);
            _pel.Set("h", _context.Height);
        }

        private void ExecuteActiveScripts()
        {
            // This would execute any active PEL scripts
            // For now, just update the context
        }

        private void UpdateParametersFromPEL()
        {
            foreach (var param in Parameters.Values)
            {
                if (_parameterProperties.TryGetValue(param.Id, out var prop) && prop.CanWrite)
                {
                    var pelValue = _pel.Get(param.Id, Convert.ToDouble(param.DefaultValue ?? 0.0));
                    
                    try
                    {
                        var convertedValue = ConvertPELValue(pelValue, param.ParameterType);
                        prop.SetValue(this, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        OnParameterUpdateError(param.Id, pelValue, ex);
                    }
                }
            }
        }

        private object ConvertPELValue(double pelValue, Type targetType)
        {
            if (targetType == typeof(bool))
                return pelValue != 0.0;
            else if (targetType == typeof(int))
                return (int)Math.Round(pelValue);
            else if (targetType == typeof(float))
                return (float)pelValue;
            else if (targetType == typeof(double))
                return pelValue;
            else if (targetType.IsEnum)
                return Enum.ToObject(targetType, (int)Math.Round(pelValue));
            else
                return Convert.ChangeType(pelValue, targetType);
        }

        #endregion

        #region Post-Processing

        protected virtual void ApplyPostProcessing(VFXRenderContext context)
        {
            // Apply opacity if not at full
            if (Math.Abs(Opacity - 1.0f) > 0.001f)
            {
                // Note: Canvas processing would be implemented in derived classes
                // For now, just update the opacity in the context
                context.BackgroundColor = System.Drawing.Color.FromArgb(
                    (int)(Opacity * 255), 
                    context.BackgroundColor.R, 
                    context.BackgroundColor.G, 
                    context.BackgroundColor.B);
            }
        }

        #endregion

        #region Parameter Management

        /// <summary>
        /// Get parameter value by ID
        /// </summary>
        public T GetParameter<T>(string parameterId)
        {
            if (Parameters.TryGetValue(parameterId, out var param) && _parameterProperties.TryGetValue(parameterId, out var prop))
            {
                try
                {
                    return (T)prop.GetValue(this);
                }
                catch
                {
                    return default(T);
                }
            }
            return default(T);
        }

        /// <summary>
        /// Set parameter value by ID
        /// </summary>
        public void SetParameter(string parameterId, object value)
        {
            if (Parameters.TryGetValue(parameterId, out var param) && _parameterProperties.TryGetValue(parameterId, out var prop) && prop.CanWrite)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(value, param.ParameterType);
                    prop.SetValue(this, convertedValue);
                }
                catch (Exception ex)
                {
                    OnParameterUpdateError(parameterId, value, ex);
                }
            }
        }

        /// <summary>
        /// Get all parameter values as dictionary
        /// </summary>
        public Dictionary<string, object> GetAllParameters()
        {
            var result = new Dictionary<string, object>();
            foreach (var param in Parameters.Values)
            {
                if (_parameterProperties.TryGetValue(param.Id, out var prop))
                {
                    result[param.Id] = prop.GetValue(this);
                }
            }
            return result;
        }

        /// <summary>
        /// Set multiple parameter values from dictionary
        /// </summary>
        public void SetAllParameters(Dictionary<string, object> parameters)
        {
            foreach (var kvp in parameters)
            {
                SetParameter(kvp.Key, kvp.Value);
            }
        }

        #endregion

        #region Error Handling

        protected virtual void OnError(Exception ex)
        {
            Performance.RecordError(ex);
            System.Diagnostics.Debug.WriteLine($"[{Name}] VFX Error: {ex.Message}");
        }

        protected virtual void OnScriptError(string scriptType, string script, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{Name}] Script Error ({scriptType}): {ex.Message}");
        }

        protected virtual void OnParameterUpdateError(string parameterId, object value, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{Name}] Parameter Update Error ({parameterId}): {ex.Message}");
        }

        #endregion

        #region IPhoenixVFX Interface Implementation

        public void Initialize(VFXRenderContext context, AudioFeatures audio)
        {
            _context = context;
            _audio = audio;
            
            if (!_initialized)
            {
                OnInitialize(context, audio);
                _initialized = true;
            }
        }

        public void ProcessFrame(VFXRenderContext context)
        {
            if (!Enabled) return;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                OnProcessFrame(context);
                Performance.UpdateFrameTime(stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        #endregion

        #region IDisposable

        public virtual void Dispose()
        {
            // Clean up resources
            // Note: PhoenixExpressionEngine doesn't implement IDisposable
        }

        #endregion
    }
}