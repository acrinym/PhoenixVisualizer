using System;
using System.Linq;
using NAudio.Wave;

namespace PhoenixVisualizer.NativeAudio
{
    public static class AudioOutput
    {
        private static WaveOutEvent _waveOut;
        private static BufferedWaveProvider _waveProvider;
        private static bool _isAudioInitialized = false;
        private static readonly object _lock = new object();

        public static bool CreateAudioBuffer(int sampleRate, int channels, int bitsPerSample, int bufferSize)
        {
            lock (_lock)
            {
                try
                {
                    Console.WriteLine($"[AudioOutput] Initializing: {sampleRate}Hz, {channels} channels, {bitsPerSample} bits, bufferSize: {bufferSize}");

                    if (_isAudioInitialized)
                    {
                        Console.WriteLine("[AudioOutput] Already initialized, resetting...");
                        Cleanup();
                    }

                    // Use default audio device
                    Console.WriteLine("[AudioOutput] Using default audio device");

                    // Initialize NAudio with default device
                    _waveOut = new WaveOutEvent { DeviceNumber = 0 }; // Default device
                    _waveProvider = new BufferedWaveProvider(new WaveFormat(sampleRate, bitsPerSample, channels))
                    {
                        BufferDuration = TimeSpan.FromMilliseconds(100), // 100ms buffer for smoother playback
                        DiscardOnBufferOverflow = true
                    };
                    _waveOut.Init(_waveProvider);
                    _waveOut.PlaybackStopped += (s, e) =>
                    {
                        Console.WriteLine($"[AudioOutput] Playback stopped: {(e.Exception != null ? e.Exception.ToString() : "No error")}");
                    };
                    _waveOut.Play();

                    _isAudioInitialized = true;
                    Console.WriteLine("[AudioOutput] ✅ Initialized successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AudioOutput] ❌ Initialization failed: {ex}");
                    _isAudioInitialized = false;
                    return false;
                }
            }
        }

        public static bool WriteAudioData(byte[] audioData, int dataSize)
        {
            lock (_lock)
            {
                try
                {
                    if (!_isAudioInitialized)
                    {
                        Console.WriteLine("[AudioOutput] ❌ Not initialized, attempting to initialize...");
                        if (!CreateAudioBuffer(44100, 2, 16, 4608))
                            return false;
                    }

                    if (audioData == null || dataSize <= 0 || dataSize > audioData.Length)
                    {
                        Console.WriteLine($"[AudioOutput] ❌ Invalid data: size={dataSize}, array={(audioData == null ? "null" : audioData.Length.ToString())}");
                        return false;
                    }

                    // Validate PCM data
                    if (dataSize != 4608)
                    {
                        Console.WriteLine($"[AudioOutput] ⚠️ Unexpected data size: {dataSize} (expected 4608)");
                    }
                    // Log first 16 bytes for debugging
                    Console.WriteLine($"[AudioOutput] PCM sample: {BitConverter.ToString(audioData.Take(16).ToArray())}");

                    _waveProvider.AddSamples(audioData, 0, dataSize);
                    Console.WriteLine($"[AudioOutput] ✅ Wrote {dataSize} bytes (Buffered: {_waveProvider.BufferedBytes}/{_waveProvider.BufferLength}, State: {_waveOut.PlaybackState})");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AudioOutput] ❌ Write failed: {ex}");
                    return false;
                }
            }
        }

        public static void Cleanup()
        {
            lock (_lock)
            {
                try
                {
                    Console.WriteLine("[AudioOutput] Cleaning up...");
                    if (_waveOut != null)
                    {
                        _waveOut.Stop();
                        _waveOut.Dispose();
                        _waveOut = null;
                    }
                    _waveProvider?.ClearBuffer();
                    _waveProvider = null;
                    _isAudioInitialized = false;
                    Console.WriteLine("[AudioOutput] ✅ Cleanup completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AudioOutput] ❌ Cleanup failed: {ex}");
                }
            }
        }
    }
}
