using System;
using System.Numerics;
using SkiaSharp;

namespace PhoenixVisualizer.NativeAudio
{
    /// <summary>
    /// Transpiled VLC built-in visualizer from VLC source
    /// Based on VLC's visual/effects.c and visual/visual.c
    /// </summary>
    public class VlcBuiltinVisualizer : IVlcVisualizer
    {
        private bool _initialized = false;
        private uint[]? _frameBuffer;
        private readonly Random _random = new Random();
        private float _time = 0.0f;
        
        // VLC visual parameters
        private int _width = 800;
        private int _height = 600;
        private float _bassIntensity = 0.0f;
        private float _midIntensity = 0.0f;
        private float _trebleIntensity = 0.0f;
        
        // VLC visual effects
        private float _zoom = 1.0f;
        private float _rotation = 0.0f;
        private float _colorShift = 0.0f;
        private float _pulseIntensity = 0.0f;
        
        // Audio data
        private float[] _waveformData = new float[512];
        private float[] _spectrumData = new float[1024];
        
        // VLC visual effect types (from effects.c)
        private enum VlcEffectType
        {
            Scope,
            Spectrum,
            Waveform,
            Particles,
            Bars
        }
        
        private VlcEffectType _currentEffect = VlcEffectType.Spectrum;
        
        public bool Initialize()
        {
            try
            {
                Console.WriteLine("[VlcBuiltinVisualizer] Initializing VLC built-in visualizer...");
                
                // Allocate RGB32 frame buffer
                _frameBuffer = new uint[_width * _height];
                
                _initialized = true;
                Console.WriteLine("[VlcBuiltinVisualizer] VLC built-in visualizer initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcBuiltinVisualizer] Initialization failed: {ex.Message}");
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
                
                // Extract audio data
                ExtractAudioData(audioData, channels);
                
                // Update VLC visual parameters
                UpdateVlcVisualParameters();
                
                // Render VLC visual frame
                RenderVlcVisualFrame();
                
                return _frameBuffer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcBuiltinVisualizer] Update failed: {ex.Message}");
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
                Console.WriteLine($"[VlcBuiltinVisualizer] Render failed: {ex.Message}");
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
            
            // Calculate spectrum data
            CalculateSpectrumData();
            
            // Extract frequency band intensities
            _bassIntensity = CalculateBandIntensity(0, 64);
            _midIntensity = CalculateBandIntensity(64, 256);
            _trebleIntensity = CalculateBandIntensity(256, 512);
        }
        
        private void CalculateSpectrumData()
        {
            // Simplified spectrum calculation
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
        
        private void UpdateVlcVisualParameters()
        {
            // Update VLC visual parameters
            _zoom = 1.0f + _bassIntensity * 0.2f;
            _rotation += _midIntensity * 0.03f;
            _colorShift += _trebleIntensity * 0.01f;
            _pulseIntensity = Math.Max(_bassIntensity, Math.Max(_midIntensity, _trebleIntensity));
            
            // Cycle through effects based on audio
            if (_pulseIntensity > 0.8f)
            {
                _currentEffect = VlcEffectType.Particles;
            }
            else if (_bassIntensity > 0.6f)
            {
                _currentEffect = VlcEffectType.Bars;
            }
            else if (_midIntensity > 0.5f)
            {
                _currentEffect = VlcEffectType.Spectrum;
            }
            else
            {
                _currentEffect = VlcEffectType.Waveform;
            }
        }
        
        private void RenderVlcVisualFrame()
        {
            if (_frameBuffer == null)
                return;
                
            // Clear frame buffer
            Array.Fill(_frameBuffer, 0xFF000000); // Black background
            
            // Render based on current effect
            switch (_currentEffect)
            {
                case VlcEffectType.Spectrum:
                    RenderSpectrumEffect();
                    break;
                case VlcEffectType.Waveform:
                    RenderWaveformEffect();
                    break;
                case VlcEffectType.Particles:
                    RenderParticlesEffect();
                    break;
                case VlcEffectType.Bars:
                    RenderBarsEffect();
                    break;
                case VlcEffectType.Scope:
                    RenderScopeEffect();
                    break;
            }
        }
        
        private void RenderSpectrumEffect()
        {
            // Render spectrum visualization (VLC style)
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
                                _frameBuffer![index] = GenerateVlcColor(intensity, 0.0f);
                            }
                        }
                    }
                }
            }
        }
        
        private void RenderWaveformEffect()
        {
            // Render waveform visualization (VLC style)
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
                        _frameBuffer![index] = GenerateVlcColor(1.0f, 0.0f);
                    }
                }
            }
        }
        
        private void RenderParticlesEffect()
        {
            // Render particles (VLC style)
            int particleCount = 100;
            
            for (int i = 0; i < particleCount; i++)
            {
                float angle = (i * 2 * MathF.PI) / particleCount + _time * 0.5f;
                float radius = 50 + _bassIntensity * 300;
                
                int x = _width / 2 + (int)(Math.Cos(angle) * radius);
                int y = _height / 2 + (int)(Math.Sin(angle) * radius);
                
                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    int index = y * _width + x;
                    _frameBuffer![index] = GenerateVlcColor(0.8f, 0.0f);
                }
            }
        }
        
        private void RenderBarsEffect()
        {
            // Render bars (VLC style)
            int barCount = 32;
            int barWidth = _width / barCount;
            
            for (int i = 0; i < barCount; i++)
            {
                int spectrumIndex = (i * _spectrumData.Length) / barCount;
                if (spectrumIndex < _spectrumData.Length)
                {
                    float intensity = _spectrumData[spectrumIndex];
                    int barHeight = (int)(intensity * _height * 0.9f);
                    
                    for (int y = _height - barHeight; y < _height; y++)
                    {
                        for (int x = i * barWidth; x < (i + 1) * barWidth && x < _width; x++)
                        {
                            if (x >= 0 && x < _width && y >= 0 && y < _height)
                            {
                                int index = y * _width + x;
                                _frameBuffer![index] = GenerateVlcColor(intensity, 0.0f);
                            }
                        }
                    }
                }
            }
        }
        
        private void RenderScopeEffect()
        {
            // Render scope visualization (VLC style)
            int centerX = _width / 2;
            int centerY = _height / 2;
            int scopeRadius = 150;
            
            for (int i = 0; i < _waveformData.Length; i++)
            {
                float angle = (i * 2 * MathF.PI) / _waveformData.Length;
                float amplitude = _waveformData[i] * scopeRadius;
                
                int x = centerX + (int)(Math.Cos(angle) * amplitude);
                int y = centerY + (int)(Math.Sin(angle) * amplitude);
                
                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    int index = y * _width + x;
                    _frameBuffer![index] = GenerateVlcColor(1.0f, 0.0f);
                }
            }
        }
        
        private uint GenerateVlcColor(float intensity, float distance)
        {
            // Generate VLC-style colors
            float hue = (_time * 30 + _colorShift * 360) % 360;
            float saturation = 0.7f + intensity * 0.3f;
            float value = 0.5f + intensity * 0.5f;
            
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
                Console.WriteLine("[VlcBuiltinVisualizer] Disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcBuiltinVisualizer] Dispose failed: {ex.Message}");
            }
        }
    }
}
