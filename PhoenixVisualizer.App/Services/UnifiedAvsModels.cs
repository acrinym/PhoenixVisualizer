// File: PhoenixVisualizer.App/Services/UnifiedAvsModels.cs
namespace PhoenixVisualizer.App.Services;

public enum AvsFileType
{
    Unknown = 0,
    PhoenixText,
    WinampBinary,
    PlainText
}

public sealed record DetectionResult(
    AvsFileType FileType,
    float Confidence,
    IReadOnlyList<string> Markers,
    string? Note = null);

public sealed record UnifiedEffect(
    int TypeId,
    byte[] RawConfig);

public sealed record UnifiedSuperscope(
    string Name,
    string? InitCode,
    string? FrameCode,
    string? PointCode,
    string? BeatCode,
    string SourceType,           // "Phoenix" | "Winamp" | "Generic"
    string CombinedCode          // concatenation of sections (for the evaluator)
);

public sealed record UnifiedAvsData(
    AvsFileType FileType,
    IReadOnlyList<UnifiedSuperscope> Superscopes,
    IReadOnlyList<UnifiedEffect> Effects,
    DetectionResult Detection,
    string? RawText,             // populated for text-based presets
    byte[]? RawBinary            // populated for binary presets
);
