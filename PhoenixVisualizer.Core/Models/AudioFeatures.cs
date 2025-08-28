using System;

namespace PhoenixVisualizer.Core.Models
{
    public class AudioFeatures
    {
        public bool IsBeat { get; init; } = false;
        public bool Beat { get; init; } = false; // Alias for IsBeat for compatibility
        public double BPM { get; init; } = 0.0;
        public double Timestamp { get; init; } = 0.0;
        
        // Legacy properties for backward compatibility
        public float[] SpectrumData { get; init; } = Array.Empty<float>();
        public float[] WaveformData { get; init; } = Array.Empty<float>();
        
        // New properties that visualizers expect
        public float[] Fft { get; init; } = Array.Empty<float>();
        public float[] Waveform { get; init; } = Array.Empty<float>();
        
        // Additional properties for effect nodes
        public float[] Spectrum { get; init; } = Array.Empty<float>();
        public float BeatStrength { get; init; } = 0.0f;
        public float RMS { get; init; } = 0.0f;
        
        public float LeftChannel { get; init; } = 0.0f;
        public float RightChannel { get; init; } = 0.0f;
        public float CenterChannel { get; init; } = 0.0f;
        public float Rms { get; init; } = 0.0f; // Root Mean Square for volume detection

        public AudioFeatures()
        {
            // Initialize with default values
            SpectrumData = new float[512];
            WaveformData = new float[512];
            Fft = new float[512];
            Waveform = new float[512];
            Spectrum = new float[512];
        }
    }
}
