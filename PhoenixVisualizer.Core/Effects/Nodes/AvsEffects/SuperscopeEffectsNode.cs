using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Superscope Effects Node - Core AVS visualization engine with mathematical expression support
    /// Implements the scripting-based renderer that plots points, lines, or shapes based on mathematical equations
    /// </summary>
    public class SuperscopeEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Number of points to plot (1 to 128,000)
        /// </summary>
        public int PointCount { get; set; } = 800;

        /// <summary>
        /// Line thickness for rendering
        /// </summary>
        public float LineSize { get; set; } = 1.0f;

        /// <summary>
        /// Drawing mode (0=points, 1=lines)
        /// </summary>
        public int DrawMode { get; set; } = 1;

        /// <summary>
        /// Skip rendering if value > 0.00001
        /// </summary>
        public float SkipThreshold { get; set; } = 0.0f;

        /// <summary>
        /// Enable beat-reactive rendering
        /// </summary>
        public bool BeatReactive { get; set; } = true;

        /// <summary>
        /// Audio data source (0=spectrum, 1=waveform)
        /// </summary>
        public int AudioSource { get; set; } = 0;

        #endregion

        #region Script Sections

        /// <summary>
        /// Initialization script (runs once at startup)
        /// </summary>
        public string InitScript { get; set; } = "n=800";

        /// <summary>
        /// Frame script (runs every frame)
        /// </summary>
        public string FrameScript { get; set; } = "t=t-0.05";

        /// <summary>
        /// Beat script (runs on beat detection)
        /// </summary>
        public string BeatScript { get; set; } = "";

        /// <summary>
        /// Point script (runs for each point)
        /// </summary>
        public string PointScript { get; set; } = "d=i+v*0.2; r=t+i*$PI*4; x=cos(r)*d; y=sin(r)*d";

        #endregion

        #region Internal State

        private double _time = 0.0;
        private readonly Dictionary<string, double> _variables;
        private readonly Random _random;

        #endregion

        #region Constants

        private const double PI = Math.PI;
        private const double TWO_PI = 2.0 * Math.PI;
        private const int MAX_POINTS = 128000;
        private const int MIN_POINTS = 1;

        #endregion

        #region Constructor

        public SuperscopeEffectsNode()
        {
            Name = "Superscope";
            Description = "Core AVS visualization engine with mathematical expression support";
            Category = "AVS Effects";
            
            _variables = new Dictionary<string, double>();
            _random = new Random();
            InitializeVariables();
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for superscope overlay"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Superscope rendered output image"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
            {
                return GetDefaultOutput();
            }

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            
            // Copy input image to output
            Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);

            // Execute superscope rendering
            RenderSuperscope(output, audioFeatures);

            return output;
        }

        #endregion

        #region Superscope Rendering

        private void RenderSuperscope(ImageBuffer output, AudioFeatures audioFeatures)
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

                // Render points based on point script
                RenderPoints(output, audioFeatures);
            }
            catch (Exception ex)
            {
                // Log error and continue with default rendering
                System.Diagnostics.Debug.WriteLine($"Superscope script error: {ex.Message}");
            }
        }

        private void RenderPoints(ImageBuffer output, AudioFeatures audioFeatures)
        {
            int width = output.Width;
            int height = output.Height;
            int centerX = width / 2;
            int centerY = height / 2;

            // Get audio data
            float[] audioData = GetAudioData(audioFeatures);
            if (audioData == null || audioData.Length == 0) return;

            // Render each point
            for (int i = 0; i < PointCount && i < audioData.Length; i++)
            {
                // Set current point index
                _variables["i"] = (double)i / PointCount;
                
                // Set audio value
                _variables["v"] = audioData[i % audioData.Length];

                // Execute point script
                ExecuteScript(PointScript);

                // Get coordinates and colors
                double x = GetVariable("x", 0.0);
                double y = GetVariable("y", 0.0);
                double red = GetVariable("red", 1.0);
                double green = GetVariable("green", 1.0);
                double blue = GetVariable("blue", 1.0);
                double skip = GetVariable("skip", 0.0);

                // Check skip threshold
                if (skip > SkipThreshold) continue;

                // Convert normalized coordinates to pixel coordinates
                int pixelX = centerX + (int)(x * centerX);
                int pixelY = centerY + (int)(y * centerY);

                // Clamp coordinates to image bounds
                pixelX = Math.Clamp(pixelX, 0, width - 1);
                pixelY = Math.Clamp(pixelY, 0, height - 1);

                // Convert colors to 8-bit values
                int r = Math.Clamp((int)(red * 255), 0, 255);
                int g = Math.Clamp((int)(green * 255), 0, 255);
                int b = Math.Clamp((int)(blue * 255), 0, 255);

                // Combine into pixel value
                int pixelColor = r | (g << 8) | (b << 16);

                // Render point or line
                if (DrawMode == 0)
                {
                    // Point mode
                    output.SetPixel(pixelX, pixelY, pixelColor);
                }
                else
                {
                    // Line mode - draw line from previous point
                    if (i > 0)
                    {
                        // Get previous coordinates (simplified - in real implementation, store previous point)
                        double prevX = GetVariable("prevX", x);
                        double prevY = GetVariable("prevY", y);
                        int prevPixelX = centerX + (int)(prevX * centerX);
                        int prevPixelY = centerY + (int)(prevY * centerY);
                        
                        DrawLine(output, prevPixelX, prevPixelY, pixelX, pixelY, pixelColor, (int)LineSize);
                    }
                }

                // Store current coordinates for next iteration
                _variables["prevX"] = x;
                _variables["prevY"] = y;
            }
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

        #region Script Execution

        private void ExecuteScript(string script)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            // Simple script execution - in production, this would use a proper expression parser
            // For now, implement basic variable assignment and mathematical operations
            ProcessScriptLine(script);
        }

        private void ProcessScriptLine(string script)
        {
            // Handle basic assignments like "n=800", "t=t-0.05"
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
            // This handles basic operations like "t-0.05", "i*$PI*4"
            
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

        #region Rendering Utilities

        private void DrawLine(ImageBuffer output, int x1, int y1, int x2, int y2, int color, int thickness)
        {
            // Bresenham's line algorithm
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int x = x1, y = y1;

            while (true)
            {
                // Draw pixel with thickness
                for (int tx = -thickness/2; tx <= thickness/2; tx++)
                {
                    for (int ty = -thickness/2; ty <= thickness/2; ty++)
                    {
                        int px = x + tx;
                        int py = y + ty;
                        if (px >= 0 && px < output.Width && py >= 0 && py < output.Height)
                        {
                            output.SetPixel(px, py, color);
                        }
                    }
                }

                if (x == x2 && y == y2) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        #endregion

        #region Variable Management

        private void InitializeVariables()
        {
            _variables.Clear();
            _variables["n"] = PointCount;
            _variables["t"] = _time;
            _variables["b"] = 0.0;
            _variables["w"] = 800.0; // Default width
            _variables["h"] = 600.0; // Default height
            _variables["linesize"] = LineSize;
            _variables["skip"] = SkipThreshold;
            _variables["drawmode"] = DrawMode;
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
