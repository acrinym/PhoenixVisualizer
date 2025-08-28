using System.Drawing;

namespace PhoenixVisualizer.Core.VFX;

public class VFXRenderContext
{
    public object? Target { get; set; }
    public object? Audio { get; set; }
    public double DeltaTime { get; set; }
    public long FrameCount { get; set; }
    public bool SupportsGPU { get; set; } = false;
}
