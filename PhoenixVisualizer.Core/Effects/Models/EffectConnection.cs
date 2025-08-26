using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Models
{
    public class EffectConnection
    {
        public required string SourceNodeId { get; set; }
        public required string SourcePort { get; set; }
        public required string TargetNodeId { get; set; }
        public required string TargetPort { get; set; }
        public EffectPort Source { get; set; } = default!;
        public EffectPort Target { get; set; } = default!;

        public EffectConnection()
        {
            // Initialize with default values to avoid nullable warnings
            SourceNodeId = string.Empty;
            SourcePort = string.Empty;
            TargetNodeId = string.Empty;
            TargetPort = string.Empty;
            Source = new EffectPort("source", typeof(object), false, null, "Source port");
            Target = new EffectPort("target", typeof(object), false, null, "Target port");
        }

        public EffectConnection(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
        {
            SourceNodeId = sourceNodeId ?? throw new ArgumentNullException(nameof(sourceNodeId));
            SourcePort = sourcePort ?? throw new ArgumentNullException(nameof(sourcePort));
            TargetNodeId = targetNodeId ?? throw new ArgumentNullException(nameof(targetNodeId));
            TargetPort = targetPort ?? throw new ArgumentNullException(nameof(targetPort));
            
            // Initialize Source and Target ports
            Source = new EffectPort(sourcePort, typeof(object), false, null, $"Source port {sourcePort}");
            Target = new EffectPort(targetPort, typeof(object), false, null, $"Target port {targetPort}");
        }
    }
}
