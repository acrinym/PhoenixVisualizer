using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Flappy Bird Visualizer - Audio-reactive bird game with procedurally generated pipes
/// Features multiple birds, collision detection, particle effects, and various skins
/// </summary>
public sealed class FlappyBirdVisualizer : IVisualizerPlugin
{
    public string Id => "flappy_bird";
    public string DisplayName => "🐤 Flappy Beats";

    private int _width, _height;
    private float _time;
    private readonly Random _random = new();

    // Game constants
    private const float GRAVITY = 800f;
    private const float FLAP_FORCE = -400f;
    private const float PIPE_WIDTH = 80f;
    private const float PIPE_GAP = 180f;
    private const float SCROLL_SPEED = 200f;
    private const float BIRD_SIZE = 30f;

    // Game state
    private List<Bird> _birds = new();
    private List<Pipe> _pipes = new();
    private List<Particle> _particles = new();
    private float _pipeSpawnTimer;
    private float _lastBeatTime;
    private int _score;
    private float _cameraShake;

    // Audio-reactive parameters
    private float _bassAccumulator;
    private float _trebleAccumulator;

    // Visual settings
    private enum BirdSkin { Classic, Phoenix, NyanCat, Meme }
    private BirdSkin _currentSkin;
    private int _maxBirds = 5;
    private float _pipeGapSize = PIPE_GAP;
    private float _scrollMultiplier = 1.0f;
    private bool _splatMode = true; // Cartoon vs subtle splats

    // Colors
    private readonly uint[] _pipeColors = new uint[]
    {
        0xFF00AA00, // Green pipes
        0xFF008800, // Dark green
        0xFF00DD00, // Bright green
        0xFF006600, // Very dark green
    };

    private readonly uint[] _birdColors = new uint[]
    {
        0xFFFFFF00, // Yellow (classic)
        0xFFFF4400, // Orange (phoenix)
        0xFFFF0080, // Pink (nyan cat)
        0xFF888888, // Gray (meme)
    };

    private struct Bird
    {
        public float X, Y;
        public float VelocityY;
        public BirdSkin Skin;
        public uint Color;
        public float AnimationTime;
        public bool Active;
        public float FlapCooldown;
    }

    private struct Pipe
    {
        public float X;
        public float GapY;
        public float GapSize;
        public uint Color;
        public bool Passed;
    }

    private struct Particle
    {
        public float X, Y;
        public float VelocityX, VelocityY;
        public uint Color;
        public float Life;
        public float MaxLife;
        public float Size;
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        _score = 0;
        _currentSkin = BirdSkin.Classic;

        // Initialize birds
        _birds = new List<Bird>();
        for (int i = 0; i < _maxBirds; i++)
        {
            SpawnBird();
        }

        // Initialize pipes
        _pipes = new List<Pipe>();
        _pipeSpawnTimer = 0;

        // Initialize particles
        _particles = new List<Particle>();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        _birds.Clear();
        _pipes.Clear();
        _particles.Clear();
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Update audio reactivity
        UpdateAudioReactivity(f);

        // Update game logic
        UpdateBirds(f);
        UpdatePipes(f);
        UpdateParticles(f);

        // Spawn new pipes
        _pipeSpawnTimer -= 0.016f * _scrollMultiplier;
        if (_pipeSpawnTimer <= 0)
        {
            SpawnPipe();
            _pipeSpawnTimer = 2.0f + _random.NextSingle() * 1.5f; // 2-3.5 seconds
        }

        // Render scene
        RenderBackground(canvas, f);
        RenderPipes(canvas, f);
        RenderParticles(canvas, f);
        RenderBirds(canvas, f);
        RenderUI(canvas, f);

        // Apply camera shake
        if (_cameraShake > 0)
        {
            _cameraShake *= 0.9f;
        }
    }

    private void UpdateAudioReactivity(AudioFeatures f)
    {
        // Accumulate audio data
        _bassAccumulator = _bassAccumulator * 0.8f + f.Bass * 0.2f;
        _trebleAccumulator = _trebleAccumulator * 0.8f + f.Treble * 0.2f;

        // Trigger flaps on bass hits
        if (f.Beat && _time - _lastBeatTime > 0.15f)
        {
            _lastBeatTime = _time;
            TriggerBirdFlaps(f.Bass);

            // Camera shake on beat
            _cameraShake = 5f;
        }

        // Spawn birds on big bass drops
        if (_bassAccumulator > 0.7f && _birds.Count < _maxBirds)
        {
            SpawnBird();
            _bassAccumulator = 0; // Reset to prevent spam
        }
    }

    private void TriggerBirdFlaps(float intensity)
    {
        for (int i = 0; i < _birds.Count; i++)
        {
            var bird = _birds[i];
            if (bird.Active && bird.FlapCooldown <= 0)
            {
                // Flap with audio-reactive force
                bird.VelocityY = FLAP_FORCE * (0.5f + intensity * 0.5f);
                bird.FlapCooldown = 0.1f; // Prevent spam flapping

                // Create flap particles
                CreateFlapParticles(bird.X, bird.Y, bird.Color, intensity);

                _birds[i] = bird; // Update the bird in the list
            }
        }
    }

    private void UpdateBirds(AudioFeatures f)
    {
        for (int i = _birds.Count - 1; i >= 0; i--)
        {
            var bird = _birds[i];

            if (!bird.Active) continue;

            // Apply gravity
            bird.VelocityY += GRAVITY * 0.016f;

            // Update position
            bird.Y += bird.VelocityY * 0.016f;

            // Update animation
            bird.AnimationTime += 0.016f * (1f + f.Treble * 2f);

            // Update flap cooldown
            if (bird.FlapCooldown > 0)
            {
                bird.FlapCooldown -= 0.016f;
            }

            // Check collisions
            if (CheckBirdCollisions(bird))
            {
                // Bird hit something!
                CreateSplatEffect(bird.X, bird.Y, bird.Color, bird.VelocityY);
                bird.Active = false;
                _cameraShake = 10f;
                _birds[i] = bird;
                continue;
            }

            // Remove birds that fall off screen
            if (bird.Y > _height + 100)
            {
                _birds.RemoveAt(i);
                continue;
            }

            _birds[i] = bird;
        }
    }

    private void UpdatePipes(AudioFeatures f)
    {
        // Move pipes left
        float moveAmount = SCROLL_SPEED * _scrollMultiplier * 0.016f;

        for (int i = _pipes.Count - 1; i >= 0; i--)
        {
            var pipe = _pipes[i];

            // Move pipe
            pipe.X -= moveAmount;

            // Audio-reactive pipe movement
            pipe.GapY += (float)Math.Sin(_time * 2f + i) * f.Mid * 50f * 0.016f;

            // Keep gap within reasonable bounds
            pipe.GapY = Math.Max(100, Math.Min(_height - 100, pipe.GapY));

            // Remove pipes that are off screen
            if (pipe.X < -PIPE_WIDTH)
            {
                _pipes.RemoveAt(i);
                continue;
            }

            // Check for scoring
            if (!pipe.Passed && pipe.X + PIPE_WIDTH < _width * 0.3f)
            {
                pipe.Passed = true;
                _score++;
            }

            _pipes[i] = pipe;
        }
    }

    private void UpdateParticles(AudioFeatures f)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];

            // Apply gravity to particles
            particle.VelocityY += GRAVITY * 0.3f * 0.016f;

            // Update position
            particle.X += particle.VelocityX * 0.016f;
            particle.Y += particle.VelocityY * 0.016f;

            // Update life
            particle.Life -= 0.016f;

            // Remove dead particles
            if (particle.Life <= 0)
            {
                _particles.RemoveAt(i);
                continue;
            }

            _particles[i] = particle;
        }
    }

    private void SpawnBird()
    {
        var bird = new Bird
        {
            X = _width * 0.3f,
            Y = _height * 0.5f,
            VelocityY = 0,
            Skin = _currentSkin,
            Color = _birdColors[(int)_currentSkin],
            AnimationTime = 0,
            Active = true,
            FlapCooldown = 0
        };

        _birds.Add(bird);
    }

    private void SpawnPipe()
    {
        // Procedurally generate pipe gap position
        float gapY = _height * 0.3f + _random.NextSingle() * _height * 0.4f;

        // Audio-reactive gap size
        float gapSize = _pipeGapSize * (0.7f + _trebleAccumulator * 0.6f);

        var pipe = new Pipe
        {
            X = _width + PIPE_WIDTH,
            GapY = gapY,
            GapSize = gapSize,
            Color = _pipeColors[_random.Next(_pipeColors.Length)],
            Passed = false
        };

        _pipes.Add(pipe);
    }

    private bool CheckBirdCollisions(Bird bird)
    {
        // Check pipe collisions
        foreach (var pipe in _pipes)
        {
            if (pipe.X < bird.X + BIRD_SIZE * 0.5f && pipe.X + PIPE_WIDTH > bird.X - BIRD_SIZE * 0.5f)
            {
                // Check if bird is in pipe gap
                if (bird.Y - BIRD_SIZE * 0.5f < pipe.GapY - pipe.GapSize * 0.5f ||
                    bird.Y + BIRD_SIZE * 0.5f > pipe.GapY + pipe.GapSize * 0.5f)
                {
                    return true; // Collision!
                }
            }
        }

        // Check ground/ceiling
        if (bird.Y - BIRD_SIZE * 0.5f < 0 || bird.Y + BIRD_SIZE * 0.5f > _height)
        {
            return true;
        }

        return false;
    }

    private void CreateFlapParticles(float x, float y, uint color, float intensity)
    {
        int particleCount = (int)(5 + intensity * 10);

        for (int i = 0; i < particleCount; i++)
        {
            float angle = _random.NextSingle() * MathF.PI * 2;
            float speed = 100f + _random.NextSingle() * 200f;
            float life = 0.5f + _random.NextSingle() * 1.0f;

            var particle = new Particle
            {
                X = x,
                Y = y,
                VelocityX = (float)Math.Cos(angle) * speed,
                VelocityY = (float)Math.Sin(angle) * speed - 100f, // Slight upward bias
                Color = color,
                Life = life,
                MaxLife = life,
                Size = 2f + _random.NextSingle() * 4f
            };

            _particles.Add(particle);
        }
    }

    private void CreateSplatEffect(float x, float y, uint color, float velocity)
    {
        int particleCount = _splatMode ? 20 : 8;

        for (int i = 0; i < particleCount; i++)
        {
            float angle = _random.NextSingle() * MathF.PI * 2;
            float speed = 200f + _random.NextSingle() * 300f;
            float life = 1.0f + _random.NextSingle() * 2.0f;

            // Rainbow colors for splat mode
            uint splatColor = _splatMode ?
                HsvToRgb(_random.NextSingle(), 1.0f, 1.0f) :
                AdjustBrightness(color, 0.8f);

            var particle = new Particle
            {
                X = x,
                Y = y,
                VelocityX = (float)Math.Cos(angle) * speed,
                VelocityY = (float)Math.Sin(angle) * speed + velocity * 0.5f,
                Color = splatColor,
                Life = life,
                MaxLife = life,
                Size = 3f + _random.NextSingle() * 8f
            };

            _particles.Add(particle);
        }
    }

    private void RenderBackground(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Create animated sky background
        uint skyColor = 0xFF87CEEB; // Sky blue
        uint cloudColor = 0xFFFFFFFF;

        canvas.Clear(skyColor);

        // Add animated clouds
        for (int i = 0; i < 8; i++)
        {
            float cloudX = (i * 200 + _time * 20) % (_width + 200) - 100;
            float cloudY = 50 + i * 40 + (float)Math.Sin(_time * 0.5f + i) * 20;

            // Draw simple cloud
            canvas.FillCircle(cloudX, cloudY, 30, cloudColor);
            canvas.FillCircle(cloudX + 25, cloudY, 35, cloudColor);
            canvas.FillCircle(cloudX + 50, cloudY, 30, cloudColor);
            canvas.FillCircle(cloudX + 25, cloudY - 15, 25, cloudColor);
        }

        // Ground
        uint groundColor = 0xFF228B22; // Forest green
        canvas.FillRect(0, _height - 50, _width, 50, groundColor);

        // Apply camera shake
        if (_cameraShake > 0.1f)
        {
            // Note: Camera shake would require canvas transformation
            // For now, we'll just add some visual feedback
        }
    }

    private void RenderPipes(ISkiaCanvas canvas, AudioFeatures f)
    {
        foreach (var pipe in _pipes)
        {
            // Top pipe
            float topHeight = pipe.GapY - pipe.GapSize * 0.5f;
            if (topHeight > 0)
            {
                canvas.FillRect(pipe.X, 0, PIPE_WIDTH, topHeight, pipe.Color);
                // Add pipe cap
                canvas.FillRect(pipe.X - 5, topHeight - 20, PIPE_WIDTH + 10, 20, pipe.Color);
            }

            // Bottom pipe
            float bottomY = pipe.GapY + pipe.GapSize * 0.5f;
            float bottomHeight = _height - bottomY;
            if (bottomHeight > 0)
            {
                canvas.FillRect(pipe.X, bottomY, PIPE_WIDTH, bottomHeight, pipe.Color);
                // Add pipe cap
                canvas.FillRect(pipe.X - 5, bottomY, PIPE_WIDTH + 10, 20, pipe.Color);
            }

            // Add glowing effect on beat
            if (f.Beat)
            {
                uint glowColor = AdjustBrightness(pipe.Color, 1.5f);
                canvas.FillRect(pipe.X - 2, 0, PIPE_WIDTH + 4, topHeight, glowColor);
                canvas.FillRect(pipe.X - 2, bottomY, PIPE_WIDTH + 4, bottomHeight, glowColor);
            }
        }
    }

    private void RenderBirds(ISkiaCanvas canvas, AudioFeatures f)
    {
        foreach (var bird in _birds)
        {
            if (!bird.Active) continue;

            // Calculate bird animation
            float flapOffset = (float)Math.Sin(bird.AnimationTime * 10f) * 5f;
            float wingAngle = (float)Math.Sin(bird.AnimationTime * 15f) * 0.3f;

            // Draw bird body
            canvas.FillCircle(bird.X, bird.Y + flapOffset, BIRD_SIZE * 0.4f, bird.Color);

            // Draw wings
            float wingY = bird.Y + flapOffset - 5;
            canvas.FillRect(bird.X - BIRD_SIZE * 0.6f, wingY, BIRD_SIZE * 0.3f, 8, bird.Color);
            canvas.FillRect(bird.X + BIRD_SIZE * 0.3f, wingY, BIRD_SIZE * 0.3f, 8, bird.Color);

            // Draw beak
            uint beakColor = 0xFFFFA500; // Orange
            canvas.FillRect(bird.X + BIRD_SIZE * 0.3f, bird.Y + flapOffset, 8, 4, beakColor);

            // Add special effects based on skin
            switch (bird.Skin)
            {
                case BirdSkin.Phoenix:
                    // Add flame trail
                    CreateFlapParticles(bird.X - 10, bird.Y, 0xFFFF4400, 0.3f);
                    break;
                case BirdSkin.NyanCat:
                    // Add rainbow trail
                    for (int i = 0; i < 3; i++)
                    {
                        var rainbowParticle = new Particle
                        {
                            X = bird.X - i * 5,
                            Y = bird.Y,
                            VelocityX = -50f,
                            VelocityY = 0,
                            Color = HsvToRgb((float)i / 3f, 1.0f, 1.0f),
                            Life = 0.5f,
                            MaxLife = 0.5f,
                            Size = 3f
                        };
                        _particles.Add(rainbowParticle);
                    }
                    break;
            }
        }
    }

    private void RenderParticles(ISkiaCanvas canvas, AudioFeatures f)
    {
        foreach (var particle in _particles)
        {
            float alpha = particle.Life / particle.MaxLife;
            uint fadedColor = (uint)((uint)(alpha * 255) << 24 | (particle.Color & 0x00FFFFFF));

            canvas.FillCircle(particle.X, particle.Y, particle.Size, fadedColor);
        }
    }

    private void RenderUI(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Render score
        string scoreText = $"Score: {_score}";
        canvas.DrawText(scoreText, 20, 40, 0xFFFFFFFF, 24f);

        // Render bird count
        int activeBirds = _birds.Count(bird => bird.Active);
        string birdText = $"Birds: {activeBirds}";
        canvas.DrawText(birdText, 20, 70, 0xFFFFFFFF, 18f);

        // Render audio levels
        string bassText = $"Bass: {_bassAccumulator:F2}";
        canvas.DrawText(bassText, _width - 150, 40, 0xFFFF0000, 16f);

        string trebleText = $"Treble: {_trebleAccumulator:F2}";
        canvas.DrawText(trebleText, _width - 150, 60, 0xFF0000FF, 16f);
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
        float c = v * s;
        float x = c * (1 - MathF.Abs((h * 6) % 2 - 1));
        float m = v - c;

        float r, g, b;
        if (h < 1f/6f) { r = c; g = x; b = 0; }
        else if (h < 2f/6f) { r = x; g = c; b = 0; }
        else if (h < 3f/6f) { r = 0; g = c; b = x; }
        else if (h < 4f/6f) { r = 0; g = x; b = c; }
        else if (h < 5f/6f) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        byte R = (byte)((r + m) * 255);
        byte G = (byte)((g + m) * 255);
        byte B = (byte)((b + m) * 255);

        return 0xFF000000u | ((uint)R << 16) | ((uint)G << 8) | (uint)B;
    }
}
