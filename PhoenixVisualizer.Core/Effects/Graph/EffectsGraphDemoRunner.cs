using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Graph
{
    public static class EffectsGraphDemoRunner
    {
        private static bool _isRunning = false;
        private static CancellationTokenSource? _cancellationTokenSource;

        public static async Task RunLiveDemoAsync()
        {
            if (_isRunning)
            {
                Console.WriteLine("Demo is already running!");
                return;
            }

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                Console.WriteLine("🚀 Starting Phoenix EffectsGraph Live Demo...");
                Console.WriteLine("Press Ctrl+C to stop the demo\n");

                // Create demo graphs
                var starfieldDemo = EffectsGraphDemo.CreateStarfieldDemo();
                var parallelDemo = EffectsGraphDemo.CreateParallelDemo();
                var complexDemo = EffectsGraphDemo.CreateComplexComposition();

                var graphs = new List<EffectsGraph> { starfieldDemo, parallelDemo, complexDemo };
                var currentGraphIndex = 0;

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var currentGraph = graphs[currentGraphIndex];
                    Console.WriteLine($"🎬 Now Playing: {currentGraph.Name}");
                    Console.WriteLine($"📝 {currentGraph.Description}");
                    Console.WriteLine($"🔗 {currentGraph.GetConnections().Count} connections, {currentGraph.GetNodes().Count} nodes");

                    // Run the graph for a few seconds
                    await RunGraphForDurationAsync(currentGraph, 5000, _cancellationTokenSource.Token);

                    // Move to next graph
                    currentGraphIndex = (currentGraphIndex + 1) % graphs.Count;
                    Console.WriteLine("\n" + new string('-', 50) + "\n");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n⏹️ Demo stopped by user");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo error: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                _cancellationTokenSource?.Dispose();
            }
        }

        public static void StopDemo()
        {
            _cancellationTokenSource?.Cancel();
        }

        private static async Task RunGraphForDurationAsync(EffectsGraph graph, int durationMs, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            var frameCount = 0;
            var lastFpsUpdate = DateTime.Now;

            Console.WriteLine("🎵 Processing graph with live audio simulation...");

            while ((DateTime.Now - startTime).TotalMilliseconds < durationMs && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Create simulated audio features
                    var audioFeatures = CreateSimulatedAudioFeatures(frameCount);
                    
                    // Process the graph
                    var results = graph.ProcessGraph(audioFeatures);
                    
                    // Update frame counter
                    frameCount++;
                    
                    // Update FPS display every second
                    if ((DateTime.Now - lastFpsUpdate).TotalSeconds >= 1.0)
                    {
                        var fps = frameCount / (DateTime.Now - startTime).TotalSeconds;
                        Console.Write($"\r🎬 Frame: {frameCount:D4} | FPS: {fps:F1} | Audio: Beat={audioFeatures.Beat} | RMS={audioFeatures.RMS:F2}");
                        lastFpsUpdate = DateTime.Now;
                    }

                    // Simulate real-time processing
                    await Task.Delay(16, cancellationToken); // ~60 FPS
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Graph processing error: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine($"\n✅ Completed {frameCount} frames in {(DateTime.Now - startTime).TotalMilliseconds:F0}ms");
        }

        private static AudioFeatures CreateSimulatedAudioFeatures(int frameCount)
        {
            var time = frameCount * 0.016; // 16ms per frame
            var beatFrequency = 2.0; // 2 beats per second
            var beat = Math.Sin(time * beatFrequency * Math.PI * 2) > 0.5;
            
            // Simulate varying audio intensity
            var baseIntensity = 0.3 + 0.4 * Math.Sin(time * 0.5);
            var beatIntensity = beat ? 0.8 + 0.2 * Math.Sin(time * 10) : 0.2;
            
            // Create simulated audio channels
            var channelLength = 1024;
            var leftChannel = new float[channelLength];
            var rightChannel = new float[channelLength];
            var centerChannel = new float[channelLength];

            for (int i = 0; i < channelLength; i++)
            {
                var frequency = 0.1 + 0.9 * (i / (float)channelLength);
                var wave = Math.Sin(time * frequency * 10) * baseIntensity;
                
                leftChannel[i] = (float)(wave * (0.8 + 0.2 * Math.Sin(time * 2)));
                rightChannel[i] = (float)(wave * (0.8 + 0.2 * Math.Sin(time * 2.1)));
                centerChannel[i] = (float)(wave * 0.5);
            }

            return new AudioFeatures
            {
                Beat = beat,
                BeatIntensity = (float)beatIntensity,
                RMS = (float)baseIntensity,
                Bass = (float)(0.6 + 0.4 * Math.Sin(time * 0.3)),
                Mid = (float)(0.4 + 0.3 * Math.Sin(time * 0.7)),
                Treble = (float)(0.3 + 0.2 * Math.Sin(time * 1.2)),
                LeftChannel = leftChannel,
                RightChannel = rightChannel,
                CenterChannel = centerChannel
            };
        }

        public static void ShowInteractiveMenu()
        {
            Console.WriteLine("\n🎮 Phoenix EffectsGraph Interactive Menu");
            Console.WriteLine("1. Run Live Demo");
            Console.WriteLine("2. Show Graph Info");
            Console.WriteLine("3. Test Individual Graphs");
            Console.WriteLine("4. Performance Benchmark");
            Console.WriteLine("5. Exit");
            Console.Write("\nSelect option (1-5): ");

            var input = Console.ReadLine();
            switch (input)
            {
                case "1":
                    _ = RunLiveDemoAsync();
                    break;
                case "2":
                    ShowGraphInfo();
                    break;
                case "3":
                    TestIndividualGraphs();
                    break;
                case "4":
                    RunPerformanceBenchmark();
                    break;
                case "5":
                    Console.WriteLine("👋 Goodbye!");
                    break;
                default:
                    Console.WriteLine("❌ Invalid option");
                    break;
            }
        }

        private static void ShowGraphInfo()
        {
            Console.WriteLine("\n📊 EffectsGraph System Information");
            Console.WriteLine("==================================");
            
            var manager = new EffectsGraphManager();
            var availableNodes = manager.GetAvailableNodeTypes();
            
            Console.WriteLine($"Available Node Types: {availableNodes.Count}");
            Console.WriteLine("Categories:");
            
            var categories = new HashSet<string>();
            foreach (var node in availableNodes.Values)
            {
                categories.Add(node.Category);
            }
            
            foreach (var category in categories)
            {
                var count = availableNodes.Values.Count(n => n.Category == category);
                Console.WriteLine($"  {category}: {count} nodes");
            }
        }

        private static void TestIndividualGraphs()
        {
            Console.WriteLine("\n🧪 Testing Individual Graphs");
            Console.WriteLine("=============================");

            var graphs = new[]
            {
                EffectsGraphDemo.CreateStarfieldDemo(),
                EffectsGraphDemo.CreateParallelDemo(),
                EffectsGraphDemo.CreateComplexComposition()
            };

            foreach (var graph in graphs)
            {
                Console.WriteLine($"\n🔍 Testing: {graph.Name}");
                Console.WriteLine($"Description: {graph.Description}");
                
                var isValid = graph.ValidateGraph();
                Console.WriteLine($"Valid: {(isValid ? "✅ Yes" : "❌ No")}");
                
                if (!isValid)
                {
                    var errors = graph.GetValidationErrors();
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"  Error: {error}");
                    }
                }
                
                var stats = graph.GetStatistics();
                Console.WriteLine($"Statistics: {stats}");
            }
        }

        private static void RunPerformanceBenchmark()
        {
            Console.WriteLine("\n⚡ Performance Benchmark");
            Console.WriteLine("========================");

            var graph = EffectsGraphDemo.CreateComplexComposition();
            var iterations = 1000;
            
            Console.WriteLine($"Running {iterations} iterations...");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var audioFeatures = CreateSimulatedAudioFeatures(i);
                var results = graph.ProcessGraph(audioFeatures);
            }
            
            stopwatch.Stop();
            
            var totalTime = stopwatch.Elapsed.TotalMilliseconds;
            var avgTime = totalTime / iterations;
            var fps = 1000.0 / avgTime;
            
            Console.WriteLine($"Total Time: {totalTime:F2}ms");
            Console.WriteLine($"Average Time: {avgTime:F3}ms per frame");
            Console.WriteLine($"Theoretical FPS: {fps:F1}");
        }
    }
}