using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.AvsEngine;

public interface IAvsEngine
{
    void Initialize(int width, int height);
    void LoadPreset(string presetText);
    void Resize(int width, int height);
    void RenderFrame(AudioFeatures features, ISkiaCanvas canvas);
}

// Minimal Superscope-like evaluator (stub)
public sealed class AvsEngine : IAvsEngine
{
    private int _width;
    private int _height;
    private Preset _preset = Preset.CreateDefault();

    public void Initialize(int width, int height)
    {
        _width = width; _height = height;
    }

    public void LoadPreset(string presetText)
    {
        // Enhanced parser: supports tokens like "points=256;mode=line;source=fft;beat=true;energy=true"
        // NEW: Also supports real Winamp superscope code blocks
        try
        {
            var p = new Preset();

            // Check if this is a real Winamp superscope preset
            if (presetText.Contains("init:") || presetText.Contains("per_frame:") || presetText.Contains("per_point:"))
            {
                // Parse Winamp superscope format
                ParseWinampPreset(presetText, p);
            }
            else
            {
                // Parse simple format
                foreach (var seg in presetText.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = seg.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length != 2) continue;
                    var key = kv[0].Trim().ToLowerInvariant();
                    var val = kv[1].Trim().ToLowerInvariant();
                    switch (key)
                    {
                        case "points":
                            if (int.TryParse(val, out var n)) p.Points = Math.Clamp(n, 16, 2048);
                            break;
                        case "mode":
                            p.Mode = val == "bars" ? RenderMode.Bars : RenderMode.Line;
                            break;
                        case "source":
                            p.Source = val == "sin" ? SourceMode.Sin : SourceMode.Fft;
                            break;
                        case "beat":
                            p.UseBeat = val == "true" || val == "1" || val == "yes";
                            break;
                        case "energy":
                            p.UseEnergy = val == "true" || val == "1" || val == "yes";
                            break;
                    }
                }
            }

            _preset = p;
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"Failed to parse preset: {ex.Message}");
            _preset = Preset.CreateDefault(); 
        }
    }

    private void ParseWinampPreset(string presetText, Preset preset)
    {
        // Parse Winamp superscope format
        var lines = presetText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("init:"))
            {
                preset.InitCode = trimmed.Substring(5).Trim();
            }
            else if (trimmed.StartsWith("per_frame:"))
            {
                preset.PerFrameCode = trimmed.Substring(11).Trim();
            }
            else if (trimmed.StartsWith("per_point:"))
            {
                preset.PerPointCode = trimmed.Substring(10).Trim();
            }
            else if (trimmed.StartsWith("beat:"))
            {
                preset.BeatCode = trimmed.Substring(5).Trim();
            }
        }

        System.Diagnostics.Debug.WriteLine($"Parsed Winamp preset: init='{preset.InitCode}', per_frame='{preset.PerFrameCode}', per_point='{preset.PerPointCode}', beat='{preset.BeatCode}'");
    }

    public void Resize(int width, int height)
    {
        _width = width; _height = height;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Debug: log that we're rendering
        System.Diagnostics.Debug.WriteLine($"AvsEngine.RenderFrame: width={_width}, height={_height}, points={_preset.Points}, mode={_preset.Mode}, source={_preset.Source}");

        canvas.Clear(0xFF000000);

        // Draw a simple superscope-like output based on preset
        int npts = Math.Min(_preset.Points, 1024);
        Span<(float x, float y)> pts = stackalloc (float x, float y)[npts];
        ReadOnlySpan<float> fft = features.Fft;

        System.Diagnostics.Debug.WriteLine($"FFT length: {fft.Length}");

        // Superscope variables (like Winamp AVS)
        float t = (float)features.TimeSeconds;
        float beat = features.Beat ? 1.0f : 0.0f;
        float energy = features.Energy;

        for (int i = 0; i < npts; i++)
        {
            // Superscope per-point variables
            float n = npts > 1 ? (float)i / (npts - 1) : 0f; // normalized position (0-1)
            float nx = n * 2.0f - 1.0f; // centered (-1 to 1)

            // Calculate x position
            float x = (nx + 1.0f) * 0.5f * (_width - 1);

            // Calculate y value based on source and effects
            float v = _preset.Source switch
            {
                SourceMode.Sin => (float)Math.Sin(t * 2 * Math.PI + nx * 4 * Math.PI),
                _ => fft.Length > 0 ? fft[(int)(n * (fft.Length - 1))] : 0f
            };

            // Apply effects
            if (_preset.UseBeat)
            {
                v *= 1.0f + beat * 0.5f; // Amplify on beat
            }

            if (_preset.UseEnergy)
            {
                v *= 0.5f + energy * 0.5f; // Scale with energy
            }

            // Calculate y position (center + offset)
            float y = _height * 0.5f - v * (_height * 0.4f);

            pts[i] = (x, y);
        }

        // Choose color based on mode and audio
        uint color = _preset.Mode switch
        {
            RenderMode.Bars => 0xFF44AAFF, // Blue bars
            RenderMode.Line => 0xFFFF8800, // Orange line
            _ => 0xFFFF8800
        };

        // Apply color effects
        if (_preset.UseBeat)
        {
            color = BlendColor(color, 0xFFFF0000, beat * 0.3f); // Red tint on beat
        }

        System.Diagnostics.Debug.WriteLine($"Drawing {npts} points with color {color:X8}");

        // Draw based on mode
        if (_preset.Mode == RenderMode.Bars)
        {
            // Draw individual bars
            for (int i = 0; i < npts; i++)
            {
                var (x, y) = pts[i];
                float barHeight = Math.Abs(y - _height * 0.5f);
                canvas.DrawLines(new[] { (x, _height * 0.5f), (x, y) }, 3.0f, color);
            }
        }
        else
        {
            // Draw connected line
            canvas.DrawLines(pts, 2.0f, color);
        }
    }

    private uint BlendColor(uint color1, uint color2, float ratio)
    {
        // Simple color blending
        uint r1 = (color1 >> 16) & 0xFF;
        uint g1 = (color1 >> 8) & 0xFF;
        uint b1 = color1 & 0xFF;

        uint r2 = (color2 >> 16) & 0xFF;
        uint g2 = (color2 >> 8) & 0xFF;
        uint b2 = color2 & 0xFF;

        uint r = (uint)(r1 * (1 - ratio) + r2 * ratio);
        uint g = (uint)(g1 * (1 - ratio) + g2 * ratio);
        uint b = (uint)(b1 * (1 - ratio) + b2 * ratio);

        return (r << 16) | (g << 8) | b;
    }
}

internal sealed class Preset
{
    public int Points { get; set; } = 256;
    public RenderMode Mode { get; set; } = RenderMode.Line;
    public SourceMode Source { get; set; } = SourceMode.Fft;
    public bool UseBeat { get; set; } = true;
    public bool UseEnergy { get; set; } = true;

    // NEW: Real Winamp superscope support
    public string InitCode { get; set; } = "";      // codehandle[3] - one-time setup
    public string PerFrameCode { get; set; } = "";  // codehandle[1] - per-frame setup
    public string PerPointCode { get; set; } = "";  // codehandle[0] - main superscope logic
    public string BeatCode { get; set; } = "";      // codehandle[2] - beat detection

    public static Preset CreateDefault() => new();
}

internal enum RenderMode { Line, Bars }
internal enum SourceMode { Fft, Sin }
