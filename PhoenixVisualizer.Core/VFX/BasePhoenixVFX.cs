using System;
using System.Collections.Generic;
using System.Linq;
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
        #region Protected Fields

        protected PhoenixExpressionEngine _pel;
        protected VFXRenderContext _context;
        protected AudioFeatures _audio;
        protected Dictionary<string, VFXParameter> _parameters;
        protected bool _initialized = false;
        protected string _vfxId;
        protected string _vfxName;
        protected string _vfxCategory;

        #endregion

        #region Properties

        /// <summary>
        /// Unique identifier for this VFX effect
        /// </summary>
        public string Id => _vfxId;

        /// <summary>
        /// Display name for this VFX effect
        /// </summary>
        public string Name => _vfxName;

        /// <summary>
        /// Category this VFX effect belongs to
        /// </summary>
        public string Category => _vfxCategory;

        /// <summary>
        /// Whether this VFX effect is currently enabled
        /// </summary>
        public virtual bool Enabled { get; set; } = true;

        /// <summary>
        /// Opacity/Alpha for the effect output (0.0 to 1.0)
        /// </summary>
        public virtual float Opacity { get; set; } = 1.0f;

        /// <summary>
        /// All discoverable parameters for this VFX
        /// </summary>
        public Dictionary<string, VFXParameter> Parameters => _parameters ??= DiscoverParameters();

        /// <summary>
        /// Performance metrics for this VFX
        /// </summary>
        public VFXPerformanceMetrics Performance { get; protected set; } = new VFXPerformanceMetrics();

        #endregion

        #region Constructor

        protected BasePhoenixVFX()
        {
            _pel = new PhoenixExpressionEngine();
            DiscoverVFXMetadata();
            InitializeVFX();
        }

        #endregion

        #region Initialization

        private void DiscoverVFXMetadata()
        {
            var vfxAttr = GetType().GetCustomAttribute<PhoenixVFXAttribute>();
            if (vfxAttr != null)
            {
                _vfxId = vfxAttr.Id;
                _vfxName = vfxAttr.Name;
                _vfxCategory = vfxAttr.Category;
            }
            else
            {
                _vfxId = GetType().Name.Replace("Phoenix", "").Replace("VFX", "").ToLowerInvariant();
                _vfxName = GetType().Name;
                _vfxCategory = "Unknown";
            }
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
                        Type = prop.PropertyType,
                        MinValue = paramAttr.MinValue,
                        MaxValue = paramAttr.MaxValue,
                        DefaultValue = paramAttr.DefaultValue,
                        Property = prop,
                        EnumValues = paramAttr.EnumValues
                    };

                    parameters[parameter.Id] = parameter;
                }
            }

            return parameters;
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
                    OnInitialize(context);
                    _initialized = true;
                }

                // Execute PEL scripts
                ExecuteScripts();

                // Update parameters from PEL variables
                UpdateParametersFromPEL();

                // Choose processing path based on capabilities
                if (context.SupportsGPU && SupportsGPUProcessing())
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
        protected virtual void OnInitialize(VFXRenderContext context) { }

        /// <summary>
        /// Override for GPU-accelerated processing
        /// </summary>
        protected virtual void ProcessFrameGPU(VFXRenderContext context)
        {
            // Default: fall back to CPU processing
            ProcessFrameCPU(context);
        }

        /// <summary>
        /// Override for CPU-based processing (required)
        /// </summary>
        protected abstract void ProcessFrameCPU(VFXRenderContext context);

        /// <summary>
        /// Override to indicate GPU processing support
        /// </summary>
        protected virtual bool SupportsGPUProcessing() => false;

        #endregion

        #region Script Integration

        /// <summary>
        /// Execute PEL scripts if they exist
        /// </summary>
        protected virtual void ExecuteScripts()
        {
            var scriptProperties = GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<VFXScriptAttribute>() != null)
                .ToList();

            foreach (var prop in scriptProperties)
            {
                var scriptAttr = prop.GetCustomAttribute<VFXScriptAttribute>();
                var script = prop.GetValue(this) as string;

                if (!string.IsNullOrWhiteSpace(script))
                {
                    try
                    {
                        // Set audio variables before execution
                        SetAudioVariables();

                        // Execute script based on type
                        switch (scriptAttr.Type.ToLowerInvariant())
                        {
                            case "init":
                                if (!_initialized) _pel.Execute(script);
                                break;
                            case "frame":
                                _pel.Execute(script);
                                break;
                            case "beat":
                                if (_audio?.Beat == true) _pel.Execute(script);
                                break;
                            default:
                                _pel.Execute(script);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        OnScriptError(scriptAttr.Type, script, ex);
                    }
                }
            }
        }

        private void SetAudioVariables()
        {
            if (_audio == null) return;

            _pel.Set("beat", _audio.Beat ? 1.0 : 0.0);
            _pel.Set("bass", _audio.Bass);
            _pel.Set("mid", _audio.Mid);
            _pel.Set("treble", _audio.Treble);
            _pel.Set("rms", _audio.RMS);
            _pel.Set("peak", _audio.Peak);

            // Time variables
            _pel.Set("time", _context?.FrameTime ?? 0.0);
            _pel.Set("frame", _context?.FrameNumber ?? 0.0);
            _pel.Set("dt", _context?.DeltaTime ?? 0.016);

            // Canvas dimensions
            if (_context?.Canvas != null)
            {
                _pel.Set("w", _context.Canvas.Width);
                _pel.Set("h", _context.Canvas.Height);
            }
        }

        private void UpdateParametersFromPEL()
        {
            foreach (var param in Parameters.Values)
            {
                if (param.Property.CanWrite)
                {
                    var pelValue = _pel.Get(param.Id, Convert.ToDouble(param.DefaultValue));
                    
                    try
                    {
                        var convertedValue = ConvertPELValue(pelValue, param.Type);
                        param.Property.SetValue(this, convertedValue);
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
            if (Math.Abs(Opacity - 1.0f) > 0.001f && context.Canvas != null)
            {
                ApplyOpacity(context.Canvas, Opacity);
            }
        }

        private void ApplyOpacity(ImageBuffer canvas, float opacity)
        {
            var alpha = (uint)(opacity * 255);
            
            for (int i = 0; i < canvas.Data.Length; i++)
            {
                uint pixel = canvas.Data[i];
                uint currentAlpha = (pixel >> 24) & 0xFF;
                uint newAlpha = (currentAlpha * alpha) / 255;
                
                canvas.Data[i] = (pixel & 0x00FFFFFF) | (newAlpha << 24);
            }
        }

        #endregion

        #region Parameter Management

        /// <summary>
        /// Get parameter value by ID
        /// </summary>
        public T GetParameter<T>(string parameterId)
        {
            if (Parameters.TryGetValue(parameterId, out var param))
            {
                return (T)param.Property.GetValue(this);
            }
            return default(T);
        }

        /// <summary>
        /// Set parameter value by ID
        /// </summary>
        public void SetParameter(string parameterId, object value)
        {
            if (Parameters.TryGetValue(parameterId, out var param) && param.Property.CanWrite)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(value, param.Type);
                    param.Property.SetValue(this, convertedValue);
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
                result[param.Id] = param.Property.GetValue(this);
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

        public void Initialize()
        {
            if (!_initialized)
            {
                OnInitialize(_context);
                _initialized = true;
            }
        }

        public void Render(VFXRenderContext context)
        {
            if (!Enabled) return;
            ProcessFrame(context, _audio);
        }

        #endregion

        #region Disposal

        public virtual void Dispose()
        {
            _pel = null;
            _context = null;
            _audio = null;
            _parameters?.Clear();
        }

        #endregion
    }
}