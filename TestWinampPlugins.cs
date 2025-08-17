using System;
using System.IO;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer;

/// <summary>
/// Simple test program for Winamp plugin loading
/// </summary>
public class TestWinampPlugins
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Winamp Plugin Test Program ===");
        Console.WriteLine();

        // Create plugin directory if it doesn't exist
        var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vis");
        if (!Directory.Exists(pluginDir))
        {
            Directory.CreateDirectory(pluginDir);
            Console.WriteLine($"Created plugin directory: {pluginDir}");
            Console.WriteLine("Please copy your Winamp visualizer .dll files to this directory.");
            Console.WriteLine();
        }

        // Check what's in the plugin directory
        var pluginFiles = Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly);
        Console.WriteLine($"Found {pluginFiles.Length} plugin files in {pluginDir}:");
        foreach (var file in pluginFiles)
        {
            Console.WriteLine($"  - {Path.GetFileName(file)}");
        }
        Console.WriteLine();

        if (pluginFiles.Length == 0)
        {
            Console.WriteLine("No plugins found. Please copy Winamp visualizer .dll files to:");
            Console.WriteLine($"  {pluginDir}");
            Console.WriteLine();
            Console.WriteLine("Common Winamp visualizer plugins include:");
            Console.WriteLine("  - vis_avs.dll (Advanced Visualization Studio)");
            Console.WriteLine("  - vis_milk2.dll (MilkDrop)");
            Console.WriteLine("  - vis_nsfs.dll (NSFS)");
            Console.WriteLine("  - vis_otv.dll (Old TV)");
            Console.WriteLine("  - vis_peaks.dll (Peaks)");
            Console.WriteLine("  - vis_spectrum.dll (Spectrum)");
            Console.WriteLine("  - vis_waveform.dll (Waveform)");
            Console.WriteLine();
            return;
        }

        // Try to load plugins
        try
        {
            Console.WriteLine("Attempting to load plugins...");
            using var host = new SimpleWinampHost(pluginDir);
            
            host.ScanForPlugins();
            
            var plugins = host.GetAvailablePlugins();
            Console.WriteLine($"Successfully loaded {plugins.Count} plugins:");
            Console.WriteLine();

            for (int i = 0; i < plugins.Count; i++)
            {
                var plugin = plugins[i];
                Console.WriteLine($"Plugin {i}: {plugin.FileName}");
                Console.WriteLine($"  Description: {plugin.Header.Description}");
                Console.WriteLine($"  Version: {plugin.Header.Version:X}");
                Console.WriteLine($"  Modules: {plugin.Modules.Count}");
                
                for (int j = 0; j < plugin.Modules.Count; j++)
                {
                    var module = plugin.Modules[j];
                    Console.WriteLine($"    Module {j}: {module.Description}");
                    Console.WriteLine($"      Sample Rate: {module.SampleRate}");
                    Console.WriteLine($"      Channels: {module.Channels}");
                    Console.WriteLine($"      Latency: {module.LatencyMs}ms");
                    Console.WriteLine($"      Delay: {module.DelayMs}ms");
                    Console.WriteLine($"      Spectrum Channels: {module.SpectrumChannels}");
                    Console.WriteLine($"      Waveform Channels: {module.WaveformChannels}");
                }
                Console.WriteLine();
            }

            // Test initialization
            if (plugins.Count > 0)
            {
                Console.WriteLine("Testing plugin initialization...");
                var success = host.InitializeModule(0, 0);
                Console.WriteLine($"Initialization result: {(success ? "SUCCESS" : "FAILED")}");
                
                if (success)
                {
                    Console.WriteLine("Plugin initialized successfully!");
                    Console.WriteLine("You can now use this plugin in your visualizer.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing plugins: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("This might mean:");
            Console.WriteLine("1. The plugins are not compatible with this system");
            Console.WriteLine("2. The plugins require additional dependencies");
            Console.WriteLine("3. The plugins are 32-bit and this is a 64-bit system");
            Console.WriteLine("4. The plugins are corrupted or incomplete");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
