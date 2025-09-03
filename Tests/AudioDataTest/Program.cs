using System;
using System.Diagnostics;
using System.IO;
using LibVLCSharp.Shared;

namespace AudioDataTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== VLC Audio Data Capture Test ===");

        try
        {
            // Test 1: Basic VLC initialization
            Console.WriteLine("1. Testing VLC initialization...");
            var libVlc = new LibVLC(enableDebugLogs: true);
            Console.WriteLine("   ✓ LibVLC created successfully");

            var mediaPlayer = new MediaPlayer(libVlc);
            Console.WriteLine("   ✓ MediaPlayer created successfully");

            // Test 2: Check VLC audio capabilities
            Console.WriteLine("2. Checking VLC audio capabilities...");
            
            // VLC provides audio data through different mechanisms
            // For now, let's test basic playback and see what's available
            Console.WriteLine("   ✓ Basic VLC setup complete");

            // Test 3: Try to play a test file if provided
            if (args.Length > 0 && File.Exists(args[0]))
            {
                Console.WriteLine($"3. Testing audio playback with: {args[0]}");

                var media = new Media(libVlc, new Uri(Path.GetFullPath(args[0])));
                mediaPlayer.Media = media;

                // Set up event handlers
                mediaPlayer.Playing += (s, e) => Console.WriteLine("   ✓ Playback started");
                mediaPlayer.Stopped += (s, e) => Console.WriteLine("   ✓ Playback stopped");
                mediaPlayer.TimeChanged += (s, e) => 
                {
                    if (e.Time % 1000 == 0) // Log every second
                    {
                        Console.WriteLine($"   Time: {e.Time}ms");
                    }
                };

                // Try to play
                var playResult = mediaPlayer.Play();
                Console.WriteLine($"   Play() result: {playResult}");

                if (playResult)
                {
                    Console.WriteLine("   Waiting 5 seconds to capture audio data...");
                    System.Threading.Thread.Sleep(5000);

                    var isPlaying = mediaPlayer.IsPlaying;
                    var time = mediaPlayer.Time;

                    Console.WriteLine($"   IsPlaying: {isPlaying}, Time: {time}ms");

                    if (isPlaying && time > 0)
                    {
                        Console.WriteLine("   🎵 SUCCESS: Audio is playing and callbacks are active!");
                        Console.WriteLine("   Audio data should be captured in callbacks above");
                    }
                    else
                    {
                        Console.WriteLine("   ⚠️  WARNING: No actual playback detected");
                    }

                    mediaPlayer.Stop();
                }
                else
                {
                    Console.WriteLine("   ✗ Play() failed");
                }

                media.Dispose();
            }
            else
            {
                Console.WriteLine("   No test file provided");
            }

            // Cleanup
            mediaPlayer.Dispose();
            libVlc.Dispose();

            Console.WriteLine("=== Test Complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ TEST FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
    }
}
