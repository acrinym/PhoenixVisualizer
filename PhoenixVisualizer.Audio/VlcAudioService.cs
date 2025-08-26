using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PhoenixVisualizer.Audio.Interfaces;
using PhoenixVisualizer.Core.Services;
using LibVLCSharp.Shared;
using System.IO;

namespace PhoenixVisualizer.Audio;

public class VlcAudioService : IAudioService, IAudioProvider, IDisposable
{
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private Media? _currentMedia;
    private string _currentFile = string.Empty;
    private bool _isPlaying = false;
    private bool _isDisposed = false;
    
    // Audio data buffers for visualizers - now populated with real VLC data
    private readonly float[] _spectrumData = new float[2048];
    private readonly float[] _waveformData = new float[2048];
    private readonly object _audioLock = new object();
    
    // VLC audio callback data
    private readonly float[] _rawAudioBuffer = new float[8192]; // Raw audio samples
    private int _audioBufferIndex = 0;
    private readonly object _rawAudioLock = new object();

    // VLC audio callback delegates
    private LibVLC.AudioPlayCb? _audioPlayCb;
    private LibVLC.AudioPauseCb? _audioPauseCb;
    private LibVLC.AudioResumeCb? _audioResumeCb;
    private LibVLC.AudioFlushCb? _audioFlushCb;
    private LibVLC.AudioDrainCb? _audioDrainCb;
    private LibVLC.AudioSetVolumeCb? _audioSetVolumeCb;
    
    // P/Invoke declarations for VLC audio callbacks
    [DllImport("libvlc")]
    private static extern int libvlc_audio_set_callbacks(IntPtr mediaPlayer, 
        AudioPlayCb play, AudioPauseCb pause, AudioResumeCb resume, 
        AudioFlushCb flush, AudioDrainCb drain);
    
    [DllImport("libvlc")]
    private static extern int libvlc_audio_set_format(IntPtr mediaPlayer, 
        [MarshalAs(UnmanagedType.LPStr)] string format, uint rate, uint channels);
    
    // VLC audio callback delegate types
    private delegate void AudioPlayCb(IntPtr data, IntPtr samples, uint count, long pts);
    private delegate void AudioPauseCb(IntPtr data, long pts);
    private delegate void AudioResumeCb(IntPtr data, long pts);
    private delegate void AudioFlushCb(IntPtr data, long pts);
    private delegate void AudioDrainCb(IntPtr data);

    public bool IsPlaying => _isPlaying;

    public VlcAudioService()
    {
        try
        {
            Debug.WriteLine("[VlcAudioService] Starting initialization...");
            
            // Create LibVLC instance with debug logging
            _libVlc = new LibVLC(enableDebugLogs: true);
            Debug.WriteLine("[VlcAudioService] LibVLC instance created successfully");
            
            // Create MediaPlayer
            _mediaPlayer = new MediaPlayer(_libVLC);
            Debug.WriteLine("[VlcAudioService] MediaPlayer instance created successfully");
            
            // Set up event handlers
            _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            
            // Set up VLC audio callbacks for real-time audio data
            SetupVlcAudioCallbacks();
            
            Debug.WriteLine("[VlcAudioService] Initialized successfully with LibVLC (debug enabled)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to initialize: {ex.Message}");
            Debug.WriteLine($"[VlcAudioService] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private void MediaPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        Debug.WriteLine($"[VlcAudioService] Time: {e.Time}ms");
    }

    private void MediaPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        Debug.WriteLine($"[VlcAudioService] Length: {e.Length}ms");
    }

    private void MediaPlayer_Playing(object? sender, EventArgs e)
    {
        _isPlaying = true;
        Debug.WriteLine("[VlcAudioService] Playback started");
    }

    private void MediaPlayer_Paused(object? sender, EventArgs e)
    {
        _isPlaying = false;
        Debug.WriteLine("[VlcAudioService] Playback paused");
    }

    private void MediaPlayer_Stopped(object? sender, EventArgs e)
    {
        _isPlaying = false;
        Debug.WriteLine("[VlcAudioService] Playback stopped");
    }

    private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
    {
        Debug.WriteLine("[VlcAudioService] MediaPlayer encountered an error");
    }

    private void SetupVlcAudioCallbacks()
    {
        try
        {
            // Set up VLC audio callbacks to capture real audio data
            // We need to use P/Invoke since LibVLCSharp doesn't expose these directly
            
            // Create callback delegates
            _audioPlayCb = new AudioPlayCb(OnVlcAudioPlay);
            _audioPauseCb = new AudioPauseCb(OnVlcAudioPause);
            _audioResumeCb = new AudioResumeCb(OnVlcAudioResume);
            _audioFlushCb = new AudioFlushCb(OnVlcAudioFlush);
            _audioDrainCb = new AudioDrainCb(OnVlcAudioDrain);
            
            Debug.WriteLine("[VlcAudioService] VLC audio callbacks configured");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to setup audio callbacks: {ex.Message}");
        }
    }

    // VLC Audio Callback Implementation
    // These methods will be called by VLC when audio data is available
    private void OnVlcAudioData(IntPtr samples, uint count, long pts)
    {
        try
        {
            lock (_rawAudioLock)
            {
                // Convert VLC audio data to our format
                // samples points to interleaved float samples (stereo)
                var sampleCount = (int)count;
                var floatSamples = new float[sampleCount];
                
                // Copy audio data from VLC buffer
                Marshal.Copy(samples, floatSamples, 0, sampleCount);
                
                // Process audio data for visualizers
                ProcessAudioData(floatSamples, sampleCount);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Error processing VLC audio data: {ex.Message}");
        }
    }
    
    // VLC Audio Callback Methods
    private void OnVlcAudioPlay(IntPtr data, IntPtr samples, uint count, long pts)
    {
        // This is the main callback for audio data
        OnVlcAudioData(samples, count, pts);
    }
    
    private void OnVlcAudioPause(IntPtr data, long pts)
    {
        Debug.WriteLine("[VlcAudioService] VLC audio paused");
    }
    
    private void OnVlcAudioResume(IntPtr data, long pts)
    {
        Debug.WriteLine("[VlcAudioService] VLC audio resumed");
    }
    
    private void OnVlcAudioFlush(IntPtr data, long pts)
    {
        Debug.WriteLine("[VlcAudioService] VLC audio flushed");
        // Clear audio buffers when flushing
        lock (_rawAudioLock)
        {
            Array.Clear(_spectrumData, 0, _spectrumData.Length);
            Array.Clear(_waveformData, 0, _waveformData.Length);
        }
    }
    
    private void OnVlcAudioDrain(IntPtr data)
    {
        Debug.WriteLine("[VlcAudioService] VLC audio drained");
    }

    private void ProcessAudioData(float[] samples, int count)
    {
        try
        {
            // Update waveform data (time domain)
            var waveformSize = Math.Min(count, _waveformData.Length);
            Array.Copy(samples, 0, _waveformData, 0, waveformSize);
            
            // Calculate FFT for spectrum data (frequency domain)
            var fftSize = Math.Min(count, _spectrumData.Length);
            CalculateFFT(samples, count, _spectrumData);
            
            // Update buffer index for circular buffer
            _audioBufferIndex = (_audioBufferIndex + count) % _rawAudioBuffer.Length;
            
            Debug.WriteLine($"[VlcAudioService] Processed {count} audio samples, FFT size: {fftSize}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Error in ProcessAudioData: {ex.Message}");
        }
    }

    private void CalculateFFT(float[] samples, int sampleCount, float[] spectrumOutput)
    {
        try
        {
            // Simple FFT calculation for real-time visualization
            // In production, you might want to use a more optimized FFT library
            
            var fftSize = spectrumOutput.Length;
            var windowedSamples = new float[fftSize];
            
            // Apply window function and zero-pad if necessary
            for (int i = 0; i < fftSize; i++)
            {
                if (i < sampleCount)
                {
                    // Apply Hann window
                    float window = 0.5f * (1.0f - (float)Math.Cos(2.0 * Math.PI * i / (fftSize - 1)));
                    windowedSamples[i] = samples[i] * window;
                }
                else
                {
                    windowedSamples[i] = 0.0f;
                }
            }
            
            // Simple magnitude calculation (simplified FFT)
            // This is a placeholder - in production use a proper FFT library
            for (int i = 0; i < fftSize; i++)
            {
                float magnitude = 0.0f;
                for (int j = 0; j < fftSize; j++)
                {
                    float phase = 2.0f * (float)Math.PI * i * j / fftSize;
                    magnitude += windowedSamples[j] * (float)Math.Cos(phase);
                }
                spectrumOutput[i] = Math.Abs(magnitude) / fftSize;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Error calculating FFT: {ex.Message}");
            // Fallback to simple data
            for (int i = 0; i < spectrumOutput.Length; i++)
            {
                spectrumOutput[i] = 0.0f;
            }
        }
    }

    public void Play(string path)
    {
        if (_isDisposed || _mediaPlayer == null) return;
        
        try
        {
            Debug.WriteLine($"[VlcAudioService] Attempting to play: {path}");
            
            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Stop();

            _currentMedia?.Dispose();
            
            // Create media from file path
            _currentMedia = new Media(_libVLC!, new Uri(Path.GetFullPath(path)));
            _mediaPlayer.Media = _currentMedia;
            _currentFile = path;
            
            // Start playback
            _mediaPlayer.Play();
            Debug.WriteLine($"[VlcAudioService] Play() called successfully for: {path}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to play {path}: {ex.Message}");
            Debug.WriteLine($"[VlcAudioService] Stack trace: {ex.StackTrace}");
        }
    }

    public void Pause()
    {
        if (_isDisposed || _mediaPlayer == null) return;
        
        try
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                Debug.WriteLine("[VlcAudioService] Paused");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to pause: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_isDisposed || _mediaPlayer == null) return;
        
        try
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
                Debug.WriteLine("[VlcAudioService] Stopped");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to stop: {ex.Message}");
        }
    }

    public float[] GetWaveformData()
    {
        lock (_audioLock)
        {
            if (_isPlaying && _mediaPlayer != null)
            {
                // Return real waveform data from VLC audio callbacks
                var realData = new float[_waveformData.Length];
                Array.Copy(_waveformData, realData, _waveformData.Length);
                return realData;
            }
            // Return last known data when not playing
            var lastData = new float[_waveformData.Length];
            Array.Copy(_waveformData, lastData, _waveformData.Length);
            return lastData;
        }
    }

    public float[] GetSpectrumData()
    {
        lock (_audioLock)
        {
            if (_isPlaying && _mediaPlayer != null)
            {
                // Return real FFT data from VLC audio callbacks
                var realData = new float[_spectrumData.Length];
                Array.Copy(_spectrumData, realData, _spectrumData.Length);
                return realData;
            }
            // Return last known data when not playing
            var lastData = new float[_spectrumData.Length];
            Array.Copy(_spectrumData, lastData, _spectrumData.Length);
            return lastData;
        }
    }

    public void SetRate(float rate)
    {
        if (_isDisposed || _mediaPlayer == null) return;
        
        try
        {
            _mediaPlayer.SetRate(rate);
            Debug.WriteLine($"[VlcAudioService] Rate set to: {rate}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to set rate: {ex.Message}");
        }
    }

    public void SetTempo(float tempo)
    {
        if (_isDisposed || _mediaPlayer == null) return;
        
        try
        {
            // VLC doesn't have direct tempo control, but we can approximate with rate
            // tempo is typically 0-200%, so convert to rate
            float rate = 1.0f + (tempo - 100.0f) / 100.0f;
            _mediaPlayer.SetRate(rate);
            Debug.WriteLine($"[VlcAudioService] Tempo set to: {tempo}% (rate: {rate})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to set tempo: {ex.Message}");
        }
    }

    // IAudioProvider interface implementation
    public double GetPositionSeconds()
    {
        if (_isDisposed || _mediaPlayer == null) return 0.0;
        return _mediaPlayer.Time / 1000.0; // Convert ms to seconds
    }

    public double GetLengthSeconds()
    {
        if (_isDisposed || _mediaPlayer == null) return 0.0;
        return _mediaPlayer.Length / 1000.0; // Convert ms to seconds
    }

    public string GetStatus()
    {
        if (_isDisposed) return "Disposed";
        if (_mediaPlayer == null) return "Not Initialized";
        if (string.IsNullOrEmpty(_currentFile)) return "No File Loaded";
        if (_isPlaying) return "Playing";
        return "Stopped";
    }

    public bool IsReadyToPlay => _isDisposed == false && _mediaPlayer != null && !string.IsNullOrEmpty(_currentFile);

    public bool Open(string path)
    {
        if (_isDisposed) return false;
        
        try
        {
            if (_mediaPlayer?.IsPlaying == true)
                _mediaPlayer.Stop();

            _currentMedia?.Dispose();
            _currentMedia = new Media(_libVLC!, new Uri(Path.GetFullPath(path)));
            _mediaPlayer!.Media = _currentMedia;
            _currentFile = path;
            
            // Set up VLC audio callbacks for this media
            SetupVlcAudioCallbacksForMedia();
            
            Debug.WriteLine($"[VlcAudioService] Opened file: {path}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to open {path}: {ex.Message}");
            return false;
        }
    }
    
    private void SetupVlcAudioCallbacksForMedia()
    {
        try
        {
            if (_mediaPlayer == null || _audioPlayCb == null) return;
            
            // Get the native VLC media player handle
            var nativeHandle = _mediaPlayer.Handle;
            if (nativeHandle == IntPtr.Zero) return;
            
            // Set audio format to float samples, 44.1kHz, stereo
            var formatResult = libvlc_audio_set_format(nativeHandle, "f32l", 44100, 2);
            if (formatResult != 0)
            {
                Debug.WriteLine("[VlcAudioService] Warning: Failed to set audio format");
            }
            
            // Set up audio callbacks
            var callbackResult = libvlc_audio_set_callbacks(
                nativeHandle,
                _audioPlayCb,
                _audioPauseCb,
                _audioResumeCb,
                _audioFlushCb,
                _audioDrainCb
            );
            
            if (callbackResult == 0)
            {
                Debug.WriteLine("[VlcAudioService] VLC audio callbacks registered successfully");
            }
            else
            {
                Debug.WriteLine("[VlcAudioService] Warning: Failed to register VLC audio callbacks");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Error setting up audio callbacks for media: {ex.Message}");
        }
    }

    public bool Play()
    {
        if (_isDisposed || !IsReadyToPlay) return false;
        
        try
        {
            _mediaPlayer!.Play();
            Debug.WriteLine("[VlcAudioService] Playback started via IAudioProvider.Play()");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to start playback: {ex.Message}");
            return false;
        }
    }

    public bool Initialize()
    {
        // Already initialized in constructor
        return _libVlc != null && _mediaPlayer != null;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        try
        {
            _isDisposed = true;
            _isPlaying = false;
            
            if (_mediaPlayer?.IsPlaying == true) _mediaPlayer.Stop();
            _currentMedia?.Dispose();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
            
            Debug.WriteLine("[VlcAudioService] Disposed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Error during disposal: {ex.Message}");
        }
    }

    private float[] GenerateResponsiveSpectrumData()
    {
        var data = new float[2048];
        
        // Generate data that responds to playback state
        for (int i = 0; i < data.Length; i++)
        {
            float frequency = i / (float)data.Length;
            float amplitude = (float)(Math.Sin(frequency * Math.PI * 4 + DateTime.Now.Ticks * 0.0001) * 0.5 + 0.5);
            amplitude *= (float)(_random.NextDouble() * 0.5 + 0.5);
            
            data[i] = amplitude;
        }
        
        return data;
    }
    
    private float[] GenerateResponsiveWaveformData()
    {
        var data = new float[2048];
        
        // Generate data that responds to playback state
        for (int i = 0; i < data.Length; i++)
        {
            float time = i / (float)data.Length;
            float amplitude = (float)(Math.Sin(time * Math.PI * 8 + DateTime.Now.Ticks * 0.0001) * 0.6);
            amplitude += (float)(Math.Sin(time * Math.PI * 16 + DateTime.Now.Ticks * 0.0002) * 0.3);
            amplitude *= (float)(_random.NextDouble() * 0.6 + 0.4);
            
            data[i] = amplitude;
        }
        
        return data;
    }

    private float[] GenerateSimulatedSpectrumData()
    {
        var data = new float[2048];
        
        // Generate some simulated frequency data
        for (int i = 0; i < data.Length; i++)
        {
            // Create a more realistic frequency response curve
            float frequency = i / (float)data.Length;
            float amplitude = (float)(Math.Sin(frequency * Math.PI * 4) * 0.5 + 0.5);
            amplitude *= (float)(_random.NextDouble() * 0.3 + 0.7); // Add some randomness
            
            data[i] = amplitude;
        }
        
        return data;
    }
    
    private float[] GenerateSimulatedWaveformData()
    {
        var data = new float[2048];
        
        // Generate some simulated waveform data
        for (int i = 0; i < data.Length; i++)
        {
            // Create a more realistic waveform pattern
            float time = i / (float)data.Length;
            float amplitude = (float)(Math.Sin(time * Math.PI * 8) * 0.6);
            amplitude += (float)(Math.Sin(time * Math.PI * 16) * 0.3);
            amplitude *= (float)(_random.NextDouble() * 0.4 + 0.8); // Add some randomness
            
            data[i] = amplitude;
        }
        
        return data;
    }
}
