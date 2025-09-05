using System;
using System.Numerics;
using SkiaSharp;

namespace PhoenixVisualizer.NativeAudio
{
    /// <summary>
    /// Transpiled ProjectM (Milkdrop) visualizer from VLC source
    /// Based on VLC's projectm.cpp wrapper
    /// </summary>
    public class ProjectMVisualizer : IVlcVisualizer
    {
        private bool _initialized = false;
        private uint[]? _frameBuffer;
        private readonly Random _random = new Random();
        private float _time = 0.0f;
        
        // ProjectM parameters (from VLC source)
        private int _width = 800;
        private int _height = 600;
        private float _bassIntensity = 0.0f;
        private float _midIntensity = 0.0f;
        private float _trebleIntensity = 0.0f;
        
        // Milkdrop-style effects
        private float _zoom = 1.0f;
        private float _rotation = 0.0f;
        private float _colorShift = 0.0f;
        private float _pulseIntensity = 0.0f;
        
        // Waveform data for Milkdrop effects
        private float[] _waveformData = new float[512];
        private float[] _spectrumData = new float[1024];
        
        public bool Initialize()
        {
            try
            {
                Console.WriteLine("[ProjectMVisualizer] Initializing ProjectM (Milkdrop)...");
                
                // Allocate RGB32 frame buffer
                _frameBuffer = new uint[_width * _height];
                
                _initialized = true;
                Console.WriteLine("[ProjectMVisualizer] ProjectM initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectMVisualizer] Initialization failed: {ex.Message}");
                return false;
            }
        }
        
        public uint[]? Update(short[] audioData, int channels, float time)
        {
            if (!_initialized)
                return null;
                
            try
            {
                _time = time;
                
                // Extract audio data (mimics VLC's audio processing)
                ExtractAudioData(audioData, channels);
                
                // Update Milkdrop parameters
                UpdateMilkdropParameters();
                
                // Render Milkdrop-style frame
                RenderMilkdropFrame();
                
                return _frameBuffer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectMVisualizer] Update failed: {ex.Message}");
                return null;
            }
        }
        
        public void Render(SKCanvas canvas, int width, int height)
        {
            if (!_initialized || _frameBuffer == null)
                return;
                
            try
            {
                // Convert RGB32 buffer to SkiaSharp bitmap
                using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
                var pixels = bitmap.GetPixels();
                
                // Copy frame buffer to bitmap
                unsafe
                {
                    fixed (uint* src = _frameBuffer)
                    {
                        var dst = (uint*)pixels.ToPointer();
                        for (int i = 0; i < width * height; i++)
                        {
                            dst[i] = src[i];
                        }
                    }
                }
                
                // Draw bitmap to canvas
                canvas.DrawBitmap(bitmap, 0, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectMVisualizer] Render failed: {ex.Message}");
            }
        }
        
        private void ExtractAudioData(short[] audioData, int channels)
        {
            if (audioData == null || audioData.Length == 0)
                return;
                
            // Calculate waveform data
            for (int i = 0; i < Math.Min(_waveformData.Length, audioData.Length); i++)
            {
                _waveformData[i] = audioData[i] / 32768.0f; // Normalize to [-1, 1]
            }
            
            // Calculate spectrum data (simplified FFT)
            CalculateSpectrumData();
            
            // Extract frequency band intensities
            _bassIntensity = CalculateBandIntensity(0, 64);      // Bass
            _midIntensity = CalculateBandIntensity(64, 256);    // Mid
            _trebleIntensity = CalculateBandIntensity(256, 512); // Treble
        }
        
        private void CalculateSpectrumData()
        {
            // Simplified spectrum calculation (mimics FFT)
            for (int i = 0; i < _spectrumData.Length; i++)
            {
                float sum = 0.0f;
                int samples = Math.Min(8, _waveformData.Length - i);
                
                for (int j = 0; j < samples; j++)
                {
                    sum += _waveformData[i + j] * _waveformData[i + j];
                }
                
                _spectrumData[i] = (float)Math.Sqrt(sum / samples);
            }
        }
        
        private float CalculateBandIntensity(int start, int end)
        {
            float sum = 0.0f;
            int count = 0;
            
            for (int i = start; i < end && i < _spectrumData.Length; i++)
            {
                sum += _spectrumData[i];
                count++;
            }
            
            return count > 0 ? sum / count : 0.0f;
        }
        
        private void UpdateMilkdropParameters()
        {
            // Update Milkdrop-style parameters based on audio
            _zoom = 1.0f + _bassIntensity * 0.3f;
            _rotation += _midIntensity * 0.05f;
            _colorShift += _trebleIntensity * 0.02f;
            _pulseIntensity = Math.Max(_bassIntensity, Math.Max(_midIntensity, _trebleIntensity));
        }
        
        private void RenderMilkdropFrame()
        {
            if (_frameBuffer == null)
                return;
                
            // Clear frame buffer
            Array.Fill(_frameBuffer, 0xFF000000); // Black background
            
            // Render Milkdrop-style effects
            RenderMilkdropBackground();
            RenderMilkdropWaves();
            RenderMilkdropParticles();
            RenderMilkdropSpectrum();
        }
        
        private void RenderMilkdropBackground()
        {
            // Render psychedelic background with Milkdrop-style patterns
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int index = y * _width + x;
                    
                    // Calculate distance from center
                    float dx = x - _width / 2.0f;
                    float dy = y - _height / 2.0f;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    float angle = (float)Math.Atan2(dy, dx);
                    
                    // Create Milkdrop-style radial patterns
                    float pattern = (float)Math.Sin(distance * 0.01f + _time * 2) * 
                                   (float)Math.Cos(angle * 3 + _time * 1.5f);
                    
                    // Apply audio-reactive coloring
                    uint color = GenerateMilkdropColor(pattern, distance);
                    _frameBuffer![index] = color;
                }
            }
        }
        
        private void RenderMilkdropWaves()
        {
            // Render waveform visualization
            int centerY = _height / 2;
            int waveHeight = 100;
            
            for (int x = 0; x < _width; x++)
            {
                int waveformIndex = (x * _waveformData.Length) / _width;
                if (waveformIndex < _waveformData.Length)
                {
                    float amplitude = _waveformData[waveformIndex] * waveHeight;
                    int y = centerY + (int)amplitude;
                    
                    if (y >= 0 && y < _height)
                    {
                        int index = y * _width + x;
                        _frameBuffer![index] = GenerateMilkdropColor(1.0f, 0.0f);
                    }
                }
            }
        }
        
        private void RenderMilkdropParticles()
        {
            // Render floating particles (Milkdrop style)
            int particleCount = 50;
            
            for (int i = 0; i < particleCount; i++)
            {
                float angle = (i * 2 * MathF.PI) / particleCount + _time * 0.5f;
                float radius = 100 + _bassIntensity * 200;
                
                int x = _width / 2 + (int)(Math.Cos(angle) * radius);
                int y = _height / 2 + (int)(Math.Sin(angle) * radius);
                
                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    int index = y * _width + x;
                    _frameBuffer![index] = GenerateMilkdropColor(0.8f, 0.0f);
                }
            }
        }
        
        private void RenderMilkdropSpectrum()
        {
            // Render spectrum bars (Milkdrop style)
            int barCount = 64;
            int barWidth = _width / barCount;
            
            for (int i = 0; i < barCount; i++)
            {
                int spectrumIndex = (i * _spectrumData.Length) / barCount;
                if (spectrumIndex < _spectrumData.Length)
                {
                    float intensity = _spectrumData[spectrumIndex];
                    int barHeight = (int)(intensity * _height * 0.8f);
                    
                    for (int y = _height - barHeight; y < _height; y++)
                    {
                        for (int x = i * barWidth; x < (i + 1) * barWidth && x < _width; x++)
                        {
                            if (x >= 0 && x < _width && y >= 0 && y < _height)
                            {
                                int index = y * _width + x;
                                _frameBuffer![index] = GenerateMilkdropColor(intensity, 0.0f);
                            }
                        }
                    }
                }
            }
        }
        
        private uint GenerateMilkdropColor(float intensity, float distance)
        {
            // Generate Milkdrop-style psychedelic colors
            float hue = (_time * 50 + _colorShift * 360) % 360;
            float saturation = 0.9f + intensity * 0.1f;
            float value = 0.7f + intensity * 0.3f;
            
            // Convert HSV to RGB
            float c = value * saturation;
            float x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            float m = value - c;
            
            float r, g, b;
            if (hue < 60) { r = c; g = x; b = 0; }
            else if (hue < 120) { r = x; g = c; b = 0; }
            else if (hue < 180) { r = 0; g = c; b = x; }
            else if (hue < 240) { r = 0; g = x; b = c; }
            else if (hue < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            
            return PackRGB32((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
        }
        
        private uint PackRGB32(byte r, byte g, byte b)
        {
            return (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        
        public void Dispose()
        {
            try
            {
                _initialized = false;
                _frameBuffer = null;
                Console.WriteLine("[ProjectMVisualizer] Disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectMVisualizer] Dispose failed: {ex.Message}");
            }
        }
    }
}
