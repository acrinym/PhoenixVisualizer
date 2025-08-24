using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class BlitEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Blit effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Source X coordinate for the blit operation
        /// </summary>
        public int SourceX { get; set; } = 0;

        /// <summary>
        /// Source Y coordinate for the blit operation
        /// </summary>
        public int SourceY { get; set; } = 0;

        /// <summary>
        /// Destination X coordinate for the blit operation
        /// </summary>
        public int DestX { get; set; } = 0;

        /// <summary>
        /// Destination Y coordinate for the blit operation
        /// </summary>
        public int DestY { get; set; } = 0;

        /// <summary>
        /// Width of the blitted region
        /// </summary>
        public int Width { get; set; } = 100;

        /// <summary>
        /// Height of the blitted region
        /// </summary>
        public int Height { get; set; } = 100;

        /// <summary>
        /// Rotation angle in degrees
        /// </summary>
        public float Rotation { get; set; } = 0.0f;

        /// <summary>
        /// Alpha transparency value (0.0 to 1.0)
        /// </summary>
        public float Alpha { get; set; } = 1.0f;

        /// <summary>
        /// Source buffer index for multi-buffer operations
        /// </summary>
        public int SourceBuffer { get; set; } = 0;

        /// <summary>
        /// Blit operation mode
        /// </summary>
        public BlitMode Mode { get; set; } = BlitMode.Copy;

        /// <summary>
        /// Blend mode for combining pixels
        /// </summary>
        public BlitBlendMode BlendMode { get; set; } = BlitBlendMode.Normal;

        /// <summary>
        /// Enable beat-reactive alpha changes
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat alpha multiplier
        /// </summary>
        public float BeatAlpha { get; set; } = 1.5f;

        #endregion

        #region Enums

        public enum BlitMode
        {
            Copy = 0,        // Direct copy
            Stretch = 1,     // Scale to fit destination
            Tile = 2,        // Repeat pattern
            Mirror = 3,      // Flip horizontally/vertically
            Rotate = 4,      // Apply rotation
            AlphaBlend = 5   // Use alpha channel
        }

        public enum BlitBlendMode
        {
            Normal = 0,      // Replace
            Add = 1,         // Brighten
            Multiply = 2,    // Darken
            Screen = 3,      // Lighten
            Overlay = 4,     // Contrast
            SoftLight = 5,   // Soft light
            HardLight = 6,   // Hard light
            ColorDodge = 7,  // Color dodge
            ColorBurn = 8,   // Color burn
            Difference = 9,  // Difference
            Exclusion = 10   // Exclusion
        }

        #endregion

        #region Private Fields

        private int lastWidth = 0;
        private int lastHeight = 0;
        private ImageBuffer sourceBuffer;
        private ImageBuffer destinationBuffer;
        private bool isInitialized = false;

        #endregion

        #region Constructor

        public BlitEffectsNode()
        {
            Name = "Blit Effects";
            Description = "Fundamental AVS rendering effects for copying image data with blending and transformations";
            Category = "AVS Effects";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null, "Input image buffer"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable effect"));
            _inputPorts.Add(new EffectPort("SourceX", typeof(int), false, 0, "Source X coordinate"));
            _inputPorts.Add(new EffectPort("SourceY", typeof(int), false, 0, "Source Y coordinate"));
            _inputPorts.Add(new EffectPort("DestX", typeof(int), false, 0, "Destination X coordinate"));
            _inputPorts.Add(new EffectPort("DestY", typeof(int), false, 0, "Destination Y coordinate"));
            _inputPorts.Add(new EffectPort("Width", typeof(int), false, 100, "Region width"));
            _inputPorts.Add(new EffectPort("Height", typeof(int), false, 100, "Region height"));
            _inputPorts.Add(new EffectPort("Rotation", typeof(float), false, 0.0f, "Rotation angle in degrees"));
            _inputPorts.Add(new EffectPort("Alpha", typeof(float), false, 1.0f, "Alpha transparency (0.0-1.0)"));
            _inputPorts.Add(new EffectPort("SourceBuffer", typeof(int), false, 0, "Source buffer index"));
            _inputPorts.Add(new EffectPort("Mode", typeof(BlitMode), false, BlitMode.Copy, "Blit operation mode"));
            _inputPorts.Add(new EffectPort("BlendMode", typeof(BlitBlendMode), false, BlitBlendMode.Normal, "Blend mode"));
            _inputPorts.Add(new EffectPort("BeatReactive", typeof(bool), false, false, "Enable beat-reactive alpha"));
            _inputPorts.Add(new EffectPort("BeatAlpha", typeof(float), false, 1.5f, "Beat alpha multiplier"));
            
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), true, null, "Blitted output image"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) return inputs["Input"];

            var input = inputs["Input"] as ImageBuffer;
            if (input == null) return inputs["Input"];

            int width = input.Width;
            int height = input.Height;

            // Initialize buffers if dimensions change
            if (lastWidth != width || lastHeight != height)
            {
                InitializeBuffers(width, height);
            }

            // Create output buffer
            var output = new ImageBuffer(width, height);
            
            // Copy input to output first
            Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

            // Calculate current alpha with beat response
            float currentAlpha = CalculateCurrentAlpha(audioFeatures);

            // Apply blit operation based on mode
            switch (Mode)
            {
                case BlitMode.Copy:
                    PerformCopy(output, currentAlpha);
                    break;
                case BlitMode.Stretch:
                    PerformStretch(output, currentAlpha);
                    break;
                case BlitMode.Tile:
                    PerformTile(output, currentAlpha);
                    break;
                case BlitMode.Mirror:
                    PerformMirror(output, currentAlpha);
                    break;
                case BlitMode.Rotate:
                    PerformRotate(output, currentAlpha);
                    break;
                case BlitMode.AlphaBlend:
                    PerformAlphaBlend(output, currentAlpha);
                    break;
            }

            return output;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the source region for blitting
        /// </summary>
        public void SetSourceRegion(int x, int y, int width, int height)
        {
            SourceX = Math.Max(0, x);
            SourceY = Math.Max(0, y);
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
        }

        /// <summary>
        /// Sets the destination region for blitting
        /// </summary>
        public void SetDestinationRegion(int x, int y, int width, int height)
        {
            DestX = Math.Max(0, x);
            DestY = Math.Max(0, y);
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
        }

        /// <summary>
        /// Sets the rotation angle
        /// </summary>
        public void SetRotation(float angle)
        {
            Rotation = angle % 360.0f;
            if (Rotation < 0) Rotation += 360.0f;
        }

        /// <summary>
        /// Sets the alpha transparency
        /// </summary>
        public void SetAlpha(float alpha)
        {
            Alpha = Math.Max(0.0f, Math.Min(1.0f, alpha));
        }

        #endregion

        #region Private Methods

        private void InitializeBuffers(int width, int height)
        {
            lastWidth = width;
            lastHeight = height;
            sourceBuffer = new ImageBuffer(width, height);
            destinationBuffer = new ImageBuffer(width, height);
            isInitialized = true;
        }

        private float CalculateCurrentAlpha(AudioFeatures audioFeatures)
        {
            float currentAlpha = Alpha;
            
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                currentAlpha *= BeatAlpha;
            }
            
            return Math.Max(0.0f, Math.Min(1.0f, currentAlpha));
        }

        private void PerformCopy(ImageBuffer imageBuffer, float alpha)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Validate bounds
            if (SourceX >= width || SourceY >= height || 
                DestX >= width || DestY >= height)
                return;

            int copyWidth = Math.Min(Width, width - Math.Max(SourceX, DestX));
            int copyHeight = Math.Min(Height, height - Math.Max(SourceY, DestY));

            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    int sourceX = SourceX + x;
                    int sourceY = SourceY + y;
                    int destX = DestX + x;
                    int destY = DestY + y;

                    if (sourceX < width && sourceY < height && 
                        destX < width && destY < height)
                    {
                        int sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                        int destColor = imageBuffer.GetPixel(destX, destY);
                        
                        int finalColor = ApplyBlendMode(destColor, sourceColor, BlendMode, alpha);
                        imageBuffer.SetPixel(destX, destY, finalColor);
                    }
                }
            }
        }

        private void PerformStretch(ImageBuffer imageBuffer, float alpha)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Calculate scaling factors
            float scaleX = (float)Width / Math.Max(1, width - SourceX);
            float scaleY = (float)Height / Math.Max(1, height - SourceY);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int sourceX = SourceX + (int)(x / scaleX);
                    int sourceY = SourceY + (int)(y / scaleY);
                    int destX = DestX + x;
                    int destY = DestY + y;

                    if (sourceX < width && sourceY < height && 
                        destX < width && destY < height)
                    {
                        int sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                        int destColor = imageBuffer.GetPixel(destX, destY);
                        
                        int finalColor = ApplyBlendMode(destColor, sourceColor, BlendMode, alpha);
                        imageBuffer.SetPixel(destX, destY, finalColor);
                    }
                }
            }
        }

        private void PerformTile(ImageBuffer imageBuffer, float alpha)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int sourceX = SourceX + (x % Width);
                    int sourceY = SourceY + (y % Height);
                    int destX = DestX + x;
                    int destY = DestY + y;

                    if (sourceX < width && sourceY < height && 
                        destX < width && destY < height)
                    {
                        int sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                        int destColor = imageBuffer.GetPixel(destX, destY);
                        
                        int finalColor = ApplyBlendMode(destColor, sourceColor, BlendMode, alpha);
                        imageBuffer.SetPixel(destX, destY, finalColor);
                    }
                }
            }
        }

        private void PerformMirror(ImageBuffer imageBuffer, float alpha)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int sourceX = SourceX + (Width - 1 - x);
                    int sourceY = SourceY + y;
                    int destX = DestX + x;
                    int destY = DestY + y;

                    if (sourceX < width && sourceY < height && 
                        destX < width && destY < height)
                    {
                        int sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                        int destColor = imageBuffer.GetPixel(destX, destY);
                        
                        int finalColor = ApplyBlendMode(destColor, sourceColor, BlendMode, alpha);
                        imageBuffer.SetPixel(destX, destY, finalColor);
                    }
                }
            }
        }

        private void PerformRotate(ImageBuffer imageBuffer, float alpha)
        {
            if (Math.Abs(Rotation) < 0.1f)
            {
                PerformCopy(imageBuffer, alpha);
                return;
            }

            int width = imageBuffer.Width;
            int height = imageBuffer.Height;
            double radians = Rotation * Math.PI / 180.0;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            // Calculate center of rotation
            int centerX = SourceX + Width / 2;
            int centerY = SourceY + Height / 2;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    // Calculate rotated source coordinates
                    int relX = x - Width / 2;
                    int relY = y - Height / 2;
                    
                    int sourceX = (int)(centerX + relX * cos - relY * sin);
                    int sourceY = (int)(centerY + relX * sin + relY * cos);
                    
                    int destX = DestX + x;
                    int destY = DestY + y;

                    if (sourceX >= 0 && sourceX < width && sourceY >= 0 && sourceY < height &&
                        destX < width && destY < height)
                    {
                        int sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                        int destColor = imageBuffer.GetPixel(destX, destY);
                        
                        int finalColor = ApplyBlendMode(destColor, sourceColor, BlendMode, alpha);
                        imageBuffer.SetPixel(destX, destY, finalColor);
                    }
                }
            }
        }

        private void PerformAlphaBlend(ImageBuffer imageBuffer, float alpha)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int sourceX = SourceX + x;
                    int sourceY = SourceY + y;
                    int destX = DestX + x;
                    int destY = DestY + y;

                    if (sourceX < width && sourceY < height && 
                        destX < width && destY < height)
                    {
                        int sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                        int destColor = imageBuffer.GetPixel(destX, destY);
                        
                        // Use source alpha for blending
                        float sourceAlpha = ((sourceColor >> 24) & 0xFF) / 255.0f;
                        int finalColor = ApplyBlendMode(destColor, sourceColor, BlendMode, sourceAlpha * alpha);
                        imageBuffer.SetPixel(destX, destY, finalColor);
                    }
                }
            }
        }

        private int ApplyBlendMode(int dest, int source, BlitBlendMode mode, float alpha)
        {
            if (alpha <= 0.0f) return dest;
            if (alpha >= 1.0f) alpha = 1.0f;

            int r1 = (dest >> 16) & 0xFF;
            int g1 = (dest >> 8) & 0xFF;
            int b1 = dest & 0xFF;
            
            int r2 = (source >> 16) & 0xFF;
            int g2 = (source >> 8) & 0xFF;
            int b2 = source & 0xFF;

            int finalR, finalG, finalB;
            
            switch (mode)
            {
                case BlitBlendMode.Add:
                    finalR = Math.Min(255, r1 + (int)(r2 * alpha));
                    finalG = Math.Min(255, g1 + (int)(g2 * alpha));
                    finalB = Math.Min(255, b1 + (int)(b2 * alpha));
                    break;
                    
                case BlitBlendMode.Multiply:
                    finalR = (int)((r1 * r2 * alpha) / 255.0f);
                    finalG = (int)((g1 * g2 * alpha) / 255.0f);
                    finalB = (int)((b1 * b2 * alpha) / 255.0f);
                    break;
                    
                case BlitBlendMode.Screen:
                    finalR = (int)(255 - ((255 - r1) * (255 - r2) * alpha) / 255.0f);
                    finalG = (int)(255 - ((255 - g1) * (255 - g2) * alpha) / 255.0f);
                    finalB = (int)(255 - ((255 - b1) * (255 - b2) * alpha) / 255.0f);
                    break;
                    
                case BlitBlendMode.Overlay:
                    finalR = ApplyOverlay(r1, r2, alpha);
                    finalG = ApplyOverlay(g1, g2, alpha);
                    finalB = ApplyOverlay(b1, b2, alpha);
                    break;
                    
                case BlitBlendMode.SoftLight:
                    finalR = ApplySoftLight(r1, r2, alpha);
                    finalG = ApplySoftLight(g1, g2, alpha);
                    finalB = ApplySoftLight(b1, b2, alpha);
                    break;
                    
                case BlitBlendMode.HardLight:
                    finalR = ApplyHardLight(r1, r2, alpha);
                    finalG = ApplyHardLight(g1, g2, alpha);
                    finalB = ApplyHardLight(b1, b2, alpha);
                    break;
                    
                case BlitBlendMode.ColorDodge:
                    finalR = ApplyColorDodge(r1, r2, alpha);
                    finalG = ApplyColorDodge(g1, g2, alpha);
                    finalB = ApplyColorDodge(b1, b2, alpha);
                    break;
                    
                case BlitBlendMode.ColorBurn:
                    finalR = ApplyColorBurn(r1, r2, alpha);
                    finalG = ApplyColorBurn(g1, g2, alpha);
                    finalB = ApplyColorBurn(b1, b2, alpha);
                    break;
                    
                case BlitBlendMode.Difference:
                    finalR = (int)(Math.Abs(r1 - r2) * alpha + r1 * (1.0f - alpha));
                    finalG = (int)(Math.Abs(g1 - g2) * alpha + g1 * (1.0f - alpha));
                    finalB = (int)(Math.Abs(b1 - b2) * alpha + b1 * (1.0f - alpha));
                    break;
                    
                case BlitBlendMode.Exclusion:
                    finalR = (int)((r1 + r2 - 2 * r1 * r2 / 255) * alpha + r1 * (1.0f - alpha));
                    finalG = (int)((g1 + g2 - 2 * g1 * g2 / 255) * alpha + g1 * (1.0f - alpha));
                    finalB = (int)((b1 + b2 - 2 * b1 * b2 / 255) * alpha + b1 * (1.0f - alpha));
                    break;
                    
                default: // Normal
                    finalR = (int)(r1 * (1.0f - alpha) + r2 * alpha);
                    finalG = (int)(g1 * (1.0f - alpha) + g2 * alpha);
                    finalB = (int)(b1 * (1.0f - alpha) + b2 * alpha);
                    break;
            }
            
            int alphaChannel = dest & 0xFF000000;
            int redChannel = (Math.Max(0, Math.Min(255, finalR)) << 16;
            int greenChannel = (Math.Max(0, Math.Min(255, finalG)) << 8;
            int blueChannel = Math.Max(0, Math.Min(255, finalB));
            
            return alphaChannel | redChannel | greenChannel | blueChannel;
        }

        private int ApplyOverlay(int baseColor, int blendColor, float alpha)
        {
            if (baseColor < 128)
                return (int)((2 * baseColor * blendColor / 255) * alpha + baseColor * (1.0f - alpha));
            else
                return (int)((255 - 2 * (255 - baseColor) * (255 - blendColor) / 255) * alpha + baseColor * (1.0f - alpha));
        }

        private int ApplySoftLight(int baseColor, int blendColor, float alpha)
        {
            if (blendColor < 128)
                return (int)((baseColor * blendColor / 255 + baseColor * blendColor / 255) * alpha + baseColor * (1.0f - alpha));
            else
                return (int)((baseColor + (255 - baseColor) * (blendColor - 128) / 128) * alpha + baseColor * (1.0f - alpha));
        }

        private int ApplyHardLight(int baseColor, int blendColor, float alpha)
        {
            if (baseColor < 128)
                return (int)((2 * baseColor * blendColor / 255) * alpha + baseColor * (1.0f - alpha));
            else
                return (int)((255 - 2 * (255 - baseColor) * (255 - blendColor) / 255) * alpha + baseColor * (1.0f - alpha));
        }

        private int ApplyColorDodge(int baseColor, int blendColor, float alpha)
        {
            if (blendColor == 255)
                return (int)(255 * alpha + baseColor * (1.0f - alpha));
            else
                return (int)(Math.Min(255, baseColor * 255 / (255 - blendColor)) * alpha + baseColor * (1.0f - alpha));
        }

        private int ApplyColorBurn(int baseColor, int blendColor, float alpha)
        {
            if (blendColor == 0)
                return (int)(0 * alpha + baseColor * (1.0f - alpha));
            else
                return (int)(Math.Max(0, 255 - (255 - baseColor) * 255 / blendColor) * alpha + baseColor * (1.0f - alpha));
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        public override bool ValidateConfiguration()
        {
            Width = Math.Max(1, Width);
            Height = Math.Max(1, Height);
            Alpha = Math.Max(0.0f, Math.Min(1.0f, Alpha));
            BeatAlpha = Math.Max(0.0f, BeatAlpha);

            return true;
        }

        /// <summary>
        /// Returns a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            return $"Blit Effect: {(Enabled ? "Enabled" : "Disabled")}, " +
                   $"Mode: {Mode}, Blend: {BlendMode}, " +
                   $"Source: ({SourceX},{SourceY}) {Width}x{Height}, " +
                   $"Dest: ({DestX},{DestY}), " +
                   $"Rotation: {Rotation:F1}°, Alpha: {Alpha:F2}";
        }

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
