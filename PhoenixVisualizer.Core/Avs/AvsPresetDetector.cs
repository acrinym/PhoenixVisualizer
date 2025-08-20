using System.Text;

namespace PhoenixVisualizer.Core.Avs;

public sealed class AvsPresetInfo
{
    public bool IsNullsoftAvs { get; init; }
    public string? Title { get; init; }
    public string? Author { get; init; }
    public IReadOnlyList<string> ProbableComponents { get; init; } = Array.Empty<string>();
    public string? WhyUnsupported { get; init; }
}

public static class AvsPresetDetector
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("Nullsoft AVS Preset 0.2");

    /// <summary>
    /// Fast check for AVS 0.2 header and a light "strings" pass to guess components.
    /// </summary>
    public static AvsPresetInfo Analyze(ReadOnlySpan<byte> blob)
    {
        var isAvs = blob.Length >= Magic.Length && blob[..Magic.Length].SequenceEqual(Magic);
        if (!isAvs) return new AvsPresetInfo { IsNullsoftAvs = false, WhyUnsupported = "Not a Nullsoft AVS 0.2 preset." };

        // Heuristic string scan (ASCII only) to pull out common component hints + title/author.
        var strings = ExtractAsciiStrings(blob, 5);
        var title = strings.FirstOrDefault(s => s.Contains("Butterfly", StringComparison.OrdinalIgnoreCase)
                                             || s.Contains("Daedalus", StringComparison.OrdinalIgnoreCase)
                                             || s.Contains("Shiny", StringComparison.OrdinalIgnoreCase));
        var author = strings.FirstOrDefault(s => s.Contains('@') || s.Contains("http", StringComparison.OrdinalIgnoreCase));

        var guesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in strings)
        {
            AddIfContains(guesses, s, "Superscope");
            AddIfContains(guesses, s, "SuperScope");
            AddIfContains(guesses, s, "Dynamic Movement");
            AddIfContains(guesses, s, "DynamicMovement");
            AddIfContains(guesses, s, "Texer");
            AddIfContains(guesses, s, "Color Map");
            AddIfContains(guesses, s, "Channel Shift");
            AddIfContains(guesses, s, "Buffer");
            AddIfContains(guesses, s, "Blur");
            AddIfContains(guesses, s, "Color");
            AddIfContains(guesses, s, "Convolution");
            AddIfContains(guesses, s, "Trans");
            // NS-EEL hints
            if (s.Contains("sin(") || s.Contains("cos(") || s.Contains("atan") || s.Contains("pow"))
                guesses.Add("NS-EEL Math");
        }

        return new AvsPresetInfo
        {
            IsNullsoftAvs = true,
            Title = title,
            Author = author,
            ProbableComponents = guesses.ToList(),
            WhyUnsupported = null
        };
    }

    private static void AddIfContains(HashSet<string> set, string s, string token)
    {
        if (s.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) set.Add(token);
    }

    private static List<string> ExtractAsciiStrings(ReadOnlySpan<byte> span, int minLen)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        foreach (var b in span)
        {
            if (b >= 32 && b < 127) { sb.Append((char)b); }
            else
            {
                if (sb.Length >= minLen) list.Add(sb.ToString());
                sb.Clear();
            }
        }
        if (sb.Length >= minLen) list.Add(sb.ToString());
        return list;
    }
}
