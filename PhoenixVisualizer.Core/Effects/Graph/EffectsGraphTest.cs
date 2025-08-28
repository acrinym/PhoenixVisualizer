using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Graph
{
    /// <summary>
    /// Simple test class to verify EffectsGraph functionality
    /// </summary>
    public static class EffectsGraphTest
    {
        /// <summary>
        /// Test basic graph creation and node management
        /// </summary>
        public static bool TestBasicGraphOperations()
        {
            try
            {
                Console.WriteLine("Testing basic graph operations...");
                
                var graph = new EffectsGraph();
                graph.Name = "Test Graph";
                graph.Description = "A test graph for verification";
                
                // Test node addition
                var starfieldNode = new StarfieldEffectsNode();
                var particleNode = new ParticleSwarmEffectsNode();
                
                bool addResult1 = graph.AddNode(starfieldNode);
                bool addResult2 = graph.AddNode(particleNode);
                
                if (!addResult1 || !addResult2)
                {
                    Console.WriteLine("Failed to add nodes");
                    return false;
                }
                
                // Test connection creation
                bool connectionResult = graph.AddConnection(
                    starfieldNode.Id, "Output",
                    particleNode.Id, "Background"
                );
                
                if (!connectionResult)
                {
                    Console.WriteLine("Failed to create connection");
                    return false;
                }
                
                // Test graph validation
                bool isValid = graph.ValidateGraph();
                if (!isValid)
                {
                    Console.WriteLine("Graph validation failed");
                    var errors = graph.GetValidationErrors();
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                    return false;
                }
                
                // Test statistics
                var stats = graph.GetStatistics();
                Console.WriteLine($"Graph stats: {stats}");
                
                Console.WriteLine("Basic graph operations test passed!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Basic graph operations test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Test graph processing
        /// </summary>
        public static bool TestGraphProcessing()
        {
            try
            {
                Console.WriteLine("Testing graph processing...");
                
                var graph = new EffectsGraph();
                var starfieldNode = new StarfieldEffectsNode();
                var particleNode = new ParticleSwarmEffectsNode();
                
                graph.AddNode(starfieldNode);
                graph.AddNode(particleNode);
                graph.AddConnection(starfieldNode.Id, "Output", particleNode.Id, "Background");
                
                // Create mock audio features
                var audioFeatures = new AudioFeatures
                {
                    Beat = true,
                    BeatIntensity = 0.8f,
                    RMS = 0.6f,
                    Bass = 0.7f,
                    Mid = 0.5f,
                    Treble = 0.4f,
                    LeftChannel = new float[1024],
                    RightChannel = new float[1024],
                    CenterChannel = new float[1024]
                };
                
                // Process the graph
                var results = graph.ProcessGraph(audioFeatures);
                
                Console.WriteLine($"Graph processing completed. Results count: {results.Count}");
                Console.WriteLine($"Processing time: {graph.ProcessingTime.TotalMilliseconds:F2}ms");
                
                Console.WriteLine("Graph processing test passed!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Graph processing test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Test graph manager functionality
        /// </summary>
        public static bool TestGraphManager()
        {
            try
            {
                Console.WriteLine("Testing graph manager...");
                
                var manager = new EffectsGraphManager();
                
                // Register node types
                manager.RegisterNodeType(new StarfieldEffectsNode());
                manager.RegisterNodeType(new ParticleSwarmEffectsNode());
                manager.RegisterNodeType(new OscilloscopeStarEffectsNode());
                
                // Create graphs using manager
                var chainGraph = manager.CreateEffectChain("Test Chain", 
                    "StarfieldEffectsNode", "ParticleSwarmEffectsNode");
                
                var parallelGraph = manager.CreateParallelEffects("Test Parallel",
                    "StarfieldEffectsNode", "OscilloscopeStarEffectsNode",
                    "StarfieldEffectsNode", "ParticleSwarmEffectsNode");
                
                // Test manager statistics
                var managerStats = manager.GetManagerStatistics();
                Console.WriteLine($"Manager stats: {managerStats}");
                
                // Test validation
                var validationResults = manager.ValidateAllGraphs();
                foreach (var result in validationResults)
                {
                    Console.WriteLine($"Validation for {result.Key}: {result.Value.Count} errors");
                }
                
                Console.WriteLine("Graph manager test passed!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Graph manager test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Run all tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== EffectsGraph System Tests ===\n");
            
            bool test1 = TestBasicGraphOperations();
            bool test2 = TestGraphProcessing();
            bool test3 = TestGraphManager();
            
            Console.WriteLine("\n=== Test Results ===");
            Console.WriteLine($"Basic Graph Operations: {(test1 ? "PASS" : "FAIL")}");
            Console.WriteLine($"Graph Processing: {(test2 ? "PASS" : "FAIL")}");
            Console.WriteLine($"Graph Manager: {(test3 ? "PASS" : "FAIL")}");
            
            bool allPassed = test1 && test2 && test3;
            Console.WriteLine($"\nOverall Result: {(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED")}");
        }
    }
}