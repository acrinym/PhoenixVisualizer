using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Dynamic Distance Modifier (DDM) effect
    /// Creates dynamic spatial transformations based on distance calculations
    /// Provides radial and linear distance-based modifications with audio reactivity
    /// </summary>
    public class DDMEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the DDM effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Distance calculation mode
        /// 0 = Radial from center, 1 = Radial from point, 2 = Linear horizontal, 3 = Linear vertical, 4 = Diagonal
        /// </summary>
        public int DistanceMode { get; set; } = 0;

        /// <summary>
        /// Center X position for radial modes (0.0 to 1.0)
        /// </summary>
        public float CenterX { get; set; } = 0.5f;

        /// <summary>
        /// Center Y position for radial modes (0.0 to 1.0)
        /// </summary>
        public float CenterY { get; set; } = 0.5f;

        /// <summary>
        /// Modification type
        /// 0 = Brightness, 1 = Contrast, 2 = Saturation, 3 = Hue shift, 4 = Displacement, 5 = Blur
        /// </summary>
        public int ModificationType { get; set; } = 0;

        /// <summary>
        /// Modification intensity (0.0 to 2.0)
        /// </summary>
        public float ModificationIntensity { get; set; } = 1.0f;

        /// <summary>
        /// Distance falloff function
        /// 0 = Linear, 1 = Exponential, 2 = Inverse, 3 = Gaussian, 4 = Step function
        /// </summary>
        public int FalloffFunction { get; set; } = 0;

        /// <summary>
        /// Falloff rate (controls steepness of falloff)
        /// </summary>
        public float FalloffRate { get; set; } = 1.0f;

        /// <summary>
        /// Maximum distance for effect (0.0 to 1.0)
        /// </summary>
        public float MaxDistance { get; set; } = 1.0f;

        /// <summary>
        /// Minimum distance for effect (0.0 to 1.0)
        /// </summary>
        public float MinDistance { get; set; } = 0.0f;

        /// <summary>
        /// Beat reactivity enabled
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat intensity multiplier
        /// </summary>
        public float BeatIntensityMultiplier { get; set; } = 2.0f;

        /// <summary>
        /// Dynamic center movement enabled
        /// </summary>
        public bool DynamicCenter { get; set; } = false;

        /// <summary>
        /// Center movement speed
        /// </summary>
        public float CenterMovementSpeed { get; set; } = 0.01f;

        /// <summary>
        /// Audio-reactive center movement
        /// </summary>
        public bool AudioReactiveCenter { get; set; } = false;

        /// <summary>
        /// Invert distance calculation
        /// </summary>
        public bool InvertDistance { get; set; } = false;

        /// <summary>
        /// Enable smooth interpolation
        /// </summary>
        public bool SmoothInterpolation { get; set; } = true;

        #endregion

        #region Private Fields

        private float _centerAngle = 0.0f;
        private int _beatCounter = 0;
        private const int BEAT_DURATION = 20;

        #endregion

        #region Constructor

        public DDMEffectsNode()
        {
            Name = "DDM Effects";
            Description = "Dynamic Distance Modifier with spatial transformations and audio reactivity";
            Category = "Transform Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for DDM processing"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "DDM processed output image"));
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

                // Update dynamic center if enabled
                UpdateDynamicCenter(audioFeatures);

                // Apply DDM effect
                ApplyDDMEffect(sourceImage, outputImage, audioFeatures);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DDM Effects] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void UpdateDynamicCenter(AudioFeatures audioFeatures)
        {
            if (DynamicCenter)
            {
                _centerAngle += CenterMovementSpeed;
                if (_centerAngle >= 2 * Math.PI)
                    _centerAngle -= (float)(2 * Math.PI);

                // Circular movement
                CenterX = 0.5f + 0.3f * (float)Math.Cos(_centerAngle);
                CenterY = 0.5f + 0.3f * (float)Math.Sin(_centerAngle);
            }

            if (AudioReactiveCenter && audioFeatures != null)
            {
                // Move center based on audio features
                float bassOffset = (audioFeatures.Bass - 0.5f) * 0.2f;
                float trebleOffset = (audioFeatures.Treble - 0.5f) * 0.2f;

                CenterX = Math.Max(0.1f, Math.Min(0.9f, CenterX + bassOffset * 0.1f));
                CenterY = Math.Max(0.1f, Math.Min(0.9f, CenterY + trebleOffset * 0.1f));
            }
        }

        private void ApplyDDMEffect(ImageBuffer source, ImageBuffer output, AudioFeatures audioFeatures)
        {
            int width = source.Width;
            int height = source.Height;
            
            // Calculate effective intensity
            float effectiveIntensity = CalculateEffectiveIntensity();

            // Get center coordinates in pixels
            float centerPixelX = CenterX * width;
            float centerPixelY = CenterY * height;

            // Calculate maximum distance for normalization
            float maxPossibleDistance = CalculateMaxDistance(width, height, centerPixelX, centerPixelY);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate distance from center/line
                    float distance = CalculateDistance(x, y, centerPixelX, centerPixelY, width, height);
                    
                    // Normalize distance
                    float normalizedDistance = distance / maxPossibleDistance;
                    
                    // Apply distance constraints
                    if (normalizedDistance < MinDistance || normalizedDistance > MaxDistance)
                    {
                        output.Data[y * width + x] = source.Data[y * width + x];
                        continue;
                    }

                    // Apply inversion if enabled
                    if (InvertDistance)
                    {
                        normalizedDistance = 1.0f - normalizedDistance;
                    }

                    // Apply falloff function
                    float falloffFactor = ApplyFalloffFunction(normalizedDistance);

                    // Calculate modification strength
                    float modificationStrength = effectiveIntensity * falloffFactor;

                    // Apply modification
                    uint sourcePixel = source.Data[y * width + x];
                    uint modifiedPixel = ApplyModification(sourcePixel, modificationStrength, normalizedDistance);
                    output.Data[y * width + x] = modifiedPixel;
                }
            }
        }

        private float CalculateEffectiveIntensity()
        {
            float intensity = ModificationIntensity;

            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                intensity *= (1.0f + (BeatIntensityMultiplier - 1.0f) * beatFactor);
            }

            return intensity;
        }

        private float CalculateDistance(int x, int y, float centerX, float centerY, int width, int height)
        {
            switch (DistanceMode)
            {
                case 0: // Radial from center
                case 1: // Radial from point
                    float dx = x - centerX;
                    float dy = y - centerY;
                    return (float)Math.Sqrt(dx * dx + dy * dy);

                case 2: // Linear horizontal
                    return Math.Abs(x - centerX);

                case 3: // Linear vertical
                    return Math.Abs(y - centerY);

                case 4: // Diagonal
                    float diagX = x - centerX;
                    float diagY = y - centerY;
                    return Math.Abs(diagX + diagY) / (float)Math.Sqrt(2);

                default:
                    return 0.0f;
            }
        }

        private float CalculateMaxDistance(int width, int height, float centerX, float centerY)
        {
            switch (DistanceMode)
            {
                case 0:
                case 1: // Radial
                    float maxDx = Math.Max(centerX, width - centerX);
                    float maxDy = Math.Max(centerY, height - centerY);
                    return (float)Math.Sqrt(maxDx * maxDx + maxDy * maxDy);

                case 2: // Linear horizontal
                    return Math.Max(centerX, width - centerX);

                case 3: // Linear vertical
                    return Math.Max(centerY, height - centerY);

                case 4: // Diagonal
                    return (width + height) / (float)Math.Sqrt(2);

                default:
                    return 1.0f;
            }
        }

        private float ApplyFalloffFunction(float distance)
        {
            switch (FalloffFunction)
            {
                case 0: // Linear
                    return 1.0f - distance;

                case 1: // Exponential
                    return (float)Math.Exp(-distance * FalloffRate);

                case 2: // Inverse
                    return 1.0f / (1.0f + distance * FalloffRate);

                case 3: // Gaussian
                    float sigma = FalloffRate * 0.5f;
                    return (float)Math.Exp(-(distance * distance) / (2 * sigma * sigma));

                case 4: // Step function
                    return distance < (1.0f / FalloffRate) ? 1.0f : 0.0f;

                default:
                    return 1.0f - distance;
            }
        }

        private uint ApplyModification(uint sourcePixel, float strength, float distance)
        {
            uint a = (sourcePixel >> 24) & 0xFF;
            uint r = (sourcePixel >> 16) & 0xFF;
            uint g = (sourcePixel >> 8) & 0xFF;
            uint b = sourcePixel & 0xFF;

            switch (ModificationType)
            {
                case 0: // Brightness
                    float brightnessFactor = 1.0f + (strength - 1.0f);
                    r = (uint)Math.Max(0, Math.Min(255, r * brightnessFactor));
                    g = (uint)Math.Max(0, Math.Min(255, g * brightnessFactor));
                    b = (uint)Math.Max(0, Math.Min(255, b * brightnessFactor));
                    break;

                case 1: // Contrast
                    float contrastFactor = strength;
                    r = ApplyContrast(r, contrastFactor);
                    g = ApplyContrast(g, contrastFactor);
                    b = ApplyContrast(b, contrastFactor);
                    break;

                case 2: // Saturation
                    var (newR, newG, newB) = ApplySaturation(r, g, b, strength);
                    r = newR;
                    g = newG;
                    b = newB;
                    break;

                case 3: // Hue shift
                    (r, g, b) = ApplyHueShift(r, g, b, strength * distance * 360.0f);
                    break;

                case 4: // Displacement (simple color shift)
                    int shift = (int)(strength * distance * 64);
                    r = (uint)((r + shift) % 256);
                    g = (uint)((g + shift) % 256);
                    b = (uint)((b + shift) % 256);
                    break;

                case 5: // Blur effect (simple averaging)
                    float blurFactor = strength * distance;
                    if (blurFactor > 0.1f)
                    {
                        // Simple blur by averaging with a neutral value
                        uint avgValue = (r + g + b) / 3;
                        r = (uint)(r * (1.0f - blurFactor) + avgValue * blurFactor);
                        g = (uint)(g * (1.0f - blurFactor) + avgValue * blurFactor);
                        b = (uint)(b * (1.0f - blurFactor) + avgValue * blurFactor);
                    }
                    break;
            }

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private uint ApplyContrast(uint value, float contrast)
        {
            float normalized = (value - 128.0f) * contrast + 128.0f;
            return (uint)Math.Max(0, Math.Min(255, Math.Round(normalized)));
        }

        private (uint r, uint g, uint b) ApplySaturation(uint r, uint g, uint b, float saturation)
        {
            // Convert to grayscale
            float gray = r * 0.299f + g * 0.587f + b * 0.114f;
            
            // Apply saturation
            float newR = gray + (r - gray) * saturation;
            float newG = gray + (g - gray) * saturation;
            float newB = gray + (b - gray) * saturation;
            
            return (
                (uint)Math.Max(0, Math.Min(255, Math.Round(newR))),
                (uint)Math.Max(0, Math.Min(255, Math.Round(newG))),
                (uint)Math.Max(0, Math.Min(255, Math.Round(newB)))
            );
        }

        private (uint r, uint g, uint b) ApplyHueShift(uint r, uint g, uint b, float hueShift)
        {
            // Simple hue shift approximation by color channel rotation
            float shift = (hueShift % 360.0f) / 360.0f;
            
            if (shift < 0.33f)
                return (g, b, r); // RGB -> GBR
            else if (shift < 0.66f)
                return (b, r, g); // RGB -> BRG
            else
                return (r, g, b); // RGB -> RGB
        }

        #endregion

        #region Configuration

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "DistanceMode", DistanceMode },
                { "CenterX", CenterX },
                { "CenterY", CenterY },
                { "ModificationType", ModificationType },
                { "ModificationIntensity", ModificationIntensity },
                { "FalloffFunction", FalloffFunction },
                { "FalloffRate", FalloffRate },
                { "MaxDistance", MaxDistance },
                { "MinDistance", MinDistance },
                { "BeatReactive", BeatReactive },
                { "BeatIntensityMultiplier", BeatIntensityMultiplier },
                { "DynamicCenter", DynamicCenter },
                { "CenterMovementSpeed", CenterMovementSpeed },
                { "AudioReactiveCenter", AudioReactiveCenter },
                { "InvertDistance", InvertDistance },
                { "SmoothInterpolation", SmoothInterpolation }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            
            if (config.TryGetValue("DistanceMode", out var distanceMode))
                DistanceMode = Convert.ToInt32(distanceMode);
            
            if (config.TryGetValue("CenterX", out var centerX))
                CenterX = Convert.ToSingle(centerX);
            
            if (config.TryGetValue("CenterY", out var centerY))
                CenterY = Convert.ToSingle(centerY);
            
            if (config.TryGetValue("ModificationType", out var modType))
                ModificationType = Convert.ToInt32(modType);
            
            if (config.TryGetValue("ModificationIntensity", out var modIntensity))
                ModificationIntensity = Convert.ToSingle(modIntensity);
            
            if (config.TryGetValue("FalloffFunction", out var falloffFunc))
                FalloffFunction = Convert.ToInt32(falloffFunc);
            
            if (config.TryGetValue("FalloffRate", out var falloffRate))
                FalloffRate = Convert.ToSingle(falloffRate);
            
            if (config.TryGetValue("MaxDistance", out var maxDist))
                MaxDistance = Convert.ToSingle(maxDist);
            
            if (config.TryGetValue("MinDistance", out var minDist))
                MinDistance = Convert.ToSingle(minDist);
            
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            
            if (config.TryGetValue("BeatIntensityMultiplier", out var beatMult))
                BeatIntensityMultiplier = Convert.ToSingle(beatMult);
            
            if (config.TryGetValue("DynamicCenter", out var dynamicCenter))
                DynamicCenter = Convert.ToBoolean(dynamicCenter);
            
            if (config.TryGetValue("CenterMovementSpeed", out var moveSpeed))
                CenterMovementSpeed = Convert.ToSingle(moveSpeed);
            
            if (config.TryGetValue("AudioReactiveCenter", out var audioCenter))
                AudioReactiveCenter = Convert.ToBoolean(audioCenter);
            
            if (config.TryGetValue("InvertDistance", out var invert))
                InvertDistance = Convert.ToBoolean(invert);
            
            if (config.TryGetValue("SmoothInterpolation", out var smooth))
                SmoothInterpolation = Convert.ToBoolean(smooth);
        }

        #endregion
    }
}