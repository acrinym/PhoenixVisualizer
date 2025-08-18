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

    /// <summary>
    /// Minimal Superscope-like evaluator for AVS effects
    /// </summary>
    public class SuperscopeEvaluator
    {
        private readonly Dictionary<string, double> _variables = new();
        private readonly Dictionary<string, Func<double[], double>> _functions = new();

        public SuperscopeEvaluator()
        {
            InitializeBuiltInFunctions();
        }

        private void InitializeBuiltInFunctions()
        {
            // Mathematical functions
            _functions["sin"] = args => Math.Sin(args[0]);
            _functions["cos"] = args => Math.Cos(args[0]);
            _functions["tan"] = args => Math.Tan(args[0]);
            _functions["sqrt"] = args => Math.Sqrt(args[0]);
            _functions["abs"] = args => Math.Abs(args[0]);
            _functions["log"] = args => Math.Log(args[0]);
            _functions["pow"] = args => Math.Pow(args[0], args[1]);
            
            // AVS-specific functions
            _functions["getosc"] = args => GetOscillatorValue(args[0], args[1]);
            _functions["getspec"] = args => GetSpectrumValue(args[0], args[1]);
            _functions["bass"] = args => GetBassLevel();
            _functions["mid"] = args => GetMidLevel();
            _functions["treb"] = args => GetTrebleLevel();
        }

        public void SetVariable(string name, double value)
        {
            _variables[name] = value;
        }

        public double GetVariable(string name)
        {
            return _variables.TryGetValue(name, out var value) ? value : 0.0;
        }

        public double EvaluateExpression(string expression)
        {
            try
            {
                // Simple expression parser for basic mathematical operations
                return ParseAndEvaluate(expression);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error evaluating expression '{expression}': {ex.Message}");
                return 0.0;
            }
        }

        private double ParseAndEvaluate(string expr)
        {
            // Remove whitespace
            expr = expr.Replace(" ", "");
            
            // Handle function calls
            if (expr.Contains("("))
            {
                return EvaluateFunction(expr);
            }
            
            // Handle basic arithmetic
            return EvaluateArithmetic(expr);
        }

        private double EvaluateFunction(string expr)
        {
            var openParen = expr.IndexOf('(');
            var closeParen = expr.LastIndexOf(')');
            
            if (openParen == -1 || closeParen == -1)
                throw new ArgumentException("Invalid function syntax");
            
            var funcName = expr.Substring(0, openParen);
            var argsStr = expr.Substring(openParen + 1, closeParen - openParen - 1);
            
            var args = ParseArguments(argsStr);
            
            if (_functions.TryGetValue(funcName, out var func))
            {
                return func(args);
            }
            
            throw new ArgumentException($"Unknown function: {funcName}");
        }

        private double[] ParseArguments(string argsStr)
        {
            if (string.IsNullOrEmpty(argsStr))
                return new double[0];
            
            var args = new List<double>();
            var current = "";
            var parenCount = 0;
            
            for (int i = 0; i < argsStr.Length; i++)
            {
                var ch = argsStr[i];
                
                if (ch == '(') parenCount++;
                else if (ch == ')') parenCount--;
                else if (ch == ',' && parenCount == 0)
                {
                    if (!string.IsNullOrEmpty(current))
                    {
                        args.Add(EvaluateArithmetic(current));
                        current = "";
                    }
                    continue;
                }
                
                current += ch;
            }
            
            if (!string.IsNullOrEmpty(current))
            {
                args.Add(EvaluateArithmetic(current));
            }
            
            return args.ToArray();
        }

        private double EvaluateArithmetic(string expr)
        {
            // Simple arithmetic evaluator
            // This is a basic implementation - in production you'd want a proper parser
            
            if (double.TryParse(expr, out var number))
                return number;
            
            if (_variables.TryGetValue(expr, out var variable))
                return variable;
            
            // Handle basic operations (very simplified)
            if (expr.Contains("+"))
            {
                var parts = expr.Split('+');
                return parts.Sum(p => EvaluateArithmetic(p));
            }
            
            if (expr.Contains("-"))
            {
                var parts = expr.Split('-');
                if (parts.Length == 2)
                    return EvaluateArithmetic(parts[0]) - EvaluateArithmetic(parts[1]);
            }
            
            if (expr.Contains("*"))
            {
                var parts = expr.Split('*');
                return parts.Aggregate(1.0, (acc, p) => acc * EvaluateArithmetic(p));
            }
            
            if (expr.Contains("/"))
            {
                var parts = expr.Split('/');
                if (parts.Length == 2)
                    return EvaluateArithmetic(parts[0]) / EvaluateArithmetic(parts[1]);
            }
            
            throw new ArgumentException($"Cannot evaluate expression: {expr}");
        }

        // Mock implementations for AVS functions
        private double GetOscillatorValue(double band, double channel)
        {
            // Mock oscillator value based on time and parameters
            var time = _variables.GetValueOrDefault("time", 0.0);
            return Math.Sin(time * band + channel) * 0.5 + 0.5;
        }

        private double GetSpectrumValue(double band, double channel)
        {
            // Mock spectrum value
            var time = _variables.GetValueOrDefault("time", 0.0);
            return Math.Max(0, Math.Sin(time * band + channel) * 0.3 + 0.2);
        }

        private double GetBassLevel()
        {
            var time = _variables.GetValueOrDefault("time", 0.0);
            return Math.Max(0, Math.Sin(time * 0.5) * 0.4 + 0.3);
        }

        private double GetMidLevel()
        {
            var time = _variables.GetValueOrDefault("time", 0.0);
            return Math.Max(0, Math.Sin(time * 1.0) * 0.3 + 0.2);
        }

        private double GetTrebleLevel()
        {
            var time = _variables.GetValueOrDefault("time", 0.0);
            return Math.Max(0, Math.Sin(time * 2.0) * 0.2 + 0.1);
        }
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
