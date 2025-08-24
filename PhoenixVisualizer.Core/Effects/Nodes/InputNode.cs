using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes
{
    public class InputNode : BaseEffectNode
    {
        public InputNode()
        {
            Name = "Input";
            Description = "Input node for the effect graph";
            Category = "System";
        }

        protected override void InitializePorts()
        {
            _outputPorts.Add(new EffectPort("Output", typeof(EffectInput), false, null, "Input data for the effect graph"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            return null;
        }

        public override object GetDefaultOutput()
        {
            return new EffectInput();
        }
    }
}
