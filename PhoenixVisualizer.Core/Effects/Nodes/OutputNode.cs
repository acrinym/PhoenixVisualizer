using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes
{
    public class OutputNode : BaseEffectNode
    {
        public OutputNode()
        {
            Name = "Output";
            Description = "Output node for the effect graph";
            Category = "System";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(EffectOutput), true, null, "Output data from the effect graph"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (inputs.TryGetValue("Input", out var input))
                return input;
            return new EffectOutput();
        }

        public override object GetDefaultOutput()
        {
            return new EffectOutput();
        }
    }
}
