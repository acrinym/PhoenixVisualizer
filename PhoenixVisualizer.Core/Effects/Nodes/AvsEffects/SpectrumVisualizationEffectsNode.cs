using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Audio spectrum visualization effect that displays frequency data
    /// as bars, lines, or other visual representations.
    /// </summary>
    public class SpectrumVisualizationEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>Whether the spectrum visualization is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Visualization mode (Bars, Lines, Circles, etc.).</summary>
        public VisualizationMode Mode { get; set; } = VisualizationMode.Bars;

        /// <summary>Color of the spectrum visualization.</summary>
        public Color SpectrumColor { get; set; } = Color.Cyan;

        /// <summary>Background color for the visualization.</summary>
        public Color BackgroundColor { get; set; } = Color.Black;

        /// <summary>Intensity multiplier for the visualization (0.1 to 5.0).</summary>
        public float Intensity { get; set; } = 1.0f;

        /// <summary>Number of frequency bands to display (16 to 256).</summary>
        public int BandCount { get; set; } = 64;

        /// <summary>Spacing between bars/lines in pixels.</summary>
        public float Spacing { get; set; } = 2.0f;

        /// <summary>Width of bars/lines in pixels.</summary>
        public float BarWidth { get; set; } = 8.0f;

        /// <summary>Height multiplier for the visualization (0.1 to 3.0).</summary>
        public float HeightMultiplier { get; set; } = 1.0f;

        /// <summary>Whether to use logarithmic frequency scaling.</summary>
        public bool UseLogarithmicScaling { get; set; } = true;

        /// <summary>Whether to smooth the spectrum data.</summary>
        public bool SmoothSpectrum { get; set; } = true;

        /// <summary>Smoothing factor for spectrum data (0.0 to 1.0).</summary>
        public float SmoothingFactor { get; set; } = 0.7f;

        /// <summary>Whether to show frequency labels.</summary>
        public bool ShowFrequencyLabels { get; set; } = false;

        /// <summary>Whether to use color gradients based on frequency.</summary>
        public bool UseColorGradients { get; set; } = true;

        /// <summary>Minimum frequency to display in Hz.</summary>
        public float MinFrequency { get; set; } = 20.0f;

        /// <summary>Maximum frequency to display in Hz.</summary>
        public float MaxFrequency { get; set; } = 20000.0f;

        #endregion

        #region Private Fields

        private readonly float[] _previousSpectrum;
        private readonly float[] _smoothedSpectrum;
        private readonly Random _random = new Random();
        private int _frameCounter;

        #endregion

        #region Constructor

        public SpectrumVisualizationEffectsNode()
        {
            Name = "Spectrum Visualization Effects";
            Description = "Displays audio frequency spectrum as visual bars, lines, or other representations";
            Category = "Audio Visualization";

            _previousSpectrum = new float[256];
            _smoothedSpectrum = new float[256];
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Audio", typeof(AudioFeatures), true, null, "Audio input for spectrum analysis"));
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for overlay"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable visualization"));
            _inputPorts.Add(new EffectPort("Mode", typeof(VisualizationMode), false, VisualizationMode.Bars, "Visualization mode"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with spectrum visualization"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var imageObj) || imageObj is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            if (!inputs.TryGetValue("Audio", out var audioObj) || audioObj is not AudioFeatures audio)
                return GetDefaultOutput();

            if (inputs.TryGetValue("Enabled", out var en))
                Enabled = (bool)en;
            if (inputs.TryGetValue("Mode", out var mode) && mode is VisualizationMode modeEnum)
                Mode = modeEnum;

            if (!Enabled)
                return imageBuffer;

            _frameCounter++;

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height, (int[])imageBuffer.Pixels.Clone());
            
            ProcessSpectrum(audio);
            RenderVisualization(output);

            return output;
        }

        #endregion

        #region Spectrum Processing

        private void ProcessSpectrum(AudioFeatures audio)
        {
            if (audio.SpectrumData == null || audio.SpectrumData.Length == 0)
                return;

            int spectrumLength = Math.Min(audio.SpectrumData.Length, _smoothedSpectrum.Length);
            
            // Apply logarithmic scaling if enabled
            if (UseLogarithmicScaling)
            {
                ApplyLogarithmicScaling(audio.SpectrumData, spectrumLength);
            }
            else
            {
                Array.Copy(audio.SpectrumData, _smoothedSpectrum, spectrumLength);
            }

            // Apply smoothing if enabled
            if (SmoothSpectrum)
            {
                ApplySmoothing(spectrumLength);
            }

            // Store current spectrum for next frame
            Array.Copy(_smoothedSpectrum, _previousSpectrum, spectrumLength);
        }

        private void ApplyLogarithmicScaling(float[] spectrum, int length)
        {
            for (int i = 0; i < length; i++)
            {
                float frequency = i * (MaxFrequency - MinFrequency) / length + MinFrequency;
                float logFreq = (float)Math.Log10(frequency / MinFrequency);
                float normalizedFreq = logFreq / (float)Math.Log10(MaxFrequency / MinFrequency);
                
                int spectrumIndex = (int)(normalizedFreq * spectrum.Length);
                if (spectrumIndex >= 0 && spectrumIndex < spectrum.Length)
                {
                    _smoothedSpectrum[i] = spectrum[spectrumIndex];
                }
            }
        }

        private void ApplySmoothing(int length)
        {
            for (int i = 0; i < length; i++)
            {
                _smoothedSpectrum[i] = _smoothedSpectrum[i] * (1.0f - SmoothingFactor) + 
                                      _previousSpectrum[i] * SmoothingFactor;
            }
        }

        #endregion

        #region Rendering

        private void RenderVisualization(ImageBuffer output)
        {
            switch (Mode)
            {
                case VisualizationMode.Bars:
                    RenderBars(output);
                    break;
                case VisualizationMode.Lines:
                    RenderLines(output);
                    break;
                case VisualizationMode.Circles:
                    RenderCircles(output);
                    break;
                case VisualizationMode.Waveform:
                    RenderWaveform(output);
                    break;
                case VisualizationMode.Spectrum:
                    RenderSpectrum(output);
                    break;
            }
        }

        private void RenderBars(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;
            int barCount = Math.Min(BandCount, _smoothedSpectrum.Length);
            float barSpacing = (float)width / barCount;
            float barWidth = Math.Min(BarWidth, barSpacing * 0.8f);

            for (int i = 0; i < barCount; i++)
            {
                float value = _smoothedSpectrum[i] * HeightMultiplier * Intensity;
                int barHeight = (int)Math.Min(value, height * 0.8f);
                
                if (barHeight <= 0) continue;

                float x = i * barSpacing + (barSpacing - barWidth) / 2;
                Color barColor = GetBarColor(i, barCount, value);
                
                // Draw bar from bottom up
                for (int y = 0; y < barHeight; y++)
                {
                    int screenY = height - 1 - y;
                    if (screenY >= 0 && screenY < height)
                    {
                        for (int dx = 0; dx < barWidth; dx++)
                        {
                            int screenX = (int)(x + dx);
                            if (screenX >= 0 && screenX < width)
                            {
                                output.SetPixel(screenX, screenY, barColor.ToArgb());
                            }
                        }
                    }
                }
            }
        }

        private void RenderLines(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;
            int lineCount = Math.Min(BandCount, _smoothedSpectrum.Length);
            float lineSpacing = (float)width / lineCount;

            for (int i = 0; i < lineCount - 1; i++)
            {
                float x1 = i * lineSpacing;
                float x2 = (i + 1) * lineSpacing;
                float y1 = height - (_smoothedSpectrum[i] * HeightMultiplier * Intensity);
                float y2 = height - (_smoothedSpectrum[i + 1] * HeightMultiplier * Intensity);

                DrawLine(output, (int)x1, (int)y1, (int)x2, (int)y2, GetBarColor(i, lineCount, _smoothedSpectrum[i]));
            }
        }

        private void RenderCircles(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;
            int centerX = width / 2;
            int centerY = height / 2;
            int maxRadius = Math.Min(width, height) / 2 - 20;
            int circleCount = Math.Min(BandCount, _smoothedSpectrum.Length);

            for (int i = 0; i < circleCount; i++)
            {
                float value = _smoothedSpectrum[i] * HeightMultiplier * Intensity;
                int radius = (int)((i + 1) * maxRadius / circleCount);
                
                if (radius <= 0) continue;

                Color circleColor = GetBarColor(i, circleCount, value);
                DrawCircle(output, centerX, centerY, radius, circleColor);
            }
        }

        private void RenderWaveform(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;
            int centerY = height / 2;
            int sampleCount = Math.Min(BandCount, _smoothedSpectrum.Length);

            for (int i = 0; i < sampleCount - 1; i++)
            {
                float x1 = i * (float)width / sampleCount;
                float x2 = (i + 1) * (float)width / sampleCount;
                float y1 = centerY + _smoothedSpectrum[i] * HeightMultiplier * Intensity;
                float y2 = centerY + _smoothedSpectrum[i + 1] * HeightMultiplier * Intensity;

                DrawLine(output, (int)x1, (int)y1, (int)x2, (int)y2, GetBarColor(i, sampleCount, _smoothedSpectrum[i]));
            }
        }

        private void RenderSpectrum(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;
            int bandCount = Math.Min(BandCount, _smoothedSpectrum.Length);

            for (int i = 0; i < bandCount; i++)
            {
                float value = _smoothedSpectrum[i] * HeightMultiplier * Intensity;
                int barHeight = (int)Math.Min(value, height);
                
                if (barHeight <= 0) continue;

                float x = i * (float)width / bandCount;
                Color barColor = GetBarColor(i, bandCount, value);
                
                // Draw vertical spectrum bar
                for (int y = 0; y < barHeight; y++)
                {
                    int screenY = height - 1 - y;
                    if (screenY >= 0 && screenY < height)
                    {
                        for (int dx = 0; dx < width / bandCount; dx++)
                        {
                            int screenX = (int)(x + dx);
                            if (screenX >= 0 && screenX < width)
                            {
                                output.SetPixel(screenX, screenY, barColor.ToArgb());
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        private Color GetBarColor(int index, int total, float value)
        {
            if (!UseColorGradients)
                return SpectrumColor;

            // Create color gradient based on frequency and intensity
            float hue = (float)index / total * 240.0f; // Blue to Red
            float saturation = 0.8f;
            float lightness = 0.5f + (value / 255.0f) * 0.3f; // Brighter for higher values

            return HslToRgb(hue, saturation, lightness);
        }

        private Color HslToRgb(float h, float s, float l)
        {
            // Simple HSL to RGB conversion
            float c = (1 - Math.Abs(2 * l - 1)) * s;
            float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            float m = l - c / 2;

            float r, g, b;
            if (h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return Color.FromArgb(
                (int)((r + m) * 255),
                (int)((g + m) * 255),
                (int)((b + m) * 255)
            );
        }

        private void DrawLine(ImageBuffer output, int x1, int y1, int x2, int y2, Color color)
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
                if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
                {
                    output.SetPixel(x, y, color.ToArgb());
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

        private void DrawCircle(ImageBuffer output, int centerX, int centerY, int radius, Color color)
        {
            // Midpoint circle algorithm
            int x = radius;
            int y = 0;
            int err = 0;

            while (x >= y)
            {
                DrawCirclePoints(output, centerX, centerY, x, y, color);
                
                if (err <= 0)
                {
                    y += 1;
                    err += 2 * y + 1;
                }
                if (err > 0)
                {
                    x -= 1;
                    err -= 2 * x + 1;
                }
            }
        }

        private void DrawCirclePoints(ImageBuffer output, int centerX, int centerY, int x, int y, Color color)
        {
            int[] points = { x, y, -x, y, x, -y, -x, -y, y, x, -y, x, y, -x, -y, -x };
            
            for (int i = 0; i < points.Length; i += 2)
            {
                int px = centerX + points[i];
                int py = centerY + points[i + 1];
                
                if (px >= 0 && px < output.Width && py >= 0 && py < output.Height)
                {
                    output.SetPixel(px, py, color.ToArgb());
                }
            }
        }

        #endregion

        #region Public Methods

        public override void Reset()
        {
            base.Reset();
            Array.Clear(_previousSpectrum, 0, _previousSpectrum.Length);
            Array.Clear(_smoothedSpectrum, 0, _smoothedSpectrum.Length);
            _frameCounter = 0;
        }

        public string GetVisualizationStats()
        {
            return $"Mode: {Mode}, Bands: {BandCount}, Frame: {_frameCounter}, Intensity: {Intensity:F2}";
        }

        #endregion

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }

    /// <summary>
    /// Available visualization modes for spectrum display
    /// </summary>
    public enum VisualizationMode
    {
        /// <summary>Vertical bars representing frequency bands</summary>
        Bars,
        /// <summary>Connected lines between frequency points</summary>
        Lines,
        /// <summary>Concentric circles with frequency data</summary>
        Circles,
        /// <summary>Waveform-style horizontal display</summary>
        Waveform,
        /// <summary>Full spectrum waterfall display</summary>
        Spectrum
    }
}
