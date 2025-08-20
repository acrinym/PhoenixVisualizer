using System.Runtime.InteropServices;
using System.Text;
using PhoenixVisualizer.Core.Diagnostics;

namespace PhoenixVisualizer.Core.Avs;

public enum AvsRoute
{
    NotAvs,
    NativeAvs,  // Windows + vis_avs.dll present (to be implemented)
    Unsupported // Show message, list missing deps
}

public sealed class AvsRouteResult
{
    public AvsRoute Route { get; init; }
    public AvsPresetInfo Info { get; init; } = new();
    public string? Message { get; init; }
}

public static class AvsPresetRouter
{
    /// <summary>
    /// Decides what to do with a dropped/loaded blob. Does not throw.
    /// </summary>
    public static AvsRouteResult Decide(byte[] blob, string? fileName = null, string? nativeAvsPath = null)
    {
        try
        {
            var info = AvsPresetDetector.Analyze(blob);
            if (!info.IsNullsoftAvs)
                return new AvsRouteResult { Route = AvsRoute.NotAvs, Info = info };

            // Windows-only native path
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new AvsRouteResult
                {
                    Route = AvsRoute.Unsupported,
                    Info = info,
                    Message = "❌ AVS preset detected, but native AVS runtime is Windows-only. Run on Windows or convert this preset."
                };
            }

            // Check DLL presence if user provided a location; allow PATH fallback
            var dll = nativeAvsPath ?? "vis_avs.dll";
            var canLoad = NativeLibrary.TryLoad(dll, out var handle);
            if (canLoad && handle != IntPtr.Zero)
            {
                NativeLibrary.Free(handle);
                return new AvsRouteResult { Route = AvsRoute.NativeAvs, Info = info, Message = $"✅ AVS preset detected{(fileName is null ? "" : $" ({fileName})")} — using native AVS runtime." };
            }

            // We don't have the runtime; construct a helpful message.
            var sb = new StringBuilder();
            sb.AppendLine("❌ AVS preset detected, but native AVS runtime (vis_avs.dll) was not found.");
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(info.Title)) sb.AppendLine($"• Title: {info.Title}");
            if (!string.IsNullOrWhiteSpace(info.Author)) sb.AppendLine($"• Author/Ref: {info.Author}");
            if (info.ProbableComponents.Count > 0)
            {
                sb.AppendLine("• Probable components used:");
                foreach (var c in info.ProbableComponents.OrderBy(x => x))
                    sb.AppendLine($"   - {c}");
            }
            sb.AppendLine();
            sb.AppendLine("➡️  Place vis_avs.dll next to the executable or in PATH, then try again.");

            return new AvsRouteResult
            {
                Route = AvsRoute.Unsupported,
                Info = info,
                Message = sb.ToString()
            };
        }
        catch (Exception ex)
        {
            Log.Error("AVS routing failed", ex);
            return new AvsRouteResult
            {
                Route = AvsRoute.Unsupported,
                Info = new AvsPresetInfo { IsNullsoftAvs = false },
                Message = $"❌ AVS preset check failed: {ex.Message}"
            };
        }
    }
}
