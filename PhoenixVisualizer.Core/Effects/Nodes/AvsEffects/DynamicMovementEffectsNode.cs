using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Dynamic Movement Effects Node - Per-pixel displacement engine for complex motion effects
    /// Applies mathematical transformations to the entire framebuffer with audio reactivity
    /// </summary>
    public class DynamicMovementEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Enable/disable the effect
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Enable multi-threading for performance
        /// </summary>
        public bool MultiThreaded { get; set; } = true;

        /// <summary>
        /// Maximum number of threads for processing
        /// </summary>
        public int MaxThreads { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Enable beat-reactive transformations
        /// </summary>
        public bool BeatReactive { get; set; } = true;

        /// <summary>
        /// Audio data source (0=spectrum, 1=waveform)
        /// </summary>
        public int AudioSource { get; set; } = 0;

        /// <summary>
        /// Blending mode for transformed pixels
        /// </summary>
        public int BlendMode { get; set; } = 0; // 0=replace, 1=add, 2=multiply

        /// <summary>
        /// Wrapping mode for out-of-bounds coordinates
        /// </summary>
        public int WrapMode { get; set; } = 0; // 0=clamp, 1=wrap, 2=mirror

        #endregion

        #region Script Sections

        /// <summary>
        /// Initialization script (runs once at startup)
        /// </summary>
        public string InitScript { get; set; } = "";

        /// <summary>
        /// Frame script (runs every frame)
        /// </summary>
        public string FrameScript { get; set; } = "";

        /// <summary>
        /// Beat script (runs on beat detection)
        /// </summary>
        public string BeatScript { get; set; } = "";

        /// <summary>
        /// Point script (runs for each pixel)
        /// </summary>
        public string PointScript { get; set; } = "x=x; y=y";

        #endregion

        #region Internal State

        private double _time = 0.0;
        private readonly Dictionary<string, double> _variables;

        #endregion

        #region Constants

        private const double PI = Math.PI;
        private const double TWO_PI = 2.0 * Math.PI;
        private const int MAX_THREADS = 16;
        private const int MIN_THREADS = 1;

        #endregion

        #region Constructor

        public DynamicMovementEffectsNode()
        {
            Name = "Dynamic Movement";
            Description = "Per-pixel displacement engine for complex motion effects and transformations";
            Category = "AVS Effects";
            
            _variables = new Dictionary<string, double>();
            InitializeVariables();
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for transformation"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Transformed output image"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled || !inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
            {
                return GetDefaultOutput();
            }

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            
            // Execute dynamic movement transformation
            if (MultiThreaded && MaxThreads > 1)
            {
                RenderMultiThreaded(imageBuffer, output, audioFeatures);
            }
            else
            {
                RenderSingleThreaded(imageBuffer, output, audioFeatures);
            }

            return output;
        }

        #endregion

        #region Rendering Methods

        private void RenderSingleThreaded(ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            lock (_processingLock)
            {
                ExecuteScripts(audioFeatures);
                TransformPixels(input, output, 0, input.Height, audioFeatures);
            }
        }

        private void RenderMultiThreaded(ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            lock (_processingLock)
            {
                ExecuteScripts(audioFeatures);
                
                int threadCount = Math.Clamp(MaxThreads, MIN_THREADS, MAX_THREADS);
                int rowsPerThread = input.Height / threadCount;
                
                var tasks = new List<Task>();
                
                for (int i = 0; i < threadCount; i++)
                {
                    int startRow = i * rowsPerThread;
                    int endRow = (i == threadCount - 1) ? input.Height : (i + 1) * rowsPerThread;
                    
                    var task = Task.Run(() => TransformPixels(input, output, startRow, endRow, audioFeatures));
                    tasks.Add(task);
                }
                
                Task.WaitAll(tasks.ToArray());
            }
        }

        #endregion

        #region Script Execution

        private void ExecuteScripts(AudioFeatures audioFeatures)
        {
            try
            {
                // Execute initialization script (runs once)
                ExecuteScript(InitScript);

                // Execute frame script (runs every frame)
                ExecuteScript(FrameScript);

                // Execute beat script if beat detected
                if (BeatReactive && audioFeatures?.IsBeat == true)
                {
                    ExecuteScript(BeatScript);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dynamic Movement script error: {ex.Message}");
            }
        }

        private void ExecuteScript(string script)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            // Simple script execution - in production, this would use a proper expression parser
            ProcessScriptLine(script);
        }

        private void ProcessScriptLine(string script)
        {
            // Handle basic assignments like "x=x*2", "y=y+0.1"
            if (script.Contains("="))
            {
                var parts = script.Split('=', 2);
                if (parts.Length == 2)
                {
                    string varName = parts[0].Trim();
                    string expression = parts[1].Trim();
                    
                    double value = EvaluateExpression(expression);
                    _variables[varName] = value;
                }
            }
        }

        private double EvaluateExpression(string expression)
        {
            // Simple expression evaluator - in production, use a proper math parser
            // This handles basic operations like "x*2", "y+0.1", "sin(t)"
            
            // Replace constants
            expression = expression.Replace("$PI", PI.ToString());
            expression = expression.Replace("PI", PI.ToString());
            
            // Handle basic arithmetic
            if (expression.Contains("+"))
            {
                var parts = expression.Split('+');
                return parts.Select(p => EvaluateExpression(p)).Sum();
            }
            else if (expression.Contains("-"))
            {
                var parts = expression.Split('-');
                if (parts.Length == 2)
                {
                    return EvaluateExpression(parts[0]) - EvaluateExpression(parts[1]);
                }
            }
            else if (expression.Contains("*"))
            {
                var parts = expression.Split('*');
                return parts.Select(p => EvaluateExpression(p)).Aggregate(1.0, (a, b) => a * b);
            }
            else if (expression.Contains("/"))
            {
                var parts = expression.Split('/');
                if (parts.Length == 2)
                {
                    return EvaluateExpression(parts[0]) / EvaluateExpression(parts[1]);
                }
            }
            
            // Handle mathematical functions
            if (expression.StartsWith("sin(") && expression.EndsWith(")"))
            {
                string arg = expression.Substring(4, expression.Length - 5);
                return Math.Sin(EvaluateExpression(arg));
            }
            else if (expression.StartsWith("cos(") && expression.EndsWith(")"))
            {
                string arg = expression.Substring(4, expression.Length - 5);
                return Math.Cos(EvaluateExpression(arg));
            }
            else if (expression.StartsWith("tan(") && expression.EndsWith(")"))
            {
                string arg = expression.Substring(4, expression.Length - 5);
                return Math.Tan(EvaluateExpression(arg));
            }
            
            // Try to parse as number
            if (double.TryParse(expression, out double number))
            {
                return number;
            }
            
            // Try to get variable value
            if (_variables.TryGetValue(expression, out double variable))
            {
                return variable;
            }
            
            return 0.0;
        }

        #endregion

        #region Pixel Transformation

        private void TransformPixels(ImageBuffer input, ImageBuffer output, int startRow, int endRow, AudioFeatures audioFeatures)
        {
            int width = input.Width;
            int height = input.Height;
            int centerX = width / 2;
            int centerY = height / 2;

            // Get audio data for reactivity
            float[] audioData = GetAudioData(audioFeatures);

            for (int y = startRow; y < endRow; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Set coordinate variables
                    _variables["x"] = (double)(x - centerX) / centerX;
                    _variables["y"] = (double)(y - centerY) / centerY;
                    _variables["d"] = Math.Sqrt(_variables["x"] * _variables["x"] + _variables["y"] * _variables["y"]);
                    _variables["r"] = Math.Atan2(_variables["y"], _variables["x"]);
                    _variables["w"] = width;
                    _variables["h"] = height;
                    _variables["b"] = audioFeatures?.IsBeat == true ? 1.0 : 0.0;

                    // Execute point script for transformation
                    ExecuteScript(PointScript);

                    // Get transformed coordinates
                    double newX = GetVariable("x", _variables["x"]);
                    double newY = GetVariable("y", _variables["y"]);

                    // Convert back to pixel coordinates
                    int sourceX = centerX + (int)(newX * centerX);
                    int sourceY = centerY + (int)(newY * centerY);

                    // Apply wrapping mode
                    ApplyWrapping(ref sourceX, ref sourceY, width, height);

                    // Get source pixel color
                    int sourceColor = GetSourcePixel(input, sourceX, sourceY);

                    // Apply blending mode
                    int finalColor = ApplyBlending(output.GetPixel(x, y), sourceColor);

                    // Set output pixel
                    output.SetPixel(x, y, finalColor);
                }
            }
        }

        private int GetSourcePixel(ImageBuffer input, int x, int y)
        {
            // Clamp coordinates to image bounds
            x = Math.Clamp(x, 0, input.Width - 1);
            y = Math.Clamp(y, 0, input.Height - 1);
            
            return input.GetPixel(x, y);
        }

        private void ApplyWrapping(ref int x, ref int y, int width, int height)
        {
            switch (WrapMode)
            {
                case 0: // Clamp
                    x = Math.Clamp(x, 0, width - 1);
                    y = Math.Clamp(y, 0, height - 1);
                    break;
                    
                case 1: // Wrap
                    x = ((x % width) + width) % width;
                    y = ((y % height) + height) % height;
                    break;
                    
                case 2: // Mirror
                    x = Math.Abs(x) % (width * 2);
                    y = Math.Abs(y) % (height * 2);
                    if (x >= width) x = width * 2 - x - 1;
                    if (y >= height) y = height * 2 - y - 1;
                    break;
            }
        }

        private int ApplyBlending(int destColor, int sourceColor)
        {
            switch (BlendMode)
            {
                case 0: // Replace
                    return sourceColor;
                    
                case 1: // Add
                    return AddColors(destColor, sourceColor);
                    
                case 2: // Multiply
                    return MultiplyColors(destColor, sourceColor);
                    
                default:
                    return sourceColor;
            }
        }

        private int AddColors(int color1, int color2)
        {
            int r1 = color1 & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = (color1 >> 16) & 0xFF;
            
            int r2 = color2 & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 16) & 0xFF;
            
            int r = Math.Clamp(r1 + r2, 0, 255);
            int g = Math.Clamp(g1 + g2, 0, 255);
            int b = Math.Clamp(b1 + b2, 0, 255);
            
            return r | (g << 8) | (b << 16);
        }

        private int MultiplyColors(int color1, int color2)
        {
            int r1 = color1 & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = (color1 >> 16) & 0xFF;
            
            int r2 = color2 & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 16) & 0xFF;
            
            int r = (r1 * r2) / 255;
            int g = (g1 * g2) / 255;
            int b = (b1 * b2) / 255;
            
            return r | (g << 8) | (b << 16);
        }

        #endregion

        #region Audio Data Processing

        private float[] GetAudioData(AudioFeatures audioFeatures)
        {
            if (audioFeatures == null) return new float[576];

            return AudioSource == 0 
                ? audioFeatures.SpectrumData 
                : audioFeatures.WaveformData;
        }

        #endregion

        #region Variable Management

        private void InitializeVariables()
        {
            _variables.Clear();
            _variables["t"] = _time;
            _variables["b"] = 0.0;
            _variables["w"] = 800.0; // Default width
            _variables["h"] = 600.0; // Default height
            _variables["alpha"] = 1.0;
        }

        private double GetVariable(string name, double defaultValue)
        {
            return _variables.TryGetValue(name, out double value) ? value : defaultValue;
        }

        private void SetVariable(string name, double value)
        {
            _variables[name] = value;
        }

        #endregion

        #region Default Output

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
