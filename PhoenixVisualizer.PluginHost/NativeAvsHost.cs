using System.Runtime.InteropServices;
using PhoenixVisualizer.Core.Diagnostics;
using System.Threading;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Windows-only: load vis_avs.dll, enumerate modules, stage presets.
/// PASS 3: embed AVS via HWND + drive Init/Render/Quit.
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
    private static nint _activeModule;
    private static NativeAvsInterop.ModuleIntFn? _initFn;
    private static NativeAvsInterop.ModuleIntFn? _renderFn;
    private static NativeAvsInterop.ModuleVoidFn? _quitFn;
    private static Timer? _renderTimer;

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

    /// <summary>
    /// Initialize the first module and parent its window to the provided HWND. Starts a 60 FPS render loop.
    /// </summary>
    public static bool Start(nint hwndParent, string stagedPresetPath, out string message, int sampleRate = 44100, int channels = 2)
    {
        message = string.Empty;
        if (_lib == 0 || _getModule is null)
        {
            message = "vis_avs not loaded.";
            return false;
        }
        Stop(); // ensure previous instance is closed
        try
        {
            var modPtr = _getModule(0);
            if (modPtr == 0) { message = "No AVS module found."; return false; }

            // Marshal module and set fields
            var mod = Marshal.PtrToStructure<NativeAvsInterop.WinampVisModule>(modPtr);
            // Set parent HWND / audio params
            unsafe
            {
                var p = (NativeAvsInterop.WinampVisModule*)modPtr;
                p->hwndParent = hwndParent;
                p->sRate = sampleRate;
                p->nCh = channels;
            }

            // Bind entry points
            _initFn = Marshal.GetDelegateForFunctionPointer<NativeAvsInterop.ModuleIntFn>(mod.Init);
            _renderFn = Marshal.GetDelegateForFunctionPointer<NativeAvsInterop.ModuleIntFn>(mod.Render);
            _quitFn = Marshal.GetDelegateForFunctionPointer<NativeAvsInterop.ModuleVoidFn>(mod.Quit);
            _activeModule = modPtr;

            // Hint AVS to load preset file: AVS watches its own UI/ini, so simplest path is to set its working dir
            // and let the user open config. For now we just run the default module; advanced preset injection will follow.
            var ok = _initFn(_activeModule) != 0;
            if (!ok) { message = "AVS Init() returned 0."; Stop(); return false; }

            // 60 FPS render loop
            _renderTimer = new Timer(_ =>
            {
                try { _ = _renderFn?.Invoke(_activeModule); }
                catch { /* swallow frame errors */ }
            }, null, dueTime: 0, period: 16);

            message = "✅ AVS initialized (HWND embedded).";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to start AVS: {ex.Message}";
            Stop();
            return false;
        }
    }

    public static void Stop()
    {
        try
        {
            _renderTimer?.Dispose();
            _renderTimer = null;
            if (_activeModule != 0 && _quitFn is not null)
            {
                try { _quitFn(_activeModule); } catch { /* ignore */ }
            }
        }
        finally
        {
            _activeModule = 0;
            _initFn = null; _renderFn = null; _quitFn = null;
        }
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
            Stop();
            if (_lib != 0) NativeLibrary.Free(_lib);
        }
        catch { /* ignored */ }
        finally { _lib = 0; }
    }
}
