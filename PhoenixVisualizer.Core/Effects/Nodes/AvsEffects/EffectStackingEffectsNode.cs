using Avalonia.Media;
using PhoenixVisualizer.Core.Audio;
using PhoenixVisualizer.Core.Utils;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class EffectStackingEffectsNode : BaseEffectNode
    {
        protected override void InitializePorts()
        {
            // For now, just leave ports empty since AddInput/AddOutput don't exist
            // This will be fixed when the proper base class methods are implemented
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
        {
            return ProcessHelpers.Stack(inputs, audio);
        }
    }
}