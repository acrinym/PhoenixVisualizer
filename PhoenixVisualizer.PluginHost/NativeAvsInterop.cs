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

    // We don't marshal the full module yet; we just keep a pointer to pass to Init/Render in next pass.
    // (AVS creates its own child window off hwndParent, so we will embed via HWND next.)
    [StructLayout(LayoutKind.Sequential)]
    internal struct WinampVisModule // partial view for description/hwnd/funcs
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
}
