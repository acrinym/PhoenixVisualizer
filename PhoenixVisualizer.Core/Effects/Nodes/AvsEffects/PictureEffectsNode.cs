using Avalonia.Media;
using Avalonia.Media.Imaging;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System;
using System.IO;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class PictureEffectsNode : BaseEffectNode
{
    [VFXParameter("FilePath")] public string FilePath { get; set; } = "";
    [VFXParameter("Scale")] public double Scale { get; set; } = 1.0;

    private Bitmap? _bmp;

    protected override void InitializePorts()
    {
        AddOutput("Result");
    }

    protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
    {
        var result = GetOutput<ImageBuffer>("Result");
        if (result == null) return null!;

        if (_bmp == null && File.Exists(FilePath))
            _bmp = new Bitmap(FilePath);

        result.Clear();
        if (_bmp != null)
        {
            int w = (int)(_bmp.PixelSize.Width * Scale);
            int h = (int)(_bmp.PixelSize.Height * Scale);
            int x = (result.Width - w) / 2;
            int y = (result.Height - h) / 2;
            result.DrawBitmap(_bmp, x, y, w, h);
        }
        
        return result;
    }
}