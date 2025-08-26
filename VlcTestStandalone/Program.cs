using System;
using PhoenixVisualizer;

namespace VlcTestStandalone;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== VLC Audio Integration Test Standalone ===");
        Console.WriteLine("Testing VLC audio service and visualizer data flow...");
        
        var test = new VlcTest();
        
        try
        {
            test.RunTest();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            test.Cleanup();
        }
    }
}
