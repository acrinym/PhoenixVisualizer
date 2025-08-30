using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Pixel scattering and distortion effect
    /// Based on r_scat.cpp from original AVS
    /// Creates digital distortion by randomly scattering pixels in specific regions
    /// </summary>
    public class ScatterEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Scatter effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Intensity of scattering effect (0.0 to 1.0)
        /// </summary>
        public float Intensity { get; set; } = 0.5f;

        /// <summary>
        /// Maximum scatter distance in pixels
        /// </summary>
        public int MaxScatterDistance { get; set; } = 16;

        /// <summary>
        /// Scatter probability (0.0 to 1.0) - controls how many pixels are scattered
        /// </summary>
        public float ScatterProbability { get; set; } = 0.3f;

        /// <summary>
        /// Whether to preserve edges (don't scatter edge pixels)
        /// </summary>
        public bool PreserveEdges { get; set; } = true;

        /// <summary>
        /// Scatter pattern mode
        /// 0 = Random, 1 = Grid-based, 2 = Circular, 3 = Horizontal, 4 = Vertical
        /// </summary>
        public int ScatterMode { get; set; } = 0;

        /// <summary>
        /// Beat reactivity - increases scatter on beat
        /// </summary>
        public bool BeatReactive { get; set; } = true;

        /// <summary>
        /// Beat multiplier for scatter intensity
        /// </summary>
        public float BeatMultiplier { get; set; } = 2.0f;

        #endregion

        #region Private Fields

        private readonly Random _random = new Random();
        private int[] _fudgeTable;

        #endregion

        #region Constructor

        public ScatterEffectsNode()
        {
            Name = "Scatter Effects";
            Description = "Creates digital distortion by randomly scattering pixels";
            Category = "Distortion Effects";
            _fudgeTable = new int[512];
            GenerateFudgeTable(320); // Default width

            // Initialize parameters for UI binding
            InitializeParameters();
        }

        private void InitializeParameters()
        {
            Params["enabled"] = new EffectParam
            {
                Label = "Enabled",
                Type = "checkbox",
                BoolValue = Enabled
            };

            Params["intensity"] = new EffectParam
            {
                Label = "Intensity",
                Type = "slider",
                FloatValue = Intensity,
                Min = 0.0f,
                Max = 1.0f
            };

            Params["maxScatterDistance"] = new EffectParam
            {
                Label = "Max Scatter Distance",
                Type = "slider",
                FloatValue = MaxScatterDistance,
                Min = 1,
                Max = 100
            };

            Params["scatterProbability"] = new EffectParam
            {
                Label = "Scatter Probability",
                Type = "slider",
                FloatValue = ScatterProbability,
                Min = 0.0f,
                Max = 1.0f
            };

            Params["preserveEdges"] = new EffectParam
            {
                Label = "Preserve Edges",
                Type = "checkbox",
                BoolValue = PreserveEdges
            };

            Params["scatterMode"] = new EffectParam
            {
                Label = "Scatter Mode",
                Type = "dropdown",
                FloatValue = ScatterMode,
                Options = new() { "Random", "Grid-based", "Circular", "Horizontal", "Vertical" }
            };

            Params["beatReactive"] = new EffectParam
            {
                Label = "Beat Reactive",
                Type = "checkbox",
                BoolValue = BeatReactive
            };

            Params["beatMultiplier"] = new EffectParam
            {
                Label = "Beat Multiplier",
                Type = "slider",
                FloatValue = BeatMultiplier,
                Min = 1.0f,
                Max = 5.0f
            };
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for scattering"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Scattered output image"));
        }

        #endregion

        #region Effect Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            // Initialize fudge table if needed
            if (_fudgeTable == null)
            {
                InitializeFudgeTable();
            }

            // Calculate beat-reactive intensity
            float currentIntensity = Intensity;
            if (BeatReactive && audioFeatures.IsBeat)
            {
                currentIntensity *= BeatMultiplier;
            }

            // Apply scatter effect based on mode
            switch (ScatterMode)
            {
                case 0: // Random scatter
                    ApplyRandomScatter(imageBuffer, output, currentIntensity);
                    break;

                case 1: // Grid-based scatter
                    ApplyGridScatter(imageBuffer, output, currentIntensity);
                    break;

                case 2: // Circular scatter
                    ApplyCircularScatter(imageBuffer, output, currentIntensity);
                    break;

                case 3: // Horizontal scatter
                    ApplyHorizontalScatter(imageBuffer, output, currentIntensity);
                    break;

                case 4: // Vertical scatter
                    ApplyVerticalScatter(imageBuffer, output, currentIntensity);
                    break;

                default:
                    // Copy input to output unchanged
                    Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);
                    break;
            }

            return output;
        }

        private void InitializeFudgeTable()
        {
            // Create a fudge table similar to original AVS implementation
            // This provides pseudo-random but deterministic scattering
            _fudgeTable = new int[256];
            for (int i = 0; i < 256; i++)
            {
                _fudgeTable[i] = (i * 17) % 256; // Simple but effective pseudo-random
            }
        }

        private void ApplyRandomScatter(ImageBuffer input, ImageBuffer output, float intensity)
        {
            int width = input.Width;
            int height = input.Height;
            int maxDistance = (int)(MaxScatterDistance * intensity);

            // First pass: copy pixels to output
            Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

            // Second pass: scatter pixels randomly
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Skip edge pixels if preserve edges is enabled
                    if (PreserveEdges && IsEdgePixel(input, x, y))
                        continue;

                    // Only scatter based on probability
                    if (_random.NextDouble() > ScatterProbability)
                        continue;

                    // Calculate scatter offset
                    int offsetX = _random.Next(-maxDistance, maxDistance + 1);
                    int offsetY = _random.Next(-maxDistance, maxDistance + 1);

                    int sourceX = Math.Clamp(x + offsetX, 0, width - 1);
                    int sourceY = Math.Clamp(y + offsetY, 0, height - 1);

                    int destIndex = y * width + x;
                    int sourceIndex = sourceY * width + sourceX;

                    output.Pixels[destIndex] = input.Pixels[sourceIndex];
                }
            }
        }

        private void ApplyGridScatter(ImageBuffer input, ImageBuffer output, float intensity)
        {
            int width = input.Width;
            int height = input.Height;
            int gridSize = Math.Max(2, (int)(8 / intensity)); // Smaller grid for higher intensity
            int maxDistance = (int)(MaxScatterDistance * intensity);

            // First pass: copy pixels to output
            Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

            // Second pass: scatter pixels in grid pattern
            for (int gridY = 0; gridY < height; gridY += gridSize)
            {
                for (int gridX = 0; gridX < width; gridX += gridSize)
                {
                    // Calculate scatter for this grid cell
                    int cellOffsetX = _random.Next(-maxDistance, maxDistance + 1);
                    int cellOffsetY = _random.Next(-maxDistance, maxDistance + 1);

                    // Apply scatter to pixels in this cell
                    for (int y = gridY; y < Math.Min(gridY + gridSize, height); y++)
                    {
                        for (int x = gridX; x < Math.Min(gridX + gridSize, width); x++)
                        {
                            if (PreserveEdges && IsEdgePixel(input, x, y))
                                continue;

                            if (_random.NextDouble() > ScatterProbability)
                                continue;

                            int sourceX = Math.Clamp(x + cellOffsetX, 0, width - 1);
                            int sourceY = Math.Clamp(y + cellOffsetY, 0, height - 1);

                            int destIndex = y * width + x;
                            int sourceIndex = sourceY * width + sourceX;

                            output.Pixels[destIndex] = input.Pixels[sourceIndex];
                        }
                    }
                }
            }
        }

        private void ApplyCircularScatter(ImageBuffer input, ImageBuffer output, float intensity)
        {
            int width = input.Width;
            int height = input.Height;
            int centerX = width / 2;
            int centerY = height / 2;
            int maxDistance = (int)(MaxScatterDistance * intensity);

            // First pass: copy pixels to output
            Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

            // Second pass: scatter pixels in circular pattern
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (PreserveEdges && IsEdgePixel(input, x, y))
                        continue;

                    if (_random.NextDouble() > ScatterProbability)
                        continue;

                    // Calculate distance from center
                    double distance = Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    double maxRadius = Math.Sqrt(centerX * centerX + centerY * centerY);
                    double normalizedDistance = distance / maxRadius;

                    // Scatter along circular path
                    double angle = normalizedDistance * Math.PI * 2 * intensity;
                    int offsetX = (int)(Math.Cos(angle) * maxDistance * normalizedDistance);
                    int offsetY = (int)(Math.Sin(angle) * maxDistance * normalizedDistance);

                    int sourceX = Math.Clamp(x + offsetX, 0, width - 1);
                    int sourceY = Math.Clamp(y + offsetY, 0, height - 1);

                    int destIndex = y * width + x;
                    int sourceIndex = sourceY * width + sourceX;

                    output.Pixels[destIndex] = input.Pixels[sourceIndex];
                }
            }
        }

        private void ApplyHorizontalScatter(ImageBuffer input, ImageBuffer output, float intensity)
        {
            int width = input.Width;
            int height = input.Height;
            int maxDistance = (int)(MaxScatterDistance * intensity);

            // First pass: copy pixels to output
            Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

            // Second pass: scatter pixels horizontally
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (PreserveEdges && IsEdgePixel(input, x, y))
                        continue;

                    if (_random.NextDouble() > ScatterProbability)
                        continue;

                    // Horizontal scatter only
                    int offsetX = _random.Next(-maxDistance, maxDistance + 1);
                    int sourceX = Math.Clamp(x + offsetX, 0, width - 1);

                    int destIndex = y * width + x;
                    int sourceIndex = y * width + sourceX;

                    output.Pixels[destIndex] = input.Pixels[sourceIndex];
                }
            }
        }

        private void ApplyVerticalScatter(ImageBuffer input, ImageBuffer output, float intensity)
        {
            int width = input.Width;
            int height = input.Height;
            int maxDistance = (int)(MaxScatterDistance * intensity);

            // First pass: copy pixels to output
            Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

            // Second pass: scatter pixels vertically
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (PreserveEdges && IsEdgePixel(input, x, y))
                        continue;

                    if (_random.NextDouble() > ScatterProbability)
                        continue;

                    // Vertical scatter only
                    int offsetY = _random.Next(-maxDistance, maxDistance + 1);
                    int sourceY = Math.Clamp(y + offsetY, 0, height - 1);

                    int destIndex = y * width + x;
                    int sourceIndex = sourceY * width + x;

                    output.Pixels[destIndex] = input.Pixels[sourceIndex];
                }
            }
        }

        private bool IsEdgePixel(ImageBuffer buffer, int x, int y)
        {
            // Simple edge detection - check if pixel is significantly different from neighbors
            int width = buffer.Width;
            int height = buffer.Height;

            if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1)
                return true; // Border pixels are always considered edges

            var current = buffer.Pixels[y * width + x];
            var left = buffer.Pixels[y * width + (x - 1)];
            var right = buffer.Pixels[y * width + (x + 1)];
            var up = buffer.Pixels[(y - 1) * width + x];
            var down = buffer.Pixels[(y + 1) * width + x];

            // Simple edge detection based on color difference
            int threshold = 30;

            // Extract RGB components for comparison
            int currentR = current & 0xFF;
            int currentG = (current >> 8) & 0xFF;
            int currentB = (current >> 16) & 0xFF;

            int leftR = left & 0xFF;
            int leftG = (left >> 8) & 0xFF;
            int leftB = (left >> 16) & 0xFF;

            int rightR = right & 0xFF;
            int rightG = (right >> 8) & 0xFF;
            int rightB = (right >> 16) & 0xFF;

            int upR = up & 0xFF;
            int upG = (up >> 8) & 0xFF;
            int upB = (up >> 16) & 0xFF;

            int downR = down & 0xFF;
            int downG = (down >> 8) & 0xFF;
            int downB = (down >> 16) & 0xFF;

            return Math.Abs(currentR - leftR) > threshold ||
                   Math.Abs(currentG - leftG) > threshold ||
                   Math.Abs(currentB - leftB) > threshold ||
                   Math.Abs(currentR - rightR) > threshold ||
                   Math.Abs(currentG - rightG) > threshold ||
                   Math.Abs(currentB - rightB) > threshold ||
                   Math.Abs(currentR - upR) > threshold ||
                   Math.Abs(currentG - upG) > threshold ||
                   Math.Abs(currentB - upB) > threshold ||
                   Math.Abs(currentR - downR) > threshold ||
                   Math.Abs(currentG - downG) > threshold ||
                   Math.Abs(currentB - downB) > threshold;
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion

        #region Private Methods

        private void GenerateFudgeTable(int width)
        {
            // Generate pre-calculated displacement table (like original AVS)
            for (int i = 0; i < 512; i++)
            {
                int displacement = _random.Next(-MaxScatterDistance, MaxScatterDistance + 1);
                _fudgeTable[i] = displacement * width; // Convert to buffer offset
            }
        }

        private void ApplyScatterEffect(ImageBuffer image, float intensity)
        {
            int width = image.Width;
            int height = image.Height;
            uint[] originalData = new uint[image.Data.Length];
            Array.Copy(image.Data, originalData, image.Data.Length);

            // Preserve edges - only scatter middle region
            int edgeMargin = PreserveEdges ? Math.Min(width / 8, height / 8) : 0;
            int startY = edgeMargin;
            int endY = height - edgeMargin;
            int startX = edgeMargin;
            int endX = width - edgeMargin;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    // Decide whether to scatter this pixel
                    if (_random.NextDouble() > ScatterProbability * intensity)
                        continue;

                    int sourceIndex = y * width + x;
                    
                    // Calculate scatter destination based on mode
                    int newX, newY;
                    CalculateScatterDestination(x, y, width, height, out newX, out newY, intensity);

                    // Bounds check
                    if (newX >= 0 && newX < width && newY >= 0 && newY < height)
                    {
                        int destIndex = newY * width + newX;
                        
                        // Swap pixels to create scatter effect
                        uint temp = image.Data[sourceIndex];
                        image.Data[sourceIndex] = originalData[destIndex];
                        image.Data[destIndex] = temp;
                    }
                }
            }
        }

        private void CalculateScatterDestination(int x, int y, int width, int height, out int newX, out int newY, float intensity)
        {
            int maxDistance = (int)(MaxScatterDistance * intensity);
            
            switch (ScatterMode)
            {
                case 0: // Random
                    newX = x + _random.Next(-maxDistance, maxDistance + 1);
                    newY = y + _random.Next(-maxDistance, maxDistance + 1);
                    break;

                case 1: // Grid-based (like original AVS fudge table)
                    int gridIndex = ((x / 8) + (y / 8) * (width / 8)) % 512;
                    int offset = (int)(_fudgeTable[gridIndex] * intensity);
                    newX = x + (offset % width);
                    newY = y + (offset / width);
                    break;

                case 2: // Circular
                    double angle = _random.NextDouble() * 2 * Math.PI;
                    double radius = _random.NextDouble() * maxDistance;
                    newX = x + (int)(Math.Cos(angle) * radius);
                    newY = y + (int)(Math.Sin(angle) * radius);
                    break;

                case 3: // Horizontal only
                    newX = x + _random.Next(-maxDistance, maxDistance + 1);
                    newY = y;
                    break;

                case 4: // Vertical only
                    newX = x;
                    newY = y + _random.Next(-maxDistance, maxDistance + 1);
                    break;

                default:
                    newX = x;
                    newY = y;
                    break;
            }

            // Keep within bounds
            newX = Math.Max(0, Math.Min(width - 1, newX));
            newY = Math.Max(0, Math.Min(height - 1, newY));
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Get the current configuration parameters
        /// </summary>


        /// <summary>
        /// Apply configuration parameters
        /// </summary>


        #endregion
    }
}
