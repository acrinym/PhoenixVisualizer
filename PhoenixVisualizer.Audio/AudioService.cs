using ManagedBass;
using ManagedBass.Fx;
using System;

namespace PhoenixVisualizer.Audio;

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

    public bool Open(string filePath)
    {
        Close();

        // Decode-only source
        _sourceHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode | BassFlags.Float);
        if (_sourceHandle == 0) return false;

        _currentFile = filePath;

        // PLAYABLE tempo stream (NO Decode flag) → this is the core Play fix
        _tempoHandle = BassFx.TempoCreate(_sourceHandle, BassFlags.FxFreeSource | BassFlags.Float);
        if (_tempoHandle == 0)
        {
            // Fallback: direct playable stream
            Bass.StreamFree(_sourceHandle);
            _sourceHandle = 0;
            _playHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Float);
            if (_playHandle == 0) return false;
            _tempoEnabled = false;
        }
        else
        {
            _tempoEnabled = true;
            _playHandle = _tempoHandle;
            ApplyTempoPitch();
        }
        return true;
    }

    public bool Play()
    {
        if (_playHandle == 0) return false;
        return Bass.ChannelPlay(_playHandle);
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
        _pitchSemitones = Clamp(semis, PitchMinSemis, PitchMaxSemis);
        ApplyTempoPitch();
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

    void ApplyTempoPitch()
    {
        if (_playHandle == 0) return;

        if (_tempoEnabled && _playHandle == _tempoHandle && _tempoHandle != 0)
        {
            Bass.ChannelSetAttribute(_tempoHandle, ChannelAttribute.Tempo, _tempoPercent);
            Bass.ChannelSetAttribute(_tempoHandle, ChannelAttribute.Pitch, _pitchSemitones);
        }
        else
        {
            if (_tempoHandle != 0)
            {
                Bass.ChannelSetAttribute(_tempoHandle, ChannelAttribute.Tempo, 0);
                Bass.ChannelSetAttribute(_tempoHandle, ChannelAttribute.Pitch, 0);
            }
        }
    }

    // ---- Legacy compatibility methods ----
    public bool Initialize() => true; // Always initialized with ManagedBass
    public bool IsReadyToPlay => _playHandle != 0 && !string.IsNullOrEmpty(_currentFile);
    
    public string GetStatus()
    {
        return $"PlayHandle: {(_playHandle != 0 ? "OK" : "NULL")}, File: {(_currentFile ?? "NONE")}, Ready: {IsReadyToPlay}, Tempo: {_tempoEnabled}, Tempo%: {_tempoPercent:F1}, Pitch: {_pitchSemitones:F1}";
    }

    // FFT and waveform reading (simplified for now)
    public float[] ReadFft()
    {
        if (_playHandle == 0) return new float[2048];
        
        try
        {
            var fftData = new float[2048];
            int fftSize = Bass.ChannelGetData(_playHandle, fftData, (int)DataFlags.FFT2048 | (int)DataFlags.FFTIndividual);
            
            if (fftSize > 0)
            {
                return fftData;
            }
        }
        catch (Exception ex)
        {
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
