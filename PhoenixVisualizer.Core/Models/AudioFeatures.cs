using System;

namespace PhoenixVisualizer.Core.Models
{
    public class AudioFeatures
    {
        public bool IsBeat { get; set; }
        public bool Beat { get; set; } // Alias for IsBeat for compatibility
        public double BPM { get; set; }
        public double Timestamp { get; set; }
        public float[] SpectrumData { get; set; }
        public float[] WaveformData { get; set; }
        public float LeftChannel { get; set; }
        public float RightChannel { get; set; }
        public float CenterChannel { get; set; }
        public float Rms { get; set; } // Root Mean Square for volume detection

        public AudioFeatures()
        {
            SpectrumData = new float[512];
            WaveformData = new float[512];
        }
    }
}
