using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Ultimate Composite Effects - Advanced multi-layer compositing system
    /// The final piece of our effects library - handles complex layer blending and composition
    /// </summary>
    public class CompositeEffectsNode : BaseEffectNode
    {
        #region Properties

        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Number of layers to composite
        /// </summary>
        public int LayerCount { get; set; } = 4;
        
        /// <summary>
        /// Main compositing mode
        /// 0 = Normal, 1 = Screen, 2 = Multiply, 3 = Overlay, 4 = Soft Light, 5 = Hard Light, 6 = Color Dodge, 7 = Color Burn
        /// </summary>
        public int CompositingMode { get; set; } = 0;
        
        /// <summary>
        /// Global opacity for the entire composite
        /// </summary>
        public float GlobalOpacity { get; set; } = 1.0f;
        
        /// <summary>
        /// Layer blend modes array
        /// </summary>
        public int[] LayerBlendModes { get; set; } = { 0, 1, 2, 3 };
        
        /// <summary>
        /// Layer opacities array
        /// </summary>
        public float[] LayerOpacities { get; set; } = { 1.0f, 0.8f, 0.6f, 0.4f };
        
        /// <summary>
        /// Layer scales array
        /// </summary>
        public float[] LayerScales { get; set; } = { 1.0f, 1.0f, 1.0f, 1.0f };
        
        /// <summary>
        /// Layer rotations array (degrees)
        /// </summary>
        public float[] LayerRotations { get; set; } = { 0.0f, 0.0f, 0.0f, 0.0f };
        
        /// <summary>
        /// Layer X offsets (normalized)
        /// </summary>
        public float[] LayerOffsetsX { get; set; } = { 0.0f, 0.0f, 0.0f, 0.0f };
        
        /// <summary>
        /// Layer Y offsets (normalized)
        /// </summary>
        public float[] LayerOffsetsY { get; set; } = { 0.0f, 0.0f, 0.0f, 0.0f };
        
        /// <summary>
        /// Beat reactive layers
        /// </summary>
        public bool BeatReactive { get; set; } = false;
        
        /// <summary>
        /// Beat effect intensity
        /// </summary>
        public float BeatIntensity { get; set; } = 1.5f;
        
        /// <summary>
        /// Audio reactive compositing
        /// </summary>
        public bool AudioReactive { get; set; } = false;
        
        /// <summary>
        /// Audio sensitivity
        /// </summary>
        public float AudioSensitivity { get; set; } = 1.0f;
        
        /// <summary>
        /// Enable dynamic layer animation
        /// </summary>
        public bool AnimateLayers { get; set; } = false;
        
        /// <summary>
        /// Animation speed
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;
        
        /// <summary>
        /// Enable layer masking
        /// </summary>
        public bool EnableMasking { get; set; } = false;
        
        /// <summary>
        /// Mask blend mode
        /// </summary>
        public int MaskBlendMode { get; set; } = 0;
        
        /// <summary>
        /// Enable color correction per layer
        /// </summary>
        public bool EnableColorCorrection { get; set; } = false;
        
        /// <summary>
        /// Layer color tints
        /// </summary>
        public Color[] LayerTints { get; set; } = { Color.White, Color.White, Color.White, Color.White };
        
        /// <summary>
        /// Layer contrast adjustments
        /// </summary>
        public float[] LayerContrasts { get; set; } = { 1.0f, 1.0f, 1.0f, 1.0f };
        
        /// <summary>
        /// Layer brightness adjustments
        /// </summary>
        public float[] LayerBrightness { get; set; } = { 0.0f, 0.0f, 0.0f, 0.0f };

        #endregion

        #region Private Classes

        private class CompositeLayer
        {
            public ImageBuffer Buffer;
            public int BlendMode;
            public float Opacity;
            public float Scale;
            public float Rotation;
            public float OffsetX, OffsetY;
            public Color Tint;
            public float Contrast;
            public float Brightness;
            public bool Active;
            
            public CompositeLayer(int width, int height)
            {
                Buffer = new ImageBuffer(width, height);
                BlendMode = 0;
                Opacity = 1.0f;
                Scale = 1.0f;
                Rotation = 0.0f;
                OffsetX = OffsetY = 0.0f;
                Tint = Color.White;
                Contrast = 1.0f;
                Brightness = 0.0f;
                Active = true;
            }
        }

        #endregion

        #region Private Fields

        private CompositeLayer[] _layers;
        private ImageBuffer _compositeBuffer;
        private ImageBuffer _maskBuffer;
        private float _time = 0.0f;
        private int _beatCounter = 0;
        private readonly Random _random = new Random();
        private const int BEAT_DURATION = 30;

        // Blend mode lookup table for performance
        private readonly Dictionary<int, string> _blendModeNames = new Dictionary<int, string>
        {
            { 0, "Normal" },
            { 1, "Screen" },
            { 2, "Multiply" },
            { 3, "Overlay" },
            { 4, "Soft Light" },
            { 5, "Hard Light" },
            { 6, "Color Dodge" },
            { 7, "Color Burn" },
            { 8, "Darken" },
            { 9, "Lighten" },
            { 10, "Difference" },
            { 11, "Exclusion" }
        };

        #endregion

        #region Constructor

        public CompositeEffectsNode()
        {
            Name = "Composite Effects";
            Description = "Ultimate multi-layer compositing system with advanced blending and effects";
            Category = "Composite Effects";
            
            // Initialize arrays with default sizes
            EnsureArraySizes();
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            // Dynamic input ports based on layer count
            for (int i = 0; i < LayerCount; i++)
            {
                _inputPorts.Add(new EffectPort($"Layer{i}", typeof(ImageBuffer), i == 0, null, $"Layer {i} input"));
            }
            
            _inputPorts.Add(new EffectPort("Mask", typeof(ImageBuffer), false, null, "Optional mask for compositing"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Composited output"));
        }

        private void EnsureArraySizes()
        {
            if (LayerBlendModes == null || LayerBlendModes.Length != LayerCount)
            {
                var newArray = new int[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                {
                    newArray[i] = LayerBlendModes?.Length > i ? LayerBlendModes[i] : i % 4;
                }
                LayerBlendModes = newArray;
            }
            
            if (LayerOpacities == null || LayerOpacities.Length != LayerCount)
            {
                var newArray = new float[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                {
                    newArray[i] = LayerOpacities?.Length > i ? LayerOpacities[i] : Math.Max(0.2f, 1.0f - i * 0.2f);
                }
                LayerOpacities = newArray;
            }
            
            // Ensure all other arrays are properly sized
            if (LayerScales?.Length != LayerCount)
            {
                var newArray = new float[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                    newArray[i] = LayerScales?.Length > i ? LayerScales[i] : 1.0f;
                LayerScales = newArray;
            }
            
            if (LayerRotations?.Length != LayerCount)
            {
                var newArray = new float[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                    newArray[i] = LayerRotations?.Length > i ? LayerRotations[i] : 0.0f;
                LayerRotations = newArray;
            }
            
            if (LayerOffsetsX?.Length != LayerCount)
            {
                var newArray = new float[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                    newArray[i] = LayerOffsetsX?.Length > i ? LayerOffsetsX[i] : 0.0f;
                LayerOffsetsX = newArray;
            }
            
            if (LayerOffsetsY?.Length != LayerCount)
            {
                var newArray = new float[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                    newArray[i] = LayerOffsetsY?.Length > i ? LayerOffsetsY[i] : 0.0f;
                LayerOffsetsY = newArray;
            }
            
            if (LayerTints?.Length != LayerCount)
            {
                var newArray = new Color[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                    newArray[i] = LayerTints?.Length > i ? LayerTints[i] : Color.White;
                LayerTints = newArray;
            }
            
            if (LayerContrasts?.Length != LayerCount)
            {
                var newArray = new float[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                    newArray[i] = LayerContrasts?.Length > i ? LayerContrasts[i] : 1.0f;
                LayerContrasts = newArray;
            }
            
            if (LayerBrightness?.Length != LayerCount)
            {
                var newArray = new float[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                    newArray[i] = LayerBrightness?.Length > i ? LayerBrightness[i] : 0.0f;
                LayerBrightness = newArray;
            }
        }

        private void InitializeLayers(int width, int height)
        {
            if (_layers == null || _layers.Length != LayerCount)
            {
                _layers = new CompositeLayer[LayerCount];
            }
            
            for (int i = 0; i < LayerCount; i++)
            {
                if (_layers[i] == null || _layers[i].Buffer.Width != width || _layers[i].Buffer.Height != height)
                {
                    _layers[i] = new CompositeLayer(width, height);
                }
                
                // Update layer properties
                _layers[i].BlendMode = LayerBlendModes[i];
                _layers[i].Opacity = LayerOpacities[i];
                _layers[i].Scale = LayerScales[i];
                _layers[i].Rotation = LayerRotations[i];
                _layers[i].OffsetX = LayerOffsetsX[i];
                _layers[i].OffsetY = LayerOffsetsY[i];
                _layers[i].Tint = LayerTints[i];
                _layers[i].Contrast = LayerContrasts[i];
                _layers[i].Brightness = LayerBrightness[i];
            }
            
            // Initialize composite buffer
            if (_compositeBuffer == null || _compositeBuffer.Width != width || _compositeBuffer.Height != height)
            {
                _compositeBuffer = new ImageBuffer(width, height);
            }
            
            // Initialize mask buffer if needed
            if (EnableMasking && (_maskBuffer == null || _maskBuffer.Width != width || _maskBuffer.Height != height))
            {
                _maskBuffer = new ImageBuffer(width, height);
            }
        }

        #endregion

        #region Effect Processing

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                // Get the first layer to determine output size
                var firstLayer = GetInputValue<ImageBuffer>("Layer0", inputData);
                if (firstLayer?.Data == null)
                {
                    outputData["Output"] = new ImageBuffer(640, 480);
                    return;
                }

                // Initialize layers and buffers
                InitializeLayers(firstLayer.Width, firstLayer.Height);
                var outputImage = new ImageBuffer(firstLayer.Width, firstLayer.Height);

                // Handle beat reactivity
                if (BeatReactive && audioFeatures.Beat)
                {
                    _beatCounter = BEAT_DURATION;
                }
                else if (_beatCounter > 0)
                {
                    _beatCounter--;
                }

                // Update time for animations
                _time += AnimationSpeed * 0.016f;

                // Process each layer
                ProcessLayers(inputData, audioFeatures);

                // Get mask if enabled
                var maskImage = EnableMasking ? GetInputValue<ImageBuffer>("Mask", inputData) : null;

                // Composite all layers
                PerformCompositing(outputImage, maskImage, audioFeatures);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Composite Effects] Error: {ex.Message}");
                // Return a black image on error
                outputData["Output"] = new ImageBuffer(640, 480);
            }
        }

        #endregion

        #region Private Methods

        private void ProcessLayers(Dictionary<string, object> inputData, AudioFeatures audioFeatures)
        {
            for (int i = 0; i < LayerCount; i++)
            {
                var layerInput = GetInputValue<ImageBuffer>($"Layer{i}", inputData);
                if (layerInput?.Data == null) continue;

                var layer = _layers[i];
                
                // Copy input to layer buffer
                Array.Copy(layerInput.Data, layer.Buffer.Data, layerInput.Data.Length);
                
                // Apply layer transformations
                ApplyLayerTransformations(layer, i, audioFeatures);
                
                // Apply color corrections
                if (EnableColorCorrection)
                {
                    ApplyColorCorrection(layer);
                }
            }
        }

        private void ApplyLayerTransformations(CompositeLayer layer, int layerIndex, AudioFeatures audioFeatures)
        {
            // Calculate effective properties with beat/audio reactivity
            float effectiveScale = CalculateEffectiveScale(layer.Scale, layerIndex, audioFeatures);
            float effectiveRotation = CalculateEffectiveRotation(layer.Rotation, layerIndex, audioFeatures);
            float effectiveOffsetX = CalculateEffectiveOffset(layer.OffsetX, layerIndex, audioFeatures, true);
            float effectiveOffsetY = CalculateEffectiveOffset(layer.OffsetY, layerIndex, audioFeatures, false);
            float effectiveOpacity = CalculateEffectiveOpacity(layer.Opacity, layerIndex, audioFeatures);
            
            // Apply transformations
            if (Math.Abs(effectiveScale - 1.0f) > 0.001f || Math.Abs(effectiveRotation) > 0.001f || 
                Math.Abs(effectiveOffsetX) > 0.001f || Math.Abs(effectiveOffsetY) > 0.001f)
            {
                ApplyGeometricTransform(layer.Buffer, effectiveScale, effectiveRotation, effectiveOffsetX, effectiveOffsetY);
            }
            
            // Update layer opacity
            layer.Opacity = effectiveOpacity;
        }

        private float CalculateEffectiveScale(float baseScale, int layerIndex, AudioFeatures audioFeatures)
        {
            float scale = baseScale;
            
            // Beat reactivity
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                scale *= (1.0f + (BeatIntensity - 1.0f) * beatFactor * 0.2f);
            }
            
            // Audio reactivity
            if (AudioReactive && audioFeatures != null)
            {
                float audioFactor = audioFeatures.RMS * AudioSensitivity;
                scale *= (1.0f + audioFactor * 0.1f * (layerIndex + 1));
            }
            
            // Animation
            if (AnimateLayers)
            {
                scale *= (1.0f + 0.1f * (float)Math.Sin(_time * 2 + layerIndex));
            }
            
            return Math.Max(0.1f, Math.Min(3.0f, scale));
        }

        private float CalculateEffectiveRotation(float baseRotation, int layerIndex, AudioFeatures audioFeatures)
        {
            float rotation = baseRotation;
            
            // Beat reactivity
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                rotation += beatFactor * 45.0f * (layerIndex % 2 == 0 ? 1 : -1);
            }
            
            // Animation
            if (AnimateLayers)
            {
                rotation += _time * 10.0f * (layerIndex + 1) * (layerIndex % 2 == 0 ? 1 : -1);
            }
            
            return rotation;
        }

        private float CalculateEffectiveOffset(float baseOffset, int layerIndex, AudioFeatures audioFeatures, bool isX)
        {
            float offset = baseOffset;
            
            // Audio reactivity
            if (AudioReactive && audioFeatures != null)
            {
                float audioFactor = isX ? audioFeatures.Bass : audioFeatures.Treble;
                offset += (audioFactor - 0.5f) * AudioSensitivity * 0.1f;
            }
            
            // Animation
            if (AnimateLayers)
            {
                float animOffset = 0.05f * (float)(isX ? Math.Cos(_time + layerIndex) : Math.Sin(_time + layerIndex));
                offset += animOffset;
            }
            
            return Math.Max(-1.0f, Math.Min(1.0f, offset));
        }

        private float CalculateEffectiveOpacity(float baseOpacity, int layerIndex, AudioFeatures audioFeatures)
        {
            float opacity = baseOpacity;
            
            // Beat reactivity
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                opacity *= (1.0f + beatFactor * 0.3f);
            }
            
            // Audio reactivity
            if (AudioReactive && audioFeatures != null)
            {
                opacity *= (0.7f + audioFeatures.RMS * AudioSensitivity * 0.3f);
            }
            
            return Math.Max(0.0f, Math.Min(1.0f, opacity));
        }

        private void ApplyGeometricTransform(ImageBuffer buffer, float scale, float rotation, float offsetX, float offsetY)
        {
            int width = buffer.Width;
            int height = buffer.Height;
            var tempBuffer = new uint[buffer.Data.Length];
            Array.Copy(buffer.Data, tempBuffer, buffer.Data.Length);
            
            // Clear buffer
            Array.Clear(buffer.Data, 0, buffer.Data.Length);
            
            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            float radians = rotation * (float)Math.PI / 180.0f;
            float cosAngle = (float)Math.Cos(radians);
            float sinAngle = (float)Math.Sin(radians);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Apply inverse transformation to find source pixel
                    float relX = x - centerX;
                    float relY = y - centerY;
                    
                    // Inverse scale
                    relX /= scale;
                    relY /= scale;
                    
                    // Inverse rotation
                    float srcRelX = relX * cosAngle + relY * sinAngle;
                    float srcRelY = -relX * sinAngle + relY * cosAngle;
                    
                    // Add center back and apply offset
                    int srcX = (int)(centerX + srcRelX - offsetX * width);
                    int srcY = (int)(centerY + srcRelY - offsetY * height);
                    
                    // Check bounds and copy pixel
                    if (srcX >= 0 && srcX < width && srcY >= 0 && srcY < height)
                    {
                        buffer.Data[y * width + x] = tempBuffer[srcY * width + srcX];
                    }
                }
            }
        }

        private void ApplyColorCorrection(CompositeLayer layer)
        {
            for (int i = 0; i < layer.Buffer.Data.Length; i++)
            {
                uint pixel = layer.Buffer.Data[i];
                
                uint a = (pixel >> 24) & 0xFF;
                uint r = (pixel >> 16) & 0xFF;
                uint g = (pixel >> 8) & 0xFF;
                uint b = pixel & 0xFF;
                
                // Apply tint
                r = (uint)Math.Min(255, r * layer.Tint.R / 255.0f);
                g = (uint)Math.Min(255, g * layer.Tint.G / 255.0f);
                b = (uint)Math.Min(255, b * layer.Tint.B / 255.0f);
                
                // Apply contrast
                r = (uint)Math.Max(0, Math.Min(255, (r - 128) * layer.Contrast + 128));
                g = (uint)Math.Max(0, Math.Min(255, (g - 128) * layer.Contrast + 128));
                b = (uint)Math.Max(0, Math.Min(255, (b - 128) * layer.Contrast + 128));
                
                // Apply brightness
                r = (uint)Math.Max(0, Math.Min(255, r + layer.Brightness * 128));
                g = (uint)Math.Max(0, Math.Min(255, g + layer.Brightness * 128));
                b = (uint)Math.Max(0, Math.Min(255, b + layer.Brightness * 128));
                
                layer.Buffer.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        private void PerformCompositing(ImageBuffer output, ImageBuffer mask, AudioFeatures audioFeatures)
        {
            // Start with first layer as base
            if (_layers[0] != null)
            {
                Array.Copy(_layers[0].Buffer.Data, output.Data, output.Data.Length);
            }
            
            // Composite remaining layers
            for (int i = 1; i < LayerCount; i++)
            {
                if (_layers[i] == null || !_layers[i].Active) continue;
                
                CompositeLayer(output, _layers[i], mask);
            }
            
            // Apply global opacity
            if (Math.Abs(GlobalOpacity - 1.0f) > 0.001f)
            {
                ApplyGlobalOpacity(output);
            }
        }

        private void CompositeLayer(ImageBuffer destination, CompositeLayer layer, ImageBuffer mask)
        {
            for (int i = 0; i < destination.Data.Length; i++)
            {
                uint destPixel = destination.Data[i];
                uint srcPixel = layer.Buffer.Data[i];
                
                // Apply mask if available
                float maskValue = 1.0f;
                if (mask != null && EnableMasking)
                {
                    uint maskPixel = mask.Data[i];
                    maskValue = ((maskPixel >> 16) & 0xFF) / 255.0f; // Use red channel as mask
                }
                
                // Apply layer opacity and mask
                float effectiveOpacity = layer.Opacity * maskValue;
                
                // Blend pixels
                uint blendedPixel = BlendPixels(destPixel, srcPixel, layer.BlendMode, effectiveOpacity);
                destination.Data[i] = blendedPixel;
            }
        }

        private uint BlendPixels(uint destination, uint source, int blendMode, float opacity)
        {
            if (opacity <= 0.0f) return destination;
            if (opacity >= 1.0f && blendMode == 0) return source;
            
            uint destA = (destination >> 24) & 0xFF;
            uint destR = (destination >> 16) & 0xFF;
            uint destG = (destination >> 8) & 0xFF;
            uint destB = destination & 0xFF;
            
            uint srcA = (source >> 24) & 0xFF;
            uint srcR = (source >> 16) & 0xFF;
            uint srcG = (source >> 8) & 0xFF;
            uint srcB = source & 0xFF;
            
            // Apply blend mode
            uint resultR = ApplyBlendMode(destR, srcR, blendMode);
            uint resultG = ApplyBlendMode(destG, srcG, blendMode);
            uint resultB = ApplyBlendMode(destB, srcB, blendMode);
            
            // Apply opacity
            resultR = (uint)(destR * (1 - opacity) + resultR * opacity);
            resultG = (uint)(destG * (1 - opacity) + resultG * opacity);
            resultB = (uint)(destB * (1 - opacity) + resultB * opacity);
            
            uint resultA = Math.Max(destA, srcA);
            
            return (resultA << 24) | (resultR << 16) | (resultG << 8) | resultB;
        }

        private uint ApplyBlendMode(uint dest, uint src, int blendMode)
        {
            float d = dest / 255.0f;
            float s = src / 255.0f;
            float result;
            
            switch (blendMode)
            {
                case 0: // Normal
                    result = s;
                    break;
                    
                case 1: // Screen
                    result = 1.0f - (1.0f - d) * (1.0f - s);
                    break;
                    
                case 2: // Multiply
                    result = d * s;
                    break;
                    
                case 3: // Overlay
                    result = d < 0.5f ? 2 * d * s : 1.0f - 2 * (1.0f - d) * (1.0f - s);
                    break;
                    
                case 4: // Soft Light
                    result = d < 0.5f ? 2 * d * s + d * d * (1 - 2 * s) : 
                             2 * d * (1 - s) + (float)Math.Sqrt(d) * (2 * s - 1);
                    break;
                    
                case 5: // Hard Light
                    result = s < 0.5f ? 2 * d * s : 1.0f - 2 * (1.0f - d) * (1.0f - s);
                    break;
                    
                case 6: // Color Dodge
                    result = d >= 1.0f ? 1.0f : (s >= 1.0f ? 1.0f : Math.Min(1.0f, d / (1.0f - s)));
                    break;
                    
                case 7: // Color Burn
                    result = d <= 0.0f ? 0.0f : (s <= 0.0f ? 0.0f : Math.Max(0.0f, 1.0f - (1.0f - d) / s));
                    break;
                    
                case 8: // Darken
                    result = Math.Min(d, s);
                    break;
                    
                case 9: // Lighten
                    result = Math.Max(d, s);
                    break;
                    
                case 10: // Difference
                    result = Math.Abs(d - s);
                    break;
                    
                case 11: // Exclusion
                    result = d + s - 2 * d * s;
                    break;
                    
                default:
                    result = s;
                    break;
            }
            
            return (uint)Math.Max(0, Math.Min(255, Math.Round(result * 255)));
        }

        private void ApplyGlobalOpacity(ImageBuffer buffer)
        {
            for (int i = 0; i < buffer.Data.Length; i++)
            {
                uint pixel = buffer.Data[i];
                uint a = (uint)((pixel >> 24) * GlobalOpacity);
                uint r = (uint)((pixel >> 16) & 0xFF * GlobalOpacity);
                uint g = (uint)((pixel >> 8) & 0xFF * GlobalOpacity);
                uint b = (uint)((pixel & 0xFF) * GlobalOpacity);
                
                buffer.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        #endregion

        #region Configuration

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "LayerCount", LayerCount },
                { "CompositingMode", CompositingMode },
                { "GlobalOpacity", GlobalOpacity },
                { "LayerBlendModes", LayerBlendModes },
                { "LayerOpacities", LayerOpacities },
                { "LayerScales", LayerScales },
                { "LayerRotations", LayerRotations },
                { "LayerOffsetsX", LayerOffsetsX },
                { "LayerOffsetsY", LayerOffsetsY },
                { "BeatReactive", BeatReactive },
                { "BeatIntensity", BeatIntensity },
                { "AudioReactive", AudioReactive },
                { "AudioSensitivity", AudioSensitivity },
                { "AnimateLayers", AnimateLayers },
                { "AnimationSpeed", AnimationSpeed },
                { "EnableMasking", EnableMasking },
                { "MaskBlendMode", MaskBlendMode },
                { "EnableColorCorrection", EnableColorCorrection },
                { "LayerTints", LayerTints },
                { "LayerContrasts", LayerContrasts },
                { "LayerBrightness", LayerBrightness }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            
            if (config.TryGetValue("LayerCount", out var layerCount))
            {
                LayerCount = Convert.ToInt32(layerCount);
                EnsureArraySizes();
                InitializePorts(); // Rebuild ports
            }
            
            if (config.TryGetValue("CompositingMode", out var compositingMode))
                CompositingMode = Convert.ToInt32(compositingMode);
            
            if (config.TryGetValue("GlobalOpacity", out var globalOpacity))
                GlobalOpacity = Convert.ToSingle(globalOpacity);
            
            // Apply all array configurations
            if (config.TryGetValue("LayerBlendModes", out var blendModes))
                LayerBlendModes = (int[])blendModes;
            
            if (config.TryGetValue("LayerOpacities", out var opacities))
                LayerOpacities = (float[])opacities;
            
            if (config.TryGetValue("LayerScales", out var scales))
                LayerScales = (float[])scales;
            
            if (config.TryGetValue("LayerRotations", out var rotations))
                LayerRotations = (float[])rotations;
            
            if (config.TryGetValue("LayerOffsetsX", out var offsetsX))
                LayerOffsetsX = (float[])offsetsX;
            
            if (config.TryGetValue("LayerOffsetsY", out var offsetsY))
                LayerOffsetsY = (float[])offsetsY;
            
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            
            if (config.TryGetValue("BeatIntensity", out var beatIntensity))
                BeatIntensity = Convert.ToSingle(beatIntensity);
            
            if (config.TryGetValue("AudioReactive", out var audioReactive))
                AudioReactive = Convert.ToBoolean(audioReactive);
            
            if (config.TryGetValue("AudioSensitivity", out var audioSensitivity))
                AudioSensitivity = Convert.ToSingle(audioSensitivity);
            
            if (config.TryGetValue("AnimateLayers", out var animateLayers))
                AnimateLayers = Convert.ToBoolean(animateLayers);
            
            if (config.TryGetValue("AnimationSpeed", out var animationSpeed))
                AnimationSpeed = Convert.ToSingle(animationSpeed);
            
            if (config.TryGetValue("EnableMasking", out var enableMasking))
                EnableMasking = Convert.ToBoolean(enableMasking);
            
            if (config.TryGetValue("MaskBlendMode", out var maskBlendMode))
                MaskBlendMode = Convert.ToInt32(maskBlendMode);
            
            if (config.TryGetValue("EnableColorCorrection", out var enableColorCorrection))
                EnableColorCorrection = Convert.ToBoolean(enableColorCorrection);
            
            if (config.TryGetValue("LayerTints", out var tints))
                LayerTints = (Color[])tints;
            
            if (config.TryGetValue("LayerContrasts", out var contrasts))
                LayerContrasts = (float[])contrasts;
            
            if (config.TryGetValue("LayerBrightness", out var brightness))
                LayerBrightness = (float[])brightness;
        }

        /// <summary>
        /// Get the name of a blend mode
        /// </summary>
        public string GetBlendModeName(int blendMode)
        {
            return _blendModeNames.TryGetValue(blendMode, out string name) ? name : "Unknown";
        }

        /// <summary>
        /// Get all available blend mode names
        /// </summary>
        public string[] GetAllBlendModeNames()
        {
            var names = new string[_blendModeNames.Count];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = _blendModeNames.TryGetValue(i, out string name) ? name : $"Mode {i}";
            }
            return names;
        }

        #endregion
    }
}