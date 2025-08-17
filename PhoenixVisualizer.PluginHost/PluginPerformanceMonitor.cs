using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Performance metrics for a single plugin
/// </summary>
public class PluginPerformanceMetrics
{
    public string PluginId { get; set; } = string.Empty;
    public string PluginName { get; set; } = string.Empty;
    
    // FPS tracking
    public double CurrentFps { get; set; }
    public double AverageFps { get; set; }
    public double MinFps { get; set; } = double.MaxValue;
    public double MaxFps { get; set; }
    
    // Render time tracking
    public double LastRenderTimeMs { get; set; }
    public double AverageRenderTimeMs { get; set; }
    public double MaxRenderTimeMs { get; set; }
    
    // Memory tracking
    public long CurrentMemoryBytes { get; set; }
    public long PeakMemoryBytes { get; set; }
    
    // Usage statistics
    public int TotalFramesRendered { get; set; }
    public DateTime LastUsed { get; set; }
    public TimeSpan TotalUsageTime { get; set; }
    
    // Performance warnings
    public bool IsPerformingWell => CurrentFps >= 30.0 && LastRenderTimeMs < 16.67;
    public string PerformanceStatus => IsPerformingWell ? "Good" : "Poor";
}

/// <summary>
/// Monitors performance of all plugins in real-time
/// </summary>
public class PluginPerformanceMonitor
{
    private readonly ConcurrentDictionary<string, PluginPerformanceMetrics> _metrics = new();
    private readonly Stopwatch _globalStopwatch = Stopwatch.StartNew();
    private readonly object _lock = new object();
    
    // Performance thresholds
    private const double TargetFps = 60.0;
    private const double MaxRenderTimeMs = 16.67; // 60 FPS = 16.67ms per frame
    
    public event Action<string, PluginPerformanceMetrics>? MetricsUpdated;
    
    /// <summary>
    /// Start monitoring a plugin
    /// </summary>
    public void StartMonitoring(string pluginId, string pluginName)
    {
        var metrics = new PluginPerformanceMetrics
        {
            PluginId = pluginId,
            PluginName = pluginName,
            LastUsed = DateTime.UtcNow
        };
        
        _metrics[pluginId] = metrics;
    }
    
    /// <summary>
    /// Record a frame render for a plugin
    /// </summary>
    public void RecordFrame(string pluginId, double renderTimeMs)
    {
        if (!_metrics.TryGetValue(pluginId, out var metrics))
            return;
            
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var timeSinceLastFrame = (now - metrics.LastUsed).TotalSeconds;
            
            // Update FPS calculation
            if (timeSinceLastFrame > 0)
            {
                metrics.CurrentFps = 1.0 / timeSinceLastFrame;
                
                // Update min/max FPS
                if (metrics.CurrentFps < metrics.MinFps) metrics.MinFps = metrics.CurrentFps;
                if (metrics.CurrentFps > metrics.MaxFps) metrics.MaxFps = metrics.CurrentFps;
                
                // Update average FPS (simple moving average)
                metrics.AverageFps = (metrics.AverageFps * 0.9) + (metrics.CurrentFps * 0.1);
            }
            
            // Update render time statistics
            metrics.LastRenderTimeMs = renderTimeMs;
            if (renderTimeMs > metrics.MaxRenderTimeMs) metrics.MaxRenderTimeMs = renderTimeMs;
            metrics.AverageRenderTimeMs = (metrics.AverageRenderTimeMs * 0.9) + (renderTimeMs * 0.1);
            
            // Update usage statistics
            metrics.TotalFramesRendered++;
            metrics.LastUsed = now;
            metrics.TotalUsageTime = now - metrics.LastUsed;
            
            // Update memory usage (approximate)
            var process = Process.GetCurrentProcess();
            metrics.CurrentMemoryBytes = process.WorkingSet64;
            if (metrics.CurrentMemoryBytes > metrics.PeakMemoryBytes)
                metrics.PeakMemoryBytes = metrics.CurrentMemoryBytes;
        }
        
        // Notify listeners
        MetricsUpdated?.Invoke(pluginId, metrics);
    }
    
    /// <summary>
    /// Stop monitoring a plugin
    /// </summary>
    public void StopMonitoring(string pluginId)
    {
        _metrics.TryRemove(pluginId, out _);
    }
    
    /// <summary>
    /// Get performance metrics for a specific plugin
    /// </summary>
    public PluginPerformanceMetrics? GetMetrics(string pluginId)
    {
        return _metrics.TryGetValue(pluginId, out var metrics) ? metrics : null;
    }
    
    /// <summary>
    /// Get all performance metrics
    /// </summary>
    public IEnumerable<PluginPerformanceMetrics> GetAllMetrics()
    {
        return _metrics.Values.ToList();
    }
    
    /// <summary>
    /// Get plugins with performance issues
    /// </summary>
    public IEnumerable<PluginPerformanceMetrics> GetPoorPerformers()
    {
        return _metrics.Values.Where(m => !m.IsPerformingWell).ToList();
    }
    
    /// <summary>
    /// Get top performing plugins
    /// </summary>
    public IEnumerable<PluginPerformanceMetrics> GetTopPerformers(int count = 5)
    {
        return _metrics.Values
            .OrderByDescending(m => m.AverageFps)
            .ThenBy(m => m.AverageRenderTimeMs)
            .Take(count)
            .ToList();
    }
    
    /// <summary>
    /// Reset all metrics
    /// </summary>
    public void ResetAllMetrics()
    {
        lock (_lock)
        {
            foreach (var metrics in _metrics.Values)
            {
                metrics.TotalFramesRendered = 0;
                metrics.CurrentFps = 0;
                metrics.AverageFps = 0;
                metrics.MinFps = double.MaxValue;
                metrics.MaxFps = 0;
                metrics.LastRenderTimeMs = 0;
                metrics.AverageRenderTimeMs = 0;
                metrics.MaxRenderTimeMs = 0;
                metrics.PeakMemoryBytes = 0;
            }
        }
    }
    
    /// <summary>
    /// Get performance summary
    /// </summary>
    public string GetPerformanceSummary()
    {
        var totalPlugins = _metrics.Count;
        var goodPerformers = _metrics.Values.Count(m => m.IsPerformingWell);
        var poorPerformers = totalPlugins - goodPerformers;
        
        var avgFps = _metrics.Values.Any() ? _metrics.Values.Average(m => m.AverageFps) : 0;
        var avgRenderTime = _metrics.Values.Any() ? _metrics.Values.Average(m => m.AverageRenderTimeMs) : 0;
        
        return $"Plugins: {totalPlugins} | Good: {goodPerformers} | Poor: {poorPerformers} | Avg FPS: {avgFps:F1} | Avg Render: {avgRenderTime:F2}ms";
    }
}
