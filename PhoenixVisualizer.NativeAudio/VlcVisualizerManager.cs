using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace PhoenixVisualizer.NativeAudio
{
    /// <summary>
    /// Manager for all VLC visualizers - transpiled from VLC source
    /// Supports: GOOM, ProjectM (Milkdrop), VSXu, VLC Built-in Visual
    /// </summary>
    public class VlcVisualizerManager : IDisposable
    {
        private readonly Dictionary<string, IVlcVisualizer> _visualizers = new();
        private IVlcVisualizer? _currentVisualizer;
        private bool _initialized = false;
        
        public VlcVisualizerManager()
        {
            InitializeVisualizers();
        }
        
        private void InitializeVisualizers()
        {
            try
            {
                Console.WriteLine("[VlcVisualizerManager] Initializing VLC visualizers...");
                
                // Initialize GOOM visualizer
                var goom = new GoomVisualizer(800, 600);
                if (goom.GoomInit(800, 600))
                {
                    _visualizers["goom"] = goom;
                    Console.WriteLine("[VlcVisualizerManager] GOOM visualizer initialized");
                }
                
                // Initialize ProjectM (Milkdrop) visualizer
                var projectM = new ProjectMVisualizer();
                if (projectM.Initialize())
                {
                    _visualizers["projectm"] = projectM;
                    Console.WriteLine("[VlcVisualizerManager] ProjectM visualizer initialized");
                }
                
                // Initialize VSXu visualizer
                var vsxu = new VsxuVisualizer();
                if (vsxu.Initialize())
                {
                    _visualizers["vsxu"] = vsxu;
                    Console.WriteLine("[VlcVisualizerManager] VSXu visualizer initialized");
                }
                
                // Initialize VLC built-in visualizer
                var vlcVisual = new VlcBuiltinVisualizer();
                if (vlcVisual.Initialize())
                {
                    _visualizers["vlc_visual"] = vlcVisual;
                    Console.WriteLine("[VlcVisualizerManager] VLC built-in visualizer initialized");
                }
                
                // Set default visualizer
                _currentVisualizer = _visualizers.Values.FirstOrDefault();
                _initialized = true;
                
                Console.WriteLine($"[VlcVisualizerManager] Initialized {_visualizers.Count} visualizers");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcVisualizerManager] Initialization failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Switch to a specific visualizer
        /// </summary>
        public bool SetVisualizer(string name)
        {
            if (!_initialized)
                return false;
                
            if (_visualizers.TryGetValue(name, out var visualizer))
            {
                _currentVisualizer = visualizer;
                Console.WriteLine($"[VlcVisualizerManager] Switched to {name} visualizer");
                return true;
            }
            
            Console.WriteLine($"[VlcVisualizerManager] Visualizer '{name}' not found");
            return false;
        }
        
        /// <summary>
        /// Get list of available visualizers
        /// </summary>
        public string[] GetAvailableVisualizers()
        {
            return _visualizers.Keys.ToArray();
        }
        
        /// <summary>
        /// Get current visualizer name
        /// </summary>
        public string GetCurrentVisualizerName()
        {
            return _visualizers.FirstOrDefault(kvp => kvp.Value == _currentVisualizer).Key ?? "none";
        }
        
        /// <summary>
        /// Update visualizer with audio data
        /// </summary>
        public uint[]? UpdateVisualizer(short[] audioData, int channels, float time)
        {
            if (!_initialized || _currentVisualizer == null)
                return null;
                
            try
            {
                return _currentVisualizer.Update(audioData, channels, time);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcVisualizerManager] Update failed: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Render visualizer to SkiaSharp canvas
        /// </summary>
        public void RenderVisualizer(SKCanvas canvas, int width, int height)
        {
            if (!_initialized || _currentVisualizer == null)
                return;
                
            try
            {
                _currentVisualizer.Render(canvas, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcVisualizerManager] Render failed: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            try
            {
                foreach (var visualizer in _visualizers.Values)
                {
                    visualizer?.Dispose();
                }
                _visualizers.Clear();
                _currentVisualizer = null;
                _initialized = false;
                Console.WriteLine("[VlcVisualizerManager] Disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcVisualizerManager] Dispose failed: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Interface for VLC visualizers - matches VLC's visualizer API
    /// </summary>
    public interface IVlcVisualizer : IDisposable
    {
        bool Initialize();
        uint[]? Update(short[] audioData, int channels, float time);
        void Render(SKCanvas canvas, int width, int height);
    }
}
