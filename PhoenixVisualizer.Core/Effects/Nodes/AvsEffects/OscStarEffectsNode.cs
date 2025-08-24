using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class OscStarEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Effect configuration - bit-packed channel and position
        /// </summary>
        public int Effect { get; set; } = 0 | (2 << 2) | (2 << 4); // Default: center channel, center position

        /// <summary>
        /// Number of colors for interpolation (1-16)
        /// </summary>
        public int NumColors { get; set; } = 1;

        /// <summary>
        /// Array of colors for interpolation
        /// </summary>
        public Color[] Colors { get; set; } = new Color[16];

        /// <summary>
        /// Star size control (1-32)
        /// </summary>
        public int Size { get; set; } = 8;

        /// <summary>
        /// Rotation speed control (1-32)
        /// </summary>
        public int Rot { get; set; } = 3;

        #endregion

        #region Constants

        // Channel selection constants
        private const int LEFT_CHANNEL = 0;
        private const int RIGHT_CHANNEL = 1;
        private const int CENTER_CHANNEL = 2;

        // Position constants
        private const int TOP_POSITION = 0;
        private const int BOTTOM_POSITION = 1;
        private const int CENTER_POSITION = 2;

        // Star generation constants
        private const int STAR_POINTS = 5;
        private const int MAX_SEGMENTS = 64;
        private const double PI = Math.PI;
        private const double TwoPI = 2.0 * Math.PI;
        private const double ANGLE_STEP = TwoPI / STAR_POINTS;

        // Performance optimization constants
        private const int MaxColors = 16;
        private const int MinColors = 1;
        private const int MaxSize = 32;
        private const int MinSize = 1;
        private const int MaxRot = 32;
        private const int MinRot = 1;
        private const int ColorInterpolationSteps = 64;
        private const double RotationSpeed = 0.01;
        private const double DecayFactor = 1.0 / 1024.0;
        private const double DecayRate = (1.0 / 1024.0 - 1.0 / 128.0) / 64.0;

        #endregion

        #region Internal State

        private int colorPos;
        private double rotationAngle;
        private int lastWidth, lastHeight;
        private readonly object renderLock = new object();

        #endregion

        #region Constructor

        public OscStarEffectsNode()
        {
            Name = "OscStar Effects";
            Description = "Creates dynamic oscillating star-shaped visualizations that respond to audio data";
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
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for star effects"));
            _inputPorts.Add(new EffectPort("Audio", typeof(AudioFeatures), false, null, "Audio features for reactive effects"));
            
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with oscillating star effects"));
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

                // Extract effect configuration
                int whichChannel = (Effect >> 2) & 3;
                int yPosition = (Effect >> 4) & 3;

                // Get audio data
                float[] audioData = GetAudioData(audioFeatures, whichChannel);

                // Calculate star parameters
                var starParams = CalculateStarParameters(audioData, yPosition);

                // Draw oscillating stars
                DrawOscillatingStars(output, starParams, currentColor);

                // Update rotation
                UpdateRotation();

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
            rotationAngle = 0.0;
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
        private float[] GetAudioData(AudioFeatures audioFeatures, int whichChannel)
        {
            float[] audioData = new float[MAX_SEGMENTS];

            if (audioFeatures?.SpectrumData == null)
            {
                // Return default audio data if none provided
                for (int i = 0; i < MAX_SEGMENTS; i++)
                {
                    audioData[i] = 0.5f;
                }
                return audioData;
            }

            if (whichChannel >= 2)
            {
                // Center channel - average of left and right
                for (int i = 0; i < MAX_SEGMENTS; i++)
                {
                    int spectrumIndex = Math.Min(i, audioFeatures.SpectrumData.Length - 1);
                    audioData[i] = (audioFeatures.SpectrumData[spectrumIndex] + 
                                   audioFeatures.SpectrumData[Math.Min(spectrumIndex + 1, audioFeatures.SpectrumData.Length - 1)]) / 2.0f;
                }
            }
            else
            {
                // Left or right channel
                for (int i = 0; i < MAX_SEGMENTS; i++)
                {
                    int spectrumIndex = Math.Min(i, audioFeatures.SpectrumData.Length - 1);
                    audioData[i] = audioFeatures.SpectrumData[spectrumIndex];
                }
            }

            return audioData;
        }

        /// <summary>
        /// Calculate star parameters based on audio data and position
        /// </summary>
        private StarParameters CalculateStarParameters(float[] audioData, int yPosition)
        {
            double scale = Size / 32.0;
            double intensityScale = Math.Min(lastHeight * scale, lastWidth * scale);

            // Calculate center position
            int centerX, centerY;
            centerY = lastHeight / 2;

            switch (yPosition)
            {
                case TOP_POSITION:
                    centerX = lastWidth / 4;
                    break;
                case BOTTOM_POSITION:
                    centerX = lastWidth / 2 + lastWidth / 4;
                    break;
                case CENTER_POSITION:
                default:
                    centerX = lastWidth / 2;
                    break;
            }

            return new StarParameters
            {
                CenterX = centerX,
                CenterY = centerY,
                IntensityScale = intensityScale,
                AudioData = audioData
            };
        }

        /// <summary>
        /// Draw oscillating stars on the image
        /// </summary>
        private void DrawOscillatingStars(ImageBuffer image, StarParameters parameters, Color color)
        {
            int colorValue = ColorToInt(color);

            // Draw each star point
            for (int pointIndex = 0; pointIndex < STAR_POINTS; pointIndex++)
            {
                double pointAngle = rotationAngle + pointIndex * ANGLE_STEP;
                double sinAngle = Math.Sin(pointAngle);
                double cosAngle = Math.Cos(pointAngle);

                // Calculate initial position
                double currentX = parameters.CenterX;
                double currentY = parameters.CenterY;
                double currentP = 0.0;
                double decayFactor = DecayFactor;

                // Draw star arm segments
                for (int segment = 0; segment < MAX_SEGMENTS; segment++)
                {
                    // Calculate audio influence
                    int audioIndex = Math.Min(segment, parameters.AudioData.Length - 1);
                    double audioInfluence = CalculateAudioInfluence(parameters.AudioData[audioIndex]);

                    // Calculate deformed position
                    double deformedX = parameters.CenterX + (cosAngle * currentP) - (sinAngle * audioInfluence * parameters.IntensityScale);
                    double deformedY = parameters.CenterY + (sinAngle * currentP) + (cosAngle * audioInfluence * parameters.IntensityScale);

                    // Draw line segment if within bounds
                    if (IsPointInBounds(deformedX, deformedY, image.Width, image.Height) ||
                        IsPointInBounds(currentX, currentY, image.Width, image.Height))
                    {
                        DrawLine(image, (int)deformedX, (int)deformedY, (int)currentX, (int)currentY, colorValue);
                    }

                    // Update current position
                    currentX = deformedX;
                    currentY = deformedY;
                    currentP += parameters.IntensityScale / MAX_SEGMENTS;

                    // Update decay factor
                    decayFactor -= DecayRate;
                    if (decayFactor < 0) decayFactor = 0;
                }
            }
        }

        /// <summary>
        /// Calculate audio influence on star deformation
        /// </summary>
        private double CalculateAudioInfluence(float audioValue)
        {
            // Convert audio value to bipolar range and apply scaling
            double bipolarValue = (audioValue * 255) - 128;
            return bipolarValue * 0.001; // Scale factor for deformation
        }

        /// <summary>
        /// Check if a point is within image bounds
        /// </summary>
        private bool IsPointInBounds(double x, double y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
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
        /// Update star rotation
        /// </summary>
        private void UpdateRotation()
        {
            rotationAngle += RotationSpeed * Rot;
            if (rotationAngle >= TwoPI)
            {
                rotationAngle -= TwoPI;
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
            Size = Math.Clamp(Size, MinSize, MaxSize);
            Rot = Math.Clamp(Rot, MinRot, MaxRot);

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
            string positionText = GetPositionText();

            return $"OscStar: {channelText}, {positionText}, " +
                   $"Colors: {NumColors}, Size: {Size}, Rotation: {Rot}";
        }

        /// <summary>
        /// Get channel selection text
        /// </summary>
        private string GetChannelText()
        {
            int whichChannel = (Effect >> 2) & 3;
            switch (whichChannel)
            {
                case LEFT_CHANNEL: return "Left";
                case RIGHT_CHANNEL: return "Right";
                case CENTER_CHANNEL: return "Center";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// Get position text
        /// </summary>
        private string GetPositionText()
        {
            int yPosition = (Effect >> 4) & 3;
            switch (yPosition)
            {
                case TOP_POSITION: return "Top";
                case BOTTOM_POSITION: return "Bottom";
                case CENTER_POSITION: return "Center";
                default: return "Unknown";
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Star parameters for drawing
        /// </summary>
        private class StarParameters
        {
            public int CenterX { get; set; }
            public int CenterY { get; set; }
            public double IntensityScale { get; set; }
            public float[] AudioData { get; set; }
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
