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

        // NEW: Additional properties that effects need
        public float Bass { get; init; } = 0.0f;
        public float Mid { get; init; } = 0.0f;
        public float Treble { get; init; } = 0.0f;
        public float Peak { get; init; } = 0.0f;
        public float Time { get; init; } = 0.0f;
        public float[] LeftChannelFFT { get; init; } = Array.Empty<float>();
        public float[] RightChannelFFT { get; init; } = Array.Empty<float>();
        public float[] FFTData { get; init; } = Array.Empty<float>();
        public TimeSpan Position { get; init; } = TimeSpan.Zero;
        public TimeSpan Duration { get; init; } = TimeSpan.Zero;
        public bool IsPlaying { get; init; } = false;
        public float Volume { get; init; } = 1.0f;
        public float PlaybackRate { get; init; } = 1.0f;

        public AudioFeatures()
        {
            // Initialize with default values
            SpectrumData = new float[512];
            WaveformData = new float[512];
            Fft = new float[512];
            Waveform = new float[512];
            Spectrum = new float[512];
            LeftChannelFFT = new float[512];
            RightChannelFFT = new float[512];
            FFTData = new float[512];
        }
    }
}
