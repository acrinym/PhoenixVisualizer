// Temporarily commented out to avoid circular dependency issues
// Will be restored once Core project is building cleanly

/*
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Models;
using LibVLCSharp.Shared;

namespace PhoenixVisualizer.Audio
{
    public class VlcAudioService : IAudioProvider
    {
        private LibVLC? _libVlc;
        private MediaPlayer? _mediaPlayer;
        private Media? _currentMedia;
        private bool _isInitialized = false;
        private bool _isPlaying = false;
        private float _volume = 1.0f;
        private float _playbackRate = 1.0f;
        private TimeSpan _position = TimeSpan.Zero;
        private TimeSpan _duration = TimeSpan.Zero;
        
        // Audio callbacks
        private AudioPlayCb? _playCallback;
        private AudioPauseCb? _pauseCallback;
        private AudioResumeCb? _resumeCallback;
        private AudioFlushCb? _flushCallback;
        private AudioDrainCb? _drainCallback;
        private AudioSetVolumeCb? _setVolumeCallback;
        
        // Audio processing
        private readonly object _audioLock = new object();
        private readonly Queue<AudioFrame> _audioQueue = new Queue<AudioFrame>();
        private readonly int _sampleRate = 44100;
        private readonly int _channels = 2;
        private readonly int _frameSize = 4096;
        
        // Event handlers
        public event EventHandler<AudioFeatures>? AudioFeaturesUpdated;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<TimeSpan>? DurationChanged;
        public event EventHandler<bool>? PlaybackStateChanged;
        public event EventHandler<float>? VolumeChanged;
        public event EventHandler<float>? PlaybackRateChanged;
        
        // Properties
        public bool IsInitialized => _isInitialized;
        public bool IsPlaying => _isPlaying;
        public float Volume 
        { 
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = Math.Clamp(value, 0.0f, 1.0f);
                    if (_mediaPlayer != null)
                    {
                        _mediaPlayer.Volume = (int)(_volume * 100);
                    }
                    VolumeChanged?.Invoke(this, _volume);
                }
            }
        }
        
        public float PlaybackRate
        {
            get => _playbackRate;
            set
            {
                if (_playbackRate != value)
                {
                    _playbackRate = Math.Clamp(value, 0.25f, 4.0f);
                    if (_mediaPlayer != null)
                    {
                        _mediaPlayer.Rate = _playbackRate;
                    }
                    PlaybackRateChanged?.Invoke(this, _playbackRate);
                }
            }
        }
        
        public TimeSpan Position
        {
            get => _position;
            set
            {
                if (_mediaPlayer != null && _mediaPlayer.IsSeekable)
                {
                    _mediaPlayer.Time = (long)value.TotalMilliseconds;
                }
            }
        }
        
        public TimeSpan Duration => _duration;
        
        // Initialization
        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (_isInitialized) return true;
                
                // Initialize LibVLC
                Core.Initialize();
                
                // Create LibVLC instance
                _libVlc = new LibVLC();
                
                // Create media player
                _mediaPlayer = new MediaPlayer(_libVlc);
                
                // Set up event handlers
                _mediaPlayer.TimeChanged += OnTimeChanged;
                _mediaPlayer.LengthChanged += OnLengthChanged;
                _mediaPlayer.Playing += OnPlaying;
                _mediaPlayer.Paused += OnPaused;
                _mediaPlayer.Stopped += OnStopped;
                _mediaPlayer.EndReached += OnEndReached;
                
                // Set up audio callbacks
                SetupAudioCallbacks();
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcAudioService] Initialization failed: {ex.Message}");
                return false;
            }
        }
        
        // Audio callback setup
        private void SetupAudioCallbacks()
        {
            if (_mediaPlayer == null) return;
            
            // Set up audio callbacks for real-time audio processing
            _playCallback = new AudioPlayCb(OnAudioPlay);
            _pauseCallback = new AudioPauseCb(OnAudioPause);
            _resumeCallback = new AudioResumeCb(OnAudioResume);
            _flushCallback = new AudioFlushCb(OnAudioFlush);
            _drainCallback = new AudioDrainCb(OnAudioDrain);
            _setVolumeCallback = new AudioSetVolumeCb(OnAudioSetVolume);
            
            // Register callbacks with media player
            // Note: This is a simplified implementation - actual LibVLCSharp audio callbacks
            // would require more complex setup and may not be directly supported
        }
        
        // Audio callback implementations
        private int OnAudioPlay(IntPtr opaque, IntPtr samples, uint count, long pts)
        {
            // Process incoming audio samples
            ProcessAudioSamples(samples, count);
            return 0;
        }
        
        private void OnAudioPause(IntPtr opaque, long pts)
        {
            // Handle audio pause
        }
        
        private void OnAudioResume(IntPtr opaque, long pts)
        {
            // Handle audio resume
        }
        
        private void OnAudioFlush(IntPtr opaque, long pts)
        {
            // Clear audio buffer
            lock (_audioLock)
            {
                _audioQueue.Clear();
            }
        }
        
        private void OnAudioDrain(IntPtr opaque)
        {
            // Wait for audio buffer to empty
        }
        
        private void OnAudioSetVolume(IntPtr opaque, float volume, bool muted)
        {
            // Handle volume changes
            Volume = muted ? 0.0f : volume;
        }
        
        // Audio processing
        private void ProcessAudioSamples(IntPtr samples, uint count)
        {
            // Convert audio samples to AudioFrame objects
            // This is a simplified implementation
            var frame = new AudioFrame
            {
                Samples = new float[count],
                SampleRate = _sampleRate,
                Channels = _channels,
                Timestamp = DateTime.UtcNow
            };
            
            // Copy samples (simplified - actual implementation would need proper marshaling)
            // Marshal.Copy(samples, frame.Samples, 0, (int)count);
            
            // Add to processing queue
            lock (_audioLock)
            {
                _audioQueue.Enqueue(frame);
                
                // Limit queue size
                while (_audioQueue.Count > 100)
                {
                    _audioQueue.Dequeue();
                }
            }
        }
        
        // Media playback
        public async Task<bool> LoadMediaAsync(string mediaPath)
        {
            try
            {
                if (!_isInitialized || _mediaPlayer == null) return false;
                
                // Stop current playback
                await StopAsync();
                
                // Create new media
                _currentMedia = new Media(_libVlc, mediaPath);
                
                // Set media on player
                _mediaPlayer.Media = _currentMedia;
                
                // Parse media to get duration
                await _currentMedia.ParseAsync();
                _duration = TimeSpan.FromMilliseconds(_currentMedia.Duration);
                
                DurationChanged?.Invoke(this, _duration);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcAudioService] Load media failed: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> PlayAsync()
        {
            try
            {
                if (!_isInitialized || _mediaPlayer == null || _currentMedia == null) return false;
                
                var result = _mediaPlayer.Play();
                if (result == 0)
                {
                    _isPlaying = true;
                    PlaybackStateChanged?.Invoke(this, true);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcAudioService] Play failed: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> PauseAsync()
        {
            try
            {
                if (!_isInitialized || _mediaPlayer == null) return false;
                
                _mediaPlayer.Pause();
                _isPlaying = false;
                PlaybackStateChanged?.Invoke(this, false);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcAudioService] Pause failed: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> StopAsync()
        {
            try
            {
                if (!_isInitialized || _mediaPlayer == null) return false;
                
                _mediaPlayer.Stop();
                _isPlaying = false;
                _position = TimeSpan.Zero;
                
                PlaybackStateChanged?.Invoke(this, false);
                PositionChanged?.Invoke(this, _position);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcAudioService] Stop failed: {ex.Message}");
                return false;
            }
        }
        
        // Audio features extraction
        public async Task<AudioFeatures> GetAudioFeaturesAsync()
        {
            try
            {
                var features = new AudioFeatures();
                
                // Get current audio frame if available
                AudioFrame? currentFrame = null;
                lock (_audioLock)
                {
                    if (_audioQueue.Count > 0)
                    {
                        currentFrame = _audioQueue.Dequeue();
                    }
                }
                
                if (currentFrame != null)
                {
                    // Calculate basic audio features
                    features.RMS = CalculateRMS(currentFrame.Samples);
                    features.Peak = CalculatePeak(currentFrame.Samples);
                    features.ZeroCrossings = CalculateZeroCrossings(currentFrame.Samples);
                    
                    // Simple beat detection (simplified)
                    features.Beat = DetectBeat(features.RMS);
                    
                    // Frequency analysis (simplified)
                    features.Fft = CalculateFFT(currentFrame.Samples);
                    
                    // Waveform data
                    features.Waveform = currentFrame.Samples;
                    
                    // Bass detection (simplified)
                    features.Bass = CalculateBass(features.Fft);
                    
                    // Mid detection (simplified)
                    features.Mid = CalculateMid(features.Fft);
                    
                    // Treble detection (simplified)
                    features.Treble = CalculateTreble(features.Fft);
                }
                
                // Update position
                if (_mediaPlayer != null)
                {
                    features.Position = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                    features.Duration = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
                }
                
                // Update playback state
                features.IsPlaying = _isPlaying;
                features.Volume = _volume;
                features.PlaybackRate = _playbackRate;
                
                return features;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcAudioService] Get audio features failed: {ex.Message}");
                return new AudioFeatures();
            }
        }
        
        // Audio analysis methods
        private float CalculateRMS(float[] samples)
        {
            if (samples == null || samples.Length == 0) return 0.0f;
            
            float sum = 0.0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }
            
            return (float)Math.Sqrt(sum / samples.Length);
        }
        
        private float CalculatePeak(float[] samples)
        {
            if (samples == null || samples.Length == 0) return 0.0f;
            
            float peak = 0.0f;
            for (int i = 0; i < samples.Length; i++)
            {
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            
            return peak;
        }
        
        private int CalculateZeroCrossings(float[] samples)
        {
            if (samples == null || samples.Length < 2) return 0;
            
            int crossings = 0;
            for (int i = 1; i < samples.Length; i++)
            {
                if ((samples[i - 1] >= 0 && samples[i] < 0) || 
                    (samples[i - 1] < 0 && samples[i] >= 0))
                {
                    crossings++;
                }
            }
            
            return crossings;
        }
        
        private bool DetectBeat(float rms)
        {
            // Simple threshold-based beat detection
            // In a real implementation, this would use more sophisticated algorithms
            static float threshold = 0.1f;
            static float decay = 0.95f;
            
            bool beat = rms > threshold;
            if (beat)
            {
                threshold = Math.Max(threshold, rms * 1.1f);
            }
            else
            {
                threshold *= decay;
            }
            
            return beat;
        }
        
        private float[] CalculateFFT(float[] samples)
        {
            // Simplified FFT calculation
            // In a real implementation, this would use a proper FFT library
            var fft = new float[samples.Length];
            Array.Copy(samples, fft, samples.Length);
            
            // Simple frequency domain processing (placeholder)
            for (int i = 0; i < fft.Length; i++)
            {
                fft[i] = (float)Math.Sin(i * 0.1f) * samples[i];
            }
            
            return fft;
        }
        
        private float CalculateBass(float[] fft)
        {
            if (fft == null || fft.Length == 0) return 0.0f;
            
            // Calculate bass energy (low frequencies)
            int bassBins = Math.Min(fft.Length / 8, 10);
            float bassEnergy = 0.0f;
            
            for (int i = 0; i < bassBins; i++)
            {
                bassEnergy += fft[i] * fft[i];
            }
            
            return (float)Math.Sqrt(bassEnergy / bassBins);
        }
        
        private float CalculateMid(float[] fft)
        {
            if (fft == null || fft.Length == 0) return 0.0f;
            
            // Calculate mid energy (mid frequencies)
            int startBin = fft.Length / 8;
            int endBin = fft.Length * 3 / 8;
            float midEnergy = 0.0f;
            
            for (int i = startBin; i < endBin; i++)
            {
                midEnergy += fft[i] * fft[i];
            }
            
            return (float)Math.Sqrt(midEnergy / (endBin - startBin));
        }
        
        private float CalculateTreble(float[] fft)
        {
            if (fft == null || fft.Length == 0) return 0.0f;
            
            // Calculate treble energy (high frequencies)
            int startBin = fft.Length * 3 / 8;
            float trebleEnergy = 0.0f;
            
            for (int i = startBin; i < fft.Length; i++)
            {
                trebleEnergy += fft[i] * fft[i];
            }
            
            return (float)Math.Sqrt(trebleEnergy / (fft.Length - startBin));
        }
        
        // Event handlers
        private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            _position = TimeSpan.FromMilliseconds(e.Time);
            PositionChanged?.Invoke(this, _position);
        }
        
        private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            _duration = TimeSpan.FromMilliseconds(e.Length);
            DurationChanged?.Invoke(this, _duration);
        }
        
        private void OnPlaying(object? sender, EventArgs e)
        {
            _isPlaying = true;
            PlaybackStateChanged?.Invoke(this, true);
        }
        
        private void OnPaused(object? sender, EventArgs e)
        {
            _isPlaying = false;
            PlaybackStateChanged?.Invoke(this, false);
        }
        
        private void OnStopped(object? sender, EventArgs e)
        {
            _isPlaying = false;
            _position = TimeSpan.Zero;
            PlaybackStateChanged?.Invoke(this, false);
            PositionChanged?.Invoke(this, _position);
        }
        
        private void OnEndReached(object? sender, EventArgs e)
        {
            _isPlaying = false;
            _position = _duration;
            PlaybackStateChanged?.Invoke(this, false);
            PositionChanged?.Invoke(this, _position);
        }
        
        // Cleanup
        public void Dispose()
        {
            try
            {
                _mediaPlayer?.Stop();
                _mediaPlayer?.Dispose();
                _currentMedia?.Dispose();
                _libVlc?.Dispose();
                
                _mediaPlayer = null;
                _currentMedia = null;
                _libVlc = null;
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VlcAudioService] Dispose failed: {ex.Message}");
            }
        }
    }
}
*/
