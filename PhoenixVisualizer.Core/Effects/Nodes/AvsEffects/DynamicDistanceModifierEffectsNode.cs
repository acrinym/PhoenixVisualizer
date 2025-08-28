using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Dynamic Distance Modifier Effects with structured distance calculations
    /// Based on r_dynamicdistance.cpp from original AVS
    /// Creates distance-based visual modifications with configurable properties and beat-reactive behaviors
    /// </summary>
    public class DynamicDistanceModifierEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Dynamic Distance Modifier effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Distance calculation type
        /// 0 = Euclidean, 1 = Manhattan, 2 = Chebyshev, 3 = Minkowski, 4 = Custom formula
        /// </summary>
        public int DistanceType { get; set; } = 0;

        /// <summary>
        /// Modification type applied based on distance
        /// 0 = Color intensity, 1 = Hue shift, 2 = Saturation, 3 = Brightness, 4 = Displacement, 5 = Blur radius
        /// </summary>
        public int ModificationType { get; set; } = 0;

        /// <summary>
        /// Distance range for effect application
        /// </summary>
        public float DistanceRange { get; set; } = 100.0f;

        /// <summary>
        /// Beat reactivity enabled
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Distance effect pattern
        /// 0 = Linear, 1 = Quadratic, 2 = Cubic, 3 = Sine wave, 4 = Pulse, 5 = Random
        /// </summary>
        public int DistanceEffect { get; set; } = 0;

        /// <summary>
        /// Modification opacity/strength (0.0 to 1.0)
        /// </summary>
        public float ModificationOpacity { get; set; } = 1.0f;

        /// <summary>
        /// Reference point X (0.0 to 1.0)
        /// </summary>
        public float ReferenceX { get; set; } = 0.5f;

        /// <summary>
        /// Reference point Y (0.0 to 1.0)
        /// </summary>
        public float ReferenceY { get; set; } = 0.5f;

        /// <summary>
        /// Parameter 1 for distance effect (context-dependent)
        /// </summary>
        public float Parameter1 { get; set; } = 1.0f;

        /// <summary>
        /// Parameter 2 for distance effect (context-dependent)
        /// </summary>
        public float Parameter2 { get; set; } = 1.0f;

        /// <summary>
        /// Parameter 3 for distance effect (context-dependent)
        /// </summary>
        public float Parameter3 { get; set; } = 1.0f;

        /// <summary>
        /// Minkowski parameter for Minkowski distance (when DistanceType = 3)
        /// </summary>
        public float MinkowskiP { get; set; } = 2.0f;

        /// <summary>
        /// Enable distance inversion
        /// </summary>
        public bool InvertDistance { get; set; } = false;

        /// <summary>
        /// Beat modification multiplier
        /// </summary>
        public float BeatMultiplier { get; set; } = 2.0f;

        /// <summary>
        /// Dynamic reference point movement
        /// </summary>
        public bool DynamicReference { get; set; } = false;

        /// <summary>
        /// Reference movement speed
        /// </summary>
        public float ReferenceSpeed { get; set; } = 0.01f;

        #endregion

        #region Private Fields

        private int _beatCounter = 0;
        private float _referenceAngle = 0.0f;
        private readonly Random _random = new Random();
        private const int BEAT_DURATION = 25;

        #endregion

        #region Constructor

        public DynamicDistanceModifierEffectsNode()
        {
            Name = "Dynamic Distance Modifier Effects";
            Description = "Distance-based visual modifications with structured algorithms and beat reactivity";
            Category = "Transform Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for distance modification"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Distance modified output image"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) 
                return GetDefaultOutput();

            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            // Handle beat reactivity
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                _beatCounter = BEAT_DURATION;
            }
            else if (_beatCounter > 0)
            {
                _beatCounter--;
            }

            // Update dynamic reference point
            UpdateDynamicReference(audioFeatures);

            // Apply distance modification
            ApplyDistanceModification(imageBuffer, output, audioFeatures);

            return output;
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion

        #region Private Methods

        private void UpdateDynamicReference(AudioFeatures? audioFeatures)
        {
            if (DynamicReference)
            {
                _referenceAngle += ReferenceSpeed;
                if (_referenceAngle >= 2 * Math.PI)
                    _referenceAngle -= (float)(2 * Math.PI);

                // Circular movement with audio influence
                float baseRadius = 0.25f;
                if (audioFeatures != null)
                {
                    baseRadius += audioFeatures.RMS * 0.15f;
                }

                ReferenceX = 0.5f + baseRadius * (float)Math.Cos(_referenceAngle);
                ReferenceY = 0.5f + baseRadius * (float)Math.Sin(_referenceAngle);
            }
        }

        private void ApplyDistanceModification(ImageBuffer source, ImageBuffer output, AudioFeatures? audioFeatures)
        {
            int width = source.Width;
            int height = source.Height;

            // Get reference point in pixel coordinates
            float refX = ReferenceX * width;
            float refY = ReferenceY * height;

            // Calculate effective parameters
            float effectiveOpacity = CalculateEffectiveOpacity();
            float maxDistance = CalculateMaxDistance(width, height, refX, refY);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate distance from reference point
                    float distance = CalculateDistance(x, y, refX, refY);
                    float normalizedDistance = distance / maxDistance;

                    // Apply distance constraints
                    if (distance > DistanceRange)
                    {
                        output.Data[y * width + x] = source.Data[y * width + x];
                        continue;
                    }

                    // Apply inversion if enabled
                    if (InvertDistance)
                    {
                        normalizedDistance = 1.0f - normalizedDistance;
                    }

                    // Apply distance effect pattern
                    float effectStrength = ApplyDistanceEffect(normalizedDistance);

                    // Apply modification
                    uint sourcePixel = source.Data[y * width + x];
                    uint modifiedPixel = ApplyModification(sourcePixel, effectStrength, effectiveOpacity, normalizedDistance);
                    output.Data[y * width + x] = modifiedPixel;
                }
            }
        }

        private float CalculateEffectiveOpacity()
        {
            float opacity = ModificationOpacity;

            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                opacity *= (1.0f + (BeatMultiplier - 1.0f) * beatFactor);
            }

            return Math.Max(0.0f, Math.Min(1.0f, opacity));
        }

        private float CalculateDistance(int x, int y, float refX, float refY)
        {
            float dx = x - refX;
            float dy = y - refY;

            switch (DistanceType)
            {
                case 0: // Euclidean
                    return (float)Math.Sqrt(dx * dx + dy * dy);

                case 1: // Manhattan
                    return Math.Abs(dx) + Math.Abs(dy);

                case 2: // Chebyshev
                    return Math.Max(Math.Abs(dx), Math.Abs(dy));

                case 3: // Minkowski
                    return (float)Math.Pow(Math.Pow(Math.Abs(dx), MinkowskiP) + Math.Pow(Math.Abs(dy), MinkowskiP), 1.0 / MinkowskiP);

                case 4: // Custom formula using parameters
                    float weightedDx = dx * Parameter1;
                    float weightedDy = dy * Parameter2;
                    return (float)Math.Sqrt(weightedDx * weightedDx + weightedDy * weightedDy) * Parameter3;

                default:
                    return (float)Math.Sqrt(dx * dx + dy * dy);
            }
        }

        private float CalculateMaxDistance(int width, int height, float refX, float refY)
        {
            // Calculate maximum possible distance from reference point to any corner
            float[] cornerDistances = {
                CalculateDistance(0, 0, refX, refY),
                CalculateDistance(width - 1, 0, refX, refY),
                CalculateDistance(0, height - 1, refX, refY),
                CalculateDistance(width - 1, height - 1, refX, refY)
            };

            float maxDist = 0;
            foreach (float dist in cornerDistances)
            {
                if (dist > maxDist) maxDist = dist;
            }

            return maxDist;
        }

        private float ApplyDistanceEffect(float normalizedDistance)
        {
            switch (DistanceEffect)
            {
                case 0: // Linear
                    return normalizedDistance;

                case 1: // Quadratic
                    return normalizedDistance * normalizedDistance;

                case 2: // Cubic
                    return normalizedDistance * normalizedDistance * normalizedDistance;

                case 3: // Sine wave
                    return (float)(0.5 * (1 + Math.Sin(normalizedDistance * Math.PI * Parameter1 - Math.PI / 2)));

                case 4: // Pulse
                    float pulseFreq = Parameter1;
                    return (float)(Math.Sin(normalizedDistance * Math.PI * pulseFreq) > 0 ? 1.0f : 0.0f);

                case 5: // Random
                    return (float)(_random.NextDouble() * normalizedDistance);

                default:
                    return normalizedDistance;
            }
        }

        private uint ApplyModification(uint sourcePixel, float effectStrength, float opacity, float distance)
        {
            uint a = (sourcePixel >> 24) & 0xFF;
            uint r = (sourcePixel >> 16) & 0xFF;
            uint g = (sourcePixel >> 8) & 0xFF;
            uint b = sourcePixel & 0xFF;

            switch (ModificationType)
            {
                case 0: // Color intensity
                    float intensityFactor = 1.0f + (effectStrength - 0.5f) * opacity * 2.0f;
                    r = (uint)Math.Max(0, Math.Min(255, r * intensityFactor));
                    g = (uint)Math.Max(0, Math.Min(255, g * intensityFactor));
                    b = (uint)Math.Max(0, Math.Min(255, b * intensityFactor));
                    break;

                case 1: // Hue shift
                    float hueShift = effectStrength * opacity * Parameter1 * 360.0f;
                    (r, g, b) = ApplyHueShift(r, g, b, hueShift);
                    break;

                case 2: // Saturation
                    float saturation = 1.0f + (effectStrength - 0.5f) * opacity * Parameter1;
                    (r, g, b) = ApplySaturation(r, g, b, saturation);
                    break;

                case 3: // Brightness
                    float brightness = effectStrength * opacity * Parameter1;
                    r = (uint)Math.Max(0, Math.Min(255, r + brightness * 128));
                    g = (uint)Math.Max(0, Math.Min(255, g + brightness * 128));
                    b = (uint)Math.Max(0, Math.Min(255, b + brightness * 128));
                    break;

                case 4: // Displacement (color shifting)
                    int displacement = (int)(effectStrength * opacity * Parameter1 * 128);
                    r = (uint)((r + displacement) % 256);
                    g = (uint)((g + displacement) % 256);
                    b = (uint)((b + displacement) % 256);
                    break;

                case 5: // Blur radius effect (simple blur approximation)
                    if (effectStrength * opacity > 0.1f)
                    {
                        // Simple blur by averaging with neighboring values
                        float blurAmount = effectStrength * opacity * Parameter1;
                        uint avgValue = (r + g + b) / 3;
                        r = (uint)(r * (1.0f - blurAmount) + avgValue * blurAmount);
                        g = (uint)(g * (1.0f - blurAmount) + avgValue * blurAmount);
                        b = (uint)(b * (1.0f - blurAmount) + avgValue * blurAmount);
                    }
                    break;
            }

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private (uint r, uint g, uint b) ApplyHueShift(uint r, uint g, uint b, float hueShift)
        {
            // Simple hue shift approximation
            float shift = (hueShift % 360.0f) / 360.0f;
            
            if (shift < 0.33f)
                return (g, b, r); // RGB -> GBR
            else if (shift < 0.66f)
                return (b, r, g); // RGB -> BRG
            else
                return (r, g, b); // RGB -> RGB
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

        #endregion
    }
}