using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Models
{
    public class EffectConnection
    {
        public string SourceNodeId { get; set; }
        public string SourcePort { get; set; }
        public string TargetNodeId { get; set; }
        public string TargetPort { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public EffectConnection()
        {
            Parameters = new Dictionary<string, object>();
        }

        public EffectConnection(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
        {
            SourceNodeId = sourceNodeId;
            SourcePort = sourcePort;
            TargetNodeId = targetNodeId;
            TargetPort = targetPort;
            Parameters = new Dictionary<string, object>();
        }
    }
}
