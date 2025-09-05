using System;
using System.Diagnostics;

namespace PhoenixVisualizer.Audio
{
    /// <summary>
    /// Simple test to verify P/Invoke VLC wrapper works
    /// </summary>
    public static class PInvokeTest
    {
        public static void TestVlcPInvoke()
        {
            try
            {
                Console.WriteLine("=== P/Invoke VLC Test ===");
                Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                
                // Test VLC initialization
                Console.WriteLine("Testing VLC initialization...");
                bool initResult = VlcPInvoke.InitializeVlc();
                Console.WriteLine($"VLC Initialize result: {initResult}");
                
                if (initResult)
                {
                    Console.WriteLine("✅ VLC P/Invoke initialization successful!");
                    
                    // Test cleanup
                    Console.WriteLine("Testing cleanup...");
                    VlcPInvoke.Cleanup();
                    Console.WriteLine("✅ VLC P/Invoke cleanup successful!");
                }
                else
                {
                    Console.WriteLine("❌ VLC P/Invoke initialization failed!");
                }
                
                Console.WriteLine("=== Test Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ P/Invoke test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
