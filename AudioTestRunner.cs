using System;
using PhoenixVisualizer.Audio;

namespace PhoenixVisualizer;

/// <summary>
/// Simple test runner for VLC audio integration testing
/// </summary>
public class AudioTestRunner
{
    public static void Main(string[] args)
    {
        Console.WriteLine("🎵 Phoenix Visualizer - VLC Audio Integration Test");
        Console.WriteLine("==================================================");

        var test = new AudioIntegrationTest();
        bool success = test.RunFullAudioIntegrationTest();

        Console.WriteLine("\n" + new string('=', 50));

        if (success)
        {
            Console.WriteLine("🎉 VLC AUDIO INTEGRATION TEST: PASSED");
            Console.WriteLine("✅ All audio systems are working correctly!");
            Console.WriteLine("✅ VLC can play audio files");
            Console.WriteLine("✅ Audio data flows to visualizers");
            Console.WriteLine("✅ Visualizers can process audio data");
        }
        else
        {
            Console.WriteLine("❌ VLC AUDIO INTEGRATION TEST: FAILED");
            Console.WriteLine("❌ Issues detected in audio integration");
            Console.WriteLine("\nDetailed Results:");
            Console.WriteLine(test.GetTestResults());
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
