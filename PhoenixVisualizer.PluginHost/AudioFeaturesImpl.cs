namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Concrete implementation of AudioFeatures interface
/// </summary>
public class AudioFeaturesImpl : AudioFeatures
{
    public float[] Fft { get; set; } = Array.Empty<float>();
    public float[] Waveform { get; set; } = Array.Empty<float>();
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
    
    /// <summary>
    /// Create AudioFeatures from basic data
    /// </summary>
    public static AudioFeaturesImpl Create(float[] fft, float[] waveform, float rms, double bpm = 0, bool beat = false)
    {
        var features = new AudioFeaturesImpl
        {
            Fft = fft ?? Array.Empty<float>(),
            Waveform = waveform ?? Array.Empty<float>(),
            Rms = rms,
            Bpm = bpm,
            Beat = beat
        };
        
        // Calculate additional properties
        features.CalculateAudioProperties();
        
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
    /// Calculate energy for a frequency band
    /// </summary>
    private float CalculateBandEnergy(int start, int end)
    {
        if (start >= end || start >= Fft.Length) return 0;
        
        var sum = 0f;
        var count = 0;
        
        for (int i = start; i < end && i < Fft.Length; i++)
        {
            sum += Fft[i] * Fft[i];
            count++;
        }
        
        return count > 0 ? (float)Math.Sqrt(sum / count) : 0;
    }
}
