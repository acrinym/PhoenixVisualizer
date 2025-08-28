using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Models
{
    /// <summary>
    /// Tracks performance metrics for VFX effects
    /// </summary>
    public class VFXPerformanceMetrics
    {
        /// <summary>
        /// Total number of frames processed
        /// </summary>
        public long TotalFrames { get; set; }
        
        /// <summary>
        /// Average frame time in milliseconds
        /// </summary>
        public double AverageFrameTime { get; set; }
        
        /// <summary>
        /// Minimum frame time in milliseconds
        /// </summary>
        public double MinFrameTime { get; set; } = double.MaxValue;
        
        /// <summary>
        /// Maximum frame time in milliseconds
        /// </summary>
        public double MaxFrameTime { get; set; } = double.MinValue;
        
        /// <summary>
        /// Current frame time in milliseconds
        /// </summary>
        public double CurrentFrameTime { get; set; }
        
        /// <summary>
        /// Total processing time in milliseconds
        /// </summary>
        public double TotalProcessingTime { get; set; }
        
        /// <summary>
        /// Average FPS
        /// </summary>
        public double AverageFPS => TotalFrames > 0 ? 1000.0 / AverageFrameTime : 0.0;
        
        /// <summary>
        /// Current FPS
        /// </summary>
        public double CurrentFPS => CurrentFrameTime > 0 ? 1000.0 / CurrentFrameTime : 0.0;
        
        /// <summary>
        /// Memory usage in bytes
        /// </summary>
        public long MemoryUsage { get; set; }
        
        /// <summary>
        /// Peak memory usage in bytes
        /// </summary>
        public long PeakMemoryUsage { get; set; }
        
        /// <summary>
        /// Number of draw calls in the last frame
        /// </summary>
        public int DrawCalls { get; set; }
        
        /// <summary>
        /// Number of vertices rendered in the last frame
        /// </summary>
        public int VertexCount { get; set; }
        
        /// <summary>
        /// Number of triangles rendered in the last frame
        /// </summary>
        public int TriangleCount { get; set; }
        
        /// <summary>
        /// Whether the effect is currently GPU accelerated
        /// </summary>
        public bool IsGPUAccelerated { get; set; }
        
        /// <summary>
        /// GPU memory usage in bytes
        /// </summary>
        public long GPUMemoryUsage { get; set; }
        
        /// <summary>
        /// Performance warnings and issues
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// Performance errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Reset all metrics to initial values
        /// </summary>
        public void Reset()
        {
            TotalFrames = 0;
            AverageFrameTime = 0;
            MinFrameTime = double.MaxValue;
            MaxFrameTime = double.MinValue;
            CurrentFrameTime = 0;
            TotalProcessingTime = 0;
            MemoryUsage = 0;
            PeakMemoryUsage = 0;
            DrawCalls = 0;
            VertexCount = 0;
            TriangleCount = 0;
            Warnings.Clear();
            Errors.Clear();
        }
        
        /// <summary>
        /// Update metrics with a new frame time
        /// </summary>
        public void UpdateFrameTime(double frameTimeMs)
        {
            CurrentFrameTime = frameTimeMs;
            TotalFrames++;
            TotalProcessingTime += frameTimeMs;
            
            // Update min/max
            if (frameTimeMs < MinFrameTime)
                MinFrameTime = frameTimeMs;
            if (frameTimeMs > MaxFrameTime)
                MaxFrameTime = frameTimeMs;
            
            // Update average
            AverageFrameTime = TotalProcessingTime / TotalFrames;
        }
        
        /// <summary>
        /// Add a performance warning
        /// </summary>
        public void AddWarning(string warning)
        {
            if (!Warnings.Contains(warning))
                Warnings.Add(warning);
        }
        
        /// <summary>
        /// Add a performance error
        /// </summary>
        public void AddError(string error)
        {
            if (!Errors.Contains(error))
                Errors.Add(error);
        }
        
        /// <summary>
        /// Check if performance is acceptable
        /// </summary>
        public bool IsPerformanceAcceptable()
        {
            return CurrentFPS >= 30.0 && CurrentFrameTime <= 33.33; // 30 FPS threshold
        }
        
        /// <summary>
        /// Get a summary of performance metrics
        /// </summary>
        public string GetSummary()
        {
            return $"FPS: {CurrentFPS:F1} | Frame Time: {CurrentFrameTime:F2}ms | Memory: {MemoryUsage / 1024 / 1024:F1}MB | Draw Calls: {DrawCalls}";
        }
    }
}