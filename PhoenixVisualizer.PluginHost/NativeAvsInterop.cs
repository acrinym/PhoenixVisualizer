using System.Runtime.InteropServices;

namespace PhoenixVisualizer.PluginHost;

// Interop for Winamp Visualization SDK (vis.h)
// We only need header + getModule for now to enumerate and init later.

internal static class NativeAvsInterop
{
    // winampVisHeader* winampVisGetHeader();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint WinampVisGetHeaderDelegate();

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WinampVisHeader
    {
        public int version;                 // version == 0x00010001 for classic SDK
        public nint description;            // char*
        public nint getModule;              // winampVisModule* (*getModule)(int)
        public int numMods;                 // number of modules
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint GetModuleDelegate(int index); // returns winampVisModule*

    // Partial view of winampVisModule (vis.h) sufficient to Init/Render/Quit
    [StructLayout(LayoutKind.Sequential)]
    internal struct WinampVisModule
    {
        public nint description;            // char*
        public nint hwndParent;             // HWND
        public nint hDllInstance;           // HINSTANCE

        public int sRate;                   // sample rate (to be set)
        public int nCh;                      // channels    (to be set)
        public int latencyMs;
        public int delayMs;
        public int spectrumNch;
        public int waveformNch;

        public nint spectrumData;           // byte[2][576] (opaque here)
        public nint waveformData;           // byte[2][576] (opaque here)

                 public nint Config;                 // void (*Config)(module*)
         public nint Init;                   // int  (*Init)(module*)
         public nint Render;                 // int  (*Render)(module*)
         public nint Quit;                   // void (*Quit)(module*)
     }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ModuleVoidFn(nint module);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ModuleIntFn(nint module);
}
