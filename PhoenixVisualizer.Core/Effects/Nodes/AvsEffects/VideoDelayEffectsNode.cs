using System;
using System.Collections.Generic;
using Avalonia.Media;
using PhoenixVisualizer.Audio;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class VideoDelayEffectsNode : BaseEffectNode
    {
        private Queue<ImageBuffer> _frameHistory = new Queue<ImageBuffer>();

        protected override void InitializePorts()
        {
            AddInputPort("DelayFrames", typeof(int));
        }

        protected override object ProcessCore(Dictionary<string, object> parameters, AudioFeatures features)
        {
            int delay = (int)parameters["DelayFrames"];
            // _frameHistory.Enqueue(InputBuffer.Clone());

            if (_frameHistory.Count > delay)
            {
                var oldFrame = _frameHistory.Dequeue();
                // OutputBuffer.Blit(oldFrame);
                return oldFrame;
            }
            else
            {
                // OutputBuffer.Blit(InputBuffer);
                // TODO: Get actual input buffer
                return new ImageBuffer(800, 600);
            }
        }
    }
}

