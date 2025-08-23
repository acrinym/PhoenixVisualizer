using System; using System.Threading.Tasks; using PhoenixVisualizer.Core.Effects; using PhoenixVisualizer.Core.Effects.Nodes.AvsEffects; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Tests { public class EffectGraphTest { public static async Task TestBasicEffectChain() { Console.WriteLine(\
Testing
basic
effect
chain...\); var graph = new EffectGraph(); var blurNode = new BlurEffectsNode { BlurIntensity = 1 }; var transNode = new TransEffectsNode { TransitionProgress = 0.5f }; var colorMapNode = new ColorMapEffectsNode { ColorMapType = 1 }; graph.AddNode(blurNode); graph.AddNode(transNode); graph.AddNode(colorMapNode); graph.ConnectNodes(graph.RootInput.Id, \Output\, blurNode.Id, \Image\); graph.ConnectNodes(blurNode.Id, \Output\, transNode.Id, \Image1\); graph.ConnectNodes(transNode.Id, \Output\, colorMapNode.Id, \Image1\); graph.ConnectNodes(colorMapNode.Id, \Output\, graph.FinalOutput.Id, \Input\); var testImage = new ImageBuffer(100, 100); testImage.Clear(0xFF0000); // Red image var audioFeatures = new AudioFeatures(); try { var result = await graph.ExecuteAsync(new EffectInput { Image = testImage }, audioFeatures); Console.WriteLine($\Effect
chain
executed
successfully!
Output:
result
\); } catch (Exception ex) { Console.WriteLine($\Effect
chain
failed:
ex.Message
\); } } } }
