using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Dynamic Shift Effects with advanced shifting algorithms
    /// Advanced shifting effects beyond basic ShiftEffects
    /// </summary>
    public class DynamicShiftEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public int ShiftType { get; set; } = 0; // 0=Horizontal, 1=Vertical, 2=Radial, 3=Wave, 4=Twist
        public float ShiftAmount { get; set; } = 10.0f;
        public float ShiftSpeed { get; set; } = 1.0f;
        public bool BeatReactive { get; set; } = false;
        public float BeatAmplifier { get; set; } = 2.0f;
        public int ShiftPattern { get; set; } = 0; // 0=Linear, 1=Sine, 2=Sawtooth, 3=Random
        public float PatternFrequency { get; set; } = 1.0f;
        public float PatternPhase { get; set; } = 0.0f;
        public bool SmoothShifting { get; set; } = true;
        public float EdgeHandling { get; set; } = 0.0f; // 0=Wrap, 1=Clamp, 2=Mirror

        private float _currentTime = 0.0f;
        private int _beatCounter = 0;
        private readonly Random _random = new Random();
        private const int BEAT_DURATION = 25;

        public DynamicShiftEffectsNode()
        {
            Name = "Dynamic Shift Effects";
            Description = "Advanced shifting effects with dynamic patterns";
            Category = "Transform Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Shifted output"));
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
                {
                    _beatCounter = BEAT_DURATION;
                }
                else if (_beatCounter > 0)
                {
                    _beatCounter--;
                }

                UpdateShiftParameters();
                ApplyDynamicShift(sourceImage, outputImage);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dynamic Shift Effects] Error: {ex.Message}");
            }
        }

        private void UpdateShiftParameters()
        {
            float effectiveSpeed = ShiftSpeed;
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                effectiveSpeed *= (1.0f + (BeatAmplifier - 1.0f) * beatFactor);
            }

            _currentTime += effectiveSpeed * 0.016f;
        }

        private void ApplyDynamicShift(ImageBuffer source, ImageBuffer output)
        {
            int width = source.Width;
            int height = source.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float shiftX = 0, shiftY = 0;
                    CalculateShiftOffset(x, y, width, height, out shiftX, out shiftY);

                    int srcX = GetSourceCoordinate(x - (int)shiftX, width);
                    int srcY = GetSourceCoordinate(y - (int)shiftY, height);

                    if (SmoothShifting)
                    {
                        // Bilinear interpolation for smooth shifting
                        float fracX = shiftX - (int)shiftX;
                        float fracY = shiftY - (int)shiftY;
                        output.Data[y * width + x] = InterpolatePixel(source, srcX, srcY, fracX, fracY, width, height);
                    }
                    else
                    {
                        output.Data[y * width + x] = source.Data[srcY * width + srcX];
                    }
                }
            }
        }

        private void CalculateShiftOffset(int x, int y, int width, int height, out float shiftX, out float shiftY)
        {
            float normalizedX = x / (float)width;
            float normalizedY = y / (float)height;
            float time = _currentTime * PatternFrequency + PatternPhase;

            switch (ShiftType)
            {
                case 0: // Horizontal
                    shiftX = GetPatternValue(time + normalizedY) * ShiftAmount;
                    shiftY = 0;
                    break;

                case 1: // Vertical
                    shiftX = 0;
                    shiftY = GetPatternValue(time + normalizedX) * ShiftAmount;
                    break;

                case 2: // Radial
                    float centerX = width * 0.5f;
                    float centerY = height * 0.5f;
                    float distance = (float)Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    float angle = (float)Math.Atan2(y - centerY, x - centerX);
                    float radialShift = GetPatternValue(time + distance / width) * ShiftAmount;
                    shiftX = radialShift * (float)Math.Cos(angle);
                    shiftY = radialShift * (float)Math.Sin(angle);
                    break;

                case 3: // Wave
                    shiftX = GetPatternValue(time + normalizedY * 2.0f) * ShiftAmount;
                    shiftY = GetPatternValue(time + normalizedX * 2.0f + 1.57f) * ShiftAmount; // 90 degree phase shift
                    break;

                case 4: // Twist
                    float twistCenterX = width * 0.5f;
                    float twistCenterY = height * 0.5f;
                    float twistDistance = (float)Math.Sqrt((x - twistCenterX) * (x - twistCenterX) + (y - twistCenterY) * (y - twistCenterY));
                    float twistAngle = GetPatternValue(time + twistDistance / width) * ShiftAmount * 0.1f;
                    float cosAngle = (float)Math.Cos(twistAngle);
                    float sinAngle = (float)Math.Sin(twistAngle);
                    float relX = x - twistCenterX;
                    float relY = y - twistCenterY;
                    shiftX = relX * cosAngle - relY * sinAngle - relX;
                    shiftY = relX * sinAngle + relY * cosAngle - relY;
                    break;

                default:
                    shiftX = shiftY = 0;
                    break;
            }
        }

        private float GetPatternValue(float input)
        {
            switch (ShiftPattern)
            {
                case 0: // Linear
                    return input % 1.0f;

                case 1: // Sine
                    return (float)Math.Sin(input * Math.PI * 2);

                case 2: // Sawtooth
                    float saw = input % 1.0f;
                    return saw < 0.5f ? saw * 2.0f : (1.0f - saw) * 2.0f;

                case 3: // Random
                    return (float)(_random.NextDouble() * 2.0 - 1.0);

                default:
                    return 0.0f;
            }
        }

        private int GetSourceCoordinate(int coord, int size)
        {
            switch ((int)EdgeHandling)
            {
                case 0: // Wrap
                    return ((coord % size) + size) % size;

                case 1: // Clamp
                    return Math.Max(0, Math.Min(size - 1, coord));

                case 2: // Mirror
                    if (coord < 0)
                        return Math.Abs(coord) % size;
                    else if (coord >= size)
                        return size - 1 - ((coord - size) % size);
                    else
                        return coord;

                default:
                    return Math.Max(0, Math.Min(size - 1, coord));
            }
        }

        private uint InterpolatePixel(ImageBuffer source, int x, int y, float fracX, float fracY, int width, int height)
        {
            int x1 = GetSourceCoordinate(x, width);
            int y1 = GetSourceCoordinate(y, height);
            int x2 = GetSourceCoordinate(x + 1, width);
            int y2 = GetSourceCoordinate(y + 1, height);

            uint p1 = source.Data[y1 * width + x1];
            uint p2 = source.Data[y1 * width + x2];
            uint p3 = source.Data[y2 * width + x1];
            uint p4 = source.Data[y2 * width + x2];

            // Interpolate each channel
            uint a = InterpolateChannel((p1 >> 24) & 0xFF, (p2 >> 24) & 0xFF, (p3 >> 24) & 0xFF, (p4 >> 24) & 0xFF, fracX, fracY);
            uint r = InterpolateChannel((p1 >> 16) & 0xFF, (p2 >> 16) & 0xFF, (p3 >> 16) & 0xFF, (p4 >> 16) & 0xFF, fracX, fracY);
            uint g = InterpolateChannel((p1 >> 8) & 0xFF, (p2 >> 8) & 0xFF, (p3 >> 8) & 0xFF, (p4 >> 8) & 0xFF, fracX, fracY);
            uint b = InterpolateChannel(p1 & 0xFF, p2 & 0xFF, p3 & 0xFF, p4 & 0xFF, fracX, fracY);

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private uint InterpolateChannel(uint c1, uint c2, uint c3, uint c4, float fracX, float fracY)
        {
            float top = c1 * (1.0f - fracX) + c2 * fracX;
            float bottom = c3 * (1.0f - fracX) + c4 * fracX;
            return (uint)(top * (1.0f - fracY) + bottom * fracY);
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "ShiftType", ShiftType },
                { "ShiftAmount", ShiftAmount },
                { "ShiftSpeed", ShiftSpeed },
                { "BeatReactive", BeatReactive },
                { "BeatAmplifier", BeatAmplifier },
                { "ShiftPattern", ShiftPattern },
                { "PatternFrequency", PatternFrequency },
                { "PatternPhase", PatternPhase },
                { "SmoothShifting", SmoothShifting },
                { "EdgeHandling", EdgeHandling }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            if (config.TryGetValue("ShiftType", out var shiftType))
                ShiftType = Convert.ToInt32(shiftType);
            if (config.TryGetValue("ShiftAmount", out var amount))
                ShiftAmount = Convert.ToSingle(amount);
            if (config.TryGetValue("ShiftSpeed", out var speed))
                ShiftSpeed = Convert.ToSingle(speed);
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            if (config.TryGetValue("BeatAmplifier", out var beatAmp))
                BeatAmplifier = Convert.ToSingle(beatAmp);
            if (config.TryGetValue("ShiftPattern", out var pattern))
                ShiftPattern = Convert.ToInt32(pattern);
            if (config.TryGetValue("PatternFrequency", out var frequency))
                PatternFrequency = Convert.ToSingle(frequency);
            if (config.TryGetValue("PatternPhase", out var phase))
                PatternPhase = Convert.ToSingle(phase);
            if (config.TryGetValue("SmoothShifting", out var smooth))
                SmoothShifting = Convert.ToBoolean(smooth);
            if (config.TryGetValue("EdgeHandling", out var edge))
                EdgeHandling = Convert.ToSingle(edge);
        }
    }
}