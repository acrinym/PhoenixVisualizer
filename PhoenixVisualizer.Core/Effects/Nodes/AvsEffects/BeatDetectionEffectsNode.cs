using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Core beat detection system that analyzes audio input to detect beats,
    /// rhythm patterns, and provide timing information for other effects.
    /// </summary>
    public class BeatDetectionEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>Whether the beat detection system is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Beat detection sensitivity (0.1 to 5.0).</summary>
        public float Sensitivity { get; set; } = 1.0f;

        /// <summary>Minimum amplitude threshold for beat detection (0.0 to 1.0).</summary>
        public float Threshold { get; set; } = 0.3f;

        /// <summary>How quickly beat signals decay (0.0 to 1.0).</summary>
        public float DecayRate { get; set; } = 0.8f;

        /// <summary>Current detected BPM value.</summary>
        public float BPM { get; private set; } = 120.0f;

        /// <summary>Confidence level of BPM detection (0.0 to 1.0).</summary>
        public float Confidence { get; private set; } = 0.0f;

        /// <summary>Whether a beat was detected in the current frame.</summary>
        public bool IsBeat { get; private set; } = false;

        /// <summary>Current position within the beat cycle (0.0 to 1.0).</summary>
        public float BeatPhase { get; private set; } = 0.0f;

        /// <summary>Weight for low frequency analysis (0.0 to 1.0).</summary>
        public float LowBandWeight { get; set; } = 0.8f;

        /// <summary>Weight for mid frequency analysis (0.0 to 1.0).</summary>
        public float MidBandWeight { get; set; } = 0.6f;

        /// <summary>Weight for high frequency analysis (0.0 to 1.0).</summary>
        public float HighBandWeight { get; set; } = 0.4f;

        /// <summary>Weight for sub-bass frequencies (0.0 to 1.0).</summary>
        public float SubBassWeight { get; set; } = 0.9f;

        #endregion

        #region Private Fields

        private readonly Queue<float> _bpmHistory = new Queue<float>();
        private readonly Queue<float> _peakHistory = new Queue<float>();
        private readonly float[] _frequencyBuffer = new float[64];
        private float _lastBeatTime;
        private float _currentTime;
        private float _beatInterval;
        private int _frameCounter;
        // Removed unused field

        #endregion

        #region Constructor

        public BeatDetectionEffectsNode()
        {
            Name = "Beat Detection Effects";
            Description = "Core audio analysis system for beat detection and rhythm analysis";
            Category = "Audio Analysis";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Audio", typeof(AudioFeatures), true, null, "Audio input for analysis"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable beat detection"));
            _inputPorts.Add(new EffectPort("Sensitivity", typeof(float), false, 1.0f, "Beat detection sensitivity"));
            _outputPorts.Add(new EffectPort("BPM", typeof(float), false, null, "Current detected BPM"));
            _outputPorts.Add(new EffectPort("IsBeat", typeof(bool), false, null, "Beat detection signal"));
            _outputPorts.Add(new EffectPort("BeatPhase", typeof(float), false, null, "Current beat phase"));
            _outputPorts.Add(new EffectPort("Confidence", typeof(float), false, null, "Detection confidence"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Audio", out var audioObj) || audioObj is not AudioFeatures audio)
                return GetDefaultOutput();

            if (inputs.TryGetValue("Enabled", out var en))
                Enabled = (bool)en;
            if (inputs.TryGetValue("Sensitivity", out var sens))
                Sensitivity = Math.Clamp((float)sens, 0.1f, 5.0f);

            if (!Enabled || audio == null)
                return GetDefaultOutput();

            _frameCounter++;
            _currentTime += 1.0f / 60.0f; // Assume 60 FPS

            AnalyzeAudio(audio);
            UpdateBeatDetection();
            UpdateBPMCalculation();

            return GetDefaultOutput();
        }

        #endregion

        #region Audio Analysis

        private void AnalyzeAudio(AudioFeatures audio)
        {
            if (audio.SpectrumData == null || audio.SpectrumData.Length == 0)
                return;

            // Process frequency bands with weighted analysis
            float lowBandEnergy = 0.0f;
            float midBandEnergy = 0.0f;
            float highBandEnergy = 0.0f;
            float subBassEnergy = 0.0f;

            int spectrumLength = audio.SpectrumData.Length;
            int lowEnd = spectrumLength / 4;
            int midEnd = spectrumLength / 2;
            int highEnd = spectrumLength * 3 / 4;

            for (int i = 0; i < spectrumLength; i++)
            {
                float value = audio.SpectrumData[i];
                if (i < lowEnd)
                    subBassEnergy += value * SubBassWeight;
                else if (i < midEnd)
                    lowBandEnergy += value * LowBandWeight;
                else if (i < highEnd)
                    midBandEnergy += value * MidBandWeight;
                else
                    highBandEnergy += value * HighBandWeight;
            }

            // Normalize and combine
            float totalEnergy = (subBassEnergy + lowBandEnergy + midBandEnergy + highBandEnergy) * Sensitivity;
            
            // Detect peaks
            if (totalEnergy > Threshold)
            {
                _peakHistory.Enqueue(totalEnergy);
                if (_peakHistory.Count > 10)
                    _peakHistory.Dequeue();

                // Check if this is a significant beat
                if (IsSignificantBeat(totalEnergy))
                {
                    _lastBeatTime = _currentTime;
                    IsBeat = true;
                    
                    if (_beatInterval > 0)
                    {
                        float newBPM = 60.0f / _beatInterval;
                        UpdateBPMHistory(newBPM);
                    }
                }
            }

            // Decay beat signal
            IsBeat = false;
        }

        private bool IsSignificantBeat(float energy)
        {
            if (_peakHistory.Count < 3)
                return false;

            float[] peaks = _peakHistory.ToArray();
            float currentPeak = peaks[peaks.Length - 1];
            float previousPeak = peaks[peaks.Length - 2];

            // Beat must be significantly higher than previous
            return currentPeak > previousPeak * 1.2f && currentPeak > Threshold * 1.5f;
        }

        #endregion

        #region Beat Detection

        private void UpdateBeatDetection()
        {
            if (_lastBeatTime > 0)
            {
                _beatInterval = _currentTime - _lastBeatTime;
                
                // Update beat phase
                BeatPhase = (_currentTime - _lastBeatTime) / _beatInterval;
                if (BeatPhase > 1.0f)
                    BeatPhase = 0.0f;
            }
        }

        private void UpdateBPMCalculation()
        {
            if (_bpmHistory.Count == 0)
                return;

            // Calculate average BPM from history
            float totalBPM = 0.0f;
            int count = 0;
            foreach (float bpm in _bpmHistory)
            {
                totalBPM += bpm;
                count++;
            }

            if (count > 0)
            {
                float newBPM = totalBPM / count;
                
                // Apply smoothing
                BPM = BPM * 0.8f + newBPM * 0.2f;
                
                // Calculate confidence based on consistency
                float variance = 0.0f;
                foreach (float bpm in _bpmHistory)
                {
                    variance += (bpm - BPM) * (bpm - BPM);
                }
                variance /= count;
                
                Confidence = Math.Max(0.0f, 1.0f - (variance / 100.0f));
            }
        }

        private void UpdateBPMHistory(float newBPM)
        {
            // Filter out unrealistic BPM values
            if (newBPM < 60.0f || newBPM > 200.0f)
                return;

            _bpmHistory.Enqueue(newBPM);
            if (_bpmHistory.Count > 20)
                _bpmHistory.Dequeue();
        }

        #endregion

        #region Public Methods

        public override void Reset()
        {
            base.Reset();
            _bpmHistory.Clear();
            _peakHistory.Clear();
            _lastBeatTime = 0;
            _currentTime = 0;
            _beatInterval = 0;
            _frameCounter = 0;
            // _isInitialized = false; // Removed unused field
            BPM = 120.0f;
            Confidence = 0.0f;
            IsBeat = false;
            BeatPhase = 0.0f;
        }

        public string GetDetectionStats()
        {
            return $"BPM: {BPM:F1}, Confidence: {Confidence:F2}, Phase: {BeatPhase:F2}, Frame: {_frameCounter}";
        }

        public float GetAverageBPM()
        {
            if (_bpmHistory.Count == 0)
                return 0.0f;

            float total = 0.0f;
            foreach (float bpm in _bpmHistory)
                total += bpm;
            return total / _bpmHistory.Count;
        }

        public int GetPeakCount()
        {
            return _peakHistory.Count;
        }

        #endregion

        public override object GetDefaultOutput()
        {
            return new AudioFeatures();
        }
    }
}
