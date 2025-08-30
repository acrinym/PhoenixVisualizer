using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Fractal Effects Node - Generates complex fractal patterns for AVS visualization
    /// Supports multiple fractal types with audio-reactive parameters
    /// </summary>
    public class FractalEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Fractal Effects are active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Fractal algorithm type (0=Mandelbrot, 1=Julia, 2=Burning Ship, 3=Tricorn, 4=Newton)
        /// </summary>
        public int FractalType { get; set; } = 0;

        /// <summary>
        /// Maximum iterations for fractal calculation (higher = more detail)
        /// </summary>
        public int MaxIterations { get; set; } = 100;

        /// <summary>
        /// Zoom level for fractal rendering
        /// </summary>
        public float Zoom { get; set; } = 1.0f;

        /// <summary>
        /// X offset for fractal position
        /// </summary>
        public float OffsetX { get; set; } = 0.0f;

        /// <summary>
        /// Y offset for fractal position
        /// </summary>
        public float OffsetY { get; set; } = 0.0f;

        /// <summary>
        /// Julia set parameter (real part)
        /// </summary>
        public float JuliaReal { get; set; } = -0.7f;

        /// <summary>
        /// Julia set parameter (imaginary part)
        /// </summary>
        public float JuliaImaginary { get; set; } = 0.27015f;

        /// <summary>
        /// Color palette mode (0=HSV, 1=RGB, 2=Grayscale, 3=Audio-reactive)
        /// </summary>
        public int ColorMode { get; set; } = 0;

        /// <summary>
        /// Color saturation multiplier
        /// </summary>
        public float Saturation { get; set; } = 1.0f;

        /// <summary>
        /// Color brightness multiplier
        /// </summary>
        public float Brightness { get; set; } = 1.0f;

        /// <summary>
        /// Audio reactivity factor (0.0 = no reactivity, 1.0 = full reactivity)
        /// </summary>
        public float AudioReactivity { get; set; } = 0.5f;

        /// <summary>
        /// Fractal animation speed
        /// </summary>
        public float AnimationSpeed { get; set; } = 0.01f;

        /// <summary>
        /// Enable smooth coloring for fractal rendering
        /// </summary>
        public bool SmoothColoring { get; set; } = true;

        /// <summary>
        /// Enable audio-reactive parameter modulation
        /// </summary>
        public bool AudioModulation { get; set; } = true;

        #endregion

        #region Private Fields

        private float _animationTime = 0.0f;
        private readonly Random _random = new Random();

        #endregion

        #region Constructor

        public FractalEffectsNode()
        {
            Name = "Fractal Effects";
            Description = "Generates complex fractal patterns with audio-reactive parameters";
            Category = "Advanced Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for fractal overlay (optional)"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Fractal-generated output image"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled)
            {
                return inputs.TryGetValue("Image", out var imageInput) && imageInput is ImageBuffer imageBuffer
                    ? imageBuffer
                    : GetDefaultOutput();
            }

            // Create output buffer
            var output = new ImageBuffer(320, 240); // Standard AVS resolution

            // Update animation time
            _animationTime += AnimationSpeed;

            // Apply audio reactivity if enabled
            float audioInfluence = 1.0f;
            if (AudioModulation && AudioReactivity > 0.0f)
            {
                // Use bass energy to modulate fractal parameters
                audioInfluence = 1.0f + (audioFeatures.Bass * AudioReactivity * 2.0f);
            }

            // Render fractal based on type
            switch (FractalType)
            {
                case 0: // Mandelbrot
                    RenderMandelbrot(output, audioFeatures, audioInfluence);
                    break;
                case 1: // Julia
                    RenderJulia(output, audioFeatures, audioInfluence);
                    break;
                case 2: // Burning Ship
                    RenderBurningShip(output, audioFeatures, audioInfluence);
                    break;
                case 3: // Tricorn
                    RenderTricorn(output, audioFeatures, audioInfluence);
                    break;
                case 4: // Newton
                    RenderNewton(output, audioFeatures, audioInfluence);
                    break;
                default:
                    RenderMandelbrot(output, audioFeatures, audioInfluence);
                    break;
            }

            // Overlay with source image if provided
            if (inputs.TryGetValue("Image", out var input) && input is ImageBuffer sourceImage)
            {
                BlendWithSource(output, sourceImage);
            }

            return output;
        }

        #endregion

        #region Fractal Rendering Methods

        private void RenderMandelbrot(ImageBuffer output, AudioFeatures audioFeatures, float audioInfluence)
        {
            float zoom = Zoom * audioInfluence;
            float centerX = OffsetX + (float)Math.Sin(_animationTime * 0.1f) * 0.5f;
            float centerY = OffsetY + (float)Math.Cos(_animationTime * 0.15f) * 0.5f;

            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    // Map pixel coordinates to complex plane
                    double real = (x - output.Width / 2.0) / (output.Width / 4.0) / zoom + centerX;
                    double imag = (y - output.Height / 2.0) / (output.Height / 4.0) / zoom + centerY;

                    // Mandelbrot iteration
                    double zReal = 0, zImag = 0;
                    int iterations = 0;

                    while (zReal * zReal + zImag * zImag < 4 && iterations < MaxIterations)
                    {
                        double temp = zReal * zReal - zImag * zImag + real;
                        zImag = 2 * zReal * zImag + imag;
                        zReal = temp;
                        iterations++;
                    }

                    // Color based on iterations
                    uint color = GetFractalColor(iterations, MaxIterations, audioFeatures);
                    output.SetPixel(x, y, (int)color);
                }
            }
        }

        private void RenderJulia(ImageBuffer output, AudioFeatures audioFeatures, float audioInfluence)
        {
            float zoom = Zoom * audioInfluence;
            float cReal = JuliaReal + (float)Math.Sin(_animationTime * 0.05f) * 0.1f;
            float cImag = JuliaImaginary + (float)Math.Cos(_animationTime * 0.07f) * 0.1f;

            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    // Map pixel coordinates to complex plane
                    double zReal = (x - output.Width / 2.0) / (output.Width / 4.0) / zoom + OffsetX;
                    double zImag = (y - output.Height / 2.0) / (output.Height / 4.0) / zoom + OffsetY;

                    // Julia iteration
                    int iterations = 0;
                    while (zReal * zReal + zImag * zImag < 4 && iterations < MaxIterations)
                    {
                        double temp = zReal * zReal - zImag * zImag + cReal;
                        zImag = 2 * zReal * zImag + cImag;
                        zReal = temp;
                        iterations++;
                    }

                    // Color based on iterations
                    uint color = GetFractalColor(iterations, MaxIterations, audioFeatures);
                    output.SetPixel(x, y, (int)color);
                }
            }
        }

        private void RenderBurningShip(ImageBuffer output, AudioFeatures audioFeatures, float audioInfluence)
        {
            float zoom = Zoom * audioInfluence;

            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    // Map pixel coordinates to complex plane
                    double real = (x - output.Width / 2.0) / (output.Width / 4.0) / zoom + OffsetX;
                    double imag = (y - output.Height / 2.0) / (output.Height / 4.0) / zoom + OffsetY;

                    // Burning Ship iteration
                    double zReal = 0, zImag = 0;
                    int iterations = 0;

                    while (zReal * zReal + zImag * zImag < 4 && iterations < MaxIterations)
                    {
                        double temp = zReal * zReal - zImag * zImag + real;
                        zImag = Math.Abs(2 * zReal * zImag) + imag;
                        zReal = Math.Abs(temp);
                        iterations++;
                    }

                    // Color based on iterations
                    uint color = GetFractalColor(iterations, MaxIterations, audioFeatures);
                    output.SetPixel(x, y, (int)color);
                }
            }
        }

        private void RenderTricorn(ImageBuffer output, AudioFeatures audioFeatures, float audioInfluence)
        {
            float zoom = Zoom * audioInfluence;

            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    // Map pixel coordinates to complex plane
                    double real = (x - output.Width / 2.0) / (output.Width / 4.0) / zoom + OffsetX;
                    double imag = (y - output.Height / 2.0) / (output.Height / 4.0) / zoom + OffsetY;

                    // Tricorn iteration
                    double zReal = 0, zImag = 0;
                    int iterations = 0;

                    while (zReal * zReal + zImag * zImag < 4 && iterations < MaxIterations)
                    {
                        double temp = zReal * zReal - zImag * zImag + real;
                        zImag = -2 * zReal * zImag + imag;
                        zReal = temp;
                        iterations++;
                    }

                    // Color based on iterations
                    uint color = GetFractalColor(iterations, MaxIterations, audioFeatures);
                    output.SetPixel(x, y, (int)color);
                }
            }
        }

        private void RenderNewton(ImageBuffer output, AudioFeatures audioFeatures, float audioInfluence)
        {
            float zoom = Zoom * audioInfluence;

            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    // Map pixel coordinates to complex plane
                    double real = (x - output.Width / 2.0) / (output.Width / 50.0) / zoom + OffsetX;
                    double imag = (y - output.Height / 2.0) / (output.Height / 50.0) / zoom + OffsetY;

                    // Newton iteration for z^3 - 1 = 0
                    int iterations = 0;
                    const int maxNewtonIterations = 20;

                    while (iterations < maxNewtonIterations)
                    {
                        if (Math.Abs(real * real + imag * imag) < 1e-10) break;

                        // z^3
                        double z3Real = real * real * real - 3 * real * imag * imag;
                        double z3Imag = 3 * real * real * imag - imag * imag * imag;

                        // z^3 - 1
                        double numReal = z3Real - 1;
                        double numImag = z3Imag;

                        // 3z^2
                        double denomReal = 3 * (real * real - imag * imag);
                        double denomImag = 6 * real * imag;

                        // Division: (z^3 - 1) / 3z^2
                        double denomMag = denomReal * denomReal + denomImag * denomImag;
                        if (Math.Abs(denomMag) < 1e-10) break;

                        double divReal = (numReal * denomReal + numImag * denomImag) / denomMag;
                        double divImag = (numImag * denomReal - numReal * denomImag) / denomMag;

                        // z - (z^3 - 1)/(3z^2)
                        real -= divReal;
                        imag -= divImag;

                        iterations++;
                    }

                    // Color based on root found
                    uint color = GetNewtonColor(real, imag, audioFeatures);
                    output.SetPixel(x, y, (int)color);
                }
            }
        }

        #endregion

        #region Helper Methods

        private uint GetFractalColor(int iterations, int maxIterations, AudioFeatures audioFeatures)
        {
            if (iterations >= maxIterations)
                return 0xFF000000; // Black for points in the set

            float t = (float)iterations / maxIterations;

            // Apply smooth coloring if enabled
            if (SmoothColoring)
            {
                // Smooth coloring algorithm
                float smoothT = t - (float)(Math.Log(Math.Log(Math.Sqrt(iterations))) / Math.Log(2));
                t = Math.Max(0, Math.Min(1, smoothT));
            }

            // Apply audio reactivity
            if (AudioReactivity > 0)
            {
                float audioFactor = (audioFeatures.Bass + audioFeatures.Mid + audioFeatures.Treble) / 3.0f;
                t = t * (1.0f - AudioReactivity) + (t * audioFactor) * AudioReactivity;
            }

            // Generate color based on mode
            switch (ColorMode)
            {
                case 0: // HSV
                    return HsvToRgb(t, Saturation, Brightness);

                case 1: // RGB
                    float r = (float)(Math.Sin(t * Math.PI * 2) * 0.5 + 0.5) * Brightness;
                    float g = (float)(Math.Sin(t * Math.PI * 2 + Math.PI * 2 / 3) * 0.5 + 0.5) * Brightness;
                    float b = (float)(Math.Sin(t * Math.PI * 2 + Math.PI * 4 / 3) * 0.5 + 0.5) * Brightness;
                    return (uint)((int)(r * 255) << 16 | (int)(g * 255) << 8 | (int)(b * 255)) | 0xFF000000;

                case 2: // Grayscale
                    int gray = (int)(t * 255 * Brightness);
                    return (uint)(gray << 16 | gray << 8 | gray) | 0xFF000000;

                case 3: // Audio-reactive
                    float bassFactor = audioFeatures.Bass * Saturation;
                    float midFactor = audioFeatures.Mid * Saturation;
                    float trebleFactor = audioFeatures.Treble * Saturation;
                    return (uint)((int)(bassFactor * 255) << 16 |
                                 (int)(midFactor * 255) << 8 |
                                 (int)(trebleFactor * 255)) | 0xFF000000;

                default:
                    return HsvToRgb(t, Saturation, Brightness);
            }
        }

        private uint GetNewtonColor(double real, double imag, AudioFeatures audioFeatures)
        {
            // Color based on which root was found
            double distance1 = Math.Sqrt(real * real + imag * imag); // Root at (1,0)
            double distance2 = Math.Sqrt((real + 0.5) * (real + 0.5) + (imag - Math.Sqrt(3)/2) * (imag - Math.Sqrt(3)/2)); // Root at (-0.5, √3/2)
            double distance3 = Math.Sqrt((real + 0.5) * (real + 0.5) + (imag + Math.Sqrt(3)/2) * (imag + Math.Sqrt(3)/2)); // Root at (-0.5, -√3/2)

            int rootIndex;
            if (distance1 < distance2 && distance1 < distance3)
                rootIndex = 0; // Red root
            else if (distance2 < distance3)
                rootIndex = 1; // Green root
            else
                rootIndex = 2; // Blue root

            float intensity = Brightness;
            if (AudioReactivity > 0)
            {
                intensity *= (1.0f + audioFeatures.Bass * AudioReactivity);
            }

            uint color = 0xFF000000;
            switch (rootIndex)
            {
                case 0: color |= (uint)(255 * intensity) << 16; break; // Red
                case 1: color |= (uint)(255 * intensity) << 8; break;  // Green
                case 2: color |= (uint)(255 * intensity); break;        // Blue
            }

            return color;
        }

        private uint HsvToRgb(float h, float s, float v)
        {
            int hi = (int)(h * 6) % 6;
            float f = h * 6 - hi;
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            float r, g, b;
            switch (hi)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }

            return (uint)((int)(r * 255) << 16 | (int)(g * 255) << 8 | (int)(b * 255)) | 0xFF000000;
        }

        private void BlendWithSource(ImageBuffer output, ImageBuffer source)
        {
            // Simple alpha blending with source image
            for (int y = 0; y < Math.Min(output.Height, source.Height); y++)
            {
                for (int x = 0; x < Math.Min(output.Width, source.Width); x++)
                {
                    uint fractalColor = (uint)output.GetPixel(x, y);
                    uint sourceColor = (uint)source.GetPixel(x, y);

                    // Extract RGBA components
                    byte fA = (byte)((fractalColor >> 24) & 0xFF);
                    byte fR = (byte)((fractalColor >> 16) & 0xFF);
                    byte fG = (byte)((fractalColor >> 8) & 0xFF);
                    byte fB = (byte)(fractalColor & 0xFF);

                    byte sA = (byte)((sourceColor >> 24) & 0xFF);
                    byte sR = (byte)((sourceColor >> 16) & 0xFF);
                    byte sG = (byte)((sourceColor >> 8) & 0xFF);
                    byte sB = (byte)(sourceColor & 0xFF);

                    // Alpha blending
                    float alpha = fA / 255.0f;
                    byte r = (byte)(sR * (1 - alpha) + fR * alpha);
                    byte g = (byte)(sG * (1 - alpha) + fG * alpha);
                    byte b = (byte)(sB * (1 - alpha) + fB * alpha);

                    uint blendedColor = (uint)(255 << 24 | r << 16 | g << 8 | b);
                    output.SetPixel(x, y, (int)blendedColor);
                }
            }
        }

        #endregion
    }
}
