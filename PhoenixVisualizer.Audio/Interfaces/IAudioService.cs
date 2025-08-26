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
}
