using System;
using System.Numerics;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.VFX.Effects
{
    /// <summary>
    /// Dynamic starfield VFX effect with audio-reactive star generation
    /// </summary>
    [PhoenixVFX(
        Id = "starfield",
        Name = "Starfield",
        Category = "Space",
        Version = "1.0.0",
        Author = "Phoenix Team",
        Description = "Dynamic starfield with audio-reactive star generation and movement"
    )]
    public class StarfieldVFX : BasePhoenixVFX
    {
        #region Parameters

        [VFXParameter(
            Id = "star_count",
            Name = "Star Count",
            Description = "Maximum number of stars in the field",
            MinValue = 50,
            MaxValue = 5000,
            DefaultValue = 500
        )]
        public int StarCount { get; set; } = 500;

        [VFXParameter(
            Id = "star_speed",
            Name = "Star Speed",
            Description = "Base speed of star movement",
            MinValue = 0.1f,
            MaxValue = 10.0f,
            DefaultValue = 1.0f
        )]
        public float StarSpeed { get; set; } = 1.0f;

        [VFXParameter(
            Id = "audio_reactivity",
            Name = "Audio Reactivity",
            Description = "How much audio affects star behavior",
            MinValue = 0.0f,
            MaxValue = 2.0f,
            DefaultValue = 1.0f
        )]
        public float AudioReactivity { get; set; } = 1.0f;

        [VFXParameter(
            Id = "star_size_range",
            Name = "Star Size Range",
            Description = "Range of star sizes (min to max)",
            MinValue = 0.1f,
            MaxValue = 5.0f,
            DefaultValue = 1.0f
        )]
        public float StarSizeRange { get; set; } = 1.0f;

        [VFXParameter(
            Id = "twinkle_speed",
            Name = "Twinkle Speed",
            Description = "Speed of star twinkling effect",
            MinValue = 0.1f,
            MaxValue = 5.0f,
            DefaultValue = 1.0f
        )]
        public float TwinkleSpeed { get; set; } = 1.0f;

        [VFXParameter(
            Id = "color_temperature",
            Name = "Color Temperature",
            Description = "Base color temperature of stars (0=cool blue, 1=warm yellow)",
            MinValue = 0.0f,
            MaxValue = 1.0f,
            DefaultValue = 0.5f
        )]
        public float ColorTemperature { get; set; } = 0.5f;

        [VFXParameter(
            Id = "depth_layers",
            Name = "Depth Layers",
            Description = "Number of depth layers for parallax effect",
            MinValue = 1,
            MaxValue = 10,
            DefaultValue = 3
        )]
        public int DepthLayers { get; set; } = 3;

        #endregion

        #region Private Fields

        private Star[] _stars;
        private Random _random;
        private float _time;
        private int _activeStarCount;

        #endregion

        #region Star Structure

        private struct Star
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Brightness;
            public float TwinklePhase;
            public float TwinkleSpeed;
            public float Hue;
            public float Saturation;
            public float Value;
            public int DepthLayer;
            public bool IsActive;
        }

        #endregion

        #region Initialization

        protected override void OnInitialize(VFXRenderContext context, AudioFeatures audio)
        {
            base.OnInitialize(context, audio);
            
            _random = new Random();
            _time = 0.0f;
            _activeStarCount = 0;
            
            InitializeStars();
        }

        private void InitializeStars()
        {
            _stars = new Star[StarCount];
            
            // Initialize all stars as inactive
            for (int i = 0; i < StarCount; i++)
            {
                _stars[i] = new Star { IsActive = false };
            }
        }

        private void SpawnStar()
        {
            if (_activeStarCount >= StarCount) return;
            
            // Find inactive star
            for (int i = 0; i < _stars.Length; i++)
            {
                if (!_stars[i].IsActive)
                {
                    _stars[i] = CreateRandomStar();
                    _stars[i].IsActive = true;
                    _activeStarCount++;
                    break;
                }
            }
        }

        private Star CreateRandomStar()
        {
            var depthLayer = _random.Next(DepthLayers);
            var depthFactor = 1.0f - (float)depthLayer / DepthLayers;
            
            return new Star
            {
                Position = new Vector2(
                    (float)(_random.NextDouble() * _context.Width),
                    (float)(_random.NextDouble() * _context.Height)
                ),
                Velocity = new Vector2(
                    (float)(_random.NextDouble() - 0.5) * StarSpeed * depthFactor,
                    (float)(_random.NextDouble() - 0.5) * StarSpeed * depthFactor
                ),
                Size = (float)(_random.NextDouble() * StarSizeRange + 0.5f) * depthFactor,
                Brightness = (float)(_random.NextDouble() * 0.5f + 0.5f),
                TwinklePhase = (float)(_random.NextDouble() * Math.PI * 2.0),
                TwinkleSpeed = TwinkleSpeed * (float)(_random.NextDouble() * 0.5f + 0.75f),
                Hue = GetStarColor(depthLayer),
                Saturation = (float)(_random.NextDouble() * 0.2f + 0.8f),
                Value = (float)(_random.NextDouble() * 0.3f + 0.7f),
                DepthLayer = depthLayer,
                IsActive = true
            };
        }

        private float GetStarColor(int depthLayer)
        {
            // Cooler colors for distant stars, warmer for close ones
            var baseHue = ColorTemperature * 60.0f; // 0=blue, 60=yellow
            var depthVariation = (float)depthLayer / DepthLayers * 40.0f;
            
            return baseHue + depthVariation;
        }

        #endregion

        #region Frame Processing

        protected override void OnProcessFrame(VFXRenderContext context)
        {
            _time += context.DeltaTime;
            
            // Spawn new stars based on audio
            UpdateStarSpawning(context);
            
            // Update all active stars
            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i].IsActive)
                {
                    UpdateStar(ref _stars[i], context);
                }
            }
        }

        private void UpdateStarSpawning(VFXRenderContext context)
        {
            // Spawn rate based on audio activity
            var audioInfluence = AudioReactivity * 0.1f;
            var beatInfluence = (_audio?.BeatIntensity ?? 0.0f) * audioInfluence;
            var rmsInfluence = (float)(_audio?.RMS ?? 0.0) * audioInfluence;
            
            var spawnChance = (beatInfluence + rmsInfluence) * context.DeltaTime;
            
            if (_random.NextDouble() < spawnChance)
            {
                SpawnStar();
            }
        }

        private void UpdateStar(ref Star star, VFXRenderContext context)
        {
            // Update twinkling
            star.TwinklePhase += star.TwinkleSpeed * context.DeltaTime;
            var twinkleFactor = 0.5f + 0.5f * (float)Math.Sin(star.TwinklePhase);
            
            // Calculate audio influence
            var audioInfluence = AudioReactivity * 0.5f;
            var beatInfluence = (_audio?.BeatIntensity ?? 0.0f) * audioInfluence;
            var bassInfluence = (float)(_audio?.Bass ?? 0.0) * audioInfluence;
            var rmsInfluence = (float)(_audio?.RMS ?? 0.0) * audioInfluence;
            
            // Update brightness based on audio
            star.Brightness = 0.5f + 0.5f * twinkleFactor + beatInfluence * 0.3f;
            
            // Update size based on bass
            var sizeVariation = 1.0f + bassInfluence * 0.5f;
            var currentSize = star.Size * sizeVariation;
            
            // Update position
            var depthFactor = 1.0f - (float)star.DepthLayer / DepthLayers;
            var movementSpeed = StarSpeed * depthFactor * (1.0f + beatInfluence);
            
            star.Position += star.Velocity * movementSpeed * context.DeltaTime;
            
            // Wrap around screen edges
            if (star.Position.X < -currentSize) star.Position.X = _context.Width + currentSize;
            if (star.Position.X > _context.Width + currentSize) star.Position.X = -currentSize;
            if (star.Position.Y < -currentSize) star.Position.Y = _context.Height + currentSize;
            if (star.Position.Y > _context.Height + currentSize) star.Position.Y = -currentSize;
            
            // Update color based on audio
            star.Hue = GetStarColor(star.DepthLayer) + beatInfluence * 20.0f;
            star.Value = 0.7f + rmsInfluence * 0.3f;
            
            // Randomly deactivate star for variety
            if (_random.NextDouble() < 0.001f) // 0.1% chance per frame
            {
                star.IsActive = false;
                _activeStarCount--;
            }
        }

        #endregion

        #region GPU Processing Support

        protected override bool SupportsGPUProcessing() => false; // CPU-only for now

        #endregion
    }
}