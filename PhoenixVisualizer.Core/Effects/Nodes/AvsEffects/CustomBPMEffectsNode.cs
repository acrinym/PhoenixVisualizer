using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Custom BPM node that can groove to a fixed tempo or sync with incoming beats. 🎶
    /// </summary>
    public class CustomBPMEffectsNode : BaseEffectNode
    {
        // Core settings
        public bool Enabled { get; set; } = true;                     // Master switch
        public bool BPMEnabled { get; set; } = true;                   // Enable/disable BPM output
        public int BPM { get; set; } = 120;                            // Fixed BPM value
        public bool TempoSync { get; set; } = false;                   // Sync to audio BPM when true

        public CustomBPMEffectsNode()
        {
            Name = "Custom BPM Effects";
            Description = "Generates custom BPM values with optional tempo sync";
            Category = "AVS Effects";
        }

        protected override void InitializePorts()
        {
            _outputPorts.Add(new EffectPort("BPM", typeof(double), false, 0.0, "Current BPM value"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled || !BPMEnabled)
            {
                return GetDefaultOutput();
            }

            double bpm = BPM;
            if (TempoSync && audioFeatures != null && audioFeatures.BPM > 0)
            {
                bpm = audioFeatures.BPM; // Ride along with the incoming tempo 🎧
            }

            return bpm;
        }

        // Friendly helpers for tweaking settings 😄
        public void SetBPM(int bpm) => BPM = Math.Clamp(bpm, 1, 1000);
        public void SetBPMEnabled(bool enabled) => BPMEnabled = enabled;
        public void SetTempoSync(bool enabled) => TempoSync = enabled;

        public override string GetSettingsSummary()
        {
            string bpmInfo = BPMEnabled ? $"{BPM} BPM" : "Disabled";
            string syncInfo = TempoSync ? "Tempo-Synced" : "Fixed";
            return $"Custom BPM: {(Enabled ? "Enabled" : "Disabled")}, BPM: {bpmInfo}, Mode: {syncInfo}";
        }

        public override object GetDefaultOutput() => 0.0;
    }
}
