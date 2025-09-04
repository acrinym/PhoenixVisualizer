using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Pong Simulation superscope visualization based on AVS superscope code
/// FIXED: Now fully audio-reactive pong game instead of independent simulation
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
        
        // FIXED: Audio-reactive time and physics updates
        var energy = features.Energy;
        var bass = features.Bass;
        var mid = features.Mid;
        var treble = features.Treble;
        var beat = features.Beat;
        var volume = features.Volume;
        
        // Audio-reactive frame rate and time
        var audioSpeed = 1.0f + energy * 0.5f + bass * 0.3f;
        _time += 0.016f * audioSpeed;
        
        // FIXED: Audio-reactive ball physics
        var baseBallSpeed = 1.0f;
        var energySpeed = energy * 0.8f;
        var bassSpeed = bass * 0.5f;
        var trebleSpeed = treble * 0.3f;
        var totalSpeed = baseBallSpeed + energySpeed + bassSpeed + trebleSpeed;
        
        // Audio-reactive ball movement
        _ballX += _ballVX * totalSpeed;
        _ballY += _ballVY * totalSpeed;

        // Ball collision with walls (using proper AVS coordinate system -1 to 1)
        if (_ballX > 0.85f) // Right wall - bounce left
        {
            _ballX = 0.85f; // Keep ball in bounds
            _ballVX = -Math.Abs(_ballVX); // Ensure it goes left
            // Add slight randomization to prevent getting stuck
            _ballVY += (Random.Shared.NextSingle() - 0.5f) * 0.01f;
        }
        if (_ballX < -0.85f) // Left wall - bounce right
        {
            _ballX = -0.85f; // Keep ball in bounds
            _ballVX = Math.Abs(_ballVX); // Ensure it goes right
            // Add slight randomization to prevent getting stuck
            _ballVY += (Random.Shared.NextSingle() - 0.5f) * 0.01f;
        }
        if (_ballY > 0.85f) // Top wall - bounce down
        {
            _ballY = 0.85f; // Keep ball in bounds
            _ballVY = -Math.Abs(_ballVY); // Ensure it goes down
            // Add slight randomization to prevent getting stuck
            _ballVX += (Random.Shared.NextSingle() - 0.5f) * 0.01f;
        }
        if (_ballY < -0.85f) // Bottom wall - bounce up
        {
            _ballY = -0.85f; // Keep ball in bounds
            _ballVY = Math.Abs(_ballVY); // Ensure it goes up
            // Add slight randomization to prevent getting stuck
            _ballVX += (Random.Shared.NextSingle() - 0.5f) * 0.01f;
        }
        
        // Prevent ball from getting completely stuck by ensuring minimum velocity
        float minVelocity = 0.005f;
        if (Math.Abs(_ballVX) < minVelocity) _ballVX = minVelocity * Math.Sign(_ballVX);
        if (Math.Abs(_ballVY) < minVelocity) _ballVY = minVelocity * Math.Sign(_ballVY);
        
        // FIXED: Audio-reactive paddle movement
        var basePaddleSpeed = 0.3f;
        var midPaddleSpeed = mid * 0.4f; // Mid frequencies affect paddle responsiveness
        var treblePaddleSpeed = treble * 0.2f; // Treble adds precision
        var totalPaddleSpeed = basePaddleSpeed + midPaddleSpeed + treblePaddleSpeed;
        
        // Audio-reactive paddle movement
        _paddleLeftY = _paddleLeftY * (1f - totalPaddleSpeed) + _ballY * totalPaddleSpeed;
        _paddleRightY = _paddleRightY * (1f - totalPaddleSpeed) + _ballY * totalPaddleSpeed;

        // Paddle collision detection
        float paddleWidth = 0.05f; // Paddle width in AVS coordinates
        float paddleHeight = 0.6f;  // Paddle height in AVS coordinates

        // FIXED: Audio-reactive paddle collision effects
        var bassSpin = bass * 0.5f; // Bass affects spin intensity
        var energyBounce = energy * 0.3f; // Energy affects bounce intensity
        
        // Left paddle collision
        if (_ballX < -0.85f + paddleWidth && _ballX > -0.9f &&
            _ballY > _paddleLeftY - paddleHeight/2 && _ballY < _paddleLeftY + paddleHeight/2)
        {
            _ballX = -0.85f + paddleWidth; // Move ball to paddle edge
            _ballVX = Math.Abs(_ballVX) * (1.0f + energyBounce); // Audio-reactive bounce
            _ballVY += (_ballY - _paddleLeftY) * (0.3f + bassSpin); // Audio-reactive spin
        }

        // Right paddle collision
        if (_ballX > 0.85f - paddleWidth && _ballX < 0.9f &&
            _ballY > _paddleRightY - paddleHeight/2 && _ballY < _paddleRightY + paddleHeight/2)
        {
            _ballX = 0.85f - paddleWidth; // Move ball to paddle edge
            _ballVX = -Math.Abs(_ballVX) * (1.0f + energyBounce); // Audio-reactive bounce
            _ballVY += (_ballY - _paddleRightY) * (0.3f + bassSpin); // Audio-reactive spin
        }
        
        // FIXED: Enhanced audio-reactive beat effects
        if (beat)
        {
            // Beat affects ball speed and direction
            _ballVX *= 1.05f + energy * 0.1f;
            _ballVY *= 1.05f + bass * 0.1f;
            
            // Beat affects paddle size temporarily
            paddleHeight *= 1.2f + mid * 0.3f;
            
            // Cap maximum speed to prevent hyperspeed
            float maxSpeed = 0.15f + energy * 0.05f;
            if (Math.Abs(_ballVX) > maxSpeed) _ballVX = maxSpeed * Math.Sign(_ballVX);
            if (Math.Abs(_ballVY) > maxSpeed) _ballVY = maxSpeed * Math.Sign(_ballVY);
        }
        
        // FIXED: Audio-reactive visual elements
        var bassColor = bass * 0.5f;
        var trebleColor = treble * 0.3f;
        var energyColor = energy * 0.4f;
        
        // Audio-reactive colors
        uint color = beat ? 0xFFFF00FF : 0xFF00FFFF; // Base color
        if (bass > 0.3f) color = 0xFFFF4400; // Orange for bass
        if (treble > 0.4f) color = 0xFF00FFFF; // Cyan for treble
        if (energy > 0.5f) color = 0xFFFFFF00; // Yellow for energy

        // Audio-reactive paddle sizes
        var paddleSizeMultiplier = 1.0f + bass * 0.3f + energy * 0.2f;
        var visualPaddleWidth = 6 * paddleSizeMultiplier;
        var visualPaddleHeight = 0.3f * paddleSizeMultiplier;

        // Draw left paddle as a solid rectangle
        float leftPaddleX = (-0.9f + 1.0f) * _width * 0.5f;
        float leftPaddleTop = (_paddleLeftY - visualPaddleHeight + 1.0f) * _height * 0.5f;
        float leftPaddleBottom = (_paddleLeftY + visualPaddleHeight + 1.0f) * _height * 0.5f;
        canvas.FillRect(leftPaddleX - visualPaddleWidth/2, leftPaddleTop, visualPaddleWidth, leftPaddleBottom - leftPaddleTop, color);

        // Draw right paddle as a solid rectangle
        float rightPaddleX = (0.9f + 1.0f) * _width * 0.5f;
        float rightPaddleTop = (_paddleRightY - visualPaddleHeight + 1.0f) * _height * 0.5f;
        float rightPaddleBottom = (_paddleRightY + visualPaddleHeight + 1.0f) * _height * 0.5f;
        canvas.FillRect(rightPaddleX - visualPaddleWidth/2, rightPaddleTop, visualPaddleWidth, rightPaddleBottom - rightPaddleTop, color);

        // Audio-reactive ball size and glow
        var ballSizeMultiplier = 1.0f + energy * 0.5f + bass * 0.3f;
        float ballScreenX = (_ballX + 1.0f) * _width * 0.5f;
        float ballScreenY = (_ballY + 1.0f) * _height * 0.5f;
        float ballRadius = Math.Min(_width, _height) * 0.03f * ballSizeMultiplier;
        
        // Draw ball glow on beat
        if (beat)
        {
            var glowColor = (color & 0x00FFFFFF) | 0x40000000; // Semi-transparent glow
            canvas.FillCircle(ballScreenX, ballScreenY, ballRadius * 1.5f, glowColor);
        }
        
        canvas.FillCircle(ballScreenX, ballScreenY, ballRadius, color);
        
        // Draw score or time
        uint textColor = beat ? 0xFFFFFF00 : 0xFF00FF00;
        canvas.DrawText($"Time: {_time:F1}s", 10, 30, textColor, 14.0f);
        canvas.DrawText($"Speed: {Math.Sqrt(_ballVX * _ballVX + _ballVY * _ballVY):F3}", 10, 50, textColor, 14.0f);
        canvas.DrawText($"Ball: ({_ballX:F2}, {_ballY:F2})", 10, 70, textColor, 14.0f);
        canvas.DrawText($"Paddles: L({_paddleLeftY:F2}) R({_paddleRightY:F2})", 10, 90, textColor, 14.0f);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
