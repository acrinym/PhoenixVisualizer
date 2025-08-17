using ManagedBass;
using ManagedBass.Fx;
using System;
using System.Runtime.InteropServices;
using System.IO;

namespace PhoenixVisualizer.Audio;

public record AudioFileInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public double Duration { get; init; }
    public float SampleRate { get; init; }
    public int Channels { get; init; }
    public float BitRate { get; init; }
}

public sealed class AudioService : IDisposable
{
    int _sourceHandle;      // decode source
    int _playHandle;        // the handle we actually play (tempo or direct)
    int _tempoHandle;       // tempo FX handle (playable)
    string? _currentFile;
    bool _tempoEnabled = true; // default ON
    float _tempoPercent;       // -95..+500 (we'll clamp)
    float _pitchSemitones;     // -60..+60 (we'll clamp)

    const float TempoMinPercent = -95f;
    const float TempoMaxPercent = 500f;
    const float PitchMinSemis = -60f;
    const float PitchMaxSemis = 60f;

    static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

    // Debug logging to file
    static void LogToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio_debug.log");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            File.AppendAllText(logPath, logMessage + Environment.NewLine);
        }
        catch
        {
            // Silently fail if logging fails
        }
    }

    public bool Open(string filePath)
    {
        LogToFile($"[AudioService] Open called with: {filePath}");
        Close();

        // Decode-only source
        _sourceHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode | BassFlags.Float);
        LogToFile($"[AudioService] CreateStream result: {_sourceHandle}, Error: {Bass.LastError}");
        if (_sourceHandle == 0) return false;

        _currentFile = filePath;
        LogToFile($"[AudioService] Source stream created successfully");

        // Try to create tempo stream for pitch/tempo control
        try
        {
            LogToFile($"[AudioService] Attempting to create tempo stream");
            var flags = BassFlags.FxFreeSource | BassFlags.Float;
            _tempoHandle = BassFx.TempoCreate(_sourceHandle, flags);
            LogToFile($"[AudioService] TempoCreate result: {_tempoHandle}, Error: {Bass.LastError}");
            
            if (_tempoHandle != 0)
            {
                _tempoEnabled = true;
                _playHandle = _tempoHandle;
                LogToFile($"[AudioService] Tempo stream created successfully, tempo enabled");
                ApplyTempoPitch();
            }
            else
            {
                _tempoEnabled = false;
                LogToFile($"[AudioService] Tempo stream failed, falling back to direct stream");
                _playHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Float);
                LogToFile($"[AudioService] Direct stream result: {_playHandle}, Error: {Bass.LastError}");
                if (_playHandle == 0) return false;
            }
        }
        catch (DllNotFoundException)
        {
            // BASS_FX not available, fall back to basic playback
            LogToFile($"[AudioService] BASS_FX not available, falling back to basic playback");
            System.Diagnostics.Debug.WriteLine("[Audio] BASS_FX not available, tempo/pitch disabled");
            _tempoEnabled = false;
            _playHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Float);
            LogToFile($"[AudioService] Basic stream result: {_playHandle}, Error: {Bass.LastError}");
            if (_playHandle == 0) return false;
        }
        
        LogToFile($"[AudioService] Open completed successfully. PlayHandle: {_playHandle}, TempoEnabled: {_tempoEnabled}");
        return true;
    }

    public bool Play()
    {
        LogToFile($"[AudioService] Play called. PlayHandle: {_playHandle}");
        if (_playHandle == 0) 
        {
            LogToFile($"[AudioService] Play failed - no play handle");
            return false;
        }
        
        var result = Bass.ChannelPlay(_playHandle);
        LogToFile($"[AudioService] ChannelPlay result: {result}, Error: {Bass.LastError}");
        return result;
    }

    public void Pause() { if (_playHandle != 0) Bass.ChannelPause(_playHandle); }
    public void Stop()
    {
        if (_playHandle != 0) Bass.ChannelStop(_playHandle);
        if (_sourceHandle != 0) Bass.ChannelSetPosition(_sourceHandle, 0);
        if (_tempoHandle != 0) Bass.ChannelSetPosition(_tempoHandle, 0);
    }

    public void Close()
    {
        if (_tempoHandle != 0) { Bass.StreamFree(_tempoHandle); _tempoHandle = 0; }
        if (_playHandle != 0 && _playHandle != _tempoHandle) { Bass.StreamFree(_playHandle); }
        _playHandle = 0;

        if (_sourceHandle != 0) { Bass.StreamFree(_sourceHandle); _sourceHandle = 0; }
        _currentFile = null;
        _tempoPercent = 0;
        _pitchSemitones = 0;
        _tempoEnabled = false;
    }

    // ---- Tempo/Pitch surface for UI ----
    public bool TempoEnabled
    {
        get => _tempoEnabled;
        set => ToggleTempo(value);
    }

    public void SetTempoPercent(float percent)
    {
        _tempoPercent = Clamp(percent, TempoMinPercent, TempoMaxPercent);
        ApplyTempoPitch();
    }

    public void SetPitchSemitones(float semis)
    {
        var oldSemis = _pitchSemitones;
        _pitchSemitones = Clamp(semis, PitchMinSemis, PitchMaxSemis);
        
        // If pitch changed significantly, recreate tempo stream
        if (Math.Abs(_pitchSemitones - oldSemis) > 0.1f && _tempoEnabled && _sourceHandle != 0)
        {
            RecreateTempoStream();
        }
        else
        {
            ApplyTempoPitch();
        }
    }

    // Multiplier helpers (what the UI uses)
    // Tempo multiplier: 1.0 = normal, 0.75 = -25%, etc.
    public void SetTempoMultiplier(double multiplier)
    {
        if (multiplier <= 0) multiplier = 0.01; // avoid zero/negatives
        var pct = (float)((multiplier - 1.0) * 100.0);
        SetTempoPercent(pct);
    }

    // Pitch multiplier → semitones = 12 * log2(multiplier)
    public void SetPitchMultiplier(double multiplier)
    {
        if (multiplier <= 0) multiplier = 0.01;
        var semis = (float)(12.0 * Math.Log(multiplier, 2.0));
        SetPitchSemitones(semis);
    }

    public void ResetTempoPitch()
    {
        _tempoPercent = 0;
        _pitchSemitones = 0;
        ApplyTempoPitch();
    }

    void ToggleTempo(bool enabled)
    {
        if (enabled == _tempoEnabled) return;
        _tempoEnabled = enabled;
        if (_currentFile == null) { _playHandle = 0; return; }

        long pos = 0;
        if (_playHandle != 0) pos = Bass.ChannelGetPosition(_playHandle);

        if (_tempoEnabled)
        {
            if (_tempoHandle == 0 && _sourceHandle != 0)
            {
                _tempoHandle = BassFx.TempoCreate(_sourceHandle, BassFlags.FxFreeSource | BassFlags.Float);
                if (_tempoHandle == 0) { _tempoEnabled = false; return; }
            }
            _playHandle = _tempoHandle;
        }
        else
        {
            var direct = Bass.CreateStream(_currentFile, 0, 0, BassFlags.Float);
            if (direct != 0)
            {
                var sec = Bass.ChannelBytes2Seconds(_playHandle, pos);
                Bass.ChannelSetPosition(direct, Bass.ChannelSeconds2Bytes(direct, sec));
                if (_playHandle != 0) Bass.ChannelStop(_playHandle);
                _playHandle = direct;
            }
            else
            {
                _tempoEnabled = true; // fail safe
                _playHandle = _tempoHandle != 0 ? _tempoHandle : _playHandle;
            }
        }
        ApplyTempoPitch();
    }

    void RecreateTempoStream()
    {
        if (_sourceHandle == 0 || !_tempoEnabled) return;
        
        var oldPos = _playHandle != 0 ? Bass.ChannelGetPosition(_playHandle) : 0;
        var wasPlaying = _playHandle != 0 && Bass.ChannelIsActive(_playHandle) == PlaybackState.Playing;
        
        // Clean up old tempo stream
        if (_tempoHandle != 0)
        {
            Bass.StreamFree(_tempoHandle);
            _tempoHandle = 0;
        }
        
        // Create new tempo stream
        try
        {
            _tempoHandle = BassFx.TempoCreate(_sourceHandle, BassFlags.FxFreeSource | BassFlags.Float);
            if (_tempoHandle != 0)
            {
                _playHandle = _tempoHandle;
                ApplyTempoPitch();
                
                // Restore position and playback state
                if (oldPos > 0)
                {
                    Bass.ChannelSetPosition(_tempoHandle, oldPos);
                }
                if (wasPlaying)
                {
                    Bass.ChannelPlay(_tempoHandle);
                }
            }
        }
        catch (DllNotFoundException)
        {
            // BASS_FX not available, fall back to basic playback
            System.Diagnostics.Debug.WriteLine("[Audio] BASS_FX not available during pitch change, tempo/pitch disabled");
            _tempoEnabled = false;
            _playHandle = Bass.CreateStream(_currentFile, 0, 0, BassFlags.Float);
            if (_playHandle != 0 && oldPos > 0)
            {
                Bass.ChannelSetPosition(_playHandle, oldPos);
            }
            if (wasPlaying && _playHandle != 0)
            {
                Bass.ChannelPlay(_playHandle);
            }
        }
    }

    void ApplyTempoPitch()
    {
        if (_playHandle == 0) return;

        if (_tempoEnabled && _playHandle == _tempoHandle && _tempoHandle != 0)
        {
            Bass.ChannelSetAttribute(_tempoHandle, ChannelAttribute.Tempo, _tempoPercent);
        }
        else
        {
            if (_tempoHandle != 0)
            {
                Bass.ChannelSetAttribute(_tempoHandle, ChannelAttribute.Tempo, 0);
            }
        }
    }

    // ---- Legacy compatibility methods ----
    public bool Initialize()
    {
        try
        {
            // -1 = default device, 44100 Hz is fine for visualization
            if (!Bass.Init(-1, 44100, DeviceInitFlags.Default))
            {
                System.Diagnostics.Debug.WriteLine($"[Audio] Bass.Init failed: {Bass.LastError}");
                return false;
            }
            return true;
        }
        catch (DllNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio] BASS native DLL missing: {ex}");
            return false;
        }
    }
    public bool IsReadyToPlay => _playHandle != 0 && !string.IsNullOrEmpty(_currentFile);
    
    public string GetStatus()
    {
        return $"PlayHandle: {(_playHandle != 0 ? "OK" : "NULL")}, File: {(_currentFile ?? "NONE")}, Ready: {IsReadyToPlay}, Tempo: {_tempoEnabled}, Tempo%: {_tempoPercent:F1}, Pitch: {_pitchSemitones:F1}";
    }

    public AudioFileInfo? GetFileInfo()
    {
        if (string.IsNullOrEmpty(_currentFile) || _playHandle == 0) return null;
        
        try
        {
            var info = new AudioFileInfo
            {
                FilePath = _currentFile,
                FileName = Path.GetFileName(_currentFile),
                Duration = GetLengthSeconds(),
                SampleRate = (float)Bass.ChannelGetAttribute(_playHandle, ChannelAttribute.Frequency),
                Channels = 2, // Default to stereo for now
                BitRate = 0 // Not easily available in BASS
            };
            return info;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetFileInfo failed: {ex.Message}");
            return null;
        }
    }

    // FFT and waveform reading (simplified for now)
    public float[] ReadFft()
    {
        if (_playHandle == 0) 
        {
            LogToFile($"[AudioService] ReadFft called but no play handle");
            return new float[2048];
        }
        
        try
        {
            var fftData = new float[2048];
            int fftSize = Bass.ChannelGetData(_playHandle, fftData, (int)DataFlags.FFT2048 | (int)DataFlags.FFTIndividual);
            LogToFile($"[AudioService] ReadFft: ChannelGetData result: {fftSize}, Error: {Bass.LastError}");
            
            if (fftSize > 0)
            {
                // Log first few values to see if we're getting data
                var firstValues = string.Join(",", fftData.Take(5).Select(f => f.ToString("F3")));
                LogToFile($"[AudioService] ReadFft: First 5 values: [{firstValues}]");
                return fftData;
            }
            else
            {
                LogToFile($"[AudioService] ReadFft: No data returned from ChannelGetData");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] ReadFft exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"AudioService.ReadFft failed: {ex.Message}");
        }
        
        return new float[2048];
    }

    public float[] ReadWaveform()
    {
        if (_playHandle == 0) return new float[2048];
        
        try
        {
            var waveData = new float[2048];
            int waveSize = Bass.ChannelGetData(_playHandle, waveData, (int)DataFlags.Float);
            
            if (waveSize > 0)
            {
                return waveData;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.ReadWaveform failed: {ex.Message}");
        }
        
        return new float[2048];
    }

    public double GetPositionSeconds()
    {
        if (_playHandle == 0) return 0.0;
        
        try
        {
            long position = Bass.ChannelGetPosition(_playHandle);
            if (position >= 0)
            {
                return Bass.ChannelBytes2Seconds(_playHandle, position);
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
        if (_playHandle == 0) return 0.0;
        
        try
        {
            long length = Bass.ChannelGetLength(_playHandle);
            if (length >= 0)
            {
                return Bass.ChannelBytes2Seconds(_playHandle, length);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.GetLengthSeconds failed: {ex.Message}");
        }
        
        return 0.0;
    }

    public void Dispose()
    {
        Close();
    }
}
