using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// BPM Effects Node - Advanced BPM detection and beat-reactive effects
    /// Based on Winamp AVS C_THISCLASS with sophisticated beat detection algorithms
    /// </summary>
    public class BPMEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the BPM effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// BPM detection mode: 0=Manual, 1=Auto, 2=Adaptive
        /// </summary>
        public int DetectionMode { get; set; } = 1;

        /// <summary>
        /// Manual BPM value (60-200 BPM) - only used in Manual mode
        /// </summary>
        public int ManualBPM { get; set; } = 120;

        /// <summary>
        /// BPM detection sensitivity (0.0-1.0)
        /// </summary>
        public float Sensitivity { get; set; } = 0.5f;

        /// <summary>
        /// Beat reaction mode: 0=Immediate, 1=Smooth, 2=Pulse, 3=Ramp
        /// </summary>
        public int BeatMode { get; set; } = 0;

        /// <summary>
        /// Beat decay time in frames (1-60)
        /// </summary>
        public int DecayFrames { get; set; } = 10;

        /// <summary>
        /// Enable skip detection (detects rhythmic patterns)
        /// </summary>
        public bool SkipDetection { get; set; } = false;

        /// <summary>
        /// Skip threshold (0.0-1.0)
        /// </summary>
        public float SkipThreshold { get; set; } = 0.3f;

        /// <summary>
        /// Output beat intensity (0.0-1.0) - read-only
        /// </summary>
        public float BeatIntensity { get; private set; } = 0.0f;

        /// <summary>
        /// Current detected BPM - read-only
        /// </summary>
        public float CurrentBPM { get; private set; } = 120.0f;

        #endregion

        #region Private Fields

        private float _beatCounter = 0.0f;
        private int _beatFrameCounter = 0;
        private float _lastBeatEnergy = 0.0f;
        private float _energyHistory = 0.0f;
        private readonly float[] _energyBuffer = new float[43]; // ~1 second at 60fps
        private int _energyBufferIndex = 0;
        private float _adaptiveBPM = 120.0f;
        private int _beatCount = 0;
        private float _lastBeatTime = 0.0f;

        #endregion

        #region Constructor

        public BPMEffectsNode()
        {
            Name = "BPM Effects";
            Description = "Advanced BPM detection with beat-reactive modulation";
            Category = "Audio Reactive";

            // Initialize energy buffer
            for (int i = 0; i < _energyBuffer.Length; i++)
            {
                _energyBuffer[i] = 0.0f;
            }
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Audio", typeof(AudioFeatures), true, null, "Audio features for BPM detection"));
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for beat modulation"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Beat-modulated output"));
            _outputPorts.Add(new EffectPort("BeatIntensity", typeof(float), false, null, "Current beat intensity (0.0-1.0)"));
            _outputPorts.Add(new EffectPort("CurrentBPM", typeof(float), false, null, "Detected BPM"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            // Handle BPM detection
            DetectBPM(audioFeatures);

            // Handle beat intensity
            UpdateBeatIntensity();

            // Process image if available
            if (inputs.TryGetValue("Image", out var imageInput) && imageInput is ImageBuffer imageBuffer)
            {
                return ProcessImageBeatSync(imageBuffer);
            }

            // Return default output with beat data
            return new Dictionary<string, object>
            {
                ["BeatIntensity"] = BeatIntensity,
                ["CurrentBPM"] = CurrentBPM
            };
        }

        #endregion

        #region BPM Detection

        private void DetectBPM(AudioFeatures audioFeatures)
        {
            // Calculate current energy
            float currentEnergy = audioFeatures.Bass + audioFeatures.Mid * 0.5f;

            // Store in circular buffer
            _energyBuffer[_energyBufferIndex] = currentEnergy;
            _energyBufferIndex = (_energyBufferIndex + 1) % _energyBuffer.Length;

            // Calculate average energy
            float avgEnergy = 0.0f;
            for (int i = 0; i < _energyBuffer.Length; i++)
            {
                avgEnergy += _energyBuffer[i];
            }
            avgEnergy /= _energyBuffer.Length;

            // Beat detection based on mode
            switch (DetectionMode)
            {
                case 0: // Manual
                    CurrentBPM = ManualBPM;
                    DetectBeatManual(currentEnergy, avgEnergy);
                    break;

                case 1: // Auto
                    DetectBeatAuto(currentEnergy, avgEnergy);
                    break;

                case 2: // Adaptive
                    DetectBeatAdaptive(currentEnergy, avgEnergy);
                    break;
            }
        }

        private void DetectBeatManual(float currentEnergy, float avgEnergy)
        {
            // Simple threshold-based beat detection
            float threshold = avgEnergy + (avgEnergy * Sensitivity * 0.5f);

            if (currentEnergy > threshold && currentEnergy > _lastBeatEnergy * 0.8f)
            {
                TriggerBeat(currentEnergy);
            }

            _lastBeatEnergy = currentEnergy;
        }

        private void DetectBeatAuto(float currentEnergy, float avgEnergy)
        {
            // Auto threshold based on energy variance
            float variance = 0.0f;
            for (int i = 0; i < _energyBuffer.Length; i++)
            {
                float diff = _energyBuffer[i] - avgEnergy;
                variance += diff * diff;
            }
            variance /= _energyBuffer.Length;

            float threshold = avgEnergy + (float)Math.Sqrt(variance) * Sensitivity;

            if (currentEnergy > threshold && currentEnergy > _lastBeatEnergy * 0.7f)
            {
                TriggerBeat(currentEnergy);
            }

            _lastBeatEnergy = currentEnergy;
        }

        private void DetectBeatAdaptive(float currentEnergy, float avgEnergy)
        {
            // Adaptive BPM detection with tempo tracking
            const float minBPM = 60.0f;
            const float maxBPM = 200.0f;

            float threshold = avgEnergy + (avgEnergy * Sensitivity * 0.3f);

            if (currentEnergy > threshold && currentEnergy > _lastBeatEnergy * 0.9f)
            {
                float currentTime = Environment.TickCount / 1000.0f;

                if (_beatCount > 0)
                {
                    float timeDiff = currentTime - _lastBeatTime;
                    float instantBPM = 60.0f / timeDiff;

                    // Smooth BPM adaptation
                    if (instantBPM >= minBPM && instantBPM <= maxBPM)
                    {
                        _adaptiveBPM = _adaptiveBPM * 0.9f + instantBPM * 0.1f;
                        CurrentBPM = _adaptiveBPM;
                    }
                }

                TriggerBeat(currentEnergy);
                _lastBeatTime = currentTime;
                _beatCount++;
            }

            _lastBeatEnergy = currentEnergy;
        }

        private void TriggerBeat(float energy)
        {
            _beatFrameCounter = DecayFrames;
            BeatIntensity = Math.Min(1.0f, energy * 0.1f);
        }

        #endregion

        #region Beat Processing

        private void UpdateBeatIntensity()
        {
            if (_beatFrameCounter > 0)
            {
                _beatFrameCounter--;

                // Different decay modes
                switch (BeatMode)
                {
                    case 0: // Immediate
                        BeatIntensity = (_beatFrameCounter > 0) ? 1.0f : 0.0f;
                        break;

                    case 1: // Smooth
                        BeatIntensity = _beatFrameCounter / (float)DecayFrames;
                        break;

                    case 2: // Pulse
                        BeatIntensity = (float)Math.Sin((_beatFrameCounter / (float)DecayFrames) * Math.PI);
                        break;

                    case 3: // Ramp
                        float t = 1.0f - (_beatFrameCounter / (float)DecayFrames);
                        BeatIntensity = t * t; // Quadratic ease out
                        break;
                }
            }
            else
            {
                BeatIntensity = 0.0f;
            }
        }

        private ImageBuffer ProcessImageBeatSync(ImageBuffer input)
        {
            var output = new ImageBuffer(input.Width, input.Height);

            // Apply beat-reactive effects to the image
            for (int y = 0; y < input.Height; y++)
            {
                for (int x = 0; x < input.Width; x++)
                {
                    uint pixelColor = (uint)input.GetPixel(x, y);

                    // Extract color components
                    byte a = (byte)((pixelColor >> 24) & 0xFF);
                    byte r = (byte)((pixelColor >> 16) & 0xFF);
                    byte g = (byte)((pixelColor >> 8) & 0xFF);
                    byte b = (byte)(pixelColor & 0xFF);

                    // Apply beat-reactive modulation
                    float modulation = 1.0f + (BeatIntensity * 0.5f);

                    r = (byte)Math.Min(255, r * modulation);
                    g = (byte)Math.Min(255, g * modulation);
                    b = (byte)Math.Min(255, b * modulation);

                    uint resultColor = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
                    output.SetPixel(x, y, (int)resultColor);
                }
            }

            return output;
        }

        #endregion

        #region Skip Detection

        /// <summary>
        /// Detects rhythmic patterns for skip effects
        /// </summary>
        public bool DetectSkip()
        {
            if (!SkipDetection) return false;

            // Simple skip detection based on beat patterns
            // In a full implementation, this would analyze rhythmic patterns
            float rhythmStrength = CalculateRhythmStrength();

            return rhythmStrength > SkipThreshold;
        }

        private float CalculateRhythmStrength()
        {
            // Simplified rhythm analysis
            // In a real implementation, this would use autocorrelation or FFT analysis
            float rhythmSum = 0.0f;
            int rhythmPeriod = (int)(60.0f / CurrentBPM * 60.0f); // Frames per beat

            for (int i = 0; i < Math.Min(_energyBuffer.Length, rhythmPeriod); i++)
            {
                rhythmSum += _energyBuffer[i];
            }

            return rhythmSum / Math.Min(_energyBuffer.Length, rhythmPeriod);
        }

        #endregion
    }
}