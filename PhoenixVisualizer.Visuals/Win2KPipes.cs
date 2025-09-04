using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core.Effects.Interfaces;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Classic Windows 2000 3D Pipes screensaver - faithfully recreated for Phoenix Visualizer
/// Features branching pipes that grow and turn in 3D space with audio reactivity
/// FIXED: Optimized drawing performance with LOD and enhanced audio reactivity
/// </summary>
public sealed class Win2KPipes : IVisualizerPlugin
{
    public string Id => "win2k_pipes";
    public string DisplayName => "🏗️ Win2K 3D Pipes";

    private int _width, _height;
    private float _time;
    private Random _random = new();

    // Pipe system constants (based on original Win2K implementation)
    private const int MAX_PIPES = 50;
    private const int MAX_PIPE_LENGTH = 100;
    private const float PIPE_RADIUS = 0.3f;
    private const float DIV_SIZE = 2.0f; // Distance between pipe segments

    // Direction constants (from original sspipes.h)
    private const int PLUS_X = 0;
    private const int MINUS_X = 1;
    private const int PLUS_Y = 2;
    private const int MINUS_Y = 3;
    private const int PLUS_Z = 4;
    private const int MINUS_Z = 5;
    private const int NUM_DIRS = 6;

    // Pipe segment structure
    private struct PipeSegment
    {
        public float X, Y, Z;
        public int Direction;
        public uint Color;
        public bool IsBranch;
        public float AudioInfluence;
    }

    // Active pipes
    private List<List<PipeSegment>> _pipes = new();
    private List<(float x, float y, float z)> _pipeHeads = new();
    
    // FIXED: Integrated effects nodes for enhanced visual effects
    private List<IEffectNode> _effects = new();
    private bool _enableGlowEffect = true;
    private bool _enableParticleTrails = true;
    private bool _enableColorShifting = true;
    private float _glowIntensity = 0.5f;
    private float _particleDensity = 0.3f;
    private float _colorShiftSpeed = 1.0f;

    // Colors inspired by the original
    private readonly uint[] _pipeColors = new uint[]
    {
        0xFFFF0000, // Red
        0xFF00FF00, // Green
        0xFF0000FF, // Blue
        0xFFFFFF00, // Yellow
        0xFFFF00FF, // Magenta
        0xFF00FFFF, // Cyan
        0xFFFF8000, // Orange
        0xFF8000FF  // Purple
    };

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;

        // Initialize with a few starter pipes
        for (int i = 0; i < 3; i++)
        {
            StartNewPipe();
        }
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        _pipes.Clear();
        _pipeHeads.Clear();
        _effects.Clear();
    }
    
    // FIXED: Effects node management methods
    public void AddEffect(IEffectNode effect)
    {
        _effects.Add(effect);
    }
    
    public void RemoveEffect(IEffectNode effect)
    {
        _effects.Remove(effect);
    }
    
    public void SetGlowEffect(bool enabled, float intensity = 0.5f)
    {
        _enableGlowEffect = enabled;
        _glowIntensity = intensity;
    }
    
    public void SetParticleTrails(bool enabled, float density = 0.3f)
    {
        _enableParticleTrails = enabled;
        _particleDensity = density;
    }
    
    public void SetColorShifting(bool enabled, float speed = 1.0f)
    {
        _enableColorShifting = enabled;
        _colorShiftSpeed = speed;
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        // FIXED: Audio-reactive time and performance optimizations
        var energy = f.Energy;
        var bass = f.Bass;
        var mid = f.Mid;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // Audio-reactive animation speed
        var baseSpeed = 0.016f;
        var energySpeed = energy * 0.02f;
        var trebleSpeed = treble * 0.015f;
        var beatSpeed = beat ? 0.03f : 0f;
        _time += baseSpeed + energySpeed + trebleSpeed + beatSpeed;

        // FIXED: Audio-reactive background color
        var baseColor = 0xFF0A0A0A;
        if (beat)
            baseColor = 0xFF1A0A1A; // Purple tint on beat
        else if (bass > 0.5f)
            baseColor = 0xFF0A0A2A; // Blue tint for bass
        else if (treble > 0.4f)
            baseColor = 0xFF2A0A0A; // Red tint for treble
        else if (energy > 0.6f)
            baseColor = 0xFF2A0A2A; // Purple tint for energy
            
        canvas.Clear(baseColor);

        // FIXED: Performance optimization - limit pipe updates based on audio
        var updateFrequency = 1f + energy * 0.5f + (beat ? 0.5f : 0f);
        if (_time % (0.016f / updateFrequency) < 0.016f)
        {
            UpdatePipes(f);
        }

        // FIXED: Optimized rendering with LOD (Level of Detail)
        RenderPipes3DOptimized(canvas, f);

        // FIXED: Audio-reactive pipe creation
        var creationChance = 0.02f;
        if (beat) creationChance = 0.05f; // More pipes on beat
        if (energy > 0.6f) creationChance = 0.03f; // More pipes on high energy
        if (bass > 0.5f) creationChance = 0.025f; // More pipes on bass
        
        if (_pipes.Count < MAX_PIPES && _random.NextDouble() < creationChance * (1f + volume))
        {
            StartNewPipe(f);
        }
    }

    private void StartNewPipe(AudioFeatures f = null)
    {
        var newPipe = new List<PipeSegment>();
        // FIXED: Audio-reactive pipe positioning and properties
        var energy = f?.Energy ?? 0f;
        var bass = f?.Bass ?? 0f;
        var treble = f?.Treble ?? 0f;
        var beat = f?.Beat ?? false;
        
        // Audio-reactive positioning
        var baseX = (float)(_random.NextDouble() * 20 - 10);
        var baseY = (float)(_random.NextDouble() * 20 - 10);
        var baseZ = (float)(_random.NextDouble() * 20 - 10);
        
        // Bass affects X position, treble affects Y position, energy affects Z
        var audioX = baseX + bass * 5f * (_random.NextDouble() > 0.5f ? 1f : -1f);
        var audioY = baseY + treble * 5f * (_random.NextDouble() > 0.5f ? 1f : -1f);
        var audioZ = baseZ + energy * 3f;
        
        var headPos = (audioX, audioY, audioZ);

        // FIXED: Audio-reactive color selection
        uint color;
        if (beat)
            color = 0xFFFFFF00; // Bright yellow on beat
        else if (bass > 0.5f)
            color = 0xFFFF0000; // Red for bass
        else if (treble > 0.4f)
            color = 0xFF00FFFF; // Cyan for treble
        else if (energy > 0.6f)
            color = 0xFFFF00FF; // Magenta for energy
        else
            color = _pipeColors[_random.Next(_pipeColors.Length)];

        // Create initial segment
        var firstSegment = new PipeSegment
        {
            X = headPos.Item1,
            Y = headPos.Item2,
            Z = headPos.Item3,
            Direction = _random.Next(NUM_DIRS),
            Color = color,
            IsBranch = false,
            AudioInfluence = energy + bass * 0.3f + treble * 0.2f
        };

        newPipe.Add(firstSegment);
        _pipes.Add(newPipe);
        _pipeHeads.Add(headPos);
    }

    private void UpdatePipes(AudioFeatures f)
    {
        var energy = f.Energy;
        var bass = f.Bass;
        var mid = f.Mid;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // FIXED: Enhanced audio-reactive pipe management
        for (int pipeIndex = _pipes.Count - 1; pipeIndex >= 0; pipeIndex--)
        {
            var pipe = _pipes[pipeIndex];
            if (pipe.Count >= MAX_PIPE_LENGTH)
            {
                // FIXED: Audio-reactive pipe removal
                var removalChance = 0.001f;
                if (beat) removalChance = 0.005f; // Remove more pipes on beat
                if (energy > 0.7f) removalChance = 0.003f; // Remove more pipes on high energy
                
                if (_random.NextDouble() < removalChance)
                {
                    _pipes.RemoveAt(pipeIndex);
                    _pipeHeads.RemoveAt(pipeIndex);
                }
                continue;
            }

            // FIXED: Audio-reactive pipe growth
            var lastSegment = pipe[pipe.Count - 1];
            var newSegment = GrowPipe(lastSegment, f);

            if (newSegment.HasValue)
            {
                pipe.Add(newSegment.Value);
                _pipeHeads[pipeIndex] = (newSegment.Value.X, newSegment.Value.Y, newSegment.Value.Z);

                // FIXED: Enhanced audio-reactive branching
                var baseBranchChance = 0.01f;
                var bassBranchChance = bass * 0.02f;
                var energyBranchChance = energy * 0.015f;
                var beatBranchChance = beat ? 0.03f : 0f;
                var totalBranchChance = baseBranchChance + bassBranchChance + energyBranchChance + beatBranchChance;
                
                if (pipe.Count > 10 && _random.NextDouble() < totalBranchChance)
                {
                    var branchSegment = CreateBranch(lastSegment, f);
                    if (branchSegment.HasValue)
                    {
                        var newBranch = new List<PipeSegment> { lastSegment, branchSegment.Value };
                        _pipes.Add(newBranch);
                        _pipeHeads.Add((branchSegment.Value.X, branchSegment.Value.Y, branchSegment.Value.Z));
                    }
                }
            }
        }
    }

    private PipeSegment? GrowPipe(PipeSegment lastSegment, AudioFeatures f)
    {
        int newDirection = ChooseNewDirection(lastSegment.Direction, f);
        if (newDirection == -1) return null; // Stuck

        var (dx, dy, dz) = GetDirectionOffset(newDirection);
        float audioMultiplier = 1f + f.Volume * 0.5f;

        return new PipeSegment
        {
            X = lastSegment.X + dx * DIV_SIZE * audioMultiplier,
            Y = lastSegment.Y + dy * DIV_SIZE * audioMultiplier,
            Z = lastSegment.Z + dz * DIV_SIZE * audioMultiplier,
            Direction = newDirection,
            Color = lastSegment.Color,
            IsBranch = false,
            AudioInfluence = f.Volume
        };
    }

    private PipeSegment? CreateBranch(PipeSegment fromSegment, AudioFeatures f)
    {
        // Try different directions for branching
        var possibleDirections = new List<int>();
        for (int dir = 0; dir < NUM_DIRS; dir++)
        {
            if (dir != fromSegment.Direction)
            {
                possibleDirections.Add(dir);
            }
        }

        if (possibleDirections.Count == 0) return null;

        int branchDirection = possibleDirections[_random.Next(possibleDirections.Count)];
        var (dx, dy, dz) = GetDirectionOffset(branchDirection);

        return new PipeSegment
        {
            X = fromSegment.X + dx * DIV_SIZE,
            Y = fromSegment.Y + dy * DIV_SIZE,
            Z = fromSegment.Z + dz * DIV_SIZE,
            Direction = branchDirection,
            Color = _pipeColors[_random.Next(_pipeColors.Length)], // Different color for branch
            IsBranch = true,
            AudioInfluence = f.Volume
        };
    }

    private int ChooseNewDirection(int currentDirection, AudioFeatures f)
    {
        // Audio-reactive direction choosing (simplified from original)
        var probabilities = new float[NUM_DIRS];
        float straightWeight = 2f + f.Volume * 2f; // Favor going straight with more audio
        float turnWeight = 1f - f.Volume * 0.5f;   // Less turning with more audio

        for (int dir = 0; dir < NUM_DIRS; dir++)
        {
            if (dir == currentDirection)
            {
                probabilities[dir] = straightWeight;
            }
            else if (IsOppositeDirection(dir, currentDirection))
            {
                probabilities[dir] = 0.1f; // Rarely go backwards
            }
            else
            {
                probabilities[dir] = turnWeight;
            }
        }

        // Normalize probabilities
        float total = 0;
        for (int i = 0; i < NUM_DIRS; i++) total += probabilities[i];
        for (int i = 0; i < NUM_DIRS; i++) probabilities[i] /= total;

        // Choose direction based on probabilities
        float random = (float)_random.NextDouble();
        float cumulative = 0;

        for (int dir = 0; dir < NUM_DIRS; dir++)
        {
            cumulative += probabilities[dir];
            if (random <= cumulative)
            {
                return dir;
            }
        }

        return currentDirection; // Fallback
    }

    private bool IsOppositeDirection(int dir1, int dir2)
    {
        return (dir1 == PLUS_X && dir2 == MINUS_X) ||
               (dir1 == MINUS_X && dir2 == PLUS_X) ||
               (dir1 == PLUS_Y && dir2 == MINUS_Y) ||
               (dir1 == MINUS_Y && dir2 == PLUS_Y) ||
               (dir1 == PLUS_Z && dir2 == MINUS_Z) ||
               (dir1 == MINUS_Z && dir2 == PLUS_Z);
    }

    private (float dx, float dy, float dz) GetDirectionOffset(int direction)
    {
        return direction switch
        {
            PLUS_X => (1, 0, 0),
            MINUS_X => (-1, 0, 0),
            PLUS_Y => (0, 1, 0),
            MINUS_Y => (0, -1, 0),
            PLUS_Z => (0, 0, 1),
            MINUS_Z => (0, 0, -1),
            _ => (0, 0, 0)
        };
    }

    private void RenderPipes3DOptimized(ISkiaCanvas canvas, AudioFeatures f)
    {
        var energy = f.Energy;
        var bass = f.Bass;
        var treble = f.Treble;
        var beat = f.Beat;
        
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // FIXED: Optimized 3D perspective projection
        float fov = 60f * (float)(Math.PI / 180f);
        float aspect = (float)_width / _height;
        float near = 1f;
        float far = 100f;

        // FIXED: Level of Detail (LOD) optimization
        var lodFactor = 1f + energy * 0.5f + (beat ? 0.3f : 0f);
        var skipSegments = Math.Max(1, (int)(3f / lodFactor)); // Skip segments based on audio

        foreach (var pipe in _pipes)
        {
            if (pipe.Count < 2) continue;

            // FIXED: Optimized rendering with LOD
            for (int i = 0; i < pipe.Count - 1; i += skipSegments)
            {
                var segment1 = pipe[i];
                var segment2 = pipe[Math.Min(i + skipSegments, pipe.Count - 1)];

                // FIXED: Audio-reactive color modification
                uint color = segment1.Color;
                if (beat)
                    color = 0xFFFFFF00; // Bright yellow on beat
                else if (bass > 0.5f)
                    color = 0xFFFF0000; // Red for bass
                else if (treble > 0.4f)
                    color = 0xFF00FFFF; // Cyan for treble
                else if (energy > 0.6f)
                    color = 0xFFFF00FF; // Magenta for energy

                // Project 3D points to 2D screen coordinates
                var screen1 = Project3D(segment1, centerX, centerY, fov, aspect, near, far);
                var screen2 = Project3D(segment2, centerX, centerY, fov, aspect, near, far);

                if (screen1.z > 0 && screen2.z > 0 && screen1.z < far && screen2.z < far)
                {
                    // FIXED: Enhanced audio-reactive thickness calculation
                    var baseThickness = PIPE_RADIUS * (far / screen1.z);
                    var bassThickness = bass * 2f;
                    var energyThickness = energy * 1.5f;
                    var beatThickness = beat ? 3f : 0f;
                    var totalThickness = baseThickness * (1f + segment1.AudioInfluence + bassThickness + energyThickness + beatThickness);
                    
                    totalThickness = Math.Max(1f, Math.Min(12f, totalThickness));

                    // FIXED: Enhanced effects node integration
                    var finalColor = color;
                    
                    // Apply color shifting effect
                    if (_enableColorShifting)
                    {
                        var hueShift = (_time * _colorShiftSpeed + energy * 0.5f) % 1.0f;
                        finalColor = ShiftColorHue(color, hueShift);
                    }
                    
                    // Apply glow effect
                    if (_enableGlowEffect && (beat || energy > 0.6f))
                    {
                        var glowColor = (finalColor & 0x00FFFFFF) | (uint)(_glowIntensity * 255) << 24;
                        canvas.DrawLine(screen1.x, screen1.y, screen2.x, screen2.y, glowColor, totalThickness * 1.5f);
                    }

                    // Draw pipe segment
                    canvas.DrawLine(screen1.x, screen1.y, screen2.x, screen2.y, finalColor, totalThickness);
                    
                    // Apply particle trails effect
                    if (_enableParticleTrails && (beat || bass > 0.4f))
                    {
                        RenderParticleTrail(canvas, screen1, screen2, f);
                    }
                }
            }
        }
    }

    private (float x, float y, float z) Project3D(PipeSegment segment, float centerX, float centerY,
                                                 float fov, float aspect, float near, float far)
    {
        // Rotate based on time for dynamic viewing
        float rotX = _time * 0.1f;
        float rotY = _time * 0.15f;

        // Apply rotations (simplified rotation matrices)
        float cosX = (float)Math.Cos(rotX), sinX = (float)Math.Sin(rotX);
        float cosY = (float)Math.Cos(rotY), sinY = (float)Math.Sin(rotY);

        float x = segment.X;
        float y = segment.Y;
        float z = segment.Z + 30f; // Push back from camera

        // Rotate around Y axis
        float tempX = x * cosY - z * sinY;
        float tempZ = x * sinY + z * cosY;
        x = tempX;
        z = tempZ;

        // Rotate around X axis
        float tempY = y * cosX - z * sinX;
        z = y * sinX + z * cosX;
        y = tempY;

        // Perspective projection
        if (z <= near) z = near + 0.1f;

        float screenX = centerX + (x / z) * (centerX / (float)Math.Tan(fov * 0.5));
        float screenY = centerY + (y / z) * (centerY / (float)Math.Tan(fov * 0.5));

        return (screenX, screenY, z);
    }
    
    // FIXED: Effects node helper methods
    private uint ShiftColorHue(uint color, float hueShift)
    {
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);
        
        // Convert to HSV, shift hue, convert back to RGB
        var (h, s, v) = RgbToHsv(r, g, b);
        h = (h + hueShift) % 1.0f;
        var (newR, newG, newB) = HsvToRgb(h, s, v);
        
        return (uint)(0xFF000000 | ((uint)newR << 16) | ((uint)newG << 8) | (uint)newB);
    }
    
    private void RenderParticleTrail(ISkiaCanvas canvas, (float x, float y, float z) start, (float x, float y, float z) end, AudioFeatures f)
    {
        var particleCount = (int)(_particleDensity * 5 + f.Bass * 3);
        
        for (int i = 0; i < particleCount; i++)
        {
            var t = (float)i / particleCount;
            var x = start.x + (end.x - start.x) * t + (_random.NextSingle() - 0.5f) * 10f;
            var y = start.y + (end.y - start.y) * t + (_random.NextSingle() - 0.5f) * 10f;
            
            var alpha = (_random.NextSingle() * 0.5f + 0.3f) * _particleDensity;
            var particleColor = (uint)((uint)(alpha * 255) << 24 | 0x00FFFFFF);
            
            canvas.FillCircle(x, y, 1f, particleColor);
        }
    }
    
    private (float h, float s, float v) RgbToHsv(byte r, byte g, byte b)
    {
        float rf = r / 255f, gf = g / 255f, bf = b / 255f;
        float max = Math.Max(Math.Max(rf, gf), bf);
        float min = Math.Min(Math.Min(rf, gf), bf);
        float delta = max - min;
        
        float h = 0f;
        if (delta != 0)
        {
            if (max == rf) h = ((gf - bf) / delta) % 6f;
            else if (max == gf) h = (bf - rf) / delta + 2f;
            else h = (rf - gf) / delta + 4f;
            h /= 6f;
            if (h < 0) h += 1f;
        }
        
        float s = max == 0 ? 0 : delta / max;
        float v = max;
        
        return (h, s, v);
    }
    
    private (byte r, byte g, byte b) HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1f - Math.Abs((h * 6f) % 2f - 1f));
        float m = v - c;
        
        float rf = 0f, gf = 0f, bf = 0f;
        
        if (h < 1f/6f) { rf = c; gf = x; bf = 0f; }
        else if (h < 2f/6f) { rf = x; gf = c; bf = 0f; }
        else if (h < 3f/6f) { rf = 0f; gf = c; bf = x; }
        else if (h < 4f/6f) { rf = 0f; gf = x; bf = c; }
        else if (h < 5f/6f) { rf = x; gf = 0f; bf = c; }
        else { rf = c; gf = 0f; bf = x; }
        
        return ((byte)((rf + m) * 255), (byte)((gf + m) * 255), (byte)((bf + m) * 255));
    }
}
