using System;
using System.Drawing;
using Avalonia.Media.Imaging;

namespace PhoenixVisualizer.Core.Models
{
    public class ImageBuffer
    {
        public int Width { get; }
        public int Height { get; }
        public int[] Pixels { get; set; }
        
        // NEW: Data property for compatibility with effects
        public uint[] Data 
        { 
            get => Array.ConvertAll(Pixels, x => (uint)x);
            set => Pixels = Array.ConvertAll(value, x => (int)x);
        }

        // NEW: Indexer support for compatibility
        public int this[int index]
        {
            get => Pixels[index];
            set => Pixels[index] = value;
        }

        public int this[int x, int y]
        {
            get => GetPixel(x, y);
            set => SetPixel(x, y, value);
        }

        public ImageBuffer(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new int[width * height];
        }

        public ImageBuffer(int width, int height, int[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels ?? new int[width * height];
        }

        public int GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return 0;
            return Pixels[y * Width + x];
        }

        public Color GetPixelColor(int x, int y)
        {
            return Color.FromArgb(GetPixel(x, y));
        }

        public void SetPixel(int x, int y, int color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            Pixels[y * Width + x] = color;
        }

        public void Clear(int color = 0)
        {
            Array.Fill(Pixels, color);
        }

        // NEW: Additional methods that effects need
        public void SetPixel(int x, int y, Color color)
        {
            SetPixel(x, y, color.ToArgb());
        }

        public void CopyTo(ImageBuffer destination)
        {
            Array.Copy(Pixels, destination.Pixels, Pixels.Length);
        }

        public ImageBuffer Clone()
        {
            var clone = new ImageBuffer(Width, Height);
            CopyTo(clone);
            return clone;
        }

        public void Blit(ImageBuffer source)
        {
            source.CopyTo(this);
        }

        // TODO: Implement DrawText using System.Drawing if needed
        public void DrawText(string text, Typeface typeface, int fontSize, Color color, Point position)
        {
            // TODO: Implement text drawing using System.Drawing
        }

        public void DrawBitmap(Avalonia.Media.Imaging.Bitmap bitmap, int x, int y, int width, int height)
        {
            // TODO: Implement bitmap drawing if needed
        }
    }

    // Placeholder classes for compatibility
    public class Typeface { }
}
