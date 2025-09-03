using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Transpile
{
    public class UnifiedEffectNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string TypeKey { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class UnifiedGraph
    {
        public List<UnifiedEffectNode> Nodes { get; set; } = new();
    }
}
