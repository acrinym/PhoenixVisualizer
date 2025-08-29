using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Pyramid Crumble Visualizer - 3D pyramid that crumbles to bass hits with physics-based falling blocks
/// Inspired by desert temples and ancient architecture, with audio-reactive destruction and regeneration
/// </summary>
public sealed class PyramidCrumbleVisualizer : IVisualizerPlugin
{
    public string Id => "pyramid_crumble";
    public string DisplayName => "🏜️ Pyramid Crumble";

    private int _width, _height;
    private float _time;
    private readonly Random _random = new();

    // Pyramid system constants
    private const int MAX_BLOCKS = 1000;
    private const float GRAVITY = 0.3f;
    private const float BLOCK_SIZE = 8f;
    private const int PYRAMID_BASE_SIZE = 12;
    private const float REGENERATION_INTERVAL = 16f; // beats

    // Pyramid state
    private PyramidStructure _pyramid;
    private readonly List<FallingBlock> _fallingBlocks;
    private readonly List<GroundBlock> _groundBlocks;
    private float _regenerationTimer;
    private float _cameraShake;
    private float _sunAngle;

    // Lighting system
    private Vector3 _sunPosition;
    private Vector3 _sunColor;
    private float _sunIntensity;

    // Audio state
    private float _bassAccumulator;
    private float _lastBeatTime;
    private int _beatCount;

    // Pyramid styles
    public enum PyramidStyle
    {
        Classic,
        Ziggurat,
        Stepped,
        Inverted,
        Randomized
    }

    private PyramidStyle _currentStyle;

    public PyramidCrumbleVisualizer()
    {
        _fallingBlocks = new List<FallingBlock>();
        _groundBlocks = new List<GroundBlock>();
        _pyramid = new PyramidStructure();
        _currentStyle = PyramidStyle.Classic;

        // Initialize Vector3 fields
        _sunPosition = new Vector3(-200, -100, 300);
        _sunColor = new Vector3(1.0f, 0.9f, 0.7f);
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        _regenerationTimer = 0;
        _beatCount = 0;
        _cameraShake = 0;
        _sunAngle = 0;

        // Initialize lighting
        _sunPosition = new Vector3(-200, -100, 300);
        _sunColor = new Vector3(1.0f, 0.9f, 0.7f); // Warm sunlight
        _sunIntensity = 1.0f;

        // Create initial pyramid
        GeneratePyramid();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        _fallingBlocks.Clear();
        _groundBlocks.Clear();
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Ensure we always have a pyramid
        if (_pyramid.Blocks.Count == 0 && _fallingBlocks.Count == 0 && _groundBlocks.Count == 0)
        {
            GeneratePyramid();
        }

        // Update audio-reactive systems
        UpdateAudioReactivity(f);

        // Update physics
        UpdatePhysics(f);

        // Update regeneration cycle
        UpdateRegeneration(f);

        // Render scene
        RenderScene(canvas, f);

        // Render UI elements
        RenderUI(canvas, f);
    }

    private void UpdateAudioReactivity(AudioFeatures f)
    {
        // Bass accumulation for crumble trigger
        _bassAccumulator = _bassAccumulator * 0.9f + f.Bass * 0.1f;

        // Beat detection
        if (f.Beat && _time - _lastBeatTime > 0.15f) // More responsive beat detection
        {
            _lastBeatTime = _time;
            _beatCount++;

            // Trigger crumble on strong bass or regular beats
            if (_bassAccumulator > 0.4f || _beatCount % 4 == 0) // Trigger every 4th beat or on strong bass
            {
                TriggerCrumble(f.Bass);
            }
        }

        // Camera shake from midrange
        _cameraShake = f.Mid * 0.5f;

        // Sun flicker from treble
        _sunIntensity = 0.8f + f.Treble * 0.4f;
        _sunAngle += f.Treble * 0.01f;
        _sunPosition = new Vector3(
            -200 + (float)Math.Sin(_sunAngle) * 50,
            -100,
            300 + (float)Math.Cos(_sunAngle) * 30
        );
    }

    private void UpdatePhysics(AudioFeatures f)
    {
        // Update falling blocks
        for (int i = _fallingBlocks.Count - 1; i >= 0; i--)
        {
            var block = _fallingBlocks[i];

            // Apply gravity
            block.VelocityY += GRAVITY;

            // Update position
            block.X += block.VelocityX;
            block.Y += block.VelocityY;
            block.Z += block.VelocityZ;

            // Rotation
            block.RotationX += block.RotationSpeedX;
            block.RotationY += block.RotationSpeedY;
            block.RotationZ += block.RotationSpeedZ;

            // Check ground collision
            if (block.Y >= GetGroundLevel())
            {
                // Convert to ground block
                _groundBlocks.Add(new GroundBlock(block.X, block.Y, block.Z, block.Color));
                _fallingBlocks.RemoveAt(i);

                // Limit ground blocks
                if (_groundBlocks.Count > MAX_BLOCKS / 2)
                {
                    _groundBlocks.RemoveAt(0);
                }
            }
        }

        // Update ground blocks (slight settling)
        foreach (var block in _groundBlocks)
        {
            if (block.Y > GetGroundLevel() - 2)
            {
                block.Y -= 0.1f;
            }
        }
    }

    private void UpdateRegeneration(AudioFeatures f)
    {
        _regenerationTimer += f.Volume * 0.1f;

        // Check for regeneration trigger
        if (_regenerationTimer >= REGENERATION_INTERVAL || GetRemainingBlocks() < 10)
        {
            // Always attempt regeneration when conditions are met
            RegeneratePyramid();
            _regenerationTimer = 0;
        }
    }

    private void TriggerCrumble(float intensity)
    {
        if (_pyramid.Blocks.Count == 0) return;

        // Determine how many blocks to crumble
        int blocksToCrumble = Math.Max(1, (int)(intensity * 5));

        for (int i = 0; i < blocksToCrumble && _pyramid.Blocks.Count > 0; i++)
        {
            // Pick a random block from the pyramid
            int blockIndex = _random.Next(_pyramid.Blocks.Count);
            var block = _pyramid.Blocks[blockIndex];

            // Create falling block
            var fallingBlock = new FallingBlock(
                block.X, block.Y, block.Z,
                block.Color
            );

            // Add random velocity
            fallingBlock.VelocityX = (float)(_random.NextDouble() - 0.5) * 8f;
            fallingBlock.VelocityY = (float)(_random.NextDouble() - 0.5) * 3f;
            fallingBlock.VelocityZ = (float)(_random.NextDouble() - 0.5) * 6f;

            // Add rotation
            fallingBlock.RotationSpeedX = (float)(_random.NextDouble() - 0.5) * 0.2f;
            fallingBlock.RotationSpeedY = (float)(_random.NextDouble() - 0.5) * 0.2f;
            fallingBlock.RotationSpeedZ = (float)(_random.NextDouble() - 0.5) * 0.2f;

            _fallingBlocks.Add(fallingBlock);
            _pyramid.Blocks.RemoveAt(blockIndex);
        }
    }

    private void GeneratePyramid()
    {
        _pyramid.Blocks.Clear();

        // Choose pyramid style
        if (_currentStyle == PyramidStyle.Randomized)
        {
            _currentStyle = (PyramidStyle)_random.Next(Enum.GetValues(typeof(PyramidStyle)).Length - 1);
        }

        float centerX = 0;
        float baseY = 200;
        float centerZ = 0;

        switch (_currentStyle)
        {
            case PyramidStyle.Classic:
                GenerateClassicPyramid(centerX, baseY, centerZ);
                break;
            case PyramidStyle.Ziggurat:
                GenerateZiggurat(centerX, baseY, centerZ);
                break;
            case PyramidStyle.Stepped:
                GenerateSteppedPyramid(centerX, baseY, centerZ);
                break;
            case PyramidStyle.Inverted:
                GenerateInvertedPyramid(centerX, baseY, centerZ);
                break;
        }
    }

    private void GenerateClassicPyramid(float centerX, float baseY, float centerZ)
    {
        for (int level = 0; level < PYRAMID_BASE_SIZE; level++)
        {
            int levelSize = PYRAMID_BASE_SIZE - level;
            float levelY = baseY - level * BLOCK_SIZE;
            float levelOffset = level * BLOCK_SIZE * 0.5f;

            for (int x = 0; x < levelSize; x++)
            {
                for (int z = 0; z < levelSize; z++)
                {
                    float blockX = centerX + (x - levelSize * 0.5f) * BLOCK_SIZE;
                    float blockZ = centerZ + (z - levelSize * 0.5f) * BLOCK_SIZE;

                    uint color = GetPyramidBlockColor(level, PYRAMID_BASE_SIZE);
                    _pyramid.Blocks.Add(new PyramidBlock(blockX, levelY, blockZ, color));
                }
            }
        }
    }

    private void GenerateZiggurat(float centerX, float baseY, float centerZ)
    {
        int tiers = 4;
        for (int tier = 0; tier < tiers; tier++)
        {
            int tierSize = PYRAMID_BASE_SIZE - tier * 2;
            if (tierSize <= 0) break;

            float tierY = baseY - tier * BLOCK_SIZE * 2;

            for (int x = 0; x < tierSize; x++)
            {
                for (int z = 0; z < tierSize; z++)
                {
                    // Only create perimeter blocks for ziggurat style
                    if (x == 0 || x == tierSize - 1 || z == 0 || z == tierSize - 1)
                    {
                        float blockX = centerX + (x - tierSize * 0.5f) * BLOCK_SIZE;
                        float blockZ = centerZ + (z - tierSize * 0.5f) * BLOCK_SIZE;

                        uint color = GetPyramidBlockColor(tier, tiers);
                        _pyramid.Blocks.Add(new PyramidBlock(blockX, tierY, blockZ, color));
                    }
                }
            }
        }
    }

    private void GenerateSteppedPyramid(float centerX, float baseY, float centerZ)
    {
        for (int step = 0; step < PYRAMID_BASE_SIZE / 2; step++)
        {
            int stepSize = PYRAMID_BASE_SIZE - step * 2;
            float stepY = baseY - step * BLOCK_SIZE * 1.5f;

            // Create step platform
            for (int x = 0; x < stepSize; x++)
            {
                for (int z = 0; z < stepSize; z++)
                {
                    if (x == 0 || x == stepSize - 1 || z == 0 || z == stepSize - 1)
                    {
                        float blockX = centerX + (x - stepSize * 0.5f) * BLOCK_SIZE;
                        float blockZ = centerZ + (z - stepSize * 0.5f) * BLOCK_SIZE;

                        uint color = GetPyramidBlockColor(step, PYRAMID_BASE_SIZE / 2);
                        _pyramid.Blocks.Add(new PyramidBlock(blockX, stepY, blockZ, color));
                    }
                }
            }
        }
    }

    private void GenerateInvertedPyramid(float centerX, float baseY, float centerZ)
    {
        for (int level = PYRAMID_BASE_SIZE - 1; level >= 0; level--)
        {
            int levelSize = PYRAMID_BASE_SIZE - level;
            float levelY = baseY - (PYRAMID_BASE_SIZE - 1 - level) * BLOCK_SIZE;

            for (int x = 0; x < levelSize; x++)
            {
                for (int z = 0; z < levelSize; z++)
                {
                    float blockX = centerX + (x - levelSize * 0.5f) * BLOCK_SIZE;
                    float blockZ = centerZ + (z - levelSize * 0.5f) * BLOCK_SIZE;

                    uint color = GetPyramidBlockColor(level, PYRAMID_BASE_SIZE);
                    _pyramid.Blocks.Add(new PyramidBlock(blockX, levelY, blockZ, color));
                }
            }
        }
    }

    private void RegeneratePyramid()
    {
        // Move ground blocks to rising animation with a delay
        foreach (var groundBlock in _groundBlocks)
        {
            var risingBlock = new FallingBlock(
                groundBlock.X, groundBlock.Y, groundBlock.Z, groundBlock.Color
            );
            risingBlock.VelocityY = -3f; // Rise up faster
            risingBlock.VelocityX = (float)(_random.NextDouble() - 0.5) * 2f; // Add some horizontal movement
            risingBlock.VelocityZ = (float)(_random.NextDouble() - 0.5) * 2f;
            _fallingBlocks.Add(risingBlock);
        }

        _groundBlocks.Clear();

        // Generate a new pyramid immediately
        GeneratePyramid();

        // Change pyramid style occasionally
        if (_random.NextDouble() < 0.3) // 30% chance to change style
        {
            _currentStyle = (PyramidStyle)_random.Next(Enum.GetValues(typeof(PyramidStyle)).Length);
        }
    }

    private uint GetPyramidBlockColor(int level, int maxLevels)
    {
        float levelRatio = (float)level / maxLevels;

        // Golden sandstone gradient
        float r = 0.8f + levelRatio * 0.2f;  // 204-255
        float g = 0.6f + levelRatio * 0.3f;  // 153-230
        float b = 0.2f + levelRatio * 0.2f;  // 51-102

        // Add some variation
        float variation = (float)(_random.NextDouble() - 0.5) * 0.1f;
        r = Math.Clamp(r + variation, 0, 1);
        g = Math.Clamp(g + variation, 0, 1);
        b = Math.Clamp(b + variation, 0, 1);

        byte red = (byte)(r * 255);
        byte green = (byte)(g * 255);
        byte blue = (byte)(b * 255);

        return 0xFF000000 | ((uint)red << 16) | ((uint)green << 8) | blue;
    }

    private float GetGroundLevel()
    {
        return _height - 50;
    }

    private int GetRemainingBlocks()
    {
        return _pyramid.Blocks.Count;
    }

    private void RenderScene(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Create desert background
        uint bgColor = CalculateDesertBackground();
        canvas.Clear(bgColor);

        // Apply camera shake
        float shakeX = (float)(Math.Sin(_time * 50) * _cameraShake * 5);
        float shakeY = (float)(Math.Cos(_time * 45) * _cameraShake * 3);

        // Render ground plane
        RenderGround(canvas, shakeX, shakeY);

        // Render pyramid blocks
        foreach (var block in _pyramid.Blocks)
        {
            RenderBlock3D(canvas, block, shakeX, shakeY, f);
        }

        // Render falling blocks
        foreach (var block in _fallingBlocks)
        {
            RenderFallingBlock3D(canvas, block, shakeX, shakeY);
        }

        // Render ground blocks
        foreach (var block in _groundBlocks)
        {
            RenderGroundBlock3D(canvas, block, shakeX, shakeY);
        }

        // Render sun and lighting effects
        RenderSun(canvas, f);

        // Render godrays if available
        RenderGodrays(canvas, f);
    }

    private void RenderBlock3D(ISkiaCanvas canvas, PyramidBlock block, float shakeX, float shakeY, AudioFeatures f)
    {
        // Simple 3D to 2D projection (isometric view)
        float screenX = _width * 0.5f + block.X + shakeX;
        float screenY = _height * 0.5f + block.Y + block.Z * 0.5f + shakeY;

        // Apply depth scaling
        float scale = 1.0f - block.Z * 0.001f;

        // Calculate lighting
        float lighting = CalculateLighting(block);
        uint litColor = AdjustBrightness(block.Color, lighting);

        // Render as cube (simplified as square with depth)
        float size = BLOCK_SIZE * scale;
        canvas.FillRect(screenX - size * 0.5f, screenY - size * 0.5f, size, size, litColor);

        // Add edges for 3D effect
        uint edgeColor = AdjustBrightness(litColor, 0.7f);
        canvas.DrawRect(screenX - size * 0.5f, screenY - size * 0.5f, size, size, edgeColor, false);

        // Add highlight for sun-facing surfaces
        if (lighting > 0.8f)
        {
            uint highlightColor = AdjustBrightness(litColor, 1.3f);
            canvas.FillRect(screenX - size * 0.3f, screenY - size * 0.3f, size * 0.6f, size * 0.6f, highlightColor);
        }
    }

    private void RenderFallingBlock3D(ISkiaCanvas canvas, FallingBlock block, float shakeX, float shakeY)
    {
        // 3D projection with rotation
        float screenX = _width * 0.5f + block.X + shakeX;
        float screenY = _height * 0.5f + block.Y + block.Z * 0.5f + shakeY;

        // Apply depth scaling and fade
        float scale = 1.0f - block.Z * 0.001f;
        float alpha = MathF.Min(1f, (GetGroundLevel() - block.Y) / 100f);
        uint fadedColor = (block.Color & 0x00FFFFFF) | ((uint)(alpha * 255) << 24);

        // Simple rotation effect (just scale variation for now)
        float rotationScale = 1f + (float)Math.Sin(block.RotationY) * 0.2f;
        float size = BLOCK_SIZE * scale * rotationScale;

        canvas.FillRect(screenX - size * 0.5f, screenY - size * 0.5f, size, size, fadedColor);
    }

    private void RenderGroundBlock3D(ISkiaCanvas canvas, GroundBlock block, float shakeX, float shakeY)
    {
        float screenX = _width * 0.5f + block.X + shakeX;
        float screenY = _height * 0.5f + block.Y + block.Z * 0.5f + shakeY;

        float scale = 0.8f; // Slightly smaller on ground
        float size = BLOCK_SIZE * scale;

        uint groundColor = AdjustBrightness(block.Color, 0.6f); // Darker on ground
        canvas.FillRect(screenX - size * 0.5f, screenY - size * 0.5f, size, size, groundColor);
    }

    private void RenderGround(ISkiaCanvas canvas, float shakeX, float shakeY)
    {
        // Render desert ground
        uint groundColor = 0xFF8B7355; // Sandy brown
        float groundY = GetGroundLevel() + shakeY;

        canvas.FillRect(0, groundY, _width, _height - groundY, groundColor);

        // Add some texture lines
        uint lineColor = 0xFF6B5B47;
        for (int i = 0; i < 20; i++)
        {
            float y = groundY + i * 3;
            canvas.DrawLine(0, y, _width, y, lineColor, 1f);
        }
    }

    private void RenderSun(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Render sun as bright circle
        float sunX = _width * 0.2f + _sunPosition.X * 0.1f;
        float sunY = _height * 0.1f + _sunPosition.Y * 0.1f;

        uint sunRenderColor = AdjustBrightness(0xFFFFFF00, _sunIntensity);
        canvas.FillCircle(sunX, sunY, 30, sunRenderColor);

        // Add sun glow
        uint glowColor = (sunRenderColor & 0x00FFFFFF) | 0x40u << 24;
        canvas.FillCircle(sunX, sunY, 60, glowColor);
    }

    private void RenderGodrays(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Simple godray effect from sun
        float sunX = _width * 0.2f;
        float sunY = _height * 0.1f;

        uint rayColor = 0x20FFFFFF; // Semi-transparent white

        for (int i = 0; i < 12; i++)
        {
            float angle = (i / 12f) * MathF.PI * 2;
            float endX = sunX + MathF.Cos(angle) * _width;
            float endY = sunY + MathF.Sin(angle) * _height;

            canvas.DrawLine(sunX, sunY, endX, endY, rayColor, 2f);
        }
    }

    private void RenderUI(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Render status indicators
        RenderStatusBar(canvas, f);

        // Render pyramid info
        RenderPyramidInfo(canvas);
    }

    private void RenderStatusBar(ISkiaCanvas canvas, AudioFeatures f)
    {
        float barY = 20;
        float barWidth = _width - 40;
        float barHeight = 8;

        // Background
        canvas.FillRect(20, barY, barWidth, barHeight, 0xFF404040);

        // Bass crumble indicator
        float bassWidth = barWidth * _bassAccumulator;
        canvas.FillRect(20, barY, bassWidth, barHeight, 0xFFFF4444);

        // Mid vibration indicator
        float midWidth = barWidth * f.Mid;
        canvas.FillRect(20, barY + barHeight + 2, midWidth, barHeight, 0xFF44FF44);

        // Treble sun indicator
        float trebleWidth = barWidth * f.Treble;
        canvas.FillRect(20, barY + (barHeight + 2) * 2, trebleWidth, barHeight, 0xFF4444FF);
    }

    private void RenderPyramidInfo(ISkiaCanvas canvas)
    {
        // Display remaining blocks and style
        string info = $"{_currentStyle} Pyramid - {_pyramid.Blocks.Count} blocks";
        canvas.DrawText(info, 20, _height - 20, 0xFFFFFFFF, 14);
    }

    private float CalculateLighting(PyramidBlock block)
    {
        // Simple directional lighting from sun
        Vector3 blockPos = new Vector3(block.X, block.Y, block.Z);
        Vector3 lightDir = Vector3.Normalize(_sunPosition - blockPos);

        // Simple dot product for diffuse lighting
        Vector3 normal = new Vector3(0, -1, 0); // Upward normal
        float diffuse = Math.Max(0, Vector3.Dot(normal, lightDir));

        return 0.3f + diffuse * 0.7f; // Ambient + diffuse
    }

    private uint CalculateDesertBackground()
    {
        // Dynamic desert sky
        float timeOfDay = (_time * 0.1f) % 1f;

        if (timeOfDay < 0.25f) // Dawn
            return 0xFFFF7F50;
        else if (timeOfDay < 0.5f) // Morning
            return 0xFF87CEEB;
        else if (timeOfDay < 0.75f) // Afternoon
            return 0xFF4682B4;
        else // Evening
            return 0xFFFF4500;
    }

    private uint AdjustBrightness(uint color, float factor)
    {
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);

        r = (byte)Math.Clamp(r * factor, 0, 255);
        g = (byte)Math.Clamp(g * factor, 0, 255);
        b = (byte)Math.Clamp(b * factor, 0, 255);

        return 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
    }

    // 3D Math helper classes
    private class Vector3
    {
        public float X, Y, Z;

        public Vector3(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vector3 Normalize(Vector3 v)
        {
            float length = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            return new Vector3(v.X / length, v.Y / length, v.Z / length);
        }
    }

    private class PyramidStructure
    {
        public List<PyramidBlock> Blocks { get; } = new();
    }

    private class PyramidBlock
    {
        public float X, Y, Z;
        public uint Color;

        public PyramidBlock(float x, float y, float z, uint color)
        {
            X = x; Y = y; Z = z; Color = color;
        }
    }

    private class FallingBlock
    {
        public float X, Y, Z;
        public float VelocityX, VelocityY, VelocityZ;
        public float RotationX, RotationY, RotationZ;
        public float RotationSpeedX, RotationSpeedY, RotationSpeedZ;
        public uint Color;

        public FallingBlock(float x, float y, float z, uint color)
        {
            X = x; Y = y; Z = z; Color = color;
            VelocityX = VelocityY = VelocityZ = 0;
            RotationX = RotationY = RotationZ = 0;
            RotationSpeedX = RotationSpeedY = RotationSpeedZ = 0;
        }
    }

    private class GroundBlock
    {
        public float X, Y, Z;
        public uint Color;

        public GroundBlock(float x, float y, float z, uint color)
        {
            X = x; Y = y; Z = z; Color = color;
        }
    }
}
