using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Water Bump effect creates fluid-like ripples with height-based displacement.
    /// </summary>
    public class WaterBumpEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Enable/disable the water bump effect.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Water density - controls wave damping (2-10).
        /// </summary>
        public int Density { get; set; } = 6;

        /// <summary>
        /// Wave depth - controls displacement intensity (100-2000).
        /// </summary>
        public int Depth { get; set; } = 600;

        /// <summary>
        /// Random drop placement - true for random, false for fixed position.
        /// </summary>
        public bool RandomDrop { get; set; } = false;

        /// <summary>
        /// Drop position X - 0=left, 1=center, 2=right.
        /// </summary>
        public int DropPositionX { get; set; } = 1;

        /// <summary>
        /// Drop position Y - 0=top, 1=middle, 2=bottom.
        /// </summary>
        public int DropPositionY { get; set; } = 1;

        /// <summary>
        /// Drop radius - controls wave source size (10-100).
        /// </summary>
        public int DropRadius { get; set; } = 40;

        /// <summary>
        /// Calculation method - 0=standard, 1=sludge.
        /// </summary>
        public int Method { get; set; } = 0;

        #endregion

        #region Private Fields

        private int[,] _heightBuffer1 = new int[0, 0];
        private int[,] _heightBuffer2 = new int[0, 0];
        private int _currentBuffer;
        private int _bufferWidth;
        private int _bufferHeight;
        private readonly Random _random = new Random();

        #endregion

        #region Constructor

        public WaterBumpEffectsNode()
        {
            Name = "Water Bump Effects";
            Description = "Simulates water ripples using height-based displacement";
            Category = "AVS Effects";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for water displacement"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Water bump output image"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled)
                return GetDefaultOutput();

            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer source)
                return GetDefaultOutput();

            int width = source.Width;
            int height = source.Height;

            if (_bufferWidth != width || _bufferHeight != height)
                InitializeBuffers(width, height);

            if (audioFeatures?.IsBeat == true)
                CreateWaterDrop();

            var output = new ImageBuffer(width, height);
            ApplyWaterDisplacement(source, output);
            CalculateWaterPhysics();
            _currentBuffer = 1 - _currentBuffer;

            return output;
        }

        private void InitializeBuffers(int width, int height)
        {
            _heightBuffer1 = new int[width, height];
            _heightBuffer2 = new int[width, height];
            _bufferWidth = width;
            _bufferHeight = height;
            _currentBuffer = 0;
        }

        private void CreateWaterDrop()
        {
            int x, y;

            if (RandomDrop)
            {
                int maxDimension = Math.Max(_bufferWidth, _bufferHeight);
                x = _random.Next(_bufferWidth);
                y = _random.Next(_bufferHeight);
                CreateSineBlob(x, y, DropRadius * maxDimension / 100, -Depth, _currentBuffer);
            }
            else
            {
                switch (DropPositionX)
                {
                    case 0: x = _bufferWidth / 4; break;
                    case 2: x = _bufferWidth * 3 / 4; break;
                    default: x = _bufferWidth / 2; break;
                }

                switch (DropPositionY)
                {
                    case 0: y = _bufferHeight / 4; break;
                    case 2: y = _bufferHeight * 3 / 4; break;
                    default: y = _bufferHeight / 2; break;
                }

                CreateSineBlob(x, y, DropRadius, -Depth, _currentBuffer);
            }
        }

        private void CreateSineBlob(int centerX, int centerY, int radius, int height, int bufferIndex)
        {
            var buffer = bufferIndex == 0 ? _heightBuffer1 : _heightBuffer2;

            if (centerX < 0) centerX = 1 + radius + _random.Next(_bufferWidth - 2 * radius - 1);
            if (centerY < 0) centerY = 1 + radius + _random.Next(_bufferHeight - 2 * radius - 1);

            int radiusSquared = radius * radius;
            double length = (1024.0 / radius) * (1024.0 / radius);

            int left = Math.Max(-radius, 1 - (centerX - radius));
            int right = Math.Min(radius, _bufferWidth - 1 - (centerX + radius));
            int top = Math.Max(-radius, 1 - (centerY - radius));
            int bottom = Math.Min(radius, _bufferHeight - 1 - (centerY + radius));

            for (int cy = top; cy < bottom; cy++)
            {
                for (int cx = left; cx < right; cx++)
                {
                    int square = cy * cy + cx * cx;
                    if (square < radiusSquared)
                    {
                        double distance = Math.Sqrt(square * length);
                        int bufferX = centerX + cx;
                        int bufferY = centerY + cy;
                        if (bufferX >= 0 && bufferX < _bufferWidth && bufferY >= 0 && bufferY < _bufferHeight)
                        {
                            int heightValue = (int)((Math.Cos(distance) + 1.0) * height) >> 19;
                            buffer[bufferX, bufferY] += heightValue;
                        }
                    }
                }
            }
        }

        private void CreateHeightBlob(int centerX, int centerY, int radius, int height, int bufferIndex)
        {
            var buffer = bufferIndex == 0 ? _heightBuffer1 : _heightBuffer2;

            if (centerX < 0) centerX = 1 + radius + _random.Next(_bufferWidth - 2 * radius - 1);
            if (centerY < 0) centerY = 1 + radius + _random.Next(_bufferHeight - 2 * radius - 1);

            int radiusSquared = radius * radius;

            int left = Math.Max(-radius, 1 - (centerX - radius));
            int right = Math.Min(radius, _bufferWidth - 1 - (centerX + radius));
            int top = Math.Max(-radius, 1 - (centerY - radius));
            int bottom = Math.Min(radius, _bufferHeight - 1 - (centerY + radius));

            for (int cy = top; cy < bottom; cy++)
            {
                int cySquared = cy * cy;
                for (int cx = left; cx < right; cx++)
                {
                    if (cx * cx + cySquared < radiusSquared)
                    {
                        int bufferX = centerX + cx;
                        int bufferY = centerY + cy;
                        if (bufferX >= 0 && bufferX < _bufferWidth && bufferY >= 0 && bufferY < _bufferHeight)
                        {
                            buffer[bufferX, bufferY] += height;
                        }
                    }
                }
            }
        }

        private void ApplyWaterDisplacement(ImageBuffer source, ImageBuffer output)
        {
            var currentBuffer = _currentBuffer == 0 ? _heightBuffer1 : _heightBuffer2;
            var srcPixels = source.Pixels;
            var dstPixels = output.Pixels;
            int width = source.Width;
            int height = source.Height;

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    int dx = 0, dy = 0;
                    if (x < _bufferWidth - 1)
                        dx = currentBuffer[x, y] - currentBuffer[x + 1, y];
                    if (y < _bufferHeight - 1)
                        dy = currentBuffer[x, y] - currentBuffer[x, y + 1];

                    int offsetX = x + (dx >> 3);
                    int offsetY = y + (dy >> 3);

                    if (offsetX < 0) offsetX = 0;
                    else if (offsetX >= width) offsetX = width - 1;
                    if (offsetY < 0) offsetY = 0;
                    else if (offsetY >= height) offsetY = height - 1;

                    dstPixels[rowOffset + x] = srcPixels[offsetY * width + offsetX];
                }
            }
        }

        private void CalculateWaterPhysics()
        {
            var currentBuffer = _currentBuffer == 0 ? _heightBuffer1 : _heightBuffer2;
            var nextBuffer = _currentBuffer == 0 ? _heightBuffer2 : _heightBuffer1;

            if (Method == 0)
                CalculateStandardWater(nextBuffer, currentBuffer);
            else
                CalculateSludgeWater(nextBuffer, currentBuffer);
        }

        private void CalculateStandardWater(int[,] newBuffer, int[,] oldBuffer)
        {
            for (int y = 1; y < _bufferHeight - 1; y++)
            {
                for (int x = 1; x < _bufferWidth - 1; x++)
                {
                    int newHeight = ((oldBuffer[x, y + 1] +
                                       oldBuffer[x, y - 1] +
                                       oldBuffer[x + 1, y] +
                                       oldBuffer[x - 1, y] +
                                       oldBuffer[x - 1, y - 1] +
                                       oldBuffer[x + 1, y - 1] +
                                       oldBuffer[x - 1, y + 1] +
                                       oldBuffer[x + 1, y + 1]) >> 2) - newBuffer[x, y];

                    newBuffer[x, y] = newHeight - (newHeight >> Density);
                }
            }
        }

        private void CalculateSludgeWater(int[,] newBuffer, int[,] oldBuffer)
        {
            for (int y = 1; y < _bufferHeight - 1; y++)
            {
                for (int x = 1; x < _bufferWidth - 1; x++)
                {
                    int newHeight = (oldBuffer[x, y] << 2) +
                                   oldBuffer[x - 1, y - 1] +
                                   oldBuffer[x + 1, y - 1] +
                                   oldBuffer[x - 1, y + 1] +
                                   oldBuffer[x + 1, y + 1] +
                                   ((oldBuffer[x - 1, y] +
                                     oldBuffer[x + 1, y] +
                                     oldBuffer[x, y - 1] +
                                     oldBuffer[x, y + 1]) << 1);

                    newBuffer[x, y] = (newHeight - (newHeight >> 6)) >> Density;
                }
            }
        }

        #endregion

        #region Configuration

        public override bool ValidateConfiguration()
        {
            Density = Math.Max(2, Math.Min(10, Density));
            Depth = Math.Max(100, Math.Min(2000, Depth));
            DropRadius = Math.Max(10, Math.Min(100, DropRadius));
            DropPositionX = Math.Max(0, Math.Min(2, DropPositionX));
            DropPositionY = Math.Max(0, Math.Min(2, DropPositionY));
            Method = Math.Max(0, Math.Min(1, Method));
            return true;
        }

        public override string GetSettingsSummary()
        {
            string positionX = DropPositionX == 0 ? "Left" : DropPositionX == 1 ? "Center" : "Right";
            string positionY = DropPositionY == 0 ? "Top" : DropPositionY == 1 ? "Middle" : "Bottom";
            string method = Method == 0 ? "Standard" : "Sludge";
            return $"Water Bump Effect - Enabled: {Enabled}, Density: {Density}, Depth: {Depth}, Radius: {DropRadius}, Method: {method}, Position: {positionX}/{positionY}, Random: {RandomDrop}";
        }

        #endregion

        #region Defaults

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(1, 1);
        }

        #endregion
    }
}
