using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class SuperScopeEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Channel selection (0=left, 1=right, 2=center, 4=subwoofer)
        /// </summary>
        public int WhichChannel { get; set; } = 2; // Default: center channel

        /// <summary>
        /// Number of colors for interpolation (1-16)
        /// </summary>
        public int NumColors { get; set; } = 1;

        /// <summary>
        /// Array of colors for interpolation
        /// </summary>
        public Color[] Colors { get; set; } = new Color[16];

        /// <summary>
        /// Drawing mode (0=points, 1=lines)
        /// </summary>
        public int Mode { get; set; } = 0;

        /// <summary>
        /// Point expression script
        /// </summary>
        public string PointExpression { get; set; } = "d=i+v*0.2; r=t+i*$PI*4; x=cos(r)*d; y=sin(r)*d";

        /// <summary>
        /// Frame expression script
        /// </summary>
        public string FrameExpression { get; set; } = "t=t-0.05";

        /// <summary>
        /// Beat expression script
        /// </summary>
        public string BeatExpression { get; set; } = "";

        /// <summary>
        /// Init expression script
        /// </summary>
        public string InitExpression { get; set; } = "n=800";

        /// <summary>
        /// Number of points to render
        /// </summary>
        public int NumPoints { get; set; } = 800;

        #endregion

        #region Constants

        private const int MaxColors = 16;
        private const int MinColors = 1;
        private const int MaxPoints = 128 * 1024; // Same as AVS limit
        private const int MinPoints = 1;
        private const int ColorInterpolationSteps = 64;

        #endregion

        #region Internal State

        private int colorPos;
        private double time;
        private int lastWidth, lastHeight;
        private readonly object renderLock = new object();

        // EEL-like variables (simplified implementation)
        private double var_b, var_x, var_y, var_i, var_n, var_v, var_w, var_h;
        private double var_red, var_green, var_blue, var_skip, var_linesize, var_drawmode;

        #endregion

        #region Constructor

        public SuperScopeEffectsNode()
        {
            Name = "SuperScope Effects";
            Description = "Programmable scope that uses expression scripts to draw custom visualizations";
            Category = "AVS Effects";

            // Initialize default colors
            Colors[0] = Color.White;
            for (int i = 1; i < MaxColors; i++)
            {
                Colors[i] = Color.Black;
            }
            ResetState();
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for SuperScope effects"));
            _inputPorts.Add(new EffectPort("Audio", typeof(AudioFeatures), false, null, "Audio features for reactive effects"));
            
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with SuperScope effects"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            if (NumColors == 0)
                return imageBuffer;

            lock (renderLock)
            {
                // Update dimensions if changed
                if (lastWidth != imageBuffer.Width || lastHeight != imageBuffer.Height)
                {
                    lastWidth = imageBuffer.Width;
                    lastHeight = imageBuffer.Height;
                    ResetState();
                }

                // Create output buffer
                var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
                Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);

                // Update color position
                colorPos++;
                if (colorPos >= NumColors * ColorInterpolationSteps)
                    colorPos = 0;

                // Get current interpolated color
                Color currentColor = GetInterpolatedColor();

                // Get audio data
                float[] audioData = GetAudioData(audioFeatures);

                // Set up EEL-like variables
                SetupVariables(audioFeatures, currentColor);

                // Execute init expression (only once)
                if (string.IsNullOrEmpty(InitExpression))
                {
                    var_n = NumPoints;
                }

                // Execute frame expression
                ExecuteFrameExpression();

                // Execute beat expression if beat detected
                if (audioFeatures?.IsBeat == true)
                {
                    ExecuteBeatExpression();
                }

                // Draw SuperScope visualization
                DrawSuperScope(output, audioData, currentColor);

                return output;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reset internal state variables
        /// </summary>
        private void ResetState()
        {
            colorPos = 0;
            time = 0.0;
            var_b = 0.0;
            var_x = 0.0;
            var_y = 0.0;
            var_i = 0.0;
            var_n = NumPoints;
            var_v = 0.0;
            var_w = lastWidth;
            var_h = lastHeight;
            var_red = 1.0;
            var_green = 1.0;
            var_blue = 1.0;
            var_skip = 0.0;
            var_linesize = 1.0;
            var_drawmode = Mode;
        }

        /// <summary>
        /// Get interpolated color based on current position
        /// </summary>
        private Color GetInterpolatedColor()
        {
            if (NumColors <= 1)
                return Colors[0];

            int primaryIndex = colorPos / ColorInterpolationSteps;
            int interpolationStep = colorPos & (ColorInterpolationSteps - 1);

            Color primaryColor = Colors[primaryIndex];
            Color secondaryColor = (primaryIndex + 1 < NumColors) ? Colors[primaryIndex + 1] : Colors[0];

            // Interpolate RGB components
            int r = InterpolateComponent(primaryColor.R, secondaryColor.R, interpolationStep);
            int g = InterpolateComponent(primaryColor.G, secondaryColor.G, interpolationStep);
            int b = InterpolateComponent(primaryColor.B, secondaryColor.B, interpolationStep);

            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Interpolate a single color component
        /// </summary>
        private int InterpolateComponent(int primary, int secondary, int step)
        {
            int maxStep = ColorInterpolationSteps - 1;
            int result = ((primary * (maxStep - step)) + (secondary * step)) / maxStep;
            return Math.Clamp(result, 0, 255);
        }

        /// <summary>
        /// Get audio data based on channel selection
        /// </summary>
        private float[] GetAudioData(AudioFeatures audioFeatures)
        {
            if (audioFeatures?.SpectrumData == null)
            {
                // Return default audio data if none provided
                var defaultData = new float[576];
                for (int i = 0; i < 576; i++)
                {
                    defaultData[i] = 0.5f;
                }
                return defaultData;
            }

            var audioData = new float[576];
            int ws = (WhichChannel & 4) != 0 ? 1 : 0;
            int xorv = (ws * 128) ^ 128;

            if ((WhichChannel & 3) >= 2)
            {
                // Center channel - average of left and right
                for (int x = 0; x < 576; x++)
                {
                    audioData[x] = (audioFeatures.SpectrumData[x] + audioFeatures.SpectrumData[Math.Min(x + 1, audioFeatures.SpectrumData.Length - 1)]) / 2.0f;
                }
            }
            else
            {
                // Left or right channel
                for (int x = 0; x < 576; x++)
                {
                    audioData[x] = audioFeatures.SpectrumData[x];
                }
            }

            return audioData;
        }

        /// <summary>
        /// Set up EEL-like variables
        /// </summary>
        private void SetupVariables(AudioFeatures audioFeatures, Color color)
        {
            var_h = lastHeight;
            var_w = lastWidth;
            var_b = audioFeatures?.IsBeat == true ? 1.0 : 0.0;
            var_blue = color.B / 255.0;
            var_green = color.G / 255.0;
            var_red = color.R / 255.0;
            var_skip = 0.0;
            var_linesize = 1.0;
            var_drawmode = Mode;
        }

        /// <summary>
        /// Execute frame expression (simplified EEL interpreter)
        /// </summary>
        private void ExecuteFrameExpression()
        {
            if (string.IsNullOrEmpty(FrameExpression))
                return;

            // Simple expression parsing for common operations
            var expr = FrameExpression.ToLower();
            
            if (expr.Contains("t=t+"))
            {
                var parts = expr.Split('+');
                if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out double increment))
                {
                    time += increment;
                }
            }
            else if (expr.Contains("t=t-"))
            {
                var parts = expr.Split('-');
                if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out double decrement))
                {
                    time -= decrement;
                }
            }
        }

        /// <summary>
        /// Execute beat expression (simplified EEL interpreter)
        /// </summary>
        private void ExecuteBeatExpression()
        {
            if (string.IsNullOrEmpty(BeatExpression))
                return;

            // Simple expression parsing for beat operations
            var expr = BeatExpression.ToLower();
            
            if (expr.Contains("n="))
            {
                var parts = expr.Split('=');
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int newN))
                {
                    var_n = Math.Clamp(newN, MinPoints, MaxPoints);
                }
            }
        }

        /// <summary>
        /// Draw SuperScope visualization
        /// </summary>
        private void DrawSuperScope(ImageBuffer image, float[] audioData, Color color)
        {
            int colorValue = ColorToInt(color);
            int numPoints = Math.Min((int)var_n, MaxPoints);
            
            if (numPoints <= 0)
                return;

            int lastX = 0, lastY = 0;
            bool canDraw = false;

            for (int a = 0; a < numPoints; a++)
            {
                // Calculate audio value for this point
                double r = (a * 576.0) / numPoints;
                double s1 = r - (int)r;
                double yr = (audioData[(int)r] * 255) * (1.0 - s1) + 
                           (audioData[Math.Min((int)r + 1, 575)] * 255) * s1;
                
                var_v = (yr / 128.0) - 1.0;
                var_i = (double)a / (double)(numPoints - 1);
                var_skip = 0.0;

                // Execute point expression (simplified)
                ExecutePointExpression();

                // Calculate screen coordinates
                int x = (int)((var_x + 1.0) * lastWidth * 0.5);
                int y = (int)((var_y + 1.0) * lastHeight * 0.5);

                if (var_skip < 0.00001)
                {
                    int thisColor = MakeInt(var_blue) | (MakeInt(var_green) << 8) | (MakeInt(var_red) << 16);

                    if (var_drawmode < 0.00001)
                    {
                        // Point mode
                        if (y >= 0 && y < lastHeight && x >= 0 && x < lastWidth)
                        {
                            image.SetPixel(x, y, thisColor);
                        }
                    }
                    else
                    {
                        // Line mode
                        if (canDraw)
                        {
                            DrawLine(image, lastX, lastY, x, y, thisColor);
                        }
                    }

                    canDraw = true;
                    lastX = x;
                    lastY = y;
                }
            }
        }

        /// <summary>
        /// Execute point expression (simplified EEL interpreter)
        /// </summary>
        private void ExecutePointExpression()
        {
            if (string.IsNullOrEmpty(PointExpression))
                return;

            // Simple expression parsing for common operations
            var expr = PointExpression.ToLower();
            
            // Handle common patterns
            if (expr.Contains("d=i+v*"))
            {
                var parts = expr.Split('*');
                if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out double factor))
                {
                    double d = var_i + var_v * factor;
                    
                    if (expr.Contains("r=t+i*$pi*"))
                    {
                        var piParts = expr.Split('$');
                        if (piParts.Length > 1 && double.TryParse(piParts[1].Split('*')[1], out double piMultiplier))
                        {
                            double r = time + var_i * Math.PI * piMultiplier;
                            
                            if (expr.Contains("x=cos(r)*d"))
                            {
                                var_x = Math.Cos(r) * d;
                            }
                            
                            if (expr.Contains("y=sin(r)*d"))
                            {
                                var_y = Math.Sin(r) * d;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert double to int (0-255 range)
        /// </summary>
        private int MakeInt(double value)
        {
            if (value <= 0.0) return 0;
            if (value >= 1.0) return 255;
            return (int)(value * 255.0);
        }

        /// <summary>
        /// Draw a line between two points using Bresenham's algorithm
        /// </summary>
        private void DrawLine(ImageBuffer image, int x1, int y1, int x2, int y2, int color)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int x = x1, y = y1;

            while (true)
            {
                if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
                {
                    image.SetPixel(x, y, color);
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

        /// <summary>
        /// Convert Color to integer representation
        /// </summary>
        private int ColorToInt(Color color)
        {
            return (color.R << 16) | (color.G << 8) | color.B;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validate and clamp property values
        /// </summary>
        public override bool ValidateConfiguration()
        {
            NumColors = Math.Clamp(NumColors, MinColors, MaxColors);
            NumPoints = Math.Clamp(NumPoints, MinPoints, MaxPoints);

            // Ensure colors array is properly sized
            if (Colors.Length != MaxColors)
            {
                var newColors = new Color[MaxColors];
                Array.Copy(Colors, newColors, Math.Min(Colors.Length, MaxColors));
                Colors = newColors;
            }

            return true;
        }

        /// <summary>
        /// Get a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            string channelText = GetChannelText();
            string modeText = Mode == 0 ? "Points" : "Lines";

            return $"SuperScope: {channelText}, {modeText}, " +
                   $"Colors: {NumColors}, Points: {NumPoints}";
        }

        /// <summary>
        /// Get channel selection text
        /// </summary>
        private string GetChannelText()
        {
            switch (WhichChannel & 3)
            {
                case 0: return "Left";
                case 1: return "Right";
                case 2: return "Center";
                default: return "Unknown";
            }
        }

        #endregion

        #region Overrides

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(1, 1);
        }

        public override void Reset()
        {
            base.Reset();
            ResetState();
        }

        #endregion
    }
}
