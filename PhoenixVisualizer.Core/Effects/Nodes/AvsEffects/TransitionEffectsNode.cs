using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    ///     Implements Winamp AVS style transition effects.  The node keeps track of
    ///     the previous frame and morphs into the current frame using the selected
    ///     <see cref="TransitionEffectType"/> over a configurable duration.
    /// </summary>
    public class TransitionEffectsNode : BaseEffectNode
    {
        #region Effect Types

        /// <summary>
        ///     All supported transition styles.  Many map directly to the classic
        ///     AVS transition presets.  Only a subset currently have bespoke
        ///     implementations – other styles gracefully fall back to a simple
        ///     cross‑fade.
        /// </summary>
        public enum TransitionEffectType
        {
            None = 0,
            SlightFuzzify = 1,
            ShiftRotateLeft = 2,
            BigSwirlOut = 3,
            MediumSwirl = 4,
            Sunburster = 5,
            SwirlToCenter = 6,
            BlockyPartialOut = 7,
            SwirlingAroundBothWays = 8,
            BubblingOutward = 9,
            BubblingOutwardWithSwirl = 10,
            FivePointedDistortion = 11,
            Tunneling = 12,
            Bleeding = 13,
            ShiftedBigSwirlOut = 14,
            PsychoticBeamingOutward = 15,
            CosineRadial3Way = 16,
            SpinnyTube = 17,
            RadialSwirlies = 18,
            Swill = 19,
            Gridley = 20,
            Grapevine = 21,
            Quadrant = 22,
            SixWayKaleidoscope = 23,
            Custom = 32767
        }

        #endregion

        #region Public Properties

        /// <summary>Selected transition style.</summary>
        public TransitionEffectType Effect { get; set; } = TransitionEffectType.None;

        /// <summary>Blending mode (0 = replace, 1 = additive, 2 = 50/50).</summary>
        public int BlendMode { get; set; } = 0;

        /// <summary>Duration of the transition in seconds.</summary>
        public float Duration { get; set; } = 1.0f;

        /// <summary>Intensity multiplier used by style specific calculations.</summary>
        public float Intensity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private ImageBuffer? _previousFrame;
        private ImageBuffer? _targetFrame;
        private float _elapsed;
        private bool _transitioning;

        #endregion

        #region Port Initialisation

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null,
                "Current frame image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null,
                "Transitioned output image"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer current)
            {
                return GetDefaultOutput();
            }

            if (_previousFrame == null)
            {
                _previousFrame = CloneBuffer(current);
                return current;
            }

            if (!_transitioning)
            {
                _targetFrame = CloneBuffer(current);
                _elapsed = 0f;
                _transitioning = true;
            }
            else
            {
                _targetFrame = CloneBuffer(current);
                _elapsed = 0f;
            }

            if (_targetFrame == null)
                return current;

            _elapsed += 1f / 60f; // assume 60fps timeline
            var progress = Math.Min(1f, _elapsed / Math.Max(0.001f, Duration));

            var output = new ImageBuffer(current.Width, current.Height);
            ApplyEffect(_previousFrame, _targetFrame, output, progress);

            if (progress >= 1f)
            {
                _previousFrame = _targetFrame;
                _transitioning = false;
            }

            return output;
        }

        private void ApplyEffect(ImageBuffer from, ImageBuffer to, ImageBuffer output, float progress)
        {
            switch (Effect)
            {
                case TransitionEffectType.SwirlToCenter:
                    ApplySwirl(from, to, output, progress);
                    break;
                case TransitionEffectType.Tunneling:
                    ApplyTunnel(from, to, output, progress);
                    break;
                default:
                    ApplyCrossfade(from, to, output, progress);
                    break;
            }
        }

        private void ApplyCrossfade(ImageBuffer from, ImageBuffer to, ImageBuffer output, float progress)
        {
            for (int i = 0; i < from.Pixels.Length && i < to.Pixels.Length; i++)
            {
                output.Pixels[i] = BlendTransition(from.Pixels[i], to.Pixels[i], progress);
            }
        }

        private void ApplySwirl(ImageBuffer from, ImageBuffer to, ImageBuffer output, float progress)
        {
            int width = to.Width;
            int height = to.Height;
            float cx = width / 2f;
            float cy = height / 2f;
            float strength = Intensity * progress * 5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float radius = MathF.Sqrt(dx * dx + dy * dy);
                    float angle = MathF.Atan2(dy, dx) + strength * (radius / MathF.Max(width, height));
                    int sx = (int)(cx + radius * MathF.Cos(angle));
                    int sy = (int)(cy + radius * MathF.Sin(angle));

                    if (sx < 0 || sx >= width || sy < 0 || sy >= height)
                    {
                        sx = x;
                        sy = y;
                    }

                    int fromColor = from.GetPixel(x, y);
                    int toColor = to.GetPixel(sx, sy);
                    output.SetPixel(x, y, BlendTransition(fromColor, toColor, progress));
                }
            }
        }

        private void ApplyTunnel(ImageBuffer from, ImageBuffer to, ImageBuffer output, float progress)
        {
            int width = to.Width;
            int height = to.Height;
            float cx = width / 2f;
            float cy = height / 2f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float factor = 1f / (1f + progress * Intensity);
                    int sx = (int)(cx + dx * factor);
                    int sy = (int)(cy + dy * factor);

                    int fromColor = from.GetPixel(x, y);
                    int toColor = to.GetPixel(sx, sy);
                    output.SetPixel(x, y, BlendTransition(fromColor, toColor, progress));
                }
            }
        }

        private int BlendTransition(int fromColor, int toColor, float progress)
        {
            int r1 = fromColor & 0xFF;
            int g1 = (fromColor >> 8) & 0xFF;
            int b1 = (fromColor >> 16) & 0xFF;

            int r2 = toColor & 0xFF;
            int g2 = (toColor >> 8) & 0xFF;
            int b2 = (toColor >> 16) & 0xFF;

            switch (BlendMode)
            {
                case 1: // additive
                    r1 = Math.Min(255, r1 + (int)(r2 * progress));
                    g1 = Math.Min(255, g1 + (int)(g2 * progress));
                    b1 = Math.Min(255, b1 + (int)(b2 * progress));
                    return (b1 << 16) | (g1 << 8) | r1;
                case 2: // 50/50
                    r1 = (r1 + r2) / 2;
                    g1 = (g1 + g2) / 2;
                    b1 = (b1 + b2) / 2;
                    return (b1 << 16) | (g1 << 8) | r1;
                default: // crossfade
                    int r = (int)(r1 * (1 - progress) + r2 * progress);
                    int g = (int)(g1 * (1 - progress) + g2 * progress);
                    int b = (int)(b1 * (1 - progress) + b2 * progress);
                    return (b << 16) | (g << 8) | r;
            }
        }

        private static ImageBuffer CloneBuffer(ImageBuffer source)
        {
            var clone = new ImageBuffer(source.Width, source.Height);
            Array.Copy(source.Pixels, clone.Pixels, source.Pixels.Length);
            return clone;
        }

        #endregion

        #region Configuration Helpers

        public override bool ValidateConfiguration()
        {
            if (Duration < 0.01f)
                Duration = 0.01f;
            if (Intensity < 0f)
                Intensity = 0f;
            return true;
        }

        public override string GetSettingsSummary()
        {
            return $"Transition: {Effect}, Duration: {Duration:F2}s, Blend: {BlendMode}, Intensity: {Intensity:F2}";
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}

