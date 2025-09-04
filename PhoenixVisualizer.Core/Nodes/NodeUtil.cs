namespace PhoenixVisualizer.Core.Nodes
{
    public static class NodeUtil
    {
        public static float ApplySensitivity(float v) => v * NodeParamBridge.Sensitivity;
        public static float ApplySmoothing(float baseSmoothing) => 1f - (1f - baseSmoothing) * (1f + (NodeParamBridge.Smoothing - 0.35f));
    }
}
