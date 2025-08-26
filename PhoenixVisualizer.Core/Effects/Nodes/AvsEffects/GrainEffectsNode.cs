using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class GrainEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Grain effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Blending mode: 0=Replace, 1=Additive, 2=50/50
        /// </summary>
        public int BlendMode { get; set; } = 1;

        /// <summary>
        /// Grain intensity (0.0 to 100.0)
        /// </summary>
        public float GrainIntensity { get; set; } = 50.0f;

        /// <summary>
        /// Use consistent grain pattern across frames
        /// </summary>
        public bool StaticGrain { get; set; } = false;

        /// <summary>
        /// Enable beat-reactive grain intensity
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat intensity multiplier
        /// </summary>
        public float BeatIntensityMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// Seed for random grain generation
        /// </summary>
        public int GrainSeed { get; set; } = 0;

        /// <summary>
        /// Enable grain pattern animation
        /// </summary>
        public bool EnableGrainAnimation { get; set; } = false;

        /// <summary>
        /// Speed of grain animation
        /// </summary>
        public float GrainAnimationSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Animation mode: 0=Pulsing, 1=Wave, 2=Random Walk, 3=Directional
        /// </summary>
        public int GrainAnimationMode { get; set; } = 0;

        /// <summary>
        /// Enable grain masking with image
        /// </summary>
        public bool EnableGrainMasking { get; set; } = false;

        /// <summary>
        /// Grain mask image buffer
        /// </summary>
        public ImageBuffer? GrainMask { get; set; } = null;

        /// <summary>
        /// Influence of mask on grain application
        /// </summary>
        public float MaskInfluence { get; set; } = 1.0f;

        /// <summary>
        /// Enable advanced grain blending
        /// </summary>
        public bool EnableGrainBlending { get; set; } = false;

        /// <summary>
        /// Strength of grain blending
        /// </summary>
        public float GrainBlendStrength { get; set; } = 0.5f;

        /// <summary>
        /// Grain pattern type: 0=Random, 1=Perlin, 2=Simplex, 3=Cellular, 4=Fractal
        /// </summary>
        public int GrainPatternType { get; set; } = 0;

        /// <summary>
        /// Scale factor for grain patterns
        /// </summary>
        public float GrainScale { get; set; } = 1.0f;

        /// <summary>
        /// Enable colored grain effects
        /// </summary>
        public bool EnableGrainColorization { get; set; } = false;

        /// <summary>
        /// Color for grain (RGB)
        /// </summary>
        public int GrainColor { get; set; } = 0xFFFFFF;

        /// <summary>
        /// Intensity of grain colorization
        /// </summary>
        public float ColorIntensity { get; set; } = 0.3f;

        /// <summary>
        /// Enable directional grain patterns
        /// </summary>
        public bool EnableGrainDirectional { get; set; } = false;

        /// <summary>
        /// X direction for grain movement
        /// </summary>
        public float GrainDirectionX { get; set; } = 0.0f;

        /// <summary>
        /// Y direction for grain movement
        /// </summary>
        public float GrainDirectionY { get; set; } = 0.0f;

        /// <summary>
        /// Enable temporal grain evolution
        /// </summary>
        public bool EnableGrainTemporal { get; set; } = false;

        /// <summary>
        /// Speed of temporal evolution
        /// </summary>
        public float TemporalSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Temporal mode: 0=Linear, 1=Cyclic, 2=Chaotic
        /// </summary>
        public int TemporalMode { get; set; } = 0;

        #endregion

        #region Private Fields

        private ImageBuffer? _grainBuffer;
        private Random? _random;
        private float _currentTime = 0.0f;
        private int _lastWidth = 0;
        private int _lastHeight = 0;
        // removed unused field randtabPos (not used in final implementation)
        private readonly byte[] _randtab = new byte[491];

        #endregion

        #region Constructor

        public GrainEffectsNode()
        {
            Name = "Grain Effects";
            Description = "Adds film grain and noise effects with configurable blending modes";
            Category = "AVS Effects";
            
            // Initialize random table
            _random = new Random();
            InitializeRandomTable();
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null, "Input image buffer"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable effect"));
            _inputPorts.Add(new EffectPort("BlendMode", typeof(int), false, 1, "Blending mode (0=Replace, 1=Additive, 2=50/50)"));
            _inputPorts.Add(new EffectPort("GrainIntensity", typeof(float), false, 50.0f, "Grain intensity (0.0-100.0)"));
            _inputPorts.Add(new EffectPort("StaticGrain", typeof(bool), false, false, "Use consistent grain pattern"));
            _inputPorts.Add(new EffectPort("BeatReactive", typeof(bool), false, false, "Enable beat-reactive behavior"));
            _inputPorts.Add(new EffectPort("BeatIntensityMultiplier", typeof(float), false, 1.5f, "Beat intensity multiplier"));
            _inputPorts.Add(new EffectPort("GrainSeed", typeof(int), false, 0, "Random seed for grain generation"));
            _inputPorts.Add(new EffectPort("EnableGrainAnimation", typeof(bool), false, false, "Enable grain animation"));
            _inputPorts.Add(new EffectPort("GrainAnimationSpeed", typeof(float), false, 1.0f, "Animation speed"));
            _inputPorts.Add(new EffectPort("GrainAnimationMode", typeof(int), false, 0, "Animation mode"));
            _inputPorts.Add(new EffectPort("EnableGrainMasking", typeof(bool), false, false, "Enable grain masking"));
            _inputPorts.Add(new EffectPort("GrainMask", typeof(ImageBuffer), false, null, "Grain mask image"));
            _inputPorts.Add(new EffectPort("MaskInfluence", typeof(float), false, 1.0f, "Mask influence"));
            _inputPorts.Add(new EffectPort("EnableGrainBlending", typeof(bool), false, false, "Enable advanced blending"));
            _inputPorts.Add(new EffectPort("GrainBlendStrength", typeof(float), false, 0.5f, "Blend strength"));
            _inputPorts.Add(new EffectPort("GrainPatternType", typeof(int), false, 0, "Pattern type"));
            _inputPorts.Add(new EffectPort("GrainScale", typeof(float), false, 1.0f, "Grain scale factor"));
            _inputPorts.Add(new EffectPort("EnableGrainColorization", typeof(bool), false, false, "Enable colored grain"));
            _inputPorts.Add(new EffectPort("GrainColor", typeof(int), false, 0xFFFFFF, "Grain color"));
            _inputPorts.Add(new EffectPort("ColorIntensity", typeof(float), false, 0.3f, "Color intensity"));
            _inputPorts.Add(new EffectPort("EnableGrainDirectional", typeof(bool), false, false, "Enable directional grain"));
            _inputPorts.Add(new EffectPort("GrainDirectionX", typeof(float), false, 0.0f, "X direction"));
            _inputPorts.Add(new EffectPort("GrainDirectionY", typeof(float), false, 0.0f, "Y direction"));
            _inputPorts.Add(new EffectPort("EnableGrainTemporal", typeof(bool), false, false, "Enable temporal evolution"));
            _inputPorts.Add(new EffectPort("TemporalSpeed", typeof(float), false, 1.0f, "Temporal speed"));
            _inputPorts.Add(new EffectPort("TemporalMode", typeof(int), false, 0, "Temporal mode"));
            
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), true, null, "Processed image with grain"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) return inputs["Input"];

            var input = inputs["Input"] as ImageBuffer;
            if (input == null) return inputs["Input"];

            // Initialize grain buffer if needed
            if (_grainBuffer == null || _grainBuffer.Width != input.Width || _grainBuffer.Height != input.Height)
            {
                InitializeGrainBuffer(input.Width, input.Height);
            }

            // Update grain pattern
            UpdateGrainPattern(audioFeatures);

            // Get current grain intensity
            var currentIntensity = GetCurrentGrainIntensity(audioFeatures);

            // Create output buffer
            var output = new ImageBuffer(input.Width, input.Height);

            // Process each pixel
            for (int i = 0; i < input.Pixels.Length; i++)
            {
                var originalColor = input.Pixels[i];
                var grainColor = GetGrainColor(i, currentIntensity);
                var processedColor = ApplyGrainBlending(originalColor, grainColor);

                // Apply grain masking if enabled
                if (EnableGrainMasking && GrainMask != null)
                {
                    processedColor = ApplyGrainMasking(originalColor, processedColor, i);
                }

                output.Pixels[i] = processedColor;
            }

            return output;
        }

        #endregion

        #region Private Methods

        private void InitializeRandomTable()
        {
            var tempRandom = new Random(GrainSeed);
            for (int i = 0; i < _randtab.Length; i++)
            {
                _randtab[i] = (byte)tempRandom.Next(0, 256);
            }
            // removed unused field assignment
        }

        private void InitializeGrainBuffer(int width, int height)
        {
            _grainBuffer = new ImageBuffer(width, height);
            _random = new Random(GrainSeed);
            _lastWidth = width;
            _lastHeight = height;

            for (int i = 0; i < _grainBuffer.Pixels.Length; i++)
            {
                var intensity = _random.Next(0, 256);
                var threshold = _random.Next(0, 101);
                _grainBuffer.Pixels[i] = (threshold << 8) | intensity;
            }
        }

        private void UpdateGrainPattern(AudioFeatures audioFeatures)
        {
            if (StaticGrain) return;

            // Update time
            _currentTime += 0.016f; // Assume 60 FPS

            // Update animation
            if (EnableGrainAnimation)
            {
                UpdateGrainAnimation();
            }

            // Update temporal evolution
            if (EnableGrainTemporal)
            {
                UpdateTemporalGrain();
            }

            // Update directional grain
            if (EnableGrainDirectional)
            {
                UpdateDirectionalGrain();
            }

            // Update random grain pattern
            var random = new Random(GrainSeed + (int)(_currentTime * 1000));
            for (int i = 0; i < _grainBuffer.Pixels.Length; i++)
            {
                if (random.Next(0, 100) < 10) // 10% chance to update each pixel
                {
                    var intensity = random.Next(0, 256);
                    var threshold = random.Next(0, 101);
                    _grainBuffer.Pixels[i] = (threshold << 8) | intensity;
                }
            }
        }

        private void UpdateGrainAnimation()
        {
            var animationProgress = (_currentTime * GrainAnimationSpeed) % (float)(Math.PI * 2);

            switch (GrainAnimationMode)
            {
                case 0: // Pulsing
                    var pulse = (float)((Math.Sin(animationProgress) + 1.0) * 0.5);
                    GrainIntensity = 20.0f + pulse * 60.0f;
                    break;

                case 1: // Wave pattern
                    var wave = (float)Math.Sin(animationProgress * 3);
                    GrainIntensity = 40.0f + wave * 30.0f;
                    break;

                case 2: // Random walk
                    if (_random.NextDouble() < 0.01f) // 1% chance per frame
                    {
                        GrainIntensity = _random.Next(20, 80);
                    }
                    break;

                case 3: // Directional movement
                    var directionX = (float)Math.Sin(animationProgress) * GrainDirectionX;
                    var directionY = (float)Math.Cos(animationProgress) * GrainDirectionY;
                    UpdateDirectionalGrain(directionX, directionY);
                    break;
            }
        }

        private void UpdateDirectionalGrain()
        {
            if (!EnableGrainDirectional) return;

            var width = _grainBuffer.Width;
            var height = _grainBuffer.Height;
            var tempBuffer = new int[_grainBuffer.Pixels.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var offsetX = (int)(GrainDirectionX * GrainScale);
                    var offsetY = (int)(GrainDirectionY * GrainScale);

                    var sourceX = (x + offsetX + width) % width;
                    var sourceY = (y + offsetY + height) % height;

                    var sourceIndex = sourceY * width + sourceX;
                    var targetIndex = y * width + x;

                    if (sourceIndex >= 0 && sourceIndex < _grainBuffer.Pixels.Length)
                    {
                        tempBuffer[targetIndex] = _grainBuffer.Pixels[sourceIndex];
                    }
                }
            }

            Array.Copy(tempBuffer, _grainBuffer.Pixels, tempBuffer.Length);
        }

        private void UpdateDirectionalGrain(float directionX, float directionY)
        {
            if (!EnableGrainDirectional) return;

            var width = _grainBuffer.Width;
            var height = _grainBuffer.Height;
            var tempBuffer = new int[_grainBuffer.Pixels.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var offsetX = (int)(directionX * GrainScale);
                    var offsetY = (int)(directionY * GrainScale);

                    var sourceX = (x + offsetX + width) % width;
                    var sourceY = (y + offsetY + height) % height;

                    var sourceIndex = sourceY * width + sourceX;
                    var targetIndex = y * width + x;

                    if (sourceIndex >= 0 && sourceIndex < _grainBuffer.Pixels.Length)
                    {
                        tempBuffer[targetIndex] = _grainBuffer.Pixels[sourceIndex];
                    }
                }
            }

            Array.Copy(tempBuffer, _grainBuffer.Pixels, tempBuffer.Length);
        }

        private void UpdateTemporalGrain()
        {
            if (!EnableGrainTemporal) return;

            var time = _currentTime * TemporalSpeed;

            switch (TemporalMode)
            {
                case 0: // Linear evolution
                    var evolution = (time % 100.0f) / 100.0f;
                    UpdateGrainEvolution(evolution);
                    break;

                case 1: // Cyclic evolution
                    var cycle = (float)((Math.Sin(time * 0.1f) + 1.0) * 0.5);
                    UpdateGrainEvolution(cycle);
                    break;

                case 2: // Chaotic evolution
                    var chaos = (float)((Math.Sin(time * 0.05f) * Math.Cos(time * 0.03f) + 1.0) * 0.5);
                    UpdateGrainEvolution(chaos);
                    break;
            }
        }

        private void UpdateGrainEvolution(float evolution)
        {
            var random = new Random((int)(evolution * 10000));

            for (int i = 0; i < _grainBuffer.Pixels.Length; i++)
            {
                if (random.NextDouble() < evolution * 0.1f)
                {
                    var intensity = random.Next(0, 256);
                    var threshold = random.Next(0, 101);
                    _grainBuffer.Pixels[i] = (threshold << 8) | intensity;
                }
            }
        }

        private float GetCurrentGrainIntensity(AudioFeatures audioFeatures)
        {
            if (!BeatReactive || audioFeatures == null)
                return GrainIntensity;

            var beatMultiplier = 1.0f;

            if (audioFeatures.IsBeat)
            {
                beatMultiplier = BeatIntensityMultiplier;
            }
            else
            {
                // Gradual return to normal
                beatMultiplier = 1.0f + (BeatIntensityMultiplier - 1.0f) * (audioFeatures.Rms > 0.1f ? audioFeatures.Rms : 0.0f);
            }

            return Math.Max(0.0f, Math.Min(100.0f, GrainIntensity * beatMultiplier));
        }

        private int GetGrainColor(int pixelIndex, float intensity)
        {
            if (pixelIndex >= _grainBuffer.Pixels.Length) return 0;

            var grainData = _grainBuffer.Pixels[pixelIndex];
            var grainIntensity = grainData & 0xFF;
            var grainThreshold = (grainData >> 8) & 0xFF;

            var intensityThreshold = (int)((intensity * 255) / 100.0f);

            if (grainThreshold > intensityThreshold)
                return 0; // No grain for this pixel

            if (EnableGrainColorization)
            {
                return GenerateColoredGrain(grainIntensity);
            }

            return (grainIntensity << 16) | (grainIntensity << 8) | grainIntensity;
        }

        private int GenerateColoredGrain(int intensity)
        {
            var r = (GrainColor >> 16) & 0xFF;
            var g = (GrainColor >> 8) & 0xFF;
            var b = GrainColor & 0xFF;

            var grainR = (int)(r * intensity * ColorIntensity / 255.0f);
            var grainG = (int)(g * intensity * ColorIntensity / 255.0f);
            var grainB = (int)(b * intensity * ColorIntensity / 255.0f);

            return (grainB << 16) | (grainG << 8) | grainR;
        }

        private int ApplyGrainBlending(int originalColor, int grainColor)
        {
            switch (BlendMode)
            {
                case 0: // Replace
                    return grainColor;

                case 1: // Additive
                    return BlendAdditive(originalColor, grainColor);

                case 2: // 50/50
                    return BlendFiftyFifty(originalColor, grainColor);

                default:
                    return originalColor;
            }
        }

        private int BlendAdditive(int color1, int color2)
        {
            var r1 = color1 & 0xFF;
            var g1 = (color1 >> 8) & 0xFF;
            var b1 = (color1 >> 16) & 0xFF;

            var r2 = color2 & 0xFF;
            var g2 = (color2 >> 8) & 0xFF;
            var b2 = (color2 >> 16) & 0xFF;

            var r = Math.Min(255, r1 + r2);
            var g = Math.Min(255, g1 + g2);
            var b = Math.Min(255, b1 + b2);

            return (b << 16) | (g << 8) | r;
        }

        private int BlendFiftyFifty(int color1, int color2)
        {
            var r1 = color1 & 0xFF;
            var g1 = (color1 >> 8) & 0xFF;
            var b1 = (color1 >> 16) & 0xFF;

            var r2 = color2 & 0xFF;
            var g2 = (color2 >> 8) & 0xFF;
            var b2 = (color2 >> 16) & 0xFF;

            var r = (r1 + r2) / 2;
            var g = (g1 + g2) / 2;
            var b = (b1 + b2) / 2;

            return (b << 16) | (g << 8) | r;
        }

        private int ApplyGrainMasking(int originalColor, int grainedColor, int pixelIndex)
        {
            if (!EnableGrainMasking || GrainMask == null || pixelIndex >= GrainMask.Pixels.Length)
                return grainedColor;

            var maskPixel = GrainMask.Pixels[pixelIndex];
            var maskIntensity = (maskPixel & 0xFF) / 255.0f; // Use red channel as mask

            // Blend original and grained based on mask
            var blendFactor = maskIntensity * MaskInfluence;
            var finalColor = BlendColors(originalColor, grainedColor, blendFactor);

            return finalColor;
        }

        private int BlendColors(int color1, int color2, float blendFactor)
        {
            var r1 = color1 & 0xFF;
            var g1 = (color1 >> 8) & 0xFF;
            var b1 = (color1 >> 16) & 0xFF;

            var r2 = color2 & 0xFF;
            var g2 = (color2 >> 8) & 0xFF;
            var b2 = (color2 >> 16) & 0xFF;

            var r = (int)(r1 * (1 - blendFactor) + r2 * blendFactor);
            var g = (int)(g1 * (1 - blendFactor) + g2 * blendFactor);
            var b = (int)(b1 * (1 - blendFactor) + b2 * blendFactor);

            return (b << 16) | (g << 8) | r;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        public override bool ValidateConfiguration()
        {
            if (GrainIntensity < 0.0f || GrainIntensity > 100.0f)
                GrainIntensity = 50.0f;

            if (BeatIntensityMultiplier < 0.1f || BeatIntensityMultiplier > 5.0f)
                BeatIntensityMultiplier = 1.5f;

            if (ColorIntensity < 0.0f || ColorIntensity > 1.0f)
                ColorIntensity = 0.3f;

            if (MaskInfluence < 0.0f || MaskInfluence > 2.0f)
                MaskInfluence = 1.0f;

            return true;
        }

        /// <summary>
        /// Returns a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            return $"Grain Effects: {(Enabled ? "Enabled" : "Disabled")}, " +
                   $"Mode: {BlendMode}, Intensity: {GrainIntensity:F1}, " +
                   $"Static: {(StaticGrain ? "On" : "Off")}, " +
                   $"Beat: {(BeatReactive ? "On" : "Off")}, " +
                   $"Animation: {(EnableGrainAnimation ? "On" : "Off")}";
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
