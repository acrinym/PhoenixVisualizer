using System;
using System.Numerics;
using ManagedBass;
using ManagedBass.Fx;

namespace PhoenixVisualizer.Audio;

public sealed class AudioService : IDisposable
{
    // Playback
    private int _streamHandle = 0;
    private int _fxHandle = 0;
    private bool _isPlaying = false;
    private bool _isPaused = false;

    // Ring buffer for the last 2048 mono samples (power of two for FFT)
    private const int N = 2048;
    private readonly float[] _ring = new float[N];
    private readonly object _lock = new();

    // Reusable buffers (returned to callers; caller treats them as read-only snapshots)
    private readonly float[] _fftBuffer = new float[N];   // magnitude spectrum
    private readonly float[] _waveBuffer = new float[N];  // ordered last-2048 waveform (mono)

    private bool _initialized;
    private string? _currentFilePath;

    public bool Initialize()
    {
        if (_initialized) return true;
        
        try
        {
            // Initialize ManagedBass
            if (!Bass.Init())
            {
                System.Diagnostics.Debug.WriteLine($"AudioService.Initialize failed: {Bass.LastError}");
                return false;
            }
            
            _initialized = true;
            System.Diagnostics.Debug.WriteLine("AudioService.Initialize: Successfully initialized ManagedBass");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Initialize failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"AudioService.Initialize stack trace: {ex.StackTrace}");
            _initialized = false;
        }
        return _initialized;
    }
    
    /// <summary>
    /// Reinitializes the audio service if there are issues
    /// </summary>
    public bool Reinitialize()
    {
        try
        {
            _initialized = false;
            CloseCurrentStream();
            return Initialize();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Reinitialize failed: {ex.Message}");
            return false;
        }
    }

    public bool Open(string filePath)
    {
        if (!_initialized && !Initialize()) return false;

        try
        {
            // Close any existing stream
            CloseCurrentStream();
            
            // Create a new stream
            _streamHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode | BassFlags.Float);
            if (_streamHandle == 0)
            {
                System.Diagnostics.Debug.WriteLine($"AudioService.Open failed: {Bass.LastError}");
                return false;
            }

            // Create FFT effect
            _fxHandle = BassFx.TempoCreate(_streamHandle, BassFlags.FxFreeSource);
            if (_fxHandle == 0)
            {
                System.Diagnostics.Debug.WriteLine($"AudioService.Open: Failed to create FFT effect: {Bass.LastError}");
                // Continue without FFT effect
                _fxHandle = _streamHandle;
            }

            _currentFilePath = filePath;
            
            // Reset ring buffer when opening a new file
            lock (_lock)
            {
                Array.Clear(_ring, 0, _ring.Length);
            }

            System.Diagnostics.Debug.WriteLine($"AudioService.Open: Successfully opened {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Open failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            return false;
        }
    }

    public bool Play()
    {
        if (!IsReadyToPlay)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Play: Not ready to play. Status: {GetStatus()}");
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                System.Diagnostics.Debug.WriteLine("AudioService.Play: No audio file loaded. Please open an audio file first.");
            }
            return false;
        }
        
        try
        {
            if (_isPaused)
            {
                // Resume from pause
                if (Bass.ChannelPlay(_fxHandle))
                {
                    _isPlaying = true;
                    _isPaused = false;
                    System.Diagnostics.Debug.WriteLine("AudioService.Play: Resumed playback successfully");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"AudioService.Play resume failed: {Bass.LastError}");
                    return false;
                }
            }
            else
            {
                // Start new playback
                if (Bass.ChannelPlay(_fxHandle))
                {
                    _isPlaying = true;
                    System.Diagnostics.Debug.WriteLine("AudioService.Play: Started playback successfully");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"AudioService.Play start failed: {Bass.LastError}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Play failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"AudioService.Play stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public void Pause()
    {
        if (!IsReadyToPlay)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Pause: Not ready to pause. Status: {GetStatus()}");
            return;
        }
        
        try
        {
            if (Bass.ChannelPause(_fxHandle))
            {
                _isPlaying = false;
                _isPaused = true;
                System.Diagnostics.Debug.WriteLine("AudioService.Pause: Paused playback successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AudioService.Pause failed: {Bass.LastError}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Pause failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"AudioService.Pause stack trace: {ex.StackTrace}");
        }
    }

    public void Stop()
    {
        if (!IsReadyToPlay)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Stop: Not ready to stop. Status: {GetStatus()}");
            return;
        }

        try
        {
            if (Bass.ChannelStop(_fxHandle))
            {
                _isPlaying = false;
                _isPaused = false;
                System.Diagnostics.Debug.WriteLine("AudioService.Stop: Stopped playback successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AudioService.Stop failed: {Bass.LastError}");
            }

            // Reset to beginning
            try
            {
                Bass.ChannelSetPosition(_fxHandle, 0);
                
                            // Clear cached audio so visualizers fall back to silence 🎧
            lock (_lock)
            {
                Array.Clear(_ring, 0, _ring.Length);
            }

                System.Diagnostics.Debug.WriteLine("AudioService.Stop: Reset to beginning");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AudioService.Stop reset failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Stop failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"AudioService.Stop stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Returns the current magnitude spectrum (size 2048).
    /// Computed from the most recent 2048 mono samples using a Hann window + radix-2 FFT.
    /// </summary>
    public float[] ReadFft()
    {
        if (_fxHandle == 0) return _fftBuffer;

        try
        {
            // Get FFT data from ManagedBass
            var fftData = new float[N];
            int fftSize = Bass.ChannelGetData(_fxHandle, fftData, (int)DataFlags.FFT2048 | (int)DataFlags.FFTIndividual);
            
            if (fftSize > 0)
            {
                // Copy FFT data to our buffer
                Array.Copy(fftData, _fftBuffer, Math.Min(fftData.Length, _fftBuffer.Length));
            }
            else
            {
                // Fallback to silence if no FFT data
                Array.Clear(_fftBuffer, 0, _fftBuffer.Length);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.ReadFft failed: {ex.Message}");
            Array.Clear(_fftBuffer, 0, _fftBuffer.Length);
        }

        return _fftBuffer;
    }

    /// <summary>
    /// Returns an ordered copy of the last 2048 mono samples (time domain).
    /// </summary>
    public float[] ReadWaveform()
    {
        if (_fxHandle == 0) return _waveBuffer;

        try
        {
            // Get waveform data from ManagedBass
            var waveData = new float[N];
            int waveSize = Bass.ChannelGetData(_fxHandle, waveData, (int)DataFlags.Float);
            
            if (waveSize > 0)
            {
                // Copy waveform data to our buffer
                Array.Copy(waveData, _waveBuffer, Math.Min(waveData.Length, _waveBuffer.Length));
            }
            else
            {
                // Fallback to silence if no waveform data
                Array.Clear(_waveBuffer, 0, _waveBuffer.Length);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.ReadWaveform failed: {ex.Message}");
            Array.Clear(_waveBuffer, 0, _waveBuffer.Length);
        }

        return _waveBuffer;
    }

    public double GetPositionSeconds()
    {
        if (_fxHandle == 0) return 0.0;
        
        try
        {
            long position = Bass.ChannelGetPosition(_fxHandle);
            if (position >= 0)
            {
                return Bass.ChannelBytes2Seconds(_fxHandle, position);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.GetPositionSeconds failed: {ex.Message}");
        }
        
        return 0.0;
    }

    public double GetLengthSeconds()
    {
        if (_fxHandle == 0) return 0.0;
        
        try
        {
            long length = Bass.ChannelGetLength(_fxHandle);
            if (length >= 0)
            {
                return Bass.ChannelBytes2Seconds(_fxHandle, length);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.GetLengthSeconds failed: {ex.Message}");
        }
        
        return 0.0;
    }
    
    /// <summary>
    /// Checks if the audio is ready to play (initialized, file loaded, and stream ready)
    /// </summary>
    public bool IsReadyToPlay => _initialized && _fxHandle != 0 && !string.IsNullOrEmpty(_currentFilePath);
    
    /// <summary>
    /// Gets the current status string for debugging
    /// </summary>
    public string GetStatus()
    {
        return $"Initialized: {_initialized}, StreamHandle: {(_fxHandle != 0 ? "OK" : "NULL")}, File: {(_currentFilePath ?? "NONE")}, Ready: {IsReadyToPlay}, Playing: {_isPlaying}, Paused: {_isPaused}";
    }

    private void CloseCurrentStream()
    {
        try
        {
            if (_fxHandle != 0 && _fxHandle != _streamHandle)
            {
                Bass.StreamFree(_fxHandle);
                _fxHandle = 0;
            }
            
            if (_streamHandle != 0)
            {
                Bass.StreamFree(_streamHandle);
                _streamHandle = 0;
            }
            
            _isPlaying = false;
            _isPaused = false;
            _currentFilePath = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.CloseCurrentStream failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            CloseCurrentStream();
        }
        catch { /* ignore */ }
        
        try
        {
            if (_initialized)
            {
                Bass.Free();
                _initialized = false;
            }
        }
        catch { /* ignore */ }
    }
}
