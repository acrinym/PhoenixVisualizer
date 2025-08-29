namespace PhoenixVisualizer.Audio.Interfaces;

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

    // Common frequency presets
    enum FrequencyPreset
    {
        Standard440Hz = 0,  // Standard concert pitch
        Healing432Hz = 432, // "Miracle frequency"
        Love528Hz = 528,    // "Love frequency"
        Custom = -1         // Custom user-defined frequency
    }
}
