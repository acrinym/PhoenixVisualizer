using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PhoenixVisualizer.Audio
{
    /// <summary>
    /// Direct P/Invoke wrapper for VLC native functions to bypass LibVLCSharp compatibility issues
    /// </summary>
    public static class VlcPInvoke
    {
        private const string LibVlcDll = "libvlc\\win-x64\\libvlc.dll";
        private const string LibVlcCoreDll = "libvlc\\win-x64\\libvlccore.dll";

        // VLC instance handle
        public static IntPtr LibVlcInstance = IntPtr.Zero;
        public static IntPtr MediaPlayerInstance = IntPtr.Zero;
        public static IntPtr MediaInstance = IntPtr.Zero;

        // Core VLC functions
        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_new(int argc, IntPtr argv);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_release(IntPtr instance);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_new_path(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)] string path);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_release(IntPtr media);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_player_new_from_media(IntPtr media);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_release(IntPtr mediaPlayer);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_play(IntPtr mediaPlayer);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_pause(IntPtr mediaPlayer);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_stop(IntPtr mediaPlayer);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_is_playing(IntPtr mediaPlayer);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern long libvlc_media_player_get_time(IntPtr mediaPlayer);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern long libvlc_media_player_get_length(IntPtr mediaPlayer);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_time(IntPtr mediaPlayer, long time);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern float libvlc_audio_get_volume(IntPtr mediaPlayer);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_set_volume(IntPtr mediaPlayer, int volume);

        // Audio callback delegates
        public delegate int AudioFormatCallback(ref IntPtr opaque, ref IntPtr format, ref uint rate, ref uint channels);
        public delegate void AudioFormatCleanupCallback(IntPtr opaque);
        public delegate void AudioCallback(IntPtr data, IntPtr samples, uint count, long pts);
        public delegate void AudioPauseCallback(IntPtr data, long pts);
        public delegate void AudioResumeCallback(IntPtr data, long pts);
        public delegate void AudioFlushCallback(IntPtr data, long pts);
        public delegate void AudioDrainCallback(IntPtr data);

        // Audio callback functions
        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_audio_set_format_callbacks(IntPtr mediaPlayer, 
            AudioFormatCallback setup, AudioFormatCleanupCallback cleanup);

        [DllImport(LibVlcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_audio_set_callbacks(IntPtr mediaPlayer,
            AudioCallback play, AudioPauseCallback pause, AudioResumeCallback resume,
            AudioFlushCallback flush, AudioDrainCallback drain, IntPtr opaque);

        // Initialize VLC with minimal options
        public static bool InitializeVlc()
        {
            try
            {
                // Create minimal argument array
                var args = new string[] { "--intf=dummy", "--no-video" };
                var argc = args.Length;
                var argv = Marshal.AllocHGlobal(argc * IntPtr.Size);
                
                for (int i = 0; i < argc; i++)
                {
                    var argPtr = Marshal.StringToHGlobalAnsi(args[i]);
                    Marshal.WriteIntPtr(argv, i * IntPtr.Size, argPtr);
                }

                LibVlcInstance = libvlc_new(argc, argv);
                
                // Clean up
                for (int i = 0; i < argc; i++)
                {
                    var argPtr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    Marshal.FreeHGlobal(argPtr);
                }
                Marshal.FreeHGlobal(argv);

                return LibVlcInstance != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VLC P/Invoke initialization failed: {ex.Message}");
                return false;
            }
        }

        // Play audio file
        public static bool PlayAudio(string filePath)
        {
            try
            {
                Console.WriteLine($"[VlcPInvoke] PlayAudio called with: {filePath}");
                System.Diagnostics.Debug.WriteLine($"[VlcPInvoke] PlayAudio called with: {filePath}");
                
                if (LibVlcInstance == IntPtr.Zero)
                {
                    Console.WriteLine("[VlcPInvoke] LibVlcInstance is null, initializing...");
                    if (!InitializeVlc()) return false;
                }

                // Create media
                Console.WriteLine("[VlcPInvoke] Creating media from path...");
                MediaInstance = libvlc_media_new_path(LibVlcInstance, filePath);
                if (MediaInstance == IntPtr.Zero) 
                {
                    Console.WriteLine("[VlcPInvoke] Failed to create media from path");
                    return false;
                }

                // Create media player
                Console.WriteLine("[VlcPInvoke] Creating media player...");
                MediaPlayerInstance = libvlc_media_player_new_from_media(MediaInstance);
                if (MediaPlayerInstance == IntPtr.Zero) 
                {
                    Console.WriteLine("[VlcPInvoke] Failed to create media player");
                    return false;
                }

                // Set audio format to 16-bit stereo PCM (standard format)
                Console.WriteLine("[VlcPInvoke] Setting audio format callbacks...");
                libvlc_audio_set_format_callbacks(MediaPlayerInstance, AudioFormatSetup, null);

                // Set audio callbacks to receive audio data
                Console.WriteLine("[VlcPInvoke] Setting audio callbacks...");
                libvlc_audio_set_callbacks(MediaPlayerInstance, AudioPlayback, null, null, null, null, IntPtr.Zero);

                // Play
                Console.WriteLine("[VlcPInvoke] Starting playback...");
                int result = libvlc_media_player_play(MediaPlayerInstance);
                Console.WriteLine($"[VlcPInvoke] Play result: {result}");
                return result == 0; // 0 = success
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VLC P/Invoke play failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"VLC P/Invoke play failed: {ex.Message}");
                return false;
            }
        }

        // Audio format setup callback
        private static int AudioFormatSetup(ref IntPtr opaque, ref IntPtr format, ref uint rate, ref uint channels)
        {
            // Set format to 16-bit signed integer, stereo
            // VLC expects a 4-character format string
            format = Marshal.StringToHGlobalAnsi("S16L"); // Signed 16-bit Little endian
            rate = 44100; // 44.1 kHz
            channels = 2; // Stereo
            Console.WriteLine($"[VlcPInvoke] AudioFormatSetup: format=S16L, rate={rate}, channels={channels}");
            return 0; // Success
        }

        // Audio playback callback - receives audio data
        private static void AudioPlayback(IntPtr opaque, IntPtr samples, uint count, long pts)
        {
            // For now, just log that we're receiving audio data
            // In a full implementation, this would process the audio samples
            Console.WriteLine($"[VlcPInvoke] Audio callback: received {count} samples at {pts}");
            System.Diagnostics.Debug.WriteLine($"[VlcPInvoke] Audio callback: received {count} samples at {pts}");
        }

        // Cleanup
        public static void Cleanup()
        {
            try
            {
                if (MediaPlayerInstance != IntPtr.Zero)
                {
                    libvlc_media_player_stop(MediaPlayerInstance);
                    libvlc_media_player_release(MediaPlayerInstance);
                    MediaPlayerInstance = IntPtr.Zero;
                }

                if (MediaInstance != IntPtr.Zero)
                {
                    libvlc_media_release(MediaInstance);
                    MediaInstance = IntPtr.Zero;
                }

                if (LibVlcInstance != IntPtr.Zero)
                {
                    libvlc_release(LibVlcInstance);
                    LibVlcInstance = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VLC P/Invoke cleanup failed: {ex.Message}");
            }
        }

        // Check if playing
        public static bool IsPlaying()
        {
            if (MediaPlayerInstance == IntPtr.Zero) return false;
            return libvlc_media_player_is_playing(MediaPlayerInstance) != 0;
        }

        // Get current time
        public static long GetCurrentTime()
        {
            if (MediaPlayerInstance == IntPtr.Zero) return 0;
            return libvlc_media_player_get_time(MediaPlayerInstance);
        }

        // Get total length
        public static long GetLength()
        {
            if (MediaPlayerInstance == IntPtr.Zero) return 0;
            return libvlc_media_player_get_length(MediaPlayerInstance);
        }

        // Pause playback
        public static void Pause()
        {
            if (MediaPlayerInstance != IntPtr.Zero)
            {
                libvlc_media_player_pause(MediaPlayerInstance);
            }
        }

        // Stop playback
        public static void Stop()
        {
            if (MediaPlayerInstance != IntPtr.Zero)
            {
                libvlc_media_player_stop(MediaPlayerInstance);
            }
        }
    }
}
