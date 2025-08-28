namespace PhoenixVisualizer.Core.Effects.Graph
{
    /// <summary>
    /// Event arguments for graph processing events
    /// </summary>
    public class EffectProcessingEventArgs : EventArgs
    {
        public EffectsGraph Graph { get; }

        public EffectProcessingEventArgs(EffectsGraph graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }
    }
}