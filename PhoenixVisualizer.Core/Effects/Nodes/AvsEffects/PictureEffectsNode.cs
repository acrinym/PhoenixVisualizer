using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Picture Effects - loads and displays images with various effects
    /// Based on picture/image effects from original AVS
    /// </summary>
    public class PictureEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public string ImagePath { get; set; } = "";
        public int BlendMode { get; set; } = 0; // 0=Replace, 1=Add, 2=Multiply, 3=Overlay, 4=Screen
        public float Opacity { get; set; } = 1.0f;
        public float PositionX { get; set; } = 0.5f; // Normalized position
        public float PositionY { get; set; } = 0.5f;
        public float ScaleX { get; set; } = 1.0f;
        public float ScaleY { get; set; } = 1.0f;
        public float Rotation { get; set; } = 0.0f; // Degrees
        public bool BeatReactive { get; set; } = false;
        public float BeatScale { get; set; } = 1.2f;
        public float BeatRotation { get; set; } = 10.0f; // Degrees per beat
        public int FilterType { get; set; } = 0; // 0=None, 1=Blur, 2=Sharpen, 3=Edge, 4=Emboss
        public float FilterStrength { get; set; } = 0.5f;
        public bool FlipHorizontal { get; set; } = false;
        public bool FlipVertical { get; set; } = false;
        public Color TintColor { get; set; } = Color.White;
        public float TintAmount { get; set; } = 0.0f;

        private ImageBuffer _loadedImage = null;
        private string _currentImagePath = "";
        private int _beatCounter = 0;
        private float _currentRotation = 0.0f;
        private const int BEAT_DURATION = 20;

        public PictureEffectsNode()
        {
            Name = "Picture Effects";
            Description = "Loads and displays images with various effects and transformations";
            Category = "Image Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Background", typeof(ImageBuffer), false, null, "Optional background image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Output with picture effect"));
        }

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                var backgroundImage = GetInputValue<ImageBuffer>("Background", inputData);
                var outputImage = backgroundImage != null ? 
                    new ImageBuffer(backgroundImage.Width, backgroundImage.Height) : 
                    new ImageBuffer(640, 480);

                // Copy background if provided
                if (backgroundImage != null)
                {
                    Array.Copy(backgroundImage.Data, outputImage.Data, backgroundImage.Data.Length);
                }

                // Load image if path changed
                if (!string.IsNullOrEmpty(ImagePath) && ImagePath != _currentImagePath)
                {
                    LoadImage();
                    _currentImagePath = ImagePath;
                }

                if (_loadedImage == null) 
                {
                    outputData["Output"] = outputImage;
                    return;
                }

                if (BeatReactive && audioFeatures.Beat)
                {
                    _beatCounter = BEAT_DURATION;
                    _currentRotation += BeatRotation;
                }
                else if (_beatCounter > 0)
                {
                    _beatCounter--;
                }

                ApplyPictureEffect(outputImage);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Picture Effects] Error: {ex.Message}");
            }
        }

        private void LoadImage()
        {
            try
            {
                if (!File.Exists(ImagePath)) return;

                // For this implementation, we'll create a simple placeholder
                // In a real implementation, you'd load the actual image file
                _loadedImage = CreatePlaceholderImage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Picture Effects] Failed to load image: {ex.Message}");
                _loadedImage = null;
            }
        }

        private ImageBuffer CreatePlaceholderImage()
        {
            // Create a simple pattern as placeholder
            var image = new ImageBuffer(100, 100);
            
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    // Create a simple gradient pattern
                    uint r = (uint)(x * 255 / 100);
                    uint g = (uint)(y * 255 / 100);
                    uint b = (uint)((x + y) * 255 / 200);
                    
                    image.Data[y * 100 + x] = (255u << 24) | (r << 16) | (g << 8) | b;
                }
            }
            
            return image;
        }

        private void ApplyPictureEffect(ImageBuffer output)
        {
            if (_loadedImage == null) return;

            // Calculate effective parameters
            float effectiveScaleX = ScaleX;
            float effectiveScaleY = ScaleY;
            float effectiveRotation = Rotation + _currentRotation;

            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                effectiveScaleX *= (1.0f + (BeatScale - 1.0f) * beatFactor);
                effectiveScaleY *= (1.0f + (BeatScale - 1.0f) * beatFactor);
            }

            // Apply transformations and render
            RenderTransformedImage(output, effectiveScaleX, effectiveScaleY, effectiveRotation);
        }

        private void RenderTransformedImage(ImageBuffer output, float scaleX, float scaleY, float rotation)
        {
            int outputWidth = output.Width;
            int outputHeight = output.Height;
            int imageWidth = _loadedImage.Width;
            int imageHeight = _loadedImage.Height;

            // Calculate center position
            float centerX = PositionX * outputWidth;
            float centerY = PositionY * outputHeight;

            // Rotation in radians
            float radians = rotation * (float)Math.PI / 180.0f;
            float cosAngle = (float)Math.Cos(radians);
            float sinAngle = (float)Math.Sin(radians);

            // Calculate scaled dimensions
            int scaledWidth = (int)(imageWidth * scaleX);
            int scaledHeight = (int)(imageHeight * scaleY);

            for (int y = 0; y < outputHeight; y++)
            {
                for (int x = 0; x < outputWidth; x++)
                {
                    // Transform output coordinates to image coordinates
                    float relX = x - centerX;
                    float relY = y - centerY;

                    // Apply inverse rotation
                    float rotatedX = relX * cosAngle + relY * sinAngle;
                    float rotatedY = -relX * sinAngle + relY * cosAngle;

                    // Apply inverse scaling and get image coordinates
                    int imageX = (int)((rotatedX / scaleX) + imageWidth / 2);
                    int imageY = (int)((rotatedY / scaleY) + imageHeight / 2);

                    // Apply flipping
                    if (FlipHorizontal) imageX = imageWidth - 1 - imageX;
                    if (FlipVertical) imageY = imageHeight - 1 - imageY;

                    // Check bounds
                    if (imageX >= 0 && imageX < imageWidth && imageY >= 0 && imageY < imageHeight)
                    {
                        uint imagePixel = _loadedImage.Data[imageY * imageWidth + imageX];
                        
                        // Apply filter if specified
                        if (FilterType > 0)
                        {
                            imagePixel = ApplyFilter(imagePixel, imageX, imageY);
                        }

                        // Apply tinting
                        if (TintAmount > 0)
                        {
                            imagePixel = ApplyTint(imagePixel);
                        }

                        // Apply opacity
                        if (Opacity < 1.0f)
                        {
                            imagePixel = ApplyOpacity(imagePixel);
                        }

                        // Blend with output
                        uint outputPixel = output.Data[y * outputWidth + x];
                        output.Data[y * outputWidth + x] = BlendPixels(outputPixel, imagePixel);
                    }
                }
            }
        }

        private uint ApplyFilter(uint pixel, int x, int y)
        {
            // Simple filter implementations
            switch (FilterType)
            {
                case 1: // Blur (simple approximation)
                    return ApplyBlur(pixel);

                case 2: // Sharpen
                    return ApplySharpen(pixel);

                case 3: // Edge detection
                    return ApplyEdge(pixel);

                case 4: // Emboss
                    return ApplyEmboss(pixel);

                default:
                    return pixel;
            }
        }

        private uint ApplyBlur(uint pixel)
        {
            // Simple blur by reducing contrast
            uint a = (pixel >> 24) & 0xFF;
            uint r = (pixel >> 16) & 0xFF;
            uint g = (pixel >> 8) & 0xFF;
            uint b = pixel & 0xFF;

            float strength = FilterStrength;
            r = (uint)(r * (1.0f - strength * 0.3f) + 128 * strength * 0.3f);
            g = (uint)(g * (1.0f - strength * 0.3f) + 128 * strength * 0.3f);
            b = (uint)(b * (1.0f - strength * 0.3f) + 128 * strength * 0.3f);

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private uint ApplySharpen(uint pixel)
        {
            // Simple sharpen by increasing contrast
            uint a = (pixel >> 24) & 0xFF;
            uint r = (pixel >> 16) & 0xFF;
            uint g = (pixel >> 8) & 0xFF;
            uint b = pixel & 0xFF;

            float strength = FilterStrength;
            r = (uint)Math.Max(0, Math.Min(255, (r - 128) * (1.0f + strength) + 128));
            g = (uint)Math.Max(0, Math.Min(255, (g - 128) * (1.0f + strength) + 128));
            b = (uint)Math.Max(0, Math.Min(255, (b - 128) * (1.0f + strength) + 128));

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private uint ApplyEdge(uint pixel)
        {
            // Simple edge detection by inverting and reducing brightness
            uint a = (pixel >> 24) & 0xFF;
            uint r = (pixel >> 16) & 0xFF;
            uint g = (pixel >> 8) & 0xFF;
            uint b = pixel & 0xFF;

            uint gray = (uint)(r * 0.299 + g * 0.587 + b * 0.114);
            uint edge = (uint)(255 - gray);
            edge = (uint)(edge * FilterStrength);

            return (a << 24) | (edge << 16) | (edge << 8) | edge;
        }

        private uint ApplyEmboss(uint pixel)
        {
            // Simple emboss effect
            uint a = (pixel >> 24) & 0xFF;
            uint r = (pixel >> 16) & 0xFF;
            uint g = (pixel >> 8) & 0xFF;
            uint b = pixel & 0xFF;

            uint gray = (uint)(r * 0.299 + g * 0.587 + b * 0.114);
            uint emboss = (uint)(128 + (gray - 128) * FilterStrength);
            emboss = Math.Max(0u, Math.Min(255u, emboss));

            return (a << 24) | (emboss << 16) | (emboss << 8) | emboss;
        }

        private uint ApplyTint(uint pixel)
        {
            uint a = (pixel >> 24) & 0xFF;
            uint r = (pixel >> 16) & 0xFF;
            uint g = (pixel >> 8) & 0xFF;
            uint b = pixel & 0xFF;

            float tint = TintAmount;
            r = (uint)(r * (1.0f - tint) + TintColor.R * tint);
            g = (uint)(g * (1.0f - tint) + TintColor.G * tint);
            b = (uint)(b * (1.0f - tint) + TintColor.B * tint);

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private uint ApplyOpacity(uint pixel)
        {
            uint a = (pixel >> 24) & 0xFF;
            a = (uint)(a * Opacity);
            return (a << 24) | (pixel & 0x00FFFFFF);
        }

        private uint BlendPixels(uint background, uint foreground)
        {
            uint bgA = (background >> 24) & 0xFF;
            uint bgR = (background >> 16) & 0xFF;
            uint bgG = (background >> 8) & 0xFF;
            uint bgB = background & 0xFF;

            uint fgA = (foreground >> 24) & 0xFF;
            uint fgR = (foreground >> 16) & 0xFF;
            uint fgG = (foreground >> 8) & 0xFF;
            uint fgB = foreground & 0xFF;

            if (fgA == 0) return background;

            switch (BlendMode)
            {
                case 0: // Replace
                    return foreground;

                case 1: // Add
                    return (Math.Max(bgA, fgA) << 24) |
                           (Math.Min(255u, bgR + fgR) << 16) |
                           (Math.Min(255u, bgG + fgG) << 8) |
                           Math.Min(255u, bgB + fgB);

                case 2: // Multiply
                    return (Math.Max(bgA, fgA) << 24) |
                           ((bgR * fgR / 255) << 16) |
                           ((bgG * fgG / 255) << 8) |
                           (bgB * fgB / 255);

                case 3: // Overlay
                    uint resultR = bgR < 128 ? (2 * bgR * fgR / 255) : (255 - 2 * (255 - bgR) * (255 - fgR) / 255);
                    uint resultG = bgG < 128 ? (2 * bgG * fgG / 255) : (255 - 2 * (255 - bgG) * (255 - fgG) / 255);
                    uint resultB = bgB < 128 ? (2 * bgB * fgB / 255) : (255 - 2 * (255 - bgB) * (255 - fgB) / 255);
                    return (Math.Max(bgA, fgA) << 24) | (resultR << 16) | (resultG << 8) | resultB;

                case 4: // Screen
                    return (Math.Max(bgA, fgA) << 24) |
                           ((255 - (255 - bgR) * (255 - fgR) / 255) << 16) |
                           ((255 - (255 - bgG) * (255 - fgG) / 255) << 8) |
                           (255 - (255 - bgB) * (255 - fgB) / 255);

                default: // Alpha blend
                    float alpha = fgA / 255.0f;
                    return (Math.Max(bgA, fgA) << 24) |
                           ((uint)(bgR * (1 - alpha) + fgR * alpha) << 16) |
                           ((uint)(bgG * (1 - alpha) + fgG * alpha) << 8) |
                           (uint)(bgB * (1 - alpha) + fgB * alpha);
            }
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "ImagePath", ImagePath },
                { "BlendMode", BlendMode },
                { "Opacity", Opacity },
                { "PositionX", PositionX },
                { "PositionY", PositionY },
                { "ScaleX", ScaleX },
                { "ScaleY", ScaleY },
                { "Rotation", Rotation },
                { "BeatReactive", BeatReactive },
                { "BeatScale", BeatScale },
                { "BeatRotation", BeatRotation },
                { "FilterType", FilterType },
                { "FilterStrength", FilterStrength },
                { "FlipHorizontal", FlipHorizontal },
                { "FlipVertical", FlipVertical },
                { "TintColor", TintColor },
                { "TintAmount", TintAmount }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            if (config.TryGetValue("ImagePath", out var imagePath))
                ImagePath = imagePath.ToString();
            if (config.TryGetValue("BlendMode", out var blendMode))
                BlendMode = Convert.ToInt32(blendMode);
            if (config.TryGetValue("Opacity", out var opacity))
                Opacity = Convert.ToSingle(opacity);
            if (config.TryGetValue("PositionX", out var posX))
                PositionX = Convert.ToSingle(posX);
            if (config.TryGetValue("PositionY", out var posY))
                PositionY = Convert.ToSingle(posY);
            if (config.TryGetValue("ScaleX", out var scaleX))
                ScaleX = Convert.ToSingle(scaleX);
            if (config.TryGetValue("ScaleY", out var scaleY))
                ScaleY = Convert.ToSingle(scaleY);
            if (config.TryGetValue("Rotation", out var rotation))
                Rotation = Convert.ToSingle(rotation);
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            if (config.TryGetValue("BeatScale", out var beatScale))
                BeatScale = Convert.ToSingle(beatScale);
            if (config.TryGetValue("BeatRotation", out var beatRotation))
                BeatRotation = Convert.ToSingle(beatRotation);
            if (config.TryGetValue("FilterType", out var filterType))
                FilterType = Convert.ToInt32(filterType);
            if (config.TryGetValue("FilterStrength", out var filterStrength))
                FilterStrength = Convert.ToSingle(filterStrength);
            if (config.TryGetValue("FlipHorizontal", out var flipH))
                FlipHorizontal = Convert.ToBoolean(flipH);
            if (config.TryGetValue("FlipVertical", out var flipV))
                FlipVertical = Convert.ToBoolean(flipV);
            if (config.TryGetValue("TintColor", out var tintColor))
                TintColor = (Color)tintColor;
            if (config.TryGetValue("TintAmount", out var tintAmount))
                TintAmount = Convert.ToSingle(tintAmount);
        }
    }
}