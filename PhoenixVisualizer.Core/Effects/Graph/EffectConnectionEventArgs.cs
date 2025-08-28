using PhoenixVisualizer.Core.Effects.Models;

namespace PhoenixVisualizer.Core.Effects.Graph
{
    /// <summary>
    /// Event arguments for connection-related events in the effects graph
    /// </summary>
    public class EffectConnectionEventArgs : EventArgs
    {
        public EffectConnection Connection { get; }

        public EffectConnectionEventArgs(EffectConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}