namespace PhoenixVisualizer.Audio.Interfaces;

public enum VlcVisualizer
{
    Goom,      // Psychedelic visualizer
    Spectrum,  // Frequency spectrum analyzer
    Visual,    // Simple waveform visualizer
    ProjectM,  // Milkdrop-compatible visualizer
    VSXu       // VSXu visualizer
}

public interface IAudioService
{
    void Play(string path);
    void Pause();
    void Stop();
    float[] GetWaveformData();
    float[] GetSpectrumData();
    void SetRate(float rate);
    void SetTempo(float tempo);

    // Frequency retuning capabilities (432Hz, 528Hz, custom)
    void SetFundamentalFrequency(float frequency);
    float GetFundamentalFrequency();
    void SetFrequencyPreset(FrequencyPreset preset);
    FrequencyPreset GetCurrentPreset();

    // VLC visualizer support
    void SetVisualizer(VlcVisualizer visualizer);
    VlcVisualizer GetCurrentVisualizer();

    // Common frequency presets
    enum FrequencyPreset
    {
        Standard440Hz = 0,  // Standard concert pitch
        Healing432Hz = 432, // "Miracle frequency"
        Love528Hz = 528,    // "Love frequency"
        Custom = -1         // Custom user-defined frequency
    }
}
