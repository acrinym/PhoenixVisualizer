using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PhoenixVisualizer.NativeAudio
{
    /// <summary>
    /// P/Invoke wrapper for LAME MP3 decoder library
    /// Now uses transpiled C# implementation instead of native DLL
    /// LAME = "LAME Ain't an MP3 Encoder" (recursive acronym)
    /// </summary>
    public static class LamePInvoke
    {
        private static Mp3Decoder? _decoder;
        private static bool _initialized = false;

        // Delegates for callbacks
        public delegate void AudioDataCallback(IntPtr data, int length, int channels, int sampleRate);

        /// <summary>
        /// Initialize LAME decoder (now using transpiled C# implementation)
        /// </summary>
        public static bool InitializeLame()
        {
            try
            {
                if (_initialized)
                    return true;

                Console.WriteLine("[LamePInvoke] 🚀 Initializing transpiled LAME decoder...");
                
                _decoder = new Mp3Decoder();
                _initialized = true;
                Console.WriteLine("[LamePInvoke] ✅ Real MP3 decoder initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LamePInvoke] ❌ LAME initialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Decode MP3 file using transpiled LAME decoder
        /// </summary>
        public static bool DecodeMp3File(string filePath, AudioDataCallback callback)
        {
            try
            {
                if (!_initialized || _decoder == null)
                {
                    Console.WriteLine("[LamePInvoke] ❌ LAME not initialized");
                    return false;
                }

                Console.WriteLine($"[LamePInvoke] 🎵 Decoding MP3 file: {filePath}");
                
                // Use the real MP3 decoder
                _decoder.DecodeMp3File(filePath, (data, length, channels, sampleRate) =>
                {
                    // Convert byte array to IntPtr for callback
                    unsafe
                    {
                        fixed (byte* ptr = data)
                        {
                            callback((IntPtr)ptr, length, channels, sampleRate);
                        }
                    }
                });
                
                Console.WriteLine("[LamePInvoke] ✅ MP3 decoding completed");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LamePInvoke] ❌ MP3 decoding failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Decode MP3 data from memory buffer (not supported with Mp3Decoder)
        /// </summary>
        public static bool DecodeMp3Data(byte[] inputData, int inputSize, byte[] outputData, int outputSize, out int bytesDecoded)
        {
            bytesDecoded = 0;
            Console.WriteLine("[LamePInvoke] ❌ DecodeMp3Data not supported with Mp3Decoder - use DecodeMp3File instead");
            return false;
        }

        /// <summary>
        /// Cleanup LAME resources
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                if (_decoder != null)
                {
                    // Mp3Decoder doesn't need cleanup
                    _decoder = null;
                }
                
                _initialized = false;
                Console.WriteLine("[LamePInvoke] 🧹 LAME resources cleaned up");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LamePInvoke] ❌ Cleanup failed: {ex.Message}");
            }
        }
    }
}