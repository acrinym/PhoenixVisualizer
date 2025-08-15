using System;
using System.Numerics;
using NAudio.Wave;

namespace PhoenixVisualizer.Audio;

public sealed class AudioService : IDisposable
{
    // Playback
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioFile;
    private ISampleProvider? _tapProvider;

    // Ring buffer for the last 2048 mono samples (power of two for FFT)
    private const int N = 2048;
    private readonly float[] _ring = new float[N];
    private int _ringIndex;
    private readonly object _lock = new();

    // Reusable buffers (returned to callers; caller treats them as read-only snapshots)
    private readonly float[] _fftBuffer = new float[N];   // magnitude spectrum
    private readonly float[] _waveBuffer = new float[N];  // ordered last-2048 waveform (mono)

    private bool _initialized;

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
            _audioFile = new AudioFileReader(filePath); // float samples, auto-converts format

            // Wrap with a tapping sample provider to capture samples into the ring buffer
            _tapProvider = new TapSampleProvider(_audioFile, OnSamples);
            _waveOut?.Init(_tapProvider);

            // Reset ring/index when opening a new file
            lock (_lock)
            {
                Array.Clear(_ring, 0, _ring.Length);
                _ringIndex = 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Open failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
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

        // Reset to beginning without re-creating the reader
        try
        {
            _audioFile!.CurrentTime = TimeSpan.Zero;
            System.Diagnostics.Debug.WriteLine("AudioService.Stop: Reset to beginning");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService.Stop reset failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the current magnitude spectrum (size 2048).
    /// Computed from the most recent 2048 mono samples using a Hann window + radix-2 FFT.
    /// </summary>
    public float[] ReadFft()
    {
        // Snapshot waveform (ordered) under lock
        float[] time = new float[N];
        lock (_lock)
        {
            int idx = _ringIndex; // next write position (head)
            int n1 = N - idx;
            Array.Copy(_ring, idx, time, 0, n1);
            if (idx != 0) Array.Copy(_ring, 0, time, n1, idx);
        }

        // Prepare complex buffer with Hann window
        Span<Complex> buf = stackalloc Complex[N];
        for (int i = 0; i < N; i++)
        {
            // Hann window
            float w = 0.5f * (1f - (float)Math.Cos((2 * Math.PI * i) / (N - 1)));
            buf[i] = new Complex(time[i] * w, 0.0);
        }

        // In-place iterative Cooley–Tukey FFT (radix-2)
        FftInPlace(buf);

        // Magnitude spectrum -> _fftBuffer
        // Typically you'd use first N/2 bins for real signals, but we return N for flexibility.
        for (int i = 0; i < N; i++)
        {
            double mag = buf[i].Magnitude;
            _fftBuffer[i] = (float)mag;
        }

        return _fftBuffer;
    }

    /// <summary>
    /// Returns an ordered copy of the last 2048 mono samples (time domain).
    /// </summary>
    public float[] ReadWaveform()
    {
        lock (_lock)
        {
            int idx = _ringIndex;
            int n1 = N - idx;
            Array.Copy(_ring, idx, _waveBuffer, 0, n1);
            if (idx != 0) Array.Copy(_ring, 0, _waveBuffer, n1, idx);
        }
        return _waveBuffer;
    }

    public double GetPositionSeconds() => _audioFile?.CurrentTime.TotalSeconds ?? 0.0;

    public double GetLengthSeconds() => _audioFile?.TotalTime.TotalSeconds ?? 0.0;

    public void Dispose()
    {
        try { _waveOut?.Stop(); } catch { /* ignore */ }
        _waveOut?.Dispose();
        _audioFile?.Dispose();
        _waveOut = null;
        _audioFile = null;
        _tapProvider = null;
    }

    // ===== Internals =====

    /// <summary>
    /// Receives interleaved floats from the pipeline; folds to mono and writes into ring buffer.
    /// </summary>
    private void OnSamples(float[] buffer, int offset, int samplesRead, int channels)
    {
        if (samplesRead <= 0 || channels <= 0) return;

        lock (_lock)
        {
            if (channels == 1)
            {
                // Mono fast path
                for (int i = 0; i < samplesRead; i++)
                {
                    _ring[_ringIndex] = buffer[offset + i];
                    _ringIndex = (_ringIndex + 1) & (N - 1);
                }
            }
            else
            {
                // Fold to mono: simple average across channels
                int frames = samplesRead / channels;
                int idx = offset;
                for (int f = 0; f < frames; f++)
                {
                    float sum = 0f;
                    for (int c = 0; c < channels; c++)
                    {
                        sum += buffer[idx++];
                    }
                    _ring[_ringIndex] = sum / channels;
                    _ringIndex = (_ringIndex + 1) & (N - 1);
                }
            }
        }
    }

    /// <summary>
    /// Iterative in-place radix-2 FFT on a Complex span (length must be power of two).
    /// </summary>
    private static void FftInPlace(Span<Complex> data)
    {
        int n = data.Length;

        // Bit-reversal permutation
        int j = 0;
        for (int i = 0; i < n; i++)
        {
            if (i < j)
            {
                (data[i], data[j]) = (data[j], data[i]);
            }
            int m = n >> 1;
            while (m >= 1 && j >= m)
            {
                j -= m;
                m >>= 1;
            }
            j += m;
        }

        // Danielson–Lanczos butterflies
        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = -2.0 * Math.PI / len;
            Complex wLen = new(Math.Cos(ang), Math.Sin(ang));
            for (int i = 0; i < n; i += len)
            {
                Complex w = Complex.One;
                int half = len >> 1;
                for (int k = 0; k < half; k++)
                {
                    Complex u = data[i + k];
                    Complex v = data[i + k + half] * w;
                    data[i + k] = u + v;
                    data[i + k + half] = u - v;
                    w *= wLen;
                }
            }
        }
    }

    /// <summary>
    /// Sample-provider wrapper that taps interleaved float samples as they flow through.
    /// </summary>
    private sealed class TapSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly Action<float[], int, int, int> _onSamples;

        public TapSampleProvider(ISampleProvider source, Action<float[], int, int, int> onSamples)
        {
            _source = source;
            _onSamples = onSamples;
            WaveFormat = source.WaveFormat;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);
            if (read > 0)
            {
                _onSamples(buffer, offset, read, WaveFormat.Channels);
            }
            return read;
        }
    }
}
