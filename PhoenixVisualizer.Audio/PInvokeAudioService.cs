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
    /// <summary>
    /// Audio service using direct P/Invoke to VLC DLLs to bypass LibVLCSharp compatibility issues
    /// </summary>
    public class PInvokeAudioService : IAudioService, IAudioProvider, IDisposable
    {
        private static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PhoenixVisualizer_PInvoke_Debug.log");
        
        private static void LogToFile(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFile, logEntry);
                Debug.WriteLine(message);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        // Fixed array sizes to match RenderSurface expectations
        private const int FixedFftSize = 512;
        private const int FixedWaveSize = 1024;
        
        // Audio data capture
        private readonly ConcurrentQueue<short[]> _pcmQueue = new();
        
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
        
        // State
        private bool _isPlaying = false;
        private bool _isInitialized = false;
        private string _currentFilePath = "";
        
        // Frequency retuning
        private float _fundamentalFrequency = 440.0f;
        private IAudioService.FrequencyPreset _currentPreset = IAudioService.FrequencyPreset.Standard440Hz;

        public bool IsPlaying => _isPlaying;
        public VlcVisualizer GetCurrentVisualizer() => VlcVisualizer.Goom; // Default for P/Invoke

        public PInvokeAudioService()
        {
            try
            {
                LogToFile("[PInvokeAudioService] 🚀 INITIALIZING P/INVOKE AUDIO SERVICE...");
                LogToFile($"[PInvokeAudioService] 📅 Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

                // Test P/Invoke initialization with more defensive approach
                LogToFile("[PInvokeAudioService] 🔧 Testing VLC DLL availability...");
                
                // Try to initialize VLC using P/Invoke
                LogToFile("[PInvokeAudioService] 🎯 Calling VlcPInvoke.InitializeVlc()...");
                bool initResult = VlcPInvoke.InitializeVlc();
                LogToFile($"[PInvokeAudioService] 📊 InitializeVlc() returned: {initResult}");
                
                if (initResult)
                {
                    _isInitialized = true;
                    LogToFile("[PInvokeAudioService] ✅ VLC P/Invoke initialized successfully");
                    Debug.WriteLine("[PInvokeAudioService] ✅ P/Invoke audio service initialized successfully");
                }
                else
                {
                    LogToFile("[PInvokeAudioService] ❌ VLC P/Invoke initialization failed");
                    // Don't throw exception - just mark as not initialized
                    _isInitialized = false;
                    LogToFile("[PInvokeAudioService] ⚠️ Continuing without VLC initialization");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"[PInvokeAudioService] ❌ Initialization failed: {ex.Message}");
                LogToFile($"[PInvokeAudioService] ❌ Stack trace: {ex.StackTrace}");
                Debug.WriteLine($"[PInvokeAudioService] ❌ Initialization failed: {ex.Message}");
                
                // Don't throw exception - just mark as not initialized
                _isInitialized = false;
                LogToFile("[PInvokeAudioService] ⚠️ Continuing without VLC initialization due to error");
            }
        }

        public bool Initialize()
        {
            return _isInitialized;
        }

        public void Play(string path)
        {
            try
            {
                LogToFile($"[PInvokeAudioService] 🎵 PLAY METHOD CALLED");
                LogToFile($"[PInvokeAudioService] 📁 Path: {path}");
                LogToFile($"[PInvokeAudioService] 📅 Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                
                // Validate inputs
                if (string.IsNullOrEmpty(path))
                {
                    LogToFile("[PInvokeAudioService] ❌ Path is null or empty");
                    throw new ArgumentException("Path cannot be null or empty");
                }
                
                if (!_isInitialized)
                {
                    LogToFile("[PInvokeAudioService] ❌ Service not initialized");
                    throw new InvalidOperationException("Service not initialized");
                }
                
                // Stop current playback
                LogToFile("[PInvokeAudioService] ⏹️ Stopping current playback...");
                Stop();
                
                // Store current file path
                _currentFilePath = path;
                
                // Play using P/Invoke
                LogToFile("[PInvokeAudioService] ▶️ Starting playback via P/Invoke...");
                if (VlcPInvoke.PlayAudio(path))
                {
                    _isPlaying = true;
                    LogToFile("[PInvokeAudioService] ✅ Playback started successfully via P/Invoke");
                    
                    // Start monitoring thread
                    Task.Run(MonitorPlayback);
                }
                else
                {
                    LogToFile("[PInvokeAudioService] ❌ P/Invoke Play() returned false");
                    throw new InvalidOperationException("VLC P/Invoke Play() failed");
                }
                
                Debug.WriteLine($"[PInvokeAudioService] ✅ Playback started via P/Invoke");
            }
            catch (Exception ex)
            {
                LogToFile($"[PInvokeAudioService] ❌ Play failed: {ex.Message}");
                LogToFile($"[PInvokeAudioService] ❌ Stack trace: {ex.StackTrace}");
                Debug.WriteLine($"[PInvokeAudioService] ❌ Play failed: {ex.Message}");
                throw;
            }
        }

        private async Task MonitorPlayback()
        {
            try
            {
                while (_isPlaying && VlcPInvoke.IsPlaying())
                {
                    // Update time
                    _time = VlcPInvoke.GetCurrentTime() / 1000.0f;
                    
                    // Generate some mock audio data for visualization
                    GenerateMockAudioData();
                    
                    await Task.Delay(50); // 20 FPS
                }
                
                if (_isPlaying)
                {
                    _isPlaying = false;
                    LogToFile("[PInvokeAudioService] 🏁 Playback ended");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"[PInvokeAudioService] ❌ Monitor thread error: {ex.Message}");
                _isPlaying = false;
            }
        }

        private void GenerateMockAudioData()
        {
            // Generate some mock audio data for visualization
            // This is a placeholder - in a real implementation, you'd capture actual audio
            lock (_audioLock)
            {
                var random = new Random();
                
                // Generate mock waveform
                for (int i = 0; i < _waveformData.Length; i++)
                {
                    _waveformData[i] = (float)(Math.Sin(_time * 10 + i * 0.1) * 0.5 + random.NextDouble() * 0.1);
                }
                
                // Generate mock spectrum
                for (int i = 0; i < _spectrumData.Length; i++)
                {
                    _spectrumData[i] = (float)(Math.Sin(_time * 5 + i * 0.2) * 0.3 + random.NextDouble() * 0.05);
                }
                
                // Update frequency bands
                _bass = _spectrumData.Take(_spectrumData.Length / 4).Average();
                _mid = _spectrumData.Skip(_spectrumData.Length / 4).Take(_spectrumData.Length / 2).Average();
                _treble = _spectrumData.Skip(_spectrumData.Length * 3 / 4).Average();
                
                // Update audio analysis
                _rms = (float)Math.Sqrt(_waveformData.Sum(f => f * f) / _waveformData.Length);
                _peak = _waveformData.Max(Math.Abs);
                _volume = _rms;
                
                // Simple beat detection
                _beat = _rms > 0.1f && _rms > _beatIntensity * 1.5f;
                _beatIntensity = _rms;
                _bpm = _beat ? 120.0f : 0.0f;
            }
        }

        public void Pause()
        {
            try
            {
                VlcPInvoke.Pause();
                _isPlaying = false;
                LogToFile("[PInvokeAudioService] ⏸️ Playback paused");
            }
            catch (Exception ex)
            {
                LogToFile($"[PInvokeAudioService] ❌ Pause failed: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                VlcPInvoke.Stop();
                _isPlaying = false;
                
                // Clear queues
                while (_pcmQueue.TryDequeue(out _)) { }
                
                LogToFile("[PInvokeAudioService] ⏹️ Playback stopped");
            }
            catch (Exception ex)
            {
                LogToFile($"[PInvokeAudioService] ❌ Stop failed: {ex.Message}");
            }
        }

        public void SetVisualizer(VlcVisualizer visualizer)
        {
            // P/Invoke version doesn't support visualizer switching yet
            LogToFile($"[PInvokeAudioService] SetVisualizer: {visualizer} (not implemented)");
        }

        public void SetVisualizerMode(VisualizerMode mode)
        {
            // P/Invoke version doesn't support mode switching yet
            LogToFile($"[PInvokeAudioService] SetVisualizerMode: {mode} (not implemented)");
        }

        // Audio processing methods
        public float[] GetWaveformData()
        {
            lock (_audioLock)
            {
                var data = new float[FixedWaveSize];
                Array.Copy(_waveformData, data, Math.Min(_waveformData.Length, FixedWaveSize));
                return data;
            }
        }

        public float[] GetSpectrumData()
        {
            lock (_audioLock)
            {
                var data = new float[FixedFftSize];
                Array.Copy(_spectrumData, data, Math.Min(_spectrumData.Length, FixedFftSize));
                return data;
            }
        }

        // IAudioService implementation
        public void SetRate(float rate)
        {
            LogToFile($"[PInvokeAudioService] SetRate: {rate} (not implemented)");
        }

        public void SetTempo(float tempo)
        {
            LogToFile($"[PInvokeAudioService] SetTempo: {tempo} (not implemented)");
        }

        public void SetFundamentalFrequency(float frequency)
        {
            _fundamentalFrequency = frequency;
            LogToFile($"[PInvokeAudioService] SetFundamentalFrequency: {frequency}Hz");
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
            LogToFile($"[PInvokeAudioService] SetFrequencyPreset: {preset} ({_fundamentalFrequency}Hz)");
        }

        public IAudioService.FrequencyPreset GetCurrentPreset()
        {
            return _currentPreset;
        }

        // IAudioProvider implementation
        public double GetPositionSeconds()
        {
            return VlcPInvoke.GetCurrentTime() / 1000.0;
        }

        public double GetLengthSeconds()
        {
            return VlcPInvoke.GetLength() / 1000.0;
        }

        public string GetStatus()
        {
            if (!_isInitialized) return "Not initialized";
            if (_isPlaying) return "Playing";
            return "Stopped";
        }

        public bool IsReadyToPlay => _isInitialized;

        public bool Open(string path)
        {
            _currentFilePath = path;
            return true;
        }

        public bool Play()
        {
            if (string.IsNullOrEmpty(_currentFilePath)) return false;
            Play(_currentFilePath);
            return _isPlaying;
        }

        public void Dispose()
        {
            try
            {
                Stop();
                VlcPInvoke.Cleanup();
                LogToFile("[PInvokeAudioService] ✅ Disposed successfully");
            }
            catch (Exception ex)
            {
                LogToFile($"[PInvokeAudioService] ❌ Dispose error: {ex.Message}");
            }
        }
    }
}
