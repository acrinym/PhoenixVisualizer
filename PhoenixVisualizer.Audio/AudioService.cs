using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Numerics;

namespace PhoenixVisualizer.Audio;

public sealed class AudioService : IDisposable
{
	private WaveOutEvent? _waveOut;
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Initialize failed: {ex.Message}");
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
		catch (Exception ex)
		{
			// Log the actual error for debugging
			System.Diagnostics.Debug.WriteLine($"AudioService.Open failed: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			return false;
		}
	}

	public void Play()
	{
		if (_audioFile == null)
		{
			System.Diagnostics.Debug.WriteLine("AudioService.Play: No audio file loaded");
			return;
		}
		_waveOut?.Play();
	}

	public void Pause()
	{
		if (_audioFile == null)
		{
			System.Diagnostics.Debug.WriteLine("AudioService.Pause: No audio file loaded");
			return;
		}
		_waveOut?.Pause();
	}

	public void Stop()
	{
		if (_audioFile == null)
		{
			System.Diagnostics.Debug.WriteLine("AudioService.Stop: No audio file loaded");
			return;
		}
		
		_waveOut?.Stop();
		
		// For Stop, we need to recreate the reader to reset position
		// This is how NAudio handles "stop and reset to beginning"
		try
		{
			if (_audioFile != null)
			{
				var currentPath = _audioFile.FileName;
				_audioFile.Dispose();
				_audioFile = new AudioFileReader(currentPath);
				_waveOut?.Init(_audioFile);
				System.Diagnostics.Debug.WriteLine("AudioService.Stop: Reset to beginning");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"AudioService.Stop: Failed to reset position: {ex.Message}");
		}
	}

	public float[] ReadFft()
	{
		// Always generate some test data so we can see visualization
		// In a real implementation, this would be actual FFT data from the audio
		var random = new Random();
		for (int i = 0; i < _fftBuffer.Length; i++)
		{
			// Generate some visible test patterns
			float freq = (float)i / _fftBuffer.Length;
			float wave = (float)Math.Sin(freq * Math.PI * 4 + DateTime.Now.Ticks * 0.0001);
			_fftBuffer[i] = wave * 0.3f + (float)(random.NextDouble() * 0.1);
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


