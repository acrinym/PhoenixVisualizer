using System;
using System.Threading.Tasks;
using SkiaSharp;
using PhoenixVisualizer.Core.Services;

namespace PhoenixVisualizer.NativeAudio
{
    /// <summary>
    /// Complete native audio visualizer service - no VLC dependencies
    /// Combines LAME audio decoding, DirectSound output, and VLC visualizers
    /// </summary>
    public class NativeAudioVisualizerService : IAudioProvider
    {
        private readonly NativeAudioService _audioService;
        private readonly VlcVisualizerManager _visualizerManager;
        private bool _isRunning = false;
        private DateTime _lastUpdate = DateTime.Now;
        private uint[]? _currentFrame = null;
        private double _time = 0.0;
        
        public NativeAudioVisualizerService(IntPtr windowHandle)
        {
            _audioService = new NativeAudioService(windowHandle);
            _visualizerManager = new VlcVisualizerManager();
            
            // Start update loop
            StartUpdateLoop();
        }
        
        private void StartUpdateLoop()
        {
            _isRunning = true;
            
            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        await UpdateVisualizer();
                        await Task.Delay(16); // ~60 FPS
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NativeAudioVisualizerService] Update error: {ex.Message}");
                    }
                }
            });
        }
        
        private Task UpdateVisualizer()
        {
            if (!_audioService.IsPlaying)
                return Task.CompletedTask;
                
            try
            {
                // Get raw audio data for visualizers
                var audioData = _audioService.GetWaveformData();
                if (audioData != null && audioData.Length > 0)
                {
                    // Convert float[] to short[] for visualizers
                    var shortData = new short[audioData.Length];
                    for (int i = 0; i < audioData.Length; i++)
                    {
                        shortData[i] = (short)(audioData[i] * short.MaxValue);
                    }
                    
                    // Update visualizer with raw audio data
                    _currentFrame = _visualizerManager.UpdateVisualizer(shortData, 2, (float)_time);
                }
                
                // Update time
                _time += 0.016; // ~60 FPS
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NativeAudioVisualizerService] Update error: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Render current visualizer to SkiaSharp canvas
        /// </summary>
        public void RenderVisualizer(SKCanvas canvas, int width, int height)
        {
            try
            {
                _visualizerManager.RenderVisualizer(canvas, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NativeAudioVisualizerService] Render error: {ex.Message}");
            }
        }
        
        // Audio service methods
        public bool Initialize() => _audioService.Initialize();
        public bool Open(string filePath) => _audioService.Open(filePath);
        public bool Play() => _audioService.Play();
        public void Pause() => _audioService.Pause();
        public void Stop() => _audioService.Stop();
        public bool IsPlaying => _audioService.IsPlaying;
        public bool IsReadyToPlay => _audioService.IsReadyToPlay;
        public double GetPositionSeconds() => _audioService.GetPositionSeconds();
        public double GetLengthSeconds() => _audioService.GetLengthSeconds();
        public string GetStatus() => _audioService.GetStatus();
        public float[] GetSpectrumData() => _audioService.GetSpectrumData();
        public float[] GetWaveformData() => _audioService.GetWaveformData();
        public AudioFeatures GetAudioFeatures() => _audioService.GetAudioFeatures();
        public void SetVolume(float volume) => _audioService.SetVolume(volume);
        public float GetVolume() => _audioService.GetVolume();
        
        // Visualizer management methods
        public string[] GetAvailableVisualizers() => _visualizerManager.GetAvailableVisualizers();
        public bool SetVisualizer(string name) => _visualizerManager.SetVisualizer(name);
        public string GetCurrentVisualizerName() => _visualizerManager.GetCurrentVisualizerName();
        
        
        public void Dispose()
        {
            _isRunning = false;
            _audioService?.Dispose();
            _visualizerManager?.Dispose();
        }
    }
}
