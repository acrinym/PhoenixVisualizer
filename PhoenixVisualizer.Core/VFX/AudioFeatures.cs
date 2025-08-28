using System;

namespace PhoenixVisualizer.Core.VFX
{
    public class AudioFeatures
    {
        public float Bass { get; set; }
        public float Mid { get; set; }
        public float Treble { get; set; }
        public float Peak { get; set; }
        public float RMS { get; set; }
        public float Time { get; set; }
        public bool Beat { get; set; }
        public float[] Waveform { get; set; } = Array.Empty<float>();
        public float[] Fft { get; set; } = Array.Empty<float>();
        public float[] LeftChannelFFT { get; set; } = Array.Empty<float>();
        public float[] RightChannelFFT { get; set; } = Array.Empty<float>();
        public float[] FFTData { get; set; } = Array.Empty<float>();
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsPlaying { get; set; }
        public float Volume { get; set; }
        public float PlaybackRate { get; set; }
    }
}
