namespace PhoenixVisualizer.Core.Services
{
    /// <summary>
    /// Interface for providing audio data to AVS effects
    /// </summary>
    public interface IAvsAudioProvider : IDisposable
    {
        /// <summary>
        /// Gets the current audio data (waveform, spectrum, etc.)
        /// </summary>
        Task<Dictionary<string, object>> GetAudioDataAsync();
        
        /// <summary>
        /// Gets spectrum data for the specified number of channels
        /// </summary>
        Task<Dictionary<string, object>> GetSpectrumDataAsync(int channels = 2);
        
        /// <summary>
        /// Gets waveform data for the specified number of channels
        /// </summary>
        Task<Dictionary<string, object>> GetWaveformDataAsync(int channels = 2);
        
        /// <summary>
        /// Checks if a beat was detected in the current audio frame
        /// </summary>
        Task<bool> IsBeatDetectedAsync();
        
        /// <summary>
        /// Gets the current BPM (beats per minute)
        /// </summary>
        Task<float> GetBPMAsync();
        
        /// <summary>
        /// Gets the current audio level (0.0 to 1.0)
        /// </summary>
        Task<float> GetAudioLevelAsync();
        
        /// <summary>
        /// Gets the current audio level for a specific channel
        /// </summary>
        Task<float> GetChannelLevelAsync(int channel);
        
        /// <summary>
        /// Gets the current frequency data for FFT analysis
        /// </summary>
        Task<Dictionary<string, object>> GetFrequencyDataAsync();
        
        /// <summary>
        /// Starts audio capture/analysis
        /// </summary>
        Task StartAsync();
        
        /// <summary>
        /// Stops audio capture/analysis
        /// </summary>
        Task StopAsync();
        
        /// <summary>
        /// Checks if audio capture is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Gets the current sample rate
        /// </summary>
        int SampleRate { get; }
        
        /// <summary>
        /// Gets the current number of channels
        /// </summary>
        int Channels { get; }
        
        /// <summary>
        /// Gets the current buffer size
        /// </summary>
        int BufferSize { get; }
    }
}
