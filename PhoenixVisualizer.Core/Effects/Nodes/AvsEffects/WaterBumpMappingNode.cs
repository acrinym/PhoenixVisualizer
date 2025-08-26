using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Water Bump Mapping effect
    /// Creates water-like surface with bump mapping and ripple effects
    /// </summary>
    public class WaterBumpMappingNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public float WaveHeight { get; set; } = 10.0f;
        public float WaveSpeed { get; set; } = 1.0f;
        public float WaveFrequency { get; set; } = 0.1f;
        public bool BeatReactive { get; set; } = false;
        public float BeatWaveBoost { get; set; } = 2.0f;
        public int RippleCount { get; set; } = 3;
        public float RippleSpeed { get; set; } = 0.5f;
        public float Refraction { get; set; } = 0.02f;
        public bool ReflectionEffect { get; set; } = true;

        private float _time = 0.0f;
        private int _beatCounter = 0;
        private readonly Random _random = new Random();

        public WaterBumpMappingNode()
        {
            Name = "Water Bump Mapping";
            Description = "Water-like surface with bump mapping and ripples";
            Category = "Transform Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Water effect output"));
        }

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                var sourceImage = GetInputValue<ImageBuffer>("Image", inputData);
                if (sourceImage?.Data == null) return;

                var outputImage = new ImageBuffer(sourceImage.Width, sourceImage.Height);

                if (BeatReactive && audioFeatures.Beat)
                    _beatCounter = 20;
                else if (_beatCounter > 0)
                    _beatCounter--;

                UpdateWater();
                ApplyWaterEffect(sourceImage, outputImage);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Water Bump Mapping] Error: {ex.Message}");
            }
        }

        private void UpdateWater()
        {
            float effectiveSpeed = WaveSpeed;
            if (BeatReactive && _beatCounter > 0)
                effectiveSpeed *= (1.0f + (BeatWaveBoost - 1.0f) * (_beatCounter / 20.0f));

            _time += effectiveSpeed * 0.016f;
        }

        private void ApplyWaterEffect(ImageBuffer source, ImageBuffer output)
        {
            int width = source.Width;
            int height = source.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate water displacement
                    float displacement = CalculateWaterDisplacement(x, y, width, height);
                    
                    // Apply refraction
                    int srcX = (int)(x + displacement * Refraction * width);
                    int srcY = (int)(y + displacement * Refraction * height * 0.5f);
                    
                    // Clamp coordinates
                    srcX = Math.Max(0, Math.Min(width - 1, srcX));
                    srcY = Math.Max(0, Math.Min(height - 1, srcY));
                    
                    uint sourcePixel = source.Data[srcY * width + srcX];
                    
                    // Apply water tint and reflection
                    if (ReflectionEffect)
                    {
                        sourcePixel = ApplyWaterTint(sourcePixel, displacement);
                    }
                    
                    output.Data[y * width + x] = sourcePixel;
                }
            }
        }

        private float CalculateWaterDisplacement(int x, int y, int width, int height)
        {
            float normalizedX = x / (float)width;
            float normalizedY = y / (float)height;
            
            // Primary wave
            float wave1 = (float)Math.Sin((normalizedX * WaveFrequency + _time) * Math.PI * 2) * WaveHeight;
            float wave2 = (float)Math.Sin((normalizedY * WaveFrequency * 1.3f + _time * 1.1f) * Math.PI * 2) * WaveHeight * 0.7f;
            
            // Add ripples
            float ripples = 0;
            for (int i = 0; i < RippleCount; i++)
            {
                float rippleX = 0.3f + (i * 0.2f);
                float rippleY = 0.3f + (i * 0.15f);
                float distance = (float)Math.Sqrt((normalizedX - rippleX) * (normalizedX - rippleX) + 
                                                (normalizedY - rippleY) * (normalizedY - rippleY));
                float ripple = (float)Math.Sin((distance * 10 - _time * RippleSpeed * (i + 1)) * Math.PI * 2) * 
                              WaveHeight * 0.3f * (float)Math.Exp(-distance * 3);
                ripples += ripple;
            }
            
            return (wave1 + wave2 + ripples) / 100.0f;
        }

        private uint ApplyWaterTint(uint pixel, float displacement)
        {
            uint a = (pixel >> 24) & 0xFF;
            uint r = (pixel >> 16) & 0xFF;
            uint g = (pixel >> 8) & 0xFF;
            uint b = pixel & 0xFF;
            
            // Apply blue tint based on displacement
            float tintFactor = Math.Abs(displacement) * 0.2f + 0.05f;
            r = (uint)(r * (1.0f - tintFactor * 0.3f));
            g = (uint)(g * (1.0f - tintFactor * 0.1f));
            b = (uint)Math.Min(255, b * (1.0f + tintFactor * 0.2f));
            
            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "WaveHeight", WaveHeight },
                { "WaveSpeed", WaveSpeed },
                { "WaveFrequency", WaveFrequency },
                { "BeatReactive", BeatReactive },
                { "BeatWaveBoost", BeatWaveBoost },
                { "RippleCount", RippleCount },
                { "RippleSpeed", RippleSpeed },
                { "Refraction", Refraction },
                { "ReflectionEffect", ReflectionEffect }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            if (config.TryGetValue("WaveHeight", out var waveHeight))
                WaveHeight = Convert.ToSingle(waveHeight);
            if (config.TryGetValue("WaveSpeed", out var waveSpeed))
                WaveSpeed = Convert.ToSingle(waveSpeed);
            if (config.TryGetValue("WaveFrequency", out var waveFreq))
                WaveFrequency = Convert.ToSingle(waveFreq);
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            if (config.TryGetValue("BeatWaveBoost", out var beatBoost))
                BeatWaveBoost = Convert.ToSingle(beatBoost);
            if (config.TryGetValue("RippleCount", out var rippleCount))
                RippleCount = Convert.ToInt32(rippleCount);
            if (config.TryGetValue("RippleSpeed", out var rippleSpeed))
                RippleSpeed = Convert.ToSingle(rippleSpeed);
            if (config.TryGetValue("Refraction", out var refraction))
                Refraction = Convert.ToSingle(refraction);
            if (config.TryGetValue("ReflectionEffect", out var reflection))
                ReflectionEffect = Convert.ToBoolean(reflection);
        }
    }
}