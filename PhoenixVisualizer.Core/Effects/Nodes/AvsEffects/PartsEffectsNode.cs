using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Multi-part video processing engine
    /// Based on r_parts.cpp C_PartsClass from original AVS
    /// Creates complex visual compositions by dividing screen into multiple sections
    /// </summary>
    public class PartsEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Parts effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Number of horizontal divisions
        /// </summary>
        public int HorizontalDivisions { get; set; } = 2;

        /// <summary>
        /// Number of vertical divisions
        /// </summary>
        public int VerticalDivisions { get; set; } = 2;

        /// <summary>
        /// Part selection mode
        /// 0 = All parts, 1 = Random part, 2 = Sequential parts, 3 = Beat-reactive
        /// </summary>
        public int PartSelectionMode { get; set; } = 0;

        /// <summary>
        /// Effect distribution mode
        /// 0 = Same effect all parts, 1 = Different effects per part, 2 = Random effects
        /// </summary>
        public int EffectDistributionMode { get; set; } = 1;

        /// <summary>
        /// Part boundary handling
        /// 0 = Hard boundaries, 1 = Soft boundaries, 2 = Overlap
        /// </summary>
        public int BoundaryMode { get; set; } = 0;

        /// <summary>
        /// Boundary width in pixels
        /// </summary>
        public int BoundaryWidth { get; set; } = 2;

        /// <summary>
        /// Beat reactivity for part switching
        /// </summary>
        public bool BeatReactive { get; set; } = true;

        /// <summary>
        /// Part switching speed for sequential mode
        /// </summary>
        public int SwitchingSpeed { get; set; } = 30; // frames

        /// <summary>
        /// Enable part mirroring
        /// </summary>
        public bool MirrorParts { get; set; } = false;

        /// <summary>
        /// Enable part rotation
        /// </summary>
        public bool RotateParts { get; set; } = false;

        /// <summary>
        /// Rotation angle per part (in degrees)
        /// </summary>
        public float RotationAnglePerPart { get; set; } = 45.0f;

        /// <summary>
        /// Part scaling factor
        /// </summary>
        public float PartScaling { get; set; } = 1.0f;

        /// <summary>
        /// Dynamic part resizing
        /// </summary>
        public bool DynamicResizing { get; set; } = false;

        /// <summary>
        /// Performance optimization level
        /// 0 = Quality, 1 = Balanced, 2 = Performance
        /// </summary>
        public int OptimizationLevel { get; set; } = 1;

        #endregion

        #region Private Fields

        private int _currentActivePart = 0;
        private int _frameCounter = 0;
        private readonly Random _random = new Random();
        private List<PartInfo> _parts = new List<PartInfo>();
        private bool _partsInitialized = false;

        #endregion

        #region Constructor

        public PartsEffectsNode()
        {
            Name = "Parts Effects";
            Description = "Multi-part video processing engine with screen partitioning";
            Category = "Composite Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for parts processing"));
            _inputPorts.Add(new EffectPort("Effect1", typeof(IEffect), false, null, "Effect for part 1"));
            _inputPorts.Add(new EffectPort("Effect2", typeof(IEffect), false, null, "Effect for part 2"));
            _inputPorts.Add(new EffectPort("Effect3", typeof(IEffect), false, null, "Effect for part 3"));
            _inputPorts.Add(new EffectPort("Effect4", typeof(IEffect), false, null, "Effect for part 4"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Parts processed output image"));
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

                // Initialize parts if needed
                if (!_partsInitialized || sourceImage.Width != _parts[0].Width || sourceImage.Height != _parts[0].Height)
                {
                    InitializeParts(sourceImage.Width, sourceImage.Height);
                }

                var outputImage = new ImageBuffer(sourceImage.Width, sourceImage.Height);

                // Update part selection
                UpdatePartSelection(audioFeatures);

                // Process parts
                ProcessParts(sourceImage, outputImage, inputData, audioFeatures);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Parts Effects] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void InitializeParts(int width, int height)
        {
            _parts.Clear();

            int partWidth = width / HorizontalDivisions;
            int partHeight = height / VerticalDivisions;

            for (int row = 0; row < VerticalDivisions; row++)
            {
                for (int col = 0; col < HorizontalDivisions; col++)
                {
                    var part = new PartInfo
                    {
                        Index = row * HorizontalDivisions + col,
                        X = col * partWidth,
                        Y = row * partHeight,
                        Width = (col == HorizontalDivisions - 1) ? width - col * partWidth : partWidth,
                        Height = (row == VerticalDivisions - 1) ? height - row * partHeight : partHeight,
                        IsActive = true,
                        EffectIndex = col % 4, // Cycle through available effects
                        Rotation = RotateParts ? (row * HorizontalDivisions + col) * RotationAnglePerPart : 0.0f
                    };

                    _parts.Add(part);
                }
            }

            _partsInitialized = true;
        }

        private void UpdatePartSelection(AudioFeatures audioFeatures)
        {
            _frameCounter++;

            switch (PartSelectionMode)
            {
                case 0: // All parts active
                    foreach (var part in _parts)
                        part.IsActive = true;
                    break;

                case 1: // Random part
                    if (BeatReactive && audioFeatures.Beat)
                    {
                        _currentActivePart = _random.Next(_parts.Count);
                    }
                    UpdateActivePartSingle(_currentActivePart);
                    break;

                case 2: // Sequential parts
                    if (_frameCounter >= SwitchingSpeed)
                    {
                        _currentActivePart = (_currentActivePart + 1) % _parts.Count;
                        _frameCounter = 0;
                    }
                    UpdateActivePartSingle(_currentActivePart);
                    break;

                case 3: // Beat-reactive
                    if (audioFeatures.Beat)
                    {
                        _currentActivePart = _random.Next(_parts.Count);
                        _frameCounter = 0;
                    }
                    
                    // Gradually activate more parts based on audio intensity
                    float intensity = (audioFeatures.Bass + audioFeatures.Mid + audioFeatures.Treble) / 3.0f;
                    int activeParts = Math.Max(1, (int)(intensity * _parts.Count));
                    
                    for (int i = 0; i < _parts.Count; i++)
                    {
                        _parts[i].IsActive = i < activeParts;
                    }
                    break;
            }
        }

        private void UpdateActivePartSingle(int activeIndex)
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                _parts[i].IsActive = (i == activeIndex);
            }
        }

        private void ProcessParts(ImageBuffer source, ImageBuffer output, Dictionary<string, object> inputData, AudioFeatures audioFeatures)
        {
            // Copy source to output as base
            Array.Copy(source.Data, output.Data, source.Data.Length);

            foreach (var part in _parts)
            {
                if (!part.IsActive) continue;

                ProcessSinglePart(source, output, part, inputData, audioFeatures);
            }

            // Draw boundaries if enabled
            if (BoundaryMode != 0 && BoundaryWidth > 0)
            {
                DrawPartBoundaries(output);
            }
        }

        private void ProcessSinglePart(ImageBuffer source, ImageBuffer output, PartInfo part, Dictionary<string, object> inputData, AudioFeatures audioFeatures)
        {
            // Extract part region
            var partImage = ExtractPartRegion(source, part);
            if (partImage == null) return;

            // Apply transformations if enabled
            if (RotateParts || MirrorParts || DynamicResizing)
            {
                partImage = ApplyPartTransformations(partImage, part, audioFeatures);
            }

            // Apply effect to part (simplified - would need actual effect application)
            var processedPart = ApplyEffectToPart(partImage, part, inputData, audioFeatures);

            // Blend processed part back to output
            BlendPartToOutput(processedPart, output, part);
        }

        private ImageBuffer ExtractPartRegion(ImageBuffer source, PartInfo part)
        {
            var partImage = new ImageBuffer(part.Width, part.Height);

            for (int y = 0; y < part.Height; y++)
            {
                for (int x = 0; x < part.Width; x++)
                {
                    int sourceX = part.X + x;
                    int sourceY = part.Y + y;

                    if (sourceX < source.Width && sourceY < source.Height)
                    {
                        int sourceIndex = sourceY * source.Width + sourceX;
                        int partIndex = y * part.Width + x;
                        partImage.Data[partIndex] = source.Data[sourceIndex];
                    }
                }
            }

            return partImage;
        }

        private ImageBuffer ApplyPartTransformations(ImageBuffer partImage, PartInfo part, AudioFeatures audioFeatures)
        {
            // Apply scaling if enabled
            if (DynamicResizing && PartScaling != 1.0f)
            {
                float dynamicScale = PartScaling + (audioFeatures.RMS - 0.5f) * 0.2f;
                partImage = ScalePartImage(partImage, dynamicScale);
            }

            // Apply rotation if enabled
            if (RotateParts && part.Rotation != 0.0f)
            {
                partImage = RotatePartImage(partImage, part.Rotation);
            }

            // Apply mirroring if enabled
            if (MirrorParts)
            {
                partImage = MirrorPartImage(partImage, part.Index);
            }

            return partImage;
        }

        private ImageBuffer ScalePartImage(ImageBuffer image, float scale)
        {
            if (Math.Abs(scale - 1.0f) < 0.01f) return image;

            int newWidth = (int)(image.Width * scale);
            int newHeight = (int)(image.Height * scale);
            var scaledImage = new ImageBuffer(newWidth, newHeight);

            float invScale = 1.0f / scale;

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    int sourceX = (int)(x * invScale);
                    int sourceY = (int)(y * invScale);

                    if (sourceX < image.Width && sourceY < image.Height)
                    {
                        scaledImage.Data[y * newWidth + x] = image.Data[sourceY * image.Width + sourceX];
                    }
                }
            }

            return scaledImage;
        }

        private ImageBuffer RotatePartImage(ImageBuffer image, float angleDegrees)
        {
            if (Math.Abs(angleDegrees) < 0.1f) return image;

            var rotatedImage = new ImageBuffer(image.Width, image.Height);
            float angleRadians = angleDegrees * (float)Math.PI / 180.0f;
            float cosAngle = (float)Math.Cos(angleRadians);
            float sinAngle = (float)Math.Sin(angleRadians);

            int centerX = image.Width / 2;
            int centerY = image.Height / 2;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int relX = x - centerX;
                    int relY = y - centerY;

                    int sourceX = centerX + (int)(relX * cosAngle - relY * sinAngle);
                    int sourceY = centerY + (int)(relX * sinAngle + relY * cosAngle);

                    if (sourceX >= 0 && sourceX < image.Width && sourceY >= 0 && sourceY < image.Height)
                    {
                        rotatedImage.Data[y * image.Width + x] = image.Data[sourceY * image.Width + sourceX];
                    }
                }
            }

            return rotatedImage;
        }

        private ImageBuffer MirrorPartImage(ImageBuffer image, int partIndex)
        {
            var mirroredImage = new ImageBuffer(image.Width, image.Height);

            bool mirrorX = (partIndex % 2) == 1;
            bool mirrorY = (partIndex / HorizontalDivisions % 2) == 1;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int sourceX = mirrorX ? (image.Width - 1 - x) : x;
                    int sourceY = mirrorY ? (image.Height - 1 - y) : y;

                    mirroredImage.Data[y * image.Width + x] = image.Data[sourceY * image.Width + sourceX];
                }
            }

            return mirroredImage;
        }

        private ImageBuffer ApplyEffectToPart(ImageBuffer partImage, PartInfo part, Dictionary<string, object> inputData, AudioFeatures audioFeatures)
        {
            // Simplified effect application - in real implementation, would apply actual effects
            // For now, just apply some basic transformations based on effect index
            
            switch (EffectDistributionMode)
            {
                case 0: // Same effect all parts
                    return ApplyBasicEffect(partImage, 0, audioFeatures);
                    
                case 1: // Different effects per part
                    return ApplyBasicEffect(partImage, part.EffectIndex, audioFeatures);
                    
                case 2: // Random effects
                    int randomEffect = _random.Next(4);
                    return ApplyBasicEffect(partImage, randomEffect, audioFeatures);
                    
                default:
                    return partImage;
            }
        }

        private ImageBuffer ApplyBasicEffect(ImageBuffer image, int effectIndex, AudioFeatures audioFeatures)
        {
            var result = new ImageBuffer(image.Width, image.Height);
            Array.Copy(image.Data, result.Data, image.Data.Length);

            // Apply basic effects based on index
            switch (effectIndex)
            {
                case 0: // Brightness modulation
                    ApplyBrightnessModulation(result, audioFeatures.RMS);
                    break;
                case 1: // Color shifting
                    ApplyColorShift(result, audioFeatures.Bass);
                    break;
                case 2: // Contrast enhancement
                    ApplyContrastEnhancement(result, audioFeatures.Mid);
                    break;
                case 3: // Hue rotation
                    ApplyHueRotation(result, audioFeatures.Treble);
                    break;
            }

            return result;
        }

        private void ApplyBrightnessModulation(ImageBuffer image, float intensity)
        {
            float multiplier = 0.5f + intensity;
            
            for (int i = 0; i < image.Data.Length; i++)
            {
                uint pixel = image.Data[i];
                uint r = (uint)Math.Min(255, ((pixel >> 16) & 0xFF) * multiplier);
                uint g = (uint)Math.Min(255, ((pixel >> 8) & 0xFF) * multiplier);
                uint b = (uint)Math.Min(255, (pixel & 0xFF) * multiplier);
                uint a = (pixel >> 24) & 0xFF;
                
                image.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        private void ApplyColorShift(ImageBuffer image, float intensity)
        {
            int shift = (int)(intensity * 64);
            
            for (int i = 0; i < image.Data.Length; i++)
            {
                uint pixel = image.Data[i];
                uint r = Math.Min(255u, Math.Max(0u, ((pixel >> 16) & 0xFF) + shift));
                uint g = (pixel >> 8) & 0xFF;
                uint b = Math.Max(0u, ((pixel & 0xFF) > shift) ? ((pixel & 0xFF) - shift) : 0);
                uint a = (pixel >> 24) & 0xFF;
                
                image.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        private void ApplyContrastEnhancement(ImageBuffer image, float intensity)
        {
            float contrast = 1.0f + intensity;
            
            for (int i = 0; i < image.Data.Length; i++)
            {
                uint pixel = image.Data[i];
                uint r = (uint)Math.Min(255, Math.Max(0, (((pixel >> 16) & 0xFF) - 128) * contrast + 128));
                uint g = (uint)Math.Min(255, Math.Max(0, (((pixel >> 8) & 0xFF) - 128) * contrast + 128));
                uint b = (uint)Math.Min(255, Math.Max(0, ((pixel & 0xFF) - 128) * contrast + 128));
                uint a = (pixel >> 24) & 0xFF;
                
                image.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        private void ApplyHueRotation(ImageBuffer image, float intensity)
        {
            float hueShift = intensity * 360.0f;
            
            for (int i = 0; i < image.Data.Length; i++)
            {
                uint pixel = image.Data[i];
                uint r = (pixel >> 16) & 0xFF;
                uint g = (pixel >> 8) & 0xFF;
                uint b = pixel & 0xFF;
                uint a = (pixel >> 24) & 0xFF;
                
                // Simple hue rotation approximation
                var rotated = RotateHue(r, g, b, hueShift);
                
                image.Data[i] = (a << 24) | (rotated.r << 16) | (rotated.g << 8) | rotated.b;
            }
        }

        private (uint r, uint g, uint b) RotateHue(uint r, uint g, uint b, float hueShift)
        {
            // Simplified hue rotation - swap color channels based on shift
            float normalizedShift = (hueShift % 360.0f) / 360.0f;
            
            if (normalizedShift < 0.33f)
                return (g, b, r); // RGB -> GBR
            else if (normalizedShift < 0.66f)
                return (b, r, g); // RGB -> BRG
            else
                return (r, g, b); // RGB -> RGB
        }

        private void BlendPartToOutput(ImageBuffer partImage, ImageBuffer output, PartInfo part)
        {
            for (int y = 0; y < part.Height && y < partImage.Height; y++)
            {
                for (int x = 0; x < part.Width && x < partImage.Width; x++)
                {
                    int outputX = part.X + x;
                    int outputY = part.Y + y;

                    if (outputX < output.Width && outputY < output.Height)
                    {
                        int outputIndex = outputY * output.Width + outputX;
                        int partIndex = y * partImage.Width + x;
                        
                        output.Data[outputIndex] = partImage.Data[partIndex];
                    }
                }
            }
        }

        private void DrawPartBoundaries(ImageBuffer output)
        {
            uint boundaryColor = 0xFF404040; // Gray

            // Draw horizontal boundaries
            for (int row = 1; row < VerticalDivisions; row++)
            {
                int y = row * (output.Height / VerticalDivisions);
                for (int x = 0; x < output.Width; x++)
                {
                    for (int dy = -BoundaryWidth/2; dy <= BoundaryWidth/2; dy++)
                    {
                        int drawY = y + dy;
                        if (drawY >= 0 && drawY < output.Height)
                        {
                            output.Data[drawY * output.Width + x] = boundaryColor;
                        }
                    }
                }
            }

            // Draw vertical boundaries
            for (int col = 1; col < HorizontalDivisions; col++)
            {
                int x = col * (output.Width / HorizontalDivisions);
                for (int y = 0; y < output.Height; y++)
                {
                    for (int dx = -BoundaryWidth/2; dx <= BoundaryWidth/2; dx++)
                    {
                        int drawX = x + dx;
                        if (drawX >= 0 && drawX < output.Width)
                        {
                            output.Data[y * output.Width + drawX] = boundaryColor;
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
                { "HorizontalDivisions", HorizontalDivisions },
                { "VerticalDivisions", VerticalDivisions },
                { "PartSelectionMode", PartSelectionMode },
                { "EffectDistributionMode", EffectDistributionMode },
                { "BoundaryMode", BoundaryMode },
                { "BoundaryWidth", BoundaryWidth },
                { "BeatReactive", BeatReactive },
                { "SwitchingSpeed", SwitchingSpeed },
                { "MirrorParts", MirrorParts },
                { "RotateParts", RotateParts },
                { "RotationAnglePerPart", RotationAnglePerPart },
                { "PartScaling", PartScaling },
                { "DynamicResizing", DynamicResizing },
                { "OptimizationLevel", OptimizationLevel }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            
            if (config.TryGetValue("HorizontalDivisions", out var hDiv))
            {
                HorizontalDivisions = Convert.ToInt32(hDiv);
                _partsInitialized = false; // Force re-initialization
            }
            
            if (config.TryGetValue("VerticalDivisions", out var vDiv))
            {
                VerticalDivisions = Convert.ToInt32(vDiv);
                _partsInitialized = false; // Force re-initialization
            }
            
            if (config.TryGetValue("PartSelectionMode", out var selMode))
                PartSelectionMode = Convert.ToInt32(selMode);
            
            if (config.TryGetValue("EffectDistributionMode", out var distMode))
                EffectDistributionMode = Convert.ToInt32(distMode);
            
            if (config.TryGetValue("BoundaryMode", out var boundMode))
                BoundaryMode = Convert.ToInt32(boundMode);
            
            if (config.TryGetValue("BoundaryWidth", out var boundWidth))
                BoundaryWidth = Convert.ToInt32(boundWidth);
            
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            
            if (config.TryGetValue("SwitchingSpeed", out var switchSpeed))
                SwitchingSpeed = Convert.ToInt32(switchSpeed);
            
            if (config.TryGetValue("MirrorParts", out var mirror))
                MirrorParts = Convert.ToBoolean(mirror);
            
            if (config.TryGetValue("RotateParts", out var rotate))
                RotateParts = Convert.ToBoolean(rotate);
            
            if (config.TryGetValue("RotationAnglePerPart", out var rotAngle))
                RotationAnglePerPart = Convert.ToSingle(rotAngle);
            
            if (config.TryGetValue("PartScaling", out var scaling))
                PartScaling = Convert.ToSingle(scaling);
            
            if (config.TryGetValue("DynamicResizing", out var dynResize))
                DynamicResizing = Convert.ToBoolean(dynResize);
            
            if (config.TryGetValue("OptimizationLevel", out var optLevel))
                OptimizationLevel = Convert.ToInt32(optLevel);
        }

        #endregion
    }

    #region Helper Classes

    public class PartInfo
    {
        public int Index { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsActive { get; set; }
        public int EffectIndex { get; set; }
        public float Rotation { get; set; }
    }

    #endregion
}