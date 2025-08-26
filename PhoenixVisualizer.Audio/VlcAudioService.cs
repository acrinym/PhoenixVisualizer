using System;
using System.Diagnostics;
using PhoenixVisualizer.Audio.Interfaces;
using PhoenixVisualizer.Core.Services;
using LibVLCSharp.Shared;
using System.IO;

namespace PhoenixVisualizer.Audio;

public class VlcAudioService : IAudioService, IAudioProvider, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _currentMedia;
    private string _currentFile = string.Empty;
    private bool _isPlaying = false;
    private bool _isDisposed = false;
    private readonly Random _random = new Random();

    public bool IsPlaying => _isPlaying;

    public VlcAudioService()
    {
        try
        {
            Debug.WriteLine("[VlcAudioService] Starting initialization...");
            
            // Use the simple approach from the working example
            _libVlc = new LibVLC(enableDebugLogs: true);
            Debug.WriteLine("[VlcAudioService] LibVLC instance created successfully");
            
            _mediaPlayer = new MediaPlayer(_libVlc);
            Debug.WriteLine("[VlcAudioService] MediaPlayer instance created successfully");
            
            // Set up event handlers
            _mediaPlayer.TimeChanged += (s, e) => Debug.WriteLine($"[VlcAudioService] Time: {_mediaPlayer.Time}ms");
            _mediaPlayer.LengthChanged += (s, e) => Debug.WriteLine($"[VlcAudioService] Length: {_mediaPlayer.Length}ms");
            _mediaPlayer.Playing += (s, e) => 
            { 
                _isPlaying = true; 
                Debug.WriteLine("[VlcAudioService] Playback started"); 
            };
            _mediaPlayer.Paused += (s, e) => 
            { 
                _isPlaying = false; 
                Debug.WriteLine("[VlcAudioService] Playback paused"); 
            };
            _mediaPlayer.Stopped += (s, e) => 
            { 
                _isPlaying = false; 
                Debug.WriteLine("[VlcAudioService] Playback stopped"); 
            };
            
            Debug.WriteLine("[VlcAudioService] Initialized successfully with LibVLC (debug enabled)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VlcAudioService] Failed to initialize: {ex.Message}");
            Debug.WriteLine($"[VlcAudioService] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public void Play(string path)
    {
        if (_isDisposed) return;
        
        try
        {
            Debug.WriteLine($"[VlcAudioService] Attempting to play: {path}");
            
            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Stop();

            _currentMedia?.Dispose();
            _currentMedia = new Media(_libVlc, new Uri(path));
            _mediaPlayer.Media = _currentMedia;
            _currentFile = path;
            
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
        // For now, return simulated data until we implement real audio capture
        return GenerateSimulatedWaveformData();
    }

    public float[] GetSpectrumData()
    {
        // For now, return simulated data until we implement real audio capture
        return GenerateSimulatedSpectrumData();
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
            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Stop();

            _currentMedia?.Dispose();
            _currentMedia = new Media(_libVlc, new Uri(path));
            _mediaPlayer.Media = _currentMedia;
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
            _mediaPlayer.Play();
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
            
            if (_mediaPlayer.IsPlaying) _mediaPlayer.Stop();
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
