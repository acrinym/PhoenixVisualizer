using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Nyan Cat Visualizer - Classic rainbow cat that flies across the screen with audio-reactive rainbow trail
/// Inspired by the legendary Nyan Cat meme with enhanced audio reactivity and customizable parameters
/// </summary>
public sealed class NyanCatVisualizer : IVisualizerPlugin
{
    public string Id => "nyan_cat";
    public string DisplayName => "🐱🌈 Nyan Cat";

    private int _width, _height;
    private float _time;
    private readonly Random _random = new();

    // Nyan Cat system constants
    private const int MAX_TRAIL_SEGMENTS = 200;
    private const int MAX_STARS = 150;
    private const int MAX_SPARKLES = 50;
    private const float CAT_WIDTH = 64f;
    private const float CAT_HEIGHT = 32f;

    // Cat state
    private float _catX, _catY;
    private float _catVelocityX, _catVelocityY;
    private float _catRotation;
    private float _catBobOffset;
    private float _catSpeed;
    private bool _isFlipping;
    private float _flipProgress;

    // Rainbow trail system
    private readonly List<RainbowSegment> _trailSegments;
    private float _trailLength;
    private float _trailWidth;

    // Star field system
    private readonly List<Star> _stars;
    private readonly List<Sparkle> _sparkles;
    private float _sparkleDensity;

    // Audio state
    private float _bassAccumulator;
    private float _midAccumulator;
    private float _trebleAccumulator;
    private float _lastBeatTime;
    private float _lastPeakTime;

    // Parameters
    private float _trailLengthParam = 0.7f; // 0-1
    private float _sparkleAmountParam = 0.6f; // 0-1
    private float _catSpeedParam = 1.0f; // 0.5-2.0
    private MovementMode _movementMode = MovementMode.AudioReactive;

    // Rainbow colors (ROYGBIV)
    private readonly uint[] _rainbowColors = new uint[]
    {
        0xFFFF0000, // Red
        0xFFFF8000, // Orange
        0xFFFFFF00, // Yellow
        0xFF00FF00, // Green
        0xFF0080FF, // Blue
        0xFF8000FF, // Indigo
        0xFFFF00FF  // Violet
    };

    public enum MovementMode
    {
        Classic,
        AudioReactive,
        Chaotic
    }

    public NyanCatVisualizer()
    {
        _trailSegments = new List<RainbowSegment>();
        _stars = new List<Star>();
        _sparkles = new List<Sparkle>();
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;

        // Initialize cat position
        _catX = -CAT_WIDTH;
        _catY = _height * 0.5f;
        _catVelocityX = 2f;
        _catVelocityY = 0;
        _catRotation = 0;
        _catBobOffset = 0;
        _catSpeed = 1.0f;

        // Initialize systems
        InitializeStars();
        InitializeSparkles();
        ResetTrail();

        // Audio state
        _bassAccumulator = 0;
        _midAccumulator = 0;
        _trebleAccumulator = 0;
        _lastBeatTime = 0;
        _lastPeakTime = 0;

        // Parameters
        _trailLength = 0.7f;
        _trailWidth = 8f;
        _sparkleDensity = 0.6f;
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        _trailSegments.Clear();
        _stars.Clear();
        _sparkles.Clear();
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Update audio reactivity
        UpdateAudioReactivity(f);

        // Update cat movement
        UpdateCatMovement(f);

        // Update trail
        UpdateTrail(f);

        // Update stars and sparkles
        UpdateStarsAndSparkles(f);

        // Render everything
        RenderScene(canvas, f);

        // Render UI
        RenderUI(canvas, f);
    }

    private void UpdateAudioReactivity(AudioFeatures f)
    {
        // Update audio accumulators with smoothing
        _bassAccumulator = _bassAccumulator * 0.95f + f.Bass * 0.05f;
        _midAccumulator = _midAccumulator * 0.95f + f.Mid * 0.05f;
        _trebleAccumulator = _trebleAccumulator * 0.95f + f.Treble * 0.05f;

        // Update parameters based on audio
        _trailLength = _trailLengthParam * (0.5f + _midAccumulator * 0.5f);
        _trailWidth = 6f + _bassAccumulator * 12f;
        _sparkleDensity = _sparkleAmountParam * (0.3f + _trebleAccumulator * 0.7f);
        _catSpeed = _catSpeedParam * (0.8f + f.Volume * 0.4f);

        // Beat detection for flips
        if (f.Beat && _time - _lastBeatTime > 0.2f)
        {
            _lastBeatTime = _time;

            // Strong bass = flip chance
            if (_bassAccumulator > 0.7f && _random.NextDouble() < 0.6f)
            {
                TriggerFlip();
            }
        }

        // Peak detection for special effects
        if (f.Volume > 0.8f && _time - _lastPeakTime > 1.0f)
        {
            _lastPeakTime = _time;
            TriggerPeakEffect();
        }
    }

    private void UpdateCatMovement(AudioFeatures f)
    {
        // Update horizontal movement
        _catVelocityX = 2f * _catSpeed;

        // Update vertical movement based on mode
        switch (_movementMode)
        {
            case MovementMode.Classic:
                // Simple sine wave bobbing
                _catBobOffset += 0.05f;
                _catVelocityY = (float)Math.Sin(_catBobOffset) * 1f;
                break;

            case MovementMode.AudioReactive:
                // Bass-reactive bobbing
                _catBobOffset += 0.05f + _bassAccumulator * 0.1f;
                _catVelocityY = (float)Math.Sin(_catBobOffset) * (2f + _bassAccumulator * 4f);
                break;

            case MovementMode.Chaotic:
                // Random chaotic movement
                _catVelocityY += (float)(_random.NextDouble() - 0.5) * 4f;
                _catVelocityY *= 0.95f; // Dampening
                _catVelocityY = Math.Clamp(_catVelocityY, -8f, 8f);
                break;
        }

        // Update position
        _catX += _catVelocityX;
        _catY += _catVelocityY;

        // Keep cat on screen vertically
        _catY = Math.Clamp(_catY, CAT_HEIGHT, _height - CAT_HEIGHT);

        // Handle flip animation
        if (_isFlipping)
        {
            _flipProgress += 0.1f;
            _catRotation = (float)Math.Sin(_flipProgress * Math.PI) * (float)Math.PI * 2;

            if (_flipProgress >= 1f)
            {
                _isFlipping = false;
                _flipProgress = 0;
                _catRotation = 0;
            }
        }

        // Reset cat when it goes off screen
        if (_catX > _width + CAT_WIDTH)
        {
            ResetCat();
        }
    }

    private void UpdateTrail(AudioFeatures f)
    {
        // Add new trail segment
        if (_trailSegments.Count == 0 || _trailSegments.Count < MAX_TRAIL_SEGMENTS)
        {
            var newSegment = new RainbowSegment(
                _catX, _catY,
                _trailWidth,
                GetRainbowColor(_trailSegments.Count),
                1.0f
            );
            _trailSegments.Add(newSegment);
        }
        else if (_trailSegments.Count >= MAX_TRAIL_SEGMENTS)
        {
            // Remove old segments to maintain length
            int segmentsToRemove = Math.Max(1, (int)(_trailSegments.Count * (1f - _trailLength)));
            _trailSegments.RemoveRange(0, segmentsToRemove);
        }

        // Update existing segments (fade and shrink)
        for (int i = _trailSegments.Count - 1; i >= 0; i--)
        {
            var segment = _trailSegments[i];
            segment.Life -= 0.02f;

            if (segment.Life <= 0)
            {
                _trailSegments.RemoveAt(i);
            }
            else
            {
                // Fade based on life
                segment.Alpha = segment.Life;

                // Shrink based on life
                segment.Width = _trailWidth * segment.Life;
            }
        }
    }

    private void UpdateStarsAndSparkles(AudioFeatures f)
    {
        // Update star twinkling
        foreach (var star in _stars)
        {
            star.TwinklePhase += 0.05f + _trebleAccumulator * 0.1f;
            star.Brightness = 0.3f + (float)Math.Sin(star.TwinklePhase) * 0.7f;
        }

        // Update sparkles
        for (int i = _sparkles.Count - 1; i >= 0; i--)
        {
            var sparkle = _sparkles[i];
            sparkle.Life -= 0.02f;
            sparkle.Size *= 0.98f;

            if (sparkle.Life <= 0 || sparkle.Size < 1f)
            {
                _sparkles.RemoveAt(i);
            }
        }

        // Add new sparkles based on density
        if (_random.NextDouble() < _sparkleDensity * 0.1f && _sparkles.Count < MAX_SPARKLES)
        {
            float x = (float)(_random.NextDouble() * _width);
            float y = (float)(_random.NextDouble() * _height * 0.6f); // Top 60% of screen

            var sparkle = new Sparkle(x, y, 3f + (float)_random.NextDouble() * 4f, 1.0f);
            _sparkles.Add(sparkle);
        }
    }

    private void TriggerFlip()
    {
        if (!_isFlipping)
        {
            _isFlipping = true;
            _flipProgress = 0;
        }
    }

    private void TriggerPeakEffect()
    {
        // Add burst of sparkles
        for (int i = 0; i < 10; i++)
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float distance = 20f + (float)_random.NextDouble() * 40f;
            float x = _catX + (float)Math.Cos(angle) * distance;
            float y = _catY + (float)Math.Sin(angle) * distance;

            var sparkle = new Sparkle(x, y, 5f + (float)_random.NextDouble() * 5f, 1.5f);
            _sparkles.Add(sparkle);
        }
    }

    private void ResetCat()
    {
        _catX = -CAT_WIDTH;
        _catY = _height * 0.5f;
        _catVelocityY = 0;
        _catBobOffset = 0;
        _isFlipping = false;
        _flipProgress = 0;
        _catRotation = 0;
    }

    private void InitializeStars()
    {
        _stars.Clear();
        for (int i = 0; i < MAX_STARS; i++)
        {
            float x = (float)(_random.NextDouble() * _width);
            float y = (float)(_random.NextDouble() * _height * 0.7f); // Top 70% of screen
            float size = 1f + (float)_random.NextDouble() * 2f;

            var star = new Star(x, y, size, (float)(_random.NextDouble() * Math.PI * 2));
            _stars.Add(star);
        }
    }

    private void InitializeSparkles()
    {
        _sparkles.Clear();
        // Sparkles will be added dynamically
    }

    private void ResetTrail()
    {
        _trailSegments.Clear();
    }

    private void RenderScene(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Black space background
        canvas.Clear(0xFF000000);

        // Render stars
        foreach (var star in _stars)
        {
            RenderStar(canvas, star);
        }

        // Render sparkles
        foreach (var sparkle in _sparkles)
        {
            RenderSparkle(canvas, sparkle);
        }

        // Render rainbow trail
        foreach (var segment in _trailSegments)
        {
            RenderTrailSegment(canvas, segment);
        }

        // Render Nyan Cat
        RenderNyanCat(canvas);
    }

    private void RenderStar(ISkiaCanvas canvas, Star star)
    {
        uint starColor = (uint)(star.Brightness * 255) << 24 | 0xFFFFFF;
        canvas.FillCircle(star.X, star.Y, star.Size, starColor);
    }

    private void RenderSparkle(ISkiaCanvas canvas, Sparkle sparkle)
    {
        uint sparkleColor = (uint)(sparkle.Life * 255) << 24 | 0xFFFFFF;
        canvas.FillCircle(sparkle.X, sparkle.Y, sparkle.Size, sparkleColor);
    }

    private void RenderTrailSegment(ISkiaCanvas canvas, RainbowSegment segment)
    {
        uint trailColor = (segment.Color & 0x00FFFFFF) | ((uint)(segment.Alpha * 255) << 24);
        canvas.FillCircle(segment.X, segment.Y, segment.Width * 0.5f, trailColor);
    }

    private void RenderNyanCat(ISkiaCanvas canvas)
    {
        // Save current transformation
        // Note: Since ISkiaCanvas doesn't have transformation methods,
        // we'll implement rotation through manual coordinate calculation

        // Render cat body (Pop-Tart)
        RenderCatBody(canvas);

        // Render cat head
        RenderCatHead(canvas);

        // Render rainbow trail connection
        RenderTrailConnection(canvas);
    }

    private void RenderCatBody(ISkiaCanvas canvas)
    {
        // Main body rectangle with rounded corners simulation
        float bodyWidth = CAT_WIDTH * 0.8f;
        float bodyHeight = CAT_HEIGHT * 0.6f;
        float bodyX = _catX - bodyWidth * 0.5f;
        float bodyY = _catY - bodyHeight * 0.5f;

        // Pink body color
        uint bodyColor = 0xFFFF69B4;
        canvas.FillRect(bodyX, bodyY, bodyWidth, bodyHeight, bodyColor);

        // White frosting stripes
        uint frostingColor = 0xFFFFFFFF;
        float stripeHeight = bodyHeight * 0.15f;

        for (int i = 0; i < 3; i++)
        {
            float stripeY = bodyY + i * (bodyHeight * 0.25f) + stripeHeight * 0.5f;
            canvas.FillRect(bodyX, stripeY, bodyWidth, stripeHeight, frostingColor);
        }

        // Sprinkle dots
        uint sprinkleColors = 0xFFFF0000; // Red sprinkles
        for (int i = 0; i < 8; i++)
        {
            float sprinkleX = bodyX + (i % 4) * (bodyWidth / 4) + bodyWidth / 8;
            float sprinkleY = bodyY + (i / 4) * (bodyHeight / 2) + bodyHeight / 4;
            canvas.FillCircle(sprinkleX, sprinkleY, 2f, sprinkleColors);
        }
    }

    private void RenderCatHead(ISkiaCanvas canvas)
    {
        float headSize = CAT_HEIGHT * 0.8f;
        float headX = _catX + CAT_WIDTH * 0.3f;
        float headY = _catY;

        // Cat head (tan color)
        uint headColor = 0xFFD2B48C;
        canvas.FillCircle(headX, headY, headSize * 0.5f, headColor);

        // Cat ears
        uint earColor = 0xFFC4A484;
        float earOffset = headSize * 0.3f;

        canvas.FillCircle(headX - earOffset, headY - earOffset, headSize * 0.2f, earColor);
        canvas.FillCircle(headX + earOffset, headY - earOffset, headSize * 0.2f, earColor);

        // Inner ears
        uint innerEarColor = 0xFFFF69B4;
        canvas.FillCircle(headX - earOffset, headY - earOffset, headSize * 0.1f, innerEarColor);
        canvas.FillCircle(headX + earOffset, headY - earOffset, headSize * 0.1f, innerEarColor);

        // Eyes
        uint eyeColor = 0xFF000000;
        float eyeOffset = headSize * 0.2f;
        canvas.FillCircle(headX - eyeOffset, headY - headSize * 0.1f, 3f, eyeColor);
        canvas.FillCircle(headX + eyeOffset, headY - headSize * 0.1f, 3f, eyeColor);

        // Nose
        uint noseColor = 0xFFFF69B4;
        canvas.FillCircle(headX, headY + headSize * 0.1f, 2f, noseColor);

        // Mouth
        uint mouthColor = 0xFF000000;
        canvas.DrawLine(headX - 3, headY + headSize * 0.15f, headX + 3, headY + headSize * 0.15f, mouthColor, 1f);

        // Whiskers
        canvas.DrawLine(headX - headSize * 0.4f, headY - headSize * 0.05f, headX - headSize * 0.2f, headY - headSize * 0.1f, mouthColor, 1f);
        canvas.DrawLine(headX - headSize * 0.4f, headY + headSize * 0.05f, headX - headSize * 0.2f, headY + headSize * 0.1f, mouthColor, 1f);
        canvas.DrawLine(headX + headSize * 0.4f, headY - headSize * 0.05f, headX + headSize * 0.2f, headY - headSize * 0.1f, mouthColor, 1f);
        canvas.DrawLine(headX + headSize * 0.4f, headY + headSize * 0.05f, headX + headSize * 0.2f, headY + headSize * 0.1f, mouthColor, 1f);
    }

    private void RenderTrailConnection(ISkiaCanvas canvas)
    {
        // Render the connection between cat and trail
        if (_trailSegments.Count > 0)
        {
            var firstSegment = _trailSegments[_trailSegments.Count - 1];
            uint connectionColor = GetRainbowColor(_trailSegments.Count - 1);

            canvas.DrawLine(_catX - CAT_WIDTH * 0.4f, _catY, firstSegment.X, firstSegment.Y, connectionColor, 3f);
        }
    }

    private void RenderUI(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Render audio indicators at the bottom
        float barY = _height - 25;
        float barWidth = _width - 40;
        float barHeight = 6;

        // Background
        canvas.FillRect(20, barY, barWidth, barHeight, 0xFF404040);

        // Bass indicator (red)
        float bassWidth = barWidth * _bassAccumulator * 0.3f;
        canvas.FillRect(20, barY, bassWidth, barHeight, 0xFFFF4444);

        // Mid indicator (green)
        float midWidth = barWidth * _midAccumulator * 0.4f;
        canvas.FillRect(20 + bassWidth, barY, midWidth, barHeight, 0xFF44FF44);

        // Treble indicator (blue)
        float trebleWidth = barWidth * _trebleAccumulator * 0.3f;
        canvas.FillRect(20 + bassWidth + midWidth, barY, trebleWidth, barHeight, 0xFF4444FF);

        // Cat status
        string status = $"{_movementMode} - Speed: {_catSpeed:F1}x - Stars: {_stars.Count}";
        canvas.DrawText(status, 20, _height - 5, 0xFFFFFFFF, 12);
    }

    private uint GetRainbowColor(int segmentIndex)
    {
        int colorIndex = segmentIndex % _rainbowColors.Length;
        return _rainbowColors[colorIndex];
    }

    // Data classes
    private class RainbowSegment
    {
        public float X, Y;
        public float Width;
        public uint Color;
        public float Alpha;
        public float Life;

        public RainbowSegment(float x, float y, float width, uint color, float alpha)
        {
            X = x;
            Y = y;
            Width = width;
            Color = color;
            Alpha = alpha;
            Life = 1.0f;
        }
    }

    private class Star
    {
        public float X, Y;
        public float Size;
        public float TwinklePhase;
        public float Brightness;

        public Star(float x, float y, float size, float twinklePhase)
        {
            X = x;
            Y = y;
            Size = size;
            TwinklePhase = twinklePhase;
            Brightness = 1.0f;
        }
    }

    private class Sparkle
    {
        public float X, Y;
        public float Size;
        public float Life;

        public Sparkle(float x, float y, float size, float life)
        {
            X = x;
            Y = y;
            Size = size;
            Life = life;
        }
    }
}
