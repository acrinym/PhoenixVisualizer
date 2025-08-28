using System;

namespace PhoenixVisualizer.Core.Models
{
    /// <summary>
    /// Provides audio features and data for VFX effects
    /// </summary>
    public class AudioFeatures
    {
        /// <summary>
        /// Current audio data for left channel
        /// </summary>
        public float[] LeftChannel { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// Current audio data for right channel
        /// </summary>
        public float[] RightChannel { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// Current audio data for center channel (mono)
        /// </summary>
        public float[] CenterChannel { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// Sample rate of the audio
        /// </summary>
        public int SampleRate { get; set; } = 44100;
        
        /// <summary>
        /// Number of samples in the current buffer
        /// </summary>
        public int SampleCount => LeftChannel.Length;
        
        /// <summary>
        /// Duration of the current buffer in seconds
        /// </summary>
        public float Duration => (float)SampleCount / SampleRate;
        
        /// <summary>
        /// Current beat detection state
        /// </summary>
        public bool Beat { get; set; }
        
        /// <summary>
        /// Beat intensity (0.0 to 1.0)
        /// </summary>
        public float BeatIntensity { get; set; }
        
        /// <summary>
        /// Time since last beat in seconds
        /// </summary>
        public float TimeSinceBeat { get; set; }
        
        /// <summary>
        /// Current BPM (beats per minute)
        /// </summary>
        public float BPM { get; set; } = 120.0f;
        
        /// <summary>
        /// Bass frequency energy (0.0 to 1.0)
        /// </summary>
        public float Bass { get; set; }
        
        /// <summary>
        /// Mid frequency energy (0.0 to 1.0)
        /// </summary>
        public float Mid { get; set; }
        
        /// <summary>
        /// Treble frequency energy (0.0 to 1.0)
        /// </summary>
        public float Treble { get; set; }
        
        /// <summary>
        /// RMS (Root Mean Square) of the audio
        /// </summary>
        public float RMS { get; set; }
        
        /// <summary>
        /// Peak amplitude of the audio
        /// </summary>
        public float Peak { get; set; }
        
        /// <summary>
        /// Spectral centroid (brightness)
        /// </summary>
        public float SpectralCentroid { get; set; }
        
        /// <summary>
        /// Spectral rolloff (high frequency content)
        /// </summary>
        public float SpectralRolloff { get; set; }
        
        /// <summary>
        /// Spectral flux (change in spectrum)
        /// </summary>
        public float SpectralFlux { get; set; }
        
        /// <summary>
        /// Zero crossing rate (noisiness)
        /// </summary>
        public float ZeroCrossingRate { get; set; }
        
        /// <summary>
        /// Whether audio is currently playing
        /// </summary>
        public bool IsPlaying { get; set; }
        
        /// <summary>
        /// Current playback position in seconds
        /// </summary>
        public float PlaybackPosition { get; set; }
        
        /// <summary>
        /// Total duration of the audio in seconds
        /// </summary>
        public float TotalDuration { get; set; }
        
        /// <summary>
        /// Volume level (0.0 to 1.0)
        /// </summary>
        public float Volume { get; set; } = 1.0f;
        
        /// <summary>
        /// Whether the audio is muted
        /// </summary>
        public bool IsMuted { get; set; }
        
        // Legacy compatibility properties
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public bool IsBeat => Beat;
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float BeatStrength => BeatIntensity;
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float Rms => RMS;
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float Time => PlaybackPosition;
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public double Timestamp => PlaybackPosition;
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float[] SpectrumData => GetFrequencyData();
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float[] WaveformData => LeftChannel;
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float[] Waveform => LeftChannel;
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float[] Spectrum => GetFrequencyData();
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float[] Fft => GetFrequencyData();
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float[] FFTData => GetFrequencyData();
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float[] LeftChannelFFT => GetFrequencyData();
        
        /// <summary>
        /// Legacy property for backward compatibility
        /// </summary>
        public float[] RightChannelFFT => GetFrequencyData();
        
        /// <summary>
        /// Get a sample from the left channel at the specified index
        /// </summary>
        public float GetLeftSample(int index)
        {
            if (index >= 0 && index < LeftChannel.Length)
                return LeftChannel[index];
            return 0.0f;
        }
        
        /// <summary>
        /// Get a sample from the right channel at the specified index
        /// </summary>
        public float GetRightSample(int index)
        {
            if (index >= 0 && index < RightChannel.Length)
                return RightChannel[index];
            return 0.0f;
        }
        
        /// <summary>
        /// Get a sample from the center channel at the specified index
        /// </summary>
        public float GetCenterSample(int index)
        {
            if (index >= 0 && index < CenterChannel.Length)
                return CenterChannel[index];
            return 0.0f;
        }
        
        /// <summary>
        /// Get a sample from the specified channel at the specified index
        /// </summary>
        public float GetSample(int index, AudioChannel channel)
        {
            return channel switch
            {
                AudioChannel.Left => GetLeftSample(index),
                AudioChannel.Right => GetRightSample(index),
                AudioChannel.Center => GetCenterSample(index),
                AudioChannel.Stereo => (GetLeftSample(index) + GetRightSample(index)) * 0.5f,
                _ => 0.0f
            };
        }
        
        /// <summary>
        /// Get the average amplitude across all channels
        /// </summary>
        public float GetAverageAmplitude()
        {
            if (SampleCount == 0) return 0.0f;
            
            float sum = 0.0f;
            for (int i = 0; i < SampleCount; i++)
            {
                sum += Math.Abs(GetLeftSample(i)) + Math.Abs(GetRightSample(i));
            }
            return sum / (SampleCount * 2);
        }
        
        /// <summary>
        /// Get the frequency domain data for FFT analysis
        /// </summary>
        public float[] GetFrequencyData()
        {
            // This would typically use FFT to convert time domain to frequency domain
            // For now, return a simple approximation
            var frequencies = new float[256];
            for (int i = 0; i < frequencies.Length; i++)
            {
                frequencies[i] = Bass * (1.0f - (float)i / frequencies.Length) + 
                                Mid * (0.5f - Math.Abs((float)i / frequencies.Length - 0.5f)) + 
                                Treble * ((float)i / frequencies.Length);
            }
            return frequencies;
        }
    }
    
    /// <summary>
    /// Audio channels for VFX effects
    /// </summary>
    public enum AudioChannel
    {
        Left,
        Right,
        Center,
        Stereo
    }
}
