using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Enhanced Dynamic Movement Effects with advanced animation patterns
    /// Based on r_dynamicmovement.cpp from original AVS
    /// </summary>
    public class DynamicMovementEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public int MovementType { get; set; } = 0; // 0=Linear, 1=Circular, 2=Figure-8, 3=Spiral
        public float MovementSpeed { get; set; } = 1.0f;
        public int MovementPath { get; set; } = 0; // 0=Forward, 1=Reverse, 2=Ping-pong
        public bool BeatReactive { get; set; } = false;
        public int MovementEffect { get; set; } = 0; // 0=Translation, 1=Rotation, 2=Scale
        public float MovementAmplitude { get; set; } = 50.0f;
        public float MovementFrequency { get; set; } = 1.0f;
        public float Parameter1 { get; set; } = 1.0f;
        public float Parameter2 { get; set; } = 1.0f;
        public float BeatSpeedMultiplier { get; set; } = 2.0f;

        private float _currentTime = 0.0f;
        private float _currentX = 0.0f;
        private float _currentY = 0.0f;
        private int _beatCounter = 0;
        private const int BEAT_DURATION = 30;

        public DynamicMovementEffectsNode()
        {
            Name = "Dynamic Movement Effects";
            Description = "Enhanced movement patterns with advanced animation";
            Category = "Transform Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Output image"));
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

                UpdateMovementParameters();
                ApplyMovementTransformation(sourceImage, outputImage);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dynamic Movement Effects] Error: {ex.Message}");
            }
        }

        private void UpdateMovementParameters()
        {
            float effectiveSpeed = MovementSpeed;
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                effectiveSpeed *= (1.0f + (BeatSpeedMultiplier - 1.0f) * beatFactor);
            }

            _currentTime += effectiveSpeed * 0.016f;
            float t = _currentTime * MovementFrequency;

            switch (MovementType)
            {
                case 0: // Linear
                    _currentX = (t * Parameter1) % 1.0f;
                    _currentY = (t * Parameter2) % 1.0f;
                    break;
                case 1: // Circular
                    _currentX = 0.5f + MovementAmplitude / 200.0f * (float)Math.Cos(t);
                    _currentY = 0.5f + MovementAmplitude / 200.0f * (float)Math.Sin(t);
                    break;
                case 2: // Figure-8
                    _currentX = 0.5f + MovementAmplitude / 200.0f * (float)Math.Sin(t);
                    _currentY = 0.5f + MovementAmplitude / 400.0f * (float)Math.Sin(2 * t);
                    break;
                case 3: // Spiral
                    float radius = MovementAmplitude / 200.0f * (0.1f + 0.9f * ((t * 0.1f) % 1.0f));
                    _currentX = 0.5f + radius * (float)Math.Cos(t);
                    _currentY = 0.5f + radius * (float)Math.Sin(t);
                    break;
            }

            if (MovementPath == 1) // Reverse
            {
                _currentX = 1.0f - _currentX;
                _currentY = 1.0f - _currentY;
            }
            else if (MovementPath == 2) // Ping-pong
            {
                if (_currentTime % 2.0f > 1.0f)
                {
                    _currentX = 1.0f - _currentX;
                    _currentY = 1.0f - _currentY;
                }
            }
        }

        private void ApplyMovementTransformation(ImageBuffer source, ImageBuffer output)
        {
            int width = source.Width;
            int height = source.Height;
            float offsetX = (_currentX - 0.5f) * width * MovementAmplitude / 100.0f;
            float offsetY = (_currentY - 0.5f) * height * MovementAmplitude / 100.0f;

            switch (MovementEffect)
            {
                case 0: // Translation
                    ApplyTranslation(source, output, offsetX, offsetY);
                    break;
                case 1: // Rotation
                    ApplyRotation(source, output, _currentTime * Parameter1 * 360.0f);
                    break;
                case 2: // Scale
                    float scale = 1.0f + MovementAmplitude / 200.0f * (float)Math.Sin(_currentTime * Parameter1);
                    ApplyScale(source, output, scale);
                    break;
                default:
                    Array.Copy(source.Data, output.Data, source.Data.Length);
                    break;
            }
        }

        private void ApplyTranslation(ImageBuffer source, ImageBuffer output, float offsetX, float offsetY)
        {
            int width = source.Width;
            int height = source.Height;
            int intOffsetX = (int)Math.Round(offsetX);
            int intOffsetY = (int)Math.Round(offsetY);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int srcX = x - intOffsetX;
                    int srcY = y - intOffsetY;

                    if (srcX >= 0 && srcX < width && srcY >= 0 && srcY < height)
                    {
                        output.Data[y * width + x] = source.Data[srcY * width + srcX];
                    }
                    else
                    {
                        output.Data[y * width + x] = 0;
                    }
                }
            }
        }

        private void ApplyRotation(ImageBuffer source, ImageBuffer output, float rotation)
        {
            int width = source.Width;
            int height = source.Height;
            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            float radians = rotation * (float)Math.PI / 180.0f;
            float cosAngle = (float)Math.Cos(radians);
            float sinAngle = (float)Math.Sin(radians);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float relX = x - centerX;
                    float relY = y - centerY;

                    int srcX = (int)(centerX + relX * cosAngle - relY * sinAngle);
                    int srcY = (int)(centerY + relX * sinAngle + relY * cosAngle);

                    if (srcX >= 0 && srcX < width && srcY >= 0 && srcY < height)
                    {
                        output.Data[y * width + x] = source.Data[srcY * width + srcX];
                    }
                    else
                    {
                        output.Data[y * width + x] = 0;
                    }
                }
            }
        }

        private void ApplyScale(ImageBuffer source, ImageBuffer output, float scale)
        {
            int width = source.Width;
            int height = source.Height;
            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            float invScale = 1.0f / scale;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float relX = (x - centerX) * invScale;
                    float relY = (y - centerY) * invScale;

                    int srcX = (int)(centerX + relX);
                    int srcY = (int)(centerY + relY);

                    if (srcX >= 0 && srcX < width && srcY >= 0 && srcY < height)
                    {
                        output.Data[y * width + x] = source.Data[srcY * width + srcX];
                    }
                    else
                    {
                        output.Data[y * width + x] = 0;
                    }
                }
            }
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "MovementType", MovementType },
                { "MovementSpeed", MovementSpeed },
                { "MovementPath", MovementPath },
                { "BeatReactive", BeatReactive },
                { "MovementEffect", MovementEffect },
                { "MovementAmplitude", MovementAmplitude },
                { "MovementFrequency", MovementFrequency },
                { "Parameter1", Parameter1 },
                { "Parameter2", Parameter2 },
                { "BeatSpeedMultiplier", BeatSpeedMultiplier }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            if (config.TryGetValue("MovementType", out var movementType))
                MovementType = Convert.ToInt32(movementType);
            if (config.TryGetValue("MovementSpeed", out var speed))
                MovementSpeed = Convert.ToSingle(speed);
            if (config.TryGetValue("MovementPath", out var path))
                MovementPath = Convert.ToInt32(path);
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            if (config.TryGetValue("MovementEffect", out var effect))
                MovementEffect = Convert.ToInt32(effect);
            if (config.TryGetValue("MovementAmplitude", out var amplitude))
                MovementAmplitude = Convert.ToSingle(amplitude);
            if (config.TryGetValue("MovementFrequency", out var frequency))
                MovementFrequency = Convert.ToSingle(frequency);
            if (config.TryGetValue("Parameter1", out var param1))
                Parameter1 = Convert.ToSingle(param1);
            if (config.TryGetValue("Parameter2", out var param2))
                Parameter2 = Convert.ToSingle(param2);
            if (config.TryGetValue("BeatSpeedMultiplier", out var beatMult))
                BeatSpeedMultiplier = Convert.ToSingle(beatMult);
        }
    }
}