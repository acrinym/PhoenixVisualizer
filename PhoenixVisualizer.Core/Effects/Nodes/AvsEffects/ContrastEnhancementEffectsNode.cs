using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Enhanced Contrast effect with advanced processing algorithms
    /// Advanced contrast processing beyond basic contrast adjustment
    /// Provides histogram equalization, adaptive contrast, and multi-band enhancement
    /// </summary>
    public class ContrastEnhancementEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Contrast Enhancement effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Enhancement algorithm
        /// 0 = Basic contrast, 1 = Histogram equalization, 2 = Adaptive contrast, 3 = CLAHE
        /// </summary>
        public int EnhancementAlgorithm { get; set; } = 0;

        /// <summary>
        /// Contrast intensity (0.0 to 3.0, 1.0 = no change)
        /// </summary>
        public float ContrastIntensity { get; set; } = 1.5f;

        /// <summary>
        /// Brightness adjustment (-1.0 to 1.0)
        /// </summary>
        public float BrightnessAdjustment { get; set; } = 0.0f;

        /// <summary>
        /// Gamma correction (0.1 to 3.0, 1.0 = no change)
        /// </summary>
        public float Gamma { get; set; } = 1.0f;

        /// <summary>
        /// Enable separate RGB channel processing
        /// </summary>
        public bool ProcessChannelsSeparately { get; set; } = false;

        /// <summary>
        /// Red channel weight for luminance calculation
        /// </summary>
        public float RedWeight { get; set; } = 0.299f;

        /// <summary>
        /// Green channel weight for luminance calculation
        /// </summary>
        public float GreenWeight { get; set; } = 0.587f;

        /// <summary>
        /// Blue channel weight for luminance calculation
        /// </summary>
        public float BlueWeight { get; set; } = 0.114f;

        /// <summary>
        /// Beat reactivity enabled
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat contrast multiplier
        /// </summary>
        public float BeatContrastMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// Adaptive window size for adaptive contrast (in pixels)
        /// </summary>
        public int AdaptiveWindowSize { get; set; } = 16;

        /// <summary>
        /// Clip limit for CLAHE algorithm (0.0 to 10.0)
        /// </summary>
        public float CLAHEClipLimit { get; set; } = 2.0f;

        /// <summary>
        /// Preserve highlights (0.0 to 1.0)
        /// </summary>
        public float HighlightPreservation { get; set; } = 0.8f;

        /// <summary>
        /// Shadow recovery (0.0 to 1.0)
        /// </summary>
        public float ShadowRecovery { get; set; } = 0.2f;

        #endregion

        #region Private Fields

        private int _beatCounter = 0;
        private int[] _histogram = new int[256];
        private float[] _lookupTable = new float[256];
        private const int BEAT_DURATION = 20;

        #endregion

        #region Constructor

        public ContrastEnhancementEffectsNode()
        {
            Name = "Contrast Enhancement Effects";
            Description = "Advanced contrast processing with histogram equalization and adaptive algorithms";
            Category = "Color Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for contrast enhancement"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Contrast enhanced output image"));
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

                // Handle beat reactivity
                if (BeatReactive && audioFeatures.Beat)
                {
                    _beatCounter = BEAT_DURATION;
                }
                else if (_beatCounter > 0)
                {
                    _beatCounter--;
                }

                // Calculate effective contrast
                float effectiveContrast = CalculateEffectiveContrast();

                // Apply enhancement based on algorithm
                switch (EnhancementAlgorithm)
                {
                    case 0:
                        ApplyBasicContrast(sourceImage, outputImage, effectiveContrast);
                        break;
                    case 1:
                        ApplyHistogramEqualization(sourceImage, outputImage, effectiveContrast);
                        break;
                    case 2:
                        ApplyAdaptiveContrast(sourceImage, outputImage, effectiveContrast);
                        break;
                    case 3:
                        ApplyCLAHE(sourceImage, outputImage, effectiveContrast);
                        break;
                    default:
                        ApplyBasicContrast(sourceImage, outputImage, effectiveContrast);
                        break;
                }

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Contrast Enhancement] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private float CalculateEffectiveContrast()
        {
            float contrast = ContrastIntensity;
            
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                contrast *= (1.0f + (BeatContrastMultiplier - 1.0f) * beatFactor);
            }
            
            return contrast;
        }

        private void ApplyBasicContrast(ImageBuffer source, ImageBuffer output, float contrast)
        {
            float brightness = BrightnessAdjustment * 255.0f;
            
            for (int i = 0; i < source.Data.Length; i++)
            {
                uint pixel = source.Data[i];
                
                uint a = (pixel >> 24) & 0xFF;
                uint r = (pixel >> 16) & 0xFF;
                uint g = (pixel >> 8) & 0xFF;
                uint b = pixel & 0xFF;

                // Apply contrast and brightness
                r = ApplyContrastToByte(r, contrast, brightness);
                g = ApplyContrastToByte(g, contrast, brightness);
                b = ApplyContrastToByte(b, contrast, brightness);

                // Apply gamma correction
                if (Math.Abs(Gamma - 1.0f) > 0.01f)
                {
                    r = ApplyGammaToByte(r);
                    g = ApplyGammaToByte(g);
                    b = ApplyGammaToByte(b);
                }

                output.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        private uint ApplyContrastToByte(uint value, float contrast, float brightness)
        {
            // Apply contrast around midpoint (128)
            float result = ((value - 128.0f) * contrast) + 128.0f + brightness;
            return (uint)Math.Max(0, Math.Min(255, Math.Round(result)));
        }

        private uint ApplyGammaToByte(uint value)
        {
            float normalized = value / 255.0f;
            float corrected = (float)Math.Pow(normalized, 1.0 / Gamma);
            return (uint)Math.Max(0, Math.Min(255, Math.Round(corrected * 255)));
        }

        private void ApplyHistogramEqualization(ImageBuffer source, ImageBuffer output, float intensity)
        {
            // Build histogram
            BuildHistogram(source);
            
            // Create cumulative distribution function
            float[] cdf = new float[256];
            cdf[0] = _histogram[0];
            for (int i = 1; i < 256; i++)
            {
                cdf[i] = cdf[i - 1] + _histogram[i];
            }

            // Normalize CDF
            float totalPixels = source.Data.Length;
            for (int i = 0; i < 256; i++)
            {
                cdf[i] /= totalPixels;
                _lookupTable[i] = cdf[i] * 255.0f;
            }

            // Apply equalization
            for (int i = 0; i < source.Data.Length; i++)
            {
                uint pixel = source.Data[i];
                
                uint a = (pixel >> 24) & 0xFF;
                uint r = (pixel >> 16) & 0xFF;
                uint g = (pixel >> 8) & 0xFF;
                uint b = pixel & 0xFF;

                if (ProcessChannelsSeparately)
                {
                    r = ApplyLookupTable(r, intensity);
                    g = ApplyLookupTable(g, intensity);
                    b = ApplyLookupTable(b, intensity);
                }
                else
                {
                    // Convert to luminance
                    float luminance = r * RedWeight + g * GreenWeight + b * BlueWeight;
                    uint lum = (uint)Math.Round(luminance);
                    uint enhancedLum = ApplyLookupTable(lum, intensity);
                    
                    // Preserve color ratios
                    if (luminance > 0)
                    {
                        float ratio = enhancedLum / luminance;
                        r = (uint)Math.Min(255, r * ratio);
                        g = (uint)Math.Min(255, g * ratio);
                        b = (uint)Math.Min(255, b * ratio);
                    }
                }

                output.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        private void BuildHistogram(ImageBuffer source)
        {
            Array.Clear(_histogram, 0, 256);
            
            for (int i = 0; i < source.Data.Length; i++)
            {
                uint pixel = source.Data[i];
                
                if (ProcessChannelsSeparately)
                {
                    uint r = (pixel >> 16) & 0xFF;
                    uint g = (pixel >> 8) & 0xFF;
                    uint b = pixel & 0xFF;
                    
                    _histogram[r]++;
                    _histogram[g]++;
                    _histogram[b]++;
                }
                else
                {
                    uint r = (pixel >> 16) & 0xFF;
                    uint g = (pixel >> 8) & 0xFF;
                    uint b = pixel & 0xFF;
                    
                    float luminance = r * RedWeight + g * GreenWeight + b * BlueWeight;
                    int lum = (int)Math.Round(luminance);
                    _histogram[Math.Max(0, Math.Min(255, lum))]++;
                }
            }
        }

        private uint ApplyLookupTable(uint value, float intensity)
        {
            uint enhanced = (uint)Math.Round(_lookupTable[value]);
            
            // Blend with original based on intensity
            return (uint)(value * (1.0f - intensity) + enhanced * intensity);
        }

        private void ApplyAdaptiveContrast(ImageBuffer source, ImageBuffer output, float intensity)
        {
            // Adaptive contrast using local statistics
            int width = source.Width;
            int height = source.Height;
            int windowSize = AdaptiveWindowSize;
            int halfWindow = windowSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate local statistics
                    float localMean = 0;
                    float localVariance = 0;
                    int pixelCount = 0;

                    // Sample window around current pixel
                    for (int dy = -halfWindow; dy <= halfWindow; dy++)
                    {
                        for (int dx = -halfWindow; dx <= halfWindow; dx++)
                        {
                            int nx = Math.Max(0, Math.Min(width - 1, x + dx));
                            int ny = Math.Max(0, Math.Min(height - 1, y + dy));
                            
                            uint pixel = source.Data[ny * width + nx];
                            float luminance = GetLuminance(pixel);
                            
                            localMean += luminance;
                            pixelCount++;
                        }
                    }

                    localMean /= pixelCount;

                    // Calculate variance
                    for (int dy = -halfWindow; dy <= halfWindow; dy++)
                    {
                        for (int dx = -halfWindow; dx <= halfWindow; dx++)
                        {
                            int nx = Math.Max(0, Math.Min(width - 1, x + dx));
                            int ny = Math.Max(0, Math.Min(height - 1, y + dy));
                            
                            uint pixel = source.Data[ny * width + nx];
                            float luminance = GetLuminance(pixel);
                            float diff = luminance - localMean;
                            localVariance += diff * diff;
                        }
                    }

                    localVariance /= pixelCount;
                    float localStdDev = (float)Math.Sqrt(localVariance);

                    // Apply adaptive enhancement
                    uint sourcePixel = source.Data[y * width + x];
                    uint enhancedPixel = ApplyAdaptiveEnhancement(sourcePixel, localMean, localStdDev, intensity);
                    output.Data[y * width + x] = enhancedPixel;
                }
            }
        }

        private float GetLuminance(uint pixel)
        {
            uint r = (pixel >> 16) & 0xFF;
            uint g = (pixel >> 8) & 0xFF;
            uint b = pixel & 0xFF;
            
            return r * RedWeight + g * GreenWeight + b * BlueWeight;
        }

        private uint ApplyAdaptiveEnhancement(uint pixel, float localMean, float localStdDev, float intensity)
        {
            uint a = (pixel >> 24) & 0xFF;
            uint r = (pixel >> 16) & 0xFF;
            uint g = (pixel >> 8) & 0xFF;
            uint b = pixel & 0xFF;

            float currentLuminance = GetLuminance(pixel);
            
            // Adaptive enhancement based on local statistics
            float enhancement = 1.0f;
            if (localStdDev > 0)
            {
                enhancement = 1.0f + (intensity * (currentLuminance - localMean) / localStdDev * 0.1f);
            }

            enhancement = Math.Max(0.1f, Math.Min(3.0f, enhancement));

            // Apply enhancement
            r = (uint)Math.Max(0, Math.Min(255, r * enhancement));
            g = (uint)Math.Max(0, Math.Min(255, g * enhancement));
            b = (uint)Math.Max(0, Math.Min(255, b * enhancement));

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private void ApplyCLAHE(ImageBuffer source, ImageBuffer output, float intensity)
        {
            // Simplified CLAHE (Contrast Limited Adaptive Histogram Equalization)
            // This is a basic implementation - full CLAHE would be more complex
            
            int tileSize = Math.Max(8, AdaptiveWindowSize);
            int tilesX = (source.Width + tileSize - 1) / tileSize;
            int tilesY = (source.Height + tileSize - 1) / tileSize;

            // Process each tile
            for (int tileY = 0; tileY < tilesY; tileY++)
            {
                for (int tileX = 0; tileX < tilesX; tileX++)
                {
                    int startX = tileX * tileSize;
                    int startY = tileY * tileSize;
                    int endX = Math.Min(startX + tileSize, source.Width);
                    int endY = Math.Min(startY + tileSize, source.Height);

                    // Build tile histogram
                    Array.Clear(_histogram, 0, 256);
                    int tilePixelCount = 0;

                    for (int y = startY; y < endY; y++)
                    {
                        for (int x = startX; x < endX; x++)
                        {
                            uint pixel = source.Data[y * source.Width + x];
                            int luminance = (int)GetLuminance(pixel);
                            _histogram[Math.Max(0, Math.Min(255, luminance))]++;
                            tilePixelCount++;
                        }
                    }

                    // Apply clip limit
                    int clipLimit = (int)(CLAHEClipLimit * tilePixelCount / 256.0f);
                    for (int i = 0; i < 256; i++)
                    {
                        if (_histogram[i] > clipLimit)
                        {
                            _histogram[i] = clipLimit;
                        }
                    }

                    // Create local lookup table
                    float[] localLUT = new float[256];
                    float cumulative = 0;
                    for (int i = 0; i < 256; i++)
                    {
                        cumulative += _histogram[i];
                        localLUT[i] = (cumulative / tilePixelCount) * 255.0f;
                    }

                    // Apply to tile
                    for (int y = startY; y < endY; y++)
                    {
                        for (int x = startX; x < endX; x++)
                        {
                            uint pixel = source.Data[y * source.Width + x];
                            int luminance = (int)GetLuminance(pixel);
                            float enhanced = localLUT[Math.Max(0, Math.Min(255, luminance))];
                            
                            // Blend with original
                            float final = luminance * (1.0f - intensity) + enhanced * intensity;
                            
                            // Preserve color ratios
                            uint r = (pixel >> 16) & 0xFF;
                            uint g = (pixel >> 8) & 0xFF;
                            uint b = pixel & 0xFF;
                            uint a = (pixel >> 24) & 0xFF;

                            if (luminance > 0)
                            {
                                float ratio = final / luminance;
                                r = (uint)Math.Min(255, r * ratio);
                                g = (uint)Math.Min(255, g * ratio);
                                b = (uint)Math.Min(255, b * ratio);
                            }

                            output.Data[y * source.Width + x] = (a << 24) | (r << 16) | (g << 8) | b;
                        }
                    }
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
                { "EnhancementAlgorithm", EnhancementAlgorithm },
                { "ContrastIntensity", ContrastIntensity },
                { "BrightnessAdjustment", BrightnessAdjustment },
                { "Gamma", Gamma },
                { "ProcessChannelsSeparately", ProcessChannelsSeparately },
                { "RedWeight", RedWeight },
                { "GreenWeight", GreenWeight },
                { "BlueWeight", BlueWeight },
                { "BeatReactive", BeatReactive },
                { "BeatContrastMultiplier", BeatContrastMultiplier },
                { "AdaptiveWindowSize", AdaptiveWindowSize },
                { "CLAHEClipLimit", CLAHEClipLimit },
                { "HighlightPreservation", HighlightPreservation },
                { "ShadowRecovery", ShadowRecovery }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            
            if (config.TryGetValue("EnhancementAlgorithm", out var algorithm))
                EnhancementAlgorithm = Convert.ToInt32(algorithm);
            
            if (config.TryGetValue("ContrastIntensity", out var intensity))
                ContrastIntensity = Convert.ToSingle(intensity);
            
            if (config.TryGetValue("BrightnessAdjustment", out var brightness))
                BrightnessAdjustment = Convert.ToSingle(brightness);
            
            if (config.TryGetValue("Gamma", out var gamma))
                Gamma = Convert.ToSingle(gamma);
            
            if (config.TryGetValue("ProcessChannelsSeparately", out var processChannels))
                ProcessChannelsSeparately = Convert.ToBoolean(processChannels);
            
            if (config.TryGetValue("RedWeight", out var redWeight))
                RedWeight = Convert.ToSingle(redWeight);
            
            if (config.TryGetValue("GreenWeight", out var greenWeight))
                GreenWeight = Convert.ToSingle(greenWeight);
            
            if (config.TryGetValue("BlueWeight", out var blueWeight))
                BlueWeight = Convert.ToSingle(blueWeight);
            
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            
            if (config.TryGetValue("BeatContrastMultiplier", out var beatMultiplier))
                BeatContrastMultiplier = Convert.ToSingle(beatMultiplier);
            
            if (config.TryGetValue("AdaptiveWindowSize", out var windowSize))
                AdaptiveWindowSize = Convert.ToInt32(windowSize);
            
            if (config.TryGetValue("CLAHEClipLimit", out var clipLimit))
                CLAHEClipLimit = Convert.ToSingle(clipLimit);
            
            if (config.TryGetValue("HighlightPreservation", out var highlight))
                HighlightPreservation = Convert.ToSingle(highlight);
            
            if (config.TryGetValue("ShadowRecovery", out var shadow))
                ShadowRecovery = Convert.ToSingle(shadow);
        }

        #endregion
    }
}