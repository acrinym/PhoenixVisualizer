using System.Diagnostics;

namespace PhoenixVisualizer.Core.Services
{
    /// <summary>
    /// VLC-based audio service that implements IAudioProvider for RenderSurface compatibility
    /// </summary>
    public class VlcAudioService : IAudioProvider
    {
        private readonly VlcAudioBus _bus;
        private bool _isInitialized;
        private bool _isDisposed;
        
        // Audio data buffers
        private float[] _spectrumData = Array.Empty<float>();
        private float[] _waveformData = Array.Empty<float>();
        
        // Playback state
        private bool _isPlaying;
        private double _position;
        private double _length;
        private string _currentFile = string.Empty;
        
        public VlcAudioService(VlcAudioBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _isInitialized = false;
        }
        
        public bool IsReadyToPlay => _isInitialized && !string.IsNullOrEmpty(_currentFile);
        
        public bool Initialize()
        {
            try
            {
                if (_isDisposed) return false;
                
                // Initialize the VLC audio bus
                var startTask = _bus.StartAsync();
                if (startTask is Task<bool> boolTask)
                {
                    _ = boolTask.Result; // Get the result if it returns bool
                }
                else
                {
                    startTask.Wait(); // Wait for completion if it's just Task
                }
                _isInitialized = true;
                
                Debug.WriteLine("[VlcAudioService] Initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] Initialization failed: {ex.Message}");
                return false;
            }
        }
        
        public bool Open(string path)
        {
            try
            {
                if (_isDisposed || !_isInitialized) return false;
                
                _currentFile = path;
                _position = 0;
                _length = 0; // VLC will provide this when file loads
                
                // Set playing state so visualizers can get data
                _isPlaying = true;
                
                Debug.WriteLine($"[VlcAudioService] Opened file: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] Failed to open file: {ex.Message}");
                return false;
            }
        }
        
        public bool Play()
        {
            try
            {
                if (_isDisposed || !IsReadyToPlay) return false;
                
                _isPlaying = true;
                Debug.WriteLine("[VlcAudioService] Playback started");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] Failed to start playback: {ex.Message}");
                return false;
            }
        }
        
        public void Pause()
        {
            try
            {
                if (_isDisposed) return;
                
                _isPlaying = false;
                Debug.WriteLine("[VlcAudioService] Playback paused");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] Failed to pause: {ex.Message}");
            }
        }
        
        public void Stop()
        {
            try
            {
                if (_isDisposed) return;
                
                _isPlaying = false;
                _position = 0;
                Debug.WriteLine("[VlcAudioService] Playback stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] Failed to stop: {ex.Message}");
            }
        }
        
        public float[] GetSpectrumData()
        {
            try
            {
                if (_isDisposed || !_isPlaying) return new float[2048]; // Return expected size
                
                // Get spectrum data from VLC audio bus
                var spectrumTask = _bus.GetSpectrumDataAsync(2);
                var spectrumResult = spectrumTask.Result; // Temporary sync bridge
                
                if (spectrumResult != null && spectrumResult.ContainsKey("channel_0"))
                {
                    var channel0 = spectrumResult["channel_0"] as float[];
                    if (channel0 != null)
                    {
                        // Resize to expected 2048 elements
                        var resizedArray = new float[2048];
                        Array.Copy(channel0, resizedArray, Math.Min(channel0.Length, 2048));
                        _spectrumData = resizedArray;
                        return _spectrumData;
                    }
                }
                
                return new float[2048]; // Return expected size
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] Failed to get spectrum data: {ex.Message}");
                return new float[2048]; // Return expected size
            }
        }
        
        public float[] GetWaveformData()
        {
            try
            {
                if (_isDisposed || !_isPlaying) return new float[2048]; // Return expected size
                
                // Get waveform data from VLC audio bus
                var waveformTask = _bus.GetWaveformDataAsync(2);
                var waveformResult = waveformTask.Result; // Temporary sync bridge
                
                if (waveformResult != null && waveformResult.ContainsKey("channel_0"))
                {
                    var channel0 = waveformResult["channel_0"] as float[];
                    if (channel0 != null)
                    {
                        // Resize to expected 2048 elements
                        var resizedArray = new float[2048];
                        Array.Copy(channel0, resizedArray, Math.Min(channel0.Length, 2048));
                        _waveformData = resizedArray;
                        return _waveformData;
                    }
                }
                
                return new float[2048]; // Return expected size
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] Failed to get waveform data: {ex.Message}");
                return new float[2048]; // Return expected size
            }
        }
        
        public double GetPositionSeconds()
        {
            return _position;
        }
        
        public double GetLengthSeconds()
        {
            return _length;
        }
        
        public string GetStatus()
        {
            if (_isDisposed) return "Disposed";
            if (!_isInitialized) return "Not Initialized";
            if (string.IsNullOrEmpty(_currentFile)) return "No File Loaded";
            if (_isPlaying) return "Playing";
            return "Stopped";
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            try
            {
                _isDisposed = true;
                _isPlaying = false;
                _currentFile = string.Empty;
                
                // Dispose the VLC audio bus
                _bus?.Dispose();
                
                Debug.WriteLine("[VlcAudioService] Disposed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VlcAudioService] Error during disposal: {ex.Message}");
            }
        }
    }
}
