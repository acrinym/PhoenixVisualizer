using PhoenixVisualizer.Audio;

namespace TestPInvoke
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting P/Invoke VLC Test...");
            
            try
            {
                PInvokeTest.TestVlcPInvoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
