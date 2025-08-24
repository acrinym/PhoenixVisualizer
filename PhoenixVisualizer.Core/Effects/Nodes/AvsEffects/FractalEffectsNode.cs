using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class FractalEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Type of fractal to generate
        /// </summary>
        public FractalType Type { get; set; } = FractalType.Mandelbrot;

        /// <summary>
        /// Maximum iterations for fractal calculation
        /// </summary>
        public int MaxIterations { get; set; } = 100;

        /// <summary>
        /// Zoom level for fractal view
        /// </summary>
        public double Zoom { get; set; } = 1.0;

        /// <summary>
        /// Center X offset for fractal view
        /// </summary>
        public double CenterX { get; set; } = 0.0;

        /// <summary>
        /// Center Y offset for fractal view
        /// </summary>
        public double CenterY { get; set; } = 0.0;

        /// <summary>
        /// Color scheme for fractal rendering
        /// </summary>
        public FractalColorScheme ColorScheme { get; set; } = FractalColorScheme.Iteration;

        /// <summary>
        /// Primary color for fractal
        /// </summary>
        public Color PrimaryColor { get; set; } = Color.Blue;

        /// <summary>
        /// Secondary color for fractal
        /// </summary>
        public Color SecondaryColor { get; set; } = Color.Red;

        /// <summary>
        /// Background color for fractal
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Black;

        /// <summary>
        /// Audio reactivity level (0.0 = none, 1.0 = full)
        /// </summary>
        public double AudioReactivity { get; set; } = 0.0;

        #endregion

        #region Enums

        public enum FractalType
        {
            Mandelbrot,
            Julia,
            BurningShip,
            Tricorn,
            Multibrot
        }

        public enum FractalColorScheme
        {
            Iteration,
            Smooth,
            Rainbow,
            Custom,
            Grayscale
        }

        #endregion

        #region Constants

        private const int MinIterations = 10;
        private const int MaxIterationsLimit = 1000;
        private const double MinZoom = 0.001;
        private const double MaxZoom = 1000.0;
        private const double MinAudioReactivity = 0.0;
        private const double MaxAudioReactivity = 1.0;

        #endregion

        #region Internal State

        private int lastWidth, lastHeight;
        private readonly object renderLock = new object();

        #endregion

        #region Constructor

        public FractalEffectsNode()
        {
            Name = "Fractal Effects";
            Description = "Generates mathematical fractal visualizations with configurable parameters";
            Category = "AVS Effects";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for fractal effects"));
            _inputPorts.Add(new EffectPort("Audio", typeof(AudioFeatures), false, null, "Audio features for reactive effects"));
            
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with fractal effects"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            lock (renderLock)
            {
                // Update dimensions if changed
                if (lastWidth != imageBuffer.Width || lastHeight != imageBuffer.Height)
                {
                    lastWidth = imageBuffer.Width;
                    lastHeight = imageBuffer.Height;
                }

                // Create output buffer
                var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

                // Calculate audio influence
                double audioInfluence = CalculateAudioInfluence(audioFeatures);

                // Generate fractal
                GenerateFractal(output, audioInfluence);

                return output;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculate audio influence on fractal parameters
        /// </summary>
        private double CalculateAudioInfluence(AudioFeatures audioFeatures)
        {
            if (audioFeatures?.SpectrumData == null || AudioReactivity <= 0.0)
                return 0.0;

            // Calculate average spectrum intensity
            double totalIntensity = 0.0;
            int sampleCount = Math.Min(audioFeatures.SpectrumData.Length, 64);
            
            for (int i = 0; i < sampleCount; i++)
            {
                totalIntensity += audioFeatures.SpectrumData[i];
            }

            double averageIntensity = totalIntensity / sampleCount;
            return averageIntensity * AudioReactivity;
        }

        /// <summary>
        /// Generate fractal visualization
        /// </summary>
        private void GenerateFractal(ImageBuffer image, double audioInfluence)
        {
            // Apply audio influence to zoom
            double currentZoom = Zoom * (1.0 + audioInfluence * 0.5);
            currentZoom = Math.Clamp(currentZoom, MinZoom, MaxZoom);

            // Calculate viewport bounds
            double viewWidth = 4.0 / currentZoom;
            double viewHeight = 4.0 / currentZoom;
            double left = CenterX - viewWidth * 0.5;
            double top = CenterY - viewHeight * 0.5;

            // Generate fractal based on type
            switch (Type)
            {
                case FractalType.Mandelbrot:
                    GenerateMandelbrot(image, left, top, viewWidth, viewHeight);
                    break;
                case FractalType.Julia:
                    GenerateJulia(image, left, top, viewWidth, viewHeight);
                    break;
                case FractalType.BurningShip:
                    GenerateBurningShip(image, left, top, viewWidth, viewHeight);
                    break;
                case FractalType.Tricorn:
                    GenerateTricorn(image, left, top, viewWidth, viewHeight);
                    break;
                case FractalType.Multibrot:
                    GenerateMultibrot(image, left, top, viewWidth, viewHeight);
                    break;
            }
        }

        /// <summary>
        /// Generate Mandelbrot set
        /// </summary>
        private void GenerateMandelbrot(ImageBuffer image, double left, double top, double width, double height)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    double real = left + (x * width) / image.Width;
                    double imag = top + (y * height) / image.Height;

                    int iterations = CalculateMandelbrotIterations(real, imag);
                    Color color = GetFractalColor(iterations);
                    
                    image.SetPixel(x, y, ColorToInt(color));
                }
            }
        }

        /// <summary>
        /// Generate Julia set
        /// </summary>
        private void GenerateJulia(ImageBuffer image, double left, double top, double width, double height)
        {
            // Julia set parameters
            double cReal = -0.7;
            double cImag = 0.27;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    double real = left + (x * width) / image.Width;
                    double imag = top + (y * height) / image.Height;

                    int iterations = CalculateJuliaIterations(real, imag, cReal, cImag);
                    Color color = GetFractalColor(iterations);
                    
                    image.SetPixel(x, y, ColorToInt(color));
                }
            }
        }

        /// <summary>
        /// Generate Burning Ship fractal
        /// </summary>
        private void GenerateBurningShip(ImageBuffer image, double left, double top, double width, double height)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    double real = left + (x * width) / image.Width;
                    double imag = top + (y * height) / image.Height;

                    int iterations = CalculateBurningShipIterations(real, imag);
                    Color color = GetFractalColor(iterations);
                    
                    image.SetPixel(x, y, ColorToInt(color));
                }
            }
        }

        /// <summary>
        /// Generate Tricorn fractal
        /// </summary>
        private void GenerateTricorn(ImageBuffer image, double left, double top, double width, double height)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    double real = left + (x * width) / image.Width;
                    double imag = top + (y * height) / image.Height;

                    int iterations = CalculateTricornIterations(real, imag);
                    Color color = GetFractalColor(iterations);
                    
                    image.SetPixel(x, y, ColorToInt(color));
                }
            }
        }

        /// <summary>
        /// Generate Multibrot fractal
        /// </summary>
        private void GenerateMultibrot(ImageBuffer image, double left, double top, double width, double height)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    double real = left + (x * width) / image.Width;
                    double imag = top + (y * height) / image.Height;

                    int iterations = CalculateMultibrotIterations(real, imag);
                    Color color = GetFractalColor(iterations);
                    
                    image.SetPixel(x, y, ColorToInt(color));
                }
            }
        }

        /// <summary>
        /// Calculate Mandelbrot set iterations
        /// </summary>
        private int CalculateMandelbrotIterations(double real, double imag)
        {
            double zReal = 0.0;
            double zImag = 0.0;
            int iterations = 0;

            while (iterations < MaxIterations)
            {
                double zRealSq = zReal * zReal;
                double zImagSq = zImag * zImag;

                if (zRealSq + zImagSq > 4.0)
                    break;

                zImag = 2.0 * zReal * zImag + imag;
                zReal = zRealSq - zImagSq + real;
                iterations++;
            }

            return iterations;
        }

        /// <summary>
        /// Calculate Julia set iterations
        /// </summary>
        private int CalculateJuliaIterations(double real, double imag, double cReal, double cImag)
        {
            double zReal = real;
            double zImag = imag;
            int iterations = 0;

            while (iterations < MaxIterations)
            {
                double zRealSq = zReal * zReal;
                double zImagSq = zImag * zImag;

                if (zRealSq + zImagSq > 4.0)
                    break;

                zImag = 2.0 * zReal * zImag + cImag;
                zReal = zRealSq - zImagSq + cReal;
                iterations++;
            }

            return iterations;
        }

        /// <summary>
        /// Calculate Burning Ship fractal iterations
        /// </summary>
        private int CalculateBurningShipIterations(double real, double imag)
        {
            double zReal = 0.0;
            double zImag = 0.0;
            int iterations = 0;

            while (iterations < MaxIterations)
            {
                double zRealSq = zReal * zReal;
                double zImagSq = zImag * zImag;

                if (zRealSq + zImagSq > 4.0)
                    break;

                zImag = 2.0 * Math.Abs(zReal) * Math.Abs(zImag) + imag;
                zReal = zRealSq - zImagSq + real;
                iterations++;
            }

            return iterations;
        }

        /// <summary>
        /// Calculate Tricorn fractal iterations
        /// </summary>
        private int CalculateTricornIterations(double real, double imag)
        {
            double zReal = 0.0;
            double zImag = 0.0;
            int iterations = 0;

            while (iterations < MaxIterations)
            {
                double zRealSq = zReal * zReal;
                double zImagSq = zImag * zImag;

                if (zRealSq + zImagSq > 4.0)
                    break;

                zImag = -2.0 * zReal * zImag + imag;
                zReal = zRealSq - zImagSq + real;
                iterations++;
            }

            return iterations;
        }

        /// <summary>
        /// Calculate Multibrot fractal iterations
        /// </summary>
        private int CalculateMultibrotIterations(double real, double imag)
        {
            double zReal = 0.0;
            double zImag = 0.0;
            int iterations = 0;
            double power = 3.0; // Cubic

            while (iterations < MaxIterations)
            {
                double zRealSq = zReal * zReal;
                double zImagSq = zImag * zImag;

                if (zRealSq + zImagSq > 4.0)
                    break;

                // Complex power calculation (simplified)
                double angle = Math.Atan2(zImag, zReal);
                double magnitude = Math.Sqrt(zRealSq + zImagSq);
                
                double newMagnitude = Math.Pow(magnitude, power);
                double newAngle = angle * power;
                
                zReal = newMagnitude * Math.Cos(newAngle) + real;
                zImag = newMagnitude * Math.Sin(newAngle) + imag;
                iterations++;
            }

            return iterations;
        }

        /// <summary>
        /// Get color for fractal based on iteration count and color scheme
        /// </summary>
        private Color GetFractalColor(int iterations)
        {
            if (iterations >= MaxIterations)
                return BackgroundColor;

            double normalizedIterations = (double)iterations / MaxIterations;

            switch (ColorScheme)
            {
                case FractalColorScheme.Iteration:
                    return GetIterationColor(normalizedIterations);
                case FractalColorScheme.Smooth:
                    return GetSmoothColor(normalizedIterations);
                case FractalColorScheme.Rainbow:
                    return GetRainbowColor(normalizedIterations);
                case FractalColorScheme.Custom:
                    return GetCustomColor(normalizedIterations);
                case FractalColorScheme.Grayscale:
                    return GetGrayscaleColor(normalizedIterations);
                default:
                    return GetIterationColor(normalizedIterations);
            }
        }

        /// <summary>
        /// Get iteration-based color
        /// </summary>
        private Color GetIterationColor(double normalizedIterations)
        {
            int intensity = (int)(normalizedIterations * 255);
            return Color.FromArgb(intensity, intensity, intensity);
        }

        /// <summary>
        /// Get smooth color interpolation
        /// </summary>
        private Color GetSmoothColor(double normalizedIterations)
        {
            // Smooth interpolation between primary and secondary colors
            int r = (int)(PrimaryColor.R * (1 - normalizedIterations) + SecondaryColor.R * normalizedIterations);
            int g = (int)(PrimaryColor.G * (1 - normalizedIterations) + SecondaryColor.G * normalizedIterations);
            int b = (int)(PrimaryColor.B * (1 - normalizedIterations) + SecondaryColor.B * normalizedIterations);
            
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Get rainbow color
        /// </summary>
        private Color GetRainbowColor(double normalizedIterations)
        {
            double hue = normalizedIterations * 360.0;
            return HsvToRgb(hue, 1.0, 1.0);
        }

        /// <summary>
        /// Get custom color scheme
        /// </summary>
        private Color GetCustomColor(double normalizedIterations)
        {
            if (normalizedIterations < 0.5)
            {
                double factor = normalizedIterations * 2.0;
                int r = (int)(BackgroundColor.R * (1 - factor) + PrimaryColor.R * factor);
                int g = (int)(BackgroundColor.G * (1 - factor) + PrimaryColor.G * factor);
                int b = (int)(BackgroundColor.B * (1 - factor) + PrimaryColor.B * factor);
                return Color.FromArgb(r, g, b);
            }
            else
            {
                double factor = (normalizedIterations - 0.5) * 2.0;
                int r = (int)(PrimaryColor.R * (1 - factor) + SecondaryColor.R * factor);
                int g = (int)(PrimaryColor.G * (1 - factor) + SecondaryColor.G * factor);
                int b = (int)(PrimaryColor.B * (1 - factor) + SecondaryColor.B * factor);
                return Color.FromArgb(r, g, b);
            }
        }

        /// <summary>
        /// Get grayscale color
        /// </summary>
        private Color GetGrayscaleColor(double normalizedIterations)
        {
            int intensity = (int)(normalizedIterations * 255);
            return Color.FromArgb(intensity, intensity, intensity);
        }

        /// <summary>
        /// Convert HSV to RGB color
        /// </summary>
        private Color HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            double r = 0, g = 0, b = 0;

            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0; b = c;
            }
            else if (h >= 300 && h < 360)
            {
                r = c; g = 0; b = x;
            }

            return Color.FromArgb(
                (int)((r + m) * 255),
                (int)((g + m) * 255),
                (int)((b + m) * 255)
            );
        }

        /// <summary>
        /// Convert Color to integer representation
        /// </summary>
        private int ColorToInt(Color color)
        {
            return (color.R << 16) | (color.G << 8) | color.B;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validate and clamp property values
        /// </summary>
        public override bool ValidateConfiguration()
        {
            MaxIterations = Math.Clamp(MaxIterations, MinIterations, MaxIterationsLimit);
            Zoom = Math.Clamp(Zoom, MinZoom, MaxZoom);
            AudioReactivity = Math.Clamp(AudioReactivity, MinAudioReactivity, MaxAudioReactivity);

            return true;
        }

        /// <summary>
        /// Get a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            return $"Fractal: {Type}, Iterations: {MaxIterations}, " +
                   $"Zoom: {Zoom:F2}, Audio: {AudioReactivity:F2}";
        }

        #endregion

        #region Overrides

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(1, 1);
        }

        #endregion
    }
}
