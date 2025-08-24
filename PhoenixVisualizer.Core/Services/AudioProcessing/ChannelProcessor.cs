using System;
using System.Threading.Tasks;

namespace PhoenixVisualizer.Core.Services.AudioProcessing
{
    /// <summary>
    /// Processes audio channels for AVS compatibility
    /// Handles channel separation, downsampling, and data normalization
    /// </summary>
    public class ChannelProcessor : IDisposable
    {
        private readonly int _targetSampleRate;
        private readonly int _targetBufferSize;
        private readonly float[] _resampleBuffer;
        private readonly float[] _channelBuffer;
        
        private bool _isDisposed;
        
        /// <summary>
        /// Gets the target sample rate for output
        /// </summary>
        public int TargetSampleRate => _targetSampleRate;
        
        /// <summary>
        /// Gets the target buffer size for output
        /// </summary>
        public int TargetBufferSize => _targetBufferSize;
        
        /// <summary>
        /// Initializes a new channel processor
        /// </summary>
        /// <param name="targetSampleRate">Target sample rate (default 44100)</param>
        /// <param name="targetBufferSize">Target buffer size (default 576 for AVS compatibility)</param>
        public ChannelProcessor(int targetSampleRate = 44100, int targetBufferSize = 576)
        {
            _targetSampleRate = targetSampleRate;
            _targetBufferSize = targetBufferSize;
            _resampleBuffer = new float[targetBufferSize * 2]; // 2x for oversampling
            _channelBuffer = new float[targetBufferSize];
        }
        
        /// <summary>
        /// Separates interleaved stereo audio into left and right channels
        /// </summary>
        /// <param name="interleavedAudio">Interleaved stereo audio data</param>
        /// <returns>Tuple of (leftChannel, rightChannel)</returns>
        public (float[] leftChannel, float[] rightChannel) SeparateChannels(float[] interleavedAudio)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ChannelProcessor));
            
            if (interleavedAudio == null || interleavedAudio.Length == 0)
                return (new float[_targetBufferSize], new float[_targetBufferSize]);
            
            int sampleCount = interleavedAudio.Length / 2;
            var leftChannel = new float[sampleCount];
            var rightChannel = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                leftChannel[i] = interleavedAudio[i * 2];
                rightChannel[i] = interleavedAudio[i * 2 + 1];
            }
            
            return (leftChannel, rightChannel);
        }
        
        /// <summary>
        /// Downsamples audio data to target buffer size using linear interpolation
        /// </summary>
        /// <param name="audioData">Input audio data</param>
        /// <param name="targetSize">Target buffer size</param>
        /// <returns>Downsampled audio data</returns>
        public float[] Downsample(float[] audioData, int targetSize)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ChannelProcessor));
            
            if (audioData == null || audioData.Length == 0)
                return new float[targetSize];
            
            if (audioData.Length == targetSize)
                return (float[])audioData.Clone();
            
            var downsampled = new float[targetSize];
            
            if (audioData.Length > targetSize)
            {
                // Downsampling: average multiple samples
                float ratio = (float)audioData.Length / targetSize;
                
                for (int i = 0; i < targetSize; i++)
                {
                    float startIndex = i * ratio;
                    float endIndex = (i + 1) * ratio;
                    
                    int startSample = (int)startIndex;
                    int endSample = Math.Min((int)endIndex, audioData.Length - 1);
                    
                    float sum = 0.0f;
                    int count = 0;
                    
                    for (int j = startSample; j <= endSample; j++)
                    {
                        sum += audioData[j];
                        count++;
                    }
                    
                    downsampled[i] = count > 0 ? sum / count : 0.0f;
                }
            }
            else
            {
                // Upsampling: linear interpolation
                float ratio = (float)audioData.Length / targetSize;
                
                for (int i = 0; i < targetSize; i++)
                {
                    float sampleIndex = i * ratio;
                    int leftIndex = (int)sampleIndex;
                    int rightIndex = Math.Min(leftIndex + 1, audioData.Length - 1);
                    
                    float fraction = sampleIndex - leftIndex;
                    
                    if (leftIndex == rightIndex)
                    {
                        downsampled[i] = audioData[leftIndex];
                    }
                    else
                    {
                        downsampled[i] = audioData[leftIndex] * (1.0f - fraction) + 
                                        audioData[rightIndex] * fraction;
                    }
                }
            }
            
            return downsampled;
        }
        
        /// <summary>
        /// Downsamples audio data to the target buffer size
        /// </summary>
        /// <param name="audioData">Input audio data</param>
        /// <returns>Downsampled audio data</returns>
        public float[] Downsample(float[] audioData)
        {
            return Downsample(audioData, _targetBufferSize);
        }
        
        /// <summary>
        /// Creates a center channel by averaging left and right channels
        /// </summary>
        /// <param name="leftChannel">Left channel audio data</param>
        /// <param name="rightChannel">Right channel audio data</param>
        /// <returns>Center channel audio data</returns>
        public float[] CreateCenterChannel(float[] leftChannel, float[] rightChannel)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ChannelProcessor));
            
            if (leftChannel == null || rightChannel == null)
                return new float[_targetBufferSize];
            
            int sampleCount = Math.Min(leftChannel.Length, rightChannel.Length);
            var centerChannel = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                centerChannel[i] = (leftChannel[i] + rightChannel[i]) * 0.5f;
            }
            
            return centerChannel;
        }
        
        /// <summary>
        /// Normalizes audio data to a target range
        /// </summary>
        /// <param name="audioData">Input audio data</param>
        /// <param name="targetRange">Target range (default 1.0)</param>
        /// <returns>Normalized audio data</returns>
        public float[] Normalize(float[] audioData, float targetRange = 1.0f)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ChannelProcessor));
            
            if (audioData == null || audioData.Length == 0)
                return new float[0];
            
            // Find maximum absolute value
            float maxValue = 0.0f;
            for (int i = 0; i < audioData.Length; i++)
            {
                maxValue = Math.Max(maxValue, Math.Abs(audioData[i]));
            }
            
            if (maxValue == 0.0f)
                return (float[])audioData.Clone();
            
            // Normalize to target range
            float scaleFactor = targetRange / maxValue;
            var normalized = new float[audioData.Length];
            
            for (int i = 0; i < audioData.Length; i++)
            {
                normalized[i] = audioData[i] * scaleFactor;
            }
            
            return normalized;
        }
        
        /// <summary>
        /// Applies a low-pass filter to smooth audio data
        /// </summary>
        /// <param name="audioData">Input audio data</param>
        /// <param name="filterStrength">Filter strength (0.0 to 1.0, default 0.1)</param>
        /// <returns>Filtered audio data</returns>
        public float[] ApplyLowPassFilter(float[] audioData, float filterStrength = 0.1f)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ChannelProcessor));
            
            if (audioData == null || audioData.Length == 0)
                return new float[0];
            
            var filtered = new float[audioData.Length];
            filtered[0] = audioData[0];
            
            for (int i = 1; i < audioData.Length; i++)
            {
                filtered[i] = filtered[i - 1] * (1.0f - filterStrength) + 
                              audioData[i] * filterStrength;
            }
            
            return filtered;
        }
        
        /// <summary>
        /// Converts audio data to AVS-compatible format
        /// </summary>
        /// <param name="leftChannel">Left channel audio data</param>
        /// <param name="rightChannel">Right channel audio data</param>
        /// <returns>AVS-compatible audio data structure</returns>
        public AvsAudioData ConvertToAvsFormat(float[] leftChannel, float[] rightChannel)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ChannelProcessor));
            
            // Downsample to target size
            var leftAvs = Downsample(leftChannel ?? new float[0]);
            var rightAvs = Downsample(rightChannel ?? new float[0]);
            
            // Create center channel
            var centerAvs = CreateCenterChannel(leftAvs, rightAvs);
            
            // Normalize channels
            var normalizedLeft = Normalize(leftAvs);
            var normalizedRight = Normalize(rightAvs);
            var normalizedCenter = Normalize(centerAvs);
            
            return new AvsAudioData
            {
                LeftChannel = normalizedLeft,
                RightChannel = normalizedRight,
                CenterChannel = normalizedCenter,
                BufferSize = _targetBufferSize,
                SampleRate = _targetSampleRate,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                // No unmanaged resources to dispose
            }
        }
    }
    
    /// <summary>
    /// AVS-compatible audio data structure
    /// </summary>
    public class AvsAudioData
    {
        /// <summary>
        /// Left channel audio data
        /// </summary>
        public float[] LeftChannel { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// Right channel audio data
        /// </summary>
        public float[] RightChannel { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// Center channel audio data (average of left and right)
        /// </summary>
        public float[] CenterChannel { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// Buffer size in samples
        /// </summary>
        public int BufferSize { get; set; }
        
        /// <summary>
        /// Sample rate in Hz
        /// </summary>
        public int SampleRate { get; set; }
        
        /// <summary>
        /// Timestamp when the data was processed
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets the channel data for a specific channel index
        /// </summary>
        /// <param name="channelIndex">Channel index (0=left, 1=right, 2=center)</param>
        /// <returns>Audio data for the specified channel</returns>
        public float[] GetChannel(int channelIndex)
        {
            return channelIndex switch
            {
                0 => LeftChannel,
                1 => RightChannel,
                2 => CenterChannel,
                _ => LeftChannel // Default to left channel
            };
        }
        
        /// <summary>
        /// Gets the number of available channels
        /// </summary>
        public int ChannelCount => 3; // Left, Right, Center
    }
}
