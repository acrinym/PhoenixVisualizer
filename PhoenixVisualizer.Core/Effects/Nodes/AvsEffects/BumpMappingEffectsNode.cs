using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Bump Mapping effect with 3D lighting and displacement
    /// Based on r_bump.cpp C_BumpClass from original AVS
    /// Creates illusion of surface relief with dynamic lighting
    /// </summary>
    public class BumpMappingEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Bump Mapping effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Normal depth intensity (0 to 255)
        /// </summary>
        public int Depth { get; set; } = 30;

        /// <summary>
        /// Beat-triggered depth intensity (0 to 255)
        /// </summary>
        public int BeatDepth { get; set; } = 100;

        /// <summary>
        /// Beat reactivity enabled
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat effect duration in frames
        /// </summary>
        public int BeatDuration { get; set; } = 15;

        /// <summary>
        /// Show light source position as a visible point
        /// </summary>
        public bool ShowLightSource { get; set; } = false;

        /// <summary>
        /// Invert depth calculation
        /// </summary>
        public bool InvertDepth { get; set; } = false;

        /// <summary>
        /// Use legacy coordinate system (0-100 range vs 0-1 range)
        /// </summary>
        public bool OldStyleCoordinates { get; set; } = true;

        /// <summary>
        /// Additive blending mode
        /// </summary>
        public bool AdditiveBlending { get; set; } = false;

        /// <summary>
        /// 50/50 blending mode
        /// </summary>
        public bool AverageBlending { get; set; } = false;

        /// <summary>
        /// Light source X position (0.0 to 1.0 or 0-100 if old style)
        /// </summary>
        public float LightX { get; set; } = 0.5f;

        /// <summary>
        /// Light source Y position (0.0 to 1.0 or 0-100 if old style)
        /// </summary>
        public float LightY { get; set; } = 0.5f;

        /// <summary>
        /// Light brightness intensity (0.0 to 1.0)
        /// </summary>
        public float LightIntensity { get; set; } = 1.0f;

        /// <summary>
        /// Light movement speed for automatic movement
        /// </summary>
        public float LightSpeed { get; set; } = 0.01f;

        /// <summary>
        /// Enable automatic light movement
        /// </summary>
        public bool AutoLightMovement { get; set; } = true;

        #endregion

        #region Private Fields

        private int _beatCounter = 0;
        private float _lightAngle = 0.0f;
        private bool _initialized = false;

        #endregion

        #region Constructor

        public BumpMappingEffectsNode()
        {
            Name = "Bump Mapping";
            Description = "3D lighting and displacement effect with dynamic light source";
            Category = "Transform Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for bump mapping"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Bump mapped output image"));
        }

        #endregion

        #region Effect Processing

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                var sourceImage = GetInputValue<ImageBuffer>("Image", inputData);
                if (sourceImage?.Data == null) return;

                var outputImage = new ImageBuffer(sourceImage.Width, sourceImage.Height);

                // Initialize if needed
                if (!_initialized)
                {
                    InitializeEffect();
                    _initialized = true;
                }

                // Update light position
                UpdateLightPosition(audioFeatures);

                // Handle beat reactivity
                bool isBeat = audioFeatures.Beat;
                if (BeatReactive && isBeat)
                {
                    _beatCounter = BeatDuration;
                }
                else if (_beatCounter > 0)
                {
                    _beatCounter--;
                }

                // Calculate effective depth
                int effectiveDepth = _beatCounter > 0 ? BeatDepth : Depth;

                // Apply bump mapping
                ApplyBumpMapping(sourceImage, outputImage, effectiveDepth, isBeat);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bump Mapping] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void InitializeEffect()
        {
            // Initialize light position and other parameters
            if (!AutoLightMovement)
            {
                // Use fixed position
                _lightAngle = 0.0f;
            }
        }

        private void UpdateLightPosition(AudioFeatures audioFeatures)
        {
            if (AutoLightMovement)
            {
                // Automatic circular movement
                _lightAngle += LightSpeed;
                if (_lightAngle >= 2 * Math.PI)
                    _lightAngle -= (float)(2 * Math.PI);

                // Update light position in circular pattern
                LightX = 0.5f + 0.3f * (float)Math.Cos(_lightAngle);
                LightY = 0.5f + 0.3f * (float)Math.Sin(_lightAngle);
            }

            // Clamp light position to valid range
            if (OldStyleCoordinates)
            {
                LightX = Math.Max(0, Math.Min(100, LightX));
                LightY = Math.Max(0, Math.Min(100, LightY));
            }
            else
            {
                LightX = Math.Max(0.0f, Math.Min(1.0f, LightX));
                LightY = Math.Max(0.0f, Math.Min(1.0f, LightY));
            }
        }

        private void ApplyBumpMapping(ImageBuffer source, ImageBuffer output, int depth, bool isBeat)
        {
            int width = source.Width;
            int height = source.Height;

            // Calculate light position in screen coordinates
            int lightScreenX, lightScreenY;
            if (OldStyleCoordinates)
            {
                lightScreenX = (int)(LightX / 100.0f * width);
                lightScreenY = (int)(LightY / 100.0f * height);
            }
            else
            {
                lightScreenX = (int)(LightX * width);
                lightScreenY = (int)(LightY * height);
            }

            // Clamp to screen boundaries
            lightScreenX = Math.Max(0, Math.Min(width - 1, lightScreenX));
            lightScreenY = Math.Max(0, Math.Min(height - 1, lightScreenY));

            // Process each pixel
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    uint sourcePixel = source.Data[index];

                    // Calculate depth for this pixel
                    int pixelDepth = CalculateDepthOfPixel(sourcePixel, InvertDepth);

                    // Calculate lighting based on distance from light source
                    float distanceFromLight = CalculateDistanceFromLight(x, y, lightScreenX, lightScreenY, width, height);
                    int lightingValue = CalculateLightingValue(pixelDepth, depth, distanceFromLight);

                    // Apply lighting to pixel
                    uint resultPixel = ApplyDepthToPixel(sourcePixel, lightingValue);

                    // Apply blending mode
                    if (AdditiveBlending)
                    {
                        output.Data[index] = BlendAdditive(sourcePixel, resultPixel);
                    }
                    else if (AverageBlending)
                    {
                        output.Data[index] = BlendAverage(sourcePixel, resultPixel);
                    }
                    else
                    {
                        output.Data[index] = resultPixel;
                    }
                }
            }

            // Draw light source indicator if enabled
            if (ShowLightSource)
            {
                DrawLightSource(output, lightScreenX, lightScreenY);
            }
        }

        private int CalculateDepthOfPixel(uint pixel, bool invert)
        {
            // Extract RGB components
            int r = (int)((pixel >> 16) & 0xFF);
            int g = (int)((pixel >> 8) & 0xFF);
            int b = (int)(pixel & 0xFF);

            // Use maximum RGB component for depth (as per original AVS)
            int maxRGB = Math.Max(Math.Max(r, g), b);

            return invert ? 255 - maxRGB : maxRGB;
        }

        private float CalculateDistanceFromLight(int x, int y, int lightX, int lightY, int width, int height)
        {
            float dx = (float)(x - lightX) / width;
            float dy = (float)(y - lightY) / height;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private int CalculateLightingValue(int pixelDepth, int depthIntensity, float distanceFromLight)
        {
            // Calculate lighting falloff based on distance
            float lightFalloff = Math.Max(0.0f, 1.0f - distanceFromLight * 2.0f);
            
            // Apply depth and intensity
            float lightingEffect = (pixelDepth / 255.0f) * (depthIntensity / 255.0f) * lightFalloff * LightIntensity;
            
            // Convert to lighting value (-127 to +127 range)
            return (int)((lightingEffect - 0.5f) * 254);
        }

        private uint ApplyDepthToPixel(uint pixel, int lightingValue)
        {
            // Extract color components
            int a = (int)((pixel >> 24) & 0xFF);
            int r = (int)((pixel >> 16) & 0xFF);
            int g = (int)((pixel >> 8) & 0xFF);
            int b = (int)(pixel & 0xFF);

            // Apply lighting with clamping (as per original AVS)
            r = Math.Max(0, Math.Min(254, r + lightingValue));
            g = Math.Max(0, Math.Min(254, g + lightingValue));
            b = Math.Max(0, Math.Min(254, b + lightingValue));

            return (uint)((a << 24) | (r << 16) | (g << 8) | b);
        }

        private uint BlendAdditive(uint source, uint result)
        {
            int aS = (int)((source >> 24) & 0xFF);
            int rS = (int)((source >> 16) & 0xFF);
            int gS = (int)((source >> 8) & 0xFF);
            int bS = (int)(source & 0xFF);

            int aR = (int)((result >> 24) & 0xFF);
            int rR = (int)((result >> 16) & 0xFF);
            int gR = (int)((result >> 8) & 0xFF);
            int bR = (int)(result & 0xFF);

            int finalR = Math.Min(255, rS + rR);
            int finalG = Math.Min(255, gS + gR);
            int finalB = Math.Min(255, bS + bR);
            int finalA = Math.Max(aS, aR);

            return (uint)((finalA << 24) | (finalR << 16) | (finalG << 8) | finalB);
        }

        private uint BlendAverage(uint source, uint result)
        {
            int aS = (int)((source >> 24) & 0xFF);
            int rS = (int)((source >> 16) & 0xFF);
            int gS = (int)((source >> 8) & 0xFF);
            int bS = (int)(source & 0xFF);

            int aR = (int)((result >> 24) & 0xFF);
            int rR = (int)((result >> 16) & 0xFF);
            int gR = (int)((result >> 8) & 0xFF);
            int bR = (int)(result & 0xFF);

            int finalR = (rS + rR) / 2;
            int finalG = (gS + gR) / 2;
            int finalB = (bS + bR) / 2;
            int finalA = Math.Max(aS, aR);

            return (uint)((finalA << 24) | (finalR << 16) | (finalG << 8) | finalB);
        }

        private void DrawLightSource(ImageBuffer output, int lightX, int lightY)
        {
            // Draw a small cross to indicate light position
            uint lightColor = 0xFFFFFF00; // Yellow

            // Draw horizontal line
            for (int x = Math.Max(0, lightX - 3); x <= Math.Min(output.Width - 1, lightX + 3); x++)
            {
                if (lightY >= 0 && lightY < output.Height)
                {
                    output.Data[lightY * output.Width + x] = lightColor;
                }
            }

            // Draw vertical line
            for (int y = Math.Max(0, lightY - 3); y <= Math.Min(output.Height - 1, lightY + 3); y++)
            {
                if (lightX >= 0 && lightX < output.Width)
                {
                    output.Data[y * output.Width + lightX] = lightColor;
                }
            }
        }

        #endregion

        #region Configuration

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "Depth", Depth },
                { "BeatDepth", BeatDepth },
                { "BeatReactive", BeatReactive },
                { "BeatDuration", BeatDuration },
                { "ShowLightSource", ShowLightSource },
                { "InvertDepth", InvertDepth },
                { "OldStyleCoordinates", OldStyleCoordinates },
                { "AdditiveBlending", AdditiveBlending },
                { "AverageBlending", AverageBlending },
                { "LightX", LightX },
                { "LightY", LightY },
                { "LightIntensity", LightIntensity },
                { "LightSpeed", LightSpeed },
                { "AutoLightMovement", AutoLightMovement }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            
            if (config.TryGetValue("Depth", out var depth))
                Depth = Convert.ToInt32(depth);
            
            if (config.TryGetValue("BeatDepth", out var beatDepth))
                BeatDepth = Convert.ToInt32(beatDepth);
            
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            
            if (config.TryGetValue("BeatDuration", out var beatDuration))
                BeatDuration = Convert.ToInt32(beatDuration);
            
            if (config.TryGetValue("ShowLightSource", out var showLight))
                ShowLightSource = Convert.ToBoolean(showLight);
            
            if (config.TryGetValue("InvertDepth", out var invert))
                InvertDepth = Convert.ToBoolean(invert);
            
            if (config.TryGetValue("OldStyleCoordinates", out var oldStyle))
                OldStyleCoordinates = Convert.ToBoolean(oldStyle);
            
            if (config.TryGetValue("AdditiveBlending", out var additive))
                AdditiveBlending = Convert.ToBoolean(additive);
            
            if (config.TryGetValue("AverageBlending", out var average))
                AverageBlending = Convert.ToBoolean(average);
            
            if (config.TryGetValue("LightX", out var lightX))
                LightX = Convert.ToSingle(lightX);
            
            if (config.TryGetValue("LightY", out var lightY))
                LightY = Convert.ToSingle(lightY);
            
            if (config.TryGetValue("LightIntensity", out var intensity))
                LightIntensity = Convert.ToSingle(intensity);
            
            if (config.TryGetValue("LightSpeed", out var speed))
                LightSpeed = Convert.ToSingle(speed);
            
            if (config.TryGetValue("AutoLightMovement", out var autoMove))
                AutoLightMovement = Convert.ToBoolean(autoMove);
        }

        #endregion
    }
}