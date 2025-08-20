using System.Runtime.InteropServices;
using PhoenixVisualizer.Core.Diagnostics;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Windows-only: load vis_avs.dll, enumerate modules, stage presets.
/// Pass 3 will embed AVS via HWND + drive Init/Render/Quit.
/// </summary>
public static class NativeAvsHost
{
#if WINDOWS
    public static bool IsSupported =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
    public static bool IsSupported => false;
#endif

    private static nint _lib;
    private static NativeAvsInterop.WinampVisHeader _hdr;
    private static NativeAvsInterop.GetModuleDelegate? _getModule;
    private static NativeAvsInterop.WinampVisGetHeaderDelegate? _getHeader;

    /// <summary>Try to load vis_avs.dll (from given path or PATH). Safe, idempotent.</summary>
    public static bool TryLoad(out string message, string? path = null)
    {
        message = string.Empty;
        if (!IsSupported)
        {
            message = "Native AVS is Windows-only.";
            return false;
        }
        if (_lib != 0)
        {
            message = "vis_avs already loaded.";
            return true;
        }
        var dll = string.IsNullOrWhiteSpace(path) ? "vis_avs.dll" : path!;
        if (!NativeLibrary.TryLoad(dll, out _lib))
        {
            message = $"vis_avs.dll not found (tried '{dll}').";
            return false;
        }
        try
        {
            var fp = NativeLibrary.GetExport(_lib, "winampVisGetHeader");
            _getHeader = Marshal.GetDelegateForFunctionPointer<NativeAvsInterop.WinampVisGetHeaderDelegate>(fp);
            var hdrPtr = _getHeader();
            _hdr = Marshal.PtrToStructure<NativeAvsInterop.WinampVisHeader>(hdrPtr);
            _getModule = Marshal.GetDelegateForFunctionPointer<NativeAvsInterop.GetModuleDelegate>(_hdr.getModule);
            message = $"✅ Loaded vis_avs.dll • modules: {_hdr.numMods}";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to bind vis_avs: {ex.Message}";
            SafeFree();
            return false;
        }
    }

    /// <summary>Enumerate module descriptions (e.g., "Advanced Visualization Studio")</summary>
    public static string[] ListModules()
    {
        if (_lib == 0 || _getModule is null) return Array.Empty<string>();
        var list = new List<string>();
        for (int i = 0; i < _hdr.numMods; i++)
        {
            try
            {
                var modPtr = _getModule(i);
                if (modPtr == 0) continue;
                var mod = Marshal.PtrToStructure<NativeAvsInterop.WinampVisModule>(modPtr);
                var desc = Marshal.PtrToStringAnsi(mod.description) ?? $"Module {i}";
                list.Add(desc);
            }
            catch { /* ignore single module errors */ }
        }
        return list.ToArray();
    }

    /// <summary>Stage preset bytes to a temp file for AVS to load from disk.</summary>
    public static string StagePreset(byte[] presetBytes)
    {
        var dir = Path.Combine(Path.GetTempPath(), "PhoenixVisualizer", "avs");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"pv_{Guid.NewGuid():N}.avs");
        File.WriteAllBytes(file, presetBytes);
        return file;
    }

    public static void SafeFree()
    {
        try
        {
            _getModule = null;
            _getHeader = null;
            if (_lib != 0) NativeLibrary.Free(_lib);
        }
        catch { /* ignored */ }
        finally { _lib = 0; }
    }
}
