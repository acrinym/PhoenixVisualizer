using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Classic Windows 2000 3D Pipes screensaver - faithfully recreated for Phoenix Visualizer
/// Features branching pipes that grow and turn in 3D space with audio reactivity
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
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Clear with dark background
        canvas.Clear(0xFF0A0A0A);

        // Update and grow existing pipes
        UpdatePipes(f);

        // Render all pipes with 3D perspective
        RenderPipes3D(canvas, f);

        // Occasionally start new pipes
        if (_pipes.Count < MAX_PIPES && _random.NextDouble() < 0.02f * (1f + f.Volume))
        {
            StartNewPipe();
        }
    }

    private void StartNewPipe()
    {
        var newPipe = new List<PipeSegment>();
        var headPos = (
            (float)(_random.NextDouble() * 20 - 10), // -10 to 10 range
            (float)(_random.NextDouble() * 20 - 10),
            (float)(_random.NextDouble() * 20 - 10)
        );

        // Create initial segment
        var firstSegment = new PipeSegment
        {
            X = headPos.Item1,
            Y = headPos.Item2,
            Z = headPos.Item3,
            Direction = _random.Next(NUM_DIRS),
            Color = _pipeColors[_random.Next(_pipeColors.Length)],
            IsBranch = false,
            AudioInfluence = 0f
        };

        newPipe.Add(firstSegment);
        _pipes.Add(newPipe);
        _pipeHeads.Add(headPos);
    }

    private void UpdatePipes(AudioFeatures f)
    {
        for (int pipeIndex = _pipes.Count - 1; pipeIndex >= 0; pipeIndex--)
        {
            var pipe = _pipes[pipeIndex];
            if (pipe.Count >= MAX_PIPE_LENGTH)
            {
                // Remove old pipes occasionally
                if (_random.NextDouble() < 0.001f)
                {
                    _pipes.RemoveAt(pipeIndex);
                    _pipeHeads.RemoveAt(pipeIndex);
                }
                continue;
            }

            // Grow the pipe
            var lastSegment = pipe[pipe.Count - 1];
            var newSegment = GrowPipe(lastSegment, f);

            if (newSegment.HasValue)
            {
                pipe.Add(newSegment.Value);
                _pipeHeads[pipeIndex] = (newSegment.Value.X, newSegment.Value.Y, newSegment.Value.Z);

                // Occasionally branch (based on audio)
                if (pipe.Count > 10 && _random.NextDouble() < 0.01f * (1f + (f.Beat ? 0.5f : 0f)))
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

    private void RenderPipes3D(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // Simple 3D perspective projection
        float fov = 60f * (float)(Math.PI / 180f);
        float aspect = (float)_width / _height;
        float near = 1f;
        float far = 100f;

        foreach (var pipe in _pipes)
        {
            if (pipe.Count < 2) continue;

            // Render pipe as connected segments
            for (int i = 0; i < pipe.Count - 1; i++)
            {
                var segment1 = pipe[i];
                var segment2 = pipe[i + 1];

                // Project 3D points to 2D screen coordinates
                var screen1 = Project3D(segment1, centerX, centerY, fov, aspect, near, far);
                var screen2 = Project3D(segment2, centerX, centerY, fov, aspect, near, far);

                if (screen1.z > 0 && screen2.z > 0 && screen1.z < far && screen2.z < far)
                {
                    // Calculate thickness based on distance and audio
                    float thickness = PIPE_RADIUS * (far / screen1.z) * (1f + segment1.AudioInfluence);
                    thickness = Math.Max(1f, Math.Min(8f, thickness));

                    // Draw pipe segment
                    canvas.DrawLine(screen1.x, screen1.y, screen2.x, screen2.y,
                                 segment1.Color, thickness);
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
}
