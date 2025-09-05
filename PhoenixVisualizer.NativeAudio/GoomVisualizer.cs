using System;
using System.Numerics;
using SkiaSharp;

namespace PhoenixVisualizer.NativeAudio
{
    /// <summary>
    /// Transpiled C# implementation of GOOM psychedelic visualizer
    /// Based on VLC's goom.c wrapper - mimics the original GOOM library behavior
    /// Key functions: goom_init(), goom_update(), goom_close()
    /// </summary>
    public class GoomVisualizer : IVlcVisualizer
    {
        private readonly Random _random = new Random();
        private float _time = 0.0f;
        private float _bassIntensity = 0.0f;
        private float _midIntensity = 0.0f;
        private float _trebleIntensity = 0.0f;
        
        // GOOM-style parameters
        private float _zoom = 1.0f;
        private float _rotation = 0.0f;
        private float _colorShift = 0.0f;
        private float _pulseIntensity = 0.0f;
        
        // Particle system for psychedelic effects
        private readonly GoomParticle[] _particles = new GoomParticle[100];
        private readonly Vector2 _center;
        
        // GOOM library state (mimics PluginInfo from original GOOM)
        private bool _initialized = false;
        private uint[]? _frameBuffer; // RGB32 pixel buffer
        private int _frameCount = 0;
        private int _width = 800;
        private int _height = 600;
        
        public GoomVisualizer(int width, int height)
        {
            _center = new Vector2(width / 2.0f, height / 2.0f);
            InitializeParticles();
        }
        
        /// <summary>
        /// Initialize visualizer - implements IVlcVisualizer
        /// </summary>
        public bool Initialize()
        {
            return GoomInit(_width, _height);
        }
        
        /// <summary>
        /// GOOM library initialization - mimics goom_init()
        /// </summary>
        public bool GoomInit(int width, int height)
        {
            try
            {
                Console.WriteLine($"[GoomVisualizer] Initializing GOOM: {width}x{height}");
                
                // Allocate RGB32 frame buffer (4 bytes per pixel)
                _frameBuffer = new uint[width * height];
                
                _initialized = true;
                Console.WriteLine("[GoomVisualizer] GOOM initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoomVisualizer] GOOM initialization failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Update visualizer - implements IVlcVisualizer
        /// </summary>
        public uint[]? Update(short[] audioData, int channels, float time)
        {
            return GoomUpdate(audioData, channels, time);
        }
        
        /// <summary>
        /// GOOM library update - mimics goom_update()
        /// Returns RGB32 pixel data pointer (as uint32_t* in original)
        /// </summary>
        public uint[]? GoomUpdate(short[] audioData, int channels, float time)
        {
            if (!_initialized)
                return null;
                
            try
            {
                _frameCount++;
                
                // Convert audio data to intensities (mimics VLC's FillBuffer)
                ExtractAudioIntensities(audioData, channels);
                
                // Update GOOM parameters based on audio
                UpdateGoomParameters();
                
                // Update particles
                UpdateParticles(0.016f); // ~60 FPS
                
                // Render frame to RGB32 buffer
                RenderFrame();
                
                return _frameBuffer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoomVisualizer] GOOM update failed: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Render visualizer - implements IVlcVisualizer
        /// </summary>
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
                Console.WriteLine($"[GoomVisualizer] Render failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// GOOM library cleanup - mimics goom_close()
        /// </summary>
        public void GoomClose()
        {
            try
            {
                _initialized = false;
                _frameBuffer = null;
                Console.WriteLine("[GoomVisualizer] GOOM closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoomVisualizer] GOOM close failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Dispose - implements IVlcVisualizer
        /// </summary>
        public void Dispose()
        {
            GoomClose();
        }
        
        private void InitializeParticles()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i] = new GoomParticle
                {
                    Position = new Vector2(_random.NextSingle() * 800, _random.NextSingle() * 600),
                    Velocity = new Vector2((_random.NextSingle() - 0.5f) * 2, (_random.NextSingle() - 0.5f) * 2),
                    Color = GetRandomColor(),
                    Size = _random.NextSingle() * 10 + 5,
                    Life = 1.0f,
                    MaxLife = _random.NextSingle() * 2 + 1
                };
            }
        }
        
        /// <summary>
        /// Extract audio intensities from raw audio data (mimics VLC's FillBuffer)
        /// </summary>
        private void ExtractAudioIntensities(short[] audioData, int channels)
        {
            if (audioData == null || audioData.Length == 0)
                return;
                
            // Calculate RMS for bass, mid, treble (simplified)
            float sum = 0.0f;
            for (int i = 0; i < audioData.Length; i++)
            {
                float sample = audioData[i] / 32768.0f; // Normalize to [-1, 1]
                sum += sample * sample;
            }
            
            float rms = (float)Math.Sqrt(sum / audioData.Length);
            
            // Distribute across frequency bands (simplified)
            _bassIntensity = rms * 0.8f;    // Lower frequencies
            _midIntensity = rms * 0.6f;     // Mid frequencies  
            _trebleIntensity = rms * 0.4f;  // Higher frequencies
        }
        
        /// <summary>
        /// Update GOOM parameters based on audio intensities
        /// </summary>
        private void UpdateGoomParameters()
        {
            _zoom = 1.0f + _bassIntensity * 0.5f;
            _rotation += _midIntensity * 0.1f;
            _colorShift += _trebleIntensity * 0.05f;
            _pulseIntensity = Math.Max(_bassIntensity, Math.Max(_midIntensity, _trebleIntensity));
        }
        
        /// <summary>
        /// Render frame to RGB32 buffer (mimics goom_update's pixel output)
        /// </summary>
        private void RenderFrame()
        {
            if (_frameBuffer == null)
                return;
                
            // Clear frame buffer (black background)
            Array.Fill(_frameBuffer, 0xFF000000); // Black RGB32
            
            // Render psychedelic patterns directly to RGB32 buffer
            RenderPsychedelicPatterns();
            RenderParticles();
            RenderMandala();
        }
        
        /// <summary>
        /// Render psychedelic background patterns to RGB32 buffer
        /// </summary>
        private void RenderPsychedelicPatterns()
        {
            int width = 800; // Assume 800x600 for now
            int height = 600;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    
                    // Calculate distance from center
                    float dx = x - width / 2.0f;
                    float dy = y - height / 2.0f;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    
                    // Create concentric circles with audio-reactive colors
                    float angle = (float)Math.Atan2(dy, dx);
                    float normalizedDistance = distance / (width / 2.0f);
                    
                    // Generate psychedelic color based on audio
                    uint color = GeneratePsychedelicColor(angle, normalizedDistance);
                    _frameBuffer![index] = color;
                }
            }
        }
        
        /// <summary>
        /// Render particles to RGB32 buffer
        /// </summary>
        private void RenderParticles()
        {
            int width = 800;
            int height = 600;
            
            foreach (var particle in _particles)
            {
                if (particle.Life > 0)
                {
                    int x = (int)particle.Position.X;
                    int y = (int)particle.Position.Y;
                    int size = (int)(particle.Size * particle.Life);
                    
                    // Draw particle as circle
                    for (int dy = -size; dy <= size; dy++)
                    {
                        for (int dx = -size; dx <= size; dx++)
                        {
                            if (dx * dx + dy * dy <= size * size)
                            {
                                int px = x + dx;
                                int py = y + dy;
                                
                                if (px >= 0 && px < width && py >= 0 && py < height)
                                {
                                    int index = py * width + px;
                                    _frameBuffer![index] = PackRGB32(particle.Color.Red, particle.Color.Green, particle.Color.Blue);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Render mandala pattern to RGB32 buffer
        /// </summary>
        private void RenderMandala()
        {
            int width = 800;
            int height = 600;
            int centerX = width / 2;
            int centerY = height / 2;
            
            // Draw rotating mandala
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45 + _rotation;
                float radius = 50 + _midIntensity * 100;
                
                int endX = centerX + (int)(Math.Cos(angle * Math.PI / 180) * radius);
                int endY = centerY + (int)(Math.Sin(angle * Math.PI / 180) * radius);
                
                // Draw line from center to end point
                DrawLine(centerX, centerY, endX, endY, GenerateAudioReactiveColor(i * 0.125f + _colorShift));
            }
        }
        
        /// <summary>
        /// Draw line to RGB32 buffer
        /// </summary>
        private void DrawLine(int x0, int y0, int x1, int y1, uint color)
        {
            int width = 800;
            int height = 600;
            
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            int x = x0;
            int y = y0;
            
            while (true)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    _frameBuffer![y * width + x] = color;
                }
                
                if (x == x1 && y == y1) break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }
        
        /// <summary>
        /// Generate psychedelic color based on audio
        /// </summary>
        private uint GeneratePsychedelicColor(float angle, float distance)
        {
            float hue = (angle + _colorShift * 360) % 360;
            float saturation = 0.8f + _pulseIntensity * 0.2f;
            float value = 0.5f + _bassIntensity * 0.5f;
            
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
        
        /// <summary>
        /// Generate audio-reactive color
        /// </summary>
        private uint GenerateAudioReactiveColor(float hue)
        {
            float adjustedHue = (hue * 360 + _colorShift * 360) % 360;
            return GeneratePsychedelicColor(adjustedHue, 0.5f);
        }
        
        /// <summary>
        /// Pack RGB values into RGB32 format
        /// </summary>
        private uint PackRGB32(byte r, byte g, byte b)
        {
            return (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        
        private SKColor GetRandomColor()
        {
            return SKColor.FromHsv(
                _random.NextSingle() * 360,
                80 + _random.NextSingle() * 20,
                80 + _random.NextSingle() * 20
            );
        }
        
        public void Update(AudioFeatures audioFeatures, float deltaTime)
        {
            _time += deltaTime;
            
            // Extract audio intensities
            _bassIntensity = audioFeatures.FrequencyBands.Length > 0 ? audioFeatures.FrequencyBands[0] : 0;
            _midIntensity = audioFeatures.FrequencyBands.Length > 3 ? audioFeatures.FrequencyBands[3] : 0;
            _trebleIntensity = audioFeatures.FrequencyBands.Length > 6 ? audioFeatures.FrequencyBands[6] : 0;
            
            // Update GOOM parameters based on audio
            _zoom = 1.0f + _bassIntensity * 0.5f;
            _rotation += _midIntensity * 0.1f;
            _colorShift += _trebleIntensity * 0.05f;
            _pulseIntensity = audioFeatures.Peak;
            
            // Update particles
            UpdateParticles(deltaTime);
        }
        
        private void UpdateParticles(float deltaTime)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                var particle = _particles[i];
                
                // Update position
                particle.Position += particle.Velocity * deltaTime;
                
                // Apply audio-reactive forces
                Vector2 audioForce = GetAudioForce(particle.Position);
                particle.Velocity += audioForce * deltaTime;
                
                // Apply damping
                particle.Velocity *= 0.98f;
                
                // Update life
                particle.Life -= deltaTime / particle.MaxLife;
                
                // Respawn if dead
                if (particle.Life <= 0)
                {
                    RespawnParticle(ref particle);
                }
                
                _particles[i] = particle;
            }
        }
        
        private Vector2 GetAudioForce(Vector2 position)
        {
            Vector2 direction = Vector2.Normalize(position - _center);
            float distance = Vector2.Distance(position, _center);
            
            // Bass creates outward force
            Vector2 bassForce = direction * _bassIntensity * 50;
            
            // Mid creates rotational force
            Vector2 midForce = new Vector2(-direction.Y, direction.X) * _midIntensity * 30;
            
            // Treble creates random jitter
            Vector2 trebleForce = new Vector2(
                (_random.NextSingle() - 0.5f) * _trebleIntensity * 20,
                (_random.NextSingle() - 0.5f) * _trebleIntensity * 20
            );
            
            return bassForce + midForce + trebleForce;
        }
        
        private void RespawnParticle(ref GoomParticle particle)
        {
            particle.Position = new Vector2(_random.NextSingle() * 800, _random.NextSingle() * 600);
            particle.Velocity = new Vector2((_random.NextSingle() - 0.5f) * 2, (_random.NextSingle() - 0.5f) * 2);
            particle.Color = GetRandomColor();
            particle.Size = _random.NextSingle() * 10 + 5;
            particle.Life = 1.0f;
            particle.MaxLife = _random.NextSingle() * 2 + 1;
        }
        
    }
    
    /// <summary>
    /// Particle for GOOM visualizer
    /// </summary>
    public struct GoomParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public SKColor Color;
        public float Size;
        public float Life;
        public float MaxLife;
    }
}
