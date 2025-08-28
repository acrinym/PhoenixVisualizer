using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

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
            
            // TODO: Implement actual effect logic here
            // For now, just copy input to output
            for (int i = 0; i < output.Pixels.Length; i++)
            {
                output.Pixels[i] = imageBuffer.Pixels[i];
            }
            
            return output;
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
