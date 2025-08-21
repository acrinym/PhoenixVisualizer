using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace PhoenixVisualizer.Core.Avs;

/// <summary>
/// Core AVS effects system for PhoenixVisualizer
/// Implements the missing effects: Trans, Channel shift, Color map, Convolution, Texer, ns-eel math
/// </summary>
public static class AvsEffects
{
    /// <summary>
    /// Transition effects for smooth blending between visual states
    /// </summary>
    public static class Trans
    {
        /// <summary>
        /// Fade transition between two color values
        /// </summary>
        public static Vector4 Fade(Vector4 from, Vector4 to, float progress)
        {
            return Vector4.Lerp(from, to, Math.Clamp(progress, 0f, 1f));
        }

        /// <summary>
        /// Slide transition with directional movement
        /// </summary>
        public static Vector2 Slide(Vector2 from, Vector2 to, float progress, Vector2 direction)
        {
            var slideOffset = direction * (1f - progress);
            return Vector2.Lerp(from, to, progress) + slideOffset;
        }

        /// <summary>
        /// Zoom transition with scaling
        /// </summary>
        public static Vector2 Zoom(Vector2 center, Vector2 point, float progress, float scaleFactor = 2f)
        {
            var scale = 1f + (scaleFactor - 1f) * progress;
            var offset = point - center;
            return center + offset * scale;
        }

        /// <summary>
        /// Rotate transition with angular movement
        /// </summary>
        public static Vector2 Rotate(Vector2 center, Vector2 point, float progress, float angleRadians)
        {
            var cos = MathF.Cos(angleRadians * progress);
            var sin = MathF.Sin(angleRadians * progress);
            var offset = point - center;
            var rotated = new Vector2(
                offset.X * cos - offset.Y * sin,
                offset.X * sin + offset.Y * cos
            );
            return center + rotated;
        }

        /// <summary>
        /// Morph transition using bezier curves
        /// </summary>
        public static Vector2 Morph(Vector2 from, Vector2 to, Vector2 control1, Vector2 control2, float progress)
        {
            var t = Math.Clamp(progress, 0f, 1f);
            var invT = 1f - t;
            
            return from * (invT * invT * invT) +
                   control1 * (3f * t * invT * invT) +
                   control2 * (3f * t * t * invT) +
                   to * (t * t * t);
        }
    }

    /// <summary>
    /// Channel shift effects for audio-visual synchronization
    /// </summary>
    public static class ChannelShift
    {
        /// <summary>
        /// Shift RGB channels independently
        /// </summary>
        public static Vector4 ShiftChannels(Vector4 color, Vector3 shift, float intensity = 1f)
        {
            var shifted = new Vector4(
                Math.Clamp(color.X + shift.X * intensity, 0f, 1f),
                Math.Clamp(color.Y + shift.Y * intensity, 0f, 1f),
                Math.Clamp(color.Z + shift.Z * intensity, 0f, 1f),
                color.W
            );
            return shifted;
        }

        /// <summary>
        /// Shift channels based on audio frequency bands
        /// </summary>
        public static Vector4 FrequencyShift(Vector4 color, float bass, float mid, float treble)
        {
            var shift = new Vector3(
                bass * 0.3f,    // Red shift from bass
                mid * 0.3f,     // Green shift from mid
                treble * 0.3f   // Blue shift from treble
            );
            return ShiftChannels(color, shift);
        }

        /// <summary>
        /// Shift channels based on beat detection
        /// </summary>
        public static Vector4 BeatShift(Vector4 color, bool isBeat, float beatIntensity)
        {
            if (!isBeat) return color;
            
            var shift = new Vector3(
                beatIntensity * 0.5f,
                beatIntensity * 0.3f,
                beatIntensity * 0.7f
            );
            return ShiftChannels(color, shift);
        }

        /// <summary>
        /// Shift channels based on audio waveform
        /// </summary>
        public static Vector4 WaveformShift(Vector4 color, float[] waveform, int sampleIndex)
        {
            if (waveform == null || sampleIndex >= waveform.Length) return color;
            
            var sample = waveform[sampleIndex];
            var shift = new Vector3(
                sample * 0.4f,
                sample * 0.2f,
                sample * 0.6f
            );
            return ShiftChannels(color, shift);
        }
    }

    /// <summary>
    /// Enhanced color mapping effects
    /// </summary>
    public static class ColorMap
    {
        /// <summary>
        /// Map grayscale to color using a gradient
        /// </summary>
        public static Vector4 GrayscaleToColor(float grayscale, Vector4[] colorGradient)
        {
            if (colorGradient == null || colorGradient.Length == 0)
                return new Vector4(grayscale, grayscale, grayscale, 1f);

            var index = grayscale * (colorGradient.Length - 1);
            var lowIndex = (int)Math.Floor(index);
            var highIndex = Math.Min(lowIndex + 1, colorGradient.Length - 1);
            var blend = index - lowIndex;

            if (lowIndex == highIndex) return colorGradient[lowIndex];
            
            return Vector4.Lerp(colorGradient[lowIndex], colorGradient[highIndex], blend);
        }

        /// <summary>
        /// Create a rainbow color gradient
        /// </summary>
        public static Vector4[] CreateRainbowGradient(int steps)
        {
            var gradient = new Vector4[steps];
            for (int i = 0; i < steps; i++)
            {
                var hue = (float)i / (steps - 1);
                gradient[i] = HsvToRgb(hue, 1f, 1f);
            }
            return gradient;
        }

        /// <summary>
        /// Create a fire color gradient
        /// </summary>
        public static Vector4[] CreateFireGradient(int steps)
        {
            var gradient = new Vector4[steps];
            for (int i = 0; i < steps; i++)
            {
                var t = (float)i / (steps - 1);
                if (t < 0.5f)
                {
                    // Black to red
                    var intensity = t * 2f;
                    gradient[i] = new Vector4(intensity, 0f, 0f, 1f);
                }
                else
                {
                    // Red to yellow to white
                    var intensity = (t - 0.5f) * 2f;
                    gradient[i] = new Vector4(1f, intensity, intensity, 1f);
                }
            }
            return gradient;
        }

        /// <summary>
        /// Convert HSV to RGB color space
        /// </summary>
        public static Vector4 HsvToRgb(float h, float s, float v)
        {
            var c = v * s;
            var x = c * (1f - Math.Abs((h * 6f) % 2f - 1f));
            var m = v - c;

            Vector3 rgb;
            if (h < 1f / 6f)
                rgb = new Vector3(c, x, 0f);
            else if (h < 2f / 6f)
                rgb = new Vector3(x, c, 0f);
            else if (h < 3f / 6f)
                rgb = new Vector3(0f, c, x);
            else if (h < 4f / 6f)
                rgb = new Vector3(0f, x, c);
            else if (h < 5f / 6f)
                rgb = new Vector3(x, 0f, c);
            else
                rgb = new Vector3(c, 0f, x);

            return new Vector4(rgb.X + m, rgb.Y + m, rgb.Z + m, 1f);
        }
    }

    /// <summary>
    /// Enhanced convolution effects for image processing
    /// </summary>
    public static class Convolution
    {
        /// <summary>
        /// Apply a convolution kernel to a 2D array
        /// </summary>
        public static float[,] ApplyKernel(float[,] input, float[,] kernel)
        {
            var inputHeight = input.GetLength(0);
            var inputWidth = input.GetLength(1);
            var kernelHeight = kernel.GetLength(0);
            var kernelWidth = kernel.GetLength(1);
            
            var output = new float[inputHeight, inputWidth];
            var kernelCenterY = kernelHeight / 2;
            var kernelCenterX = kernelWidth / 2;

            for (int y = 0; y < inputHeight; y++)
            {
                for (int x = 0; x < inputWidth; x++)
                {
                    float sum = 0f;
                    float weightSum = 0f;

                    for (int ky = 0; ky < kernelHeight; ky++)
                    {
                        for (int kx = 0; kx < kernelWidth; kx++)
                        {
                            var inputY = y + ky - kernelCenterY;
                            var inputX = x + kx - kernelCenterX;

                            if (inputY >= 0 && inputY < inputHeight && 
                                inputX >= 0 && inputX < inputWidth)
                            {
                                sum += input[inputY, inputX] * kernel[ky, kx];
                                weightSum += kernel[ky, kx];
                            }
                        }
                    }

                    output[y, x] = weightSum != 0 ? sum / weightSum : 0f;
                }
            }

            return output;
        }

        /// <summary>
        /// Create a Gaussian blur kernel
        /// </summary>
        public static float[,] CreateGaussianKernel(int size, float sigma)
        {
            var kernel = new float[size, size];
            var center = size / 2;
            var sum = 0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var distance = Math.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    var value = (float)Math.Exp(-(distance * distance) / (2 * sigma * sigma));
                    kernel[y, x] = value;
                    sum += value;
                }
            }

            // Normalize
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[y, x] /= sum;
                }
            }

            return kernel;
        }

        /// <summary>
        /// Create an edge detection kernel
        /// </summary>
        public static float[,] CreateEdgeDetectionKernel()
        {
            return new float[,]
            {
                { -1, -1, -1 },
                { -1,  8, -1 },
                { -1, -1, -1 }
            };
        }

        /// <summary>
        /// Create a sharpening kernel
        /// </summary>
        public static float[,] CreateSharpeningKernel()
        {
            return new float[,]
            {
                {  0, -1,  0 },
                { -1,  5, -1 },
                {  0, -1,  0 }
            };
        }
    }

    /// <summary>
    /// Enhanced Texer effects for texture generation
    /// </summary>
    public static class Texer
    {
        /// <summary>
        /// Generate noise texture
        /// </summary>
        public static float[,] GenerateNoise(int width, int height, float scale = 1f, int seed = 0)
        {
            var random = new Random(seed);
            var noise = new float[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noise[y, x] = (float)random.NextDouble() * scale;
                }
            }

            return noise;
        }

        /// <summary>
        /// Generate Perlin noise texture
        /// </summary>
        public static float[,] GeneratePerlinNoise(int width, int height, float scale = 1f, int octaves = 4)
        {
            var noise = new float[height, width];
            var amplitude = 1f;
            var frequency = 1f;
            var maxValue = 0f;

            for (int octave = 0; octave < octaves; octave++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var sampleX = x * frequency / width;
                        var sampleY = y * frequency / height;
                        var perlinValue = PerlinNoise(sampleX, sampleY) * amplitude;
                        noise[y, x] += perlinValue;
                    }
                }

                maxValue += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            // Normalize
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noise[y, x] = (noise[y, x] / maxValue) * scale;
                }
            }

            return noise;
        }

        /// <summary>
        /// Generate cellular texture
        /// </summary>
        public static float[,] GenerateCellular(int width, int height, int cellCount = 16, float scale = 1f)
        {
            var random = new Random();
            var cells = new Vector2[cellCount];
            var noise = new float[height, width];

            // Generate random cell centers
            for (int i = 0; i < cellCount; i++)
            {
                cells[i] = new Vector2(
                    (float)random.NextDouble() * width,
                    (float)random.NextDouble() * height
                );
            }

            // Calculate distance to nearest cell for each pixel
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var minDistance = float.MaxValue;
                    var pixel = new Vector2(x, y);

                    foreach (var cell in cells)
                    {
                        var distance = Vector2.Distance(pixel, cell);
                        minDistance = Math.Min(minDistance, distance);
                    }

                    noise[y, x] = minDistance * scale;
                }
            }

            return noise;
        }

        /// <summary>
        /// Simple Perlin noise implementation
        /// </summary>
        private static float PerlinNoise(float x, float y)
        {
            // Simplified Perlin noise - in production, use a proper implementation
            var n = (int)(x + y * 57);
            n = (n << 13) ^ n;
            return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
        }
    }

    /// <summary>
    /// Enhanced ns-eel math functions for AVS expressions
    /// </summary>
    public static class NsEelMath
    {
        /// <summary>
        /// Evaluate a mathematical expression string
        /// </summary>
        public static float Evaluate(string expression, Dictionary<string, float>? variables = null)
        {
            // Simple expression evaluator - in production, use a proper math parser
            try
            {
                // Replace variables with values
                if (variables != null)
                {
                    foreach (var kvp in variables)
                    {
                        expression = expression.Replace(kvp.Key, kvp.Value.ToString());
                    }
                }

                // Basic arithmetic evaluation (simplified)
                return EvaluateBasicExpression(expression);
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// Basic mathematical expression evaluator
        /// </summary>
        private static float EvaluateBasicExpression(string expression)
        {
            // This is a simplified evaluator - in production, use a proper math parser
            // For now, just handle basic arithmetic
            expression = expression.Replace(" ", "");
            
            // Handle basic operations
            if (expression.Contains("+"))
            {
                var parts = expression.Split('+');
                return parts.Select(p => EvaluateBasicExpression(p)).Sum();
            }
            if (expression.Contains("-"))
            {
                var parts = expression.Split('-');
                var first = EvaluateBasicExpression(parts[0]);
                var rest = parts.Skip(1).Select(p => EvaluateBasicExpression(p)).Sum();
                return first - rest;
            }
            if (expression.Contains("*"))
            {
                var parts = expression.Split('*');
                return parts.Select(p => EvaluateBasicExpression(p)).Aggregate(1f, (a, b) => a * b);
            }
            if (expression.Contains("/"))
            {
                var parts = expression.Split('/');
                var first = EvaluateBasicExpression(parts[0]);
                var rest = parts.Skip(1).Select(p => EvaluateBasicExpression(p)).Aggregate(1f, (a, b) => a * b);
                return first / rest;
            }

            // Try to parse as float
            if (float.TryParse(expression, out float result))
                return result;

            return 0f;
        }

        /// <summary>
        /// Trigonometric functions
        /// </summary>
        public static float Sin(float x) => MathF.Sin(x);
        public static float Cos(float x) => MathF.Cos(x);
        public static float Tan(float x) => MathF.Tan(x);
        public static float Asin(float x) => MathF.Asin(x);
        public static float Acos(float x) => MathF.Acos(x);
        public static float Atan(float x) => MathF.Atan(x);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);

        /// <summary>
        /// Exponential and logarithmic functions
        /// </summary>
        public static float Exp(float x) => MathF.Exp(x);
        public static float Log(float x) => MathF.Log(x);
        public static float Log10(float x) => MathF.Log10(x);
        public static float Pow(float x, float y) => MathF.Pow(x, y);
        public static float Sqrt(float x) => MathF.Sqrt(x);

        /// <summary>
        /// Utility functions
        /// </summary>
        public static float Abs(float x) => MathF.Abs(x);
        public static float Min(float a, float b) => MathF.Min(a, b);
        public static float Max(float a, float b) => MathF.Max(a, b);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static float Floor(float x) => MathF.Floor(x);
        public static float Ceiling(float x) => MathF.Ceiling(x);
        public static float Round(float x) => MathF.Round(x);

        /// <summary>
        /// Random number generation
        /// </summary>
        private static readonly Random _random = new Random();
        public static float Random() => (float)_random.NextDouble();
        public static float Random(float min, float max) => min + (float)_random.NextDouble() * (max - min);
    }

    /// <summary>
    /// Clear frame effects for resetting visual state
    /// </summary>
    public static class ClearFrame
    {
        /// <summary>
        /// Clear frame with solid color
        /// </summary>
        public static void ClearSolid(Vector4[,] frameBuffer, Vector4 color)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    frameBuffer[y, x] = color;
                }
            }
        }

        /// <summary>
        /// Clear frame with gradient
        /// </summary>
        public static void ClearGradient(Vector4[,] frameBuffer, Vector4 topLeft, Vector4 topRight, Vector4 bottomLeft, Vector4 bottomRight)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            
            for (int y = 0; y < height; y++)
            {
                var yProgress = (float)y / (height - 1);
                for (int x = 0; x < width; x++)
                {
                    var xProgress = (float)x / (width - 1);
                    
                    var top = Vector4.Lerp(topLeft, topRight, xProgress);
                    var bottom = Vector4.Lerp(bottomLeft, bottomRight, xProgress);
                    frameBuffer[y, x] = Vector4.Lerp(top, bottom, yProgress);
                }
            }
        }

        /// <summary>
        /// Clear frame with alpha blending
        /// </summary>
        public static void ClearBlend(Vector4[,] frameBuffer, Vector4 color, float alpha)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    frameBuffer[y, x] = Vector4.Lerp(frameBuffer[y, x], color, alpha);
                }
            }
        }

        /// <summary>
        /// Clear frame with motion blur
        /// </summary>
        public static void ClearMotionBlur(Vector4[,] frameBuffer, float decay = 0.95f)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    frameBuffer[y, x] *= decay;
                }
            }
        }
    }

    /// <summary>
    /// SuperScope effects for creating dynamic visualizations
    /// </summary>
    public static class SuperScope
    {
        /// <summary>
        /// SuperScope context for maintaining state
        /// </summary>
        public class ScopeContext
        {
            public Dictionary<string, float> Variables { get; } = new();
            public float Time { get; set; }
            public float[] AudioData { get; set; } = Array.Empty<float>();
            public float[] SpectrumData { get; set; } = Array.Empty<float>();
            public bool IsBeat { get; set; }
            public float BeatIntensity { get; set; }
        }

        /// <summary>
        /// Simple oscilloscope visualization
        /// </summary>
        public static Vector2[] CreateOscilloscope(ScopeContext context, int pointCount = 128)
        {
            var points = new Vector2[pointCount];
            
            for (int i = 0; i < pointCount; i++)
            {
                var t = (float)i / (pointCount - 1);
                var x = t * 2f - 1f; // -1 to 1
                
                var audioIndex = (int)(t * (context.AudioData.Length - 1));
                var y = audioIndex < context.AudioData.Length ? context.AudioData[audioIndex] : 0f;
                
                points[i] = new Vector2(x, y);
            }
            
            return points;
        }

        /// <summary>
        /// Spectrum analyzer visualization
        /// </summary>
        public static Vector2[] CreateSpectrum(ScopeContext context, int pointCount = 64)
        {
            var points = new Vector2[pointCount];
            
            for (int i = 0; i < pointCount; i++)
            {
                var t = (float)i / (pointCount - 1);
                var x = t * 2f - 1f; // -1 to 1
                
                var spectrumIndex = (int)(t * (context.SpectrumData.Length - 1));
                var y = spectrumIndex < context.SpectrumData.Length ? context.SpectrumData[spectrumIndex] : 0f;
                
                points[i] = new Vector2(x, -y); // Negative for upward bars
            }
            
            return points;
        }

        /// <summary>
        /// Circular oscilloscope
        /// </summary>
        public static Vector2[] CreateCircularScope(ScopeContext context, int pointCount = 128, float radius = 0.5f)
        {
            var points = new Vector2[pointCount];
            
            for (int i = 0; i < pointCount; i++)
            {
                var angle = (float)i / pointCount * MathF.PI * 2f;
                var audioIndex = i % context.AudioData.Length;
                var amplitude = audioIndex < context.AudioData.Length ? context.AudioData[audioIndex] : 0f;
                
                var effectiveRadius = radius + amplitude * 0.3f;
                points[i] = new Vector2(
                    MathF.Cos(angle) * effectiveRadius,
                    MathF.Sin(angle) * effectiveRadius
                );
            }
            
            return points;
        }

        /// <summary>
        /// Tunnel visualization
        /// </summary>
        public static Vector2[] CreateTunnel(ScopeContext context, int rings = 8, int pointsPerRing = 16)
        {
            var allPoints = new List<Vector2>();
            
            for (int ring = 0; ring < rings; ring++)
            {
                var t = (float)ring / (rings - 1);
                var radius = 0.1f + t * 0.7f;
                
                // Audio modulation
                var audioIndex = ring % context.AudioData.Length;
                var audioMod = audioIndex < context.AudioData.Length ? context.AudioData[audioIndex] * 0.2f : 0f;
                radius += audioMod;
                
                for (int point = 0; point < pointsPerRing; point++)
                {
                    var angle = (float)point / pointsPerRing * MathF.PI * 2f + context.Time * 0.5f;
                    allPoints.Add(new Vector2(
                        MathF.Cos(angle) * radius,
                        MathF.Sin(angle) * radius
                    ));
                }
            }
            
            return allPoints.ToArray();
        }

        /// <summary>
        /// Spirograph pattern
        /// </summary>
        public static Vector2[] CreateSpirograph(ScopeContext context, int pointCount = 256, float R = 0.7f, float r = 0.3f, float d = 0.5f)
        {
            var points = new Vector2[pointCount];
            var audioAvg = context.AudioData.Length > 0 ? context.AudioData.Average() : 0f;
            var timeOffset = context.Time * 2f;
            
            for (int i = 0; i < pointCount; i++)
            {
                var t = (float)i / pointCount * MathF.PI * 8f + timeOffset;
                
                // Audio modulation
                var audioMod = audioAvg * 0.3f;
                var effectiveR = R + audioMod;
                var effectiveD = d + audioMod * 0.5f;
                
                var x = (effectiveR - r) * MathF.Cos(t) + effectiveD * MathF.Cos((effectiveR - r) / r * t);
                var y = (effectiveR - r) * MathF.Sin(t) - effectiveD * MathF.Sin((effectiveR - r) / r * t);
                
                points[i] = new Vector2(x * 0.3f, y * 0.3f);
            }
            
            return points;
        }

        /// <summary>
        /// Lissajous curves
        /// </summary>
        public static Vector2[] CreateLissajous(ScopeContext context, int pointCount = 256, float freqX = 3f, float freqY = 2f)
        {
            var points = new Vector2[pointCount];
            var audioAvg = context.AudioData.Length > 0 ? context.AudioData.Average() : 0f;
            var timeOffset = context.Time;
            
            for (int i = 0; i < pointCount; i++)
            {
                var t = (float)i / pointCount * MathF.PI * 2f;
                
                // Audio modulation
                var audioMod = audioAvg * 2f;
                var effectiveFreqX = freqX + audioMod;
                var effectiveFreqY = freqY + audioMod * 0.7f;
                
                var x = MathF.Sin(effectiveFreqX * t + timeOffset) * 0.7f;
                var y = MathF.Sin(effectiveFreqY * t + timeOffset * 1.3f) * 0.7f;
                
                points[i] = new Vector2(x, y);
            }
            
            return points;
        }
    }

    /// <summary>
    /// Movement effects for dynamic positioning
    /// </summary>
    public static class Movement
    {
        /// <summary>
        /// Rotate coordinates around a center point
        /// </summary>
        public static Vector2 Rotate(Vector2 point, Vector2 center, float angleRadians)
        {
            var cos = MathF.Cos(angleRadians);
            var sin = MathF.Sin(angleRadians);
            var offset = point - center;
            return center + new Vector2(
                offset.X * cos - offset.Y * sin,
                offset.X * sin + offset.Y * cos
            );
        }

        /// <summary>
        /// Scale coordinates from a center point
        /// </summary>
        public static Vector2 Scale(Vector2 point, Vector2 center, float scale)
        {
            return center + (point - center) * scale;
        }

        /// <summary>
        /// Apply wave distortion
        /// </summary>
        public static Vector2 WaveDistort(Vector2 point, float time, float amplitude = 0.1f, float frequency = 2f)
        {
            var wave = MathF.Sin(point.X * frequency + time) * amplitude;
            return new Vector2(point.X, point.Y + wave);
        }

        /// <summary>
        /// Apply ripple effect
        /// </summary>
        public static Vector2 Ripple(Vector2 point, Vector2 center, float time, float amplitude = 0.1f, float frequency = 4f)
        {
            var distance = Vector2.Distance(point, center);
            var ripple = MathF.Sin(distance * frequency - time * 5f) * amplitude;
            var direction = Vector2.Normalize(point - center);
            return point + direction * ripple;
        }
    }

    /// <summary>
    /// Mirror effects for symmetrical visualizations
    /// </summary>
    public static class Mirror
    {
        /// <summary>
        /// Horizontal mirror
        /// </summary>
        public static void HorizontalMirror(Vector4[,] frameBuffer)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            var halfHeight = height / 2;
            
            for (int y = 0; y < halfHeight; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var mirrorY = height - 1 - y;
                    frameBuffer[mirrorY, x] = frameBuffer[y, x];
                }
            }
        }

        /// <summary>
        /// Vertical mirror
        /// </summary>
        public static void VerticalMirror(Vector4[,] frameBuffer)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            var halfWidth = width / 2;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < halfWidth; x++)
                {
                    var mirrorX = width - 1 - x;
                    frameBuffer[y, mirrorX] = frameBuffer[y, x];
                }
            }
        }

        /// <summary>
        /// Quadrant mirror (all four quadrants)
        /// </summary>
        public static void QuadrantMirror(Vector4[,] frameBuffer)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            var halfHeight = height / 2;
            var halfWidth = width / 2;
            
            for (int y = 0; y < halfHeight; y++)
            {
                for (int x = 0; x < halfWidth; x++)
                {
                    var color = frameBuffer[y, x];
                    frameBuffer[y, width - 1 - x] = color;
                    frameBuffer[height - 1 - y, x] = color;
                    frameBuffer[height - 1 - y, width - 1 - x] = color;
                }
            }
        }
    }

    /// <summary>
    /// Awesome built-in effects
    /// </summary>
    public static class AwesomeEffects
    {
        /// <summary>
        /// Matrix rain effect
        /// </summary>
        public static void MatrixRain(Vector4[,] frameBuffer, Random random, float intensity = 1f)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            
            // Fade existing content
            ClearFrame.ClearMotionBlur(frameBuffer, 0.92f);
            
            // Add new rain drops
            for (int x = 0; x < width; x += 8)
            {
                if (random.NextDouble() < 0.1 * intensity)
                {
                    var y = random.Next(height);
                    var green = 0.5f + (float)random.NextDouble() * 0.5f;
                    frameBuffer[y, x] = new Vector4(0f, green, 0f, 1f);
                }
            }
        }

        /// <summary>
        /// Plasma effect
        /// </summary>
        public static void Plasma(Vector4[,] frameBuffer, float time, float scale = 1f)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var nx = (float)x / width * scale;
                    var ny = (float)y / height * scale;
                    
                    var plasma = MathF.Sin(nx * 10f + time) +
                                MathF.Sin(ny * 10f + time * 1.3f) +
                                MathF.Sin((nx + ny) * 8f + time * 0.7f) +
                                MathF.Sin(MathF.Sqrt(nx * nx + ny * ny) * 12f + time * 2f);
                    
                    plasma = (plasma + 4f) / 8f; // Normalize to 0-1
                    
                    var hue = plasma;
                    frameBuffer[y, x] = ColorMap.HsvToRgb(hue, 1f, 1f);
                }
            }
        }

        /// <summary>
        /// Starfield effect
        /// </summary>
        public static void Starfield(Vector4[,] frameBuffer, List<Vector3> stars, float speed = 1f)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            
            // Clear frame
            ClearFrame.ClearSolid(frameBuffer, Vector4.Zero);
            
            // Update and draw stars
            for (int i = 0; i < stars.Count; i++)
            {
                var star = stars[i];
                star.Z -= speed;
                
                if (star.Z <= 0)
                {
                    // Reset star
                    var random = new Random();
                    star = new Vector3(
                        (float)random.NextDouble() * 2f - 1f,
                        (float)random.NextDouble() * 2f - 1f,
                        10f
                    );
                }
                
                // Project to 2D
                var screenX = (int)((star.X / star.Z + 1f) * width * 0.5f);
                var screenY = (int)((star.Y / star.Z + 1f) * height * 0.5f);
                
                if (screenX >= 0 && screenX < width && screenY >= 0 && screenY < height)
                {
                    var brightness = 1f / star.Z;
                    frameBuffer[screenY, screenX] = new Vector4(brightness, brightness, brightness, 1f);
                }
                
                stars[i] = star;
            }
        }

        /// <summary>
        /// Mandelbrot fractal
        /// </summary>
        public static void Mandelbrot(Vector4[,] frameBuffer, float centerX = 0f, float centerY = 0f, float zoom = 1f, int maxIterations = 80)
        {
            var height = frameBuffer.GetLength(0);
            var width = frameBuffer.GetLength(1);
            
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    var x0 = (px - width * 0.5f) / (width * 0.25f * zoom) + centerX;
                    var y0 = (py - height * 0.5f) / (height * 0.25f * zoom) + centerY;
                    
                    var x = 0f;
                    var y = 0f;
                    var iteration = 0;
                    
                    while (x * x + y * y <= 4f && iteration < maxIterations)
                    {
                        var xtemp = x * x - y * y + x0;
                        y = 2f * x * y + y0;
                        x = xtemp;
                        iteration++;
                    }
                    
                    var color = iteration == maxIterations ? Vector4.Zero : 
                               ColorMap.HsvToRgb((float)iteration / maxIterations, 1f, 1f);
                    frameBuffer[py, px] = color;
                }
            }
        }
    }
}
