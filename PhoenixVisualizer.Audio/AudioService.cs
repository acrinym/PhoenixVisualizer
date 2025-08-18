using ManagedBass;
using ManagedBass.Fx;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq; // Added for .Take() and .Skip()

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
    
    // Audio buffer management
    private readonly object _audioLock = new object();
    private bool _isProcessing = false;

    const float TempoMinPercent = -95f;
    const float TempoMaxPercent = 500f;
    const float PitchMinSemis = -60f;
    const float PitchMaxSemis = 60f;

    static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

    // Load additional BASS codec plugins for better format support
    private static void LoadBassCodecPlugins()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var libsDir = Path.Combine(baseDir, "libs");
            
            LogToFile($"[AudioService] Looking for codec plugins in: {libsDir}");
            
            // Check if libs directory exists
            if (!Directory.Exists(libsDir))
            {
                LogToFile($"[AudioService] Warning: libs directory does not exist: {libsDir}");
                LogToFile($"[AudioService] This may cause some audio formats to not play correctly");
                return;
            }
            
            // List of common BASS codec plugins to try loading
            var codecPlugins = new[]
            {
                "bassflac.dll",      // FLAC support
                "bassogg.dll",       // OGG support
                "bass_aac.dll",      // AAC/M4A support
                "basswma.dll",       // WMA support
                "bass_mpc.dll",      // Musepack support
                "bass_ape.dll",      // Monkey's Audio support
                "bass_tta.dll",      // TTA support
                "bass_alac.dll",     // Apple Lossless support
            };

            var loadedPlugins = 0;
            foreach (var plugin in codecPlugins)
            {
                var pluginPath = Path.Combine(libsDir, plugin);
                if (File.Exists(pluginPath))
                {
                    try
                    {
                        var result = Bass.PluginLoad(pluginPath);
                        if (result != 0)
                        {
                            loadedPlugins++;
                            LogToFile($"[AudioService] Successfully loaded codec plugin: {plugin}");
                        }
                        else
                        {
                            var error = Bass.LastError;
                            LogToFile($"[AudioService] Failed to load codec plugin: {plugin}, Error: {error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"[AudioService] Exception loading codec plugin {plugin}: {ex.Message}");
                    }
                }
                else
                {
                    LogToFile($"[AudioService] Codec plugin not found: {plugin}");
                }
            }
            
            LogToFile($"[AudioService] Loaded {loadedPlugins} codec plugins out of {codecPlugins.Length} attempted");
            
            if (loadedPlugins == 0)
            {
                LogToFile($"[AudioService] Warning: No codec plugins loaded. Only basic formats (MP3, WAV) may work.");
                LogToFile($"[AudioService] Consider downloading BASS codec plugins from: https://www.un4seen.com/");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] Error in LoadBassCodecPlugins: {ex.Message}");
        }
    }

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

        try
        {
            // Load additional BASS codec plugins for better format support
            LoadBassCodecPlugins();

            // Validate file exists and is accessible
            if (!File.Exists(filePath))
            {
                LogToFile($"[AudioService] File does not exist: {filePath}");
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                LogToFile($"[AudioService] File is empty: {filePath}");
                return false;
            }

            LogToFile($"[AudioService] File validation passed. Size: {fileInfo.Length} bytes");
            LogToFile($"[AudioService] File extension: {Path.GetExtension(filePath)}");

            // Ensure BASS is initialized (skip complex checking for now)
            try
            {
                Bass.Init();
                LogToFile($"[AudioService] BASS initialization attempted");
            }
            catch (Exception ex)
            {
                LogToFile($"[AudioService] BASS initialization exception: {ex.Message}");
                // Continue anyway - BASS might already be initialized
            }

            // Try to create stream with more detailed error reporting
            LogToFile($"[AudioService] Attempting to create source stream...");
            _sourceHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode | BassFlags.Float);
            var sourceError = Bass.LastError;
            LogToFile($"[AudioService] CreateStream result: {_sourceHandle}, Error: {sourceError}");
            
            if (_sourceHandle == 0)
            {
                LogToFile($"[AudioService] Failed to create source stream: {sourceError}");
                
                // Try alternative approach - direct stream without decode flag
                LogToFile($"[AudioService] Trying alternative stream creation...");
                _sourceHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Float);
                var altError = Bass.LastError;
                LogToFile($"[AudioService] Alternative CreateStream result: {_sourceHandle}, Error: {altError}");
                
                if (_sourceHandle == 0)
                {
                    LogToFile($"[AudioService] All stream creation attempts failed");
                    LogToFile($"[AudioService] This file format may not be supported by BASS");
                    LogToFile($"[AudioService] Try installing additional codec plugins or convert to MP3/WAV");
                    return false;
                }
            }

            _currentFile = filePath;
            LogToFile($"[AudioService] Source stream created successfully");

            // Create playable stream
            LogToFile($"[AudioService] Creating playable stream...");
            _playHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Float);
            if (_playHandle == 0)
            {
                LogToFile($"[AudioService] Failed to create direct play stream: {Bass.LastError}");
                Bass.StreamFree(_sourceHandle);
                _sourceHandle = 0;
                _currentFile = null;
                return false;
            }

            LogToFile($"[AudioService] Playable stream created successfully");

            // Optimize audio buffer settings for visualization
            try
            {
                // Set a larger buffer to prevent audio dropouts
                Bass.ChannelSetAttribute(_playHandle, ChannelAttribute.Buffer, 1000); // 1 second buffer
                LogToFile($"[AudioService] Audio buffer settings optimized");
            }
            catch (Exception ex)
            {
                LogToFile($"[AudioService] Failed to optimize audio buffer settings: {ex.Message}");
            }

            // Try to create tempo stream as backup (but don't use it yet)
            try
            {
                LogToFile($"[AudioService] Attempting to create tempo stream");
                var flags = BassFlags.FxFreeSource | BassFlags.Float;
                _tempoHandle = BassFx.TempoCreate(_sourceHandle, flags);
                LogToFile($"[AudioService] TempoCreate result: {_tempoHandle}, Error: {Bass.LastError}");
                
                if (_tempoHandle != 0)
                {
                    _tempoEnabled = true;
                    LogToFile($"[AudioService] Tempo stream created successfully as backup");
                    
                    // Apply same buffer optimizations to tempo stream
                    try
                    {
                        Bass.ChannelSetAttribute(_tempoHandle, ChannelAttribute.Buffer, 1000);
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"[AudioService] Failed to optimize tempo stream buffer settings: {ex.Message}");
                    }
                }
                else
                {
                    _tempoEnabled = false;
                    LogToFile($"[AudioService] Tempo stream failed, using direct stream only");
                }
            }
            catch (DllNotFoundException)
            {
                LogToFile($"[AudioService] BASS_FX not available, tempo/pitch disabled");
                _tempoEnabled = false;
                _tempoHandle = 0;
            }
            
            LogToFile($"[AudioService] Open completed successfully. PlayHandle: {_playHandle}, TempoEnabled: {_tempoEnabled}");
            return true;
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] Open failed with exception: {ex.Message}");
            LogToFile($"[AudioService] Exception stack trace: {ex.StackTrace}");
            Close();
            return false;
        }
    }

    public bool Play()
    {
        LogToFile($"[AudioService] Play called. PlayHandle: {_playHandle}");
        if (_playHandle == 0) 
        {
            LogToFile($"[AudioService] Play failed - no play handle");
            return false;
        }
        
        try
        {
            // Ensure the stream is in a clean state before playing
            var state = Bass.ChannelIsActive(_playHandle);
            if (state == PlaybackState.Stopped)
            {
                // Reset position to start if stopped
                Bass.ChannelSetPosition(_playHandle, 0);
            }
            
            var result = Bass.ChannelPlay(_playHandle);
            LogToFile($"[AudioService] ChannelPlay result: {result}, Error: {Bass.LastError}");
            return result;
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] Play failed with exception: {ex.Message}");
            return false;
        }
    }

    public void Pause() 
    { 
        try
        {
            if (_playHandle != 0) 
            {
                var state = Bass.ChannelIsActive(_playHandle);
                if (state == PlaybackState.Playing)
                {
                    Bass.ChannelPause(_playHandle);
                }
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] Pause failed with exception: {ex.Message}");
        }
    }

    public void Stop()
    {
        try
        {
            if (_playHandle != 0) 
            {
                Bass.ChannelStop(_playHandle);
                // Reset position to start
                Bass.ChannelSetPosition(_playHandle, 0);
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] Stop failed with exception: {ex.Message}");
        }
    }

    public void Close()
    {
        try
        {
            if (_tempoHandle != 0) 
            { 
                Bass.StreamFree(_tempoHandle); 
                _tempoHandle = 0; 
            }
            if (_playHandle != 0) 
            { 
                Bass.StreamFree(_playHandle); 
                _playHandle = 0;
            }
            if (_sourceHandle != 0) 
            { 
                Bass.StreamFree(_sourceHandle); 
                _sourceHandle = 0; 
            }
            _currentFile = null;
            _tempoPercent = 0;
            _pitchSemitones = 0;
            _tempoEnabled = false;
            
            LogToFile($"[AudioService] Close completed successfully");
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] Close failed with exception: {ex.Message}");
        }
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

    public bool ResetAudioStream()
    {
        LogToFile($"[AudioService] ResetAudioStream called");
        
        try
        {
            if (_playHandle == 0) return false;
            
            var wasPlaying = Bass.ChannelIsActive(_playHandle) == PlaybackState.Playing;
            var currentPos = Bass.ChannelGetPosition(_playHandle);
            
            // Stop current playback
            Bass.ChannelStop(_playHandle);
            
            // Create a fresh stream
            int newHandle;
            if (_tempoEnabled && _tempoHandle != 0)
            {
                newHandle = Bass.CreateStream(_currentFile, 0, 0, BassFlags.Float);
                if (newHandle != 0)
                {
                    _playHandle = newHandle;
                    ApplyTempoPitch();
                }
            }
            else
            {
                newHandle = Bass.CreateStream(_currentFile, 0, 0, BassFlags.Float);
                if (newHandle != 0)
                {
                    _playHandle = newHandle;
                }
            }
            
            if (newHandle != 0)
            {
                // Restore position and playback state
                if (currentPos > 0)
                {
                    Bass.ChannelSetPosition(_playHandle, currentPos);
                }
                if (wasPlaying)
                {
                    Bass.ChannelPlay(_playHandle);
                }
                
                LogToFile($"[AudioService] Audio stream reset successfully");
                return true;
            }
            else
            {
                LogToFile($"[AudioService] Failed to create new audio stream: {Bass.LastError}");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] ResetAudioStream failed with exception: {ex.Message}");
            return false;
        }
    }

    void ToggleTempo(bool enabled)
    {
        if (enabled == _tempoEnabled) return;
        
        LogToFile($"[AudioService] ToggleTempo called: {enabled}");
        
        try
        {
            if (enabled && _tempoHandle != 0)
            {
                // Switch to tempo stream - simple approach
                if (_playHandle != 0 && _playHandle != _tempoHandle)
                {
                    Bass.ChannelStop(_playHandle);
                }
                
                _playHandle = _tempoHandle;
                _tempoEnabled = true;
                
                LogToFile($"[AudioService] Switched to tempo stream successfully");
            }
            else if (!enabled)
            {
                // Switch back to direct stream - simple approach
                if (_playHandle != 0 && _playHandle != _tempoHandle)
                {
                    Bass.ChannelStop(_playHandle);
                }
                
                // Create new direct stream
                var direct = Bass.CreateStream(_currentFile, 0, 0, BassFlags.Float);
                if (direct != 0)
                {
                    _playHandle = direct;
                    _tempoEnabled = false;
                    LogToFile($"[AudioService] Switched to direct stream successfully");
                }
                else
                {
                    LogToFile($"[AudioService] Failed to create direct stream, keeping current stream");
                    _tempoEnabled = true; // fail safe
                }
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] ToggleTempo failed with exception: {ex.Message}");
            // Keep current state on error
        }
    }

    void RecreateTempoStream()
    {
        // Simplified - just recreate the tempo stream without switching
        if (_sourceHandle == 0 || !_tempoEnabled) return;
        
        LogToFile($"[AudioService] RecreateTempoStream called");
        
        try
        {
            // Clean up old tempo stream
            if (_tempoHandle != 0)
            {
                Bass.StreamFree(_tempoHandle);
                _tempoHandle = 0;
            }
            
            // Create new tempo stream
            _tempoHandle = BassFx.TempoCreate(_sourceHandle, BassFlags.FxFreeSource | BassFlags.Float);
            if (_tempoHandle != 0)
            {
                LogToFile($"[AudioService] Tempo stream recreated successfully");
                ApplyTempoPitch();
            }
            else
            {
                LogToFile($"[AudioService] Failed to recreate tempo stream: {Bass.LastError}");
                _tempoEnabled = false;
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] RecreateTempoStream failed with exception: {ex.Message}");
            _tempoEnabled = false;
        }
    }

    void ApplyTempoPitch()
    {
        if (_playHandle == 0) return;

        try
        {
            if (_tempoEnabled && _playHandle == _tempoHandle && _tempoHandle != 0)
            {
                Bass.ChannelSetAttribute(_tempoHandle, ChannelAttribute.Tempo, _tempoPercent);
                LogToFile($"[AudioService] Applied tempo: {_tempoPercent:F1}%");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[AudioService] ApplyTempoPitch failed with exception: {ex.Message}");
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

    public string GetAudioHealth()
    {
        if (_playHandle == 0) return "No audio handle";
        
        try
        {
            var state = Bass.ChannelIsActive(_playHandle);
            var position = Bass.ChannelGetPosition(_playHandle);
            var length = Bass.ChannelGetLength(_playHandle);
            var freq = Bass.ChannelGetAttribute(_playHandle, ChannelAttribute.Frequency);
            var volume = Bass.ChannelGetAttribute(_playHandle, ChannelAttribute.Volume);
            
            return $"State: {state}, Pos: {position}, Length: {length}, Freq: {freq:F0}Hz, Vol: {volume:F2}";
        }
        catch (Exception ex)
        {
            return $"Health check failed: {ex.Message}";
        }
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
        
        // Thread-safe audio reading
        lock (_audioLock)
        {
            if (_isProcessing)
            {
                return new float[2048]; // Return zeros if already processing
            }
            
            _isProcessing = true;
            
            try
            {
                var fftData = new float[2048];
                
                // Check if channel is actually playing
                var playbackState = Bass.ChannelIsActive(_playHandle);
                if (playbackState != PlaybackState.Playing)
                {
                    LogToFile($"[AudioService] ReadFft: Channel not playing, returning zeros");
                    return new float[2048];
                }
                
                // Clear the FFT data array first to ensure we don't get stale data
                Array.Clear(fftData, 0, fftData.Length);
                
                // Use FFT2048 to get 2048 frequency bins (0-22050 Hz for 44.1kHz audio)
                // Mono FFT for stability (stereo FFTIndividual can cause blocking)
                int fftSize = Bass.ChannelGetData(_playHandle, fftData, (int)DataFlags.FFT2048);
                
                if (fftSize > 0)
                {
                    // Simple validation - just check if we got any meaningful data
                    var sum = fftData.Sum(f => MathF.Abs(f));
                    var maxValue = fftData.Max(f => MathF.Abs(f));
                    
                    if (sum > 0.001f && maxValue > 0.001f)
                    {
                        return fftData;
                    }
                    else
                    {
                        LogToFile($"[AudioService] ReadFft: Data appears stuck (sum: {sum:F6}, max: {maxValue:F6})");
                        
                        // Try to recover by resetting the audio stream
                        if (ResetAudioStream())
                        {
                            LogToFile($"[AudioService] ReadFft: Audio stream reset, retrying FFT read");
                            // Try reading again after reset
                            Array.Clear(fftData, 0, fftData.Length);
                            fftSize = Bass.ChannelGetData(_playHandle, fftData, (int)DataFlags.FFT2048);
                            if (fftSize > 0)
                            {
                                sum = fftData.Sum(f => MathF.Abs(f));
                                maxValue = fftData.Max(f => MathF.Abs(f));
                                if (sum > 0.001f && maxValue > 0.001f)
                                {
                                    return fftData;
                                }
                            }
                        }
                    }
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
            finally
            {
                _isProcessing = false;
            }
        }
        
        return new float[2048];
    }

    public float[] ReadWaveform()
    {
        if (_playHandle == 0) 
        {
            LogToFile($"[AudioService] ReadWaveform called but no play handle");
            return new float[2048];
        }
        
        // Thread-safe audio reading
        lock (_audioLock)
        {
            if (_isProcessing)
            {
                return new float[2048]; // Return zeros if already processing
            }
            
            _isProcessing = true;
            
            try
            {
                var waveData = new float[2048];
                
                // Check if channel is actually playing
                var playbackState = Bass.ChannelIsActive(_playHandle);
                if (playbackState != PlaybackState.Playing)
                {
                    LogToFile($"[AudioService] ReadWaveform: Channel not playing, returning zeros");
                    return new float[2048];
                }
                
                // Clear the waveform data array first to ensure we don't get stale data
                Array.Clear(waveData, 0, waveData.Length);
                
                // Get raw waveform data - request exactly 2048 samples
                // Explicit byte count prevents blocking for ~1MB of audio
                int bytesToRead = waveData.Length * sizeof(float);
                int waveSize = Bass.ChannelGetData(_playHandle, waveData, bytesToRead | (int)DataFlags.Float);
                
                if (waveSize > 0)
                {
                    // Simple validation - just check if we got any meaningful data
                    var sum = waveData.Sum(w => MathF.Abs(w));
                    var maxValue = waveData.Max(w => MathF.Abs(w));
                    
                    if (sum > 0.001f && maxValue > 0.001f)
                    {
                        return waveData;
                    }
                    else
                    {
                        LogToFile($"[AudioService] ReadWaveform: Data appears stuck (sum: {sum:F6}, max: {maxValue:F6})");
                        
                        // Try to recover by resetting the audio stream
                        if (ResetAudioStream())
                        {
                            LogToFile($"[AudioService] ReadWaveform: Audio stream reset, retrying waveform read");
                            // Try reading again after reset
                            Array.Clear(waveData, 0, waveData.Length);
                            waveSize = Bass.ChannelGetData(_playHandle, waveData, bytesToRead | (int)DataFlags.Float);
                            if (waveSize > 0)
                            {
                                sum = waveData.Sum(w => MathF.Abs(w));
                                maxValue = waveData.Max(w => MathF.Abs(w));
                                if (sum > 0.001f && maxValue > 0.001f)
                                {
                                    return waveData;
                                }
                            }
                        }
                    }
                }
                else
                {
                    LogToFile($"[AudioService] ReadWaveform: No data returned from ChannelGetData");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"[AudioService] ReadWaveform exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AudioService.ReadWaveform failed: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
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
