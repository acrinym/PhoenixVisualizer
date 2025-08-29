using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Classic Windows 2000 3D Maze screensaver - faithfully recreated for Phoenix Visualizer
/// Features maze generation and navigation with audio-reactive camera movement
/// </summary>
public sealed class Win2KMaze : IVisualizerPlugin
{
    public string Id => "win2k_maze";
    public string DisplayName => "🏛️ Win2K 3D Maze";

    private int _width, _height;
    private float _time;
    private Random _random = new();

    // Maze constants (based on original Win2K implementation)
    private const int MAZE_GRID = 16; // 16x16 maze
    private const int MAZE_WALL_HEIGHT = 3;
    private const float MAZE_CELL_SIZE = 4.0f;
    private const float CAMERA_HEIGHT = 2.0f;

    // Maze cell structure
    private enum WallFlags
    {
        WALL_NORTH = 0x01,
        WALL_SOUTH = 0x02,
        WALL_EAST = 0x04,
        WALL_WEST = 0x08,
        WALL_TOP = 0x10,
        WALL_BOTTOM = 0x20
    }

    private struct MazeCell
    {
        public WallFlags Walls;
        public bool Visited;
    }

    // Maze data
    private MazeCell[,] _maze;
    private float _cameraX, _cameraZ;
    private float _cameraRot;
    private float _viewAngle;

    public Win2KMaze()
    {
        _maze = new MazeCell[MAZE_GRID, MAZE_GRID];
    }

    // Colors inspired by the original maze
    private readonly uint[] _wallColors = new uint[]
    {
        0xFF4A90E2, // Blue
        0xFFE94B3C, // Red
        0xFF50C878, // Green
        0xFFFFD700, // Gold
        0xFF9B59B6, // Purple
        0xFFFF6B6B, // Coral
        0xFF4ECDC4, // Teal
        0xFFFFA07A, // Light Salmon
        0xFF98D8C8, // Mint
        0xFFF7DC6F, // Light Yellow
        0xFFBB8FCE, // Light Purple
        0xFF85C1E9, // Light Blue
        0xFFF8C471, // Orange
        0xFF82E0AA, // Light Green
        0xFFF1948A, // Light Red
        0xFFABEBC6  // Pale Green
    };



    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;

        InitializeMaze();
        GenerateMaze();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose() { }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Update camera position based on audio
        UpdateCamera(f);

        // Clear with black background
        canvas.Clear(0xFF000000);

        // Render the 3D maze
        RenderMaze3D(canvas, f);
    }

    private void InitializeMaze()
    {
        _maze = new MazeCell[MAZE_GRID, MAZE_GRID];

        // Initialize all cells with all walls
        for (int x = 0; x < MAZE_GRID; x++)
        {
            for (int z = 0; z < MAZE_GRID; z++)
            {
                _maze[x, z] = new MazeCell
                {
                    Walls = WallFlags.WALL_NORTH | WallFlags.WALL_SOUTH |
                           WallFlags.WALL_EAST | WallFlags.WALL_WEST,
                    Visited = false
                };
            }
        }

        // Start camera in center
        _cameraX = MAZE_GRID / 2.0f;
        _cameraZ = MAZE_GRID / 2.0f;
        _cameraRot = 0;
        _viewAngle = 0;
    }

    private void GenerateMaze()
    {
        // Recursive backtracking maze generation (simplified)
        var stack = new Stack<(int x, int z)>();
        var startX = _random.Next(MAZE_GRID);
        var startZ = _random.Next(MAZE_GRID);

        stack.Push((startX, startZ));
        _maze[startX, startZ].Visited = true;

        while (stack.Count > 0)
        {
            var (x, z) = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(x, z);

            if (neighbors.Count > 0)
            {
                var (nx, nz) = neighbors[_random.Next(neighbors.Count)];
                RemoveWallBetween(x, z, nx, nz);
                _maze[nx, nz].Visited = true;
                stack.Push((nx, nz));
            }
            else
            {
                stack.Pop();
            }
        }
    }

    private List<(int x, int z)> GetUnvisitedNeighbors(int x, int z)
    {
        var neighbors = new List<(int x, int z)>();

        // Check all four directions
        if (x > 0 && !_maze[x - 1, z].Visited) neighbors.Add((x - 1, z)); // West
        if (x < MAZE_GRID - 1 && !_maze[x + 1, z].Visited) neighbors.Add((x + 1, z)); // East
        if (z > 0 && !_maze[x, z - 1].Visited) neighbors.Add((x, z - 1)); // North
        if (z < MAZE_GRID - 1 && !_maze[x, z + 1].Visited) neighbors.Add((x, z + 1)); // South

        return neighbors;
    }

    private void RemoveWallBetween(int x1, int z1, int x2, int z2)
    {
        if (x1 < x2) // Moving east
        {
            _maze[x1, z1].Walls &= ~WallFlags.WALL_EAST;
            _maze[x2, z2].Walls &= ~WallFlags.WALL_WEST;
        }
        else if (x1 > x2) // Moving west
        {
            _maze[x1, z1].Walls &= ~WallFlags.WALL_WEST;
            _maze[x2, z2].Walls &= ~WallFlags.WALL_EAST;
        }
        else if (z1 < z2) // Moving south
        {
            _maze[x1, z1].Walls &= ~WallFlags.WALL_SOUTH;
            _maze[x2, z2].Walls &= ~WallFlags.WALL_NORTH;
        }
        else if (z1 > z2) // Moving north
        {
            _maze[x1, z1].Walls &= ~WallFlags.WALL_NORTH;
            _maze[x2, z2].Walls &= ~WallFlags.WALL_SOUTH;
        }
    }

    private void UpdateCamera(AudioFeatures f)
    {
        // Audio-reactive camera movement
        float baseSpeed = 0.5f;
        float audioSpeed = baseSpeed + f.Volume * 2f;

        // Move forward/backward based on bass
        float moveSpeed = (f.Bass - 0.5f) * audioSpeed;
        _cameraX += (float)Math.Cos(_cameraRot) * moveSpeed;
        _cameraZ += (float)Math.Sin(_cameraRot) * moveSpeed;

        // Rotate based on mid frequencies
        float rotateSpeed = (f.Mid - 0.5f) * 0.1f;
        _cameraRot += rotateSpeed;

        // Keep camera within bounds
        _cameraX = Math.Max(0.5f, Math.Min(MAZE_GRID - 0.5f, _cameraX));
        _cameraZ = Math.Max(0.5f, Math.Min(MAZE_GRID - 0.5f, _cameraZ));

        // Audio-reactive view angle (look up/down)
        _viewAngle = (f.Treble - 0.5f) * 0.5f;
    }

    private void RenderMaze3D(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // 3D perspective parameters
        float fov = 75f * (float)(Math.PI / 180f);
        float near = 0.1f;
        float far = 50f;

        // Render floor and ceiling first
        RenderFloorAndCeiling(canvas, centerX, centerY, fov, near, far);

        // Render walls
        RenderWalls(canvas, centerX, centerY, fov, near, far, f);

        // Render some fog effect based on distance
        RenderFogEffect(canvas, f);
    }

    private void RenderFloorAndCeiling(ISkiaCanvas canvas, float centerX, float centerY,
                                     float fov, float near, float far)
    {
        // Simple floor and ceiling rendering
        int gridLines = 20;
        float gridSpacing = MAZE_GRID * MAZE_CELL_SIZE / gridLines;

        // Floor grid lines
        for (int i = 0; i <= gridLines; i++)
        {
            float worldZ = i * gridSpacing - MAZE_GRID * MAZE_CELL_SIZE * 0.5f;

            var start3D = ProjectPoint(-MAZE_GRID * MAZE_CELL_SIZE * 0.5f, 0, worldZ,
                                     centerX, centerY, fov, near, far);
            var end3D = ProjectPoint(MAZE_GRID * MAZE_CELL_SIZE * 0.5f, 0, worldZ,
                                   centerX, centerY, fov, near, far);

            if (start3D.z > near && end3D.z > near && start3D.z < far && end3D.z < far)
            {
                float alpha = Math.Max(0.1f, 1f - start3D.z / far);
                // Dynamic floor color based on position and time
                float hue = (worldZ * 0.01f + _time * 0.1f) % 1.0f;
                uint baseColor = HsvToRgb(hue, 0.3f, 0.4f);
                uint color = (uint)((uint)(alpha * 255) << 24 | (baseColor & 0x00FFFFFF));
                canvas.DrawLine(start3D.x, start3D.y, end3D.x, end3D.y, color, 1f);
            }
        }

        // Ceiling grid lines (similar but above)
        for (int i = 0; i <= gridLines; i++)
        {
            float worldZ = i * gridSpacing - MAZE_GRID * MAZE_CELL_SIZE * 0.5f;

            var start3D = ProjectPoint(-MAZE_GRID * MAZE_CELL_SIZE * 0.5f, MAZE_WALL_HEIGHT, worldZ,
                                     centerX, centerY, fov, near, far);
            var end3D = ProjectPoint(MAZE_GRID * MAZE_CELL_SIZE * 0.5f, MAZE_WALL_HEIGHT, worldZ,
                                   centerX, centerY, fov, near, far);

            if (start3D.z > near && end3D.z > near && start3D.z < far && end3D.z < far)
            {
                float alpha = Math.Max(0.05f, 1f - start3D.z / far);
                // Dynamic ceiling color (darker than floor)
                float hue = (worldZ * 0.01f + _time * 0.05f + 0.5f) % 1.0f; // Offset hue for different color
                uint baseColor = HsvToRgb(hue, 0.2f, 0.2f);
                uint color = (uint)((uint)(alpha * 255) << 24 | (baseColor & 0x00FFFFFF));
                canvas.DrawLine(start3D.x, start3D.y, end3D.x, end3D.y, color, 1f);
            }
        }
    }

    private void RenderWalls(ISkiaCanvas canvas, float centerX, float centerY,
                           float fov, float near, float far, AudioFeatures f)
    {
        // Render visible walls using raycasting-like approach
        int renderDistance = 8; // How far to render

        for (int dz = -renderDistance; dz <= renderDistance; dz++)
        {
            for (int dx = -renderDistance; dx <= renderDistance; dx++)
            {
                int cellX = (int)(_cameraX + dx);
                int cellZ = (int)(_cameraZ + dz);

                if (cellX < 0 || cellX >= MAZE_GRID || cellZ < 0 || cellZ >= MAZE_GRID)
                    continue;

                var cell = _maze[cellX, cellZ];
                var worldX = cellX * MAZE_CELL_SIZE - MAZE_GRID * MAZE_CELL_SIZE * 0.5f;
                var worldZ = cellZ * MAZE_CELL_SIZE - MAZE_GRID * MAZE_CELL_SIZE * 0.5f;

                // Render walls for this cell
                RenderCellWalls(canvas, cell, worldX, worldZ, centerX, centerY, fov, near, far, f);
            }
        }
    }

    private void RenderCellWalls(ISkiaCanvas canvas, MazeCell cell, float worldX, float worldZ,
                               float centerX, float centerY, float fov, float near, float far, AudioFeatures f)
    {
        // Audio-reactive wall color
        uint wallColor = _wallColors[_random.Next(_wallColors.Length)];
        float audioBrightness = 0.5f + f.Volume * 0.5f;
        wallColor = AdjustBrightness(wallColor, audioBrightness);

        // North wall
        if ((cell.Walls & WallFlags.WALL_NORTH) != 0)
        {
            RenderWallQuad(canvas,
                worldX, 0, worldZ,
                worldX + MAZE_CELL_SIZE, 0, worldZ,
                worldX + MAZE_CELL_SIZE, MAZE_WALL_HEIGHT, worldZ,
                worldX, MAZE_WALL_HEIGHT, worldZ,
                wallColor, centerX, centerY, fov, near, far);
        }

        // South wall
        if ((cell.Walls & WallFlags.WALL_SOUTH) != 0)
        {
            RenderWallQuad(canvas,
                worldX, 0, worldZ + MAZE_CELL_SIZE,
                worldX + MAZE_CELL_SIZE, 0, worldZ + MAZE_CELL_SIZE,
                worldX + MAZE_CELL_SIZE, MAZE_WALL_HEIGHT, worldZ + MAZE_CELL_SIZE,
                worldX, MAZE_WALL_HEIGHT, worldZ + MAZE_CELL_SIZE,
                wallColor, centerX, centerY, fov, near, far);
        }

        // East wall
        if ((cell.Walls & WallFlags.WALL_EAST) != 0)
        {
            RenderWallQuad(canvas,
                worldX + MAZE_CELL_SIZE, 0, worldZ,
                worldX + MAZE_CELL_SIZE, 0, worldZ + MAZE_CELL_SIZE,
                worldX + MAZE_CELL_SIZE, MAZE_WALL_HEIGHT, worldZ + MAZE_CELL_SIZE,
                worldX + MAZE_CELL_SIZE, MAZE_WALL_HEIGHT, worldZ,
                wallColor, centerX, centerY, fov, near, far);
        }

        // West wall
        if ((cell.Walls & WallFlags.WALL_WEST) != 0)
        {
            RenderWallQuad(canvas,
                worldX, 0, worldZ,
                worldX, 0, worldZ + MAZE_CELL_SIZE,
                worldX, MAZE_WALL_HEIGHT, worldZ + MAZE_CELL_SIZE,
                worldX, MAZE_WALL_HEIGHT, worldZ,
                wallColor, centerX, centerY, fov, near, far);
        }
    }

    private void RenderWallQuad(ISkiaCanvas canvas,
                              float x1, float y1, float z1,
                              float x2, float y2, float z2,
                              float x3, float y3, float z3,
                              float x4, float y4, float z4,
                              uint color, float centerX, float centerY, float fov, float near, float far)
    {
        var p1 = ProjectPoint(x1, y1, z1, centerX, centerY, fov, near, far);
        var p2 = ProjectPoint(x2, y2, z2, centerX, centerY, fov, near, far);
        var p3 = ProjectPoint(x3, y3, z3, centerX, centerY, fov, near, far);
        var p4 = ProjectPoint(x4, y4, z4, centerX, centerY, fov, near, far);

        // Only render if all points are visible
        if (p1.z > near && p2.z > near && p3.z > near && p4.z > near &&
            p1.z < far && p2.z < far && p3.z < far && p4.z < far)
        {
            // Distance-based alpha
            float avgZ = (p1.z + p2.z + p3.z + p4.z) / 4f;
            float alpha = Math.Max(0.3f, 1f - avgZ / far);
            uint fadedColor = (uint)((uint)(alpha * 255) << 24 | (color & 0x00FFFFFF));

            // Draw the quad as four triangles
            canvas.DrawLine(p1.x, p1.y, p2.x, p2.y, fadedColor, 2f);
            canvas.DrawLine(p2.x, p2.y, p3.x, p3.y, fadedColor, 2f);
            canvas.DrawLine(p3.x, p3.y, p4.x, p4.y, fadedColor, 2f);
            canvas.DrawLine(p4.x, p4.y, p1.x, p1.y, fadedColor, 2f);
        }
    }

    private void RenderFogEffect(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Add some fog particles that react to audio
        int particleCount = (int)(20 + f.Volume * 50);

        for (int i = 0; i < particleCount; i++)
        {
            float x = _random.Next(_width);
            float y = _random.Next(_height);
            float alpha = (float)_random.NextDouble() * 0.3f;
            uint fogColor = (uint)((uint)(alpha * 255) << 24 | 0x00C0C0C0);

            canvas.FillCircle(x, y, 1f, fogColor);
        }
    }

    private (float x, float y, float z) ProjectPoint(float worldX, float worldY, float worldZ,
                                                   float centerX, float centerY, float fov, float near, float far)
    {
        // Transform to camera space
        float localX = worldX - _cameraX * MAZE_CELL_SIZE + MAZE_GRID * MAZE_CELL_SIZE * 0.5f;
        float localY = worldY - CAMERA_HEIGHT;
        float localZ = worldZ - _cameraZ * MAZE_CELL_SIZE + MAZE_GRID * MAZE_CELL_SIZE * 0.5f;

        // Apply camera rotation
        float cosRot = (float)Math.Cos(_cameraRot);
        float sinRot = (float)Math.Sin(_cameraRot);

        float rotX = localX * cosRot - localZ * sinRot;
        float rotZ = localX * sinRot + localZ * cosRot;

        // Apply view angle (look up/down)
        float cosView = (float)Math.Cos(_viewAngle);
        float sinView = (float)Math.Sin(_viewAngle);

        float viewY = localY * cosView - rotZ * sinView;
        float viewZ = localY * sinView + rotZ * cosView;

        // Perspective projection
        if (viewZ <= near) viewZ = near + 0.1f;

        float screenX = centerX + (rotX / viewZ) * (centerX / (float)Math.Tan(fov * 0.5));
        float screenY = centerY + (viewY / viewZ) * (centerY / (float)Math.Tan(fov * 0.5));

        return (screenX, screenY, viewZ);
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

    private uint HsvToRgb(float h, float s, float v)
    {
        float r, g, b;

        int i = (int)(h * 6);
        float f = h * 6 - i;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);

        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
            default: r = v; g = p; b = q; break;
        }

        byte rb = (byte)(r * 255);
        byte gb = (byte)(g * 255);
        byte bb = (byte)(b * 255);

        return (uint)((uint)rb << 16 | (uint)gb << 8 | (uint)bb);
    }
}
