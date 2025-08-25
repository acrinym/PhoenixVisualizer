using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class BassSpinEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Bass Spin effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Enables visualization for the left audio channel
        /// </summary>
        public bool LeftChannelEnabled { get; set; } = true;

        /// <summary>
        /// Enables visualization for the right audio channel
        /// </summary>
        public bool RightChannelEnabled { get; set; } = true;

        /// <summary>
        /// Rendering mode (0 = Lines, 1 = Triangles)
        /// </summary>
        public int Mode { get; set; } = 1;

        /// <summary>
        /// Color for the left channel visualization
        /// </summary>
        public Color LeftColor { get; set; } = Color.White;

        /// <summary>
        /// Color for the right channel visualization
        /// </summary>
        public Color RightColor { get; set; } = Color.White;

        /// <summary>
        /// Overall intensity of the effect (0.0 to 1.0)
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private int lastAudioLevel = 0;
        private readonly Point[,] leftPositions = new Point[2, 2];
        private readonly Point[,] rightPositions = new Point[2, 2];
        private double leftVelocity = 0.0;
        private double rightVelocity = 0.0;
        private double leftRotation = Math.PI;
        private double rightRotation = 0.0;
        private double leftDirection = -1.0;
        private double rightDirection = 1.0;
        private const double RotationStep = Math.PI / 6.0;
        private const int BassBandCount = 44;

        #endregion

        #region Constructor

        public BassSpinEffectsNode()
        {
            Name = "Bass Spin Effects";
            Description = "Spinning lines or triangles reacting to bass frequencies";
            Category = "AVS Effects";

            // Initialize positions
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    leftPositions[i, j] = Point.Empty;
                    rightPositions[i, j] = Point.Empty;
                }
            }
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for sizing"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Bass spin output image"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            if (!Enabled)
                return output;

            int width = output.Width;
            int height = output.Height;

            if (LeftChannelEnabled)
                ProcessChannel(output, audioFeatures, 0, LeftColor, width, height);

            if (RightChannelEnabled)
                ProcessChannel(output, audioFeatures, 1, RightColor, width, height);

            return output;
        }

        private void ProcessChannel(ImageBuffer imageBuffer, AudioFeatures audioFeatures, int channelIndex, Color color, int width, int height)
        {
            int audioLevel = CalculateBassLevel(audioFeatures);
            double velocity = 0.7 * (Math.Max(audioLevel - 104, 12) / 96.0) + 0.3 * GetChannelVelocity(channelIndex);
            SetChannelVelocity(channelIndex, velocity);

            double rotation = GetChannelRotation(channelIndex);
            double direction = GetChannelDirection(channelIndex);
            rotation += RotationStep * velocity * direction;
            SetChannelRotation(channelIndex, rotation);

            int maxSize = Math.Min(height / 2, (width * 3) / 8);
            double size = maxSize * (audioLevel / 256.0) * Intensity;

            int centerX = (channelIndex == 0) ? width / 2 - maxSize / 2 : width / 2 + maxSize / 2;
            int centerY = height / 2;

            int xPos = (int)(Math.Cos(rotation) * size);
            int yPos = (int)(Math.Sin(rotation) * size);

            if (Mode == 0)
                RenderLines(imageBuffer, centerX, centerY, xPos, yPos, color, channelIndex);
            else
                RenderTriangles(imageBuffer, centerX, centerY, xPos, yPos, color, channelIndex);
        }

        private int CalculateBassLevel(AudioFeatures audioFeatures)
        {
            var spectrumData = audioFeatures?.SpectrumData;
            int totalLevel = 0;

            if (spectrumData != null && spectrumData.Length > 0)
            {
                int bandCount = Math.Min(BassBandCount, spectrumData.Length);
                for (int i = 0; i < bandCount; i++)
                    totalLevel += (int)spectrumData[i];
            }

            int relativeLevel = (totalLevel * 512) / (lastAudioLevel + 30 * 256);
            lastAudioLevel = totalLevel;
            return Math.Min(relativeLevel, 255);
        }

        private void RenderLines(ImageBuffer imageBuffer, int centerX, int centerY, int xPos, int yPos, Color color, int channelIndex)
        {
            var positions = GetChannelPositions(channelIndex);

            if (positions[0, 0] != Point.Empty || positions[0, 1] != Point.Empty)
                DrawLine(imageBuffer, positions[0, 0], positions[0, 1], centerX + xPos, centerY + yPos, color);

            DrawLine(imageBuffer, centerX, centerY, centerX + xPos, centerY + yPos, color);

            if (positions[1, 0] != Point.Empty || positions[1, 1] != Point.Empty)
                DrawLine(imageBuffer, positions[1, 0], positions[1, 1], centerX - xPos, centerY - yPos, color);

            DrawLine(imageBuffer, centerX, centerY, centerX - xPos, centerY - yPos, color);

            UpdateChannelPositions(channelIndex, centerX + xPos, centerY + yPos, centerX - xPos, centerY - yPos);
        }

        private void RenderTriangles(ImageBuffer imageBuffer, int centerX, int centerY, int xPos, int yPos, Color color, int channelIndex)
        {
            var positions = GetChannelPositions(channelIndex);

            if (positions[0, 0] != Point.Empty || positions[0, 1] != Point.Empty)
            {
                Point[] triangle1 = { new Point(centerX, centerY), positions[0, 0], new Point(centerX + xPos, centerY + yPos) };
                RenderTriangle(imageBuffer, triangle1, color);
            }

            if (positions[1, 0] != Point.Empty || positions[1, 1] != Point.Empty)
            {
                Point[] triangle2 = { new Point(centerX, centerY), positions[1, 0], new Point(centerX - xPos, centerY - yPos) };
                RenderTriangle(imageBuffer, triangle2, color);
            }

            UpdateChannelPositions(channelIndex, centerX + xPos, centerY + yPos, centerX - xPos, centerY - yPos);
        }

        private void DrawLine(ImageBuffer imageBuffer, Point start, Point end, int x, int y, Color color)
        {
            if (start == Point.Empty)
                return;

            DrawLine(imageBuffer, start.X, start.Y, x, y, color);
        }

        private void DrawLine(ImageBuffer imageBuffer, int x1, int y1, int x2, int y2, Color color)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int x = x1;
            int y = y1;

            while (true)
            {
                if (x >= 0 && x < imageBuffer.Width && y >= 0 && y < imageBuffer.Height)
                {
                    Color existing = Color.FromArgb(imageBuffer.GetPixel(x, y));
                    Color blended = BlendColors(existing, color);
                    imageBuffer.SetPixel(x, y, blended.ToArgb());
                }

                if (x == x2 && y == y2)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private void RenderTriangle(ImageBuffer imageBuffer, Point[] points, Color color)
        {
            if (points.Length != 3)
                return;

            Array.Sort(points, (a, b) => a.Y.CompareTo(b.Y));

            int x1 = points[0].X, y1 = points[0].Y;
            int x2 = points[1].X, y2 = points[1].Y;
            int x3 = points[2].X, y3 = points[2].Y;

            double slope1 = (y2 - y1) != 0 ? (double)(x2 - x1) / (y2 - y1) : 0;
            double slope2 = (y3 - y1) != 0 ? (double)(x3 - x1) / (y3 - y1) : 0;
            double slope3 = (y3 - y2) != 0 ? (double)(x3 - x2) / (y3 - y2) : 0;

            for (int y = y1; y <= y3; y++)
            {
                if (y < 0 || y >= imageBuffer.Height) continue;

                int startX, endX;
                if (y < y2)
                {
                    startX = (int)(x1 + slope1 * (y - y1));
                    endX = (int)(x1 + slope2 * (y - y1));
                }
                else
                {
                    startX = (int)(x2 + slope3 * (y - y2));
                    endX = (int)(x1 + slope2 * (y - y1));
                }

                if (startX > endX)
                {
                    int temp = startX;
                    startX = endX;
                    endX = temp;
                }

                for (int x = startX; x <= endX; x++)
                {
                    if (x >= 0 && x < imageBuffer.Width)
                    {
                        Color existing = Color.FromArgb(imageBuffer.GetPixel(x, y));
                        Color blended = BlendColors(existing, color);
                        imageBuffer.SetPixel(x, y, blended.ToArgb());
                    }
                }
            }
        }

        private Color BlendColors(Color existing, Color source)
        {
            return Color.FromArgb(
                Math.Min(255, existing.A + source.A),
                Math.Min(255, existing.R + source.R),
                Math.Min(255, existing.G + source.G),
                Math.Min(255, existing.B + source.B)
            );
        }

        #region Channel State Management

        private double GetChannelVelocity(int channelIndex) => channelIndex == 0 ? leftVelocity : rightVelocity;
        private void SetChannelVelocity(int channelIndex, double velocity)
        {
            if (channelIndex == 0) leftVelocity = velocity; else rightVelocity = velocity;
        }

        private double GetChannelRotation(int channelIndex) => channelIndex == 0 ? leftRotation : rightRotation;
        private void SetChannelRotation(int channelIndex, double rotation)
        {
            if (channelIndex == 0) leftRotation = rotation; else rightRotation = rotation;
        }

        private double GetChannelDirection(int channelIndex) => channelIndex == 0 ? leftDirection : rightDirection;

        private Point[,] GetChannelPositions(int channelIndex) => channelIndex == 0 ? leftPositions : rightPositions;

        private void UpdateChannelPositions(int channelIndex, int x1, int y1, int x2, int y2)
        {
            var positions = GetChannelPositions(channelIndex);
            positions[0, 0] = positions[0, 1];
            positions[0, 1] = new Point(x1, y1);
            positions[1, 0] = positions[1, 1];
            positions[1, 1] = new Point(x2, y2);
        }

        #endregion

        #region Configuration

        public override bool ValidateConfiguration()
        {
            if (Mode < 0 || Mode > 1)
                Mode = 1;

            if (Intensity < 0.0f || Intensity > 1.0f)
                Intensity = 1.0f;

            return true;
        }

        public override string GetSettingsSummary()
        {
            string channels;
            if (LeftChannelEnabled && RightChannelEnabled)
                channels = "Both";
            else if (LeftChannelEnabled)
                channels = "Left";
            else if (RightChannelEnabled)
                channels = "Right";
            else
                channels = "None";

            return $"Bass Spin: {(Enabled ? "Enabled" : "Disabled")}, Channels: {channels}, Mode: {(Mode == 0 ? "Lines" : "Triangles")}";
        }

        #endregion

        #region Public Configuration Helpers

        public void SetMode(int mode) => Mode = (mode == 0 || mode == 1) ? mode : 1;
        public void SetLeftColor(Color color) => LeftColor = color;
        public void SetRightColor(Color color) => RightColor = color;
        public void SetLeftChannelEnabled(bool enabled) => LeftChannelEnabled = enabled;
        public void SetRightChannelEnabled(bool enabled) => RightChannelEnabled = enabled;

        #endregion

        #region Reset & Defaults

        public override void Reset()
        {
            leftRotation = Math.PI;
            rightRotation = 0.0;
            leftVelocity = 0.0;
            rightVelocity = 0.0;
            lastAudioLevel = 0;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    leftPositions[i, j] = Point.Empty;
                    rightPositions[i, j] = Point.Empty;
                }
            }
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(1, 1);
        }

        #endregion
    }
}
