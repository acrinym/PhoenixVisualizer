using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Fractal Flame visualizer - ported from xscreensaver flame.c
/// Creates recursive fractal cosmic flames using iterated function systems
/// </summary>
public sealed class FlameFractal : IVisualizerPlugin
{
    public string Id => "flame_fractal";
    public string DisplayName => "Fractal Flame";

    // Algorithm constants
    private const int POINT_BUFFER_SIZE = 10;
    private const int MAXLEV = 4;
    private const int MAXKINDS = 10;
    private const int MAX_COLORS = 128;

    // State variables
    private int _width, _height;
    private readonly Random _rng = new();

    // Flame parameters
    private double[,,] _f = null!; // [2][3][MAXLEV] - three non-homogeneous transforms
    private int _maxTotal = 10000;
    private int _maxLevels = 100;
    private int _curLevel;
    private int _variation;
    private int _snum;
    private int _anum;
    private int _numPoints;
    private int _totalPoints;
    private int _pixcol;
    private int _ncolors = 64;

    // Color palette
    private uint[] _colors = null!;

    // Point buffer for batched rendering
    private readonly (int x, int y)[ ] _points = new (int x, int y)[POINT_BUFFER_SIZE];

    // Timing
    private float _time;
    private bool _doReset;
    private int _scale = 1;
    private bool _flameAlt;

    // Audio reactivity
    private float _audioModulation;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;

        // Initialize high-DPI scaling
        if (_width > 2560 || _height > 2560)
            _scale *= 2; // Retina displays

        // Initialize transform matrices
        _f = new double[2, 3, MAXLEV];

        // Initialize color palette
        InitializeColors();

        // Reset flame state
        ResetFlame();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        if (_width > 2560 || _height > 2560)
            _scale *= 2;
        else
            _scale = 1;
    }

    public void Dispose() { }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Update audio modulation
        _audioModulation = f.Volume;

        // Clear canvas occasionally
        if (_doReset)
        {
            canvas.Clear(0xFF000000); // Black background
            _doReset = false;
        }

        // Update level and reset periodically
        if (_curLevel++ % _maxLevels == 0)
        {
            _doReset = true;
            _flameAlt = !_flameAlt;
            _variation = _rng.Next(MAXKINDS);
            _pixcol = _rng.Next(_ncolors);
        }

        // Generate new transform coefficients
        GenerateTransforms();

        // Reset point counters
        _numPoints = 0;
        _totalPoints = 0;

        // Start recursive fractal generation
        Recurse(0.0, 0.0, 0, canvas);

        // Render any remaining points
        RenderPoints(canvas);

        // Cycle colors for animation
        if (_ncolors > 2)
        {
            _pixcol = (_pixcol + 1) % _ncolors;
        }
    }

    private void ResetFlame()
    {
        _curLevel = 0;
        _variation = _rng.Next(MAXKINDS);
        _doReset = false;
        _flameAlt = false;
        _pixcol = _rng.Next(_ncolors);
    }

    private void InitializeColors()
    {
        _colors = new uint[MAX_COLORS];

        // Create a smooth color palette from cool blues to hot oranges/reds
        for (int i = 0; i < MAX_COLORS; i++)
        {
            float t = (float)i / (MAX_COLORS - 1);

            // HSV to RGB conversion for smooth flame colors
            // Hue: blue (240°) to red (0°)
            float hue = 240f - (240f * t);

            // Saturation: high for vibrant colors
            float saturation = 0.8f;

            // Value/Brightness: varies for flame effect
            float value = 0.3f + 0.7f * (float)Math.Sin(t * Math.PI);

            _colors[i] = HsvToRgb(hue, saturation, value);
        }

        _ncolors = MAX_COLORS;
    }

    private uint HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
        float m = v - c;

        float r, g, b;
        if (h < 60f) { r = c; g = x; b = 0f; }
        else if (h < 120f) { r = x; g = c; b = 0f; }
        else if (h < 180f) { r = 0f; g = c; b = x; }
        else if (h < 240f) { r = 0f; g = x; b = c; }
        else if (h < 300f) { r = x; g = 0f; b = c; }
        else { r = c; g = 0f; b = x; }

        byte R = (byte)((r + m) * 255f);
        byte G = (byte)((g + m) * 255f);
        byte B = (byte)((b + m) * 255f);

        return (uint)(0xFF << 24 | R << 16 | G << 8 | B);
    }

    private void GenerateTransforms()
    {
        // Number of functions
        _snum = 2 + (_curLevel % (MAXLEV - 1));

        // How many use alternate (variation) form
        if (_flameAlt)
            _anum = 0;
        else
            _anum = _rng.Next(_snum) + 2;

        // Generate 6 coefficients per function (affine transform)
        for (int k = 0; k < _snum; k++)
        {
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    _f[i, j, k] = ((double)(_rng.Next() & 1023) / 512.0 - 1.0);
        }
    }

    private bool Recurse(double x, double y, int level, ISkiaCanvas canvas)
    {
        if (level == _maxLevels)
        {
            _totalPoints++;
            if (_totalPoints > _maxTotal) // Limit total points
                return false;

            // Check bounds (-1.0 to 1.0 coordinate system)
            if (x >= -1.0 && x <= 1.0 && y >= -1.0 && y <= 1.0)
            {
                // Convert to screen coordinates
                int screenX = (int)((_width / 2) * (x + 1.0));
                int screenY = (int)((_height / 2) * (y + 1.0));

                // Add to point buffer
                _points[_numPoints] = (screenX, screenY);
                _numPoints++;

                // Render batch when buffer is full
                if (_numPoints >= POINT_BUFFER_SIZE)
                {
                    RenderPoints(canvas);
                    _numPoints = 0;
                }
            }
            return true;
        }
        else
        {
            // Apply each transform function
            for (int i = 0; i < _snum; i++)
            {
                double nx = x;
                double ny = y;

                // Apply affine transformation
                nx = _f[0, 0, i] * x + _f[0, 1, i] * y + _f[0, 2, i];
                ny = _f[1, 0, i] * x + _f[1, 1, i] * y + _f[1, 2, i];

                // Scale back when values get very large
                if ((Math.Abs(nx) > 1.0E5) || (Math.Abs(ny) > 1.0E5))
                {
                    if (ny != 0)
                        nx = nx / ny;
                }

                // Apply variation if this function uses alternate form
                if (i < _anum)
                {
                    ApplyVariation(ref nx, ref ny);
                }

                // Recurse to next level
                if (!Recurse(nx, ny, level + 1, canvas))
                    return false;
            }
            return true;
        }
    }

    private void ApplyVariation(ref double nx, ref double ny)
    {
        switch (_variation)
        {
            case 0: // sinusoidal
                nx = Math.Sin(nx);
                ny = Math.Sin(ny);
                break;

            case 1: // complex
                {
                    double r2 = nx * nx + ny * ny + 1e-6;
                    nx = nx / r2;
                    ny = ny / r2;
                }
                break;

            case 2: // bent
                if (nx < 0.0) nx = nx * 2.0;
                if (ny < 0.0) ny = ny / 2.0;
                break;

            case 3: // swirl
                {
                    double r = (nx * nx + ny * ny); // times k here is fun
                    double c1 = Math.Sin(r);
                    double c2 = Math.Cos(r);
                    double t = nx;

                    if (nx > 1e4 || nx < -1e4 || ny > 1e4 || ny < -1e4)
                        ny = 1e4;
                    else
                        ny = c2 * t + c1 * ny;
                    nx = c1 * nx - c2 * ny;
                }
                break;

            case 4: // horseshoe
                {
                    double r;
                    if (nx == 0.0 && ny == 0.0)
                        r = 0.0;
                    else
                        r = Math.Atan2(nx, ny); // times k here is fun

                    double c1 = Math.Sin(r);
                    double c2 = Math.Cos(r);
                    double t = nx;

                    nx = c1 * nx - c2 * ny;
                    ny = c2 * t + c1 * ny;
                }
                break;

            case 5: // drape
                {
                    double t;
                    if (nx == 0.0 && ny == 0.0)
                        t = 0.0;
                    else
                        t = Math.Atan2(nx, ny) / Math.PI;

                    if (nx > 1e4 || nx < -1e4 || ny > 1e4 || ny < -1e4)
                        ny = 1e4;
                    else
                        ny = Math.Sqrt(nx * nx + ny * ny) - 1.0;
                    nx = t;
                }
                break;

            case 6: // broken
                if (nx > 1.0) nx = nx - 1.0;
                if (nx < -1.0) nx = nx + 1.0;
                if (ny > 1.0) ny = ny - 1.0;
                if (ny < -1.0) ny = ny + 1.0;
                break;

            case 7: // spherical
                {
                    double r = 0.5 + Math.Sqrt(nx * nx + ny * ny + 1e-6);
                    nx = nx / r;
                    ny = ny / r;
                }
                break;

            case 8: // atan variant
                nx = Math.Atan(nx) / (Math.PI / 2);
                ny = Math.Atan(ny) / (Math.PI / 2);
                break;

            case 9: // complex sine
                {
                    double u = nx;
                    double v = ny;
                    double ev = Math.Exp(v);
                    double emv = Math.Exp(-v);

                    nx = (ev + emv) * Math.Sin(u) / 2.0;
                    ny = (ev - emv) * Math.Cos(u) / 2.0;
                }
                break;

            default:
                nx = Math.Sin(nx);
                ny = Math.Sin(ny);
                break;
        }
    }

    private void RenderPoints(ISkiaCanvas canvas)
    {
        if (_numPoints == 0) return;

        uint color = (_ncolors > 2) ? _colors[_pixcol] : 0xFFFFFFFF;

        // Add audio-reactive brightness modulation
        float brightness = 0.5f + _audioModulation * 0.5f;
        color = ModulateBrightness(color, brightness);

        // Enhanced flame rendering with density-based blending
        var densityMap = new int[_width, _height];

        // Accumulate points into density map
        for (int i = 0; i < _numPoints; i++)
        {
            var point = _points[i];
            if (point.x >= 0 && point.x < _width && point.y >= 0 && point.y < _height)
            {
                int px = (int)point.x;
                int py = (int)point.y;

                // Add to density map with gaussian-like distribution
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int nx = px + dx;
                        int ny = py + dy;
                        if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                        {
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                            int density = (int)(4 / (1 + dist));
                            densityMap[nx, ny] += density;
                        }
                    }
                }
            }
        }

        // Render density-based flame
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int density = densityMap[x, y];
                if (density > 0)
                {
                    // Create flame-like color based on density and position
                    float intensity = Math.Min(density / 8.0f, 1.0f);
                    float hue = 0.05f + (y / (float)_height) * 0.1f; // Red to orange gradient
                    uint flameColor = HsvToRgb(hue, 0.8f, intensity);

                    // Blend with background
                    canvas.FillRect(x, y, 1, 1, flameColor);
                }
            }
        }
    }

    private uint ModulateBrightness(uint color, float brightness)
    {
        // Extract RGB components
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);

        // Apply brightness modulation
        r = (byte)(r * brightness);
        g = (byte)(g * brightness);
        b = (byte)(b * brightness);

        return (uint)(0xFF << 24 | r << 16 | g << 8 | b);
    }
}
