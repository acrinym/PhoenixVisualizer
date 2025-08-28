using PhoenixVisualizer.Core.Effects.Interfaces;

namespace PhoenixVisualizer.Core.Effects.Graph
{
    /// <summary>
    /// Event arguments for node-related events in the effects graph
    /// </summary>
    public class EffectNodeEventArgs : EventArgs
    {
        public IEffectNode Node { get; }

        public EffectNodeEventArgs(IEffectNode node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }
    }
}