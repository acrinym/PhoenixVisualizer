using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

// Needed for platform specific drawing
using System.Runtime.Versioning;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Fun little text renderer with fonts, alignment, and optional animation. 🎨
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class TextEffectsNode : BaseEffectNode
    {
        // ✅ Public properties for external configuration
        public bool Enabled { get; set; } = true;
        public string Text { get; set; } = "Sample Text";
        public Color TextColor { get; set; } = Color.White;
        public bool Outline { get; set; } = false;
        public Color OutlineColor { get; set; } = Color.Black;
        public int OutlineSize { get; set; } = 1;
        public string FontFamily { get; set; } = "Arial";
        public float FontSize { get; set; } = 24.0f;
        public FontStyle FontStyle { get; set; } = FontStyle.Regular;
        // Alignment options: 0=Left/Top, 1=Center, 2=Right/Bottom
        public int HorizontalAlignment { get; set; } = 1;
        public int VerticalAlignment { get; set; } = 1;
        // Simple word-by-word animation toggle
        public bool Animate { get; set; } = false;
        public int AnimationSpeed { get; set; } = 15; // frames per word
        // Optional pixel shift for precise placement
        public int XShift { get; set; } = 0;
        public int YShift { get; set; } = 0;

        // 🧰 Private fields to keep track of state
        private Font? _currentFont;
        private readonly StringFormat _stringFormat = new();
        private string[] _words = Array.Empty<string>();
        private int _currentWordIndex = 0;
        private int _frameCounter = 0;

        public TextEffectsNode()
        {
            Name = "Text Effects";
            Description = "Renders customizable text with optional animation";
            Category = "AVS Effects";
        }

        protected override void InitializePorts()
        {
            // We expect an input image to determine canvas size
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Base image for text overlay"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with rendered text"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
            {
                return GetDefaultOutput();
            }

            if (!Enabled)
            {
                return imageBuffer;
            }

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            // Copy original image first – keeps background intact
            for (int y = 0; y < imageBuffer.Height; y++)
            {
                for (int x = 0; x < imageBuffer.Width; x++)
                {
                    output.SetPixel(x, y, imageBuffer.GetPixel(x, y));
                }
            }

            // Grab the current text to display
            string textToRender = PrepareText();

            using var bmp = new Bitmap(output.Width, output.Height);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            RectangleF rect = new RectangleF(XShift, YShift, output.Width, output.Height);

            if (Outline)
            {
                using var path = new GraphicsPath();
                path.AddString(textToRender, _currentFont?.FontFamily ?? new FontFamily(FontFamily),
                    (int)FontStyle, g.DpiY * FontSize / 72f, rect, _stringFormat);
                using var brush = new SolidBrush(TextColor);
                g.FillPath(brush, path);
                using var pen = new Pen(OutlineColor, OutlineSize) { LineJoin = LineJoin.Round };
                g.DrawPath(pen, path);
            }
            else
            {
                using var brush = new SolidBrush(TextColor);
                g.DrawString(textToRender, _currentFont ?? new Font(FontFamily, FontSize, FontStyle), brush, rect, _stringFormat);
            }

            // Copy rendered pixels back into the image buffer
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    if (c.A > 0)
                    {
                        output.SetPixel(x, y, c.ToArgb());
                    }
                }
            }

            return output;
        }

        private string PrepareText()
        {
            // Keep the font up to date – we don't want stale style! 😄
            if (_currentFont == null || _currentFont.FontFamily.Name != FontFamily ||
                Math.Abs(_currentFont.Size - FontSize) > float.Epsilon || _currentFont.Style != FontStyle)
            {
                _currentFont?.Dispose();
                _currentFont = new Font(FontFamily, FontSize, FontStyle);
            }

            _stringFormat.Alignment = HorizontalAlignment switch
            {
                0 => StringAlignment.Near,
                2 => StringAlignment.Far,
                _ => StringAlignment.Center
            };
            _stringFormat.LineAlignment = VerticalAlignment switch
            {
                0 => StringAlignment.Near,
                2 => StringAlignment.Far,
                _ => StringAlignment.Center
            };

            if (!Animate)
            {
                return Text.Replace(';', '\n');
            }

            if (_words.Length == 0)
            {
                _words = Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                _currentWordIndex = 0;
                _frameCounter = 0;
            }

            _frameCounter++;
            if (_frameCounter >= AnimationSpeed)
            {
                _frameCounter = 0;
                _currentWordIndex = (_currentWordIndex + 1) % _words.Length;
            }

            return _words[_currentWordIndex];
        }

        public override void Reset()
        {
            _currentWordIndex = 0;
            _frameCounter = 0;
        }

        public override object GetDefaultOutput() => new ImageBuffer(1, 1);
    }
}
