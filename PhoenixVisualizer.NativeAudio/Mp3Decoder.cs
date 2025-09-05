using System;
using NAudio.Wave;

namespace PhoenixVisualizer.NativeAudio
{
    public class Mp3Decoder
    {
        public void DecodeMp3File(string filePath, Action<byte[], int, int, int> callback)
        {
            try
            {
                Console.WriteLine($"[Mp3Decoder] Decoding MP3: {filePath}");
                using var mp3Reader = new Mp3FileReader(filePath);
                var waveFormat = mp3Reader.WaveFormat;

                Console.WriteLine($"[Mp3Decoder] Format: {waveFormat.SampleRate}Hz, {waveFormat.Channels} channels, {waveFormat.BitsPerSample} bits");

                // Ensure format matches expected (44.1kHz, 16-bit, stereo)
                if (waveFormat.SampleRate != 44100 || waveFormat.Channels != 2 || waveFormat.BitsPerSample != 16)
                {
                    Console.WriteLine($"[Mp3Decoder] ⚠️ Unexpected format, expected 44100Hz, 2 channels, 16 bits");
                }

                byte[] buffer = new byte[4608]; // 1152 samples * 2 channels * 2 bytes
                int bytesRead;
                while ((bytesRead = mp3Reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    callback(buffer, bytesRead, waveFormat.Channels, waveFormat.SampleRate);
                    Console.WriteLine($"[Mp3Decoder] Read {bytesRead} bytes");
                    System.Threading.Thread.Sleep(26); // Maintain ~26ms frame timing
                }
                Console.WriteLine("[Mp3Decoder] ✅ Decoding completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Mp3Decoder] ❌ Decoding failed: {ex}");
            }
        }
    }
}
