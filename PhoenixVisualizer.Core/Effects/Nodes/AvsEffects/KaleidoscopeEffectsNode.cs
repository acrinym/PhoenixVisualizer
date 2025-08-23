using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class KaleidoscopeEffectsNode : BaseEffectNode { public int NumSides { get; set; } = 6; public float Rotation { get; set; } = 0.0f; public float Scale { get; set; } = 1.0f; public bool BeatReactive { get; set; } = false; public float BeatRotation { get; set; } = 0.5f; public KaleidoscopeEffectsNode() { Name = \
Kaleidoscope
Effects\; Description = \Creates
kaleidoscope
patterns
with
configurable
sides
and
rotation\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
kaleidoscope
effect\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Kaleidoscope
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); float currentRotation = Rotation; if (BeatReactive && audioFeatures?.IsBeat == true) { currentRotation += BeatRotation; } float centerX = imageBuffer.Width / 2.0f; float centerY = imageBuffer.Height / 2.0f; float angleStep = (2.0f * (float)Math.PI) / NumSides; for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { float dx = x - centerX; float dy = y - centerY; float distance = (float)Math.Sqrt(dx * dx + dy * dy); float angle = (float)Math.Atan2(dy, dx) + currentRotation; angle = angle % (2.0f * (float)Math.PI); if (angle < 0) angle += 2.0f * (float)Math.PI; int sector = (int)(angle / angleStep); float sectorAngle = sector * angleStep; float rotatedX = (float)Math.Cos(sectorAngle) * distance; float rotatedY = (float)Math.Sin(sectorAngle) * distance; int sourceX = (int)(centerX + rotatedX / Scale); int sourceY = (int)(centerY + rotatedY / Scale); if (sourceX >= 0 && sourceX < imageBuffer.Width && sourceY >= 0 && sourceY < imageBuffer.Height) { int pixel = imageBuffer.GetPixel(sourceX, sourceY); output.SetPixel(x, y, pixel); } } } return output; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
