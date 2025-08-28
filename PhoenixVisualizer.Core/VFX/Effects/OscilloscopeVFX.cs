using System;
using System.Numerics;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.VFX.Effects
{
    /// <summary>
    /// Oscilloscope VFX effect for audio waveform visualization
    /// </summary>
    [PhoenixVFX(
        Id = "oscilloscope",
        Name = "Oscilloscope",
        Category = "Audio",
        Version = "1.0.0",
        Author = "Phoenix Team",
        Description = "Real-time oscilloscope visualization with multiple display modes"
    )]
    public class OscilloscopeVFX : BasePhoenixVFX
    {
        #region Parameters

        [VFXParameter(
            Id = "display_mode",
            Name = "Display Mode",
            Description = "Oscilloscope display mode",
            EnumValues = new[] { "Waveform", "Spectrum", "Lissajous", "Circular" },
            DefaultValue = "Waveform"
        )]
        public string DisplayMode { get; set; } = "Waveform";

        [VFXParameter(
            Id = "line_thickness",
            Name = "Line Thickness",
            Description = "Thickness of the oscilloscope lines",
            MinValue = 1.0f,
            MaxValue = 10.0f,
            DefaultValue = 2.0f
        )]
        public float LineThickness { get; set; } = 2.0f;

        [VFXParameter(
            Id = "smoothing",
            Name = "Smoothing",
            Description = "Amount of line smoothing (0=none, 1=maximum)",
            MinValue = 0.0f,
            MaxValue = 1.0f,
            DefaultValue = 0.3f
        )]
        public float Smoothing { get; set; } = 0.3f;

        [VFXParameter(
            Id = "color_mode",
            Name = "Color Mode",
            Description = "Color scheme for the oscilloscope",
            EnumValues = new[] { "Audio", "Gradient", "Solid", "Rainbow" },
            DefaultValue = "Audio"
        )]
        public string ColorMode { get; set; } = "Audio";

        [VFXParameter(
            Id = "base_color",
            Name = "Base Color",
            Description = "Base color for solid color mode",
            DefaultValue = "#00FFFF"
        )]
        public string BaseColor { get; set; } = "#00FFFF";

        [VFXParameter(
            Id = "fade_trail",
            Name = "Fade Trail",
            Description = "Length of the fade trail effect",
            MinValue = 0.0f,
            MaxValue = 1.0f,
            DefaultValue = 0.5f
        )]
        public float FadeTrail { get; set; } = 0.5f;

        [VFXParameter(
            Id = "scale_factor",
            Name = "Scale Factor",
            Description = "Scaling factor for the display",
            MinValue = 0.1f,
            MaxValue = 5.0f,
            DefaultValue = 1.0f
        )]
        public float ScaleFactor { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private Vector2[] _waveformBuffer;
        private Vector2[] _spectrumBuffer;
        private Vector2[] _lissajousBuffer;
        private Vector2[] _circularBuffer;
        private Vector2[] _previousBuffer;
        private int _bufferSize;
        private float _time;
        private Random _random;

        #endregion

        #region Initialization

        protected override void OnInitialize(VFXRenderContext context, AudioFeatures audio)
        {
            base.OnInitialize(context, audio);
            
            _random = new Random();
            _time = 0.0f;
            _bufferSize = 512; // Power of 2 for FFT efficiency
            
            InitializeBuffers();
        }

        private void InitializeBuffers()
        {
            _waveformBuffer = new Vector2[_bufferSize];
            _spectrumBuffer = new Vector2[_bufferSize];
            _lissajousBuffer = new Vector2[_bufferSize];
            _circularBuffer = new Vector2[_bufferSize];
            _previousBuffer = new Vector2[_bufferSize];
            
            // Initialize with zeros
            for (int i = 0; i < _bufferSize; i++)
            {
                _waveformBuffer[i] = Vector2.Zero;
                _spectrumBuffer[i] = Vector2.Zero;
                _lissajousBuffer[i] = Vector2.Zero;
                _circularBuffer[i] = Vector2.Zero;
                _previousBuffer[i] = Vector2.Zero;
            }
        }

        #endregion

        #region Frame Processing

        protected override void OnProcessFrame(VFXRenderContext context)
        {
            _time += context.DeltaTime;
            
            // Update audio buffers
            UpdateAudioBuffers(context);
            
            // Apply smoothing
            ApplySmoothing();
            
            // Update previous buffer for trails
            UpdatePreviousBuffer();
        }

        private void UpdateAudioBuffers(VFXRenderContext context)
        {
            if (_audio == null) return;
            
            var centerX = context.Width / 2.0f;
            var centerY = context.Height / 2.0f;
            var scaleX = context.Width / (float)_bufferSize * ScaleFactor;
            var scaleY = context.Height * 0.4f * ScaleFactor;
            
            switch (DisplayMode.ToLower())
            {
                case "waveform":
                    UpdateWaveformBuffer(centerX, centerY, scaleX, scaleY);
                    break;
                case "spectrum":
                    UpdateSpectrumBuffer(centerX, centerY, scaleX, scaleY);
                    break;
                case "lissajous":
                    UpdateLissajousBuffer(centerX, centerY, scaleX, scaleY);
                    break;
                case "circular":
                    UpdateCircularBuffer(centerX, centerY, scaleX, scaleY);
                    break;
            }
        }

        private void UpdateWaveformBuffer(float centerX, float centerY, float scaleX, float scaleY)
        {
            var leftChannel = _audio.LeftChannel;
            var rightChannel = _audio.RightChannel;
            
            for (int i = 0; i < _bufferSize; i++)
            {
                var sampleIndex = (i * leftChannel.Length) / _bufferSize;
                if (sampleIndex >= leftChannel.Length) sampleIndex = leftChannel.Length - 1;
                
                var leftSample = leftChannel[sampleIndex];
                var rightSample = rightChannel[sampleIndex];
                var mixedSample = (leftSample + rightSample) * 0.5f;
                
                var x = centerX + (i - _bufferSize / 2.0f) * scaleX;
                var y = centerY + mixedSample * scaleY;
                
                _waveformBuffer[i] = new Vector2(x, y);
            }
        }

        private void UpdateSpectrumBuffer(float centerX, float centerY, float scaleX, float scaleY)
        {
            var centerChannel = _audio.CenterChannel;
            
            for (int i = 0; i < _bufferSize; i++)
            {
                var sampleIndex = (i * centerChannel.Length) / _bufferSize;
                if (sampleIndex >= centerChannel.Length) sampleIndex = centerChannel.Length - 1;
                
                var sample = Math.Abs(centerChannel[sampleIndex]);
                var frequency = (float)i / _bufferSize;
                
                var x = centerX + (i - _bufferSize / 2.0f) * scaleX;
                var y = centerY + sample * scaleY;
                
                _spectrumBuffer[i] = new Vector2(x, y);
            }
        }

        private void UpdateLissajousBuffer(float centerX, float centerY, float scaleX, float scaleY)
        {
            var leftChannel = _audio.LeftChannel;
            var rightChannel = _audio.RightChannel;
            
            for (int i = 0; i < _bufferSize; i++)
            {
                var sampleIndex = (i * leftChannel.Length) / _bufferSize;
                if (sampleIndex >= leftChannel.Length) sampleIndex = leftChannel.Length - 1;
                
                var leftSample = leftChannel[sampleIndex];
                var rightSample = rightChannel[sampleIndex];
                
                var x = centerX + leftSample * scaleX;
                var y = centerY + rightSample * scaleY;
                
                _lissajousBuffer[i] = new Vector2(x, y);
            }
        }

        private void UpdateCircularBuffer(float centerX, float centerY, float scaleX, float scaleY)
        {
            var centerChannel = _audio.CenterChannel;
            
            for (int i = 0; i < _bufferSize; i++)
            {
                var sampleIndex = (i * centerChannel.Length) / _bufferSize;
                if (sampleIndex >= centerChannel.Length) sampleIndex = centerChannel.Length - 1;
                
                var sample = Math.Abs(centerChannel[sampleIndex]);
                var angle = (float)i / _bufferSize * Math.PI * 2.0f;
                var radius = sample * scaleY;
                
                var x = centerX + (float)Math.Cos(angle) * radius;
                var y = centerY + (float)Math.Sin(angle) * radius;
                
                _circularBuffer[i] = new Vector2(x, y);
            }
        }

        private void ApplySmoothing()
        {
            var currentBuffer = GetCurrentBuffer();
            if (currentBuffer == null) return;
            
            for (int i = 0; i < _bufferSize; i++)
            {
                var smoothed = Vector2.Lerp(_previousBuffer[i], currentBuffer[i], Smoothing);
                _previousBuffer[i] = smoothed;
            }
        }

        private void UpdatePreviousBuffer()
        {
            var currentBuffer = GetCurrentBuffer();
            if (currentBuffer == null) return;
            
            for (int i = 0; i < _bufferSize; i++)
            {
                _previousBuffer[i] = currentBuffer[i];
            }
        }

        private Vector2[] GetCurrentBuffer()
        {
            return DisplayMode.ToLower() switch
            {
                "waveform" => _waveformBuffer,
                "spectrum" => _spectrumBuffer,
                "lissajous" => _lissajousBuffer,
                "circular" => _circularBuffer,
                _ => _waveformBuffer
            };
        }

        #endregion

        #region Color Management

        public System.Drawing.Color GetColorForIndex(int index)
        {
            return ColorMode.ToLower() switch
            {
                "audio" => GetAudioReactiveColor(index),
                "gradient" => GetGradientColor(index),
                "solid" => GetSolidColor(),
                "rainbow" => GetRainbowColor(index),
                _ => GetAudioReactiveColor(index)
            };
        }

        private System.Drawing.Color GetAudioReactiveColor(int index)
        {
            if (_audio == null) return System.Drawing.Color.Cyan;
            
            var beatIntensity = _audio.BeatIntensity;
            var rms = _audio.RMS;
            
            var hue = (beatIntensity * 60.0f + index * 2.0f) % 360.0f;
            var saturation = 0.8f + rms * 0.2f;
            var value = 0.7f + beatIntensity * 0.3f;
            
            return HsvToRgb(hue, saturation, value);
        }

        private System.Drawing.Color GetGradientColor(int index)
        {
            var progress = (float)index / _bufferSize;
            var hue = progress * 360.0f;
            return HsvToRgb(hue, 0.8f, 0.9f);
        }

        private System.Drawing.Color GetSolidColor()
        {
            try
            {
                return System.Drawing.ColorTranslator.FromHtml(BaseColor);
            }
            catch
            {
                return System.Drawing.Color.Cyan;
            }
        }

        private System.Drawing.Color GetRainbowColor(int index)
        {
            var hue = (_time * 50.0f + index * 3.0f) % 360.0f;
            return HsvToRgb(hue, 0.9f, 1.0f);
        }

        private System.Drawing.Color HsvToRgb(float h, float s, float v)
        {
            var c = v * s;
            var x = c * (1 - Math.Abs((h / 60.0f) % 2 - 1));
            var m = v - c;
            
            double r, g, b;
            
            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0.0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0.0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0.0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0.0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0.0; b = c;
            }
            else
            {
                r = c; g = 0.0; b = x;
            }
            
            return System.Drawing.Color.FromArgb(
                (int)((r + m) * 255),
                (int)((g + m) * 255),
                (int)((b + m) * 255)
            );
        }

        #endregion

        #region GPU Processing Support

        protected override bool SupportsGPUProcessing() => false; // CPU-only for now

        #endregion
    }
}