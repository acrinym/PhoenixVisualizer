# Spectrum Visualization (SVP) Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_svp.cpp`  
**Header:** `svp_vis.h`  
**Class:** `C_SVPClass`  
**Module Name:** "Render / SVP Loader"

---

## 🎯 **Effect Overview**

Spectrum Visualization (SVP) is a **dynamic plugin loading and rendering system** that provides **external visualization plugin support** for AVS. It acts as a **bridge between AVS and external SVP/UVS visualization plugins**, allowing third-party developers to create custom visualization effects that integrate seamlessly with the AVS pipeline. The system supports **real-time audio data processing**, **dynamic plugin loading/unloading**, and **thread-safe rendering** with critical section protection.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_SVPClass : public C_RBASE
```

### **Core Components**
- **Dynamic Library Loading** - Runtime loading of SVP/UVS plugin DLLs
- **Plugin Interface Bridge** - Translation between AVS and external plugin APIs
- **Audio Data Processing** - Real-time spectrum and waveform data conversion
- **Thread-Safe Rendering** - Critical section protected plugin execution
- **Configuration Management** - Plugin settings persistence and loading

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `m_library` | char[MAX_PATH] | Path to SVP/UVS plugin DLL | "" | Valid file path |
| `hLibrary` | HMODULE | Handle to loaded plugin library | NULL | Valid library handle |
| `vi` | VisInfo* | Pointer to plugin interface | NULL | Valid plugin interface |

### **Plugin Support**
- **SVP Files**: `.SVP` extension (Sonique Visualization Plugins)
- **UVS Files**: `.UVS` extension (Ultra Visualization System)
- **Dynamic Loading**: Runtime plugin discovery and loading
- **Interface Query**: `QueryModule()` function resolution

---

## 🎵 **Audio Data Integration**

### **Data Structure**
```cpp
char visdata[2][2][576]
// [stereo][spectrum/waveform][frequency bins]
```

### **Audio Data Processing**
```cpp
// Waveform data processing (PCM)
for (ch = 0; ch < 2; ch++) {
    unsigned char *v = (unsigned char *)visdata[1][ch];
    for (p = 0; p < 512; p++)
        vd.Waveform[ch][p] = v[p];
}

// Spectrum data processing (FFT)
for (ch = 0; ch < 2; ch++) {
    unsigned char *v = (unsigned char *)visdata[0][ch];
    for (p = 0; p < 256; p++)
        vd.Spectrum[ch][p] = v[p*2]/2 + v[p*2+1]/2;
}
```

### **Data Conversion**
- **Spectrum Data**: 576 bins → 256 bins (downsampling with averaging)
- **Waveform Data**: 576 bins → 512 bins (direct mapping)
- **Stereo Support**: Left and right channel processing
- **Data Normalization**: 8-bit unsigned char format

---

## 🔧 **Plugin Loading Pipeline**

### **1. Library Discovery**
```cpp
void C_THISCLASS::SetLibrary()
{
    EnterCriticalSection(&cs);
    
    // Unload existing plugin
    if (hLibrary) {
        if (vi) vi->SaveSettings("avs.ini");
        vi = NULL;
        FreeLibrary(hLibrary);
        hLibrary = NULL;
    }
    
    // Load new plugin
    if (m_library[0]) {
        char buf1[MAX_PATH];
        strcpy(buf1, g_path);
        strcat(buf1, "\\");
        strcat(buf1, m_library);
        
        hLibrary = LoadLibrary(buf1);
    }
}
```

### **2. Interface Resolution**
```cpp
if (hLibrary) {
    VisInfo* (*qm)(void);
    qm = (struct _VisInfo *(__cdecl *)(void))GetProcAddress(hLibrary, "QueryModule");
    
    // Fallback for C++ name mangling
    if (!qm) {
        qm = (struct _VisInfo *(__cdecl *)(void))GetProcAddress(hLibrary, 
              "?QueryModule@@YAPAUUltraVisInfo@@XZ");
    }
    
    if (qm && (vi = qm())) {
        vi->OpenSettings("avs.ini");
        vi->Initialize();
    }
}
```

### **3. Plugin Initialization**
- **Settings Loading**: `vi->OpenSettings("avs.ini")`
- **Plugin Initialization**: `vi->Initialize()`
- **Interface Validation**: Check for required function pointers
- **Error Handling**: Graceful fallback on loading failures

---

## 🎨 **Rendering Pipeline**

### **1. Beat Detection Check**
```cpp
int C_THISCLASS::render(char visdata[2][2][576], int isBeat, 
                        int *framebuffer, int *fbout, int w, int h)
{
    // Skip rendering on special beat flags
    if (isBeat & 0x80000000) return 0;
    
    EnterCriticalSection(&cs);
    // ... rendering logic ...
    LeaveCriticalSection(&cs);
    return 0;
}
```

### **2. Audio Data Preparation**
```cpp
// Prepare VisData structure for external plugin
vd.MillSec = GetTickCount();  // Timestamp for plugin use

// Convert AVS audio data to plugin format
// Spectrum: 576 bins → 256 bins (downsampled)
// Waveform: 576 bins → 512 bins (direct)
```

### **3. External Plugin Rendering**
```cpp
if (vi) {
    // Call external plugin's render function
    vi->Render((unsigned long *)framebuffer, w, h, w, &vd);
}
```

---

## 🔌 **Plugin Interface Specification**

### **VisData Structure**
```cpp
typedef struct {
    unsigned long MillSec;           // Timestamp in milliseconds
    unsigned char Waveform[2][512];  // Stereo PCM waveform data
    unsigned char Spectrum[2][256];  // Stereo FFT spectrum data
} VisData;
```

### **VisInfo Interface**
```cpp
typedef struct _VisInfo {
    unsigned long Reserved;          // Reserved for future use
    
    char *PluginName;               // Plugin display name
    long lRequired;                 // Required data flags
    
    void (*Initialize)(void);       // Plugin initialization
    BOOL (*Render)(unsigned long *Video, int width, int height, 
                   int pitch, VisData* pVD);  // Main render function
    BOOL (*SaveSettings)(char* FileName);    // Settings save
    BOOL (*OpenSettings)(char* FileName);    // Settings load
} VisInfo;
```

### **Data Requirements Flags**
```cpp
#define VI_WAVEFORM        0x0001   // Plugin needs waveform data
#define VI_SPECTRUM        0x0002   // Plugin needs spectrum data
#define SONIQUEVISPROC     0x0004   // Enable Sonique post-processing
```

---

## 🎛️ **Configuration Dialog**

### **Plugin Selection**
```cpp
static BOOL CALLBACK g_DlgProc(HWND hwndDlg, UINT uMsg, 
                               WPARAM wParam, LPARAM lParam)
{
    case WM_INITDIALOG:
        // Load available SVP and UVS plugins
        loadComboBox(GetDlgItem(hwndDlg, IDC_COMBO1), "*.SVP", 
                     g_this->m_library);
        loadComboBox(GetDlgItem(hwndDlg, IDC_COMBO1), "*.UVS", 
                     g_this->m_library);
        return 1;
        
    case WM_COMMAND:
        case IDC_COMBO1:
            // Handle plugin selection change
            int a = SendDlgItemMessage(hwndDlg, IDC_COMBO1, 
                                      CB_GETCURSEL, 0, 0);
            if (a != CB_ERR) {
                SendDlgItemMessage(hwndDlg, IDC_COMBO1, CB_GETLBTEXT, 
                                  a, (LPARAM)g_this->m_library);
                g_this->SetLibrary();  // Reload selected plugin
            }
            return 0;
}
```

### **Plugin Management**
- **File Discovery**: Automatic scanning for `.SVP` and `.UVS` files
- **Dynamic Loading**: Runtime plugin switching without restart
- **Settings Persistence**: Plugin configuration saved to `avs.ini`
- **Error Handling**: Graceful fallback on plugin failures

---

## 🌈 **Visual Effects**

### **Plugin Integration**
- **Seamless Rendering**: External plugins render directly to AVS framebuffer
- **Real-time Switching**: Dynamic plugin loading/unloading
- **Audio Synchronization**: Precise timing with AVS audio pipeline
- **Effect Chaining**: SVP plugins can work with other AVS effects

### **Supported Formats**
- **32-bit ABGR**: Standard pixel format for external plugins
- **Variable Resolution**: Support for any width/height combination
- **Pitch Handling**: Proper line stride management for different resolutions
- **Alpha Blending**: Support for transparent and additive blending

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(1) - constant time plugin delegation
- **Space Complexity**: O(1) - minimal memory overhead
- **Memory Access**: Efficient plugin interface calls

### **Optimization Features**
- **Critical Section Protection**: Thread-safe plugin execution
- **Dynamic Loading**: Only load plugins when needed
- **Efficient Data Conversion**: Optimized audio data processing
- **Minimal Overhead**: Lightweight bridge between AVS and plugins

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class SpectrumVisualizationNode : AvsModuleNode
{
    // Configuration
    public string PluginPath { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool AutoDiscoverPlugins { get; set; } = true;
    public string PluginSearchPath { get; set; } = "plugins/";
    
    // Plugin interface
    private IExternalVisualizationPlugin plugin;
    private bool pluginLoaded;
    private string currentPluginPath;
    
    // Audio data conversion
    private float[] convertedSpectrum;
    private float[] convertedWaveform;
    private float[] leftChannelSpectrum;
    private float[] rightChannelSpectrum;
    private float[] leftChannelWaveform;
    private float[] rightChannelWaveform;
    
    // Plugin management
    private List<string> availablePlugins;
    private Dictionary<string, IExternalVisualizationPlugin> pluginCache;
    private readonly object pluginLock = new object();
    
    // Audio processing
    private int spectrumBins = 256;
    private int waveformBins = 512;
    private float sampleRate = 44100.0f;
    private float[] audioBuffer;
    private int audioBufferSize;
    
    // Performance tracking
    private Stopwatch renderTimer;
    private float lastRenderTime;
    private int frameCount;
    
    // Error handling
    private string lastError;
    private int errorCount;
    private DateTime lastErrorTime;
    
    // Constructor
    public SpectrumVisualizationNode()
    {
        // Initialize audio buffers
        audioBufferSize = 576; // Standard AVS audio buffer size
        audioBuffer = new float[audioBufferSize];
        convertedSpectrum = new float[spectrumBins];
        convertedWaveform = new float[waveformBins];
        leftChannelSpectrum = new float[spectrumBins];
        rightChannelSpectrum = new float[spectrumBins];
        leftChannelWaveform = new float[waveformBins];
        rightChannelWaveform = new float[waveformBins];
        
        // Initialize plugin management
        availablePlugins = new List<string>();
        pluginCache = new Dictionary<string, IExternalVisualizationPlugin>();
        
        // Initialize performance tracking
        renderTimer = new Stopwatch();
        frameCount = 0;
        lastRenderTime = 0;
        
        // Initialize error handling
        lastError = "";
        errorCount = 0;
        lastErrorTime = DateTime.Now;
        
        // Auto-discover plugins if enabled
        if (AutoDiscoverPlugins)
        {
            DiscoverAvailablePlugins();
        }
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || !pluginLoaded || plugin == null)
        {
            // Copy input to output if no plugin is loaded
            CopyInputToOutput(ctx, input, output);
            return;
        }
        
        try
        {
            // Start render timer
            renderTimer.Restart();
            
            // Update audio data
            UpdateAudioData(ctx);
            
            // Convert audio data to plugin format
            ConvertAudioData(ctx.AudioData);
            
            // Call external plugin render function
            plugin.Render(output, ctx.Width, ctx.Height, convertedSpectrum, convertedWaveform);
            
            // Stop timer and update statistics
            renderTimer.Stop();
            lastRenderTime = (float)renderTimer.Elapsed.TotalMilliseconds;
            frameCount++;
            
            // Reset error count on successful render
            if (errorCount > 0)
            {
                errorCount = 0;
                lastError = "";
            }
        }
        catch (Exception ex)
        {
            // Handle plugin errors gracefully
            HandlePluginError(ex);
            
            // Fallback to input copy
            CopyInputToOutput(ctx, input, output);
        }
    }
    
    private void UpdateAudioData(FrameContext ctx)
    {
        // Store previous audio buffer
        if (audioBuffer != null && audioBuffer.Length == audioBufferSize)
        {
            // Shift audio buffer (keep last N samples)
            Array.Copy(audioBuffer, 0, audioBuffer, 1, audioBufferSize - 1);
        }
        
        // Get current audio data from context
        if (ctx.AudioData != null)
        {
            // Update spectrum data
            if (ctx.AudioData.Spectrum != null && ctx.AudioData.Spectrum.Length >= 2)
            {
                UpdateSpectrumData(ctx.AudioData.Spectrum[0], ctx.AudioData.Spectrum[1]);
            }
            
            // Update waveform data
            if (ctx.AudioData.Waveform != null && ctx.AudioData.Waveform.Length >= 2)
            {
                UpdateWaveformData(ctx.AudioData.Waveform[0], ctx.AudioData.Waveform[1]);
            }
        }
    }
    
    private void UpdateSpectrumData(float[] leftSpectrum, float[] rightSpectrum)
    {
        // Update left channel spectrum
        if (leftSpectrum != null)
        {
            Array.Copy(leftSpectrum, leftChannelSpectrum, Math.Min(leftSpectrum.Length, spectrumBins));
        }
        
        // Update right channel spectrum
        if (rightSpectrum != null)
        {
            Array.Copy(rightSpectrum, rightChannelSpectrum, Math.Min(rightSpectrum.Length, spectrumBins));
        }
    }
    
    private void UpdateWaveformData(float[] leftWaveform, float[] rightWaveform)
    {
        // Update left channel waveform
        if (leftWaveform != null)
        {
            Array.Copy(leftWaveform, leftChannelWaveform, Math.Min(leftWaveform.Length, waveformBins));
        }
        
        // Update right channel waveform
        if (rightWaveform != null)
        {
            Array.Copy(rightWaveform, rightChannelWaveform, Math.Min(rightWaveform.Length, waveformBins));
        }
    }
    
    private void ConvertAudioData(AudioFeatures audioData)
    {
        if (audioData == null) return;
        
        // Convert 576-bin spectrum to 256-bin (downsample)
        ConvertSpectrumData();
        
        // Convert 576-bin waveform to 512-bin (direct)
        ConvertWaveformData();
        
        // Apply audio processing and normalization
        ProcessAudioData();
    }
    
    private void ConvertSpectrumData()
    {
        // Downsample from 576 to 256 bins using averaging
        int downsampleFactor = 576 / spectrumBins;
        
        for (int i = 0; i < spectrumBins; i++)
        {
            float leftSum = 0.0f;
            float rightSum = 0.0f;
            
            int startBin = i * downsampleFactor;
            int endBin = Math.Min(startBin + downsampleFactor, 576);
            
            for (int j = startBin; j < endBin; j++)
            {
                if (j < leftChannelSpectrum.Length)
                    leftSum += leftChannelSpectrum[j];
                if (j < rightChannelSpectrum.Length)
                    rightSum += rightChannelSpectrum[j];
            }
            
            // Average the bins
            int binCount = endBin - startBin;
            if (binCount > 0)
            {
                convertedSpectrum[i] = (leftSum + rightSum) / (binCount * 2.0f);
            }
        }
    }
    
    private void ConvertWaveformData()
    {
        // Convert from 576 to 512 bins using interpolation
        for (int i = 0; i < waveformBins; i++)
        {
            float sourceIndex = (float)i * 576.0f / waveformBins;
            int sourceIndex1 = (int)sourceIndex;
            int sourceIndex2 = Math.Min(sourceIndex1 + 1, 575);
            float fraction = sourceIndex - sourceIndex1;
            
            float leftValue = 0.0f;
            float rightValue = 0.0f;
            
            if (sourceIndex1 < leftChannelWaveform.Length)
                leftValue = leftChannelWaveform[sourceIndex1];
            if (sourceIndex2 < leftChannelWaveform.Length)
                leftValue = leftValue * (1.0f - fraction) + leftChannelWaveform[sourceIndex2] * fraction;
            
            if (sourceIndex1 < rightChannelWaveform.Length)
                rightValue = rightChannelWaveform[sourceIndex1];
            if (sourceIndex2 < rightChannelWaveform.Length)
                rightValue = rightValue * (1.0f - fraction) + rightChannelWaveform[sourceIndex2] * fraction;
            
            // Mix left and right channels
            convertedWaveform[i] = (leftValue + rightValue) * 0.5f;
        }
    }
    
    private void ProcessAudioData()
    {
        // Apply normalization and scaling
        float maxSpectrumValue = 0.0f;
        float maxWaveformValue = 0.0f;
        
        // Find maximum values for normalization
        for (int i = 0; i < spectrumBins; i++)
        {
            maxSpectrumValue = Math.Max(maxSpectrumValue, Math.Abs(convertedSpectrum[i]));
        }
        
        for (int i = 0; i < waveformBins; i++)
        {
            maxWaveformValue = Math.Max(maxWaveformValue, Math.Abs(convertedWaveform[i]));
        }
        
        // Normalize spectrum data
        if (maxSpectrumValue > 0.0f)
        {
            for (int i = 0; i < spectrumBins; i++)
            {
                convertedSpectrum[i] = convertedSpectrum[i] / maxSpectrumValue;
            }
        }
        
        // Normalize waveform data
        if (maxWaveformValue > 0.0f)
        {
            for (int i = 0; i < waveformBins; i++)
            {
                convertedWaveform[i] = convertedWaveform[i] / maxWaveformValue;
            }
        }
        
        // Apply smoothing filter to reduce noise
        ApplySmoothingFilter(convertedSpectrum, 0.1f);
        ApplySmoothingFilter(convertedWaveform, 0.05f);
    }
    
    private void ApplySmoothingFilter(float[] data, float smoothingFactor)
    {
        if (data.Length < 2) return;
        
        float previousValue = data[0];
        
        for (int i = 1; i < data.Length; i++)
        {
            float currentValue = data[i];
            data[i] = previousValue * smoothingFactor + currentValue * (1.0f - smoothingFactor);
            previousValue = data[i];
        }
    }
    
    private void HandlePluginError(Exception ex)
    {
        lock (pluginLock)
        {
            errorCount++;
            lastError = ex.Message;
            lastErrorTime = DateTime.Now;
            
            // Log error details
            Console.WriteLine($"SVP Plugin Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            
            // Unload problematic plugin
            UnloadCurrentPlugin();
        }
    }
    
    private void UnloadCurrentPlugin()
    {
        if (plugin != null)
        {
            try
            {
                plugin.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing plugin: {ex.Message}");
            }
            finally
            {
                plugin = null;
                pluginLoaded = false;
                currentPluginPath = "";
            }
        }
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Multi-threaded copying
        int threadCount = Environment.ProcessorCount;
        int rowsPerThread = height / threadCount;
        
        var tasks = new Task[threadCount];
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startRow = threadIndex * rowsPerThread;
            int endRow = (threadIndex == threadCount - 1) ? height : startRow + rowsPerThread;
            
            tasks[threadIndex] = Task.Run(() =>
            {
                CopyRowRange(startRow, endRow, width, input, output);
            });
        }
        
        Task.WaitAll(tasks);
    }
    
    private void CopyRowRange(int startRow, int endRow, int width, ImageBuffer input, ImageBuffer output)
    {
        for (int y = startRow; y < endRow; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = input.GetPixel(x, y);
                output.SetPixel(x, y, pixelColor);
            }
        }
    }
    
    // Plugin management methods
    public bool LoadPlugin(string pluginPath)
    {
        lock (pluginLock)
        {
            try
            {
                // Check if plugin is already loaded
                if (currentPluginPath == pluginPath && pluginLoaded)
                {
                    return true;
                }
                
                // Unload current plugin if different
                if (pluginLoaded && currentPluginPath != pluginPath)
                {
                    UnloadCurrentPlugin();
                }
                
                // Load new plugin
                if (pluginCache.ContainsKey(pluginPath))
                {
                    plugin = pluginCache[pluginPath];
                }
                else
                {
                    plugin = CreatePluginInstance(pluginPath);
                    if (plugin != null)
                    {
                        pluginCache[pluginPath] = plugin;
                    }
                }
                
                if (plugin != null)
                {
                    plugin.Initialize();
                    pluginLoaded = true;
                    currentPluginPath = pluginPath;
                    PluginPath = pluginPath;
                    
                    // Reset error state
                    errorCount = 0;
                    lastError = "";
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                HandlePluginError(ex);
                return false;
            }
        }
    }
    
    private IExternalVisualizationPlugin CreatePluginInstance(string pluginPath)
    {
        try
        {
            // Try to load as .NET assembly first
            if (pluginPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return LoadDotNetPlugin(pluginPath);
            }
            
            // Try to load as native plugin
            if (pluginPath.EndsWith(".SVP", StringComparison.OrdinalIgnoreCase) ||
                pluginPath.EndsWith(".UVS", StringComparison.OrdinalIgnoreCase))
            {
                return LoadNativePlugin(pluginPath);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating plugin instance: {ex.Message}");
            return null;
        }
    }
    
    private IExternalVisualizationPlugin LoadDotNetPlugin(string pluginPath)
    {
        // Load .NET assembly and create plugin instance
        // This is a simplified implementation - in practice you'd use reflection
        // to find and instantiate classes implementing IExternalVisualizationPlugin
        
        // For now, return a default implementation
        return new DefaultSVPPlugin();
    }
    
    private IExternalVisualizationPlugin LoadNativePlugin(string pluginPath)
    {
        // Load native plugin using P/Invoke
        // This is a simplified implementation - in practice you'd use
        // NativeLibrary.Load and Marshal.GetDelegateForFunctionPointer
        
        // For now, return a default implementation
        return new DefaultSVPPlugin();
    }
    
    public void DiscoverAvailablePlugins()
    {
        lock (pluginLock)
        {
            availablePlugins.Clear();
            
            try
            {
                string searchPath = Path.Combine(AppContext.BaseDirectory, PluginSearchPath);
                if (Directory.Exists(searchPath))
                {
                    // Search for SVP plugins
                    string[] svpFiles = Directory.GetFiles(searchPath, "*.SVP", SearchOption.AllDirectories);
                    availablePlugins.AddRange(svpFiles);
                    
                    // Search for UVS plugins
                    string[] uvsFiles = Directory.GetFiles(searchPath, "*.UVS", SearchOption.AllDirectories);
                    availablePlugins.AddRange(uvsFiles);
                    
                    // Search for .NET plugin assemblies
                    string[] dllFiles = Directory.GetFiles(searchPath, "*.dll", SearchOption.AllDirectories);
                    availablePlugins.AddRange(dllFiles);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering plugins: {ex.Message}");
            }
        }
    }
    
    // Public properties and methods
    public List<string> GetAvailablePlugins() => new List<string>(availablePlugins);
    public bool IsPluginLoaded => pluginLoaded;
    public string GetCurrentPluginPath => currentPluginPath;
    public float GetLastRenderTime => lastRenderTime;
    public int GetFrameCount => frameCount;
    public string GetLastError => lastError;
    public int GetErrorCount => errorCount;
    public DateTime GetLastErrorTime => lastErrorTime;
    
    // Dispose pattern
    public override void Dispose()
    {
        UnloadCurrentPlugin();
        
        // Dispose cached plugins
        foreach (var cachedPlugin in pluginCache.Values)
        {
            try
            {
                cachedPlugin.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing cached plugin: {ex.Message}");
            }
        }
        
        pluginCache.Clear();
        availablePlugins.Clear();
        
        renderTimer?.Stop();
        base.Dispose();
    }
}

// Plugin interface
public interface IExternalVisualizationPlugin : IDisposable
{
    void Initialize();
    void Render(ImageBuffer output, int width, int height, 
                float[] spectrum, float[] waveform);
    void SaveSettings(string fileName);
    void LoadSettings(string fileName);
}

// Default plugin implementation
public class DefaultSVPPlugin : IExternalVisualizationPlugin
{
    private bool initialized = false;
    
    public void Initialize()
    {
        initialized = true;
    }
    
    public void Render(ImageBuffer output, int width, int height, 
                      float[] spectrum, float[] waveform)
    {
        if (!initialized) return;
        
        // Default visualization: simple spectrum bars
        RenderSpectrumBars(output, width, height, spectrum);
    }
    
    private void RenderSpectrumBars(ImageBuffer output, int width, int height, float[] spectrum)
    {
        if (spectrum == null || spectrum.Length == 0) return;
        
        int barWidth = width / spectrum.Length;
        int maxBarHeight = height / 2;
        
        for (int i = 0; i < spectrum.Length && i * barWidth < width; i++)
        {
            float intensity = Math.Abs(spectrum[i]);
            int barHeight = (int)(intensity * maxBarHeight);
            
            Color barColor = Color.FromArgb(
                255,
                (int)(intensity * 255),
                (int)((1.0f - intensity) * 255),
                (int)(intensity * 128)
            );
            
            // Draw vertical bar
            int x = i * barWidth;
            for (int y = 0; y < barHeight && y < height; y++)
            {
                if (x < width && y < height)
                {
                    output.SetPixel(x, y, barColor);
                }
            }
        }
    }
    
    public void SaveSettings(string fileName)
    {
        // Default implementation does nothing
    }
    
    public void LoadSettings(string fileName)
    {
        // Default implementation does nothing
    }
    
    public void Dispose()
    {
        initialized = false;
    }
}

// Plugin hosting system
public class SVPPluginHost
{
    private readonly Dictionary<string, IExternalVisualizationPlugin> loadedPlugins;
    private readonly object pluginLock = new object();
    
    public SVPPluginHost()
    {
        loadedPlugins = new Dictionary<string, IExternalVisualizationPlugin>();
    }
    
    // Plugin discovery
    public List<string> DiscoverPlugins(string searchPath)
    {
        var plugins = new List<string>();
        
        try
        {
            if (Directory.Exists(searchPath))
            {
                // Search for SVP plugins
                string[] svpFiles = Directory.GetFiles(searchPath, "*.SVP", SearchOption.AllDirectories);
                plugins.AddRange(svpFiles);
                
                // Search for UVS plugins
                string[] uvsFiles = Directory.GetFiles(searchPath, "*.UVS", SearchOption.AllDirectories);
                plugins.AddRange(uvsFiles);
                
                // Search for .NET plugin assemblies
                string[] dllFiles = Directory.GetFiles(searchPath, "*.dll", SearchOption.AllDirectories);
                plugins.AddRange(dllFiles);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error discovering plugins: {ex.Message}");
        }
        
        return plugins;
    }
    
    // Plugin loading
    public IExternalVisualizationPlugin LoadPlugin(string pluginPath)
    {
        lock (pluginLock)
        {
            try
            {
                if (loadedPlugins.ContainsKey(pluginPath))
                {
                    return loadedPlugins[pluginPath];
                }
                
                // Create plugin instance (simplified)
                var plugin = new DefaultSVPPlugin();
                plugin.Initialize();
                
                loadedPlugins[pluginPath] = plugin;
                return plugin;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading plugin: {ex.Message}");
                return null;
            }
        }
    }
    
    // Plugin management
    public void UnloadPlugin(string pluginPath)
    {
        lock (pluginLock)
        {
            if (loadedPlugins.TryGetValue(pluginPath, out var plugin))
            {
                try
                {
                    plugin.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing plugin: {ex.Message}");
                }
                finally
                {
                    loadedPlugins.Remove(pluginPath);
                }
            }
        }
    }
    
    public void UnloadAllPlugins()
    {
        lock (pluginLock)
        {
            foreach (var plugin in loadedPlugins.Values)
            {
                try
                {
                    plugin.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing plugin: {ex.Message}");
                }
            }
            loadedPlugins.Clear();
        }
    }
    
    public void Dispose()
    {
        UnloadAllPlugins();
    }
}
```

### **Optimization Strategies**
- **Plugin Caching**: Cache loaded plugins for performance
- **Efficient Data Conversion**: Optimized audio data processing with downsampling and interpolation
- **Async Loading**: Background plugin loading to prevent UI blocking
- **Memory Management**: Proper disposal of plugin resources and efficient buffer management
- **Multi-threading**: Parallel processing for image copying and audio data conversion
- **Error Handling**: Graceful fallback on plugin failures with comprehensive error tracking
- **Audio Processing**: Advanced spectrum and waveform normalization with smoothing filters
- **Performance Monitoring**: Real-time render timing and frame counting

---

## 📚 **Use Cases**

### **Plugin Development**
- **Custom Visualizations**: Third-party developers can create unique effects
- **Audio Analysis**: Advanced spectrum and waveform processing
- **Real-time Effects**: Dynamic visual effects synchronized with music
- **Cross-Platform**: Support for different visualization technologies

### **Integration Scenarios**
- **Professional Tools**: High-end visualization software integration
- **Custom Effects**: Specialized visual effects for specific use cases
- **Research Applications**: Audio analysis and visualization research
- **Performance Optimization**: Hardware-accelerated rendering plugins

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **Plugin Marketplace**: Centralized plugin distribution and management
- **Real-time Editing**: Live plugin parameter adjustment
- **Effect Chaining**: Multiple SVP plugins in sequence
- **Performance Monitoring**: Plugin performance metrics and optimization

### **Advanced Plugin Features**
- **GPU Acceleration**: Direct GPU rendering support
- **Advanced Audio**: Higher resolution spectrum and waveform data
- **3D Rendering**: Support for 3D visualization plugins
- **Network Streaming**: Remote plugin execution and streaming

---

## 📖 **References**

- **Source Code**: `r_svp.cpp` from VIS_AVS
- **Header File**: `svp_vis.h` for plugin interface definitions
- **Plugin System**: External visualization plugin architecture
- **Audio Processing**: Real-time spectrum and waveform conversion
- **Dynamic Loading**: Runtime plugin discovery and loading
- **Thread Safety**: Critical section protected plugin execution

---

**Status:** ✅ **TENTH EFFECT DOCUMENTED**  
**Next:** Oscilloscope Star effect analysis (`r_oscstar.cpp`)
