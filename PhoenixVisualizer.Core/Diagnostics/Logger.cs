namespace PhoenixVisualizer.Core.Diagnostics;

public interface ILogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
}

public sealed class NullLogger : ILogger
{
    public static readonly NullLogger Instance = new();
    private NullLogger() {}
    public void Info(string message) {}
    public void Warn(string message) {}
    public void Error(string message, Exception? ex = null) {}
}

public static class Log
{
#if DEBUG && !NO_DEBUG_SPEW
    private sealed class DebugLogger : ILogger
    {
        public void Info(string message)
            => System.Diagnostics.Debug.WriteLine($"✅ {message}");
        public void Warn(string message)
            => System.Diagnostics.Debug.WriteLine($"⚠️ {message}");
        public void Error(string message, Exception? ex = null)
            => System.Diagnostics.Debug.WriteLine($"❌ {message}{(ex is null ? "" : " :: " + ex)}");
    }
    private static ILogger _current = new DebugLogger();
#else
    private static ILogger _current = NullLogger.Instance;
#endif

    public static void SetLogger(ILogger logger) => _current = logger ?? NullLogger.Instance;
    public static void Info(string m)  => _current.Info(m);
    public static void Warn(string m)  => _current.Warn(m);
    public static void Error(string m, Exception? ex = null) => _current.Error(m, ex);
}
