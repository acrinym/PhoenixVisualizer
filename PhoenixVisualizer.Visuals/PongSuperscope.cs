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

        // Ball collision with walls (using proper AVS coordinate system -1 to 1)
        if (_ballX > 0.85f) // Right wall - bounce left
        {
            _ballX = 0.85f; // Keep ball in bounds
            _ballVX = -Math.Abs(_ballVX); // Ensure it goes left
        }
        if (_ballX < -0.85f) // Left wall - bounce right
        {
            _ballX = -0.85f; // Keep ball in bounds
            _ballVX = Math.Abs(_ballVX); // Ensure it goes right
        }
        if (_ballY > 0.85f) // Top wall - bounce down
        {
            _ballY = 0.85f; // Keep ball in bounds
            _ballVY = -Math.Abs(_ballVY); // Ensure it goes down
        }
        if (_ballY < -0.85f) // Bottom wall - bounce up
        {
            _ballY = -0.85f; // Keep ball in bounds
            _ballVY = Math.Abs(_ballVY); // Ensure it goes up
        }
        
        // Update paddle positions (follow ball with some lag)
        _paddleLeftY = _paddleLeftY * 0.8f + _ballY * 0.2f;
        _paddleRightY = _paddleRightY * 0.8f + _ballY * 0.2f;

        // Paddle collision detection
        float paddleWidth = 0.05f; // Paddle width in AVS coordinates
        float paddleHeight = 0.6f;  // Paddle height in AVS coordinates

        // Left paddle collision
        if (_ballX < -0.85f + paddleWidth && _ballX > -0.9f &&
            _ballY > _paddleLeftY - paddleHeight/2 && _ballY < _paddleLeftY + paddleHeight/2)
        {
            _ballX = -0.85f + paddleWidth; // Move ball to paddle edge
            _ballVX = Math.Abs(_ballVX); // Bounce right
            _ballVY += (_ballY - _paddleLeftY) * 0.3f; // Add spin based on where ball hits paddle
        }

        // Right paddle collision
        if (_ballX > 0.85f - paddleWidth && _ballX < 0.9f &&
            _ballY > _paddleRightY - paddleHeight/2 && _ballY < _paddleRightY + paddleHeight/2)
        {
            _ballX = 0.85f - paddleWidth; // Move ball to paddle edge
            _ballVX = -Math.Abs(_ballVX); // Bounce left
            _ballVY += (_ballY - _paddleRightY) * 0.3f; // Add spin based on where ball hits paddle
        }
        
        // Handle beat events - speed up ball
        if (beat)
        {
            _ballVX *= 1.05f;
            _ballVY *= 1.05f;
        }
        
        // Draw the pong game elements
        uint color = beat ? 0xFFFF00FF : 0xFF00FFFF; // Magenta on beat, cyan otherwise

        // Draw left paddle as a solid rectangle
        float leftPaddleX = (-0.9f + 1.0f) * _width * 0.5f;
        float leftPaddleTop = (_paddleLeftY - 0.3f + 1.0f) * _height * 0.5f;
        float leftPaddleBottom = (_paddleLeftY + 0.3f + 1.0f) * _height * 0.5f;
        canvas.FillRect(leftPaddleX - 3, leftPaddleTop, 6, leftPaddleBottom - leftPaddleTop, color);

        // Draw right paddle as a solid rectangle
        float rightPaddleX = (0.9f + 1.0f) * _width * 0.5f;
        float rightPaddleTop = (_paddleRightY - 0.3f + 1.0f) * _height * 0.5f;
        float rightPaddleBottom = (_paddleRightY + 0.3f + 1.0f) * _height * 0.5f;
        canvas.FillRect(rightPaddleX - 3, rightPaddleTop, 6, rightPaddleBottom - rightPaddleTop, color);

        // Draw ball as a solid circle
        float ballScreenX = (_ballX + 1.0f) * _width * 0.5f;
        float ballScreenY = (_ballY + 1.0f) * _height * 0.5f;
        float ballRadius = Math.Min(_width, _height) * 0.03f;
        canvas.FillCircle(ballScreenX, ballScreenY, ballRadius, color);
        
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
