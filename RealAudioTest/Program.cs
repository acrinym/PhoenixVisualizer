using System;
using System.Diagnostics;
using PhoenixVisualizer.Audio;

namespace RealAudioTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Real Audio Data Processing Test ===");

        try
        {
            // Test 1: Create VlcAudioService
            Console.WriteLine("1. Creating VlcAudioService...");
            var audioService = new VlcAudioService();
            Console.WriteLine("   ✓ VlcAudioService created successfully");

            // Test 2: Generate test audio data
            Console.WriteLine("2. Generating test audio data...");
            var testAudioData = GenerateTestAudioData(48000, 2.0); // 2 seconds of 48kHz audio
            Console.WriteLine($"   ✓ Generated {testAudioData.Length} audio samples");

            // Test 3: Feed real audio data to the service
            Console.WriteLine("3. Feeding real audio data...");
            audioService.FeedAudioData(testAudioData);
            Console.WriteLine("   ✓ Audio data fed to service");

            // Test 4: Verify spectrum data
            Console.WriteLine("4. Testing spectrum data...");
            var spectrumData = audioService.GetSpectrumData();
            Console.WriteLine($"   ✓ Got spectrum data: {spectrumData.Length} samples");
            
            // Check if it's real data (not simulated)
            float maxValue = 0;
            for (int i = 0; i < spectrumData.Length; i++)
            {
                maxValue = Math.Max(maxValue, spectrumData[i]);
            }
            Console.WriteLine($"   Max spectrum value: {maxValue:F4}");

            // Test 5: Verify waveform data
            Console.WriteLine("5. Testing waveform data...");
            var waveformData = audioService.GetWaveformData();
            Console.WriteLine($"   ✓ Got waveform data: {waveformData.Length} samples");
            
            // Check if it's real data (not simulated)
            maxValue = 0;
            for (int i = 0; i < waveformData.Length; i++)
            {
                maxValue = Math.Max(maxValue, Math.Abs(waveformData[i]));
            }
            Console.WriteLine($"   Max waveform value: {maxValue:F4}");

            // Test 6: Verify data consistency
            Console.WriteLine("6. Testing data consistency...");
            var spectrumData2 = audioService.GetSpectrumData();
            var waveformData2 = audioService.GetWaveformData();
            
            bool spectrumConsistent = AreArraysEqual(spectrumData, spectrumData2);
            bool waveformConsistent = AreArraysEqual(waveformData, waveformData2);
            
            Console.WriteLine($"   Spectrum data consistent: {spectrumConsistent}");
            Console.WriteLine($"   Waveform data consistent: {waveformConsistent}");

            // Test 7: Test with different audio data
            Console.WriteLine("7. Testing with different audio data...");
            var testAudioData2 = GenerateTestAudioData(48000, 1.0); // 1 second of different audio
            audioService.FeedAudioData(testAudioData2);
            
            var spectrumData3 = audioService.GetSpectrumData();
            var waveformData3 = audioService.GetWaveformData();
            
            bool spectrumChanged = !AreArraysEqual(spectrumData, spectrumData3);
            bool waveformChanged = !AreArraysEqual(waveformData, waveformData3);
            
            Console.WriteLine($"   Spectrum data changed: {spectrumChanged}");
            Console.WriteLine($"   Waveform data changed: {waveformChanged}");

            // Cleanup
            audioService.Dispose();
            Console.WriteLine("   ✓ Service disposed");

            Console.WriteLine("=== Test Complete ===");
            
            if (spectrumConsistent && waveformConsistent && spectrumChanged && waveformChanged)
            {
                Console.WriteLine("🎉 SUCCESS: Real audio data processing is working!");
            }
            else
            {
                Console.WriteLine("⚠️  WARNING: Some tests failed - check output above");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ TEST FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
    }

    private static float[] GenerateTestAudioData(int sampleRate, double duration)
    {
        int numSamples = (int)(sampleRate * duration);
        var audioData = new float[numSamples];
        
        // Generate a test tone with multiple frequencies
        for (int i = 0; i < numSamples; i++)
        {
            double time = i / (double)sampleRate;
            
            // Mix of different frequencies
            audioData[i] = (float)(
                Math.Sin(2 * Math.PI * 440 * time) * 0.3 +  // A4 note
                Math.Sin(2 * Math.PI * 880 * time) * 0.2 +  // A5 note
                Math.Sin(2 * Math.PI * 220 * time) * 0.1    // A3 note
            );
        }
        
        return audioData;
    }

    private static bool AreArraysEqual(float[] arr1, float[] arr2)
    {
        if (arr1.Length != arr2.Length) return false;
        
        for (int i = 0; i < arr1.Length; i++)
        {
            if (Math.Abs(arr1[i] - arr2[i]) > 0.0001f) return false;
        }
        
        return true;
    }
}
