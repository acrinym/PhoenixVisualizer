using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Advanced water simulation effect that creates realistic water ripples,
    /// waves, and fluid dynamics with audio reactivity.
    /// </summary>
    public class WaterEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>Whether the water effect is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Water surface resolution (32 to 256).</summary>
        public int Resolution { get; set; } = 128;

        /// <summary>Wave propagation speed (0.1 to 5.0).</summary>
        public float WaveSpeed { get; set; } = 1.0f;

        /// <summary>Wave damping factor (0.0 to 1.0).</summary>
        public float Damping { get; set; } = 0.98f;

        /// <summary>Wave amplitude multiplier (0.1 to 3.0).</summary>
        public float Amplitude { get; set; } = 1.0f;

        /// <summary>Whether to enable audio reactivity.</summary>
        public bool AudioReactive { get; set; } = true;

        /// <summary>Audio sensitivity multiplier.</summary>
        public float AudioSensitivity { get; set; } = 1.0f;

        /// <summary>Water color tint.</summary>
        public Color WaterColor { get; set; } = Color.FromArgb(255, 64, 128, 255);

        /// <summary>Reflection intensity (0.0 to 1.0).</summary>
        public float Reflection { get; set; } = 0.3f;

        /// <summary>Refraction intensity (0.0 to 1.0).</summary>
        public float Refraction { get; set; } = 0.5f;

        /// <summary>Whether to enable caustics (light focusing).</summary>
        public bool EnableCaustics { get; set; } = false;

        /// <summary>Caustics intensity (0.0 to 1.0).</summary>
        public float CausticsIntensity { get; set; } = 0.7f;

        /// <summary>Wave frequency for natural movement.</summary>
        public float WaveFrequency { get; set; } = 0.1f;

        /// <summary>Wind direction and strength.</summary>
        public float WindDirection { get; set; } = 0.0f;
        public float WindStrength { get; set; } = 0.1f;

        #endregion

        #region Private Fields

        private float[,] _heightMap = new float[1,1];
        private float[,] _velocityMap = new float[1,1];
        private float[,] _previousHeightMap = new float[1,1];
        private readonly Random _random = new Random();
        private int _frameCounter;
        private float _time;

        #endregion

        #region Constructor

        public WaterEffectsNode()
        {
            Name = "Water Effects";
            Description = "Advanced water simulation with ripples, waves, and fluid dynamics";
            Category = "Simulation Effects";

            InitializeWaterSurface();
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for water overlay"));
            _inputPorts.Add(new EffectPort("Audio", typeof(AudioFeatures), false, null, "Audio input for wave generation"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable water effect"));
            _inputPorts.Add(new EffectPort("WaveSpeed", typeof(float), false, 1.0f, "Wave propagation speed"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with water effects"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var imageObj) || imageObj is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            if (inputs.TryGetValue("Enabled", out var en))
                Enabled = (bool)en;
            if (inputs.TryGetValue("WaveSpeed", out var speed))
                WaveSpeed = Math.Clamp((float)speed, 0.1f, 5.0f);

            if (!Enabled)
                return imageBuffer;

            _frameCounter++;
            _time += 1.0f / 60.0f; // Assume 60 FPS

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height, (int[])imageBuffer.Pixels.Clone());

            UpdateWaterSurface(audioFeatures);
            RenderWaterEffect(output, imageBuffer);

            return output;
        }

        #endregion

        #region Water Surface Management

        private void InitializeWaterSurface()
        {
            _heightMap = new float[Resolution, Resolution];
            _velocityMap = new float[Resolution, Resolution];
            _previousHeightMap = new float[Resolution, Resolution];

            // Initialize with some random waves
            for (int x = 0; x < Resolution; x++)
            {
                for (int y = 0; y < Resolution; y++)
                {
                    float noise = (float)(_random.NextDouble() - 0.5) * 0.1f;
                    _heightMap[x, y] = noise;
                    _previousHeightMap[x, y] = noise;
                }
            }
        }

        private void UpdateWaterSurface(AudioFeatures audioFeatures)
        {
            // Apply natural wave movement
            ApplyNaturalWaves();

            // Apply audio-reactive waves
            if (AudioReactive && audioFeatures != null)
            {
                ApplyAudioWaves(audioFeatures);
            }

            // Apply wind effects
            ApplyWindEffects();

            // Update wave physics
            UpdateWavePhysics();

            // Store previous state
            Array.Copy(_heightMap, _previousHeightMap, _heightMap.Length);
        }

        private void ApplyNaturalWaves()
        {
            for (int x = 0; x < Resolution; x++)
            {
                for (int y = 0; y < Resolution; y++)
                {
                    float xNorm = (float)x / Resolution;
                    float yNorm = (float)y / Resolution;
                    
                    // Create natural wave patterns
                    float wave1 = (float)Math.Sin(xNorm * Math.PI * 4 + _time * WaveFrequency) * 0.02f;
                    float wave2 = (float)Math.Sin(yNorm * Math.PI * 3 + _time * WaveFrequency * 0.7f) * 0.015f;
                    float wave3 = (float)Math.Sin((xNorm + yNorm) * Math.PI * 2 + _time * WaveFrequency * 1.3f) * 0.01f;
                    
                    _heightMap[x, y] += (wave1 + wave2 + wave3) * Amplitude;
                }
            }
        }

        private void ApplyAudioWaves(AudioFeatures audioFeatures)
        {
            if (audioFeatures.SpectrumData == null || audioFeatures.SpectrumData.Length == 0)
                return;

            // Get audio energy from different frequency bands
            float lowEnergy = 0.0f, midEnergy = 0.0f, highEnergy = 0.0f;
            int spectrumLength = audioFeatures.SpectrumData.Length;
            
            for (int i = 0; i < spectrumLength; i++)
            {
                if (i < spectrumLength / 3)
                    lowEnergy += audioFeatures.SpectrumData[i];
                else if (i < 2 * spectrumLength / 3)
                    midEnergy += audioFeatures.SpectrumData[i];
                else
                    highEnergy += audioFeatures.SpectrumData[i];
            }

            lowEnergy /= spectrumLength / 3;
            midEnergy /= spectrumLength / 3;
            highEnergy /= spectrumLength / 3;

            // Apply beat-reactive waves
            if (audioFeatures.IsBeat)
            {
                float beatForce = (lowEnergy + midEnergy + highEnergy) * AudioSensitivity * 0.1f;
                CreateRipple(Resolution / 2, Resolution / 2, beatForce);
            }

            // Apply continuous audio waves
            for (int x = 0; x < Resolution; x++)
            {
                for (int y = 0; y < Resolution; y++)
                {
                    float xNorm = (float)x / Resolution;
                    float yNorm = (float)y / Resolution;
                    
                    // Low frequencies affect center
                    float centerDist = (float)Math.Sqrt((xNorm - 0.5f) * (xNorm - 0.5f) + (yNorm - 0.5f) * (yNorm - 0.5f));
                    float lowWave = lowEnergy * (1.0f - centerDist) * AudioSensitivity * 0.05f;
                    
                    // Mid frequencies create horizontal waves
                    float midWave = midEnergy * (float)Math.Sin(yNorm * Math.PI * 6 + _time * 2.0f) * AudioSensitivity * 0.03f;
                    
                    // High frequencies create vertical waves
                    float highWave = highEnergy * (float)Math.Sin(xNorm * Math.PI * 8 + _time * 3.0f) * AudioSensitivity * 0.02f;
                    
                    _heightMap[x, y] += lowWave + midWave + highWave;
                }
            }
        }

        private void ApplyWindEffects()
        {
            if (WindStrength <= 0.001f)
                return;

            for (int x = 0; x < Resolution; x++)
            {
                for (int y = 0; y < Resolution; y++)
                {
                    float xNorm = (float)x / Resolution;
                    float yNorm = (float)y / Resolution;
                    
                    // Wind creates directional waves
                    float windWave = (float)Math.Sin(
                        xNorm * Math.Cos(WindDirection) * Math.PI * 4 + 
                        yNorm * Math.Sin(WindDirection) * Math.PI * 4 + 
                        _time * WindStrength
                    ) * WindStrength * 0.02f;
                    
                    _heightMap[x, y] += windWave;
                }
            }
        }

        private void UpdateWavePhysics()
        {
            // Simple wave equation: d²h/dt² = c²∇²h - damping
            for (int x = 1; x < Resolution - 1; x++)
            {
                for (int y = 1; y < Resolution - 1; y++)
                {
                    // Laplacian approximation
                    float laplacian = _heightMap[x + 1, y] + _heightMap[x - 1, y] + 
                                    _heightMap[x, y + 1] + _heightMap[x, y - 1] - 
                                    4 * _heightMap[x, y];
                    
                    // Update velocity
                    _velocityMap[x, y] += WaveSpeed * laplacian;
                    
                    // Apply damping
                    _velocityMap[x, y] *= Damping;
                    
                    // Update height
                    _heightMap[x, y] += _velocityMap[x, y];
                }
            }

            // Boundary conditions (reflecting waves)
            for (int x = 0; x < Resolution; x++)
            {
                _heightMap[x, 0] = _heightMap[x, 1];
                _heightMap[x, Resolution - 1] = _heightMap[x, Resolution - 2];
            }
            for (int y = 0; y < Resolution; y++)
            {
                _heightMap[0, y] = _heightMap[1, y];
                _heightMap[Resolution - 1, y] = _heightMap[Resolution - 2, y];
            }
        }

        private void CreateRipple(int centerX, int centerY, float force)
        {
            for (int x = Math.Max(0, centerX - 10); x < Math.Min(Resolution, centerX + 10); x++)
            {
                for (int y = Math.Max(0, centerY - 10); y < Math.Min(Resolution, centerY + 10); y++)
                {
                    float distance = (float)Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    if (distance < 10)
                    {
                        float ripple = force * (float)Math.Exp(-distance * 0.3f);
                        _heightMap[x, y] += ripple;
                    }
                }
            }
        }

        #endregion

        #region Rendering

        private void RenderWaterEffect(ImageBuffer output, ImageBuffer input)
        {
            int width = output.Width;
            int height = output.Height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Map screen coordinates to water surface
                    int waterX = (int)((float)x / width * (Resolution - 1));
                    int waterY = (int)((float)y / height * (Resolution - 1));
                    
                    if (waterX >= 0 && waterX < Resolution && waterY >= 0 && waterY < Resolution)
                    {
                        // Get water height and calculate displacement
                        float waterHeight = _heightMap[waterX, waterY];
                        
                        // Calculate normal for lighting
                        Vector3 normal = CalculateNormal(waterX, waterY);
                        
                        // Apply water distortion
                        int sourceX = x + (int)(normal.X * waterHeight * 10);
                        int sourceY = y + (int)(normal.Y * waterHeight * 10);
                        
                        sourceX = Math.Clamp(sourceX, 0, width - 1);
                        sourceY = Math.Clamp(sourceY, 0, height - 1);
                        
                        // Get source pixel
                        int sourceColor = input.GetPixel(sourceX, sourceY);
                        
                        // Apply water tint and lighting
                        int waterColor = ApplyWaterTint(sourceColor, waterHeight, normal);
                        
                        output.SetPixel(x, y, waterColor);
                    }
                }
            }
        }

        private Vector3 CalculateNormal(int x, int y)
        {
            if (x <= 0 || x >= Resolution - 1 || y <= 0 || y >= Resolution - 1)
                return new Vector3(0, 0, 1);

            float dx = _heightMap[x + 1, y] - _heightMap[x - 1, y];
            float dy = _heightMap[x, y + 1] - _heightMap[x, y - 1];
            
            Vector3 normal = new Vector3(-dx, -dy, 1.0f);
            return Vector3.Normalize(normal);
        }

        private int ApplyWaterTint(int sourceColor, float waterHeight, Vector3 normal)
        {
            // Extract RGB components
            int r = sourceColor & 0xFF;
            int g = (sourceColor >> 8) & 0xFF;
            int b = (sourceColor >> 16) & 0xFF;

            // Apply water color tint
            float waterR = WaterColor.R / 255.0f;
            float waterG = WaterColor.G / 255.0f;
            float waterB = WaterColor.B / 255.0f;

            float heightFactor = Math.Abs(waterHeight) * 2.0f;
            float tintStrength = Math.Min(heightFactor, 0.5f);

            r = (int)(r * (1.0f - tintStrength) + waterR * 255 * tintStrength);
            g = (int)(g * (1.0f - tintStrength) + waterG * 255 * tintStrength);
            b = (int)(b * (1.0f - tintStrength) + waterB * 255 * tintStrength);

            // Apply lighting based on normal
            float lighting = 0.5f + 0.5f * normal.Z;
            r = (int)(r * lighting);
            g = (int)(g * lighting);
            b = (int)(b * lighting);

            // Clamp values
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);

            return (b << 16) | (g << 8) | r;
        }

        #endregion

        #region Public Methods

        public override void Reset()
        {
            base.Reset();
            InitializeWaterSurface();
            _frameCounter = 0;
            _time = 0;
        }

        public string GetWaterStats()
        {
            return $"Resolution: {Resolution}x{Resolution}, Frame: {_frameCounter}, Time: {_time:F2}";
        }

        public void CreateRippleAt(int screenX, int screenY, float force)
        {
            int waterX = (int)((float)screenX / 800 * (Resolution - 1)); // Assume 800x600
            int waterY = (int)((float)screenY / 600 * (Resolution - 1));
            CreateRipple(waterX, waterY, force);
        }

        public float GetWaterHeight(int x, int y)
        {
            if (x >= 0 && x < Resolution && y >= 0 && y < Resolution)
                return _heightMap[x, y];
            return 0.0f;
        }

        #endregion

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }

    /// <summary>
    /// Simple 3D vector for water calculations
    /// </summary>
    public struct Vector3
    {
        public float X, Y, Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 Normalize(Vector3 v)
        {
            float length = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            if (length < 0.0001f)
                return new Vector3(0, 0, 1);
            
            return new Vector3(v.X / length, v.Y / length, v.Z / length);
        }
    }
}
