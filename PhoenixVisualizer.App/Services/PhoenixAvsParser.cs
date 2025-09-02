// File: PhoenixVisualizer.App/Services/PhoenixAvsParser.cs
using System.Text;

namespace PhoenixVisualizer.App.Services;

public static class PhoenixAvsParser
{
    private sealed class Work
    {
        public string Name = "main";
        public StringBuilder Init = new();
        public StringBuilder Frame = new();
        public StringBuilder Point = new();
        public StringBuilder Beat = new();
    }

    public static UnifiedAvsData Parse(byte[] bytes, DetectionResult detection)
    {
        // Decode text (try UTF-8 then fallback)
        string text = TryDecodeUtf8(bytes, out var usedUtf8)
            ? Encoding.UTF8.GetString(bytes)
            : Encoding.Default.GetString(bytes);

        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var superscopes = new List<UnifiedSuperscope>();
        var current = new Work();
        var inSection = ""; // INIT | FRAME | POINT | BEAT
        string currentSuperscopeName = "main";
        var presetsMeta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void FlushCurrent()
        {
            if (current == null) return;
            var combined = BuildCombined(current.Init.ToString(), current.Frame.ToString(), current.Point.ToString(), current.Beat.ToString());
            superscopes.Add(new UnifiedSuperscope(
                Name: current.Name,
                InitCode: ToNullIfEmpty(current.Init.ToString()),
                FrameCode: ToNullIfEmpty(current.Frame.ToString()),
                PointCode: ToNullIfEmpty(current.Point.ToString()),
                BeatCode:  ToNullIfEmpty(current.Beat.ToString()),
                SourceType: "Phoenix",
                CombinedCode: combined
            ));
            current = new Work();
        }

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            // Section headers like [avs], [preset00], etc.
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                var section = line[1..^1].Trim().ToLowerInvariant();
                inSection = ""; // reset code section

                if (section == "avs")
                {
                    continue;
                }

                if (section.StartsWith("preset"))
                {
                    // new preset block may indicate new superscope namespace; flush previous
                    if (currentSuperscopeName != "main" || current.Init.Length + current.Frame.Length + current.Point.Length + current.Beat.Length > 0)
                    {
                        FlushCurrent();
                    }

                    currentSuperscopeName = "main";
                    current.Name = "main";
                    continue;
                }

                // Code region headers inside a preset (allow both token and "TOKEN:" styles)
                switch (section)
                {
                    case "init":  inSection = "INIT";  break;
                    case "frame": inSection = "FRAME"; break;
                    case "point": inSection = "POINT"; break;
                    case "beat":  inSection = "BEAT";  break;
                    default:      inSection = "";       break;
                }

                continue;
            }

            // key=value pairs (metadata or sn=Superscope)
            var eqIdx = line.IndexOf('=');
            if (eqIdx > 0)
            {
                var key = line[..eqIdx].Trim();
                var value = line[(eqIdx + 1)..].Trim();

                if (key.Equals("sn", StringComparison.OrdinalIgnoreCase) &&
                    value.StartsWith("Superscope(", StringComparison.OrdinalIgnoreCase))
                {
                    // encountered new superscope -> flush previous one
                    if (current.Name != value)
                        FlushCurrent();

                    current.Name = ExtractSuperscopeName(value) ?? "main";
                    currentSuperscopeName = current.Name;
                    Console.WriteLine($"### JUSTIN DEBUG: [PhoenixAvsParser] Found superscope: {current.Name}");
                    continue;
                }

                // header fields like PRESET_NAME, DESCRIPTION
                if (!key.Equals("init", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("frame", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("point", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("beat", StringComparison.OrdinalIgnoreCase))
                {
                    presetsMeta[key] = value;
                    continue;
                }
            }

            // Label style: "INIT:" / "FRAME:" / etc
            if (line.EndsWith(":"))
            {
                var token = line.TrimEnd(':').Trim().ToUpperInvariant();
                inSection = token is "INIT" or "FRAME" or "POINT" or "BEAT" ? token : "";
                Console.WriteLine($"### JUSTIN DEBUG: [PhoenixAvsParser] Entering section: {inSection}");
                continue;
            }

            // Append code to the current section (if any)
            switch (inSection)
            {
                case "INIT":  current.Init.AppendLine(raw);  break;
                case "FRAME": current.Frame.AppendLine(raw); break;
                case "POINT": current.Point.AppendLine(raw); break;
                case "BEAT":  current.Beat.AppendLine(raw);  break;
                default:
                    // Not in a code section; ignore stray lines
                    break;
            }
        }

        // final flush
        FlushCurrent();

        Console.WriteLine($"### JUSTIN DEBUG: [PhoenixAvsParser] Parsed {superscopes.Count} superscopes total");

        return new UnifiedAvsData(
            FileType: AvsFileType.PhoenixText,
            Superscopes: superscopes,
            Effects: Array.Empty<UnifiedEffect>(),
            Detection: detection,
            RawText: text,
            RawBinary: null
        );
    }

    private static string BuildCombined(string init, string frame, string point, string beat)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(init))  sb.AppendLine("// INIT").AppendLine(init.Trim());
        if (!string.IsNullOrWhiteSpace(frame)) sb.AppendLine("// FRAME").AppendLine(frame.Trim());
        if (!string.IsNullOrWhiteSpace(point)) sb.AppendLine("// POINT").AppendLine(point.Trim());
        if (!string.IsNullOrWhiteSpace(beat))  sb.AppendLine("// BEAT").AppendLine(beat.Trim());
        return sb.ToString();
    }

    private static bool TryDecodeUtf8(byte[] b, out bool utf8)
    {
        utf8 = true;
        try { Encoding.UTF8.GetString(b); return true; } catch { utf8 = false; return false; }
    }

    private static string? ToNullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static string? ExtractSuperscopeName(string value)
    {
        // sn=Superscope(name) — return "name"
        var start = value.IndexOf('(');
        var end = value.LastIndexOf(')');
        if (start >= 0 && end > start) return value[(start + 1)..end].Trim();
        return null;
    }
}