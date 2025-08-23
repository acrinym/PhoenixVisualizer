using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class MovementEffectsNode : BaseEffectNode { public float MoveX { get; set; } = 0.0f; public float MoveY { get; set; } = 0.0f; public float Rotate { get; set; } = 0.0f; public float Scale { get; set; } = 1.0f; public bool BeatReactive { get; set; } = false; public float BeatMoveX { get; set; } = 10.0f; public float BeatMoveY { get; set; } = 10.0f; public MovementEffectsNode() { Name = \
Movement
Effects\; Description = \Applies
movement
rotation
and
scaling
transformations\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
movement\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Moved
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); float currentMoveX = MoveX; float currentMoveY = MoveY; if (BeatReactive && audioFeatures?.IsBeat == true) { currentMoveX += BeatMoveX; currentMoveY += BeatMoveY; } float centerX = imageBuffer.Width / 2.0f; float centerY = imageBuffer.Height / 2.0f; float cosRot = (float)Math.Cos(Rotate); float sinRot = (float)Math.Sin(Rotate); for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { float dx = (x - centerX) / Scale; float dy = (y - centerY) / Scale; float rotatedX = dx * cosRot - dy * sinRot; float rotatedY = dx * sinRot + dy * cosRot; int sourceX = (int)(centerX + rotatedX + currentMoveX); int sourceY = (int)(centerY + rotatedY + currentMoveY); if (sourceX >= 0 && sourceX < imageBuffer.Width && sourceY >= 0 && sourceY < imageBuffer.Height) { int pixel = imageBuffer.GetPixel(sourceX, sourceY); output.SetPixel(x, y, pixel); } } } return output; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
