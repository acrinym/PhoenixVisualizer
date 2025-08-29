using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Phoenix Grid Pulse - Dynamic grid structure with audio-reactive scaling and pulsing effects
/// Inspired by Windows Media Player's Visualizer4 but with advanced grid animations
/// </summary>
public sealed class PhoenixGridPulse : IVisualizerPlugin
{
    public string Id => "phoenix_grid_pulse";
    public string DisplayName => "🔳 Phoenix Grid Pulse";

    private int _width, _height;
    private float _time;
    private readonly Random _random = new();

    // Grid system constants
    private const int GRID_SIZE = 16;
    private const float PULSE_SPEED = 0.08f;
    private const float GRID_SPACING = 40f;
    private const float MAX_PULSE_SCALE = 2.5f;

    // Grid state
    private readonly float[] _gridPulsePhases;
    private readonly float[] _gridEnergies;
    private readonly float[] _gridOffsets;
    private float _globalPulsePhase;
    private float _globalEnergy;

    // Color system
    private readonly uint[] _gridColors = new uint[]
    {
        0xFF00FF00, // Bright Green
        0xFFFF0000, // Bright Red
        0xFF0000FF, // Bright Blue
        0xFFFFFF00, // Bright Yellow
        0xFFFF00FF, // Bright Magenta
        0xFF00FFFF, // Bright Cyan
        0xFFFF8000, // Bright Orange
        0xFF8000FF, // Bright Purple
        0xFF80FF00, // Bright Lime
        0xFF0080FF, // Electric Blue
    };

    public PhoenixGridPulse()
    {
        int totalCells = GRID_SIZE * GRID_SIZE;
        _gridPulsePhases = new float[totalCells];
        _gridEnergies = new float[totalCells];
        _gridOffsets = new float[totalCells];

        // Initialize grid with random phases and offsets
        for (int i = 0; i < totalCells; i++)
        {
            _gridPulsePhases[i] = (float)(_random.NextDouble() * Math.PI * 2);
            _gridOffsets[i] = (float)(_random.NextDouble() * Math.PI);
        }
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        _globalPulsePhase = 0;
        _globalEnergy = 0;
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

        // Update global state
        UpdateGlobalState(f);

        // Create dynamic grid background
        uint bgColor = CalculateGridBackground(_globalEnergy);
        canvas.Clear(bgColor);

        // Render grid structure
        RenderGrid(canvas, f);

        // Add grid effects
        RenderGridEffects(canvas, f);
    }

    private void UpdateGlobalState(AudioFeatures f)
    {
        // Update global pulse phase
        _globalPulsePhase += PULSE_SPEED * (1f + f.Volume * 0.5f);

        // Update global energy with smoothing
        float targetEnergy = (f.Bass + f.Mid + f.Treble) / 3f;
        _globalEnergy = _globalEnergy * 0.95f + targetEnergy * 0.05f;

        // Update individual grid cells
        int totalCells = GRID_SIZE * GRID_SIZE;
        for (int i = 0; i < totalCells; i++)
        {
            // Update pulse phases
            _gridPulsePhases[i] += PULSE_SPEED * (0.5f + f.Treble * 0.5f);

            // Update energies based on frequency bands
            float frequencyRatio = (float)i / totalCells;
            float bandEnergy = GetFrequencyBandEnergy(f, frequencyRatio);
            _gridEnergies[i] = _gridEnergies[i] * 0.9f + bandEnergy * 0.1f;
        }
    }

    private void RenderGrid(ISkiaCanvas canvas, AudioFeatures f)
    {
        float startX = (_width - (GRID_SIZE - 1) * GRID_SPACING) * 0.5f;
        float startY = (_height - (GRID_SIZE - 1) * GRID_SPACING) * 0.5f;

        // Render grid lines
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                int cellIndex = row * GRID_SIZE + col;
                float cellX = startX + col * GRID_SPACING;
                float cellY = startY + row * GRID_SPACING;

                RenderGridCell(canvas, cellIndex, cellX, cellY, f);
            }
        }

        // Render connecting lines with pulse effects
        RenderConnectingLines(canvas, startX, startY, f);
    }

    private void RenderGridCell(ISkiaCanvas canvas, int cellIndex, float x, float y, AudioFeatures f)
    {
        // Calculate cell pulse effect
        float basePulse = (float)Math.Sin(_gridPulsePhases[cellIndex] + _gridOffsets[cellIndex]);
        float energyPulse = _gridEnergies[cellIndex] * 2f;
        float beatPulse = f.Beat ? 1.5f : 1f;

        float totalPulse = (basePulse * 0.5f + 0.5f) * energyPulse * beatPulse;
        float scale = 1f + totalPulse * (MAX_PULSE_SCALE - 1f);

        // Calculate cell size and color
        float cellSize = GRID_SPACING * 0.3f * scale;
        uint cellColor = GetGridCellColor(cellIndex, totalPulse, f.Volume);

        // Draw cell as a filled circle
        canvas.FillCircle(x, y, cellSize, cellColor);

        // Add glow effect for high-energy cells
        if (totalPulse > 1.2f)
        {
            uint glowColor = (cellColor & 0x00FFFFFF) | 0x60u << 24;
            canvas.FillCircle(x, y, cellSize * 1.8f, glowColor);
        }
    }

    private void RenderConnectingLines(ISkiaCanvas canvas, float startX, float startY, AudioFeatures f)
    {
        // Horizontal lines
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE - 1; col++)
            {
                int cell1Index = row * GRID_SIZE + col;
                int cell2Index = row * GRID_SIZE + col + 1;

                float x1 = startX + col * GRID_SPACING;
                float y1 = startY + row * GRID_SPACING;
                float x2 = startX + (col + 1) * GRID_SPACING;
                float y2 = startY + row * GRID_SPACING;

                RenderConnectingLine(canvas, cell1Index, cell2Index, x1, y1, x2, y2, f);
            }
        }

        // Vertical lines
        for (int col = 0; col < GRID_SIZE; col++)
        {
            for (int row = 0; row < GRID_SIZE - 1; row++)
            {
                int cell1Index = row * GRID_SIZE + col;
                int cell2Index = (row + 1) * GRID_SIZE + col;

                float x1 = startX + col * GRID_SPACING;
                float y1 = startY + row * GRID_SPACING;
                float x2 = startX + col * GRID_SPACING;
                float y2 = startY + (row + 1) * GRID_SPACING;

                RenderConnectingLine(canvas, cell1Index, cell2Index, x1, y1, x2, y2, f);
            }
        }
    }

    private void RenderConnectingLine(ISkiaCanvas canvas, int cell1Index, int cell2Index,
                                   float x1, float y1, float x2, float y2, AudioFeatures f)
    {
        // Calculate line energy based on connected cells
        float energy1 = _gridEnergies[cell1Index];
        float energy2 = _gridEnergies[cell2Index];
        float lineEnergy = (energy1 + energy2) * 0.5f;

        // Calculate line thickness based on energy
        float baseThickness = 1f;
        float thickness = baseThickness + lineEnergy * 4f;

        // Calculate line color based on energy
        uint lineColor = GetGridLineColor(lineEnergy, f.Volume, f.Beat);

        // Add wave effect to line
        float waveOffset = (float)Math.Sin(_time * 4f + (cell1Index + cell2Index) * 0.5f) * 3f;
        float midX = (x1 + x2) * 0.5f + waveOffset;
        float midY = (y1 + y2) * 0.5f + waveOffset;

        // Draw curved line through midpoint
        canvas.DrawLine(x1, y1, midX, midY, lineColor, thickness);
        canvas.DrawLine(midX, midY, x2, y2, lineColor, thickness);
    }

    private void RenderGridEffects(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Add energy waves
        RenderEnergyWaves(canvas, f);

        // Add pulsing rings
        RenderPulsingRings(canvas, f);

        // Add grid status indicator
        RenderGridStatus(canvas, f);
    }

    private void RenderEnergyWaves(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // Create expanding wave rings
        for (int wave = 0; wave < 5; wave++)
        {
            float waveRadius = (_time * 100f + wave * 80f) % (_width * 0.8f);
            float waveThickness = 2f + wave * 0.5f;
            float alpha = (int)(200 - wave * 40);

            uint waveColor = _gridColors[wave % _gridColors.Length];
            waveColor = (waveColor & 0x00FFFFFF) | ((uint)alpha << 24);

            canvas.DrawCircle(centerX, centerY, waveRadius, waveColor, false);
        }
    }

    private void RenderPulsingRings(ISkiaCanvas canvas, AudioFeatures f)
    {
        if (_globalEnergy < 0.2f) return;

        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // Create pulsing rings around the grid
        int ringCount = (int)(_globalEnergy * 8);
        for (int i = 0; i < ringCount; i++)
        {
            float ringRadius = 100f + i * 30f;
            float pulseScale = 1f + (float)Math.Sin(_globalPulsePhase * 2f + i * 0.5f) * 0.3f;
            ringRadius *= pulseScale;

            uint ringColor = _gridColors[i % _gridColors.Length];
            ringColor = (ringColor & 0x00FFFFFF) | 0x80u << 24;

            canvas.DrawCircle(centerX, centerY, ringRadius, ringColor, false);
        }
    }

    private void RenderGridStatus(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Draw grid status at the bottom
        float statusY = _height - 25;
        float statusWidth = _width - 50;
        float statusHeight = 4;

        // Background
        canvas.FillRect(25, statusY, statusWidth, statusHeight, 0xFF404040);

        // Grid energy indicators
        int totalCells = GRID_SIZE * GRID_SIZE;
        float cellWidth = statusWidth / totalCells;

        for (int i = 0; i < totalCells; i++)
        {
            float cellEnergy = _gridEnergies[i];
            float cellHeight = statusHeight * cellEnergy;

            uint cellColor = _gridColors[i % _gridColors.Length];
            canvas.FillRect(
                25 + i * cellWidth,
                statusY + statusHeight - cellHeight,
                cellWidth,
                cellHeight,
                cellColor
            );
        }
    }

    private uint CalculateGridBackground(float energy)
    {
        // Dynamic background based on grid energy
        if (energy < 0.2f)
            return 0xFF101010; // Very dark
        else if (energy < 0.4f)
            return 0xFF181820; // Dark with slight blue
        else if (energy < 0.6f)
            return 0xFF202030; // Medium dark
        else
            return 0xFF282840; // Lighter with blue tint
    }

    private uint GetGridCellColor(int cellIndex, float pulse, float volume)
    {
        uint baseColor = _gridColors[cellIndex % _gridColors.Length];

        // Enhance brightness based on pulse and volume
        float brightness = 0.5f + pulse * 0.5f + volume * 0.3f;
        brightness = MathF.Min(1f, brightness);

        return AdjustBrightness(baseColor, brightness);
    }

    private uint GetGridLineColor(float energy, float volume, bool beat)
    {
        // Choose color based on energy level
        int colorIndex = (int)(energy * _gridColors.Length);
        colorIndex = Math.Clamp(colorIndex, 0, _gridColors.Length - 1);

        uint baseColor = _gridColors[colorIndex];

        // Enhance for beat
        if (beat)
        {
            return AdjustBrightness(baseColor, 1.5f);
        }

        return AdjustBrightness(baseColor, 0.7f + volume * 0.3f);
    }

    private float GetFrequencyBandEnergy(AudioFeatures f, float frequencyRatio)
    {
        // Map frequency ratio to audio bands
        if (frequencyRatio < 0.3f)
            return f.Bass;
        else if (frequencyRatio < 0.7f)
            return f.Mid;
        else
            return f.Treble;
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
}
