using System;
using System.Drawing;

namespace PhoenixVisualizer.Core.Models
{
    public class ImageBuffer
    {
        public int Width { get; }
        public int Height { get; }
        public int[] Pixels { get; }

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
    }
}
