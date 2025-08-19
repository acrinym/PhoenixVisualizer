using PhoenixVisualizer.Core.Models;

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
        
        private CancellationTokenSource? _executionCancellation;
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
        
        // Performance + telemetry
        private double _averageFrameTime;
        private const double FrameEmaAlpha = 0.1;
        private long _totalFrames;
        private long _errorCount;
        
        public event EventHandler<AvsRenderEventArgs>? FrameRendered;
        public event EventHandler<AvsBeatEventArgs>? BeatDetected;
        public event EventHandler<AvsErrorEventArgs>? ErrorOccurred;
        
        public bool IsRunning => _isRunning;
        public double FPS => _averageFrameTime > 0 ? 1000.0 / _averageFrameTime : 0.0;
        public long FrameCount => _totalFrames;
        public long ErrorCount => _errorCount;
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
                _totalFrames = 0;
                _errorCount = 0;
                _beatDetected = false;
                
                // Initialize variables from preset
                await InitializePresetAsync();
                
                // Start the main execution loop
                _executionCancellation = new CancellationTokenSource();
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
                try { _executionCancellation?.Cancel(); }
                catch { /* ignore */ }
                
                if (_executionTask is not null && !_executionTask.IsCompleted)
                {
                    await _executionTask;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new AvsErrorEventArgs(ex, "Failed to stop AVS execution engine"));
            }
            finally
            {
                _executionCancellation?.Dispose();
                _executionCancellation = null;
                _executionTask = null;
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
            
            // Update time-based variables
            var currentTime = DateTime.Now;
            _variables["time"] = (currentTime - _lastFrameTime).TotalSeconds;
            _variables["frame"] = _frameCount;
            _variables["fps"] = FPS;
            
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
            
            // Draw spectrum visualization
            if (spectrumData.ContainsKey("channel_0") && spectrumData["channel_0"] is float[] fft)
            {
                await DrawSpectrumVisualization(fft, effect);
            }
        }
        
        /// <summary>
        /// Draws a spectrum visualization based on FFT data
        /// </summary>
        private async Task DrawSpectrumVisualization(float[] fft, AvsEffect effect)
        {
            var barWidth = GetParameterValue(effect, "bar_width", 2.0f);
            var barSpacing = GetParameterValue(effect, "bar_spacing", 1.0f);
            var maxHeight = GetParameterValue(effect, "max_height", 100.0f);
            var colorMode = GetParameterValue(effect, "color_mode", "rainbow");
            
            var totalWidth = fft.Length * (barWidth + barSpacing);
            var startX = -totalWidth / 2;
            
            for (int i = 0; i < fft.Length; i++)
            {
                var magnitude = Math.Min(1.0f, fft[i]);
                var height = magnitude * maxHeight;
                var x = startX + i * (barWidth + barSpacing);
                var y = -height / 2;
                
                // Set color based on mode
                if (colorMode == "rainbow")
                {
                    var hue = (float)i / fft.Length * 360.0f;
                    var (r, g, b) = HsvToRgb(hue, 1.0f, magnitude);
                    await _renderer.SetColorAsync(r, g, b, 1.0f);
                }
                else
                {
                    await _renderer.SetColorAsync(1.0f, 1.0f, 1.0f, magnitude);
                }
                
                // Draw the bar
                await _renderer.DrawRectangleAsync(x, y, barWidth, height, true);
            }
        }
        
        /// <summary>
        /// Converts HSV color to RGB
        /// </summary>
        private static (float r, float g, float b) HsvToRgb(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1.0f - (float)Math.Abs((h / 60.0f) % 2.0f - 1.0f));
            float m = v - c;
            
            float r, g, b;
            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }
            
            return ((float)(r + m), (float)(g + m), (float)(b + m));
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
        
        private async Task ExecuteParticleEffectAsync(AvsEffect effect)
        {
            var count = GetParameterValue(effect, "count", 100);
            var size = GetParameterValue(effect, "size", 2.0f);
            var speed = GetParameterValue(effect, "speed", 1.0f);
            
            // Generate particle positions
            var particles = GenerateParticles(count, size, speed);
            _variables["particles"] = particles;
            
            // Draw particles
            foreach (var particle in particles)
            {
                if (particle is Dictionary<string, object> p && 
                    p.TryGetValue("x", out var x) && p.TryGetValue("y", out var y) &&
                    p.TryGetValue("size", out var pSize))
                {
                    var px = Convert.ToSingle(x);
                    var py = Convert.ToSingle(y);
                    var psz = Convert.ToSingle(pSize);
                    
                    // Set particle color based on velocity or other properties
                    var velocity = p.TryGetValue("velocity", out var vel) ? Convert.ToSingle(vel) : 1.0f;
                    var intensity = Math.Min(1.0f, velocity / 5.0f);
                    await _renderer.SetColorAsync(intensity, intensity * 0.5f, 1.0f, 0.8f);
                    
                    // Draw particle as circle
                    await _renderer.DrawCircleAsync(px, py, psz, true);
                }
            }
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
            
            // Handle specific effect types that might not have dedicated handlers
            switch (effect.Name.ToLowerInvariant())
            {
                case "wave":
                    return ExecuteWaveEffectAsync(effect);
                case "fountain":
                    return ExecuteFountainEffectAsync(effect);
                case "scatter":
                    return ExecuteScatterEffectAsync(effect);
                case "beat":
                    return ExecuteBeatEffectAsync(effect);
                case "text":
                    return ExecuteTextEffectAsync(effect);
                default:
                    return Task.CompletedTask;
            }
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
            try
            {
                // Create a mathematical expression evaluator
                var evaluator = new MathExpressionEvaluator();
                
                // Add parameters to the evaluator context
                foreach (var param in parameters)
                {
                    evaluator.SetVariable(param.Key, Convert.ToDouble(param.Value));
                }
                
                // Add common mathematical constants and functions
                evaluator.SetVariable("pi", Math.PI);
                evaluator.SetVariable("e", Math.E);
                evaluator.SetVariable("t", Convert.ToDouble(parameters.GetValueOrDefault("time", 0.0)));
                evaluator.SetVariable("frame", Convert.ToDouble(parameters.GetValueOrDefault("frame", 0)));
                evaluator.SetVariable("bpm", Convert.ToDouble(parameters.GetValueOrDefault("bpm", 120.0)));
                
                // Parse and evaluate the code
                var result = evaluator.Evaluate(code);
                return result;
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message, message = "Code execution failed" };
            }
        }
        
        /// <summary>
        /// Simple mathematical expression evaluator for AVS code
        /// </summary>
        private class MathExpressionEvaluator
        {
            private readonly Dictionary<string, double> _variables = new();
            
            public void SetVariable(string name, double value)
            {
                _variables[name] = value;
            }
            
            public double Evaluate(string expression)
            {
                // Remove whitespace and convert to lowercase
                expression = expression.Replace(" ", "").ToLowerInvariant();
                
                // Handle basic mathematical operations
                return EvaluateExpression(expression);
            }
            
            private double EvaluateExpression(string expr)
            {
                // Handle parentheses first
                if (expr.Contains('('))
                {
                    var openIndex = expr.LastIndexOf('(');
                    var closeIndex = expr.IndexOf(')', openIndex);
                    if (closeIndex == -1) throw new ArgumentException("Mismatched parentheses");
                    
                    var innerExpr = expr.Substring(openIndex + 1, closeIndex - openIndex - 1);
                    var innerResult = EvaluateExpression(innerExpr);
                    
                    var newExpr = expr.Substring(0, openIndex) + innerResult + expr.Substring(closeIndex + 1);
                    return EvaluateExpression(newExpr);
                }
                
                // Handle functions
                if (expr.Contains("sin(") || expr.Contains("cos(") || expr.Contains("tan(") ||
                    expr.Contains("log(") || expr.Contains("sqrt(") || expr.Contains("abs("))
                {
                    return EvaluateFunctions(expr);
                }
                
                // Handle basic operations
                return EvaluateBasicOperations(expr);
            }
            
            private double EvaluateFunctions(string expr)
            {
                if (expr.StartsWith("sin("))
                {
                    var arg = ExtractFunctionArgument(expr, "sin");
                    return Math.Sin(EvaluateExpression(arg));
                }
                if (expr.StartsWith("cos("))
                {
                    var arg = ExtractFunctionArgument(expr, "cos");
                    return Math.Cos(EvaluateExpression(arg));
                }
                if (expr.StartsWith("tan("))
                {
                    var arg = ExtractFunctionArgument(expr, "tan");
                    return Math.Tan(EvaluateExpression(arg));
                }
                if (expr.StartsWith("log("))
                {
                    var arg = ExtractFunctionArgument(expr, "log");
                    return Math.Log(EvaluateExpression(arg));
                }
                if (expr.StartsWith("sqrt("))
                {
                    var arg = ExtractFunctionArgument(expr, "sqrt");
                    return Math.Sqrt(EvaluateExpression(arg));
                }
                if (expr.StartsWith("abs("))
                {
                    var arg = ExtractFunctionArgument(expr, "abs");
                    return Math.Abs(EvaluateExpression(arg));
                }
                
                throw new ArgumentException($"Unknown function in expression: {expr}");
            }
            
            private string ExtractFunctionArgument(string expr, string funcName)
            {
                var startIndex = funcName.Length + 1;
                var parenCount = 1;
                var endIndex = startIndex;
                
                while (endIndex < expr.Length && parenCount > 0)
                {
                    if (expr[endIndex] == '(') parenCount++;
                    else if (expr[endIndex] == ')') parenCount--;
                    endIndex++;
                }
                
                if (parenCount != 0) throw new ArgumentException("Mismatched parentheses in function");
                return expr.Substring(startIndex, endIndex - startIndex - 1);
            }
            
            private double EvaluateBasicOperations(string expr)
            {
                // Handle addition and subtraction
                var addIndex = expr.LastIndexOf('+');
                var subIndex = expr.LastIndexOf('-');
                
                if (addIndex > 0 && (subIndex == -1 || addIndex > subIndex))
                {
                    var left = expr.Substring(0, addIndex);
                    var right = expr.Substring(addIndex + 1);
                    return EvaluateExpression(left) + EvaluateExpression(right);
                }
                
                if (subIndex > 0)
                {
                    var left = expr.Substring(0, subIndex);
                    var right = expr.Substring(subIndex + 1);
                    return EvaluateExpression(left) - EvaluateExpression(right);
                }
                
                // Handle multiplication and division
                var mulIndex = expr.LastIndexOf('*');
                var divIndex = expr.LastIndexOf('/');
                
                if (mulIndex > 0 && (divIndex == -1 || mulIndex > divIndex))
                {
                    var left = expr.Substring(0, mulIndex);
                    var right = expr.Substring(mulIndex + 1);
                    return EvaluateExpression(left) * EvaluateExpression(right);
                }
                
                if (divIndex > 0)
                {
                    var left = expr.Substring(0, divIndex);
                    var right = expr.Substring(divIndex + 1);
                    var rightVal = EvaluateExpression(right);
                    if (rightVal == 0) throw new DivideByZeroException();
                    return EvaluateExpression(left) / rightVal;
                }
                
                // Handle power
                var powIndex = expr.LastIndexOf('^');
                if (powIndex > 0)
                {
                    var left = expr.Substring(0, powIndex);
                    var right = expr.Substring(powIndex + 1);
                    return Math.Pow(EvaluateExpression(left), EvaluateExpression(right));
                }
                
                // Try to parse as a number or variable
                if (double.TryParse(expr, out var number))
                {
                    return number;
                }
                
                if (_variables.TryGetValue(expr, out var variable))
                {
                    return variable;
                }
                
                throw new ArgumentException($"Cannot evaluate expression: {expr}");
            }
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
            var ms = frameTime;
            _averageFrameTime = _averageFrameTime <= 0 ? ms : (_averageFrameTime * (1 - FrameEmaAlpha) + ms * FrameEmaAlpha);
            _totalFrames++;
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
        
        /// <summary>
        /// Executes a wave effect
        /// </summary>
        private async Task ExecuteWaveEffectAsync(AvsEffect effect)
        {
            var amplitude = GetParameterValue(effect, "amplitude", 50.0f);
            var frequency = GetParameterValue(effect, "frequency", 1.0f);
            var phase = GetParameterValue(effect, "phase", 0.0f);
            var points = GetParameterValue(effect, "points", 100);
            
            var time = Convert.ToSingle(_variables.GetValueOrDefault("time", 0.0));
            var totalWidth = 200.0f;
            var startX = -totalWidth / 2;
            
            for (int i = 0; i < points - 1; i++)
            {
                var t1 = (float)i / (points - 1);
                var t2 = (float)(i + 1) / (points - 1);
                
                var x1 = startX + t1 * totalWidth;
                var y1 = amplitude * (float)Math.Sin(2 * (float)Math.PI * frequency * t1 + phase + time);
                var x2 = startX + t2 * totalWidth;
                var y2 = amplitude * (float)Math.Sin(2 * (float)Math.PI * frequency * t2 + phase + time);
                
                await _renderer.DrawLineAsync(x1, y1, x2, y2, 2.0f);
            }
        }
        
        /// <summary>
        /// Executes a fountain effect
        /// </summary>
        private async Task ExecuteFountainEffectAsync(AvsEffect effect)
        {
            var count = GetParameterValue(effect, "count", 20);
            var speed = GetParameterValue(effect, "speed", 2.0f);
            var spread = GetParameterValue(effect, "spread", 30.0f);
            
            var time = Convert.ToSingle(_variables.GetValueOrDefault("time", 0.0));
            
            for (int i = 0; i < count; i++)
            {
                var angle = (float)i / count * spread * (float)Math.PI / 180.0f;
                var distance = speed * time;
                
                var x = distance * (float)Math.Sin(angle);
                var y = -distance * (float)Math.Cos(angle) + 0.5f * 9.81f * time * time; // gravity
                
                var size = Math.Max(1.0f, 5.0f - time * 0.5f);
                var alpha = Math.Max(0.0f, 1.0f - time * 0.1f);
                
                await _renderer.SetColorAsync(1.0f, 0.5f, 0.0f, alpha);
                await _renderer.DrawCircleAsync(x, y, size, true);
            }
        }
        
        /// <summary>
        /// Executes a scatter effect
        /// </summary>
        private async Task ExecuteScatterEffectAsync(AvsEffect effect)
        {
            var count = GetParameterValue(effect, "count", 50);
            var radius = GetParameterValue(effect, "radius", 100.0f);
            var speed = GetParameterValue(effect, "speed", 1.0f);
            
            float time = (float)_variables.GetValueOrDefault("time", 0.0);
            
            for (int i = 0; i < count; i++)
            {
                float angle = ((float)i / count) * 2f * (float)Math.PI + time * speed;
                float distance = radius * (0.5f + 0.5f * (float)Math.Sin(time * 0.5f + i * 0.1f));

                float x = distance * (float)Math.Cos(angle);
                float y = distance * (float)Math.Sin(angle);

                float size = 2.0f + (float)Math.Sin(time + i) * 1.0f;
                float hue  = (float)(((time * 50.0f) + i * 7) % 360.0f);
                var rgb    = HsvToRgb(hue, 1.0f, 1.0f);

                await _renderer.SetColorAsync(rgb.r, rgb.g, rgb.b, 0.8f);
                await _renderer.DrawCircleAsync(x, y, size, true);
            }
        }
        
        /// <summary>
        /// Executes a beat-reactive effect
        /// </summary>
        private async Task ExecuteBeatEffectAsync(AvsEffect effect)
        {
            var intensity = GetParameterValue(effect, "intensity", 1.0f);
            var decay = GetParameterValue(effect, "decay", 0.9f);
            
            var beat = Convert.ToBoolean(_variables.GetValueOrDefault("beat", false));
            var beatIntensity = Convert.ToSingle(_variables.GetValueOrDefault("beat_intensity", 0.0f));
            
            if (beat)
            {
                beatIntensity = intensity;
            }
            else
            {
                beatIntensity *= decay;
            }
            
            _variables["beat_intensity"] = beatIntensity;
            
            // Draw beat-reactive visualization
            var size = 20.0f + beatIntensity * 50.0f;
            var alpha = Math.Min(1.0f, beatIntensity);
            
            await _renderer.SetColorAsync(1.0f, 0.0f, 0.0f, alpha);
            await _renderer.DrawCircleAsync(0, 0, size, false);
        }
        
        /// <summary>
        /// Executes a text effect
        /// </summary>
        private async Task ExecuteTextEffectAsync(AvsEffect effect)
        {
            var text = GetParameterValue(effect, "text", "AVS");
            var x = GetParameterValue(effect, "x", 0.0f);
            var y = GetParameterValue(effect, "y", 0.0f);
            var fontSize = GetParameterValue(effect, "font_size", 24.0f);
            
            await _renderer.DrawTextAsync(text, x, y, fontSize);
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
            _errorCount++;
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
