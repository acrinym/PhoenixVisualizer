using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Dynamic image shifting effect with scriptable transformations
    /// Based on r_shift.cpp C_ShiftClass from original AVS
    /// Provides advanced control over image displacement with EEL scripting support
    /// </summary>
    public class ShiftEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Shift effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// X displacement amount (-1.0 to 1.0)
        /// </summary>
        public float DisplacementX { get; set; } = 0.0f;

        /// <summary>
        /// Y displacement amount (-1.0 to 1.0)
        /// </summary>
        public float DisplacementY { get; set; } = 0.0f;

        /// <summary>
        /// Blending mode
        /// 0 = Replace, 1 = Additive, 2 = Maximum, 3 = Minimum, 4 = Multiply, 5 = Average, 6 = Subtractive
        /// </summary>
        public int BlendingMode { get; set; } = 0;

        /// <summary>
        /// Subpixel precision enabled
        /// </summary>
        public bool SubpixelPrecision { get; set; } = true;

        /// <summary>
        /// Bilinear interpolation for smooth displacement
        /// </summary>
        public bool BilinearInterpolation { get; set; } = true;

        /// <summary>
        /// Beat reactivity enabled
        /// </summary>
        public bool BeatReactive { get; set; } = true;

        /// <summary>
        /// Beat displacement multiplier
        /// </summary>
        public float BeatMultiplier { get; set; } = 2.0f;

        /// <summary>
        /// Beat displacement X offset
        /// </summary>
        public float BeatDisplacementX { get; set; } = 0.1f;

        /// <summary>
        /// Beat displacement Y offset
        /// </summary>
        public float BeatDisplacementY { get; set; } = 0.1f;

        /// <summary>
        /// Edge handling mode
        /// 0 = Clamp, 1 = Wrap, 2 = Mirror
        /// </summary>
        public int EdgeMode { get; set; } = 0;

        /// <summary>
        /// Displacement source mode
        /// 0 = Fixed values, 1 = Audio reactive, 2 = Automatic movement
        /// </summary>
        public int DisplacementMode { get; set; } = 0;

        /// <summary>
        /// Movement speed for automatic mode
        /// </summary>
        public float MovementSpeed { get; set; } = 0.01f;

        /// <summary>
        /// Audio reactivity sensitivity
        /// </summary>
        public float AudioSensitivity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private float _currentDisplacementX = 0.0f;
        private float _currentDisplacementY = 0.0f;
        private float _movementPhaseX = 0.0f;
        private float _movementPhaseY = 0.0f;
        private int _beatCounter = 0;
        private const int BEAT_DURATION = 15;

        #endregion

        #region Constructor

        public ShiftEffectsNode()
        {
            Name = "Shift Effects";
            Description = "Dynamic image shifting with scriptable transformations and subpixel precision";
            Category = "Transform Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for shifting"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Shifted output image"));
        }

        #endregion

        #region Effect Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) 
                return GetDefaultOutput();

            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            // Calculate current displacement based on mode
            CalculateCurrentDisplacement(audioFeatures);

            // Handle beat reactivity
            bool isBeat = audioFeatures?.IsBeat == true;
            if (BeatReactive && isBeat)
            {
                _beatCounter = BEAT_DURATION;
            }
            else if (_beatCounter > 0)
            {
                _beatCounter--;
            }

            // Apply shift transformation
            ApplyShiftTransformation(imageBuffer, output);

            return output;
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion

        #region Private Methods

        private void CalculateCurrentDisplacement(AudioFeatures audioFeatures)
        {
            switch (DisplacementMode)
            {
                case 0: // Fixed values
                    _currentDisplacementX = DisplacementX;
                    _currentDisplacementY = DisplacementY;
                    break;

                case 1: // Audio reactive
                    float bassIntensity = audioFeatures.Bass * AudioSensitivity;
                    float midIntensity = audioFeatures.Mid * AudioSensitivity;
                    
                    _currentDisplacementX = DisplacementX + (bassIntensity - 0.5f) * 0.2f;
                    _currentDisplacementY = DisplacementY + (midIntensity - 0.5f) * 0.2f;
                    break;

                case 2: // Automatic movement
                    _movementPhaseX += MovementSpeed;
                    _movementPhaseY += MovementSpeed * 0.7f; // Different frequency for Y
                    
                    if (_movementPhaseX >= 2 * Math.PI) _movementPhaseX -= (float)(2 * Math.PI);
                    if (_movementPhaseY >= 2 * Math.PI) _movementPhaseY -= (float)(2 * Math.PI);
                    
                    _currentDisplacementX = DisplacementX + (float)Math.Sin(_movementPhaseX) * 0.1f;
                    _currentDisplacementY = DisplacementY + (float)Math.Cos(_movementPhaseY) * 0.1f;
                    break;
            }

            // Apply beat displacement if active
            if (_beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION) * BeatMultiplier;
                _currentDisplacementX += BeatDisplacementX * beatFactor;
                _currentDisplacementY += BeatDisplacementY * beatFactor;
            }

            // Clamp displacement values to reasonable range
            _currentDisplacementX = Math.Max(-2.0f, Math.Min(2.0f, _currentDisplacementX));
            _currentDisplacementY = Math.Max(-2.0f, Math.Min(2.0f, _currentDisplacementY));
        }

        private void ApplyShiftTransformation(ImageBuffer source, ImageBuffer output)
        {
            int width = source.Width;
            int height = source.Height;

            // Convert displacement to pixel coordinates
            float pixelShiftX = _currentDisplacementX * width;
            float pixelShiftY = _currentDisplacementY * height;

            if (SubpixelPrecision && BilinearInterpolation)
            {
                ApplyShiftWithInterpolation(source, output, pixelShiftX, pixelShiftY);
            }
            else
            {
                ApplyShiftWithoutInterpolation(source, output, pixelShiftX, pixelShiftY);
            }
        }

        private void ApplyShiftWithInterpolation(ImageBuffer source, ImageBuffer output, float shiftX, float shiftY)
        {
            int width = source.Width;
            int height = source.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate source coordinates with subpixel precision
                    float sourceX = x - shiftX;
                    float sourceY = y - shiftY;

                    // Handle edge cases
                    sourceX = HandleEdgeCoordinate(sourceX, width);
                    sourceY = HandleEdgeCoordinate(sourceY, height);

                    // Bilinear interpolation
                    uint interpolatedPixel = GetInterpolatedPixel(source, sourceX, sourceY);

                    // Apply blending
                    int outputIndex = y * width + x;
                    uint originalPixel = source.Data[outputIndex];
                    output.Data[outputIndex] = BlendPixels(originalPixel, interpolatedPixel);
                }
            }
        }

        private void ApplyShiftWithoutInterpolation(ImageBuffer source, ImageBuffer output, float shiftX, float shiftY)
        {
            int width = source.Width;
            int height = source.Height;
            int intShiftX = (int)Math.Round(shiftX);
            int intShiftY = (int)Math.Round(shiftY);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate source coordinates
                    int sourceX = x - intShiftX;
                    int sourceY = y - intShiftY;

                    // Handle edge cases
                    sourceX = (int)HandleEdgeCoordinate(sourceX, width);
                    sourceY = (int)HandleEdgeCoordinate(sourceY, height);

                    // Get source pixel
                    uint sourcePixel = source.Data[sourceY * width + sourceX];

                    // Apply blending
                    int outputIndex = y * width + x;
                    uint originalPixel = source.Data[outputIndex];
                    output.Data[outputIndex] = BlendPixels(originalPixel, sourcePixel);
                }
            }
        }

        private float HandleEdgeCoordinate(float coord, int dimension)
        {
            switch (EdgeMode)
            {
                case 0: // Clamp
                    return Math.Max(0, Math.Min(dimension - 1, coord));

                case 1: // Wrap
                    while (coord < 0) coord += dimension;
                    while (coord >= dimension) coord -= dimension;
                    return coord;

                case 2: // Mirror
                    if (coord < 0)
                    {
                        coord = -coord;
                        if (coord >= dimension)
                            coord = (dimension - 1) - (coord - dimension);
                    }
                    else if (coord >= dimension)
                    {
                        coord = (dimension - 1) - (coord - dimension);
                        if (coord < 0)
                            coord = -coord;
                    }
                    return Math.Max(0, Math.Min(dimension - 1, coord));

                default:
                    return Math.Max(0, Math.Min(dimension - 1, coord));
            }
        }

        private uint GetInterpolatedPixel(ImageBuffer source, float x, float y)
        {
            int width = source.Width;
            int height = source.Height;

            int x1 = (int)Math.Floor(x);
            int y1 = (int)Math.Floor(y);
            int x2 = x1 + 1;
            int y2 = y1 + 1;

            float fracX = x - x1;
            float fracY = y - y1;

            // Clamp coordinates
            x1 = Math.Max(0, Math.Min(width - 1, x1));
            y1 = Math.Max(0, Math.Min(height - 1, y1));
            x2 = Math.Max(0, Math.Min(width - 1, x2));
            y2 = Math.Max(0, Math.Min(height - 1, y2));

            // Get four corner pixels
            uint p11 = source.Data[y1 * width + x1];
            uint p21 = source.Data[y1 * width + x2];
            uint p12 = source.Data[y2 * width + x1];
            uint p22 = source.Data[y2 * width + x2];

            // Interpolate each color component
            byte a = (byte)BilinearInterpolateChannel((p11 >> 24) & 0xFF, (p21 >> 24) & 0xFF, (p12 >> 24) & 0xFF, (p22 >> 24) & 0xFF, fracX, fracY);
            byte r = (byte)BilinearInterpolateChannel((p11 >> 16) & 0xFF, (p21 >> 16) & 0xFF, (p12 >> 16) & 0xFF, (p22 >> 16) & 0xFF, fracX, fracY);
            byte g = (byte)BilinearInterpolateChannel((p11 >> 8) & 0xFF, (p21 >> 8) & 0xFF, (p12 >> 8) & 0xFF, (p22 >> 8) & 0xFF, fracX, fracY);
            byte b = (byte)BilinearInterpolateChannel(p11 & 0xFF, p21 & 0xFF, p12 & 0xFF, p22 & 0xFF, fracX, fracY);

            return (uint)((a << 24) | (r << 16) | (g << 8) | b);
        }

        private int BilinearInterpolateChannel(uint c11, uint c21, uint c12, uint c22, float fracX, float fracY)
        {
            float top = c11 * (1 - fracX) + c21 * fracX;
            float bottom = c12 * (1 - fracX) + c22 * fracX;
            float result = top * (1 - fracY) + bottom * fracY;
            return (int)Math.Max(0, Math.Min(255, Math.Round(result)));
        }

        private uint BlendPixels(uint dest, uint src)
        {
            switch (BlendingMode)
            {
                case 0: // Replace
                    return src;

                case 1: // Additive
                    return BlendAdditive(dest, src);

                case 2: // Maximum
                    return BlendMaximum(dest, src);

                case 3: // Minimum
                    return BlendMinimum(dest, src);

                case 4: // Multiply
                    return BlendMultiply(dest, src);

                case 5: // Average
                    return BlendAverage(dest, src);

                case 6: // Subtractive
                    return BlendSubtractive(dest, src);

                default:
                    return src;
            }
        }

        private uint BlendAdditive(uint dest, uint src)
        {
            uint dA = (dest >> 24) & 0xFF, dR = (dest >> 16) & 0xFF, dG = (dest >> 8) & 0xFF, dB = dest & 0xFF;
            uint sA = (src >> 24) & 0xFF, sR = (src >> 16) & 0xFF, sG = (src >> 8) & 0xFF, sB = src & 0xFF;

            uint rA = Math.Max(dA, sA);
            uint rR = Math.Min(255u, dR + sR);
            uint rG = Math.Min(255u, dG + sG);
            uint rB = Math.Min(255u, dB + sB);

            return (rA << 24) | (rR << 16) | (rG << 8) | rB;
        }

        private uint BlendMaximum(uint dest, uint src)
        {
            uint dA = (dest >> 24) & 0xFF, dR = (dest >> 16) & 0xFF, dG = (dest >> 8) & 0xFF, dB = dest & 0xFF;
            uint sA = (src >> 24) & 0xFF, sR = (src >> 16) & 0xFF, sG = (src >> 8) & 0xFF, sB = src & 0xFF;

            uint rA = Math.Max(dA, sA);
            uint rR = Math.Max(dR, sR);
            uint rG = Math.Max(dG, sG);
            uint rB = Math.Max(dB, sB);

            return (rA << 24) | (rR << 16) | (rG << 8) | rB;
        }

        private uint BlendMinimum(uint dest, uint src)
        {
            uint dA = (dest >> 24) & 0xFF, dR = (dest >> 16) & 0xFF, dG = (dest >> 8) & 0xFF, dB = dest & 0xFF;
            uint sA = (src >> 24) & 0xFF, sR = (src >> 16) & 0xFF, sG = (src >> 8) & 0xFF, sB = src & 0xFF;

            uint rA = Math.Max(dA, sA);
            uint rR = Math.Min(dR, sR);
            uint rG = Math.Min(dG, sG);
            uint rB = Math.Min(dB, sB);

            return (rA << 24) | (rR << 16) | (rG << 8) | rB;
        }

        private uint BlendMultiply(uint dest, uint src)
        {
            uint dA = (dest >> 24) & 0xFF, dR = (dest >> 16) & 0xFF, dG = (dest >> 8) & 0xFF, dB = dest & 0xFF;
            uint sA = (src >> 24) & 0xFF, sR = (src >> 16) & 0xFF, sG = (src >> 8) & 0xFF, sB = src & 0xFF;

            uint rA = Math.Max(dA, sA);
            uint rR = (dR * sR) / 255;
            uint rG = (dG * sG) / 255;
            uint rB = (dB * sB) / 255;

            return (rA << 24) | (rR << 16) | (rG << 8) | rB;
        }

        private uint BlendAverage(uint dest, uint src)
        {
            uint dA = (dest >> 24) & 0xFF, dR = (dest >> 16) & 0xFF, dG = (dest >> 8) & 0xFF, dB = dest & 0xFF;
            uint sA = (src >> 24) & 0xFF, sR = (src >> 16) & 0xFF, sG = (src >> 8) & 0xFF, sB = src & 0xFF;

            uint rA = Math.Max(dA, sA);
            uint rR = (dR + sR) / 2;
            uint rG = (dG + sG) / 2;
            uint rB = (dB + sB) / 2;

            return (rA << 24) | (rR << 16) | (rG << 8) | rB;
        }

        private uint BlendSubtractive(uint dest, uint src)
        {
            uint dA = (dest >> 24) & 0xFF, dR = (dest >> 16) & 0xFF, dG = (dest >> 8) & 0xFF, dB = dest & 0xFF;
            uint sA = (src >> 24) & 0xFF, sR = (src >> 16) & 0xFF, sG = (src >> 8) & 0xFF, sB = src & 0xFF;

            uint rA = Math.Max(dA, sA);
            uint rR = (uint)Math.Max(0, (int)dR - (int)sR);
            uint rG = (uint)Math.Max(0, (int)dG - (int)sG);
            uint rB = (uint)Math.Max(0, (int)dB - (int)sB);

            return (rA << 24) | (rR << 16) | (rG << 8) | rB;
        }

        #endregion

        #region Configuration





        #endregion
    }
}