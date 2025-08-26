using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Engine;
using PhoenixVisualizer.Core.Effects.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Superscope Effects Node
    /// Core AVS visualization engine, now fully powered by PhoenixExpressionEngine
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
        public string InitScript { get; set; } = "n=800;";

        /// <summary>
        /// Frame script (runs every frame)
        /// </summary>
        public string FrameScript { get; set; } = "t=t-0.05;";

        /// <summary>
        /// Beat script (runs on beat detection)
        /// </summary>
        public string BeatScript { get; set; } = "";

        /// <summary>
        /// Point script (runs for each point)
        /// </summary>
        public string PointScript { get; set; } =
            "d=i+v*0.2; r=t+i*$PI*4; x=cos(r)*d; y=sin(r)*d;";

        #endregion

        #region Internal State
        private double _time = 0.0;

        #endregion

        #region Constructor

        public SuperscopeEffectsNode()
        {
            Name = "Superscope";
            Description = "Core AVS visualization engine with Phoenix scripting";
            Category = "AVS Effects";
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
            Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);

            RenderSuperscope(output, audioFeatures);
            return output;
        }

        #endregion

        #region Superscope Rendering

        private void RenderSuperscope(ImageBuffer output, AudioFeatures audioFeatures)
        {
            try
            {
                Engine?.Execute(InitScript);
                Engine?.Set("t", _time);
                Engine?.Execute(FrameScript);

                if (BeatReactive && audioFeatures?.IsBeat == true)
                    Engine?.Execute(BeatScript);

                if (audioFeatures != null)
            {
                RenderPoints(output, audioFeatures);
            }
                _time += 0.016; // ~60fps delta
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Superscope error: {ex.Message}");
            }
        }

        private void RenderPoints(ImageBuffer output, AudioFeatures audioFeatures)
        {
            int width = output.Width;
            int height = output.Height;
            int centerX = width / 2;
            int centerY = height / 2;

            float[] audioData = GetAudioData(audioFeatures);
            if (audioData.Length == 0) return;

            for (int i = 0; i < PointCount && i < audioData.Length; i++)
            {
                Engine?.Set("i", (double)i / PointCount);
                Engine?.Set("v", audioData[i % audioData.Length]);

                Engine?.Execute(PointScript);

                double x = Engine?.Get("x", 0.0) ?? 0.0;
                double y = Engine?.Get("y", 0.0) ?? 0.0;
                double red = Engine?.Get("red", 1.0) ?? 1.0;
                double green = Engine?.Get("green", 1.0) ?? 1.0;
                double blue = Engine?.Get("blue", 1.0) ?? 1.0;
                double skip = Engine?.Get("skip", 0.0) ?? 0.0;

                if (skip > SkipThreshold) continue;

                int pixelX = centerX + (int)(x * centerX);
                int pixelY = centerY + (int)(y * centerX);
                pixelX = Math.Clamp(pixelX, 0, width - 1);
                pixelY = Math.Clamp(pixelY, 0, height - 1);

                int r = Math.Clamp((int)(red * 255), 0, 255);
                int g = Math.Clamp((int)(green * 255), 0, 255);
                int b = Math.Clamp((int)(blue * 255), 0, 255);
                int pixelColor = r | (g << 8) | (b << 16);

                if (DrawMode == 0)
                {
                    output.SetPixel(pixelX, pixelY, pixelColor);
                }
                else if (i > 0)
                {
                    double prevX = Engine?.Get("prevX", x) ?? x;
                    double prevY = Engine?.Get("prevY", y) ?? y;
                    int prevPixelX = centerX + (int)(prevX * centerX);
                    int prevPixelY = centerY + (int)(prevY * centerX);
                    DrawLine(output, prevPixelX, prevPixelY, pixelX, pixelY, pixelColor, (int)LineSize);
                }

                Engine?.Set("prevX", x);
                Engine?.Set("prevY", y);
            }
        }

        #endregion

        #region Audio Data Processing

        private float[] GetAudioData(AudioFeatures? audioFeatures)
        {
            if (audioFeatures == null) return Array.Empty<float>();
            return AudioSource == 0 ? audioFeatures.SpectrumData : audioFeatures.WaveformData;
        }

        #endregion

        #region Script Execution

        private void ExecuteScript(string script)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            // Use the Phoenix Expression Engine for proper ns-eel script execution
            Engine?.Execute(script);
            // Note: Variables are now managed globally by the PhoenixExecutionEngine
            // Local variables are maintained for backward compatibility
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



        #region Default Output

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
