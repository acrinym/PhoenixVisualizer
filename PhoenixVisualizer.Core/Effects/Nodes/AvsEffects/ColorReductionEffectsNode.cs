using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ColorReductionEffectsNode : BaseEffectNode
    {
        // Core properties
        public int ReductionLevel { get; set; } = 256; // Number of colors to reduce to
        public int ReductionMethod { get; set; } = 0; // 0=Uniform, 1=Median Cut, 2=Octree, 3=K-Means, 4=Popularity, 5=Adaptive
        public bool EnableDithering { get; set; } = false;
        public int DitheringType { get; set; } = 0; // 0=None, 1=Floyd-Steinberg, 2=Ordered, 3=Random, 4=Bayer
        public int PaletteType { get; set; } = 0; // 0=Grayscale, 1=RGB, 2=CMY, 3=Custom, 4=Adaptive, 5=Retro
        public bool BeatReactive { get; set; } = false;
        public int BeatReductionLevel { get; set; } = 64;
        public float DitheringStrength { get; set; } = 1.0f;
        public int[]? CustomPalette { get; set; } = null;
        public bool PreserveBrightness { get; set; } = true;

        // Internal state
        private int[]? _currentPalette;
        private readonly object _paletteLock = new object();
        private readonly Random _random = new Random();

        // Performance optimization constants
        private const int MaxReductionLevel = 256;
        private const int MinReductionLevel = 2;

        public ColorReductionEffectsNode()
        {
            Name = "Color Reduction Effects";
            Description = "Advanced color reduction with multiple quantization methods and dithering";
            Category = "Color Transformation";
            _currentPalette = GenerateGrayscalePalette(64); // Initialize with default palette
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for color reduction"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Color reduced output image"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            // Determine current reduction level
            int currentLevel = ReductionLevel;
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                currentLevel = BeatReductionLevel;
            }

            // Clamp reduction level to valid range
            currentLevel = Math.Clamp(currentLevel, MinReductionLevel, MaxReductionLevel);

            // Generate or select color palette
            int[] palette = GeneratePalette(currentLevel);

            // Apply color reduction based on selected method
            switch (ReductionMethod)
            {
                case 0: // Uniform Quantization
                    ApplyUniformQuantization(imageBuffer, output, palette);
                    break;
                case 1: // Median Cut
                    ApplyMedianCutQuantization(imageBuffer, output, palette);
                    break;
                case 2: // Octree Quantization
                    ApplyOctreeQuantization(imageBuffer, output, palette);
                    break;
                case 3: // K-Means Clustering
                    ApplyKMeansQuantization(imageBuffer, output, palette);
                    break;
                case 4: // Popularity Algorithm
                    ApplyPopularityQuantization(imageBuffer, output, palette);
                    break;
                case 5: // Adaptive Quantization
                    ApplyAdaptiveQuantization(imageBuffer, output, palette);
                    break;
                default:
                    ApplyUniformQuantization(imageBuffer, output, palette);
                    break;
            }

            return output;
        }

        private int[] GeneratePalette(int level)
        {
            lock (_paletteLock)
            {
                if (_currentPalette != null && _currentPalette.Length == level)
                    return _currentPalette;

                switch (PaletteType)
                {
                    case 0: // Grayscale
                        _currentPalette = GenerateGrayscalePalette(level);
                        break;
                    case 1: // RGB
                        _currentPalette = GenerateRgbPalette(level);
                        break;
                    case 2: // CMY
                        _currentPalette = GenerateCmyPalette(level);
                        break;
                    case 3: // Custom
                        _currentPalette = CustomPalette ?? GenerateGrayscalePalette(level);
                        break;
                    case 4: // Adaptive
                        _currentPalette = GenerateAdaptivePalette(level);
                        break;
                    case 5: // Retro
                        _currentPalette = GenerateRetroPalette(level);
                        break;
                    default:
                        _currentPalette = GenerateGrayscalePalette(level);
                        break;
                }

                return _currentPalette;
            }
        }

        private int[] GenerateGrayscalePalette(int level)
        {
            var palette = new int[level];
            int step = 256 / (level - 1);

            for (int i = 0; i < level; i++)
            {
                int intensity = Math.Min(i * step, 255);
                palette[i] = intensity | (intensity << 8) | (intensity << 16);
            }

            return palette;
        }

        private int[] GenerateRgbPalette(int level)
        {
            var palette = new int[level];
            int colorsPerChannel = (int)Math.Ceiling(Math.Pow(level, 1.0 / 3.0));
            int step = 256 / colorsPerChannel;

            int index = 0;
            for (int r = 0; r < colorsPerChannel && index < level; r++)
            {
                for (int g = 0; g < colorsPerChannel && index < level; g++)
                {
                    for (int b = 0; b < colorsPerChannel && index < level; b++)
                    {
                        int red = Math.Min(r * step, 255);
                        int green = Math.Min(g * step, 255);
                        int blue = Math.Min(b * step, 255);

                        palette[index] = red | (green << 8) | (blue << 16);
                        index++;
                    }
                }
            }

            return palette;
        }

        private int[] GenerateCmyPalette(int level)
        {
            var palette = new int[level];
            int step = 256 / (level - 1);

            for (int i = 0; i < level; i++)
            {
                int intensity = Math.Min(i * step, 255);
                int cyan = 255 - intensity;
                int magenta = 255 - intensity;
                int yellow = 255 - intensity;
                palette[i] = cyan | (magenta << 8) | (yellow << 16);
            }

            return palette;
        }

        private int[] GenerateAdaptivePalette(int level)
        {
            // Simple adaptive palette - could be enhanced with actual image analysis
            var palette = new int[level];
            int step = 256 / (level - 1);

            for (int i = 0; i < level; i++)
            {
                int intensity = Math.Min(i * step, 255);
                int r = intensity;
                int g = (intensity + 85) % 256;
                int b = (intensity + 170) % 256;
                palette[i] = r | (g << 8) | (b << 16);
            }

            return palette;
        }

        private int[] GenerateRetroPalette(int level)
        {
            // Classic retro color schemes
            var baseColors = new int[]
            {
                0x000000, 0xFFFFFF, 0xFF0000, 0x00FF00, 0x0000FF,
                0xFFFF00, 0xFF00FF, 0x00FFFF, 0xFF8000, 0x8000FF,
                0x00FF80, 0xFF0080, 0x80FF00, 0x0080FF, 0xFF8080
            };

            var palette = new int[level];
            for (int i = 0; i < level; i++)
            {
                palette[i] = baseColors[i % baseColors.Length];
            }

            return palette;
        }

        private void ApplyUniformQuantization(ImageBuffer source, ImageBuffer output, int[] palette)
        {
            int paletteSize = palette.Length;
            int step = 256 / paletteSize;

            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int pixel = source.GetPixel(x, y);
                    int reducedPixel = QuantizePixel(pixel, palette, step);

                    if (EnableDithering)
                    {
                        reducedPixel = ApplyDithering(source, output, x, y, pixel, reducedPixel);
                    }

                    output.SetPixel(x, y, reducedPixel);
                }
            }
        }

        private void ApplyMedianCutQuantization(ImageBuffer source, ImageBuffer output, int[] palette)
        {
            // Collect all unique colors
            var uniqueColors = new HashSet<int>();
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    uniqueColors.Add(source.GetPixel(x, y));
                }
            }

            // Apply median cut algorithm
            var optimizedPalette = MedianCut(uniqueColors.ToArray(), palette.Length);

            // Apply quantization with optimized palette
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int pixel = source.GetPixel(x, y);
                    int reducedPixel = FindClosestPaletteColor(pixel, optimizedPalette);

                    if (EnableDithering)
                    {
                        reducedPixel = ApplyDithering(source, output, x, y, pixel, reducedPixel);
                    }

                    output.SetPixel(x, y, reducedPixel);
                }
            }
        }

        private void ApplyOctreeQuantization(ImageBuffer source, ImageBuffer output, int[] palette)
        {
            // Simplified octree quantization
            var octree = new Octree();
            
            // Build octree from source image
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    int pixel = source.GetPixel(x, y);
                    octree.AddColor(pixel);
                }
            }

            // Reduce octree to target palette size
            var optimizedPalette = octree.ReduceToPalette(palette.Length);

            // Apply quantization
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int pixel = source.GetPixel(x, y);
                    int reducedPixel = FindClosestPaletteColor(pixel, optimizedPalette);

                    if (EnableDithering)
                    {
                        reducedPixel = ApplyDithering(source, output, x, y, pixel, reducedPixel);
                    }

                    output.SetPixel(x, y, reducedPixel);
                }
            }
        }

        private void ApplyKMeansQuantization(ImageBuffer source, ImageBuffer output, int[] palette)
        {
            // Simplified K-means clustering
            var centroids = new List<int>();

            // Initialize centroids from palette
            for (int i = 0; i < Math.Min(palette.Length, 8); i++)
            {
                centroids.Add(palette[i]);
            }

            // Simple clustering (could be enhanced with full K-means)
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int pixel = source.GetPixel(x, y);
                    int reducedPixel = FindClosestPaletteColor(pixel, centroids.ToArray());

                    if (EnableDithering)
                    {
                        reducedPixel = ApplyDithering(source, output, x, y, pixel, reducedPixel);
                    }

                    output.SetPixel(x, y, reducedPixel);
                }
            }
        }

        private void ApplyPopularityQuantization(ImageBuffer source, ImageBuffer output, int[] palette)
        {
            // Count color frequencies
            var colorCounts = new Dictionary<int, int>();
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    int pixel = source.GetPixel(x, y);
                    if (colorCounts.ContainsKey(pixel))
                        colorCounts[pixel]++;
                    else
                        colorCounts[pixel] = 1;
                }
            }

            // Sort by popularity and take top colors
            var popularColors = colorCounts.OrderByDescending(kvp => kvp.Value)
                                         .Take(palette.Length)
                                         .Select(kvp => kvp.Key)
                                         .ToArray();

            // Apply quantization
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int pixel = source.GetPixel(x, y);
                    int reducedPixel = FindClosestPaletteColor(pixel, popularColors);

                    if (EnableDithering)
                    {
                        reducedPixel = ApplyDithering(source, output, x, y, pixel, reducedPixel);
                    }

                    output.SetPixel(x, y, reducedPixel);
                }
            }
        }

        private void ApplyAdaptiveQuantization(ImageBuffer source, ImageBuffer output, int[] palette)
        {
            // Adaptive quantization based on image content
            var adaptivePalette = GenerateAdaptivePalette(palette.Length);

            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int pixel = source.GetPixel(x, y);
                    int reducedPixel = FindClosestPaletteColor(pixel, adaptivePalette);

                    if (EnableDithering)
                    {
                        reducedPixel = ApplyDithering(source, output, x, y, pixel, reducedPixel);
                    }

                    output.SetPixel(x, y, reducedPixel);
                }
            }
        }

        private int QuantizePixel(int pixel, int[] palette, int step)
        {
            int r = pixel & 0xFF;
            int g = (pixel >> 8) & 0xFF;
            int b = (pixel >> 16) & 0xFF;

            // Quantize each channel
            int quantizedR = (r / step) * step;
            int quantizedG = (g / step) * step;
            int quantizedB = (b / step) * step;

            // Find closest palette color
            int closestColor = FindClosestPaletteColor(quantizedR, quantizedG, quantizedB, palette);

            return closestColor;
        }

        private int FindClosestPaletteColor(int r, int g, int b, int[] palette)
        {
            int closestColor = palette[0];
            int minDistance = int.MaxValue;

            foreach (int paletteColor in palette)
            {
                int pr = paletteColor & 0xFF;
                int pg = (paletteColor >> 8) & 0xFF;
                int pb = (paletteColor >> 16) & 0xFF;

                // Calculate Euclidean distance
                int distance = (r - pr) * (r - pr) + (g - pg) * (g - pg) + (b - pb) * (b - pb);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestColor = paletteColor;
                }
            }

            return closestColor;
        }

        private int FindClosestPaletteColor(int pixel, int[] palette)
        {
            int r = pixel & 0xFF;
            int g = (pixel >> 8) & 0xFF;
            int b = (pixel >> 16) & 0xFF;

            return FindClosestPaletteColor(r, g, b, palette);
        }

        private int ApplyDithering(ImageBuffer source, ImageBuffer output, int x, int y, int originalPixel, int quantizedPixel)
        {
            if (!EnableDithering)
                return quantizedPixel;

            switch (DitheringType)
            {
                case 1: // Floyd-Steinberg
                    return ApplyFloydSteinbergDithering(source, output, x, y, originalPixel, quantizedPixel);
                case 2: // Ordered
                    return ApplyOrderedDithering(x, y, originalPixel, quantizedPixel);
                case 3: // Random
                    return ApplyRandomDithering(originalPixel, quantizedPixel);
                case 4: // Bayer
                    return ApplyBayerDithering(x, y, originalPixel, quantizedPixel);
                default:
                    return quantizedPixel;
            }
        }

        private int ApplyFloydSteinbergDithering(ImageBuffer source, ImageBuffer output, int x, int y, int originalPixel, int quantizedPixel)
        {
            // Calculate quantization error
            int errorR = (originalPixel & 0xFF) - (quantizedPixel & 0xFF);
            int errorG = ((originalPixel >> 8) & 0xFF) - ((quantizedPixel >> 8) & 0xFF);
            int errorB = ((originalPixel >> 16) & 0xFF) - ((quantizedPixel >> 16) & 0xFF);

            // Distribute error to neighboring pixels (Floyd-Steinberg)
            if (x + 1 < source.Width)
            {
                DistributeError(output, x + 1, y, errorR, errorG, errorB, 7.0f / 16.0f);
            }

            if (x - 1 >= 0 && y + 1 < source.Height)
            {
                DistributeError(output, x - 1, y + 1, errorR, errorG, errorB, 3.0f / 16.0f);
            }

            if (y + 1 < source.Height)
            {
                DistributeError(output, x, y + 1, errorR, errorG, errorB, 5.0f / 16.0f);
            }

            if (x + 1 < source.Width && y + 1 < source.Height)
            {
                DistributeError(output, x + 1, y + 1, errorR, errorG, errorB, 1.0f / 16.0f);
            }

            return quantizedPixel;
        }

        private int ApplyOrderedDithering(int x, int y, int originalPixel, int quantizedPixel)
        {
            // Simple ordered dithering with Bayer matrix
            var bayerMatrix = new int[,] { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };
            int threshold = bayerMatrix[x % 4, y % 4] * 16;

            int r = originalPixel & 0xFF;
            int g = (originalPixel >> 8) & 0xFF;
            int b = (originalPixel >> 16) & 0xFF;

            if (r > threshold) r = Math.Min(255, r + 32);
            if (g > threshold) g = Math.Min(255, g + 32);
            if (b > threshold) b = Math.Min(255, b + 16);

            return r | (g << 8) | (b << 16);
        }

        private int ApplyRandomDithering(int originalPixel, int quantizedPixel)
        {
            if (_random.NextDouble() < DitheringStrength * 0.1f)
            {
                return quantizedPixel;
            }
            return originalPixel;
        }

        private int ApplyBayerDithering(int x, int y, int originalPixel, int quantizedPixel)
        {
            // 8x8 Bayer matrix for better dithering
            var bayerMatrix = new int[,] {
                { 0, 48, 12, 60, 3, 51, 15, 63 },
                { 32, 16, 44, 28, 35, 19, 47, 31 },
                { 8, 56, 4, 52, 11, 59, 7, 55 },
                { 40, 24, 36, 20, 43, 27, 39, 23 },
                { 2, 50, 14, 62, 1, 49, 13, 61 },
                { 34, 18, 46, 30, 33, 17, 45, 29 },
                { 10, 58, 6, 54, 9, 57, 5, 53 },
                { 42, 26, 38, 22, 41, 25, 37, 21 }
            };

            int threshold = bayerMatrix[x % 8, y % 8] * 4;
            int r = originalPixel & 0xFF;
            int g = (originalPixel >> 8) & 0xFF;
            int b = (originalPixel >> 16) & 0xFF;

            if (r > threshold) r = Math.Min(255, r + 16);
            if (g > threshold) g = Math.Min(255, g + 16);
            if (b > threshold) b = Math.Min(255, b + 16);

            return r | (g << 8) | (b << 16);
        }

        private void DistributeError(ImageBuffer output, int x, int y, int errorR, int errorG, int errorB, float factor)
        {
            int pixel = output.GetPixel(x, y);

            int r = Math.Clamp((pixel & 0xFF) + (int)(errorR * factor), 0, 255);
            int g = Math.Clamp(((pixel >> 8) & 0xFF) + (int)(errorG * factor), 0, 255);
            int b = Math.Clamp(((pixel >> 16) & 0xFF) + (int)(errorB * factor), 0, 255);

            int newPixel = r | (g << 8) | (b << 16);
            output.SetPixel(x, y, newPixel);
        }

        private int[] MedianCut(int[] colors, int targetSize)
        {
            // Simplified median cut algorithm
            if (colors.Length <= targetSize)
                return colors;

            var result = new int[targetSize];
            var colorList = new List<int>(colors);

            for (int i = 0; i < targetSize; i++)
            {
                if (colorList.Count == 0) break;

                // Find median color
                int medianIndex = colorList.Count / 2;
                result[i] = colorList[medianIndex];
                colorList.RemoveAt(medianIndex);
            }

            return result;
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        // Helper class for Octree quantization
        private class Octree
        {
            private readonly Dictionary<int, int> _colorCounts = new Dictionary<int, int>();

            public void AddColor(int color)
            {
                if (_colorCounts.ContainsKey(color))
                    _colorCounts[color]++;
                else
                    _colorCounts[color] = 1;
            }

            public int[] ReduceToPalette(int paletteSize)
            {
                var colors = _colorCounts.Keys.ToArray();
                if (colors.Length <= paletteSize)
                    return colors;

                // Simple reduction - take evenly distributed colors
                var result = new int[paletteSize];
                int step = colors.Length / paletteSize;
                for (int i = 0; i < paletteSize; i++)
                {
                    result[i] = colors[i * step];
                }
                return result;
            }
        }
    }
}
