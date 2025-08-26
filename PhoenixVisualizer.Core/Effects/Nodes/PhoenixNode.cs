using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes
{
    /// <summary>
    /// PhoenixNode - Base class for custom Phoenix effects in the visualization system
    /// Provides specialized functionality for audio-reactive visual effects
    /// </summary>
    public abstract class PhoenixNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Whether this node responds to beat detection
        /// </summary>
        public virtual bool IsBeatReactive { get; set; } = false;

        /// <summary>
        /// Whether this node supports real-time parameter adjustment
        /// </summary>
        public virtual bool SupportsRealtimeAdjustment { get; set; } = true;

        /// <summary>
        /// Quality level for this effect
        /// </summary>
        public virtual EffectQuality Quality { get; set; } = EffectQuality.Standard;

        /// <summary>
        /// Frame counter for temporal effects
        /// </summary>
        protected int FrameCounter { get; set; } = 0;

        /// <summary>
        /// Last beat time for beat-reactive effects
        /// </summary>
        protected double LastBeatTime { get; set; } = 0.0;

        #endregion

        #region Constructor

        protected PhoenixNode()
        {
            Category = "Phoenix";
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Check if a beat was detected in the current frame
        /// </summary>
        protected virtual bool IsBeatDetected(AudioFeatures audioFeatures)
        {
            if (audioFeatures?.IsBeat == true)
            {
                LastBeatTime = audioFeatures.Timestamp;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the time since the last beat
        /// </summary>
        protected virtual double GetTimeSinceLastBeat(AudioFeatures audioFeatures)
        {
            if (audioFeatures?.Timestamp > 0)
            {
                return audioFeatures.Timestamp - LastBeatTime;
            }
            return 0.0;
        }

        /// <summary>
        /// Get the current BPM from audio features
        /// </summary>
        protected virtual double GetCurrentBPM(AudioFeatures audioFeatures) => audioFeatures?.BPM ?? 120.0;

        /// <summary>
        /// Get the current audio intensity (0.0 to 1.0)
        /// </summary>
        protected virtual double GetAudioIntensity(AudioFeatures audioFeatures)
        {
            if (audioFeatures?.SpectrumData != null && audioFeatures.SpectrumData.Length > 0)
            {
                // Calculate average intensity from spectrum data
                double sum = 0.0;
                for (int i = 0; i < Math.Min(audioFeatures.SpectrumData.Length, 64); i++)
                {
                    sum += audioFeatures.SpectrumData[i];
                }
                return Math.Clamp(sum / 64.0, 0.0, 1.0);
            }
            return 0.0;
        }

        /// <summary>
        /// Update frame counter and handle temporal effects
        /// </summary>
        protected virtual void UpdateFrameCounter() => FrameCounter++;

        /// <summary>
        /// Get a normalized value based on the current frame
        /// </summary>
        protected virtual double GetFrameBasedValue(double frequency = 1.0) => Math.Sin(FrameCounter * frequency * 0.1) * 0.5 + 0.5;

        #endregion

        #region Overrides

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            UpdateFrameCounter();
            return ProcessPhoenixEffect(inputs, audioFeatures);
        }

        protected override void OnReset()
        {
            base.OnReset();
            FrameCounter = 0;
            LastBeatTime = 0.0;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Process the Phoenix effect with the given inputs and audio features
        /// </summary>
        /// <param name="inputs">Input data from connected nodes</param>
        /// <param name="audioFeatures">Audio features for beat-reactive effects</param>
        /// <returns>Processed output data</returns>
        protected abstract object ProcessPhoenixEffect(Dictionary<string, object> inputs, AudioFeatures audioFeatures);

        #endregion
    }
}
