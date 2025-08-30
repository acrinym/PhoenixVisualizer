using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// BIG Cat Face Emoji Visualizer - Large, expressive cat face that responds to audio
/// </summary>
public sealed class CatFaceSuperscope : IVisualizerPlugin
{
    public string Id => "cat_face_superscope";
    public string DisplayName => "🐱 BIG Cat Face";

    private int _width;
    private int _height;
    private float _time;

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
        
        // Update time with proper frame rate
        _time += 0.016f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        float energy = features.Energy;
        float bass = features.Bass;
        float mid = features.Mid;
        float treble = features.Treble;
        
        // MUCH BIGGER cat face - emoji style
        float faceScale = 0.7f + energy * 0.3f; // Much larger base size
        float earTwitch = beat ? 0.15f : 0.05f; // More dramatic ear movement
        float breathingIntensity = 1f + (bass + mid) * 0.2f; // More pronounced breathing
        float eyeGlow = treble * 0.8f; // Bigger eye effects
        float whiskerWave = mid * 0.5f; // More dramatic whisker movement
        
        // Calculate center position for the big cat face
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;
        
        // Create the main cat face outline - much bigger
        var facePoints = new System.Collections.Generic.List<(float x, float y)>();
        
        // Draw a large, round cat face (emoji style)
        int faceSegments = 64;
        for (int i = 0; i < faceSegments; i++)
        {
            float t = i / (float)faceSegments;
            float angle = t * (float)Math.PI * 2;
            
            // Large round face with audio-reactive breathing
            float breathingScale = 1f + 0.08f * (float)Math.Sin(_time * 2) * breathingIntensity;
            float radius = faceScale * breathingScale * Math.Min(_width, _height) * 0.3f; // Much bigger
            
            float x = centerX + (float)Math.Cos(angle) * radius;
            float y = centerY + (float)Math.Sin(angle) * radius * 0.9f; // Slightly oval
            
            facePoints.Add((x, y));
        }
        
        // Draw the big cat face outline
        uint faceColor = beat ? 0xFFFF8844 : 0xFFFFAA66; // Warm orange
        canvas.SetLineWidth(4.0f); // Thicker lines
        canvas.DrawLines(facePoints.ToArray(), 4.0f, faceColor);
        
        // Close the face outline
        if (facePoints.Count > 1)
        {
            canvas.DrawLine(facePoints[^1].x, facePoints[^1].y, facePoints[0].x, facePoints[0].y, faceColor, 4.0f);
        }
        
        // BIG triangular cat ears (emoji style)
        float earSize = faceScale * Math.Min(_width, _height) * 0.15f; // Much bigger ears
        
        // Left ear
        var leftEarPoints = new (float x, float y)[]
        {
            (centerX - earSize * 0.8f, centerY - earSize * 1.2f), // Base left
            (centerX - earSize * 0.3f, centerY - earSize * 1.2f), // Base right
            (centerX - earSize * 0.55f, centerY - earSize * 2.0f)  // Tip
        };
        
        // Add ear twitching animation
        if (beat)
        {
            leftEarPoints[2].y += (float)Math.Sin(_time * 10) * earSize * 0.1f;
        }
        
        uint earColor = beat ? 0xFFFF9966 : 0xFFFFAA77;
        DrawTriangle(canvas, leftEarPoints[0], leftEarPoints[1], leftEarPoints[2], earColor);
        
        // Right ear
        var rightEarPoints = new (float x, float y)[]
        {
            (centerX + earSize * 0.3f, centerY - earSize * 1.2f),  // Base left
            (centerX + earSize * 0.8f, centerY - earSize * 1.2f),  // Base right
            (centerX + earSize * 0.55f, centerY - earSize * 2.0f)   // Tip
        };
        
        // Add ear twitching animation
        if (beat)
        {
            rightEarPoints[2].y += (float)Math.Sin(_time * 10 + 1f) * earSize * 0.1f;
        }
        
        DrawTriangle(canvas, rightEarPoints[0], rightEarPoints[1], rightEarPoints[2], earColor);
        
        // BIG expressive cat eyes (emoji style)
        float eyeSize = faceScale * Math.Min(_width, _height) * 0.08f; // Much bigger eyes
        uint eyeColor = beat ? 0xFFFF8800 : 0xFFFFAA00; // Bright orange
        uint pupilColor = 0xFF000000; // Black pupils
        
        // Left eye - much bigger
        float leftEyeX = centerX - earSize * 0.4f;
        float leftEyeY = centerY - earSize * 0.2f;
        canvas.FillCircle(leftEyeX, leftEyeY, eyeSize, eyeColor);
        canvas.FillCircle(leftEyeX, leftEyeY, eyeSize * 0.6f, pupilColor);
        
        // Right eye - much bigger
        float rightEyeX = centerX + earSize * 0.4f;
        float rightEyeY = centerY - earSize * 0.2f;
        canvas.FillCircle(rightEyeX, rightEyeY, eyeSize, eyeColor);
        canvas.FillCircle(rightEyeX, rightEyeY, eyeSize * 0.6f, pupilColor);
        
        // BIG eye shine effects
        uint shineColor = 0xFFFFFFFF;
        float shineSize = eyeSize * 0.4f + eyeGlow * eyeSize * 0.5f;
        canvas.FillCircle(leftEyeX - eyeSize * 0.2f, leftEyeY - eyeSize * 0.2f, shineSize, shineColor);
        canvas.FillCircle(rightEyeX - eyeSize * 0.2f, rightEyeY - eyeSize * 0.2f, shineSize, shineColor);
        
        // BIG nose (emoji style)
        float noseSize = faceScale * Math.Min(_width, _height) * 0.04f + bass * faceScale * Math.Min(_width, _height) * 0.02f;
        uint noseColor = beat ? 0xFFFF1493 : 0xFFFF69B4; // Bright pink
        canvas.FillCircle(centerX, centerY + earSize * 0.3f, noseSize, noseColor);
        
        // BIG whiskers (emoji style)
        uint whiskerColor = beat ? 0xFFFFFFFF : 0xFFCCCCCC;
        float whiskerLength = faceScale * Math.Min(_width, _height) * 0.25f + whiskerWave * faceScale * Math.Min(_width, _height) * 0.1f;
        float whiskerThickness = 3.0f; // Thicker whiskers
        
        // Left whiskers - much longer
        for (int i = 0; i < 3; i++)
        {
            float yOffset = (i - 1) * earSize * 0.3f;
            float waveOffset = (float)Math.Sin(_time * 3 + i * 0.5f) * whiskerWave * earSize * 0.2f;
            canvas.DrawLine(centerX - earSize * 0.8f, centerY + yOffset,
                          centerX - earSize * 0.8f - whiskerLength, centerY + yOffset + waveOffset, whiskerColor, whiskerThickness);
        }
        
        // Right whiskers - much longer
        for (int i = 0; i < 3; i++)
        {
            float yOffset = (i - 1) * earSize * 0.3f;
            float waveOffset = (float)Math.Sin(_time * 3 + i * 0.5f) * whiskerWave * earSize * 0.2f;
            canvas.DrawLine(centerX + earSize * 0.8f, centerY + yOffset,
                          centerX + earSize * 0.8f + whiskerLength, centerY + yOffset + waveOffset, whiskerColor, whiskerThickness);
        }
        
        // BIG mouth (emoji style)
        float mouthY = centerY + earSize * 0.6f;
        float mouthWidth = faceScale * Math.Min(_width, _height) * 0.08f + volume * faceScale * Math.Min(_width, _height) * 0.1f;
        uint mouthColor = beat ? 0xFFFF0000 : 0xFF333333;
        canvas.DrawLine(centerX - mouthWidth, mouthY, centerX + mouthWidth, mouthY, mouthColor, 4.0f);
        
        // Add BIG audio-reactive particle effects around the cat
        if (beat || energy > 0.3f)
        {
            DrawBigAudioParticles(canvas, features, beat, energy, centerX, centerY, faceScale);
        }
        
        // Draw audio info
        uint infoColor = beat ? 0xFFFFFF00 : 0xFF00FF00;
        canvas.DrawText($"Energy: {energy:F2}", 10, 30, infoColor, 16.0f);
        canvas.DrawText($"Bass: {bass:F2}", 10, 50, infoColor, 16.0f);
        canvas.DrawText($"Beat: {beat}", 10, 70, infoColor, 16.0f);
        canvas.DrawText($"Cat Size: {faceScale:F2}", 10, 90, infoColor, 16.0f);
    }
    
    private void DrawBigAudioParticles(ISkiaCanvas canvas, AudioFeatures features, bool beat, float energy, float centerX, float centerY, float faceScale)
    {
        // Draw BIG sparkles around the cat when there's audio activity
        int particleCount = (int)(energy * 30) + (beat ? 20 : 0);
        float particleRadius = faceScale * Math.Min(canvas.Width, canvas.Height) * 0.4f;
        
        for (int i = 0; i < particleCount; i++)
        {
            float angle = (float)(i * Math.PI * 2 / particleCount + _time * 2);
            float radius = particleRadius + energy * particleRadius * 0.5f;
            float x = centerX + (float)Math.Cos(angle) * radius;
            float y = centerY + (float)Math.Sin(angle) * radius;
            
            if (x >= 0 && x < canvas.Width && y >= 0 && y < canvas.Height)
            {
                uint particleColor = beat ? 0xFFFFD700 : 0xFFFFA500; // Gold on beat, orange otherwise
                float particleSize = 4f + energy * 6f; // Much bigger particles
                canvas.FillCircle(x, y, particleSize, particleColor);
            }
        }
    }
    
    private void DrawTriangle(ISkiaCanvas canvas, (float x, float y) p1, (float x, float y) p2, (float x, float y) p3, uint color)
    {
        // Draw triangle by connecting three points
        canvas.DrawLine(p1.x, p1.y, p2.x, p2.y, color, 4.0f);
        canvas.DrawLine(p2.x, p2.y, p3.x, p3.y, color, 4.0f);
        canvas.DrawLine(p3.x, p3.y, p1.x, p1.y, color, 4.0f);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
