using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Pong Simulation superscope visualization based on AVS superscope code
/// </summary>
public sealed class PongSuperscope : IVisualizerPlugin
{
    public string Id => "pong_superscope";
    public string DisplayName => "Pong Simulation";

    private int _width;
    private int _height;
    private float _time;
    private float _ballX = 0;
    private float _ballY = 0;
    private float _ballVX = 0.02f;
    private float _ballVY = 0.015f;
    private float _paddleLeftY = 0;
    private float _paddleRightY = 0;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear with dark background
        canvas.Clear(0xFF000000);
        
        // Update time
        _time += 0.02f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        
        // Update ball physics
        _ballX += _ballVX;
        _ballY += _ballVY;
        
        // Ball collision with walls
        if (_ballX > 0.9f) _ballVX = -Math.Abs(_ballVX);
        if (_ballX < -0.9f) _ballVX = Math.Abs(_ballVX);
        if (_ballY > 0.8f) _ballVY = -Math.Abs(_ballVY);
        if (_ballY < -0.8f) _ballVY = Math.Abs(_ballVY);
        
        // Update paddle positions (follow ball with some lag)
        _paddleLeftY = _paddleLeftY * 0.8f + _ballY * 0.2f;
        _paddleRightY = _paddleRightY * 0.8f + _ballY * 0.2f;
        
        // Handle beat events - speed up ball
        if (beat)
        {
            _ballVX *= 1.05f;
            _ballVY *= 1.05f;
        }
        
        // Create points array for the pong game
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        // Draw left paddle
        for (int i = 0; i < 40; i++)
        {
            float t = i / 40.0f;
            float x = -0.9f;
            float y = _paddleLeftY + 0.6f * (t - 0.5f);
            points.Add((x, y));
        }
        
        // Draw right paddle
        for (int i = 0; i < 40; i++)
        {
            float t = i / 40.0f;
            float x = 0.9f;
            float y = _paddleRightY + 0.6f * (t - 0.5f);
            points.Add((x, y));
        }
        
        // Draw ball
        for (int i = 0; i < 20; i++)
        {
            float t = i / 20.0f;
            float angle = t * 6.283f;
            float x = _ballX + 0.05f * (float)Math.Cos(angle);
            float y = _ballY + 0.05f * (float)Math.Sin(angle);
            points.Add((x, y));
        }
        
        // Scale and center all points
        var scaledPoints = new System.Collections.Generic.List<(float x, float y)>();
        foreach (var point in points)
        {
            float x = point.x * _width * 0.4f + _width * 0.5f;
            float y = point.y * _height * 0.4f + _height * 0.5f;
            scaledPoints.Add((x, y));
        }
        
        // Draw the pong game elements
        uint color = beat ? 0xFFFF00FF : 0xFF00FFFF; // Magenta on beat, cyan otherwise
        canvas.SetLineWidth(2.0f);
        
        // Draw paddles
        for (int i = 0; i < 39; i++)
        {
            canvas.DrawLine(scaledPoints[i].x, scaledPoints[i].y, 
                           scaledPoints[i + 1].x, scaledPoints[i + 1].y, color, 2.0f);
        }
        
        // Draw ball
        for (int i = 40; i < 59; i++)
        {
            canvas.DrawLine(scaledPoints[i].x, scaledPoints[i].y, 
                           scaledPoints[(i + 1) % 60].x, scaledPoints[(i + 1) % 60].y, color, 2.0f);
        }
        
        // Draw score or time
        uint textColor = beat ? 0xFFFFFF00 : 0xFF00FF00;
        canvas.DrawText($"Time: {_time:F1}s", 10, 30, textColor, 14.0f);
        canvas.DrawText($"Speed: {Math.Sqrt(_ballVX * _ballVX + _ballVY * _ballVY):F3}", 10, 50, textColor, 14.0f);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
