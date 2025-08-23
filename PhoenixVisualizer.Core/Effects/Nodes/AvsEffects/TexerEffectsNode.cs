using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class TexerEffectsNode : BaseEffectNode { public int TexerType { get; set; } = 0; public float Scale { get; set; } = 1.0f; public float Rotation { get; set; } = 0.0f; public bool BeatReactive { get; set; } = false; public float BeatScale { get; set; } = 2.0f; public TexerEffectsNode() { Name = \
Texer
Effects\; Description = \Creates
dynamic
texture
effects
with
scaling
and
rotation\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
texturing\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Textured
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); float currentScale = Scale; if (BeatReactive && audioFeatures?.IsBeat == true) { currentScale *= BeatScale; } for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { float centerX = imageBuffer.Width / 2.0f; float centerY = imageBuffer.Height / 2.0f; float dx = x - centerX; float dy = y - centerY; float cosRot = (float)Math.Cos(Rotation); float sinRot = (float)Math.Sin(Rotation); float rotatedX = dx * cosRot - dy * sinRot; float rotatedY = dx * sinRot + dy * cosRot; int sourceX = (int)(centerX + rotatedX / currentScale); int sourceY = (int)(centerY + rotatedY / currentScale); if (sourceX >= 0 && sourceX < imageBuffer.Width && sourceY >= 0 && sourceY < imageBuffer.Height) { int pixel = imageBuffer.GetPixel(sourceX, sourceY); output.SetPixel(x, y, pixel); } } } return output; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
