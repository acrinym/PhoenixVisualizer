// File: PhoenixVisualizer.App/Services/UnifiedAvsService.cs
using System.IO;

namespace PhoenixVisualizer.App.Services;

public interface IUnifiedAvsService
{
    UnifiedAvsData Load(string path);
    UnifiedAvsData Load(byte[] bytes, string? fileName = null);
}

public sealed class UnifiedAvsService : IUnifiedAvsService
{
    public UnifiedAvsData Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Load(bytes, Path.GetFileName(path));
    }

    public UnifiedAvsData Load(byte[] bytes, string? fileName = null)
    {
        var detection = AvsFileDetector.Detect(bytes, fileName);

        Console.WriteLine($"### JUSTIN DEBUG: [UnifiedAvsService] Detected {detection.FileType} with confidence {detection.Confidence:F2}");

        return detection.FileType switch
        {
            AvsFileType.PhoenixText   => PhoenixAvsParser.Parse(bytes, detection),
            AvsFileType.WinampBinary  => WinampAvsParser.Parse(bytes, detection),
            AvsFileType.PlainText     => PhoenixAvsParser.Parse(bytes, detection with { FileType = AvsFileType.PhoenixText, Note = "Plain text treated as Phoenix" }),
            _                         => new UnifiedAvsData(AvsFileType.Unknown, Array.Empty<UnifiedSuperscope>(), Array.Empty<UnifiedEffect>(), detection, null, bytes)
        };
    }
}
