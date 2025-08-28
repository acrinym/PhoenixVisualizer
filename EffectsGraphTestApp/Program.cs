using System;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Effects.Graph;

namespace EffectsGraphTestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Phoenix EffectsGraph Test Application");
            Console.WriteLine("==========================================\n");

            try
            {
                // Run the interactive demo
                EffectsGraphDemoRunner.ShowInteractiveMenu();
                
                // Keep the application running
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Application error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}