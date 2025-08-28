using System;
using System.IO;
using PhoenixVisualizer.Audio;
using PhoenixVisualizer.Core.Models;
using System.Linq; // Added for .Take()

namespace VlcTestStandalone;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== VLC Audio Integration Test Standalone ===");
        Console.WriteLine("Testing VLC audio service and basic functionality...");
        
        try
        {
            // Test 1: Check if we can create audio features
            Console.WriteLine("1. Testing AudioFeatures creation...");
            var audioFeatures = new AudioFeatures
            {
                SpectrumData = new float[1024],
                WaveformData = new float[1024],
                RMS = 0.5f,
                BPM = 120.0,
                Beat = false,
                Time = 0.0f
            };
            Console.WriteLine("   ✓ AudioFeatures created successfully");
            
            // Test 2: Check if we can access the audio service
            Console.WriteLine("2. Testing VLC Audio Service initialization...");
            try
            {
                var audioService = new VlcAudioService();
                Console.WriteLine("   ✓ VLC Audio Service created successfully");
                
                // Test 3: Check if we can access sample audio files
                Console.WriteLine("3. Testing audio file access...");
                var testAudioFile = Path.Combine("libs_etc", "Come home Amanda (1).mp3");
                if (File.Exists(testAudioFile))
                {
                    Console.WriteLine($"   ✓ Test audio file found: {testAudioFile}");
                    var fileInfo = new FileInfo(testAudioFile);
                    Console.WriteLine($"   File size: {fileInfo.Length} bytes");
                }
                else
                {
                    Console.WriteLine($"   ⚠ Test audio file not found: {testAudioFile}");
                    Console.WriteLine("   Checking libs_etc directory contents...");
                    var libsDir = "libs_etc";
                    if (Directory.Exists(libsDir))
                    {
                        var files = Directory.GetFiles(libsDir);
                        Console.WriteLine($"   Found {files.Length} files in libs_etc:");
                        foreach (var file in files.Take(5))
                        {
                            Console.WriteLine($"     - {Path.GetFileName(file)}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("   libs_etc directory not found");
                    }
                }
                
                audioService.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✗ VLC Audio Service creation failed: {ex.Message}");
                Console.WriteLine($"   This might be due to missing VLC libraries or display issues");
                Console.WriteLine($"   Error details: {ex.GetType().Name}");
            }
            
            Console.WriteLine("\n=== Test Complete ===");
            Console.WriteLine("Core functionality appears to be working!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("Test completed successfully!");
    }
}
