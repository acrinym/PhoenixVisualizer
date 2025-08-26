using System.Text.Json;

namespace PhoenixVisualizer.Core.Config;

// 🔊 Spectrum scaling options
public enum SpectrumScale { Linear, Log, Sqrt }

// 🎲 Random preset modes
public enum RandomPresetMode
{
    Off = 0,
    OnBeat = 1,
    Interval = 2,
    Stanza = 3
}

// 🎛️ Visualizer settings persisted to disk
public sealed partial class VisualizerSettings
{
    // --- Sensitivity / visual tweaks ---
    public float InputGainDb { get; set; } = 0f;            // -24..+24
    public bool AutoGain { get; set; } = true;              // AGC keeps levels steady
    public float TargetRms { get; set; } = 0.08f;           // AGC target
    public float SmoothingMs { get; set; } = 120f;          // EMA over FFT magnitude
    public float FrameBlend { get; set; } = 0.25f;          // 0..1 (visual frame lerp)
    public float NoiseGateDb { get; set; } = -60f;          // gate low-level noise
    public float FloorDb { get; set; } = -48f;              // spectral floor
    public float CeilingDb { get; set; } = -6f;             // spectral ceiling
    public SpectrumScale SpectrumScale { get; set; } = SpectrumScale.Log;
    public float PeakFalloffPerSec { get; set; } = 1.5f;    // bar peak falloff
    public float BeatSensitivity { get; set; } = 1.35f;     // energy multiple to flag beat
    public int BeatCooldownMs { get; set; } = 400;          // don’t spam beats
    public int FftSize { get; set; } = 2048;                // 1024/2048 like Winamp
    public bool ShowPeaks { get; set; } = true;             // classic spectrum peak caps
    public bool EnableHotkeys { get; set; } = true;         // Y/U/Space/R/Enter

    // --- Engine selection ---
    public string SelectedEngine { get; set; } = "avs";  // "avs" or "phoenix"
    
    // --- Random preset switching ---
    public RandomPresetMode RandomPresetMode { get; set; } = RandomPresetMode.Off;
    public int RandomPresetIntervalSeconds { get; set; } = 30;
    public int BeatsPerBar { get; set; } = 4;
    public int StanzaBars { get; set; } = 16;
    public int RandomPresetCooldownMs { get; set; } = 800;
    public bool RandomWhenSilent { get; set; } = false;
    public float SilenceRmsGate { get; set; } = 0.010f;

    // legacy flag to detect old json
    private bool _legacyRandomOnBeat = false;

    public static string Path =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "settings.visualizer.json");

    public static VisualizerSettings Load()
    {
        if (!File.Exists(Path)) return new VisualizerSettings();
        var json = File.ReadAllText(Path);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var settings = JsonSerializer.Deserialize<VisualizerSettings>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new VisualizerSettings();

        if (root.TryGetProperty("RandomPresetOnBeat", out var legacy) && legacy.GetBoolean())
            settings._legacyRandomOnBeat = true;

        settings.OnLoadedCompat();
        return settings;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path, json);
    }

    // map legacy bool to new enum
    partial void OnLoadedCompat();
}

public sealed partial class VisualizerSettings
{
    partial void OnLoadedCompat()
    {
        if (RandomPresetMode == RandomPresetMode.Off && _legacyRandomOnBeat)
            RandomPresetMode = RandomPresetMode.OnBeat;
    }
}

// ✨ Helper extension
public static class VisualizerSettingsExtensions
{
    public static float BeatSensitivityOrDefault(this VisualizerSettings v)
        => v.BeatSensitivity <= 0f ? 1.35f : v.BeatSensitivity;
}

