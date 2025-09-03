using System;
using System.Runtime.InteropServices;
using System.IO;

namespace PluginTest
{
    class Program
    {
        // Win32 API functions for DLL loading
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        // Winamp plugin structures
        [StructLayout(LayoutKind.Sequential)]
        public struct WinampVisHeader
        {
            public int Version;
            [MarshalAs(UnmanagedType.LPStr)]
            public string Description;
            public IntPtr GetModuleFunc;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WinampVisModule
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string Description;
            public IntPtr HwndParent;
            public IntPtr HDllInstance;
            public int SampleRate;
            public int Channels;
            public int LatencyMs;
            public int DelayMs;
            public int SpectrumChannels;
            public int WaveformChannels;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[,] SpectrumData;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[,] WaveformData;
            public IntPtr ConfigFunc;
            public IntPtr InitFunc;
            public IntPtr RenderFunc;
            public IntPtr QuitFunc;
            public IntPtr UserData;
        }

        // Function delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetModuleDelegate(int index);

        static void Main(string[] args)
        {
            Console.WriteLine("🧪 Winamp Plugin Test Shim");
            Console.WriteLine("==========================");
            
            var pluginDir = Path.Combine(Directory.GetCurrentDirectory(), "plugins");
            Console.WriteLine($"🔍 Testing plugins in: {pluginDir}");
            
            if (!Directory.Exists(pluginDir))
            {
                Console.WriteLine($"❌ Plugin directory doesn't exist: {pluginDir}");
                return;
            }

            var dllFiles = Directory.GetFiles(pluginDir, "*.dll");
            Console.WriteLine($"🔍 Found {dllFiles.Length} DLL files:");
            foreach (var dll in dllFiles)
            {
                Console.WriteLine($"  - {Path.GetFileName(dll)}");
            }

            Console.WriteLine("\n🧪 Testing individual plugins:");
            Console.WriteLine("=============================");

            foreach (var dllFile in dllFiles)
            {
                TestPlugin(dllFile);
                Console.WriteLine();
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void TestPlugin(string dllPath)
        {
            var fileName = Path.GetFileName(dllPath);
            Console.WriteLine($"🔍 Testing: {fileName}");
            
            try
            {
                // 1. Try to load the DLL
                Console.WriteLine($"  📥 Loading DLL...");
                var libraryHandle = LoadLibrary(dllPath);
                if (libraryHandle == IntPtr.Zero)
                {
                    var error = GetLastError();
                    Console.WriteLine($"  ❌ Failed to load DLL. Error: {error}");
                    return;
                }
                Console.WriteLine($"  ✅ DLL loaded successfully. Handle: {libraryHandle}");

                try
                {
                    // 2. Try to find the visHeader function
                    Console.WriteLine($"  🔍 Looking for visHeader function...");
                    var visHeaderPtr = GetProcAddress(libraryHandle, "visHeader");
                    if (visHeaderPtr == IntPtr.Zero)
                    {
                        Console.WriteLine($"  ❌ visHeader function not found");
                        
                        // Try alternative names
                        Console.WriteLine($"  🔍 Trying alternative function names...");
                        var alternatives = new[] { "winampVisGetHeader", "getHeader", "header" };
                        foreach (var alt in alternatives)
                        {
                            var altPtr = GetProcAddress(libraryHandle, alt);
                            if (altPtr != IntPtr.Zero)
                            {
                                Console.WriteLine($"  ✅ Found alternative function: {alt} at {altPtr}");
                                visHeaderPtr = altPtr;
                                break;
                            }
                        }
                        
                        if (visHeaderPtr == IntPtr.Zero)
                        {
                            Console.WriteLine($"  ❌ No header function found");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  ✅ visHeader function found at {visHeaderPtr}");
                    }

                    // 3. Try to call the header function
                    Console.WriteLine($"  🔍 Calling header function...");
                    try
                    {
                        var getHeaderFunc = Marshal.GetDelegateForFunctionPointer<GetModuleDelegate>(visHeaderPtr);
                        var headerPtr = getHeaderFunc(0);
                        
                        if (headerPtr == IntPtr.Zero)
                        {
                            Console.WriteLine($"  ❌ Header function returned null pointer");
                            return;
                        }
                        
                        Console.WriteLine($"  ✅ Header function returned pointer: {headerPtr}");
                        
                        // 4. Try to read the header structure
                        Console.WriteLine($"  🔍 Reading header structure...");
                        try
                        {
                            var header = Marshal.PtrToStructure<WinampVisHeader>(headerPtr);
                            Console.WriteLine($"  ✅ Header read successfully:");
                            Console.WriteLine($"     Version: {header.Version:X}");
                            Console.WriteLine($"     Description: {header.Description}");
                            Console.WriteLine($"     GetModuleFunc: {header.GetModuleFunc}");
                            
                            // 5. Try to get modules
                            if (header.GetModuleFunc != IntPtr.Zero)
                            {
                                Console.WriteLine($"  🔍 Getting modules...");
                                var getModuleFunc = Marshal.GetDelegateForFunctionPointer<GetModuleDelegate>(header.GetModuleFunc);
                                
                                int moduleCount = 0;
                                for (int i = 0; i < 10; i++) // Limit to 10 modules
                                {
                                    var modulePtr = getModuleFunc(i);
                                    if (modulePtr == IntPtr.Zero) break;
                                    
                                    try
                                    {
                                        var module = Marshal.PtrToStructure<WinampVisModule>(modulePtr);
                                        if (string.IsNullOrEmpty(module.Description)) break;
                                        
                                        Console.WriteLine($"    ✅ Module {i}: {module.Description}");
                                        moduleCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"    ❌ Error reading module {i}: {ex.Message}");
                                        break;
                                    }
                                }
                                
                                Console.WriteLine($"  📊 Total modules found: {moduleCount}");
                            }
                            else
                            {
                                Console.WriteLine($"  ❌ GetModuleFunc is null");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  ❌ Error reading header structure: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ❌ Error calling header function: {ex.Message}");
                    }
                }
                finally
                {
                    // Clean up
                    FreeLibrary(libraryHandle);
                    Console.WriteLine($"  🧹 DLL unloaded");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Unexpected error: {ex.Message}");
                Console.WriteLine($"  ❌ Stack trace: {ex.StackTrace}");
            }
        }
    }
}
