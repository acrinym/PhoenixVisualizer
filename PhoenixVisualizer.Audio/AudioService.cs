using ManagedBass;

namespace PhoenixVisualizer.Audio;

public sealed class AudioService
{
	private int _stream;
	private bool _initialized;
	private readonly float[] _fftBuffer = new float[2048];

	public bool Initialize()
	{
		if (_initialized) return true;
		_initialized = Bass.Init();
		return _initialized;
	}

	public bool Open(string filePath)
	{
		if (!_initialized && !Initialize()) return false;
		if (_stream != 0)
		{
			Bass.StreamFree(_stream);
			_stream = 0;
		}
		_stream = Bass.CreateStream(filePath, 0, 0, BassFlags.AutoFree);
		return _stream != 0;
	}

	public void Play()
	{
		if (_stream != 0) Bass.ChannelPlay(_stream);
	}

	public void Pause()
	{
		if (_stream != 0) Bass.ChannelPause(_stream);
	}

	public void Stop()
	{
		if (_stream != 0) Bass.ChannelStop(_stream);
	}

	public float[] ReadFft()
	{
		if (_stream == 0) Array.Clear(_fftBuffer, 0, _fftBuffer.Length);
		else Bass.ChannelGetData(_stream, _fftBuffer, (int)DataFlags.FFT4096);
		return _fftBuffer;
	}

	public double GetPositionSeconds()
	{
		if (_stream == 0) return 0;
		long pos = Bass.ChannelGetPosition(_stream);
		double seconds = Bass.ChannelBytes2Seconds(_stream, pos);
		return double.IsFinite(seconds) ? seconds : 0;
	}
}


