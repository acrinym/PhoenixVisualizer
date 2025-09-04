namespace PhoenixVisualizer.Core.Nodes
{
    // Provides global multipliers for nodes to read (from RenderSurface UI)
    public static class NodeParamBridge
    {
        public static float Sensitivity { get; set; } = 1f;  // maps to analyzer gain, waveform gain, etc.
        public static float Smoothing { get; set; } = 0.35f; // maps to EMA / decay
    }
}
