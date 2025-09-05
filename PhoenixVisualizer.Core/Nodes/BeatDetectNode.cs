using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;

namespace PhoenixVisualizer.Core.Nodes
{
    public class BeatDetectNode : IEffectNode
    {
        public string Name => "BeatDetect";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["sensitivity"] = new EffectParam{ Label="Sensitivity", Type="slider", Min=0f, Max=2f, FloatValue=1f },
            ["band"] = new EffectParam{ Label="Band", Type="dropdown", StringValue="All", Options=new List<string>{"All", "Bass", "Mid", "Treble"} }
        };

        private float _lastBeatTime = 0f;
        private float _beatThreshold = 0.5f;

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var canvas = ctx.Canvas;
            if (canvas == null || spectrum == null || spectrum.Length == 0) return;

            float sensitivity = Params["sensitivity"].FloatValue;
            string band = Params["band"].StringValue;
            
            // Calculate energy for the specified band
            float energy = 0f;
            int startIdx = 0, endIdx = spectrum.Length;
            
            switch (band)
            {
                case "Bass":
                    endIdx = Math.Min(spectrum.Length / 4, spectrum.Length);
                    break;
                case "Mid":
                    startIdx = spectrum.Length / 4;
                    endIdx = spectrum.Length * 3 / 4;
                    break;
                case "Treble":
                    startIdx = spectrum.Length * 3 / 4;
                    break;
            }
            
            for (int i = startIdx; i < endIdx; i++)
            {
                energy += spectrum[i];
            }
            energy /= Math.Max(1, endIdx - startIdx);
            
            // Simple beat detection
            bool isBeat = energy > _beatThreshold && (ctx.Time - _lastBeatTime) > 0.1f;
            
            if (isBeat)
            {
                _lastBeatTime = ctx.Time;
                // Trigger beat effect - could flash screen, emit particles, etc.
                FlashBeat(canvas, ctx.Width, ctx.Height, energy * sensitivity);
            }
            
            // Update threshold dynamically
            _beatThreshold = Math.Max(0.1f, energy * 0.8f);
        }

        private void FlashBeat(ISkiaCanvas canvas, int width, int height, float intensity)
        {
            // Simple beat flash effect
            uint flashColor = 0x20FFFFFF; // Semi-transparent white flash
            canvas.FillRect(0, 0, width, height, flashColor);
        }
    }
}
