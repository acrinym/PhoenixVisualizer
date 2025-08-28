using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Graph
{
    /// <summary>
    /// Demo class showing how to use the EffectsGraph system
    /// </summary>
    public static class EffectsGraphDemo
    {
        /// <summary>
        /// Create a demo starfield effects chain
        /// </summary>
        public static EffectsGraph CreateStarfieldDemo()
        {
            var manager = new EffectsGraphManager();
            
            // Register available node types
            manager.RegisterNodeType(new StarfieldEffectsNode());
            manager.RegisterNodeType(new ParticleSwarmEffectsNode());
            manager.RegisterNodeType(new OscilloscopeStarEffectsNode());
            
            // Create a simple chain
            var graph = manager.CreateEffectChain("Starfield Demo", 
                "StarfieldEffectsNode", 
                "ParticleSwarmEffectsNode", 
                "OscilloscopeStarEffectsNode");
            
            return graph;
        }

        /// <summary>
        /// Create a parallel effects demo
        /// </summary>
        public static EffectsGraph CreateParallelDemo()
        {
            var manager = new EffectsGraphManager();
            
            // Register available node types
            manager.RegisterNodeType(new StarfieldEffectsNode());
            manager.RegisterNodeType(new ParticleSwarmEffectsNode());
            manager.RegisterNodeType(new OscilloscopeStarEffectsNode());
            
            // Create parallel effects
            var graph = manager.CreateParallelEffects("Parallel Demo",
                "StarfieldEffectsNode",  // Input node
                "OscilloscopeStarEffectsNode",  // Output node
                "StarfieldEffectsNode",  // Parallel branch 1
                "ParticleSwarmEffectsNode");  // Parallel branch 2
            
            return graph;
        }

        /// <summary>
        /// Create a complex effects composition
        /// </summary>
        public static EffectsGraph CreateComplexComposition()
        {
            var manager = new EffectsGraphManager();
            
            // Register available node types
            manager.RegisterNodeType(new StarfieldEffectsNode());
            manager.RegisterNodeType(new ParticleSwarmEffectsNode());
            manager.RegisterNodeType(new OscilloscopeStarEffectsNode());
            manager.RegisterNodeType(new ColorFadeEffectsNode());
            manager.RegisterNodeType(new BlurEffectsNode());
            
            // Create a new graph
            var graph = manager.CreateGraph("Complex Composition", "Multiple effect chains with cross-connections");
            
            // Add nodes
            var starfield = new StarfieldEffectsNode();
            var particles = new ParticleSwarmEffectsNode();
            var oscilloscope = new OscilloscopeStarEffectsNode();
            var colorFade = new ColorFadeEffectsNode();
            var blur = new BlurEffectsNode();
            
            graph.AddNode(starfield);
            graph.AddNode(particles);
            graph.AddNode(oscilloscope);
            graph.AddNode(colorFade);
            graph.AddNode(blur);
            
            // Create connections
            graph.AddConnection("StarfieldEffectsNode", "Output", "ParticleSwarmEffectsNode", "Background");
            graph.AddConnection("ParticleSwarmEffectsNode", "Output", "ColorFadeEffectsNode", "Input");
            graph.AddConnection("ColorFadeEffectsNode", "Output", "BlurEffectsNode", "Input");
            graph.AddConnection("BlurEffectsNode", "Output", "OscilloscopeStarEffectsNode", "Background");
            
            // Add some cross-connections
            graph.AddConnection("StarfieldEffectsNode", "Output", "OscilloscopeStarEffectsNode", "Background");
            
            return graph;
        }

        /// <summary>
        /// Run a demo of the effects graph system
        /// </summary>
        public static void RunDemo()
        {
            Console.WriteLine("=== Phoenix Visualizer Effects Graph Demo ===\n");
            
            // Create demo graphs
            var starfieldGraph = CreateStarfieldDemo();
            var parallelGraph = CreateParallelDemo();
            var complexGraph = CreateComplexComposition();
            
            // Display graph information
            Console.WriteLine("1. Starfield Demo Graph:");
            DisplayGraphInfo(starfieldGraph);
            
            Console.WriteLine("\n2. Parallel Effects Graph:");
            DisplayGraphInfo(parallelGraph);
            
            Console.WriteLine("\n3. Complex Composition Graph:");
            DisplayGraphInfo(complexGraph);
            
            // Create a manager and add all graphs
            var manager = new EffectsGraphManager();
            manager.RegisterNodeType(new StarfieldEffectsNode());
            manager.RegisterNodeType(new ParticleSwarmEffectsNode());
            manager.RegisterNodeType(new OscilloscopeStarEffectsNode());
            manager.RegisterNodeType(new ColorFadeEffectsNode());
            manager.RegisterNodeType(new BlurEffectsNode());
            
            // Validate all graphs
            var validationResults = manager.ValidateAllGraphs();
            Console.WriteLine("\n=== Validation Results ===");
            foreach (var result in validationResults)
            {
                Console.WriteLine($"\n{result.Key}:");
                foreach (var error in result.Value)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            
            // Get manager statistics
            var stats = manager.GetManagerStatistics();
            Console.WriteLine($"\n=== Manager Statistics ===\n{stats}");
        }

        /// <summary>
        /// Display information about a graph
        /// </summary>
        private static void DisplayGraphInfo(EffectsGraph graph)
        {
            Console.WriteLine($"  Name: {graph.Name}");
            Console.WriteLine($"  Description: {graph.Description}");
            Console.WriteLine($"  Nodes: {graph.GetNodes().Count}");
            Console.WriteLine($"  Connections: {graph.GetConnections().Count}");
            Console.WriteLine($"  Valid: {graph.ValidateGraph()}");
            
            var stats = graph.GetStatistics();
            Console.WriteLine($"  Last Processed: {stats.LastProcessed:HH:mm:ss}");
            Console.WriteLine($"  Processing Time: {stats.ProcessingTime.TotalMilliseconds:F2}ms");
        }
    }
}