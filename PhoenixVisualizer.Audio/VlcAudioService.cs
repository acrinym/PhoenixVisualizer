using System;
using System.Diagnostics;
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
    private readonly Random _random = new Random();
    
    // Audio data buffers for visualizers
    private readonly float[] _spectrumData = new float[2048];
    private readonly float[] _waveformData = new float[2048];
    private readonly object _audioLock = new object();

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
            _mediaPlayer = new MediaPlayer(_libVlc);
            Debug.WriteLine("[VlcAudioService] MediaPlayer instance created successfully");
            
            // Set up event handlers
            _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            
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
            _currentMedia = new Media(_libVlc!, new Uri(Path.GetFullPath(path)));
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
                // For now, return simulated data that responds to playback state
                return GenerateResponsiveWaveformData();
            }
            return GenerateSimulatedWaveformData();
        }
    }

    public float[] GetSpectrumData()
    {
        lock (_audioLock)
        {
            if (_isPlaying && _mediaPlayer != null)
            {
                // For now, return simulated data that responds to playback state
                return GenerateResponsiveSpectrumData();
            }
            return GenerateSimulatedSpectrumData();
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
            _currentMedia = new Media(_libVlc!, new Uri(Path.GetFullPath(path)));
            _mediaPlayer!.Media = _currentMedia;
            _currentFile = path;
            
            Debug.WriteLine($"[VlcAudioService] Opened file: {path}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to open {path}: {ex.Message}");
            return false;
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
