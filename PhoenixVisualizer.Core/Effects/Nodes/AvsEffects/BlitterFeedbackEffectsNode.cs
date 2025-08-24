using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class BlitterFeedbackEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Blitter Feedback effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Primary scaling factor (0-256) for normal scaling mode
        /// </summary>
        public int Scale { get; set; } = 30;

        /// <summary>
        /// Secondary scaling factor (0-256) for beat-responsive scaling
        /// </summary>
        public int Scale2 { get; set; } = 30;

        /// <summary>
        /// Enables blending between scaled content and original frame
        /// </summary>
        public bool Blend { get; set; } = false;

        /// <summary>
        /// Enables automatic scaling changes in response to beat detection
        /// </summary>
        public bool BeatResponse { get; set; } = false;

        /// <summary>
        /// Enables high-quality subpixel interpolation for smooth scaling
        /// </summary>
        public bool Subpixel { get; set; } = true;

        /// <summary>
        /// Overall intensity of the effect (0.0 to 1.0)
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private int currentPosition;
        private int lastWidth = 0;
        private int lastHeight = 0;
        private const int ScaleThreshold = 32;
        private const int TransitionSpeed = 3;

        #endregion

        #region Constructor

        public BlitterFeedbackEffectsNode()
        {
            Name = "Blitter Feedback Effects";
            Description = "Advanced scaling and feedback operations with beat-responsive behavior";
            Category = "AVS Effects";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null, "Input image buffer"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable effect"));
            _inputPorts.Add(new EffectPort("Scale", typeof(int), false, 30, "Primary scaling factor (0-256)"));
            _inputPorts.Add(new EffectPort("Scale2", typeof(int), false, 30, "Beat-responsive scaling factor (0-256)"));
            _inputPorts.Add(new EffectPort("Blend", typeof(bool), false, false, "Enable blending with original frame"));
            _inputPorts.Add(new EffectPort("BeatResponse", typeof(bool), false, false, "Enable beat-responsive scaling"));
            _inputPorts.Add(new EffectPort("Subpixel", typeof(bool), false, true, "Enable subpixel interpolation"));
            _inputPorts.Add(new EffectPort("Intensity", typeof(float), false, 1.0f, "Effect intensity (0.0-1.0)"));
            
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), true, null, "Processed image buffer"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) return inputs["Input"];

            var input = inputs["Input"] as ImageBuffer;
            if (input == null) return inputs["Input"];

            int width = input.Width;
            int height = input.Height;

            // Initialize position if dimensions change
            if (lastWidth != width || lastHeight != height)
            {
                InitializeDimensions(width, height);
            }

            // Create output buffer
            var output = new ImageBuffer(width, height);
            
            // Copy input to output first
            Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

            // Handle beat response and position updates
            HandleBeatResponse(audioFeatures);
            UpdateScalingPosition();

            // Determine scaling value and mode
            int scaleValue = CalculateScaleValue();
            if (scaleValue < 0) scaleValue = 0;

            // Apply appropriate scaling mode
            if (scaleValue < ScaleThreshold)
            {
                ApplyNormalScaling(output, scaleValue);
            }
            else if (scaleValue > ScaleThreshold)
            {
                ApplyOutwardScaling(output, scaleValue);
            }

            return output;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the primary scaling factor
        /// </summary>
        public void SetScale(int scale)
        {
            Scale = Math.Max(0, Math.Min(256, scale));
            if (currentPosition == 0)
                currentPosition = Scale;
        }

        /// <summary>
        /// Sets the secondary scaling factor for beat response
        /// </summary>
        public void SetScale2(int scale)
        {
            Scale2 = Math.Max(0, Math.Min(256, scale));
        }

        /// <summary>
        /// Enables or disables blending mode
        /// </summary>
        public void SetBlending(bool enable)
        {
            Blend = enable;
        }

        /// <summary>
        /// Enables or disables beat response
        /// </summary>
        public void SetBeatResponse(bool enable)
        {
            BeatResponse = enable;
        }

        /// <summary>
        /// Enables or disables subpixel interpolation
        /// </summary>
        public void SetSubpixel(bool enable)
        {
            Subpixel = enable;
        }

        #endregion

        #region Private Methods

        private void InitializeDimensions(int width, int height)
        {
            lastWidth = width;
            lastHeight = height;
            currentPosition = Scale;
        }

        private void HandleBeatResponse(AudioFeatures audioFeatures)
        {
            if (BeatResponse && audioFeatures?.IsBeat == true)
            {
                currentPosition = Scale2;
            }
        }

        private void UpdateScalingPosition()
        {
            if (Scale < Scale2)
            {
                currentPosition = Math.Max(Scale, currentPosition);
                currentPosition -= TransitionSpeed;
            }
            else
            {
                currentPosition = Math.Min(Scale, currentPosition);
                currentPosition += TransitionSpeed;
            }
        }

        private int CalculateScaleValue()
        {
            return currentPosition;
        }

        private void ApplyNormalScaling(ImageBuffer imageBuffer, int scaleValue)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Calculate scaling factors
            double scaleX = ((scaleValue + 32) << 16) / 64.0;
            int startX = (int)(((width << 16) - (scaleX * width)) / 2);
            int startY = (int)(((height << 16) - (scaleX * height)) / 2);

            if (Subpixel)
            {
                ApplySubpixelNormalScaling(imageBuffer, scaleX, startX, startY);
            }
            else
            {
                ApplyIntegerNormalScaling(imageBuffer, scaleX, startX, startY);
            }
        }

        private void ApplySubpixelNormalScaling(ImageBuffer imageBuffer, double scaleX, int startX, int startY)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;
            double currentY = startY;

            for (int y = 0; y < height; y++)
            {
                double currentX = startX;
                int sourceY = (int)((long)currentY >> 16);
                int yPart = (int)((long)currentY >> 8) & 0xFF;
                currentY += scaleX;

                if (sourceY >= 0 && sourceY < height)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceX = (int)((long)currentX >> 16);
                        int xPart = (int)((long)currentX >> 8) & 0xFF;
                        currentX += scaleX;

                        if (sourceX >= 0 && sourceX < width)
                        {
                            int sourceColor = GetInterpolatedColor(imageBuffer, sourceX, sourceY, xPart, yPart);
                            
                            if (Blend)
                            {
                                int existingColor = imageBuffer.GetPixel(x, y);
                                sourceColor = BlendAverage(existingColor, sourceColor);
                            }

                            imageBuffer.SetPixel(x, y, sourceColor);
                        }
                    }
                }
            }
        }

        private void ApplyIntegerNormalScaling(ImageBuffer imageBuffer, double scaleX, int startX, int startY)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;
            double currentY = startY;

            for (int y = 0; y < height; y++)
            {
                double currentX = startX;
                int sourceY = (int)((long)currentY >> 16);
                currentY += scaleX;

                if (sourceY >= 0 && sourceY < height)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceX = (int)((long)currentX >> 16);
                        currentX += scaleX;

                        if (sourceX >= 0 && sourceX < width)
                        {
                            int sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                            
                            if (Blend)
                            {
                                int existingColor = imageBuffer.GetPixel(x, y);
                                sourceColor = BlendAverage(existingColor, sourceColor);
                            }

                            imageBuffer.SetPixel(x, y, sourceColor);
                        }
                    }
                }
            }
        }

        private void ApplyOutwardScaling(ImageBuffer imageBuffer, int scaleValue)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Calculate scaling factors for outward expansion
            const int adjustment = 7;
            int deltaScale = ((scaleValue + (1 << adjustment) - 32) << (16 - adjustment));
            
            if (deltaScale <= 0) return;

            int xLength = ((width << 16) / deltaScale) & ~3;
            int yLength = (height << 16) / deltaScale;

            if (xLength >= width || yLength >= height) return;

            int startX = (width - xLength) / 2;
            int startY = (height - yLength) / 2;

            ApplyOutwardScalingToRegion(imageBuffer, startX, startY, xLength, yLength, deltaScale);
        }

        private void ApplyOutwardScalingToRegion(ImageBuffer imageBuffer, int startX, int startY, int xLength, int yLength, int deltaScale)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;
            double currentY = 32768.0; // 0.5 in fixed-point

            for (int y = 0; y < yLength; y++)
            {
                double currentX = 32768.0;
                int sourceY = (int)((long)currentY >> 16);
                currentY += deltaScale;

                if (sourceY >= 0 && sourceY < height)
                {
                    for (int x = 0; x < xLength; x++)
                    {
                        int sourceX = (int)((long)currentX >> 16);
                        currentX += deltaScale;

                        if (sourceX >= 0 && sourceX < width)
                        {
                            int sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                            int targetX = startX + x;
                            int targetY = startY + y;

                            if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
                            {
                                if (Blend)
                                {
                                    int existingColor = imageBuffer.GetPixel(targetX, targetY);
                                    sourceColor = BlendAverage(existingColor, sourceColor);
                                }

                                imageBuffer.SetPixel(targetX, targetY, sourceColor);
                            }
                        }
                    }
                }
            }
        }

        private int GetInterpolatedColor(ImageBuffer imageBuffer, int x, int y, int xPart, int yPart)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Get the four surrounding pixels for bilinear interpolation
            int c00 = imageBuffer.GetPixel(x, y);
            int c10 = (x + 1 < width) ? imageBuffer.GetPixel(x + 1, y) : c00;
            int c01 = (y + 1 < height) ? imageBuffer.GetPixel(x, y + 1) : c00;
            int c11 = (x + 1 < width && y + 1 < height) ? imageBuffer.GetPixel(x + 1, y + 1) : c00;

            // Extract color channels
            int r00 = (c00 >> 16) & 0xFF, g00 = (c00 >> 8) & 0xFF, b00 = c00 & 0xFF, a00 = (c00 >> 24) & 0xFF;
            int r10 = (c10 >> 16) & 0xFF, g10 = (c10 >> 8) & 0xFF, b10 = c10 & 0xFF, a10 = (c10 >> 24) & 0xFF;
            int r01 = (c01 >> 16) & 0xFF, g01 = (c01 >> 8) & 0xFF, b01 = c01 & 0xFF, a01 = (c01 >> 24) & 0xFF;
            int r11 = (c11 >> 16) & 0xFF, g11 = (c11 >> 8) & 0xFF, b11 = c11 & 0xFF, a11 = (c11 >> 24) & 0xFF;

            // Perform bilinear interpolation
            int xWeight = 255 - xPart;
            int yWeight = 255 - yPart;

            int r = (r00 * xWeight * yWeight + r10 * xPart * yWeight + 
                    r01 * xWeight * yPart + r11 * xPart * yPart) >> 16;
            int g = (g00 * xWeight * yWeight + g10 * xPart * yWeight + 
                    g01 * xWeight * yPart + g11 * xPart * yPart) >> 16;
            int b = (b00 * xWeight * yWeight + b10 * xPart * yWeight + 
                    b01 * xWeight * yPart + b11 * xPart * yPart) >> 16;
            int a = (a00 * xWeight * yWeight + a10 * xPart * yWeight + 
                    a01 * xWeight * yPart + a11 * xPart * yPart) >> 16;

            // Clamp values and combine channels
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);
            a = Math.Clamp(a, 0, 255);

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private int BlendAverage(int a, int b)
        {
            // Extract and average color channels
            int ar = (a >> 24) & 0xFF, ag = (a >> 16) & 0xFF, ab = (a >> 8) & 0xFF, aa = a & 0xFF;
            int br = (b >> 24) & 0xFF, bg = (b >> 16) & 0xFF, bb = (b >> 8) & 0xFF, ba = b & 0xFF;

            int r = (ar + br) / 2;
            int g = (ag + bg) / 2;
            int b_avg = (ab + bb) / 2;
            int a_avg = (aa + ba) / 2;

            return (a_avg << 24) | (r << 16) | (g << 8) | b_avg;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        public override bool ValidateConfiguration()
        {
            if (Scale < 0 || Scale > 256)
                Scale = 30;

            if (Scale2 < 0 || Scale2 > 256)
                Scale2 = 30;

            if (Intensity < 0.0f || Intensity > 1.0f)
                Intensity = 1.0f;

            return true;
        }

        /// <summary>
        /// Returns a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            return $"Blitter Feedback: {(Enabled ? "Enabled" : "Disabled")}, " +
                   $"Scale: {Scale}, Scale2: {Scale2}, " +
                   $"Blend: {(Blend ? "On" : "Off")}, " +
                   $"Beat: {(BeatResponse ? "On" : "Off")}, " +
                   $"Subpixel: {(Subpixel ? "On" : "Off")}";
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
