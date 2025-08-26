using System;
using System.Diagnostics;
using System.IO;
using LibVLCSharp.Shared;

namespace VlcTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== VLC Audio Test ===");
        
        try
        {
            // Test 1: Basic VLC initialization
            Console.WriteLine("1. Testing VLC initialization...");
            var libVlc = new LibVLC(enableDebugLogs: true);
            Console.WriteLine("   ✓ LibVLC created successfully");
            
            var mediaPlayer = new MediaPlayer(libVlc);
            Console.WriteLine("   ✓ MediaPlayer created successfully");
            
            // Test 2: Try to create a media object (this will test if native libvlc.dll is found)
            Console.WriteLine("2. Testing media creation...");
            
            // Create a dummy media object to test if VLC can find the native library
            var testMedia = new Media(libVlc, new Uri("file:///dummy"));
            Console.WriteLine("   ✓ Media object created successfully");
            testMedia.Dispose();
            
            // Test 3: Check if we can access VLC properties
            Console.WriteLine("3. Testing VLC properties...");
            var version = libVlc.Version;
            Console.WriteLine($"   ✓ VLC Version: {version}");
            
            var libVersion = libVlc.LibVlcVersion;
            Console.WriteLine($"   ✓ LibVLC Version: {libVersion}");
            
            // Test 4: Try to play a test file if provided
            if (args.Length > 0 && File.Exists(args[0]))
            {
                Console.WriteLine($"4. Testing audio playback with: {args[0]}");
                
                var media = new Media(libVlc, new Uri(Path.GetFullPath(args[0])));
                mediaPlayer.Media = media;
                
                // Set up event handlers
                mediaPlayer.TimeChanged += (s, e) => Console.WriteLine($"   Time: {e.Time}ms");
                mediaPlayer.LengthChanged += (s, e) => Console.WriteLine($"   Length: {e.Length}ms");
                mediaPlayer.Playing += (s, e) => Console.WriteLine("   ✓ Playback started");
                mediaPlayer.Paused += (s, e) => Console.WriteLine("   ✓ Playback paused");
                mediaPlayer.Stopped += (s, e) => Console.WriteLine("   ✓ Playback stopped");
                mediaPlayer.EncounteredError += (s, e) => Console.WriteLine("   ✗ Error encountered");
                
                // Try to play
                var playResult = mediaPlayer.Play();
                Console.WriteLine($"   Play() result: {playResult}");
                
                if (playResult)
                {
                    Console.WriteLine("   Waiting 3 seconds to see if playback starts...");
                    System.Threading.Thread.Sleep(3000);
                    
                    var isPlaying = mediaPlayer.IsPlaying;
                    var time = mediaPlayer.Time;
                    var length = mediaPlayer.Length;
                    
                    Console.WriteLine($"   IsPlaying: {isPlaying}");
                    Console.WriteLine($"   Current Time: {time}ms");
                    Console.WriteLine($"   Total Length: {length}ms");
                    
                    if (isPlaying && time > 0)
                    {
                        Console.WriteLine("   🎵 SUCCESS: Audio is actually playing!");
                    }
                    else
                    {
                        Console.WriteLine("   ⚠️  WARNING: Play() succeeded but no actual playback detected");
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
                Console.WriteLine("4. No test file provided. Usage: dotnet run VlcTest.cs <audio-file-path>");
            }
            
            // Cleanup
            mediaPlayer.Dispose();
            libVlc.Dispose();
            
            Console.WriteLine("\n=== Test Complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ TEST FAILED: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
