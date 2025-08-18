using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Services;

namespace PhoenixVisualizer.Core.Services
{
    /// <summary>
    /// Executes AVS presets and renders visualizations in real-time
    /// </summary>
    public class AvsExecutionEngine : IDisposable
    {
        private readonly AvsPreset _currentPreset;
        private readonly IAvsRenderer _renderer;
        private readonly IAvsAudioProvider _audioProvider;
        
        private CancellationTokenSource _executionCancellation;
        private Task? _executionTask;
        private bool _isRunning;
        private bool _isDisposed;
        
        // Execution state
        private readonly Dictionary<string, object> _variables = new();
        private Dictionary<string, object> _audioData = new();
        private DateTime _lastFrameTime;
        private int _frameCount;
        private bool _beatDetected;
        private float _bpm = 120.0f;
        
        // Performance tracking
        private readonly Queue<double> _frameTimes = new(60);
        private double _averageFrameTime;
        
        public event EventHandler<AvsRenderEventArgs>? FrameRendered;
        public event EventHandler<AvsBeatEventArgs>? BeatDetected;
        public event EventHandler<AvsErrorEventArgs>? ErrorOccurred;
        
        public bool IsRunning => _isRunning;
        public double FPS => _frameTimes.Count > 0 ? 1000.0 / _averageFrameTime : 0.0;
        public float BPM => _bpm;
        public bool IsBeatDetected => _beatDetected;
        
        public AvsExecutionEngine(AvsPreset preset, IAvsRenderer renderer, IAvsAudioProvider audioProvider)
        {
            _currentPreset = preset ?? throw new ArgumentNullException(nameof(preset));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _audioProvider = audioProvider ?? throw new ArgumentNullException(nameof(audioProvider));
            
            _executionCancellation = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Starts the AVS execution engine
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning) return;
            
            try
            {
                _isRunning = true;
                _lastFrameTime = DateTime.Now;
                _frameCount = 0;
                _beatDetected = false;
                
                // Initialize variables from preset
                await InitializePresetAsync();
                
                // Start the main execution loop
                _executionTask = ExecutePresetAsync(_executionCancellation.Token);
                await _executionTask;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Failed to start AVS execution engine"));
                _isRunning = false;
                throw;
            }
        }
        
        /// <summary>
        /// Stops the AVS execution engine
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning) return;
            
            try
            {
                _isRunning = false;
                _executionCancellation.Cancel();
                
                if (_executionTask != null && !_executionTask.IsCompleted)
                {
                    await _executionTask;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Failed to stop AVS execution engine"));
            }
        }
        
        /// <summary>
        /// Updates the current preset and restarts execution if needed
        /// </summary>
        public async Task UpdatePresetAsync(AvsPreset newPreset)
        {
            if (newPreset == null) return;
            
            var wasRunning = _isRunning;
            
            if (wasRunning)
            {
                await StopAsync();
            }
            
            // Update preset and restart if it was running
            // Note: In a real implementation, you'd want to update the preset reference
            // For now, we'll just restart with the new preset
            
            if (wasRunning)
            {
                await StartAsync();
            }
        }
        
        /// <summary>
        /// Main execution loop for the AVS preset
        /// </summary>
        private async Task ExecutePresetAsync(CancellationToken cancellationToken)
        {
            const int targetFPS = 60;
            const double targetFrameTime = 1000.0 / targetFPS;
            
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                var frameStartTime = DateTime.Now;
                
                try
                {
                    // Execute the current frame
                    await ExecuteFrameAsync();
                    
                    // Calculate frame timing
                    var frameTime = (DateTime.Now - frameStartTime).TotalMilliseconds;
                    UpdateFrameTiming(frameTime);
                    
                    // Maintain target FPS
                    if (frameTime < targetFrameTime)
                    {
                        var sleepTime = (int)(targetFrameTime - frameTime);
                        await Task.Delay(sleepTime, cancellationToken);
                    }
                    
                    _frameCount++;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(new AvsErrorEventArgs(ex, $"Error executing frame {_frameCount}"));
                    
                    // Continue execution unless it's a critical error
                    if (ex is OutOfMemoryException)
                    {
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Executes a single frame of the AVS preset
        /// </summary>
        private async Task ExecuteFrameAsync()
        {
            // Get current audio data
            await UpdateAudioDataAsync();
            
            // Execute Init section (only once)
            if (_frameCount == 0)
            {
                await ExecuteSectionAsync(_currentPreset.InitEffects, AvsSection.Init);
            }
            
            // Execute Beat section (if beat detected)
            if (_beatDetected)
            {
                await ExecuteSectionAsync(_currentPreset.BeatEffects, AvsSection.Beat);
                _beatDetected = false; // Reset beat flag
            }
            
            // Execute Frame section (every frame)
            await ExecuteSectionAsync(_currentPreset.FrameEffects, AvsSection.Frame);
            
            // Execute Point section (for superscopes)
            await ExecuteSectionAsync(_currentPreset.PointEffects, AvsSection.Point);
            
            // Render the frame
            var renderResult = await _renderer.RenderFrameAsync(_variables, _audioData);
            
            // Notify frame rendered
            OnFrameRendered(new AvsRenderEventArgs(renderResult, _frameCount, _variables));
        }
        
        /// <summary>
        /// Executes all effects in a specific section
        /// </summary>
        private async Task ExecuteSectionAsync(List<AvsEffect> effects, AvsSection section)
        {
            foreach (var effect in effects)
            {
                if (!effect.IsEnabled) continue;
                
                try
                {
                    await ExecuteEffectAsync(effect, section);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(new AvsErrorEventArgs(ex, $"Error executing effect {effect.DisplayName} in {section} section"));
                }
            }
        }
        
        /// <summary>
        /// Executes a single AVS effect
        /// </summary>
        private async Task ExecuteEffectAsync(AvsEffect effect, AvsSection section)
        {
            // Clear frame if requested
            if (effect.ClearEveryFrame)
            {
                await _renderer.ClearFrameAsync();
            }
            
            // Execute effect based on type
            switch (effect.Type)
            {
                case AvsEffectType.Clear:
                    await ExecuteClearEffectAsync(effect);
                    break;
                    
                case AvsEffectType.Blend:
                    await ExecuteBlendEffectAsync(effect);
                    break;
                    
                case AvsEffectType.Superscope:
                    await ExecuteSuperscopeEffectAsync(effect);
                    break;
                    
                case AvsEffectType.Spectrum:
                    await ExecuteSpectrumEffectAsync(effect);
                    break;
                    
                case AvsEffectType.Movement:
                    await ExecuteMovementEffectAsync(effect);
                    break;
                    
                case AvsEffectType.Color:
                    await ExecuteColorEffectAsync(effect);
                    break;
                    
                case AvsEffectType.Particle:
                    await ExecuteParticleEffectAsync(effect);
                    break;
                    
                case AvsEffectType.Custom:
                    await ExecuteCustomEffectAsync(effect);
                    break;
                    
                default:
                    // Handle other effect types
                    await ExecuteGenericEffectAsync(effect);
                    break;
            }
        }
        
        // Effect execution methods
        private async Task ExecuteClearEffectAsync(AvsEffect effect)
        {
            var color = GetParameterValue(effect, "color", "#000000");
            await _renderer.ClearFrameAsync(color);
        }
        
        private async Task ExecuteBlendEffectAsync(AvsEffect effect)
        {
            var mode = GetParameterValue(effect, "mode", "Normal");
            var opacity = GetParameterValue(effect, "opacity", 0.5f);
            await _renderer.SetBlendModeAsync(mode, opacity);
        }
        
        /// <summary>
        /// Executes a superscope effect
        /// </summary>
        private async Task ExecuteSuperscopeEffectAsync(AvsEffect effect)
        {
            var code = effect.Code;
            if (!string.IsNullOrEmpty(code))
            {
                var result = await ExecuteSuperscopeCodeAsync(code, effect.Parameters);
                if (result != null)
                {
                    _variables["superscope_result"] = result;
                }
            }
        }
        
        private async Task ExecuteSpectrumEffectAsync(AvsEffect effect)
        {
            var channels = GetParameterValue(effect, "channels", 2);
            
            // Get spectrum data from audio provider
            var spectrumData = await _audioProvider.GetSpectrumDataAsync(channels);
            _variables["spectrum_data"] = spectrumData;
        }
        
        private async Task ExecuteMovementEffectAsync(AvsEffect effect)
        {
            var x = GetParameterValue(effect, "x", 0.0f);
            var y = GetParameterValue(effect, "y", 0.0f);
            var rotation = GetParameterValue(effect, "rotation", 0.0f);
            var scale = GetParameterValue(effect, "scale", 1.0f);
            
            await _renderer.SetTransformAsync(x, y, rotation, scale);
        }
        
        private async Task ExecuteColorEffectAsync(AvsEffect effect)
        {
            var red = GetParameterValue(effect, "red", 1.0f);
            var green = GetParameterValue(effect, "green", 1.0f);
            var blue = GetParameterValue(effect, "blue", 1.0f);
            var alpha = GetParameterValue(effect, "alpha", 1.0f);
            
            await _renderer.SetColorAsync(red, green, blue, alpha);
        }
        
        private Task ExecuteParticleEffectAsync(AvsEffect effect)
        {
            var count = GetParameterValue(effect, "count", 100);
            var size = GetParameterValue(effect, "size", 2.0f);
            var speed = GetParameterValue(effect, "speed", 1.0f);
            
            // Generate particle positions
            var particles = GenerateParticles(count, size, speed);
            _variables["particles"] = particles;
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Executes a custom effect
        /// </summary>
        private async Task ExecuteCustomEffectAsync(AvsEffect effect)
        {
            // Execute custom effect code
            var code = effect.Code;
            if (!string.IsNullOrEmpty(code))
            {
                var result = await ExecuteCustomCodeAsync(code, effect.Parameters);
                if (result != null)
                {
                    _variables["custom_result"] = result;
                }
            }
        }
        
        private Task ExecuteGenericEffectAsync(AvsEffect effect)
        {
            // Generic effect execution - could be extended for other effect types
            _variables[$"effect_{effect.Name}"] = true;
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Executes superscope code (simplified implementation)
        /// </summary>
        private Task<object?> ExecuteSuperscopeCodeAsync(string code, Dictionary<string, object> parameters)
        {
            // This is a simplified implementation
            // In a real system, you'd want a proper scripting engine or compiler
            
            try
            {
                // Parse basic mathematical expressions
                var result = ParseAndExecuteCode(code, parameters);
                return Task.FromResult<object?>(result);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Error executing superscope code"));
                return Task.FromResult<object?>(null);
            }
        }
        
        /// <summary>
        /// Executes custom effect code
        /// </summary>
        private Task<object?> ExecuteCustomCodeAsync(string code, Dictionary<string, object> parameters)
        {
            // Similar to superscope execution but for custom effects
            try
            {
                var result = ParseAndExecuteCode(code, parameters);
                return Task.FromResult<object?>(result);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Error executing custom effect code"));
                return Task.FromResult<object?>(null);
            }
        }
        
        /// <summary>
        /// Parses and executes simple mathematical expressions
        /// </summary>
        private static object ParseAndExecuteCode(string code, Dictionary<string, object> parameters)
        {
            // This is a very basic implementation
            // In production, you'd want a proper expression parser
            
            // For now, just return a placeholder result
            // This would need to be implemented with a proper math expression evaluator
            return new { success = true, message = "Code execution placeholder" };
        }
        
        /// <summary>
        /// Generates particle positions for particle effects
        /// </summary>
        private static List<object> GenerateParticles(int count, float size, float speed)
        {
            var particles = new List<object>();
            var random = new Random();
            
            for (int i = 0; i < count; i++)
            {
                var x = (float)(random.NextDouble() * 2 - 1); // -1 to 1
                var y = (float)(random.NextDouble() * 2 - 1); // -1 to 1
                var vx = (float)(random.NextDouble() * 2 - 1) * speed;
                var vy = (float)(random.NextDouble() * 2 - 1) * speed;
                
                particles.Add(new { x, y, vx, vy, size });
            }
            
            return particles;
        }
        
        /// <summary>
        /// Gets a parameter value with fallback
        /// </summary>
        private static T GetParameterValue<T>(AvsEffect effect, string paramName, T defaultValue)
        {
            if (effect.Parameters.TryGetValue(paramName, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Updates frame timing statistics
        /// </summary>
        private void UpdateFrameTiming(double frameTime)
        {
            _frameTimes.Enqueue(frameTime);
            
            if (_frameTimes.Count > 60)
            {
                _frameTimes.Dequeue();
            }
            
            _averageFrameTime = 0;
            foreach (var time in _frameTimes)
            {
                _averageFrameTime += time;
            }
            _averageFrameTime /= _frameTimes.Count;
        }
        
        /// <summary>
        /// Updates audio data from the audio provider
        /// </summary>
        private async Task UpdateAudioDataAsync()
        {
            try
            {
                var audioData = await _audioProvider.GetAudioDataAsync();
                _audioData = audioData;
                
                // Check for beat detection
                var newBeatDetected = await _audioProvider.IsBeatDetectedAsync();
                if (newBeatDetected && !_beatDetected)
                {
                    _beatDetected = true;
                    OnBeatDetected(new AvsBeatEventArgs(_bpm, _frameCount));
                }
                
                // Update BPM
                var newBpm = await _audioProvider.GetBPMAsync();
                if (newBpm > 0)
                {
                    _bpm = newBpm;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Error updating audio data"));
            }
        }
        
        /// <summary>
        /// Initializes the preset and sets up initial variables
        /// </summary>
        private async Task InitializePresetAsync()
        {
            _variables.Clear();
            
            // Set default variables
            _variables["time"] = 0.0f;
            _variables["frame"] = 0;
            _variables["bpm"] = _bpm;
            _variables["beat"] = false;
            
            // Set preset-specific variables
            _variables["preset_name"] = _currentPreset.Name;
            _variables["preset_author"] = _currentPreset.Author;
            _variables["clear_every_frame"] = _currentPreset.ClearEveryFrame;
            _variables["frame_rate"] = _currentPreset.FrameRate;
            _variables["beat_detection"] = _currentPreset.BeatDetection;
            
            // Initialize renderer
            await _renderer.InitializeAsync(_variables);
        }
        
        // Event raising methods
        protected virtual void OnFrameRendered(AvsRenderEventArgs e)
        {
            FrameRendered?.Invoke(this, e);
        }
        
        protected virtual void OnBeatDetected(AvsBeatEventArgs e)
        {
            BeatDetected?.Invoke(this, e);
        }
        
        protected virtual void OnErrorOccurred(AvsErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            try
            {
                StopAsync().Wait();
                _executionCancellation?.Dispose();
                _audioProvider?.Dispose();
                _renderer?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
    
    // Event argument classes
    public class AvsRenderEventArgs : EventArgs
    {
        public object RenderResult { get; }
        public int FrameNumber { get; }
        public Dictionary<string, object> Variables { get; }
        
        public AvsRenderEventArgs(object renderResult, int frameNumber, Dictionary<string, object> variables)
        {
            RenderResult = renderResult;
            FrameNumber = frameNumber;
            Variables = variables;
        }
    }
    
    public class AvsBeatEventArgs : EventArgs
    {
        public float BPM { get; }
        public int FrameNumber { get; }
        
        public AvsBeatEventArgs(float bpm, int frameNumber)
        {
            BPM = bpm;
            FrameNumber = frameNumber;
        }
    }
    
    public class AvsErrorEventArgs : EventArgs
    {
        public Exception Error { get; }
        public string Context { get; }
        
        public AvsErrorEventArgs(Exception error, string context)
        {
            Error = error;
            Context = context;
        }
    }
}
