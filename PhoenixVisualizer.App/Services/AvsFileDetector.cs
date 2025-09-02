// File: PhoenixVisualizer.App/Services/AvsFileDetector.cs
using System.Text;

namespace PhoenixVisualizer.App.Services;

public static class AvsFileDetector
{
    private static readonly byte[] WinampHeader = Encoding.ASCII.GetBytes("Nullsoft AVS Preset");

    public static DetectionResult Detect(byte[] bytes, string? fileName = null)
    {
        var markers = new List<string>();
        float confidence = 0f;

        // 1) Winamp binary check (magic header at file start)
        if (bytes.Length >= WinampHeader.Length && bytes.AsSpan(0, WinampHeader.Length).SequenceEqual(WinampHeader))
        {
            markers.Add("WinampHeader:Nullsoft AVS Preset");
            return new DetectionResult(AvsFileType.WinampBinary, 1.0f, markers, "Binary AVS");
        }

        // 2) Text heuristics
        string textProbe;
        try
        {
            // Try UTF-8 first; fallback to ANSI if obvious mismatch later in parsing
            textProbe = Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return new DetectionResult(AvsFileType.Unknown, 0.0f, markers, "Decode failed");
        }

        var probeLower = textProbe.ToLowerInvariant();
        if (probeLower.Contains("[avs]")) { markers.Add("[avs]"); confidence += 0.50f; }
        if (probeLower.Contains("sn=superscope(")) { markers.Add("sn=Superscope("); confidence += 0.35f; }
        if (probeLower.Contains("init")) { markers.Add("INIT"); confidence += 0.05f; }
        if (probeLower.Contains("frame")) { markers.Add("FRAME"); confidence += 0.05f; }
        if (probeLower.Contains("point")) { markers.Add("POINT"); confidence += 0.05f; }

        if (confidence >= 0.60f)
            return new DetectionResult(AvsFileType.PhoenixText, confidence, markers, "Phoenix AVS text");

        if (confidence > 0.15f)
            return new DetectionResult(AvsFileType.PlainText, confidence, markers, "Unstructured text but AVS-ish");

        return new DetectionResult(AvsFileType.Unknown, 0.0f, markers, "No reliable markers");
    }
}