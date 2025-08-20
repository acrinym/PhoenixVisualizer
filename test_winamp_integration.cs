using PhoenixVisualizer.Core.Services;
using PhoenixVisualizer.PluginHost;
using System;
using System.IO;
using System.Threading.Tasks;

// Simple test script for Winamp integration
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🧪 Testing Winamp Integration Service...");
        
        try
        {
            // Test the integration service
            using var service = new WinampIntegrationService();
            
            if (!service.IsInitialized)
            {
                Console.WriteLine("❌ Service failed to initialize");
                return;
            }
            
            Console.WriteLine("✅ Service initialized successfully");
            
            // Test plugin scanning
            Console.WriteLine("🔍 Scanning for plugins...");
            var plugins = await service.ScanForPluginsAsync();
            
            Console.WriteLine($"📦 Found {plugins.Count} plugins:");
            foreach (var plugin in plugins)
            {
                Console.WriteLine($"  - {plugin.FileName}: {plugin.Header.Description} ({plugin.Modules.Count} modules)");
            }
            
            // Test plugin selection if any found
            if (plugins.Count > 0)
            {
                Console.WriteLine("🎯 Testing plugin selection...");
                var firstPlugin = plugins[0];
                var success = await service.SelectPluginAsync(firstPlugin, 0);
                
                if (success)
                {
                    Console.WriteLine($"✅ Successfully activated plugin: {firstPlugin.FileName}");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to activate plugin: {firstPlugin.FileName}");
                }
            }
            else
            {
                Console.WriteLine("⚠️ No plugins found. Make sure you have .dll files in the plugins/vis/ directory.");
                Console.WriteLine("💡 Try downloading some Winamp visualizer plugins like:");
                Console.WriteLine("   - vis_avs.dll (Advanced Visualization Studio)");
                Console.WriteLine("   - vis_milk2.dll (MilkDrop 2)");
                Console.WriteLine("   - vis_nsfs.dll (NSFS)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\n🎵 Test complete! Press any key to exit...");
        Console.ReadKey();
    }
}
