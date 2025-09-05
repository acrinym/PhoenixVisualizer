using System;

namespace PhoenixVisualizer.NativeAudio
{
    /// <summary>
    /// Clean native audio service interface - no VLC dependencies
    /// </summary>
    public interface INativeAudioService : IDisposable
    {
        /// <summary>
        /// Initialize the audio service
        /// </summary>
        bool Initialize();

        /// <summary>
        /// Open an audio file for playback
        /// </summary>
        bool Open(string filePath);

        /// <summary>
        /// Start playback
        /// </summary>
        bool Play();

        /// <summary>
        /// Pause playback
        /// </summary>
        void Pause();

        /// <summary>
        /// Stop playback
        /// </summary>
        void Stop();

        /// <summary>
        /// Check if audio is currently playing
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Check if service is ready to play
        /// </summary>
        bool IsReadyToPlay { get; }

        /// <summary>
        /// Get current playback position in seconds
        /// </summary>
        double GetPositionSeconds();

        /// <summary>
        /// Get total audio length in seconds
        /// </summary>
        double GetLengthSeconds();

        /// <summary>
        /// Get current status string
        /// </summary>
        string GetStatus();

        /// <summary>
        /// Get current spectrum data for visualization
        /// </summary>
        float[] GetSpectrumData();

        /// <summary>
        /// Get current waveform data for visualization
        /// </summary>
        float[] GetWaveformData();

        /// <summary>
        /// Get current audio features (RMS, Peak, etc.)
        /// </summary>
        AudioFeatures GetAudioFeatures();

        /// <summary>
        /// Set volume level (0.0 to 1.0)
        /// </summary>
        void SetVolume(float volume);

        /// <summary>
        /// Get current volume level
        /// </summary>
        float GetVolume();
    }

    /// <summary>
    /// Audio features for visualization
    /// </summary>
    public class AudioFeatures
    {
        public float[] Spectrum { get; set; } = Array.Empty<float>();
        public float[] Waveform { get; set; } = Array.Empty<float>();
        public float RMS { get; set; }
        public float Peak { get; set; }
        public float Volume { get; set; } = 1.0f;
        public float BPM { get; set; }
        public float[] FrequencyBands { get; set; } = Array.Empty<float>();
        public bool IsPlaying { get; set; }
        public double PositionSeconds { get; set; }
        public double LengthSeconds { get; set; }
    }
}
