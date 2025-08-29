using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Minecart Rollercoaster Visualizer - Procedural track generation with audio-reactive physics
/// Features multiple carts, dynamic track shapes, particle effects, and various camera modes
/// </summary>
public sealed class MinecartRollercoasterVisualizer : IVisualizerPlugin
{
    public string Id => "minecart_rollercoaster";
    public string DisplayName => "🛤️ Phoenix Cart Ride";

    private int _width, _height;
    private float _time;
    private readonly Random _random = new();

    // Track generation constants
    private const int TRACK_SEGMENTS = 100;
    private const float SEGMENT_LENGTH = 40f;
    private const float TRACK_WIDTH = 8f;
    private const float CART_SIZE = 12f;
    private const float GRAVITY = 600f;
    private const float MAX_SPEED = 400f;
    private const float MIN_SPEED = 50f;

    // Game state
    private List<TrackSegment> _trackSegments = new();
    private List<Minecart> _carts = new();
    private List<Particle> _particles = new();
    private List<SceneryItem> _sceneryItems = new();
    private float _trackOffset;
    private int _nextSegmentId;
    private float _cameraShake;

    // Audio-reactive parameters
    private float _bassAccumulator;
    private float _midAccumulator;
    private float _trebleAccumulator;

    // Visual settings
    private enum TrackStyle { Wooden, Metallic, Neon, HellRail }
    private enum CameraMode { FollowCart, InsideCart, Overhead, SideView }
    private TrackStyle _currentTrackStyle;
    private int _maxCarts = 3;
    private float _speedMultiplier = 1.0f;
    private float _trackCurvature = 1.0f;

    // Colors for different track styles
    private readonly uint[][] _trackColors = new uint[][]
    {
        // Wooden
        new uint[] { 0xFF8B4513, 0xFF654321, 0xFFA0522D },
        // Metallic
        new uint[] { 0xFFC0C0C0, 0xFF808080, 0xFF404040 },
        // Neon
        new uint[] { 0xFFFF0080, 0xFF00FFFF, 0xFFFFFF00 },
        // Hell Rail (fire themed)
        new uint[] { 0xFFFF4400, 0xFFFF6600, 0xFFFF8800 }
    };

    private struct TrackSegment
    {
        public int Id;
        public float X, Y, Z;
        public float BankAngle; // Side-to-side banking
        public float UpAngle;   // Up-down slope
        public uint Color;
        public float GlowIntensity;
        public TrackStyle Style;
    }

    private struct Minecart
    {
        public float Position; // Along track position (0-1 within segment)
        public int SegmentId;
        public float Speed;
        public float VerticalOffset; // Bouncing effect
        public uint Color;
        public float SparkTimer;
        public bool IsPlayerCart;
    }

    private struct Particle
    {
        public float X, Y, Z;
        public float VelocityX, VelocityY, VelocityZ;
        public uint Color;
        public float Life;
        public float MaxLife;
        public float Size;
        public ParticleType Type;
    }

    private struct SceneryItem
    {
        public float X, Z;
        public SceneryType Type;
        public float Scale;
        public uint Color;
    }

    private enum ParticleType { Spark, Dust, Flame, Glow }
    private enum SceneryType { Tree, Mountain, Cave, Cloud }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        _trackOffset = 0;
        _nextSegmentId = 0;
        _currentTrackStyle = TrackStyle.Wooden;

        // Initialize track
        _trackSegments = new List<TrackSegment>();
        GenerateInitialTrack();

        // Initialize carts
        _carts = new List<Minecart>();
        for (int i = 0; i < _maxCarts; i++)
        {
            SpawnCart(i == 0); // First cart is player cart
        }

        // Initialize particles and scenery
        _particles = new List<Particle>();
        _sceneryItems = new List<SceneryItem>();
        GenerateScenery();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        _trackSegments.Clear();
        _carts.Clear();
        _particles.Clear();
        _sceneryItems.Clear();
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Update audio reactivity
        UpdateAudioReactivity(f);

        // Update game logic
        UpdateTrack();
        UpdateCarts(f);
        UpdateParticles(f);
        UpdateScenery();

        // Render scene
        RenderBackground(canvas, f);
        RenderScenery(canvas, f);
        RenderTrack(canvas, f);
        RenderCarts(canvas, f);
        RenderParticles(canvas, f);
        RenderUI(canvas, f);

        // Apply camera effects
        if (_cameraShake > 0)
        {
            _cameraShake *= 0.9f;
        }
    }

    private void UpdateAudioReactivity(AudioFeatures f)
    {
        // Accumulate audio data for smoother response
        _bassAccumulator = _bassAccumulator * 0.8f + f.Bass * 0.2f;
        _midAccumulator = _midAccumulator * 0.8f + f.Mid * 0.2f;
        _trebleAccumulator = _trebleAccumulator * 0.8f + f.Treble * 0.2f;

        // Camera shake on beats
        if (f.Beat)
        {
            _cameraShake = 8f;
        }
    }

    private void UpdateTrack()
    {
        // Generate new track segments as we move forward
        while (_trackSegments.Count < TRACK_SEGMENTS)
        {
            GenerateTrackSegment();
        }

        // Remove old segments behind us
        while (_trackSegments.Count > 0 && _trackSegments[0].Z < -100)
        {
            _trackSegments.RemoveAt(0);
        }

        // Update track offset (scrolling effect)
        _trackOffset += 100f * _speedMultiplier * 0.016f;
    }

    private void UpdateCarts(AudioFeatures f)
    {
        for (int i = 0; i < _carts.Count; i++)
        {
            var cart = _carts[i];

            // Audio-reactive speed
            float baseSpeed = 100f + _bassAccumulator * 200f;
            baseSpeed *= _speedMultiplier;
            baseSpeed = Math.Clamp(baseSpeed, MIN_SPEED, MAX_SPEED);

            cart.Speed = baseSpeed;

            // Update cart position along track
            cart.Position += cart.Speed * 0.016f / SEGMENT_LENGTH;

            // Wrap around to next segment
            while (cart.Position >= 1.0f)
            {
                cart.Position -= 1.0f;
                cart.SegmentId++;

                // Find next valid segment
                bool segmentFound = false;
                foreach (var segment in _trackSegments)
                {
                    if (segment.Id == cart.SegmentId)
                    {
                        segmentFound = true;
                        break;
                    }
                }

                if (!segmentFound) // Segment not found
                {
                    cart.SegmentId = _trackSegments.Count > 0 ? _trackSegments[0].Id : 0;
                    cart.Position = 0;
                }
            }

            // Add bouncing effect
            cart.VerticalOffset = (float)Math.Sin(_time * 8f + cart.Position * 10f) * 3f;

            // Create sparks on high speed
            if (cart.Speed > 200f && _random.NextDouble() < 0.3f)
            {
                CreateSparkParticles(cart);
            }

            // Audio-reactive effects
            if (f.Beat && cart.IsPlayerCart)
            {
                CreateBeatParticles(cart);
            }

            _carts[i] = cart; // Update the cart in the list
        }
    }

    private void UpdateParticles(AudioFeatures f)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];

            // Apply physics
            particle.VelocityY += GRAVITY * 0.1f * 0.016f;

            // Update position
            particle.X += particle.VelocityX * 0.016f;
            particle.Y += particle.VelocityY * 0.016f;
            particle.Z += particle.VelocityZ * 0.016f;

            // Update life
            particle.Life -= 0.016f;

            // Special effects based on type
            switch (particle.Type)
            {
                case ParticleType.Flame:
                    particle.Y -= 20f * 0.016f; // Flames rise
                    break;
                case ParticleType.Glow:
                    particle.Size *= 0.98f; // Glow fades
                    break;
            }

            // Remove dead particles
            if (particle.Life <= 0)
            {
                _particles.RemoveAt(i);
                continue;
            }

            _particles[i] = particle; // Update the particle in the list
        }
    }

    private void UpdateScenery()
    {
        for (int i = _sceneryItems.Count - 1; i >= 0; i--)
        {
            var scenery = _sceneryItems[i];

            // Update scenery positions based on track movement
            scenery.Z -= 100f * _speedMultiplier * 0.016f;

            // Remove scenery that's too far back
            if (scenery.Z < -200)
            {
                _sceneryItems.RemoveAt(i);
                continue;
            }

            _sceneryItems[i] = scenery; // Update the scenery in the list
        }

        // Add new scenery occasionally
        if (_random.NextDouble() < 0.02f)
        {
            GenerateSceneryItem();
        }
    }

    private void GenerateInitialTrack()
    {
        for (int i = 0; i < TRACK_SEGMENTS; i++)
        {
            GenerateTrackSegment();
        }
    }

    private void GenerateTrackSegment()
    {
        float z = _nextSegmentId * SEGMENT_LENGTH;
        float x = 0;
        float y = 0;

        // Audio-reactive track shape
        if (_trackSegments.Count > 0)
        {
            var lastSegment = _trackSegments[_trackSegments.Count - 1];
            x = lastSegment.X;
            y = lastSegment.Y;

            // Add variation based on audio
            x += (_random.NextSingle() - 0.5f) * 20f * _trackCurvature;
            y += (_random.NextSingle() - 0.5f) * 15f * _trebleAccumulator * 2f;

            // Smooth transitions
            x = x * 0.7f + lastSegment.X * 0.3f;
            y = y * 0.7f + lastSegment.Y * 0.3f;
        }

        var segment = new TrackSegment
        {
            Id = _nextSegmentId++,
            X = x,
            Y = y,
            Z = z,
            BankAngle = _midAccumulator * 0.5f, // Midrange controls banking
            UpAngle = _trebleAccumulator * 0.3f, // Treble controls slope
            Color = _trackColors[(int)_currentTrackStyle][_random.Next(_trackColors[(int)_currentTrackStyle].Length)],
            GlowIntensity = _bassAccumulator * 0.5f,
            Style = _currentTrackStyle
        };

        _trackSegments.Add(segment);
    }

    private void GenerateScenery()
    {
        for (int i = 0; i < 20; i++)
        {
            GenerateSceneryItem();
        }
    }

    private void GenerateSceneryItem()
    {
        var scenery = new SceneryItem
        {
            X = (_random.NextSingle() - 0.5f) * 400f,
            Z = _random.NextSingle() * 400f + 200f,
            Type = (SceneryType)_random.Next(Enum.GetValues(typeof(SceneryType)).Length),
            Scale = 0.5f + _random.NextSingle() * 1.5f,
            Color = GetSceneryColor()
        };

        _sceneryItems.Add(scenery);
    }

    private void SpawnCart(bool isPlayerCart)
    {
        var cart = new Minecart
        {
            Position = _random.NextSingle() * 0.5f, // Start within first half of track
            SegmentId = _trackSegments.Count > 0 ? _trackSegments[0].Id : 0,
            Speed = 100f,
            VerticalOffset = 0,
            Color = isPlayerCart ? 0xFFFF0000 : 0xFF00FF00,
            SparkTimer = 0,
            IsPlayerCart = isPlayerCart
        };

        _carts.Add(cart);
    }

    private void CreateSparkParticles(Minecart cart)
    {
        // Get cart world position
        TrackSegment? foundSegment = null;
        foreach (var seg in _trackSegments)
        {
            if (seg.Id == cart.SegmentId)
            {
                foundSegment = seg;
                break;
            }
        }

        if (foundSegment == null || foundSegment.Value.Id == 0) return;

        var segment = foundSegment.Value;
        float cartX = segment.X;
        float cartY = segment.Y + cart.VerticalOffset;
        float cartZ = segment.Z + cart.Position * SEGMENT_LENGTH;

        for (int i = 0; i < 3; i++)
        {
            var particle = new Particle
            {
                X = cartX + (_random.NextSingle() - 0.5f) * 10f,
                Y = cartY - 5f,
                Z = cartZ,
                VelocityX = (_random.NextSingle() - 0.5f) * 100f,
                VelocityY = -_random.NextSingle() * 50f,
                VelocityZ = (_random.NextSingle() - 0.5f) * 50f,
                Color = 0xFFFFFF00, // Yellow sparks
                Life = 0.5f + _random.NextSingle() * 0.5f,
                MaxLife = 1.0f,
                Size = 2f + _random.NextSingle() * 2f,
                Type = ParticleType.Spark
            };

            _particles.Add(particle);
        }
    }

    private void CreateBeatParticles(Minecart cart)
    {
        TrackSegment? foundSegment = null;
        foreach (var seg in _trackSegments)
        {
            if (seg.Id == cart.SegmentId)
            {
                foundSegment = seg;
                break;
            }
        }

        if (foundSegment == null || foundSegment.Value.Id == 0) return;

        var segment = foundSegment.Value;
        float cartX = segment.X;
        float cartY = segment.Y + cart.VerticalOffset;
        float cartZ = segment.Z + cart.Position * SEGMENT_LENGTH;

        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(i * Math.PI * 2 / 8);
            var particle = new Particle
            {
                X = cartX,
                Y = cartY,
                Z = cartZ,
                VelocityX = (float)Math.Cos(angle) * 150f,
                VelocityY = (float)Math.Sin(angle) * 150f,
                VelocityZ = (_random.NextSingle() - 0.5f) * 50f,
                Color = HsvToRgb(_random.NextSingle(), 1.0f, 1.0f), // Rainbow colors
                Life = 1.0f,
                MaxLife = 1.0f,
                Size = 3f,
                Type = ParticleType.Glow
            };

            _particles.Add(particle);
        }
    }

    private uint GetSceneryColor()
    {
        return _random.Next(3) switch
        {
            0 => 0xFF228B22, // Forest green (trees)
            1 => 0xFF8B4513, // Brown (mountains)
            2 => 0xFF696969, // Gray (caves)
            _ => 0xFFFFFFFF  // White (clouds)
        };
    }

    private void RenderBackground(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Create gradient sky
        uint topColor = 0xFF87CEEB; // Sky blue
        uint bottomColor = 0xFF4682B4; // Steel blue

        // Audio-reactive sky colors
        if (_currentTrackStyle == TrackStyle.HellRail)
        {
            topColor = AdjustBrightness(0xFFFF4500, 0.7f); // Orange-red
            bottomColor = AdjustBrightness(0xFF8B0000, 0.8f); // Dark red
        }
        else if (_currentTrackStyle == TrackStyle.Neon)
        {
            topColor = 0xFF191970; // Midnight blue
            bottomColor = 0xFF000080; // Navy
        }

        canvas.Clear(bottomColor);

        // Add gradient effect (simplified)
        for (int y = 0; y < _height / 2; y++)
        {
            float t = (float)y / (_height / 2);
            uint color = InterpolateColor(bottomColor, topColor, t);
            canvas.DrawLine(0, y, _width, y, color, 1f);
        }
    }

    private void RenderScenery(ISkiaCanvas canvas, AudioFeatures f)
    {
        foreach (var scenery in _sceneryItems)
        {
            // Skip scenery that's too far or behind camera
            if (scenery.Z < 10 || scenery.Z > 300) continue;

            // Project to screen coordinates
            float screenX = _width * 0.5f + scenery.X / scenery.Z * 200f;
            float screenY = _height * 0.7f - scenery.Scale * 50f / scenery.Z * 100f;

            if (screenX < -100 || screenX > _width + 100) continue;

            // Render based on type
            switch (scenery.Type)
            {
                case SceneryType.Tree:
                    RenderTree(canvas, screenX, screenY, scenery.Scale, scenery.Color);
                    break;
                case SceneryType.Mountain:
                    RenderMountain(canvas, screenX, screenY, scenery.Scale, scenery.Color);
                    break;
                case SceneryType.Cave:
                    RenderCave(canvas, screenX, screenY, scenery.Scale, scenery.Color);
                    break;
                case SceneryType.Cloud:
                    RenderCloud(canvas, screenX, screenY, scenery.Scale, scenery.Color);
                    break;
            }
        }
    }

    private void RenderTrack(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Sort track segments by Z for proper rendering order
        var visibleSegments = _trackSegments.Where(s => s.Z >= -50).OrderByDescending(s => s.Z).ToList();

        for (int i = 0; i < visibleSegments.Count; i++)
        {
            var segment = visibleSegments[i];

            float screenX = _width * 0.5f + segment.X;
            float screenY = _height * 0.8f - segment.Y - segment.Z * 0.1f;

            // Apply camera shake
            screenX += (float)Math.Sin(_time * 20) * _cameraShake;
            screenY += (float)Math.Cos(_time * 18) * _cameraShake * 0.5f;

            // Render track rails with proper connections
            uint railColor = segment.Color;
            float railWidth = TRACK_WIDTH;

            // Add glow effect for energy
            if (segment.GlowIntensity > 0.1f)
            {
                uint glowColor = AdjustBrightness(railColor, 2.0f);
                canvas.FillRect(screenX - railWidth * 0.7f, screenY - 3, railWidth * 1.4f, 6, glowColor);
            }

            // Main rails with thickness based on Z-depth
            float railThickness = Math.Max(1, 3 - Math.Abs(segment.Z) * 0.05f);
            canvas.FillRect(screenX - railWidth * 0.5f, screenY - railThickness * 0.5f, railWidth, railThickness, railColor);

            // Connect to next segment for continuity
            if (i < visibleSegments.Count - 1)
            {
                var nextSegment = visibleSegments[i + 1];
                float nextScreenX = _width * 0.5f + nextSegment.X;
                float nextScreenY = _height * 0.8f - nextSegment.Y - nextSegment.Z * 0.1f;

                nextScreenX += (float)Math.Sin(_time * 20) * _cameraShake;
                nextScreenY += (float)Math.Cos(_time * 18) * _cameraShake * 0.5f;

                // Draw connecting rail segments
                canvas.DrawLine(screenX - railWidth * 0.5f, screenY, nextScreenX - railWidth * 0.5f, nextScreenY, railColor, railThickness);
                canvas.DrawLine(screenX + railWidth * 0.5f, screenY, nextScreenX + railWidth * 0.5f, nextScreenY, railColor, railThickness);
            }

            // Rail ties - spaced and connected
            float tieSpacing = SEGMENT_LENGTH / 3f;
            for (int t = 0; t < 3; t++)
            {
                float tieZ = segment.Z + t * tieSpacing;
                float tieScreenY = _height * 0.8f - segment.Y - tieZ * 0.1f;

                if (tieZ >= -50 && tieZ <= 50)
                {
                    uint tieColor = AdjustBrightness(railColor, 0.4f);
                    canvas.FillRect(screenX - railWidth * 0.6f, tieScreenY + 2, railWidth * 1.2f, 6, tieColor);
                }
            }

            // Add track supports/pillars for elevated sections
            if (segment.Y > 10)
            {
                float pillarHeight = segment.Y * 0.8f;
                uint pillarColor = AdjustBrightness(railColor, 0.3f);
                canvas.FillRect(screenX - 3, screenY + railThickness, 6, pillarHeight, pillarColor);
            }
        }
    }

    private void RenderCarts(ISkiaCanvas canvas, AudioFeatures f)
    {
        foreach (var cart in _carts)
        {
            TrackSegment? foundSegment = null;
            foreach (var seg in _trackSegments)
            {
                if (seg.Id == cart.SegmentId)
                {
                    foundSegment = seg;
                    break;
                }
            }

            if (foundSegment == null || foundSegment.Value.Id == 0) continue;

            var segment = foundSegment.Value;
            float cartX = segment.X + cart.Position * 10f; // Slight forward offset
            float cartY = segment.Y + cart.VerticalOffset;
            float cartZ = segment.Z + cart.Position * SEGMENT_LENGTH;

            // Skip carts that are too far back
            if (cartZ < -20) continue;

            // Project to screen
            float screenX = _width * 0.5f + cartX;
            float screenY = _height * 0.8f - cartY - cartZ * 0.1f;

            // Apply camera shake
            screenX += (float)Math.Sin(_time * 20) * _cameraShake;
            screenY += (float)Math.Cos(_time * 18) * _cameraShake * 0.5f;

            // Render cart body (more detailed minecart shape)
            float cartW = CART_SIZE;
            float cartH = CART_SIZE * 0.6f;

            // Main cart body
            canvas.FillRect(screenX - cartW * 0.4f, screenY - cartH * 0.3f, cartW * 0.8f, cartH * 0.6f, cart.Color);

            // Cart sides (angled for 3D effect)
            canvas.FillRect(screenX - cartW * 0.5f, screenY - cartH * 0.2f, cartW * 0.1f, cartH * 0.4f, AdjustBrightness(cart.Color, 0.8f));
            canvas.FillRect(screenX + cartW * 0.4f, screenY - cartH * 0.2f, cartW * 0.1f, cartH * 0.4f, AdjustBrightness(cart.Color, 0.8f));

            // Cart front and back
            canvas.FillRect(screenX - cartW * 0.45f, screenY - cartH * 0.4f, cartW * 0.05f, cartH * 0.5f, AdjustBrightness(cart.Color, 0.9f));
            canvas.FillRect(screenX + cartW * 0.4f, screenY - cartH * 0.4f, cartW * 0.05f, cartH * 0.5f, AdjustBrightness(cart.Color, 0.9f));

            // Render cart wheels with proper positioning
            uint wheelColor = 0xFF222222;
            float wheelRadius = 4;
            canvas.FillCircle(screenX - cartW * 0.25f, screenY + cartH * 0.2f, wheelRadius, wheelColor);
            canvas.FillCircle(screenX + cartW * 0.25f, screenY + cartH * 0.2f, wheelRadius, wheelColor);

            // Wheel centers
            canvas.FillCircle(screenX - cartW * 0.25f, screenY + cartH * 0.2f, wheelRadius * 0.4f, 0xFF444444);
            canvas.FillCircle(screenX + cartW * 0.25f, screenY + cartH * 0.2f, wheelRadius * 0.4f, 0xFF444444);

            // Add cart details (rails on top)
            canvas.FillRect(screenX - cartW * 0.35f, screenY - cartH * 0.45f, cartW * 0.7f, 2, AdjustBrightness(cart.Color, 1.2f));

            // Add speed lines for fast carts
            if (cart.Speed > 250f)
            {
                for (int i = 0; i < 3; i++)
                {
                    float lineX = screenX - cartW * 0.6f - i * 8;
                    canvas.DrawLine(lineX, screenY, lineX - 15, screenY, 0x80FFFFFF, 2f);
                }
            }

            // Special effects for player cart
            if (cart.IsPlayerCart)
            {
                // Add glow effect
                uint glowColor = AdjustBrightness(cart.Color, 1.5f);
                canvas.FillRect(screenX - cartW * 0.6f, screenY - cartH * 0.6f, cartW * 1.2f, cartH * 1.2f, glowColor);
            }
        }
    }

    private void RenderParticles(ISkiaCanvas canvas, AudioFeatures f)
    {
        foreach (var particle in _particles)
        {
            // Skip particles that are too far back
            if (particle.Z < -10) continue;

            // Project to screen
            float screenX = _width * 0.5f + particle.X;
            float screenY = _height * 0.8f - particle.Y - particle.Z * 0.1f;

            if (screenX < -50 || screenX > _width + 50 || screenY < -50 || screenY > _height + 50)
                continue;

            float alpha = particle.Life / particle.MaxLife;
            uint fadedColor = (uint)((uint)(alpha * 255) << 24 | (particle.Color & 0x00FFFFFF));

            canvas.FillCircle(screenX, screenY, particle.Size, fadedColor);
        }
    }

    private void RenderUI(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Render speed and audio info
        string speedText = $"Speed: {_speedMultiplier:F1}x";
        canvas.DrawText(speedText, 20, 40, 0xFFFFFFFF, 18f);

        string cartText = $"Carts: {_carts.Count}";
        canvas.DrawText(cartText, 20, 65, 0xFFFFFFFF, 16f);

        string trackText = $"Track: {_currentTrackStyle}";
        canvas.DrawText(trackText, 20, 85, 0xFFFFFFFF, 16f);

        // Audio meters
        string bassText = $"Bass: {_bassAccumulator:F2}";
        canvas.DrawText(bassText, _width - 150, 40, 0xFFFF0000, 14f);

        string midText = $"Mid: {_midAccumulator:F2}";
        canvas.DrawText(midText, _width - 150, 60, 0xFF00FF00, 14f);

        string trebleText = $"Treble: {_trebleAccumulator:F2}";
        canvas.DrawText(trebleText, _width - 150, 80, 0xFF0000FF, 14f);
    }

    // Helper rendering methods for scenery
    private void RenderTree(ISkiaCanvas canvas, float x, float y, float scale, uint color)
    {
        float trunkWidth = 4 * scale;
        float trunkHeight = 20 * scale;
        float crownRadius = 15 * scale;

        // Trunk
        canvas.FillRect(x - trunkWidth * 0.5f, y, trunkWidth, trunkHeight, 0xFF8B4513);

        // Crown
        canvas.FillCircle(x, y - crownRadius * 0.5f, crownRadius, color);
    }

    private void RenderMountain(ISkiaCanvas canvas, float x, float y, float scale, uint color)
    {
        float width = 60 * scale;
        float height = 40 * scale;

        // Simple triangular mountain
        var points = new (float x, float y)[]
        {
            (x - width * 0.5f, y + height),
            (x, y),
            (x + width * 0.5f, y + height)
        };

        // Fill triangle (simplified)
        canvas.FillRect(x - width * 0.5f, y, width, height, color);
    }

    private void RenderCave(ISkiaCanvas canvas, float x, float y, float scale, uint color)
    {
        float width = 40 * scale;
        float height = 30 * scale;

        // Cave entrance
        canvas.FillRect(x - width * 0.5f, y, width, height, color);
        canvas.FillCircle(x, y + height * 0.5f, width * 0.3f, AdjustBrightness(color, 0.7f));
    }

    private void RenderCloud(ISkiaCanvas canvas, float x, float y, float scale, uint color)
    {
        float size = 25 * scale;

        // Puffy cloud shape
        canvas.FillCircle(x - size * 0.4f, y, size * 0.6f, color);
        canvas.FillCircle(x + size * 0.4f, y, size * 0.6f, color);
        canvas.FillCircle(x, y - size * 0.3f, size * 0.5f, color);
    }

    private uint AdjustBrightness(uint color, float factor)
    {
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);

        r = (byte)Math.Min(255, r * factor);
        g = (byte)Math.Min(255, g * factor);
        b = (byte)Math.Min(255, b * factor);

        return (uint)(0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b);
    }

    private uint InterpolateColor(uint color1, uint color2, float t)
    {
        byte r1 = (byte)((color1 >> 16) & 0xFF);
        byte g1 = (byte)((color1 >> 8) & 0xFF);
        byte b1 = (byte)(color1 & 0xFF);

        byte r2 = (byte)((color2 >> 16) & 0xFF);
        byte g2 = (byte)((color2 >> 8) & 0xFF);
        byte b2 = (byte)(color2 & 0xFF);

        byte r = (byte)(r1 + (r2 - r1) * t);
        byte g = (byte)(g1 + (g2 - g1) * t);
        byte b = (byte)(b1 + (b2 - b1) * t);

        return (uint)(0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b);
    }

    private uint HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1 - MathF.Abs((h * 6) % 2 - 1));
        float m = v - c;

        float r, g, b;
        if (h < 1f/6f) { r = c; g = x; b = 0; }
        else if (h < 2f/6f) { r = x; g = c; b = 0; }
        else if (h < 3f/6f) { r = 0; g = c; b = x; }
        else if (h < 4f/6f) { r = 0; g = x; b = c; }
        else if (h < 5f/6f) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        byte R = (byte)((r + m) * 255);
        byte G = (byte)((g + m) * 255);
        byte B = (byte)((b + m) * 255);

        return 0xFF000000u | ((uint)R << 16) | ((uint)G << 8) | (uint)B;
    }
}
