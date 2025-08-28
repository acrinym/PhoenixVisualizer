using System;

namespace PhoenixVisualizer.Core.Effects.Models
{
    /// <summary>
    /// Represents a connection between two effect nodes in the effects graph
    /// </summary>
    public class EffectConnection
    {
        public string Id { get; set; } = string.Empty;
        public string SourceNodeId { get; set; } = string.Empty;
        public string SourcePortName { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
        public string TargetPortName { get; set; } = string.Empty;
        public Type DataType { get; set; } = typeof(object);
        public bool IsEnabled { get; set; } = true;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }

        public EffectConnection()
        {
        }

        public EffectConnection(string sourceNodeId, string sourcePortName, string targetNodeId, string targetPortName)
        {
            SourceNodeId = sourceNodeId ?? throw new ArgumentNullException(nameof(sourceNodeId));
            SourcePortName = sourcePortName ?? throw new ArgumentNullException(nameof(sourcePortName));
            TargetNodeId = targetNodeId ?? throw new ArgumentNullException(nameof(targetNodeId));
            TargetPortName = targetPortName ?? throw new ArgumentNullException(nameof(targetPortName));
        }

        public override string ToString()
        {
            return $"{SourceNodeId}.{SourcePortName} -> {TargetNodeId}.{TargetPortName}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is EffectConnection other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
