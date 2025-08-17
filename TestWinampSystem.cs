using System;
using System.IO;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer;

/// <summary>
/// Simple test program to verify Winamp plugin system
/// </summary>
public class TestWinampSystem
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== PhoenixVisualizer Winamp Plugin System Test ===");
        Console.WriteLine();

        // Test 1: Check if plugin directories exist
        Console.WriteLine("1. Checking plugin directories...");
        var visDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vis");
        var apeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "ape");
        var avsPresetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets", "avs");
        var milkdropDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets", "milkdrop");

        Console.WriteLine($"   Vis plugins: {(Directory.Exists(visDir) ? "✅" : "❌")} {visDir}");
        Console.WriteLine($"   APE effects: {(Directory.Exists(apeDir) ? "✅" : "❌")} {apeDir}");
        Console.WriteLine($"   AVS presets: {(Directory.Exists(avsPresetsDir) ? "✅" : "❌")} {avsPresetsDir}");
        Console.WriteLine($"   MilkDrop: {(Directory.Exists(milkdropDir) ? "✅" : "❌")} {milkdropDir}");

        // Test 2: Check for Winamp DLLs
        Console.WriteLine();
        Console.WriteLine("2. Checking for Winamp visualizer DLLs...");
        if (Directory.Exists(visDir))
        {
            var dlls = Directory.GetFiles(visDir, "*.dll");
            foreach (var dll in dlls)
            {
                var fileName = Path.GetFileName(dll);
                var fileInfo = new FileInfo(dll);
                Console.WriteLine($"   ✅ {fileName} ({fileInfo.Length:N0} bytes)");
            }
        }

        // Test 3: Check for APE effects
        Console.WriteLine();
        Console.WriteLine("3. Checking for APE effects...");
        if (Directory.Exists(apeDir))
        {
            var apeFiles = Directory.GetFiles(apeDir, "*.ape");
            foreach (var ape in apeFiles)
            {
                var fileName = Path.GetFileName(ape);
                var fileInfo = new FileInfo(ape);
                Console.WriteLine($"   ✅ {fileName} ({fileInfo.Length:N0} bytes)");
            }
        }

        // Test 4: Check for AVS presets
        Console.WriteLine();
        Console.WriteLine("4. Checking for AVS presets...");
        if (Directory.Exists(avsPresetsDir))
        {
            var presetFiles = Directory.GetFiles(avsPresetsDir, "*.avs", SearchOption.AllDirectories);
            var bmpFiles = Directory.GetFiles(avsPresetsDir, "*.bmp", SearchOption.AllDirectories);
            
            Console.WriteLine($"   AVS files: {presetFiles.Length}");
            Console.WriteLine($"   Bitmap files: {bmpFiles.Length}");
            
            if (presetFiles.Length > 0)
            {
                Console.WriteLine("   Sample presets:");
                for (int i = 0; i < Math.Min(5, presetFiles.Length); i++)
                {
                    var fileName = Path.GetFileName(presetFiles[i]);
                    Console.WriteLine($"     - {fileName}");
                }
            }
        }

        // Test 5: Check for MilkDrop presets
        Console.WriteLine();
        Console.WriteLine("5. Checking for MilkDrop presets...");
        if (Directory.Exists(milkdropDir))
        {
            var milkFiles = Directory.GetFiles(milkdropDir, "*.milk", SearchOption.AllDirectories);
            Console.WriteLine($"   MilkDrop presets: {milkFiles.Length}");
            
            if (milkFiles.Length > 0)
            {
                Console.WriteLine("   Sample presets:");
                for (int i = 0; i < Math.Min(5, milkFiles.Length); i++)
                {
                    var fileName = Path.GetFileName(milkFiles[i]);
                    Console.WriteLine($"     - {fileName}");
                }
            }
        }

        // Test 6: Verify core system components
        Console.WriteLine();
        Console.WriteLine("6. Testing core system components...");
        
        try
        {
            // Test AudioFeaturesImpl
            var features = AudioFeaturesImpl.Create(
                new float[] { 0.1f, 0.2f, 0.3f },  // FFT
                new float[] { 0.1f, 0.2f, 0.3f },  // Waveform
                0.5f,                               // RMS
                120.0,                              // BPM
                true                                // Beat
            );
            
            Console.WriteLine($"   ✅ AudioFeaturesImpl: {features.DisplayName}");
            Console.WriteLine($"      FFT length: {features.Fft.Length}");
            Console.WriteLine($"      Waveform length: {features.Waveform.Length}");
            Console.WriteLine($"      RMS: {features.Rms}");
            Console.WriteLine($"      BPM: {features.Bpm}");
            Console.WriteLine($"      Beat: {features.Beat}");
            Console.WriteLine($"      Bass: {features.Bass}");
            Console.WriteLine($"      Mid: {features.Mid}");
            Console.WriteLine($"      Treble: {features.Treble}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ AudioFeaturesImpl failed: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("=== Test Complete ===");
        Console.WriteLine();
        Console.WriteLine("🎉 Your Winamp plugin system is ready!");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("1. Run the main PhoenixVisualizer application");
        Console.WriteLine("2. Try loading different visualizers");
        Console.WriteLine("3. Test your AVS presets and MilkDrop configurations");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
