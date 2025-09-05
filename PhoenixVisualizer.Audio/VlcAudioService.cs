using LibVLCSharp.Shared;
using System;
using System.Collections.Concurrent;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using PhoenixVisualizer.Audio.Interfaces;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Services;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace PhoenixVisualizer.Audio
{
    public enum VisualizerMode 
    { 
        Custom,     // PCM capture → Custom effect nodes
        Goom,       // VLC Goom visualizer as video frames
        Spectrum,   // VLC Spectrum visualizer as video frames
        Visual,     // VLC Visual visualizer as video frames
        ProjectM    // VLC ProjectM visualizer as video frames
    }

    public class VlcAudioService : IAudioService, IAudioProvider, IDisposable
    {
        private static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PhoenixVisualizer_Debug.log");
        
        private static void LogToFile(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFile, logEntry);
                Debug.WriteLine(message); // Also write to debug output
            }
            catch
            {
                // Ignore logging errors
            }
        }
        
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media? _currentMedia;
        
        // Fixed array sizes to match RenderSurface expectations
        private const int FixedFftSize = 512;
        private const int FixedWaveSize = 1024;
        
        // Audio data capture (Custom mode)
        private readonly ConcurrentQueue<short[]> _pcmQueue = new();
        private bool _audioCallbackRegistered = false;
        
        // Video frame capture (VLC Native mode)
        private readonly ConcurrentQueue<byte[]> _frameQueue = new();
        private bool _videoCallbackRegistered = false;
        
        // Audio analysis data
        private readonly object _audioLock = new();
        private float[] _waveformData = new float[FixedWaveSize];
        private float[] _spectrumData = new float[FixedFftSize];
        private float[] _leftChannel = new float[512];
        private float[] _rightChannel = new float[512];
        private float[] _centerChannel = new float[512];
        
        // Audio features
        private float _rms = 0.0f;
        private float _peak = 0.0f;
        private float _volume = 0.0f;
        private float _bpm = 0.0f;
        private bool _beat = false;
        private float _beatIntensity = 0.0f;
        private float _time = 0.0f;
        
        // Frequency bands
        private float _bass = 0.0f;
        private float _mid = 0.0f;
        private float _treble = 0.0f;
        
        // Mode and state
        private VisualizerMode _currentMode = VisualizerMode.Custom;
        private bool _isPlaying = false;
        private bool _isInitialized = false;
        
        // Frequency retuning
        private float _fundamentalFrequency = 440.0f;
        private IAudioService.FrequencyPreset _currentPreset = IAudioService.FrequencyPreset.Standard440Hz;

        public bool IsPlaying => _isPlaying;
        public VlcVisualizer GetCurrentVisualizer() => _currentMode switch
        {
            VisualizerMode.Goom => VlcVisualizer.Goom,
            VisualizerMode.Spectrum => VlcVisualizer.Spectrum,
            VisualizerMode.Visual => VlcVisualizer.Visual,
            VisualizerMode.ProjectM => VlcVisualizer.ProjectM,
            _ => VlcVisualizer.Goom
        };

        public VlcAudioService()
        {
            try
            {
                LogToFile("[VlcAudioService] 🚀 INITIALIZING VLC AUDIO SERVICE...");
                LogToFile($"[VlcAudioService] 📅 Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

                // Create LibVLC with safer options - disable problematic plugins
                var options = new string[] { 
                    "--no-video", 
                    "--no-audio-display",
                    "--intf=dummy",
                    "--no-plugins-cache",
                    "--no-media-library",
                    "--no-osd",
                    "--no-sout-rtp-sap",
                    "--no-sout-standard-sap",
                    "--no-sout-all",
                    "--no-interact",
                    "--no-keyboard-events",
                    "--no-mouse-events"
                };
                LogToFile($"[VlcAudioService] 🔧 LibVLC Options: {string.Join(" ", options)}");
                
                _libVLC = new LibVLC(options);
                LogToFile("[VlcAudioService] ✅ LibVLC instance created successfully");
                
                _mediaPlayer = new MediaPlayer(_libVLC);
                LogToFile("[VlcAudioService] ✅ MediaPlayer instance created successfully");

                // Set up event handlers with null checks
                LogToFile("[VlcAudioService] 🔗 Setting up event handlers...");
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
                    _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
                    _mediaPlayer.Playing += MediaPlayer_Playing;
                    _mediaPlayer.Paused += MediaPlayer_Paused;
                    _mediaPlayer.Stopped += MediaPlayer_Stopped;
                    _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
                }
                LogToFile("[VlcAudioService] ✅ Event handlers registered successfully");
                
                _isInitialized = true;
                Debug.WriteLine("[VlcAudioService] ✅ Hybrid VLC service initialized successfully");
            }
            catch (Exception ex)
            {
                LogToFile($"[VlcAudioService] ❌ Initialization failed: {ex.Message}");
                LogToFile($"[VlcAudioService] ❌ Stack trace: {ex.StackTrace}");
                Debug.WriteLine($"[VlcAudioService] ❌ Initialization failed: {ex.Message}");
                throw;
            }
        }

        public bool Initialize()
        {
            if (_isInitialized) return true;
            // Already initialized in constructor
            return _isInitialized;
        }

        public void Play(string path)
        {
            try
            {
                LogToFile($"[VlcAudioService] 🎵 PLAY METHOD CALLED");
                LogToFile($"[VlcAudioService] 📁 Path: {path}");
                LogToFile($"[VlcAudioService] 📅 Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                LogToFile($"[VlcAudioService] 🔧 Current Mode: {_currentMode}");
                LogToFile($"[VlcAudioService] 🎯 LibVLC Instance: {_libVLC != null}");
                LogToFile($"[VlcAudioService] 🎯 MediaPlayer Instance: {_mediaPlayer != null}");
                
                // Validate inputs
                if (string.IsNullOrEmpty(path))
                {
                    LogToFile("[VlcAudioService] ❌ Path is null or empty");
                    throw new ArgumentException("Path cannot be null or empty");
                }
                
                if (_libVLC == null || _mediaPlayer == null)
                {
                    LogToFile("[VlcAudioService] ❌ VLC not initialized");
                    throw new InvalidOperationException("VLC not initialized");
                }
                
                // Stop current playback safely
                LogToFile("[VlcAudioService] ⏹️ Stopping current playback...");
                try
                {
                    Stop();
                }
                catch (Exception ex)
                {
                    LogToFile($"[VlcAudioService] ⚠️ Stop failed (continuing): {ex.Message}");
                }
                
                // Create new media with error handling
                LogToFile("[VlcAudioService] 📄 Creating new Media object...");
                try
                {
                    _currentMedia?.Dispose(); // Clean up previous media
                    _currentMedia = new Media(_libVLC, path, FromType.FromPath);
                    LogToFile($"[VlcAudioService] ✅ Media created: {_currentMedia != null}");
                }
                catch (Exception ex)
                {
                    LogToFile($"[VlcAudioService] ❌ Media creation failed: {ex.Message}");
                    throw new InvalidOperationException($"Failed to create media from path: {path}", ex);
                }
                
                // Set up callbacks based on current mode
                LogToFile("[VlcAudioService] 🔗 Setting up callbacks...");
                try
                {
                    SetupCallbacks();
                    LogToFile("[VlcAudioService] ✅ Callbacks setup complete");
                }
                catch (Exception ex)
                {
                    LogToFile($"[VlcAudioService] ⚠️ Callback setup failed (continuing): {ex.Message}");
                }
                
                // Play the media with error handling
                LogToFile("[VlcAudioService] ▶️ Starting playback...");
                try
                {
                    var result = _mediaPlayer.Play(_currentMedia);
                    if (result)
                    {
                        _isPlaying = true;
                        LogToFile("[VlcAudioService] ✅ Playback started successfully");
                    }
                    else
                    {
                        LogToFile("[VlcAudioService] ❌ Play() returned false");
                        throw new InvalidOperationException("VLC Play() returned false");
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"[VlcAudioService] ❌ Play() failed: {ex.Message}");
                    LogToFile($"[VlcAudioService] ❌ Stack trace: {ex.StackTrace}");
                    throw new InvalidOperationException("VLC Play() failed", ex);
                }
                
                Debug.WriteLine($"[VlcAudioService] ✅ Playback started in {_currentMode} mode");
            }
            catch (Exception ex)
            {
                LogToFile($"[VlcAudioService] ❌ Play failed: {ex.Message}");
                LogToFile($"[VlcAudioService] ❌ Stack trace: {ex.StackTrace}");
                Debug.WriteLine($"[VlcAudioService] ❌ Play failed: {ex.Message}");
                throw;
            }
        }

        public void Pause()
        {
            _mediaPlayer.Pause();
            _isPlaying = false;
            Debug.WriteLine("[VlcAudioService] Playback paused");
        }

        public void Stop()
        {
            _mediaPlayer.Stop();
            _isPlaying = false;
            
            // Clear queues
            while (_pcmQueue.TryDequeue(out _)) { }
            while (_frameQueue.TryDequeue(out _)) { }
            
            Debug.WriteLine("[VlcAudioService] Playback stopped");
        }

        public void SetVisualizer(VlcVisualizer visualizer)
        {
            var newMode = visualizer switch
            {
                VlcVisualizer.Goom => VisualizerMode.Goom,
                VlcVisualizer.Spectrum => VisualizerMode.Spectrum,
                VlcVisualizer.Visual => VisualizerMode.Visual,
                VlcVisualizer.ProjectM => VisualizerMode.ProjectM,
                _ => VisualizerMode.Custom
            };
            
            SetVisualizerMode(newMode);
        }

        public void SetVisualizerMode(VisualizerMode mode)
        {
            if (_currentMode == mode) return;
            
            Debug.WriteLine($"[VlcAudioService] Switching to {mode} mode");
            _currentMode = mode;
            
            // If currently playing, restart with new mode
            if (_isPlaying && _currentMedia != null)
            {
                var currentPath = _currentMedia.Mrl;
                Stop();
                Play(currentPath);
            }
        }

        private void SetupCallbacks()
        {
            // Always set up audio callbacks for Custom mode
            SetupAudioCallbacks();
            
            // Set up video callbacks for VLC Native modes
            if (_currentMode != VisualizerMode.Custom)
            {
                SetupVideoCallbacks();
            }
        }

        private void SetupAudioCallbacks()
        {
            if (_audioCallbackRegistered) 
            {
                Debug.WriteLine("[VlcAudioService] ⚠️ Audio callbacks already registered, skipping");
                return;
            }
            
            try
            {
                Debug.WriteLine("[VlcAudioService] 🔧 SETTING UP AUDIO CALLBACKS...");
                Debug.WriteLine($"[VlcAudioService] 📅 Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                
                // Audio format callback with safe parameters
                Debug.WriteLine("[VlcAudioService] 🎛️ Setting audio format callback...");
                _mediaPlayer.SetAudioFormatCallback(
                    (ref IntPtr opaque, ref IntPtr format, ref uint rate, ref uint channels) =>
                    {
                        var formatStr = "S16N";
                        format = Marshal.StringToHGlobalAnsi(formatStr);
                        rate = 44100;
                        channels = 2;
                        Debug.WriteLine($"[VlcAudioService] ✅ Audio format callback: {formatStr}, {rate}Hz, {channels}ch");
                        return 4096; // Buffer size in bytes (safer than 0)
                    },
                    (IntPtr opaque) =>
                    {
                        Debug.WriteLine("[VlcAudioService] 🧹 Audio format cleanup callback");
                    }
                );
                Debug.WriteLine("[VlcAudioService] ✅ Audio format callback registered");

                // Audio data callback
                Debug.WriteLine("[VlcAudioService] 🎧 Setting audio data callback...");
                _mediaPlayer.SetAudioCallbacks(
                    (IntPtr data, IntPtr samples, uint count, long pts) =>
                    {
                        try
                        {
                            Debug.WriteLine($"[VlcAudioService] 🔥 AUDIO CALLBACK FIRED!");
                            Debug.WriteLine($"[VlcAudioService] 📊 Samples: {count}, Queue size: {_pcmQueue.Count}");
                            Debug.WriteLine($"[VlcAudioService] 📅 Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                            Debug.WriteLine($"[VlcAudioService] 🎯 Data pointer: {data}, Samples pointer: {samples}");
                            
                            // Convert PCM data to short array (16-bit stereo)
                            var pcmBuffer = new short[count * 2];
                            Debug.WriteLine($"[VlcAudioService] 📦 Creating PCM buffer: {pcmBuffer.Length} elements");
                            
                            Marshal.Copy(data, pcmBuffer, 0, pcmBuffer.Length);
                            Debug.WriteLine($"[VlcAudioService] ✅ PCM data copied successfully");
                            
                            // Queue for processing
                            _pcmQueue.Enqueue(pcmBuffer);
                            Debug.WriteLine($"[VlcAudioService] ✅ REAL AUDIO DATA QUEUED: {pcmBuffer.Length} samples");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[VlcAudioService] ❌ PCM callback error: {ex.Message}");
                            Debug.WriteLine($"[VlcAudioService] ❌ Stack trace: {ex.StackTrace}");
                        }
                    },
                    null, // pause callback
                    null, // resume callback
                    null, // flush callback
                    null  // drain callback
                );

                _audioCallbackRegistered = true;
                Debug.WriteLine("[VlcAudioService] ✅ Audio callbacks registered");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] ❌ Audio callback setup failed: {ex.Message}");
            }
        }

        private void SetupVideoCallbacks()
        {
            if (_videoCallbackRegistered) return;
            
            try
            {
                Debug.WriteLine($"[VlcAudioService] Setting up video callbacks for {_currentMode}...");
                
                // TODO: Fix video callback API signatures
                // For now, just mark as registered to avoid errors
                _videoCallbackRegistered = true;
                Debug.WriteLine("[VlcAudioService] ✅ Video callbacks registered (placeholder)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] ❌ Video callback setup failed: {ex.Message}");
            }
        }

        // Audio processing methods
        public float[] GetWaveformData()
        {
            lock (_audioLock)
            {
                // Process any queued PCM data
                ProcessPcmQueue();
                
                var data = new float[FixedWaveSize];
                Array.Copy(_waveformData, data, Math.Min(_waveformData.Length, FixedWaveSize));
                
                // Log array status
                float sum = data.Sum(f => MathF.Abs(f));
                if (sum < 0.001f)
                {
                    Debug.WriteLine("[VlcAudioService] ⚠️ GetWaveformData: No data yet - returning zeroed array");
                }
                else
                {
                    Debug.WriteLine($"[VlcAudioService] ✅ GetWaveformData: {data.Length} samples, sum={sum:F6}");
                }
                return data;
            }
        }

        public float[] GetSpectrumData()
        {
            lock (_audioLock)
            {
                // Process any queued PCM data
                ProcessPcmQueue();
                
                var data = new float[FixedFftSize];
                Array.Copy(_spectrumData, data, Math.Min(_spectrumData.Length, FixedFftSize));
                
                // Log array status
                float sum = data.Sum(f => MathF.Abs(f));
                if (sum < 0.001f)
                {
                    Debug.WriteLine("[VlcAudioService] ⚠️ GetSpectrumData: No data yet - returning zeroed array");
                }
                else
                {
                    Debug.WriteLine($"[VlcAudioService] ✅ GetSpectrumData: {data.Length} bins, sum={sum:F6}");
                }
                return data;
            }
        }

        private void ProcessPcmQueue()
        {
            int processedCount = 0;
            while (_pcmQueue.TryDequeue(out var pcmBuffer))
            {
                if (pcmBuffer != null)
                {
                    Debug.WriteLine($"[VlcAudioService] 🔄 Processing PCM buffer: {pcmBuffer.Length} samples");
                    ProcessRealAudioData(pcmBuffer);
                    processedCount++;
                }
                else
                {
                    Debug.WriteLine("[VlcAudioService] ⚠️ Skipping null PCM buffer");
                }
            }
            
            if (processedCount > 0)
            {
                Debug.WriteLine($"[VlcAudioService] ✅ Processed {processedCount} PCM buffers");
            }
        }

        private void ProcessRealAudioData(short[] pcmBuffer)
        {
            try
            {
                // Separate stereo channels
                var leftSamples = new float[pcmBuffer.Length / 2];
                var rightSamples = new float[pcmBuffer.Length / 2];
                
                for (int i = 0; i < pcmBuffer.Length / 2; i++)
                {
                    leftSamples[i] = pcmBuffer[i * 2] / 32768.0f;
                    rightSamples[i] = pcmBuffer[i * 2 + 1] / 32768.0f;
                }
                
                // Update waveform data
                UpdateWaveformData(leftSamples, rightSamples);
                
                // Update spectrum data (FFT)
                UpdateSpectrumData(leftSamples, rightSamples);
                
                // Update frequency bands
                UpdateFrequencyBands();
                
                // Update audio analysis
                UpdateAudioAnalysis(leftSamples, rightSamples);
                
                Debug.WriteLine($"[VlcAudioService] ✅ PROCESSED REAL AUDIO: RMS={_rms:F3}, Peak={_peak:F3}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] ❌ Processing error: {ex.Message}");
            }
        }

        private void UpdateWaveformData(float[] leftSamples, float[] rightSamples)
        {
            // Downsample to fit our waveform buffer
            int step = Math.Max(1, leftSamples.Length / _waveformData.Length);
            
            for (int i = 0; i < _waveformData.Length && i * step < leftSamples.Length; i++)
            {
                _waveformData[i] = (leftSamples[i * step] + rightSamples[i * step]) / 2.0f;
            }
            
            // Update channel data
            Array.Copy(_waveformData, 0, _leftChannel, 0, Math.Min(_waveformData.Length, _leftChannel.Length));
            Array.Copy(_waveformData, 0, _rightChannel, 0, Math.Min(_waveformData.Length, _rightChannel.Length));
            Array.Copy(_waveformData, 0, _centerChannel, 0, Math.Min(_waveformData.Length, _centerChannel.Length));
        }

        private void UpdateSpectrumData(float[] leftSamples, float[] rightSamples)
        {
            // Use MathNet.Numerics for proper FFT
            var complexSamples = new Complex[leftSamples.Length];
            for (int i = 0; i < leftSamples.Length; i++)
            {
                complexSamples[i] = new Complex(leftSamples[i] + rightSamples[i], 0);
            }
            
            // Perform FFT
            Fourier.Forward(complexSamples, FourierOptions.Matlab);
            
            // Convert to spectrum data
            for (int i = 0; i < _spectrumData.Length && i < complexSamples.Length / 2; i++)
            {
                _spectrumData[i] = (float)complexSamples[i].Magnitude;
            }
        }

        private void UpdateFrequencyBands()
        {
            // Calculate frequency bands based on spectrum data
            int bassEnd = _spectrumData.Length / 4;
            int midEnd = _spectrumData.Length * 3 / 4;
            
            _bass = _spectrumData.Take(bassEnd).Average();
            _mid = _spectrumData.Skip(bassEnd).Take(midEnd - bassEnd).Average();
            _treble = _spectrumData.Skip(midEnd).Average();
        }

        private void UpdateAudioAnalysis(float[] leftSamples, float[] rightSamples)
        {
            // Calculate RMS
            float sumSquares = 0.0f;
            float maxSample = 0.0f;
            
            for (int i = 0; i < leftSamples.Length; i++)
            {
                float sample = (leftSamples[i] + rightSamples[i]) / 2.0f;
                sumSquares += sample * sample;
                maxSample = Math.Max(maxSample, Math.Abs(sample));
            }
            
            _rms = (float)Math.Sqrt(sumSquares / leftSamples.Length);
            _peak = maxSample;
            _volume = _rms;
            
            // Simple beat detection
            _beat = _rms > 0.1f && _rms > _beatIntensity * 1.5f;
            _beatIntensity = _rms;
            
            // Simple BPM estimation
            _bpm = _beat ? 120.0f : 0.0f;
        }

        // Video frame methods (for VLC Native mode)
        public byte[] GetCurrentFrame()
        {
            _frameQueue.TryDequeue(out var frame);
            return frame ?? new byte[0];
        }

        // IAudioService implementation
        public void SetRate(float rate)
        {
            Debug.WriteLine($"[VlcAudioService] SetRate: {rate}");
            // Rate adjustment not implemented yet
        }

        public void SetTempo(float tempo)
        {
            Debug.WriteLine($"[VlcAudioService] SetTempo: {tempo}");
            // Tempo adjustment not implemented yet
        }

        public void SetFundamentalFrequency(float frequency)
        {
            _fundamentalFrequency = frequency;
            Debug.WriteLine($"[VlcAudioService] SetFundamentalFrequency: {frequency}Hz");
        }

        public float GetFundamentalFrequency()
        {
            return _fundamentalFrequency;
        }

        public void SetFrequencyPreset(IAudioService.FrequencyPreset preset)
        {
            _currentPreset = preset;
            switch (preset)
            {
                case IAudioService.FrequencyPreset.Standard440Hz:
                    _fundamentalFrequency = 440.0f;
                    break;
                case IAudioService.FrequencyPreset.Healing432Hz:
                    _fundamentalFrequency = 432.0f;
                    break;
                case IAudioService.FrequencyPreset.Love528Hz:
                    _fundamentalFrequency = 528.0f;
                    break;
            }
            Debug.WriteLine($"[VlcAudioService] SetFrequencyPreset: {preset} ({_fundamentalFrequency}Hz)");
        }

        public IAudioService.FrequencyPreset GetCurrentPreset()
        {
            return _currentPreset;
        }

        // Event handlers
        private void MediaPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            _time = e.Time / 1000.0f; // Convert to seconds
        }

        private void MediaPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            Debug.WriteLine($"[VlcAudioService] Media length: {e.Length}ms");
        }

        private void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            _isPlaying = true;
            Debug.WriteLine("[VlcAudioService] MediaPlayer started playing");
        }

        private void MediaPlayer_Paused(object? sender, EventArgs e)
        {
            _isPlaying = false;
            Debug.WriteLine("[VlcAudioService] MediaPlayer paused");
        }

        private void MediaPlayer_Stopped(object? sender, EventArgs e)
        {
            _isPlaying = false;
            Debug.WriteLine("[VlcAudioService] MediaPlayer stopped");
        }

        private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
        {
            Debug.WriteLine("[VlcAudioService] MediaPlayer encountered an error");
        }

        // IAudioProvider implementation
        public double GetPositionSeconds()
        {
            return _mediaPlayer?.Time / 1000.0 ?? 0.0;
        }

        public double GetLengthSeconds()
        {
            return _mediaPlayer?.Length / 1000.0 ?? 0.0;
        }

        public string GetStatus()
        {
            if (_mediaPlayer == null) return "Not initialized";
            if (_isPlaying) return "Playing";
            return "Stopped";
        }

        public bool IsReadyToPlay => _isInitialized && _mediaPlayer != null;


        public bool Open(string path)
        {
            try
            {
                Debug.WriteLine($"[VlcAudioService] Opening: {path}");
                _currentMedia = new Media(_libVLC, path, FromType.FromPath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] ❌ Open failed: {ex.Message}");
                return false;
            }
        }

        public bool Play()
        {
            try
            {
                if (_currentMedia == null) return false;
                
                SetupCallbacks();
                _mediaPlayer.Play(_currentMedia);
                _isPlaying = true;
                Debug.WriteLine("[VlcAudioService] ✅ Playback started");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] ❌ Play failed: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                // Avoid Stop() crash by pausing first (Grok's suggestion)
                if (_mediaPlayer?.IsPlaying == true)
                {
                    _mediaPlayer.Pause();
                    Debug.WriteLine("[VlcAudioService] Paused before disposal");
                }
                
                _mediaPlayer?.Dispose();
                _currentMedia?.Dispose();
                _libVLC?.Dispose();
                
                Debug.WriteLine("[VlcAudioService] ✅ Disposed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] ❌ Dispose error: {ex.Message}");
            }
        }
    }
}
