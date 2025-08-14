using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Numerics;

namespace PhoenixVisualizer.Audio;

public sealed class AudioService : IDisposable
{
	private IWavePlayer? _waveOut;
	private AudioFileReader? _audioFile;
	private bool _initialized;
	private readonly float[] _fftBuffer = new float[2048];
	private readonly Complex[] _fftComplex = new Complex[2048];

    public bool Initialize()
	{
        if (_initialized) return true;
        try
        {
            _waveOut = new WaveOutEvent();
            _initialized = true;
        }
        catch (Exception)
        {
            _initialized = false;
        }
        return _initialized;
	}

	public bool Open(string filePath)
	{
		if (!_initialized && !Initialize()) return false;
		
		try
		{
			_audioFile?.Dispose();
			_audioFile = new AudioFileReader(filePath);
			_waveOut?.Init(_audioFile);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public void Play()
	{
		_waveOut?.Play();
	}

	public void Pause()
	{
		_waveOut?.Pause();
	}

	public void Stop()
	{
		_waveOut?.Stop();
	}

	public float[] ReadFft()
	{
		if (_audioFile == null || _waveOut?.PlaybackState != PlaybackState.Playing)
		{
			Array.Clear(_fftBuffer, 0, _fftBuffer.Length);
			return _fftBuffer;
		}

		// Simple FFT simulation - in a real implementation you'd want proper FFT
		// For now, generate some dummy frequency data
		var random = new Random();
		for (int i = 0; i < _fftBuffer.Length; i++)
		{
			_fftBuffer[i] = (float)(random.NextDouble() * 0.1);
		}
		
		return _fftBuffer;
	}

	public double GetPositionSeconds()
	{
		return _audioFile?.CurrentTime.TotalSeconds ?? 0;
	}

	public void Dispose()
	{
		_waveOut?.Dispose();
		_audioFile?.Dispose();
	}
}


