using System;
using System.Numerics;
using System.Threading.Tasks;

namespace PhoenixVisualizer.Core.Services.AudioProcessing
{
    /// <summary>
    /// High-performance FFT processor for real-time audio spectrum analysis
    /// Converts audio samples to frequency domain data compatible with AVS effects
    /// </summary>
    public class FftProcessor : IDisposable
    {
        private readonly int _fftSize;
        private readonly Complex[] _fftBuffer;
        private readonly float[] _windowFunction;
        private readonly float[] _magnitudeBuffer;
        private readonly float[] _phaseBuffer;
        private readonly float[] _frequencyBuffer;
        
        private bool _isDisposed;
        
        /// <summary>
        /// Gets the FFT size (number of frequency bins)
        /// </summary>
        public int FftSize => _fftSize;
        
        /// <summary>
        /// Gets the frequency resolution in Hz
        /// </summary>
        public float FrequencyResolution { get; }
        
        /// <summary>
        /// Gets the Nyquist frequency (half the sample rate)
        /// </summary>
        public float NyquistFrequency { get; }
        
        /// <summary>
        /// Initializes a new FFT processor with the specified size
        /// </summary>
        /// <param name="fftSize">FFT size (must be power of 2, default 576 for AVS compatibility)</param>
        /// <param name="sampleRate">Audio sample rate in Hz (default 44100)</param>
        public FftProcessor(int fftSize = 576, int sampleRate = 44100)
        {
            // Validate FFT size is power of 2
            if (!IsPowerOfTwo(fftSize))
            {
                throw new ArgumentException("FFT size must be a power of 2", nameof(fftSize));
            }
            
            _fftSize = fftSize;
            _fftBuffer = new Complex[_fftSize];
            _windowFunction = new float[_fftSize];
            _magnitudeBuffer = new float[_fftSize];
            _phaseBuffer = new float[_fftSize];
            _frequencyBuffer = new float[_fftSize];
            
            // Calculate frequency resolution and Nyquist frequency
            FrequencyResolution = (float)sampleRate / _fftSize;
            NyquistFrequency = (float)sampleRate / 2.0f;
            
            // Generate Hann window function for better frequency resolution
            GenerateHannWindow();
            
            // Pre-calculate frequency array for each bin
            GenerateFrequencyArray();
        }
        
        /// <summary>
        /// Processes audio data and returns spectrum information
        /// </summary>
        /// <param name="audioData">Input audio samples (will be zero-padded if shorter than FFT size)</param>
        /// <returns>Processed spectrum data with magnitudes and phases</returns>
        public async Task<SpectrumData> ProcessAsync(float[] audioData)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FftProcessor));
            
            return await Task.Run(() => ProcessSynchronous(audioData));
        }
        
        /// <summary>
        /// Processes audio data synchronously (for performance-critical applications)
        /// </summary>
        /// <param name="audioData">Input audio samples</param>
        /// <returns>Processed spectrum data</returns>
        public SpectrumData ProcessSynchronous(float[] audioData)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FftProcessor));
            
            // Prepare input buffer with windowing and zero-padding
            PrepareInputBuffer(audioData);
            
            // Perform FFT
            PerformFFT();
            
            // Calculate magnitudes and phases
            CalculateMagnitudesAndPhases();
            
            // Return spectrum data
            return new SpectrumData
            {
                Magnitudes = (float[])_magnitudeBuffer.Clone(),
                Phases = (float[])_phaseBuffer.Clone(),
                Frequencies = (float[])_frequencyBuffer.Clone(),
                FftSize = _fftSize,
                FrequencyResolution = FrequencyResolution,
                NyquistFrequency = NyquistFrequency,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Gets magnitude data for AVS compatibility (normalized 0.0 to 1.0)
        /// </summary>
        /// <param name="audioData">Input audio samples</param>
        /// <returns>Normalized magnitude array compatible with AVS effects</returns>
        public float[] GetAvsCompatibleMagnitudes(float[] audioData)
        {
            var spectrumData = ProcessSynchronous(audioData);
            var normalizedMagnitudes = new float[_fftSize];
            
            // Find maximum magnitude for normalization
            float maxMagnitude = 0.0f;
            for (int i = 0; i < _fftSize; i++)
            {
                maxMagnitude = Math.Max(maxMagnitude, spectrumData.Magnitudes[i]);
            }
            
            // Normalize to 0.0-1.0 range with logarithmic scaling for better visual representation
            float normalizationFactor = maxMagnitude > 0.0f ? 1.0f / maxMagnitude : 1.0f;
            for (int i = 0; i < _fftSize; i++)
            {
                // Apply logarithmic scaling for better frequency response visualization
                float magnitude = spectrumData.Magnitudes[i] * normalizationFactor;
                normalizedMagnitudes[i] = magnitude > 0.0f ? (float)Math.Log(1.0 + magnitude * 9.0) : 0.0f;
            }
            
            return normalizedMagnitudes;
        }
        
        /// <summary>
        /// Prepares input buffer with windowing and zero-padding
        /// </summary>
        private void PrepareInputBuffer(float[] audioData)
        {
            // Clear buffer
            Array.Clear(_fftBuffer, 0, _fftSize);
            
            // Copy audio data with windowing and zero-padding
            int copyLength = Math.Min(audioData.Length, _fftSize);
            for (int i = 0; i < copyLength; i++)
            {
                // Apply Hann window and convert to complex
                _fftBuffer[i] = new Complex(audioData[i] * _windowFunction[i], 0.0);
            }
            
            // Zero-pad remaining samples if audio data is shorter than FFT size
            for (int i = copyLength; i < _fftSize; i++)
            {
                _fftBuffer[i] = Complex.Zero;
            }
        }
        
        /// <summary>
        /// Performs Fast Fourier Transform using Cooley-Tukey algorithm
        /// </summary>
        private void PerformFFT()
        {
            // Bit-reversal permutation
            BitReversePermutation();
            
            // FFT computation
            int log2n = (int)Math.Log2(_fftSize);
            
            for (int s = 1; s <= log2n; s++)
            {
                int m = 1 << s;
                Complex wm = Complex.FromPolarCoordinates(1.0, -2.0 * Math.PI / m);
                
                for (int k = 0; k < _fftSize; k += m)
                {
                    Complex w = Complex.One;
                    
                    for (int j = 0; j < m / 2; j++)
                    {
                        Complex t = w * _fftBuffer[k + j + m / 2];
                        Complex u = _fftBuffer[k + j];
                        
                        _fftBuffer[k + j] = u + t;
                        _fftBuffer[k + j + m / 2] = u - t;
                        
                        w *= wm;
                    }
                }
            }
        }
        
        /// <summary>
        /// Performs bit-reversal permutation for FFT
        /// </summary>
        private void BitReversePermutation()
        {
            int j = 0;
            for (int i = 0; i < _fftSize - 1; i++)
            {
                if (i < j)
                {
                    // Swap elements
                    Complex temp = _fftBuffer[i];
                    _fftBuffer[i] = _fftBuffer[j];
                    _fftBuffer[j] = temp;
                }
                
                int k = _fftSize >> 1;
                while (k <= j)
                {
                    j -= k;
                    k >>= 1;
                }
                j += k;
            }
        }
        
        /// <summary>
        /// Calculates magnitudes and phases from FFT results
        /// </summary>
        private void CalculateMagnitudesAndPhases()
        {
            for (int i = 0; i < _fftSize; i++)
            {
                _magnitudeBuffer[i] = (float)_fftBuffer[i].Magnitude;
                _phaseBuffer[i] = (float)_fftBuffer[i].Phase;
            }
        }
        
        /// <summary>
        /// Generates Hann window function for better frequency resolution
        /// </summary>
        private void GenerateHannWindow()
        {
            for (int i = 0; i < _fftSize; i++)
            {
                _windowFunction[i] = 0.5f * (1.0f - (float)Math.Cos(2.0 * Math.PI * i / (_fftSize - 1)));
            }
        }
        
        /// <summary>
        /// Generates frequency array for each FFT bin
        /// </summary>
        private void GenerateFrequencyArray()
        {
            for (int i = 0; i < _fftSize; i++)
            {
                _frequencyBuffer[i] = i * FrequencyResolution;
            }
        }
        
        /// <summary>
        /// Checks if a number is a power of 2
        /// </summary>
        private static bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
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
    /// Contains processed spectrum data from FFT analysis
    /// </summary>
    public class SpectrumData
    {
        /// <summary>
        /// Magnitude values for each frequency bin
        /// </summary>
        public float[] Magnitudes { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// Phase values for each frequency bin (in radians)
        /// </summary>
        public float[] Phases { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// Frequency values for each bin (in Hz)
        /// </summary>
        public float[] Frequencies { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// FFT size used for processing
        /// </summary>
        public int FftSize { get; set; }
        
        /// <summary>
        /// Frequency resolution between bins (in Hz)
        /// </summary>
        public float FrequencyResolution { get; set; }
        
        /// <summary>
        /// Nyquist frequency (half the sample rate)
        /// </summary>
        public float NyquistFrequency { get; set; }
        
        /// <summary>
        /// Timestamp when the data was processed
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
