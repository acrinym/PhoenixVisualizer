using System.Drawing;

namespace PhoenixVisualizer.Core.VFX
{
    public class VFXRenderContext
    {
        public float FrameTime { get; set; }
        public int FrameNumber { get; set; }
        public Canvas Canvas { get; set; } = new();
    }

    public enum OscilloscopeChannel { Left, Right, Stereo }
    public enum OscilloscopePosition { Top, Center, Bottom }
    public enum AudioSourceType { Waveform, Spectrum, Beat }

    public class Canvas { }
    public class Typeface { }
}
