namespace PhoenixVisualizer.Core.VFX;

public class VFXPerformanceMetrics
{
    public double CpuMs { get; set; }
    public double GpuMs { get; set; }
    public int NodesRendered { get; set; }
    
    public void Reset()
    {
        CpuMs = 0;
        GpuMs = 0;
        NodesRendered = 0;
    }
    
    public void UpdateFrameTime(double milliseconds)
    {
        CpuMs = milliseconds;
    }
    
    public void RecordError(Exception ex)
    {
        // Log error for performance tracking
    }
}
