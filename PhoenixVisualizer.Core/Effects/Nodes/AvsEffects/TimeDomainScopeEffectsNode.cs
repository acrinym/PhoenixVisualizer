using System;
using System.Collections.Generic;
using Avalonia.Media;
using PhoenixVisualizer.Core.Audio;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class TimeDomainScopeEffectsNode : BaseEffectNode
    {
        private Color _waveColor = Colors.Lime;

        protected override void InitializePorts()
        {
            AddInputPort("Channel", typeof(OscilloscopeChannel));
            AddInputPort("Position", typeof(OscilloscopePosition));
        }

        protected override object ProcessCore(Dictionary<string, object> parameters, AudioFeatures features)
        {
            var samples = features.Waveform;
            var channel = (OscilloscopeChannel)parameters["Channel"];

            // Draw waveform line to buffer
            // OutputBuffer.DrawWaveform(samples, _waveColor, channel);
            
            // TODO: Implement actual effect logic
            return new ImageBuffer(800, 600);
        }
    }
}