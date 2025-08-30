using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Simple spectrum analyzer and oscilloscope visualization effect
    /// Based on r_simple.cpp from original AVS
    /// Provides fundamental audio visualization with multiple modes
    /// </summary>
    public class SimpleEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Simple effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Visualization mode
        /// 0 = Spectrum (lines), 1 = Spectrum (dots), 2 = Spectrum (solid), 
        /// 3 = Oscilloscope (lines), 4 = Oscilloscope (dots), 5 = Oscilloscope (solid)
        /// </summary>
        public int VisualizationMode { get; set; } = 0;

        /// <summary>
        /// Audio channel selection
        /// 0 = Left, 1 = Right, 2 = Center (L+R)
        /// </summary>
        public int ChannelMode { get; set; } = 2;

        /// <summary>
        /// Visualization color
        /// </summary>
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// X position offset (relative to center)
        /// </summary>
        public int XPosition { get; set; } = 0;

        /// <summary>
        /// Y position offset (relative to center)
        /// </summary>
        public int YPosition { get; set; } = 0;

        /// <summary>
        /// Width of the visualization
        /// </summary>
        public int VisualizationWidth { get; set; } = 300;

        /// <summary>
        /// Height of the visualization
        /// </summary>
        public int VisualizationHeight { get; set; } = 100;

        /// <summary>
        /// Oscilloscope trigger level (-1.0 to 1.0)
        /// </summary>
        public float TriggerLevel { get; set; } = 0.0f;

        /// <summary>
        /// Spectrum analyzer bands (for spectrum mode)
        /// </summary>
        public int SpectrumBands { get; set; } = 64;

        /// <summary>
        /// Peak hold time in frames
        /// </summary>
        public int PeakHoldTime { get; set; } = 30;

        /// <summary>
        /// Whether to draw peaks
        /// </summary>
        public bool DrawPeaks { get; set; } = true;

        /// <summary>
        /// Smoothing factor (0.0 to 1.0)
        /// </summary>
        public float Smoothing { get; set; } = 0.8f;

        /// <summary>
        /// Audio sensitivity multiplier (0.1 to 10.0)
        /// </summary>
        public float Sensitivity { get; set; } = 1.0f;

        /// <summary>
        /// Scale factor for visualization (0.1 to 5.0)
        /// </summary>
        public float ScaleFactor { get; set; } = 1.0f;

        /// <summary>
        /// Minimum bar height (0.0 to 1.0)
        /// </summary>
        public float MinBarHeight { get; set; } = 0.0f;

        /// <summary>
        /// Maximum bar height (0.1 to 2.0)
        /// </summary>
        public float MaxBarHeight { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private float[] _lastSpectrumData;
        private float[] _peakData;
        private int[] _peakHoldCounters;
        private float[] _smoothedSpectrumData;

        #endregion

        #region Constructor

        public SimpleEffectsNode()
        {
            Name = "Simple Effects";
            Description = "Spectrum analyzer and oscilloscope audio visualization";
            Category = "Audio Visualization";
            
            _lastSpectrumData = new float[512];
            _peakData = new float[512];
            _peakHoldCounters = new int[512];
            _smoothedSpectrumData = new float[512];
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Canvas", typeof(ImageBuffer), true, null, "Canvas to draw on"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Canvas with visualization"));
        }

        #endregion

        #region Effect Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            // Copy input to output
            for (int i = 0; i < output.Pixels.Length; i++)
            {
                output.Pixels[i] = imageBuffer.Pixels[i];
            }

            // Process visualization based on mode
            if (VisualizationMode >= 0 && VisualizationMode <= 2)
            {
                // Spectrum visualization
                var fftData = GetAudioData(audioFeatures);
                ProcessSpectrumVisualization(output, fftData);
            }
            else if (VisualizationMode >= 3 && VisualizationMode <= 5)
            {
                // Oscilloscope visualization
                var waveData = GetWaveformData(audioFeatures);
                ProcessOscilloscopeVisualization(output, waveData);
            }

            return output;
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion

        #region Private Methods

        private float[] GetAudioData(AudioFeatures audioFeatures)
        {
            switch (ChannelMode)
            {
                case 0: // Left
                    return audioFeatures.FFTData;
                case 1: // Right
                    return audioFeatures.FFTData;
                case 2: // Center (L+R)
                default:
                    return audioFeatures.FFTData;
            }
        }

        private float[] GetWaveformData(AudioFeatures audioFeatures)
        {
            switch (ChannelMode)
            {
                case 0: // Left
                    return audioFeatures.WaveformData;
                case 1: // Right
                    return audioFeatures.WaveformData;
                case 2: // Center (L+R)
                default:
                    return audioFeatures.WaveformData;
            }
        }

        private void ProcessSpectrumVisualization(ImageBuffer canvas, float[] fftData)
        {
            if (fftData == null) return;

            int bands = Math.Min(SpectrumBands, fftData.Length / 2);
            int centerX = canvas.Width / 2 + XPosition;
            int centerY = canvas.Height / 2 + YPosition;
            int startX = centerX - VisualizationWidth / 2;
            int baseY = centerY + VisualizationHeight / 2;

            uint colorValue = (uint)((Color.A << 24) | (Color.R << 16) | (Color.G << 8) | Color.B);

            for (int i = 0; i < bands; i++)
            {
                float magnitude = fftData[i];
                
                // Apply smoothing
                _smoothedSpectrumData[i] = _smoothedSpectrumData[i] * Smoothing + magnitude * (1 - Smoothing);
                magnitude = _smoothedSpectrumData[i];

                // FIXED: Apply proper scaling and clamping to prevent "constantly at MAX" issue
                float scaledMagnitude = Math.Max(MinBarHeight, Math.Min(MaxBarHeight, magnitude * Sensitivity * ScaleFactor));
                int barHeight = (int)(scaledMagnitude * VisualizationHeight);
                int peakHeight = (int)(Math.Max(MinBarHeight, Math.Min(MaxBarHeight, _peakData[i] * Sensitivity * ScaleFactor)) * VisualizationHeight);

                // Update peaks with proper scaling
                if (magnitude > _peakData[i])
                {
                    _peakData[i] = magnitude;
                    _peakHoldCounters[i] = PeakHoldTime;
                }
                else if (_peakHoldCounters[i] > 0)
                {
                    _peakHoldCounters[i]--;
                }
                else
                {
                    _peakData[i] *= 0.95f; // Slow peak decay
                }

                int x = startX + (i * VisualizationWidth) / bands;
                int barWidth = Math.Max(1, VisualizationWidth / bands);

                switch (VisualizationMode)
                {
                    case 0: // Spectrum lines
                        DrawVerticalLine(canvas, x, baseY, baseY - barHeight, colorValue);
                        if (DrawPeaks && peakHeight > barHeight)
                        {
                            DrawHorizontalLine(canvas, x, x + barWidth, baseY - peakHeight, colorValue);
                        }
                        break;

                    case 1: // Spectrum dots
                        if (barHeight > 0)
                        {
                            SetPixel(canvas, x, baseY - barHeight, colorValue);
                        }
                        if (DrawPeaks && peakHeight > barHeight)
                        {
                            SetPixel(canvas, x, baseY - peakHeight, colorValue);
                        }
                        break;

                    case 2: // Spectrum solid
                        DrawFilledRectangle(canvas, x, baseY - barHeight, barWidth, barHeight, colorValue);
                        if (DrawPeaks && peakHeight > barHeight)
                        {
                            DrawHorizontalLine(canvas, x, x + barWidth, baseY - peakHeight, colorValue);
                        }
                        break;
                }
            }
        }

        private void ProcessOscilloscopeVisualization(ImageBuffer canvas, float[] waveData)
        {
            if (waveData == null) return;

            int centerX = canvas.Width / 2 + XPosition;
            int centerY = canvas.Height / 2 + YPosition;
            int startX = centerX - VisualizationWidth / 2;
            int endX = centerX + VisualizationWidth / 2;

            uint colorValue = (uint)((Color.A << 24) | (Color.R << 16) | (Color.G << 8) | Color.B);

            // Find trigger point for stable display
            int triggerIndex = FindTriggerPoint(waveData);
            
            int prevY = centerY;
            for (int x = startX; x < endX; x++)
            {
                float progress = (float)(x - startX) / VisualizationWidth;
                int sampleIndex = triggerIndex + (int)(progress * (waveData.Length - triggerIndex));
                
                if (sampleIndex >= waveData.Length) break;

                float sample = waveData[sampleIndex];
                
                // FIXED: Apply proper scaling and clamping for oscilloscope
                float scaledSample = Math.Max(-1f, Math.Min(1f, sample * Sensitivity * ScaleFactor));
                int y = centerY - (int)(scaledSample * VisualizationHeight / 2);
                y = Math.Max(0, Math.Min(canvas.Height - 1, y));

                switch (VisualizationMode)
                {
                    case 3: // Oscilloscope lines
                        if (x > startX)
                        {
                            DrawLine(canvas, x - 1, prevY, x, y, colorValue);
                        }
                        prevY = y;
                        break;

                    case 4: // Oscilloscope dots
                        SetPixel(canvas, x, y, colorValue);
                        break;

                    case 5: // Oscilloscope solid
                        DrawVerticalLine(canvas, x, centerY, y, colorValue);
                        break;
                }
            }
        }

        private int FindTriggerPoint(float[] waveData)
        {
            // Simple trigger detection - find rising edge near trigger level
            for (int i = 1; i < waveData.Length / 2; i++)
            {
                if (waveData[i - 1] <= TriggerLevel && waveData[i] > TriggerLevel)
                {
                    return i;
                }
            }
            return 0;
        }

        private void SetPixel(ImageBuffer canvas, int x, int y, uint color)
        {
            if (x >= 0 && x < canvas.Width && y >= 0 && y < canvas.Height)
            {
                canvas.Pixels[y * canvas.Width + x] = (int)color;
            }
        }

        private void DrawVerticalLine(ImageBuffer canvas, int x, int y1, int y2, uint color)
        {
            int startY = Math.Min(y1, y2);
            int endY = Math.Max(y1, y2);
            
            for (int y = startY; y <= endY; y++)
            {
                SetPixel(canvas, x, y, color);
            }
        }

        private void DrawHorizontalLine(ImageBuffer canvas, int x1, int x2, int y, uint color)
        {
            int startX = Math.Min(x1, x2);
            int endX = Math.Max(x1, x2);
            
            for (int x = startX; x <= endX; x++)
            {
                SetPixel(canvas, x, y, color);
            }
        }

        private void DrawLine(ImageBuffer canvas, int x1, int y1, int x2, int y2, uint color)
        {
            // Simple Bresenham line algorithm
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int x = x1, y = y1;
            while (true)
            {
                SetPixel(canvas, x, y, color);
                
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

        private void DrawFilledRectangle(ImageBuffer canvas, int x, int y, int width, int height, uint color)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    SetPixel(canvas, x + dx, y + dy, color);
                }
            }
        }

        #endregion
    }
}
