using System;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Services;

namespace PhoenixVisualizer.Core.Services
{
    /// <summary>
    /// Bridges the AVS editor with the main visualization window
    /// </summary>
    public class AvsEditorBridge
    {
        private AvsExecutionEngine? _currentEngine;
        private IAvsRenderer? _renderer;
        private IAvsAudioProvider? _audioProvider;
        
        public event EventHandler<AvsPresetEventArgs>? PresetLoaded;
        public event EventHandler<AvsPresetEventArgs>? PresetStarted;
        public event EventHandler<AvsPresetEventArgs>? PresetStopped;
        public event EventHandler<AvsErrorEventArgs>? ErrorOccurred;
        
        public bool IsPresetRunning => _currentEngine?.IsRunning ?? false;
        public AvsPreset? CurrentPreset { get; private set; }
        
        /// <summary>
        /// Sets the renderer for the bridge
        /// </summary>
        public void SetRenderer(IAvsRenderer renderer)
        {
            _renderer = renderer;
        }
        
        /// <summary>
        /// Sets the audio provider for the bridge
        /// </summary>
        public void SetAudioProvider(IAvsAudioProvider audioProvider)
        {
            _audioProvider = audioProvider;
        }
        
        /// <summary>
        /// Loads a preset from the editor and prepares it for execution
        /// </summary>
        public async Task<bool> LoadPresetAsync(AvsPreset preset)
        {
            try
            {
                if (preset == null)
                {
                    throw new ArgumentNullException(nameof(preset));
                }
                
                // Stop current preset if running
                if (_currentEngine?.IsRunning == true)
                {
                    await StopCurrentPresetAsync();
                }
                
                // Validate preset
                if (!ValidatePreset(preset))
                {
                    throw new InvalidOperationException("Preset validation failed");
                }
                
                // Store the preset
                CurrentPreset = preset;
                
                // Notify preset loaded
                OnPresetLoaded(new AvsPresetEventArgs(preset, "Preset loaded successfully"));
                
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Failed to load preset"));
                return false;
            }
        }
        
        /// <summary>
        /// Starts executing the loaded preset
        /// </summary>
        public async Task<bool> StartPresetAsync()
        {
            try
            {
                if (CurrentPreset == null)
                {
                    throw new InvalidOperationException("No preset loaded");
                }
                
                if (_renderer == null)
                {
                    throw new InvalidOperationException("No renderer set");
                }
                
                if (_audioProvider == null)
                {
                    throw new InvalidOperationException("No audio provider set");
                }
                
                // Create new execution engine
                _currentEngine = new AvsExecutionEngine(CurrentPreset, _renderer, _audioProvider);
                
                        // Wire up events
        _currentEngine.FrameRendered += OnFrameRendered;
        _currentEngine.BeatDetected += OnEngineBeatDetected;
        _currentEngine.ErrorOccurred += OnEngineError;
                
                // Start the engine
                await _currentEngine.StartAsync();
                
                // Notify preset started
                OnPresetStarted(new AvsPresetEventArgs(CurrentPreset, "Preset started successfully"));
                
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Failed to start preset"));
                return false;
            }
        }
        
        /// <summary>
        /// Stops the currently running preset
        /// </summary>
        public async Task<bool> StopPresetAsync()
        {
            try
            {
                if (_currentEngine?.IsRunning == true)
                {
                    await StopCurrentPresetAsync();
                    OnPresetStopped(new AvsPresetEventArgs(CurrentPreset!, "Preset stopped"));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Failed to stop preset"));
                return false;
            }
        }
        
        /// <summary>
        /// Updates the current preset and restarts execution if needed
        /// </summary>
        public async Task<bool> UpdatePresetAsync(AvsPreset updatedPreset)
        {
            try
            {
                if (updatedPreset == null)
                {
                    throw new ArgumentNullException(nameof(updatedPreset));
                }
                
                var wasRunning = _currentEngine?.IsRunning ?? false;
                
                // Stop current execution
                if (wasRunning)
                {
                    await StopCurrentPresetAsync();
                }
                
                // Load the updated preset
                var success = await LoadPresetAsync(updatedPreset);
                if (!success) return false;
                
                // Restart if it was running
                if (wasRunning)
                {
                    return await StartPresetAsync();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Failed to update preset"));
                return false;
            }
        }
        
        /// <summary>
        /// Gets the current execution status
        /// </summary>
        public AvsExecutionStatus GetExecutionStatus()
        {
            if (_currentEngine == null)
            {
                return new AvsExecutionStatus
                {
                    IsRunning = false,
                    FPS = 0.0,
                    BPM = 0.0f,
                    BeatDetected = false,
                    FrameCount = 0,
                    ErrorCount = 0
                };
            }
            
            return new AvsExecutionStatus
            {
                IsRunning = _currentEngine.IsRunning,
                FPS = _currentEngine.FPS,
                BPM = _currentEngine.BPM,
                BeatDetected = _currentEngine.IsBeatDetected,
                FrameCount = (int)_currentEngine.FrameCount,
                ErrorCount = (int)_currentEngine.ErrorCount
            };
        }
        
        /// <summary>
        /// Takes a screenshot of the current visualization
        /// </summary>
        public async Task<object?> TakeScreenshotAsync()
        {
            try
            {
                if (_renderer == null) return null;
                return await _renderer.TakeScreenshotAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Failed to take screenshot"));
                return null;
            }
        }
        
        /// <summary>
        /// Gets the current frame buffer
        /// </summary>
        public async Task<object?> GetFrameBufferAsync()
        {
            try
            {
                if (_renderer == null) return null;
                return await _renderer.GetFrameBufferAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Failed to get frame buffer"));
                return null;
            }
        }
        
        /// <summary>
        /// Validates a preset before execution
        /// </summary>
        private bool ValidatePreset(AvsPreset preset)
        {
            if (string.IsNullOrEmpty(preset.Name))
            {
                OnErrorOccurred(new AvsErrorEventArgs(new InvalidOperationException("Preset name is required"), "Preset validation"));
                return false;
            }
            
            // Check if preset has any effects
            var totalEffects = preset.InitEffects.Count + preset.BeatEffects.Count + 
                              preset.FrameEffects.Count + preset.PointEffects.Count;
            
            if (totalEffects == 0)
            {
                OnErrorOccurred(new AvsErrorEventArgs(new InvalidOperationException("Preset must contain at least one effect"), "Preset validation"));
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Stops the current preset execution
        /// </summary>
        private async Task StopCurrentPresetAsync()
        {
            if (_currentEngine != null)
            {
                // Unwire events
                _currentEngine.FrameRendered -= OnFrameRendered;
                _currentEngine.BeatDetected -= OnEngineBeatDetected;
                _currentEngine.ErrorOccurred -= OnEngineError;
                
                // Stop and dispose
                await _currentEngine.StopAsync();
                _currentEngine.Dispose();
                _currentEngine = null;
            }
        }
        
        // Event handlers for the execution engine
        private void OnFrameRendered(object? sender, AvsRenderEventArgs e)
        {
            // Forward frame rendered events to the main window
            // This allows real-time updates of the visualization
        }
        
        private void OnEngineBeatDetected(object? sender, AvsBeatEventArgs e)
        {
            // Forward beat detection events to the main window
            // This allows beat-reactive effects
        }
        
        private void OnEngineError(object? sender, AvsErrorEventArgs e)
        {
            // Forward engine errors
            OnErrorOccurred(e);
        }
        
        // Event raising methods
        protected virtual void OnPresetLoaded(AvsPresetEventArgs e)
        {
            PresetLoaded?.Invoke(this, e);
        }
        
        protected virtual void OnPresetStarted(AvsPresetEventArgs e)
        {
            PresetStarted?.Invoke(this, e);
        }
        
        protected virtual void OnPresetStopped(AvsPresetEventArgs e)
        {
            PresetStopped?.Invoke(this, e);
        }
        
        protected virtual void OnErrorOccurred(AvsErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
        
        public void Dispose()
        {
            StopCurrentPresetAsync().Wait();
            _currentEngine?.Dispose();
        }
    }
    
    // Event argument classes
    public class AvsPresetEventArgs : EventArgs
    {
        public AvsPreset Preset { get; }
        public string Message { get; }
        
        public AvsPresetEventArgs(AvsPreset preset, string message)
        {
            Preset = preset;
            Message = message;
        }
    }
    
    public class AvsExecutionStatus
    {
        public bool IsRunning { get; set; }
        public double FPS { get; set; }
        public float BPM { get; set; }
        public bool BeatDetected { get; set; }
        public int FrameCount { get; set; }
        public int ErrorCount { get; set; }
    }
}
