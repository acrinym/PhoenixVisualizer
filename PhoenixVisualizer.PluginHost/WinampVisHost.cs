using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Winamp Visualizer Plugin Host
/// Loads and manages actual Winamp visualizer plugins using BASS_WA extension
/// </summary>
public sealed class WinampVisHost : IDisposable
{
    // Winamp visualizer plugin structures (matching the C++ SDK)
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

    [StructLayout(LayoutKind.Sequential)]
    public struct WinampVisHeader
    {
        public int Version;
        [MarshalAs(UnmanagedType.LPStr)]
        public string Description;
        public IntPtr GetModuleFunc;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WinampPluginProps
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string FilePath;
        [MarshalAs(UnmanagedType.LPStr)]
        public string Extension;
        [MarshalAs(UnmanagedType.LPStr)]
        public string FileName;
        public uint NumberOfModules;
        public IntPtr HDll;
        public IntPtr Module;
    }

    // BASS_WA function delegates
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool LoadVisPluginDelegate(string path);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void StartVisDelegate(int pluginIndex, int moduleIndex);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void StopVisDelegate(int pluginIndex);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void ConfigVisDelegate(int pluginIndex, int moduleIndex);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate uint GetModuleCountDelegate(int pluginIndex);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntPtr GetModuleInfoDelegate(int pluginIndex, int moduleIndex);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate uint GetPluginCountDelegate();
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntPtr GetPluginInfoDelegate(int pluginIndex);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void SetHwndDelegate(IntPtr hwnd);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntPtr GetVisHwndDelegate();
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void SetSongTitleDelegate(string title);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void SetElapsedDelegate(int elapsed);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void SetLengthDelegate(int length);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void SetPlayingDelegate(int isPlaying);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void SetModuleDelegate(int moduleIndex);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void SetChannelDelegate(int channel);

    // Plugin management
    private readonly List<WinampPluginProps> _loadedPlugins = new();
    private readonly Dictionary<int, WinampVisModule> _activeModules = new();
    private readonly string _pluginDirectory;
    private IntPtr _bassWaHandle;
    private bool _disposed;

    // BASS_WA function pointers
    private LoadVisPluginDelegate? _loadVisPlugin;
    private StartVisDelegate? _startVis;
    private StopVisDelegate? _stopVis;
    private ConfigVisDelegate? _configVis;
    private GetModuleCountDelegate? _getModuleCount;
    private GetModuleInfoDelegate? _getModuleInfo;
    private GetPluginCountDelegate? _getPluginCount;
    private GetPluginInfoDelegate? _getPluginInfo;
    private SetHwndDelegate? _setHwnd;
    private GetVisHwndDelegate? _getVisHwnd;
    private SetSongTitleDelegate? _setSongTitle;
    private SetElapsedDelegate? _setElapsed;
    private SetLengthDelegate? _setLength;
    private SetPlayingDelegate? _setPlaying;
    private SetModuleDelegate? _setModule;
    private SetChannelDelegate? _setChannel;

    public WinampVisHost(string pluginDirectory = "")
    {
        _pluginDirectory = string.IsNullOrEmpty(pluginDirectory) 
            ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vis") 
            : pluginDirectory;
        
        InitializeBassWa();
    }

    private void InitializeBassWa()
    {
        try
        {
            // Try to load BASS_WA from the libs directory
            var bassWaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", "bass_wa.dll");
            if (!File.Exists(bassWaPath))
            {
                // Try alternative locations
                bassWaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bass_wa.dll");
            }

            if (!File.Exists(bassWaPath))
            {
                throw new FileNotFoundException("BASS_WA.dll not found. Please ensure it's in the libs directory.");
            }

            _bassWaHandle = LoadLibrary(bassWaPath);
            if (_bassWaHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to load BASS_WA.dll: {GetLastError()}");
            }

            // Load all the function pointers
            LoadBassWaFunctions();
            
            Console.WriteLine("[WinampVisHost] BASS_WA initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Failed to initialize BASS_WA: {ex.Message}");
            throw;
        }
    }

    private void LoadBassWaFunctions()
    {
        _loadVisPlugin = GetFunction<LoadVisPluginDelegate>("BASS_WA_LoadVisPlugin");
        _startVis = GetFunction<StartVisDelegate>("BASS_WA_Start_Vis");
        _stopVis = GetFunction<StopVisDelegate>("BASS_WA_Stop_Vis");
        _configVis = GetFunction<ConfigVisDelegate>("BASS_WA_Config_Vis");
        _getModuleCount = GetFunction<GetModuleCountDelegate>("BASS_WA_GetModuleCount");
        _getModuleInfo = GetFunction<GetModuleInfoDelegate>("BASS_WA_GetModuleInfo");
        _getPluginCount = GetFunction<GetPluginCountDelegate>("BASS_WA_GetWinampPluginCount");
        _getPluginInfo = GetFunction<GetPluginInfoDelegate>("BASS_WA_GetWinampPluginInfo");
        _setHwnd = GetFunction<SetHwndDelegate>("BASS_WA_SetHwnd");
        _getVisHwnd = GetFunction<GetVisHwndDelegate>("BASS_WA_GetVisHwnd");
        _setSongTitle = GetFunction<SetSongTitleDelegate>("BASS_WA_SetSongTitle");
        _setElapsed = GetFunction<SetElapsedDelegate>("BASS_WA_SetElapsed");
        _setLength = GetFunction<SetLengthDelegate>("BASS_WA_SetLength");
        _setPlaying = GetFunction<SetPlayingDelegate>("BASS_WA_IsPlaying");
        _setModule = GetFunction<SetModuleDelegate>("BASS_WA_SetModule");
        _setChannel = GetFunction<SetChannelDelegate>("BASS_WA_SetChannel");
    }

    private T GetFunction<T>(string functionName) where T : Delegate
    {
        var ptr = GetProcAddress(_bassWaHandle, functionName);
        if (ptr == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Function {functionName} not found in BASS_WA.dll");
        }
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    /// <summary>
    /// Scan for and load available Winamp visualizer plugins
    /// </summary>
    public void ScanForPlugins()
    {
        if (_disposed) return;

        try
        {
            if (!Directory.Exists(_pluginDirectory))
            {
                Directory.CreateDirectory(_pluginDirectory);
                Console.WriteLine($"[WinampVisHost] Created plugin directory: {_pluginDirectory}");
                return;
            }

            var pluginFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            Console.WriteLine($"[WinampVisHost] Found {pluginFiles.Length} potential plugin files");

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    if (_loadVisPlugin?.Invoke(pluginFile) == true)
                    {
                        Console.WriteLine($"[WinampVisHost] Successfully loaded plugin: {Path.GetFileName(pluginFile)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WinampVisHost] Failed to load plugin {pluginFile}: {ex.Message}");
                }
            }

            // Get loaded plugin information
            RefreshPluginList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Error scanning for plugins: {ex.Message}");
        }
    }

    private void RefreshPluginList()
    {
        _loadedPlugins.Clear();
        
        try
        {
            var pluginCount = _getPluginCount?.Invoke() ?? 0;
            Console.WriteLine($"[WinampVisHost] Found {pluginCount} loaded plugins");

            for (uint i = 0; i < pluginCount; i++)
            {
                if (_getPluginInfo != null)
                {
                    var pluginInfo = _getPluginInfo.Invoke((int)i);
                    if (pluginInfo != IntPtr.Zero)
                    {
                        var props = Marshal.PtrToStructure<WinampPluginProps>(pluginInfo);
                        _loadedPlugins.Add(props);
                        Console.WriteLine($"[WinampVisHost] Plugin {i}: {props.FileName} ({props.NumberOfModules} modules)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Error refreshing plugin list: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all available plugins
    /// </summary>
    public IReadOnlyList<WinampPluginProps> GetAvailablePlugins()
    {
        return _loadedPlugins.AsReadOnly();
    }

    /// <summary>
    /// Start a visualizer plugin
    /// </summary>
    public bool StartVisualizer(int pluginIndex, int moduleIndex = 0)
    {
        if (_disposed) return false;

        try
        {
            _startVis?.Invoke(pluginIndex, moduleIndex);
            Console.WriteLine($"[WinampVisHost] Started visualizer plugin {pluginIndex}, module {moduleIndex}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Failed to start visualizer: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stop a visualizer plugin
    /// </summary>
    public void StopVisualizer(int pluginIndex)
    {
        if (_disposed) return;

        try
        {
            _stopVis?.Invoke(pluginIndex);
            Console.WriteLine($"[WinampVisHost] Stopped visualizer plugin {pluginIndex}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Failed to stop visualizer: {ex.Message}");
        }
    }

    /// <summary>
    /// Configure a visualizer plugin
    /// </summary>
    public void ConfigureVisualizer(int pluginIndex, int moduleIndex = 0)
    {
        if (_disposed) return;

        try
        {
            _configVis?.Invoke(pluginIndex, moduleIndex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Failed to configure visualizer: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the parent window for visualizers
    /// </summary>
    public void SetParentWindow(IntPtr hwnd)
    {
        if (_disposed) return;

        try
        {
            _setHwnd?.Invoke(hwnd);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Failed to set parent window: {ex.Message}");
        }
    }

    /// <summary>
    /// Update song information for visualizers
    /// </summary>
    public void UpdateSongInfo(string title, int elapsed, int length, bool isPlaying)
    {
        if (_disposed) return;

        try
        {
            _setSongTitle?.Invoke(title);
            _setElapsed?.Invoke(elapsed);
            _setLength?.Invoke(length);
            _setPlaying?.Invoke(isPlaying ? 1 : 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Failed to update song info: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the audio channel for visualizers
    /// </summary>
    public void SetAudioChannel(int channel)
    {
        if (_disposed) return;

        try
        {
            _setChannel?.Invoke(channel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Failed to set audio channel: {ex.Message}");
        }
    }

    // P/Invoke declarations
    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll")]
    private static extern int GetLastError();

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hModule);

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // Stop all active visualizers
            foreach (var plugin in _loadedPlugins)
            {
                StopVisualizer(_loadedPlugins.IndexOf(plugin));
            }

            // Free the BASS_WA library
            if (_bassWaHandle != IntPtr.Zero)
            {
                FreeLibrary(_bassWaHandle);
                _bassWaHandle = IntPtr.Zero;
            }

            _disposed = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampVisHost] Error during disposal: {ex.Message}");
        }
    }
}
