// File: PhoenixVisualizer.App/Services/WinampAvsParser.cs
using System.Text;

namespace PhoenixVisualizer.App.Services;

public static class WinampAvsParser
{
    // Known superscope/texer types that may carry EEL code in config
    private static readonly HashSet<int> SuperscopeTypeIds = new() { 36, 93 }; // 36: Superscope, 93: SuperScope (Modern)
    private static readonly Encoding Encoder = Encoding.ASCII;

    public sealed record ParsedEffect(int TypeId, byte[] Config);

    public static UnifiedAvsData Parse(byte[] bytes, DetectionResult detection)
    {
        if (!IsWinampHeader(bytes))
        {
            Console.WriteLine("### JUSTIN DEBUG: [WinampAvsParser] No Winamp header found");
            return new UnifiedAvsData(
                AvsFileType.WinampBinary,
                Array.Empty<UnifiedSuperscope>(),
                Array.Empty<UnifiedEffect>(),
                detection,
                RawText: null,
                RawBinary: bytes
            );
        }

        var effects = new List<ParsedEffect>();
        int offset = "Nullsoft AVS Preset".Length;

        // Header structure (observed): [Header ASCII][uint16 version][uint32 count] then blocks
        ushort version = ReadUInt16LE(bytes, ref offset);
        uint count = ReadUInt32LE(bytes, ref offset);

        Console.WriteLine($"### JUSTIN DEBUG: [WinampAvsParser] Version: {version}, Effect count: {count}");

        for (uint i = 0; i < count; i++)
        {
            if (offset + 8 > bytes.Length) break;
            int typeId = ReadInt32LE(bytes, ref offset);
            int configSize = ReadInt32LE(bytes, ref offset);
            if (configSize < 0 || offset + configSize > bytes.Length)
            {
                Console.WriteLine($"### JUSTIN DEBUG: [WinampAvsParser] Corrupt effect {i}: configSize={configSize}, remaining bytes={bytes.Length - offset}");
                // corrupt; stop gracefully
                break;
            }

            var cfg = new byte[configSize];
            Buffer.BlockCopy(bytes, offset, cfg, 0, configSize);
            offset += configSize;

            effects.Add(new ParsedEffect(typeId, cfg));
            Console.WriteLine($"### JUSTIN DEBUG: [WinampAvsParser] Effect {i}: TypeId={typeId}, ConfigSize={configSize}");
        }

        var supers = new List<UnifiedSuperscope>();
        foreach (var e in effects)
        {
            if (!SuperscopeTypeIds.Contains(e.TypeId))
                continue;

            Console.WriteLine($"### JUSTIN DEBUG: [WinampAvsParser] Processing superscope effect TypeId={e.TypeId}");

            // The config blob commonly includes null-terminated ASCII sections or length-prefixed strings.
            // We search conservatively for ASCII fragments around keywords to avoid false 0-results.
            var textGuess = GuessAscii(e.Config);
            if (string.IsNullOrWhiteSpace(textGuess))
            {
                Console.WriteLine($"### JUSTIN DEBUG: [WinampAvsParser] No ASCII text found in superscope config");
                continue;
            }

            Console.WriteLine($"### JUSTIN DEBUG: [WinampAvsParser] Extracted ASCII: {textGuess.Substring(0, Math.Min(100, textGuess.Length))}...");

            // Pull INIT/FRAME/POINT (case-insensitive) lines/blocks
            var (init, frame, point, beat) = ExtractEelSections(textGuess);
            var name = DeriveName(textGuess) ?? $"Superscope_{e.TypeId}";

            supers.Add(new UnifiedSuperscope(
                Name: name,
                InitCode: init, FrameCode: frame, PointCode: point, BeatCode: beat,
                SourceType: "Winamp",
                CombinedCode: BuildCombined(init, frame, point, beat)
            ));
        }

        Console.WriteLine($"### JUSTIN DEBUG: [WinampAvsParser] Found {effects.Count} effects, {supers.Count} superscopes");

        return new UnifiedAvsData(
            FileType: AvsFileType.WinampBinary,
            Superscopes: supers,
            Effects: effects.Select(e => new UnifiedEffect(e.TypeId, e.Config)).ToList(),
            Detection: detection,
            RawText: null,
            RawBinary: bytes
        );
    }

    private static bool IsWinampHeader(byte[] bytes)
    {
        var head = Encoder.GetBytes("Nullsoft AVS Preset");
        return bytes.Length >= head.Length && bytes.AsSpan(0, head.Length).SequenceEqual(head);
    }

    private static ushort ReadUInt16LE(byte[] b, ref int i)
    {
        if (i + 2 > b.Length) return 0;
        ushort v = (ushort)(b[i] | (b[i + 1] << 8));
        i += 2; return v;
    }

    private static uint ReadUInt32LE(byte[] b, ref int i)
    {
        if (i + 4 > b.Length) return 0;
        uint v = (uint)(b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24));
        i += 4; return v;
    }

    private static int ReadInt32LE(byte[] b, ref int i) => unchecked((int)ReadUInt32LE(b, ref i));

    private static string GuessAscii(byte[] cfg)
    {
        // Pull out printable ASCII sequences (length >= 3), join with newlines
        var sb = new StringBuilder();
        int run = 0;
        for (int idx = 0; idx < cfg.Length; idx++)
        {
            var c = cfg[idx];
            bool printable = c >= 32 && c <= 126;
            if (printable)
            {
                sb.Append((char)c);
                run++;
            }
            else
            {
                if (run > 0) sb.AppendLine();
                run = 0;
            }
        }
        var text = sb.ToString();
        return string.IsNullOrWhiteSpace(text) ? "" : text;
    }

    private static (string? init, string? frame, string? point, string? beat) ExtractEelSections(string text)
    {
        // Very tolerant: scan for lines starting with tokens or small blocks
        string? Take(string token)
        {
            // 1) Block form: "TOKEN:" then subsequent non-empty lines until next all-caps token or blank gap
            var lines = text.Split('\n');
            var idx = Array.FindIndex(lines, l => l.Trim().StartsWith(token, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                // 2) Key=Value line form: token=....
                var line = lines.FirstOrDefault(l => l.Trim().StartsWith(token + "=", StringComparison.OrdinalIgnoreCase));
                if (line is null) return null;
                var eq = line.IndexOf('='); 
                return eq > 0 ? line[(eq + 1)..].Trim() : null;
            }

            var buf = new List<string>();
            // If it's "TOKEN:", skip the header line
            bool headerConsumed = false;
            for (int i = idx; i < lines.Length; i++)
            {
                var t = lines[i].TrimEnd('\r');
                if (!headerConsumed)
                {
                    headerConsumed = true;
                    // allow both "TOKEN:" and "TOKEN"
                    if (t.EndsWith(":")) continue; else continue;
                }

                if (IsAllCapsHeader(t)) break;
                if (string.IsNullOrWhiteSpace(t)) { /* allow small gaps */ continue; }
                buf.Add(lines[i]);
            }

            var result = string.Join("\n", buf).Trim();
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        static bool IsAllCapsHeader(string s)
        {
            var t = s.Trim().TrimEnd(':');
            return t is "INIT" or "FRAME" or "POINT" or "BEAT";
        }

        return (Take("INIT"), Take("FRAME"), Take("POINT"), Take("BEAT"));
    }

    private static string? DeriveName(string text)
    {
        // Try to pull a meaningful name from nearby metadata
        var lines = text.Split('\n');
        foreach (var l in lines)
        {
            var s = l.Trim();
            if (s.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                return s[5..].Trim();
            if (s.StartsWith("title=", StringComparison.OrdinalIgnoreCase))
                return s[6..].Trim();
        }
        return null;
    }

    private static string BuildCombined(string? init, string? frame, string? point, string? beat)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(init))  sb.AppendLine("// INIT").AppendLine(init.Trim());
        if (!string.IsNullOrWhiteSpace(frame)) sb.AppendLine("// FRAME").AppendLine(frame.Trim());
        if (!string.IsNullOrWhiteSpace(point)) sb.AppendLine("// POINT").AppendLine(point.Trim());
        if (!string.IsNullOrWhiteSpace(beat))  sb.AppendLine("// BEAT").AppendLine(beat.Trim());
        return sb.ToString();
    }
}
