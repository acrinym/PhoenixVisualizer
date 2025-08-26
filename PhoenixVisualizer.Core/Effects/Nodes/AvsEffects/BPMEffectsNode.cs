using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Comprehensive BPM detection and analysis engine
    /// Based on bpm.cpp from original AVS
    /// Provides real-time beat detection, BPM calculation, and confidence assessment
    /// </summary>
    public class BPMEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the BPM analysis is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Minimum BPM to detect
        /// </summary>
        public float MinBPM { get; set; } = 60.0f;

        /// <summary>
        /// Maximum BPM to detect
        /// </summary>
        public float MaxBPM { get; set; } = 200.0f;

        /// <summary>
        /// Beat detection sensitivity (0.0 to 1.0)
        /// </summary>
        public float Sensitivity { get; set; } = 0.5f;

        /// <summary>
        /// Confidence threshold for beat validation (0.0 to 1.0)
        /// </summary>
        public float ConfidenceThreshold { get; set; } = 0.7f;

        /// <summary>
        /// Adaptive learning rate (0.0 to 1.0)
        /// </summary>
        public float LearningRate { get; set; } = 0.1f;

        /// <summary>
        /// BPM smoothing factor (0.0 to 1.0)
        /// </summary>
        public float BPMSmoothing { get; set; } = 0.8f;

        /// <summary>
        /// Current detected BPM
        /// </summary>
        public float CurrentBPM { get; private set; } = 120.0f;

        /// <summary>
        /// Current beat confidence (0.0 to 1.0)
        /// </summary>
        public float BeatConfidence { get; private set; } = 0.0f;

        /// <summary>
        /// Whether a beat was detected this frame
        /// </summary>
        public bool BeatDetected { get; private set; } = false;

        /// <summary>
        /// Beat prediction confidence
        /// </summary>
        public float PredictionConfidence { get; private set; } = 0.0f;

        /// <summary>
        /// Time since last beat in seconds
        /// </summary>
        public float TimeSinceLastBeat { get; private set; } = 0.0f;

        #endregion

        #region Private Fields

        private readonly Queue<BeatEvent> _beatHistory = new Queue<BeatEvent>();
        private readonly Queue<float> _bpmHistory = new Queue<float>();
        private readonly List<float> _energyHistory = new List<float>();
        private readonly Queue<float> _intervalHistory = new Queue<float>();
        
        private DateTime _lastBeatTime = DateTime.MinValue;
        private DateTime _lastFrameTime = DateTime.Now;
        private float _averageEnergy = 0.0f;
        private float _energyVariance = 0.0f;
        private float _predictedBeatTime = 0.0f;
        private float _adaptiveThreshold = 0.5f;
        private int _consecutiveBeats = 0;
        private float _phaseAccumulator = 0.0f;

        private const int MAX_BEAT_HISTORY = 32;
        private const int MAX_BPM_HISTORY = 16;
        private const int MAX_ENERGY_HISTORY = 128;
        private const int MAX_INTERVAL_HISTORY = 16;

        #endregion

        #region Constructor

        public BPMEffectsNode()
        {
            Name = "BPM Effects";
            Description = "Comprehensive real-time beat detection and BPM analysis engine";
            Category = "Audio Analysis";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("AudioFeatures", typeof(AudioFeatures), true, null, "Audio features for BPM analysis"));
            _outputPorts.Add(new EffectPort("BPMData", typeof(BPMAnalysisResult), false, null, "BPM analysis results"));
        }

        #endregion

        #region Effect Processing

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                DateTime currentTime = DateTime.Now;
                float deltaTime = (float)(currentTime - _lastFrameTime).TotalSeconds;
                _lastFrameTime = currentTime;

                // Update time since last beat
                if (_lastBeatTime != DateTime.MinValue)
                {
                    TimeSinceLastBeat = (float)(currentTime - _lastBeatTime).TotalSeconds;
                }

                // Analyze energy and detect beats
                float currentEnergy = CalculateCurrentEnergy(audioFeatures);
                UpdateEnergyHistory(currentEnergy);
                
                bool beatDetected = AnalyzeBeat(currentEnergy, deltaTime);
                BeatDetected = beatDetected;

                if (beatDetected)
                {
                    ProcessBeatDetection(currentTime);
                }

                // Update BPM calculation
                UpdateBPMCalculation();

                // Update prediction
                UpdateBeatPrediction(deltaTime);

                // Create output data
                var bpmResult = new BPMAnalysisResult
                {
                    BPM = CurrentBPM,
                    BeatDetected = BeatDetected,
                    Confidence = BeatConfidence,
                    PredictionConfidence = PredictionConfidence,
                    TimeSinceLastBeat = TimeSinceLastBeat,
                    AdaptiveThreshold = _adaptiveThreshold,
                    EnergyLevel = currentEnergy,
                    PhasePosition = _phaseAccumulator
                };

                outputData["BPMData"] = bpmResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BPM Effects] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private float CalculateCurrentEnergy(AudioFeatures audioFeatures)
        {
            if (audioFeatures.FFTData == null) return 0.0f;

            // Calculate energy in bass and low-mid frequencies (most important for beat detection)
            float bassEnergy = 0.0f;
            float midEnergy = 0.0f;
            int bassBands = Math.Min(8, audioFeatures.FFTData.Length / 8);
            int midBands = Math.Min(16, audioFeatures.FFTData.Length / 4);

            for (int i = 0; i < bassBands; i++)
            {
                bassEnergy += audioFeatures.FFTData[i] * audioFeatures.FFTData[i];
            }

            for (int i = bassBands; i < midBands; i++)
            {
                midEnergy += audioFeatures.FFTData[i] * audioFeatures.FFTData[i];
            }

            // Weight bass more heavily for beat detection
            return (bassEnergy * 2.0f + midEnergy) / (bassBands * 2 + (midBands - bassBands));
        }

        private void UpdateEnergyHistory(float energy)
        {
            _energyHistory.Add(energy);
            if (_energyHistory.Count > MAX_ENERGY_HISTORY)
            {
                _energyHistory.RemoveAt(0);
            }

            // Update average energy and variance
            if (_energyHistory.Count > 10)
            {
                _averageEnergy = _energyHistory.Average();
                float variance = _energyHistory.Select(e => (e - _averageEnergy) * (e - _averageEnergy)).Average();
                _energyVariance = (float)Math.Sqrt(variance);
            }
        }

        private bool AnalyzeBeat(float currentEnergy, float deltaTime)
        {
            if (_energyHistory.Count < 10) return false;

            // Adaptive threshold calculation
            float energyThreshold = _averageEnergy + (_energyVariance * Sensitivity * 2.0f);
            _adaptiveThreshold = _adaptiveThreshold * (1 - LearningRate) + energyThreshold * LearningRate;

            // Basic energy-based beat detection
            bool energyBeat = currentEnergy > _adaptiveThreshold;

            // Temporal validation - avoid double beats
            if (energyBeat && TimeSinceLastBeat < 0.2f) // Minimum 200ms between beats
            {
                energyBeat = false;
            }

            // Prediction-based validation
            float predictionWeight = PredictionConfidence;
            if (predictionWeight > 0.5f)
            {
                float timeToPredictedBeat = Math.Abs(TimeSinceLastBeat - _predictedBeatTime);
                if (timeToPredictedBeat < 0.1f) // Within 100ms of prediction
                {
                    energyBeat = true; // Boost confidence if near prediction
                }
                else if (timeToPredictedBeat > 0.2f && energyBeat)
                {
                    energyBeat = false; // Suppress if too far from prediction
                }
            }

            // Calculate confidence
            if (energyBeat)
            {
                float energyRatio = currentEnergy / _adaptiveThreshold;
                BeatConfidence = Math.Min(1.0f, energyRatio - 1.0f);
            }
            else
            {
                BeatConfidence = 0.0f;
            }

            return energyBeat && BeatConfidence >= ConfidenceThreshold;
        }

        private void ProcessBeatDetection(DateTime beatTime)
        {
            _lastBeatTime = beatTime;
            TimeSinceLastBeat = 0.0f;
            _consecutiveBeats++;

            // Add to beat history
            var beatEvent = new BeatEvent
            {
                Time = beatTime,
                Energy = _energyHistory.LastOrDefault(),
                Confidence = BeatConfidence
            };

            _beatHistory.Enqueue(beatEvent);
            if (_beatHistory.Count > MAX_BEAT_HISTORY)
            {
                _beatHistory.Dequeue();
            }

            // Calculate interval if we have previous beats
            if (_beatHistory.Count >= 2)
            {
                var prevBeat = _beatHistory.ElementAt(_beatHistory.Count - 2);
                float interval = (float)(beatTime - prevBeat.Time).TotalSeconds;
                
                if (interval > 0.2f && interval < 2.0f) // Valid interval range
                {
                    _intervalHistory.Enqueue(interval);
                    if (_intervalHistory.Count > MAX_INTERVAL_HISTORY)
                    {
                        _intervalHistory.Dequeue();
                    }
                }
            }

            // Reset phase accumulator
            _phaseAccumulator = 0.0f;
        }

        private void UpdateBPMCalculation()
        {
            if (_intervalHistory.Count < 3) return;

            // Calculate BPM from recent intervals
            float averageInterval = _intervalHistory.Average();
            float newBPM = 60.0f / averageInterval;

            // Validate BPM range
            if (newBPM >= MinBPM && newBPM <= MaxBPM)
            {
                // Smooth BPM changes
                CurrentBPM = CurrentBPM * BPMSmoothing + newBPM * (1 - BPMSmoothing);

                _bpmHistory.Enqueue(CurrentBPM);
                if (_bpmHistory.Count > MAX_BPM_HISTORY)
                {
                    _bpmHistory.Dequeue();
                }
            }

            // Calculate BPM confidence based on consistency
            if (_bpmHistory.Count >= 5)
            {
                float bpmVariance = _bpmHistory.Select(bpm => (bpm - CurrentBPM) * (bpm - CurrentBPM)).Average();
                float bpmStability = Math.Max(0.0f, 1.0f - (bpmVariance / 100.0f)); // Normalize variance
                PredictionConfidence = bpmStability * Math.Min(1.0f, _consecutiveBeats / 8.0f);
            }
        }

        private void UpdateBeatPrediction(float deltaTime)
        {
            if (CurrentBPM > 0 && PredictionConfidence > 0.3f)
            {
                float beatInterval = 60.0f / CurrentBPM;
                _predictedBeatTime = beatInterval;
                
                // Update phase accumulator
                _phaseAccumulator += deltaTime / beatInterval;
                if (_phaseAccumulator >= 1.0f)
                {
                    _phaseAccumulator -= 1.0f;
                }
            }
            else
            {
                PredictionConfidence *= 0.95f; // Decay confidence if no stable BPM
            }
        }

        #endregion

        #region Configuration

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "MinBPM", MinBPM },
                { "MaxBPM", MaxBPM },
                { "Sensitivity", Sensitivity },
                { "ConfidenceThreshold", ConfidenceThreshold },
                { "LearningRate", LearningRate },
                { "BPMSmoothing", BPMSmoothing }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            
            if (config.TryGetValue("MinBPM", out var minBpm))
                MinBPM = Convert.ToSingle(minBpm);
            
            if (config.TryGetValue("MaxBPM", out var maxBpm))
                MaxBPM = Convert.ToSingle(maxBpm);
            
            if (config.TryGetValue("Sensitivity", out var sensitivity))
                Sensitivity = Convert.ToSingle(sensitivity);
            
            if (config.TryGetValue("ConfidenceThreshold", out var threshold))
                ConfidenceThreshold = Convert.ToSingle(threshold);
            
            if (config.TryGetValue("LearningRate", out var learningRate))
                LearningRate = Convert.ToSingle(learningRate);
            
            if (config.TryGetValue("BPMSmoothing", out var smoothing))
                BPMSmoothing = Convert.ToSingle(smoothing);
        }

        #endregion
    }

    #region Helper Classes

    public class BeatEvent
    {
        public DateTime Time { get; set; }
        public float Energy { get; set; }
        public float Confidence { get; set; }
    }

    public class BPMAnalysisResult
    {
        public float BPM { get; set; }
        public bool BeatDetected { get; set; }
        public float Confidence { get; set; }
        public float PredictionConfidence { get; set; }
        public float TimeSinceLastBeat { get; set; }
        public float AdaptiveThreshold { get; set; }
        public float EnergyLevel { get; set; }
        public float PhasePosition { get; set; }
    }

    #endregion
}