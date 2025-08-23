using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class BrightnessEffectsNode : BaseEffectNode { public float Brightness { get; set; } = 1.0f; public float Contrast { get; set; } = 1.0f; public float Gamma { get; set; } = 1.0f; public bool BeatReactive { get; set; } = false; public float BeatBrightness { get; set; } = 1.5f; public BrightnessEffectsNode() { Name = \
Brightness
Effects\; Description = \Adjusts
brightness
contrast
and
gamma
of
the
image\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
brightness
adjustment\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Brightness
adjusted
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); float currentBrightness = Brightness; if (BeatReactive && audioFeatures?.IsBeat == true) { currentBrightness *= BeatBrightness; } for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { int pixel = imageBuffer.GetPixel(x, y); int r = pixel & 0xFF; int g = (pixel >> 8) & 0xFF; int b = (pixel >> 16) & 0xFF; float rNorm = r / 255.0f; float gNorm = g / 255.0f; float bNorm = b / 255.0f; rNorm = ApplyBrightnessContrastGamma(rNorm, currentBrightness, Contrast, Gamma); gNorm = ApplyBrightnessContrastGamma(gNorm, currentBrightness, Contrast, Gamma); bNorm = ApplyBrightnessContrastGamma(bNorm, currentBrightness, Contrast, Gamma); int newR = Math.Clamp((int)(rNorm * 255), 0, 255); int newG = Math.Clamp((int)(gNorm * 255), 0, 255); int newB = Math.Clamp((int)(bNorm * 255), 0, 255); int newPixel = newR | (newG << 8) | (newB << 16); output.SetPixel(x, y, newPixel); } } return output; } private float ApplyBrightnessContrastGamma(float value, float brightness, float contrast, float gamma) { value = value * brightness; value = (value - 0.5f) * contrast + 0.5f; value = (float)Math.Pow(value, gamma); return Math.Clamp(value, 0.0f, 1.0f); } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
