namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Concrete implementation of AudioFeatures interface with enhanced FFT analysis
/// </summary>
public class AudioFeaturesImpl : AudioFeatures
{
    public float[] Fft { get; set; } = new float[0];
    public float[] Waveform { get; set; } = new float[0];
    public float Rms { get; set; }
    public double Bpm { get; set; }
    public bool Beat { get; set; }
    
    // Additional audio analysis properties
    public float Bass { get; set; }
    public float Mid { get; set; }
    public float Treble { get; set; }
    public float Energy { get; set; }
    public float Volume { get; set; }
    public float Peak { get; set; }
    public double TimeSeconds { get; set; }
    
    // Enhanced frequency analysis
    public float[] FrequencyBands { get; set; } = new float[0];
    public float[] SmoothedFft { get; set; } = new float[0];
    
    /// <summary>
    /// Create AudioFeatures from basic data
    /// </summary>
    public static AudioFeaturesImpl Create(float[] fft, float[] waveform, float rms, double bpm = 0, bool beat = false)
    {
        var features = new AudioFeaturesImpl
        {
            Fft = fft ?? new float[0],
            Waveform = waveform ?? new float[0],
            Rms = rms,
            Bpm = bpm,
            Beat = beat
        };
        
        // Calculate additional properties
        features.CalculateAudioProperties();
        
        return features;
    }
    
    /// <summary>
    /// Create AudioFeatures with enhanced FFT processing
    /// </summary>
    public static AudioFeaturesImpl CreateEnhanced(float[] fft, float[] waveform, float rms, double bpm = 0, bool beat = false, double timeSeconds = 0)
    {
        var features = new AudioFeaturesImpl
        {
            Fft = fft ?? new float[0],
            Waveform = waveform ?? new float[0],
            Rms = rms,
            Bpm = bpm,
            Beat = beat,
            TimeSeconds = timeSeconds
        };
        
        // Calculate enhanced audio properties
        features.CalculateEnhancedAudioProperties();
        
        return features;
    }
    
    /// <summary>
    /// Calculate additional audio properties from FFT data
    /// </summary>
    private void CalculateAudioProperties()
    {
        if (Fft.Length == 0) return;

        // Calculate frequency bands (simplified)
        var fftSize = Fft.Length;
        
        // Bass: 20-250 Hz (roughly first 5% of FFT)
        var bassStart = 0;
        var bassEnd = Math.Min((int)(fftSize * 0.05), fftSize - 1);
        Bass = CalculateBandEnergy(bassStart, bassEnd);
        
        // Mid: 250-4000 Hz (roughly 5%-40% of FFT)
        var midStart = bassEnd;
        var midEnd = Math.Min((int)(fftSize * 0.4), fftSize - 1);
        Mid = CalculateBandEnergy(midStart, midEnd);
        
        // Treble: 4000-20000 Hz (roughly 40%-90% of FFT)
        var trebleStart = midEnd;
        var trebleEnd = Math.Min((int)(fftSize * 0.9), fftSize - 1);
        Treble = CalculateBandEnergy(trebleStart, trebleEnd);
        
        // Overall energy and volume
        Energy = Rms;
        Volume = Rms;
        
        // Peak detection
        Peak = Fft.Length > 0 ? Fft.Max() : 0;
    }
    
    /// <summary>
    /// Calculate enhanced audio properties with proper frequency analysis
    /// </summary>
    private void CalculateEnhancedAudioProperties()
    {
        if (Fft.Length == 0) return;

        var fftSize = Fft.Length;
        
        // Create smoothed FFT data for better visualization
        SmoothedFft = new float[fftSize];
        Array.Copy(Fft, SmoothedFft, fftSize);
        
        // Apply basic smoothing to reduce noise
        ApplyFFTSmoothing(SmoothedFft);
        
        // Calculate frequency bands based on actual frequency ranges
        // Assuming 44.1kHz sample rate, FFT gives us 0-22.05kHz
        var sampleRate = 44100.0;
        var nyquist = sampleRate / 2.0;
        var freqPerBin = nyquist / fftSize;
        
        // Define frequency bands (in Hz)
        var bandFrequencies = new[] { 60, 250, 500, 1000, 2000, 4000, 8000, 16000 };
        FrequencyBands = new float[bandFrequencies.Length];
        
        for (int i = 0; i < bandFrequencies.Length; i++)
        {
            var targetFreq = bandFrequencies[i];
            var binIndex = (int)(targetFreq / freqPerBin);
            if (binIndex < fftSize)
            {
                FrequencyBands[i] = SmoothedFft[binIndex];
            }
        }
        
        // Calculate traditional frequency bands with better accuracy
        var bassStart = (int)(60.0 / freqPerBin);   // 60 Hz
        var bassEnd = (int)(250.0 / freqPerBin);    // 250 Hz
        var midStart = bassEnd;
        var midEnd = (int)(4000.0 / freqPerBin);    // 4 kHz
        var trebleStart = midEnd;
        var trebleEnd = Math.Min((int)(16000.0 / freqPerBin), fftSize - 1); // 16 kHz
        
        Bass = CalculateBandEnergy(bassStart, bassEnd);
        Mid = CalculateBandEnergy(midStart, midEnd);
        Treble = CalculateBandEnergy(trebleStart, trebleEnd);
        
        // Enhanced energy calculation
        Energy = CalculateTotalEnergy();
        Volume = CalculateRMS();
        Peak = SmoothedFft.Length > 0 ? SmoothedFft.Max() : 0;
        
        // Beat detection enhancement
        if (Energy > 0.01f) // Only detect beats when there's significant audio
        {
            Beat = DetectBeat(Energy);
        }
    }
    
    /// <summary>
    /// Apply smoothing to FFT data to reduce noise
    /// </summary>
    private static void ApplyFFTSmoothing(float[] fftData)
    {
        if (fftData.Length < 3) return;
        
        var smoothed = new float[fftData.Length];
        Array.Copy(fftData, smoothed, fftData.Length);
        
        // Simple 3-point moving average
        for (int i = 1; i < fftData.Length - 1; i++)
        {
            smoothed[i] = (fftData[i - 1] + fftData[i] + fftData[i + 1]) / 3.0f;
        }
        
        // Copy back smoothed data
        Array.Copy(smoothed, fftData, fftData.Length);
    }
    
    /// <summary>
    /// Calculate total energy across all frequency bands
    /// </summary>
    private float CalculateTotalEnergy()
    {
        if (SmoothedFft.Length == 0) return 0;
        
        var sum = 0f;
        for (int i = 0; i < SmoothedFft.Length; i++)
        {
            sum += SmoothedFft[i] * SmoothedFft[i];
        }
        return (float)Math.Sqrt(sum / SmoothedFft.Length);
    }
    
    /// <summary>
    /// Calculate RMS (Root Mean Square) for volume measurement
    /// </summary>
    private float CalculateRMS()
    {
        if (Waveform.Length == 0) return 0;
        
        var sum = 0f;
        for (int i = 0; i < Waveform.Length; i++)
        {
            sum += Waveform[i] * Waveform[i];
        }
        return (float)Math.Sqrt(sum / Waveform.Length);
    }
    
    /// <summary>
    /// Enhanced beat detection using energy threshold
    /// </summary>
    private static bool DetectBeat(float currentEnergy)
    {
        // Simple threshold-based beat detection
        // In a real implementation, this would use more sophisticated algorithms
        const float beatThreshold = 0.1f;
        return currentEnergy > beatThreshold;
    }
    
    /// <summary>
    /// Calculate energy for a frequency band
    /// </summary>
    private float CalculateBandEnergy(int start, int end)
    {
        if (start >= end || start >= SmoothedFft.Length) return 0;
        
        var sum = 0f;
        var count = 0;
        
        for (int i = start; i < end && i < SmoothedFft.Length; i++)
        {
            sum += SmoothedFft[i] * SmoothedFft[i];
            count++;
        }
        
        return count > 0 ? (float)Math.Sqrt(sum / count) : 0;
    }
    
    /// <summary>
    /// Get frequency value for a specific FFT bin
    /// </summary>
    public float GetFrequencyAtBin(int binIndex)
    {
        if (binIndex < 0 || binIndex >= SmoothedFft.Length) return 0;
        
        // Assuming 44.1kHz sample rate
        var sampleRate = 44100.0;
        var nyquist = sampleRate / 2.0;
        var freqPerBin = nyquist / SmoothedFft.Length;
        
        return (float)(binIndex * freqPerBin);
    }
    
    /// <summary>
    /// Get amplitude at a specific frequency
    /// </summary>
    public float GetAmplitudeAtFrequency(float frequency)
    {
        if (SmoothedFft.Length == 0) return 0;
        
        // Assuming 44.1kHz sample rate
        var sampleRate = 44100.0;
        var nyquist = sampleRate / 2.0;
        var freqPerBin = nyquist / SmoothedFft.Length;
        
        var binIndex = (int)(frequency / freqPerBin);
        if (binIndex >= 0 && binIndex < SmoothedFft.Length)
        {
            return SmoothedFft[binIndex];
        }
        
        return 0;
    }
}
