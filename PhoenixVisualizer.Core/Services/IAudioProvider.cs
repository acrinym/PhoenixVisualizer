namespace PhoenixVisualizer.Core.Services
{
    /// <summary>
    /// Common interface for audio providers that RenderSurface can use
    /// </summary>
    public interface IAudioProvider : IDisposable
    {
        /// <summary>
        /// Gets the current FFT spectrum data
        /// </summary>
        float[] GetSpectrumData();
        
        /// <summary>
        /// Gets the current waveform data
        /// </summary>
        float[] GetWaveformData();
        
        /// <summary>
        /// Gets the current playback position in seconds
        /// </summary>
        double GetPositionSeconds();
        
        /// <summary>
        /// Gets the total audio length in seconds
        /// </summary>
        double GetLengthSeconds();
        
        /// <summary>
        /// Gets the current playback status
        /// </summary>
        string GetStatus();
        
        /// <summary>
        /// Checks if the audio service is ready to play
        /// </summary>
        bool IsReadyToPlay { get; }
        
        /// <summary>
        /// Opens an audio file
        /// </summary>
        bool Open(string path);
        
        /// <summary>
        /// Starts playback
        /// </summary>
        bool Play();
        
        /// <summary>
        /// Pauses playback
        /// </summary>
        void Pause();
        
        /// <summary>
        /// Stops playback
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Initializes the audio service
        /// </summary>
        bool Initialize();
    }
}
