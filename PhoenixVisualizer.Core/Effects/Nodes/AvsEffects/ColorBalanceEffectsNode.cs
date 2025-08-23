using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class ColorBalanceEffectsNode : BaseEffectNode { public float RedBalance { get; set; } = 1.0f; public float GreenBalance { get; set; } = 1.0f; public float BlueBalance { get; set; } = 1.0f; public float Saturation { get; set; } = 1.0f; public float HueShift { get; set; } = 0.0f; public bool BeatReactive { get; set; } = false; public float BeatSaturation { get; set; } = 1.5f; public ColorBalanceEffectsNode() { Name = \
Color
Balance
Effects\; Description = \Adjusts
color
balance
saturation
and
hue
of
the
image\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
color
balance
adjustment\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Color
balanced
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); float currentSaturation = Saturation; if (BeatReactive && audioFeatures?.IsBeat == true) { currentSaturation *= BeatSaturation; } for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { int pixel = imageBuffer.GetPixel(x, y); int r = pixel & 0xFF; int g = (pixel >> 8) & 0xFF; int b = (pixel >> 16) & 0xFF; float rNorm = r / 255.0f; float gNorm = g / 255.0f; float bNorm = b / 255.0f; rNorm *= RedBalance; gNorm *= GreenBalance; bNorm *= BlueBalance; if (HueShift != 0) { ApplyHueShift(ref rNorm, ref gNorm, ref bNorm, HueShift); } if (currentSaturation != 1.0f) { ApplySaturation(ref rNorm, ref gNorm, ref bNorm, currentSaturation); } int newR = Math.Clamp((int)(rNorm * 255), 0, 255); int newG = Math.Clamp((int)(gNorm * 255), 0, 255); int newB = Math.Clamp((int)(bNorm * 255), 0, 255); int newPixel = newR | (newG << 8) | (newB << 16); output.SetPixel(x, y, newPixel); } } return output; } private void ApplyHueShift(ref float r, ref float g, ref float b, float hueShift) { float h, s, v; RgbToHsv(r, g, b, out h, out s, out v); h = (h + hueShift) % 360.0f; if (h < 0) h += 360.0f; HsvToRgb(h, s, v, out r, out g, out b); } private void ApplySaturation(ref float r, ref float g, ref float b, float saturation) { float h, s, v; RgbToHsv(r, g, b, out h, out s, out v); s = Math.Clamp(s * saturation, 0.0f, 1.0f); HsvToRgb(h, s, v, out r, out g, out b); } private void RgbToHsv(float r, float g, float b, out float h, out float s, out float v) { float max = Math.Max(Math.Max(r, g), b); float min = Math.Min(Math.Min(r, g), b); float delta = max - min; v = max; s = max == 0 ? 0 : delta / max; if (delta == 0) { h = 0; } else if (max == r) { h = ((g - b) / delta) % 6; } else if (max == g) { h = (b - r) / delta + 2; } else { h = (r - g) / delta + 4; } h *= 60; if (h < 0) h += 360; } private void HsvToRgb(float h, float s, float v, out float r, out float g, out float b) { float c = v * s; float x = c * (1 - Math.Abs((h / 60) % 2 - 1)); float m = v - c; if (h >= 0 && h < 60) { r = c; g = x; b = 0; } else if (h >= 60 && h < 120) { r = x; g = c; b = 0; } else if (h >= 120 && h < 180) { r = 0; g = c; b = x; } else if (h >= 180 && h < 240) { r = 0; g = x; b = c; } else if (h >= 240 && h < 300) { r = x; g = 0; b = c; } else { r = c; g = 0; b = x; } r += m; g += m; b += m; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
