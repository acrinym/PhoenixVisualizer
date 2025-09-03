using System;
using System.Runtime.InteropServices;

namespace CheckExports
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int GetLastError();
        
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: CheckExports <dll_path>");
                return;
            }
            
            string dllPath = args[0];
            Console.WriteLine($"Checking exports for: {dllPath}");
            
            IntPtr hModule = LoadLibrary(dllPath);
            if (hModule == IntPtr.Zero)
            {
                int error = GetLastError();
                Console.WriteLine($"Failed to load library: {error}");
                return;
            }
            
            Console.WriteLine("Library loaded successfully!");
            
            // Check for common Winamp export names
            string[] exportNames = {
                "winampVisGetHeader",
                "visHeader", 
                "winampVisGetHeaderType",
                "getModule"
            };
            
            foreach (string exportName in exportNames)
            {
                IntPtr proc = GetProcAddress(hModule, exportName);
                if (proc != IntPtr.Zero)
                {
                    Console.WriteLine($"✅ Found export: {exportName} at {proc}");
                }
                else
                {
                    Console.WriteLine($"❌ Export not found: {exportName}");
                }
            }
            
            FreeLibrary(hModule);
            Console.WriteLine("Library unloaded.");
        }
    }
}
