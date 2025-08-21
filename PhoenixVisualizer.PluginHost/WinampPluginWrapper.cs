using PhoenixVisualizer.PluginHost.Services;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Wrapper that makes Winamp plugins compatible with IVisualizerPlugin interface
/// </summary>
public class WinampPluginWrapper : IVisualizerPlugin
{
    private readonly WinampIntegrationService _winampService;
    private readonly SimpleWinampHost.LoadedPlugin _plugin;
    private readonly int _moduleIndex;
    private bool _initialized;
    private bool _disposed;

    public string Id { get; }
    public string DisplayName { get; }

    public WinampPluginWrapper(WinampIntegrationService winampService, SimpleWinampHost.LoadedPlugin plugin, int moduleIndex = 0)
    {
        _winampService = winampService;
        _plugin = plugin;
        _moduleIndex = moduleIndex;
        
        Id = $"winamp_{plugin.FileName}_{moduleIndex}";
        DisplayName = moduleIndex < plugin.Modules.Count 
            ? plugin.Modules[moduleIndex].Description 
            : plugin.FileName;
    }

    public void Initialize(int width, int height)
    {
        if (_disposed || _initialized) return;

        try
        {
            // Initialize the Winamp plugin
            var result = _winampService.SelectPluginAsync(_plugin, _moduleIndex).GetAwaiter().GetResult();
            _initialized = result.Success;
            
            if (!_initialized)
            {
                Console.WriteLine($"[WinampPluginWrapper] Failed to initialize plugin {DisplayName}: {result.Status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampPluginWrapper] Error initializing plugin {DisplayName}: {ex.Message}");
        }
    }

    public void Resize(int width, int height)
    {
        // Winamp plugins typically handle resizing internally
        // This is a no-op for most Winamp visualizers
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        if (!_initialized || _disposed) return;

        try
        {
            // Convert AudioFeatures to Winamp format
            var spectrumData = ConvertFFTToSpectrum(features.Fft);
            var waveformData = ConvertWaveformToWinamp(features.Waveform);

            // Update audio data in the Winamp plugin
            var pluginIndex = GetPluginIndex();
            if (pluginIndex >= 0)
            {
                _winampService.UpdateAudioData(pluginIndex, _moduleIndex, spectrumData, waveformData);
                
                // Trigger render if possible
                _winampService.RenderPlugin(pluginIndex, _moduleIndex);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampPluginWrapper] Error rendering frame for {DisplayName}: {ex.Message}");
        }
    }

    private byte[] ConvertFFTToSpectrum(float[] fft)
    {
        if (fft == null || fft.Length == 0) return new byte[256];

        // Winamp spectrum data is typically 256 bytes representing frequency bands
        var spectrum = new byte[256];
        var fftLength = Math.Min(fft.Length, 256);

        for (int i = 0; i < fftLength; i++)
        {
            // Convert float FFT to byte spectrum (0-255)
            var amplitude = Math.Clamp(fft[i] * 255f, 0f, 255f);
            spectrum[i] = (byte)amplitude;
        }

        return spectrum;
    }

    private byte[] ConvertWaveformToWinamp(float[] waveform)
    {
        if (waveform == null || waveform.Length == 0) return new byte[576];

        // Winamp waveform data is typically 576 bytes (288 samples per channel)
        var winampWaveform = new byte[576];
        var samplesPerChannel = 288;

        for (int i = 0; i < samplesPerChannel && i < waveform.Length; i++)
        {
            // Convert float waveform to signed byte (-128 to 127), then to unsigned (0-255)
            var sample = Math.Clamp(waveform[i] * 127f, -127f, 127f);
            var unsignedSample = (byte)(sample + 128);
            
            // Store for both channels (mono to stereo)
            winampWaveform[i] = unsignedSample;
            winampWaveform[i + samplesPerChannel] = unsignedSample;
        }

        return winampWaveform;
    }

    private int GetPluginIndex()
    {
        // This is a simple approach - in a more robust implementation,
        // we would maintain a mapping between plugins and their indices
        var availablePlugins = _winampService.GetAvailablePlugins();
        for (int i = 0; i < availablePlugins.Count; i++)
        {
            if (availablePlugins[i].FileName == _plugin.FileName)
            {
                return i;
            }
        }
        return -1;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            // The actual Winamp plugin cleanup is handled by WinampIntegrationService
            _disposed = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinampPluginWrapper] Error disposing plugin {DisplayName}: {ex.Message}");
        }
    }
}
