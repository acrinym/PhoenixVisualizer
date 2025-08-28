using System.Drawing;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.VFX
{
    public class VFXRenderContext
    {
        public float FrameTime { get; set; }
        public int FrameNumber { get; set; }
        public double DeltaTime { get; set; }
        public bool SupportsGPU { get; set; }
        public ImageBuffer? Canvas { get; set; }
    }

    public enum OscilloscopeChannel { Left, Right, Stereo }
    public enum OscilloscopePosition { Top, Center, Bottom }
    public enum AudioSourceType { Waveform, Spectrum, Beat }

}
