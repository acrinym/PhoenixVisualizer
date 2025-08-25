using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Renders a 3D fountain of colored dots that respond to audio input.
    /// </summary>
    public class DotFountainEffectsNode : BaseEffectNode
    {
        #region Constants
        private const int NUM_ROT_DIV = 30;
        private const int NUM_ROT_HEIGHT = 256;
        private const int MAX_PARTICLES = NUM_ROT_DIV * NUM_ROT_HEIGHT;
        #endregion

        #region Public Properties
        public bool Enabled { get; set; } = true;
        public float RotationVelocity { get; set; } = 16.0f;
        public float Angle { get; set; } = -20.0f;
        public float BaseRadius { get; set; } = 1.0f;
        public float Intensity { get; set; } = 1.0f;
        public bool BeatResponse { get; set; } = true;
        public float AudioSensitivity { get; set; } = 1.0f;
        public float ParticleLifetime { get; set; } = 1.0f;
        public float Gravity { get; set; } = 0.05f;
        public float HeightOffset { get; set; } = -20.0f;
        public float Depth { get; set; } = 400.0f;
        public Color[] DefaultColors { get; set; } = new Color[5];
        #endregion

        #region Private Fields
        private float _currentRotation;
        private Matrix4x4 _transformationMatrix = Matrix4x4.Identity;
        private FountainPoint[,] _points;
        private int[] _colorTable;
        private int _currentWidth;
        private int _currentHeight;
        private int _frameCounter;
        private bool _isInitialized;
        #endregion

        #region Fountain Point Structure
        private struct FountainPoint
        {
            public float Radius;
            public float RadiusVelocity;
            public float Height;
            public float HeightVelocity;
            public float AngularX;
            public float AngularY;
            public int ColorIndex;
            public bool IsActive;
        }
        #endregion

        public DotFountainEffectsNode()
        {
            Name = "Dot Fountain Effects";
            Description = "Creates a 3D audio responsive fountain of dots";
            Category = "Particle Effects";

            _points = new FountainPoint[NUM_ROT_HEIGHT, NUM_ROT_DIV];
            _colorTable = new int[64];
            SetDefaultColors();
            InitializeColorTable();
        }

        #region Initialization
        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for fountain overlay"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with dot fountain"));
        }

        private void SetDefaultColors()
        {
            DefaultColors = new[]
            {
                Color.FromArgb(28, 107, 24),
                Color.FromArgb(255, 10, 35),
                Color.FromArgb(42, 29, 116),
                Color.FromArgb(144, 54, 217),
                Color.FromArgb(107, 136, 255)
            };
        }

        private void InitializeColorTable()
        {
            for (int t = 0; t < 4; t++)
            {
                Color c1 = DefaultColors[t];
                Color c2 = DefaultColors[t + 1];
                int dr = (c2.R - c1.R) / 16;
                int dg = (c2.G - c1.G) / 16;
                int db = (c2.B - c1.B) / 16;
                for (int i = 0; i < 16; i++)
                {
                    int r = Math.Clamp(c1.R + dr * i, 0, 255);
                    int g = Math.Clamp(c1.G + dg * i, 0, 255);
                    int b = Math.Clamp(c1.B + db * i, 0, 255);
                    _colorTable[t * 16 + i] = Color.FromArgb(255, r, g, b).ToArgb();
                }
            }
        }

        private void InitializeEffect(int width, int height)
        {
            if (_isInitialized && width == _currentWidth && height == _currentHeight)
                return;

            _currentWidth = width;
            _currentHeight = height;
            for (int h = 0; h < NUM_ROT_HEIGHT; h++)
                for (int r = 0; r < NUM_ROT_DIV; r++)
                    _points[h, r].IsActive = false;

            _isInitialized = true;
        }
        #endregion

        #region Processing
        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();
            if (!Enabled)
                return imageBuffer;

            InitializeEffect(imageBuffer.Width, imageBuffer.Height);
            _frameCounter++;
            UpdateTransformationMatrix();
            UpdateFountainPoints(audioFeatures);

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height, (int[])imageBuffer.Pixels.Clone());
            RenderFountain(output);
            UpdateRotation();
            return output;
        }

        private void UpdateTransformationMatrix()
        {
            Matrix4x4 rotY = Matrix4x4.CreateRotationY(_currentRotation * (float)Math.PI / 180f);
            Matrix4x4 rotX = Matrix4x4.CreateRotationX(Angle * (float)Math.PI / 180f);
            Matrix4x4 trans = Matrix4x4.CreateTranslation(0f, HeightOffset, Depth);
            _transformationMatrix = trans * rotX * rotY;
        }

        private void UpdateFountainPoints(AudioFeatures audioFeatures)
        {
            for (int h = NUM_ROT_HEIGHT - 2; h >= 0; h--)
            {
                for (int r = 0; r < NUM_ROT_DIV; r++)
                {
                    if (!_points[h, r].IsActive)
                        continue;

                    FountainPoint p = _points[h, r];
                    p.Height += p.HeightVelocity;
                    p.HeightVelocity += Gravity;
                    p.Radius += p.RadiusVelocity;
                    p.HeightVelocity *= ParticleLifetime;
                    _points[h + 1, r] = p;
                    _points[h, r].IsActive = false;
                }
            }

            GenerateNewPoints(audioFeatures);
        }

        private void GenerateNewPoints(AudioFeatures audioFeatures)
        {
            if (GetActiveParticleCount() >= MAX_PARTICLES)
                return; // cap performance

            for (int r = 0; r < NUM_ROT_DIV; r++)
            {
                float audioVal = GetAudioValue(r, audioFeatures);
                float angle = r * 2f * (float)Math.PI / NUM_ROT_DIV;
                FountainPoint p = new FountainPoint
                {
                    Radius = BaseRadius,
                    Height = 250f,
                    AngularX = (float)Math.Sin(angle),
                    AngularY = (float)Math.Cos(angle),
                    HeightVelocity = -Math.Abs(audioVal) / 200f * 2.8f,
                    ColorIndex = Math.Clamp((int)(audioVal / 4f), 0, _colorTable.Length - 1),
                    IsActive = true
                };
                _points[0, r] = p;
            }
        }

        private float GetAudioValue(int index, AudioFeatures audioFeatures)
        {
            float baseVal = 0f;
            if (audioFeatures?.SpectrumData != null && audioFeatures.SpectrumData.Length > 0)
            {
                int band = index % audioFeatures.SpectrumData.Length;
                baseVal = audioFeatures.SpectrumData[band];
            }
            float variation = (float)Math.Sin(index * 0.5f + _frameCounter * 0.1f) * 50f;
            baseVal += variation;
            if (BeatResponse && audioFeatures?.IsBeat == true)
                baseVal += 128f;
            baseVal *= AudioSensitivity;
            return Math.Clamp(baseVal, -255f, 255f);
        }

        private void RenderFountain(ImageBuffer buffer)
        {
            int w = buffer.Width;
            int h = buffer.Height;
            float persp = Math.Min(w * 440f / 640f, h * 440f / 480f);

            for (int y = 0; y < NUM_ROT_HEIGHT; y++)
            {
                for (int r = 0; r < NUM_ROT_DIV; r++)
                {
                    if (!_points[y, r].IsActive)
                        continue;
                    RenderPoint(_points[y, r], buffer, persp);
                }
            }
        }

        private void RenderPoint(FountainPoint p, ImageBuffer buffer, float persp)
        {
            Vector3 pos = new Vector3(p.AngularX * p.Radius, p.Height, p.AngularY * p.Radius);
            Vector3 tp = Vector3.Transform(pos, _transformationMatrix);
            if (tp.Z <= 1e-7f) return;
            float scale = persp / tp.Z;
            int sx = (int)(tp.X * scale) + buffer.Width / 2;
            int sy = (int)(tp.Y * scale) + buffer.Height / 2;
            if (sx < 0 || sx >= buffer.Width || sy < 0 || sy >= buffer.Height)
                return;
            int color = _colorTable[Math.Min(p.ColorIndex, _colorTable.Length - 1)];
            if (Intensity > 1f)
            {
                Color c = Color.FromArgb(color);
                int r = Math.Min(255, (int)(c.R * Intensity));
                int g = Math.Min(255, (int)(c.G * Intensity));
                int b = Math.Min(255, (int)(c.B * Intensity));
                color = Color.FromArgb(c.A, r, g, b).ToArgb();
            }
            buffer.SetPixel(sx, sy, color);
        }

        private void UpdateRotation()
        {
            _currentRotation += RotationVelocity / 5f;
            if (_currentRotation >= 360f) _currentRotation -= 360f;
            if (_currentRotation < 0f) _currentRotation += 360f;
        }

        private int GetActiveParticleCount()
        {
            int count = 0;
            for (int h = 0; h < NUM_ROT_HEIGHT; h++)
                for (int r = 0; r < NUM_ROT_DIV; r++)
                    if (_points[h, r].IsActive) count++;
            return count;
        }
        #endregion

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }
}
