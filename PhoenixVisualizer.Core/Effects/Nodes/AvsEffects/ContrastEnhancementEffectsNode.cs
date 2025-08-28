using Avalonia.Media;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Utils;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ContrastEnhancementEffectsNode : BaseEffectNode
    {
        protected override void InitializePorts()
        {
            // For now, just leave ports empty since AddInput/AddOutput don't exist
            // This will be fixed when the proper base class methods are implemented
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
        {
            return ProcessHelpers.AdjustContrast(inputs, audio);
        }
    }
}