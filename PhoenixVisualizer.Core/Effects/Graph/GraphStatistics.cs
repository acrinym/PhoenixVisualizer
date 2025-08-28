namespace PhoenixVisualizer.Core.Effects.Graph
{
    /// <summary>
    /// Statistics and information about the effects graph
    /// </summary>
    public class GraphStatistics
    {
        public int NodeCount { get; set; }
        public int ConnectionCount { get; set; }
        public int Categories { get; set; }
        public bool IsValid { get; set; }
        public DateTime LastProcessed { get; set; }
        public TimeSpan ProcessingTime { get; set; }

        public override string ToString()
        {
            return $"Graph: {NodeCount} nodes, {ConnectionCount} connections, {Categories} categories, Valid: {IsValid}, Last: {LastProcessed:HH:mm:ss}, Time: {ProcessingTime.TotalMilliseconds:F2}ms";
        }
    }
}