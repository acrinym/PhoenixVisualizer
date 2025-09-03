using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    /// <summary>
    /// Sacred Snowflake Visualizer - Audio-reactive sacred geometry pattern generator
    /// Creates intricate, symmetrical patterns based on audio features with fractal-like properties
    /// </summary>
    public sealed class SacredSnowflakeVisualizer : IVisualizerPlugin
    {
        // Visualizer identity
        public string Id => "sacred_snowflakes";
        public string DisplayName => "❄️ Sacred Snowflakes";

        // Pattern parameters
        private int _symmetry = 6;
        private float _radius = 200f;
        private float _rotationSpeed = 0.2f;
        private float _complexity = 3f;
        private float _layerCount = 5;
        private float _time = 0f;
        private bool _useColor = true;
        private bool _useRotation = true;
        private bool _usePulsation = true;
        private bool _useLayering = true;

        // Audio reactivity mappings
        private float _bassRadius = 0f;
        private float _midRotation = 0f;
        private float _trebleComplexity = 0f;
        private float _beatPulse = 0f;
        private float _beatDecay = 0.9f;

        private int _width;
        private int _height;

        public void Initialize(int width, int height)
        {
            Resize(width, height);
            // Initialize with default settings
            _time = 0f;
            _beatPulse = 0f;
        }

        public void Resize(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void Dispose()
        {
            // Cleanup resources if needed
        }

        public void RenderFrame(AudioFeatures audioFeatures, ISkiaCanvas canvas)
        {
            // Update time (assume 60fps for delta time)
            _time += 1f / 60f;

            // Update audio reactivity
            UpdateAudioReactivity(audioFeatures, 1f / 60f);

            // Calculate center
            float centerX = _width / 2f;
            float centerY = _height / 2f;

            // Calculate effective radius with audio reactivity
            float effectiveRadius = _radius + _bassRadius + _beatPulse;

            // Calculate rotation with audio reactivity
            float rotation = _useRotation ? _time * _rotationSpeed + _midRotation : 0f;

            // Calculate complexity with audio reactivity
            float effectiveComplexity = _complexity + _trebleComplexity;

            // Clear canvas with a dark background
            canvas.Clear(0xFF0A0A14u); // Dark blue background

            // Draw layers from back to front
            int layers = _useLayering ? (int)_layerCount : 1;
            for (int layer = 0; layer < layers; layer++)
            {
                float layerFactor = (float)layer / Math.Max(1, layers - 1);
                float layerRadius = effectiveRadius * (0.3f + 0.7f * (1f - layerFactor));
                float layerRotation = rotation + layerFactor * MathF.PI * 2f / _symmetry * 0.5f;

                // Draw the sacred snowflake pattern
                DrawSacredSnowflake(canvas, centerX, centerY, layerRadius, layerRotation,
                    effectiveComplexity, layerFactor, audioFeatures);
            }
        }

        private void UpdateAudioReactivity(AudioFeatures audioFeatures, float deltaTime)
        {
            // Update beat pulse
            if (audioFeatures.Beat && _usePulsation)
            {
                _beatPulse = _radius * 0.3f * audioFeatures.Energy;
            }
            _beatPulse *= _beatDecay;

            // Map audio features to visual parameters
            _bassRadius = _radius * 0.4f * audioFeatures.Bass;
            _midRotation = MathF.PI * 0.25f * audioFeatures.Mid;
            _trebleComplexity = 2f * audioFeatures.Treble;
        }

        private void DrawSacredSnowflake(ISkiaCanvas canvas, float centerX, float centerY, float radius,
            float rotation, float complexity, float layerFactor, AudioFeatures audioFeatures)
        {
            // Determine colors based on audio
            uint baseColor = CalculateColor(audioFeatures, layerFactor);
            uint innerColor = CalculateInnerColor(audioFeatures, layerFactor);

            // Draw the main snowflake pattern using simple circles and lines
            DrawSnowflakePattern(canvas, centerX, centerY, radius, rotation, baseColor);

            // Draw inner pattern
            if (_useLayering)
            {
                DrawSnowflakePattern(canvas, centerX, centerY, radius * 0.6f, -rotation * 0.7f, innerColor);
            }
        }

        private void DrawSnowflakePattern(ISkiaCanvas canvas, float centerX, float centerY, float radius, float rotation, uint color)
        {
            // Draw the main symmetry pattern using circles and lines
            for (int i = 0; i < _symmetry; i++)
            {
                float angle = rotation + i * MathF.PI * 2f / _symmetry;
                float x = centerX + MathF.Cos(angle) * radius;
                float y = centerY + MathF.Sin(angle) * radius;

                // Draw connecting lines from center to points
                canvas.DrawLine(centerX, centerY, x, y, color, 2f);

                // Draw circles at each symmetry point
                canvas.DrawCircle(x, y, 8f, color, false);

                // Draw smaller inner circles for complexity
                if (_complexity > 1)
                {
                    float innerX = centerX + MathF.Cos(angle) * (radius * 0.7f);
                    float innerY = centerY + MathF.Sin(angle) * (radius * 0.7f);
                    canvas.DrawCircle(innerX, innerY, 4f, color, false);
                }
            }

            // Draw center circle
            canvas.DrawCircle(centerX, centerY, 12f, color, false);

            // Draw connecting rings if complexity is high
            if (_complexity > 2)
            {
                float ringRadius = radius * 0.8f;
                for (int i = 0; i < _symmetry * 2; i++)
                {
                    float angle = rotation + i * MathF.PI / _symmetry;
                    float x = centerX + MathF.Cos(angle) * ringRadius;
                    float y = centerY + MathF.Sin(angle) * ringRadius;
                    canvas.DrawCircle(x, y, 2f, color, true);
                }
            }
        }



        private uint CalculateColor(AudioFeatures audioFeatures, float layerFactor)
        {
            if (!_useColor)
            {
                return 0xFFFFFFFF; // White
            }

            // Calculate hue based on audio features and time
            float hue = (_time * 10f + audioFeatures.Bass * 120f) % 360f;
            
            // Calculate saturation and brightness
            float saturation = 0.7f + audioFeatures.Mid * 0.3f;
            float brightness = 0.6f + audioFeatures.Treble * 0.4f;
            
            // Adjust for layer
            hue = (hue + layerFactor * 60f) % 360f;
            
            return HSVToRGB(hue, saturation, brightness);
        }

        private uint CalculateInnerColor(AudioFeatures audioFeatures, float layerFactor)
        {
            if (!_useColor)
            {
                return 0xFFCCCCCC; // Light gray
            }

            // Calculate complementary color for inner shape
            float hue = (_time * 15f + audioFeatures.Treble * 120f + 180f) % 360f;
            
            // Calculate saturation and brightness
            float saturation = 0.8f + audioFeatures.Bass * 0.2f;
            float brightness = 0.7f + audioFeatures.Mid * 0.3f;
            
            // Adjust for layer
            hue = (hue - layerFactor * 30f) % 360f;
            
            return HSVToRGB(hue, saturation, brightness);
        }

        private static uint HSVToRGB(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs(h / 60f % 2 - 1));
            float m = v - c;
            
            float r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            
            byte red = (byte)((r + m) * 255);
            byte green = (byte)((g + m) * 255);
            byte blue = (byte)((b + m) * 255);
            
            return 0xFF000000u | ((uint)red << 16) | ((uint)green << 8) | blue;
        }
    }
}
