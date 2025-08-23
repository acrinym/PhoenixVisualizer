using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class StarfieldEffectsNode : BaseEffectNode { public int NumStars { get; set; } = 100; public float WarpSpeed { get; set; } = 1.0f; public bool BeatReactive { get; set; } = false; public float BeatWarpSpeed { get; set; } = 2.0f; public int StarColor { get; set; } = 0xFFFFFF; private readonly Random random = new Random(); private readonly List<Star> stars = new List<Star>(); public StarfieldEffectsNode() { Name = \
Starfield
Effects\; Description = \Creates
3D
star
field
visualization
with
depth
and
movement\; Category = \AVS
Effects\; InitializeStars(); } private void InitializeStars() { stars.Clear(); for (int i = 0; i < NumStars; i++) { stars.Add(new Star { X = random.Next(-1000, 1000), Y = random.Next(-1000, 1000), Z = random.Next(0, 1000) }); } } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
starfield
overlay\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Starfield
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length); float currentWarpSpeed = WarpSpeed; if (BeatReactive && audioFeatures?.IsBeat == true) { currentWarpSpeed *= BeatWarpSpeed; } UpdateStars(currentWarpSpeed); RenderStars(output); return output; } private void UpdateStars(float warpSpeed) { foreach (var star in stars) { star.Z -= warpSpeed; if (star.Z < 1) { star.Z = 1000; star.X = random.Next(-1000, 1000); star.Y = random.Next(-1000, 1000); } } } private void RenderStars(ImageBuffer output) { float centerX = output.Width / 2.0f; float centerY = output.Height / 2.0f; foreach (var star in stars) { if (star.Z > 0) { float x = centerX + (star.X / star.Z) * 100; float y = centerY + (star.Y / star.Z) * 100; if (x >= 0 && x < output.Width && y >= 0 && y < output.Height) { int brightness = (int)(255 * (1.0f - star.Z / 1000.0f)); int color = (brightness << 16) | (brightness << 8) | brightness; output.SetPixel((int)x, (int)y, color); } } } } private class Star { public float X { get; set; } public float Y { get; set; } public float Z { get; set; } } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
