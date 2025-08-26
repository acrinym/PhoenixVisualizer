using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Nodes;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Services
{
    /// <summary>
    /// Phoenix-native execution engine that manages effect nodes and expression engine binding
    /// </summary>
    public class PhoenixExecutionEngine
    {
        private readonly List<BaseEffectNode> _nodes = new();
        private readonly PhoenixExpressionEngine _engine = new PhoenixExpressionEngine();
        private long _frameCounter = 0;
        private double _timeSeconds = 0.0;

        /// <summary>
        /// Add a node to the execution engine
        /// </summary>
        public void AddNode(BaseEffectNode node)
        {
            if (node != null)
            {
                _nodes.Add(node);
                // Bind the expression engine immediately
                node.BindExpressionEngine(_engine);
            }
        }

        /// <summary>
        /// Remove a node from the execution engine
        /// </summary>
        public void RemoveNode(BaseEffectNode node)
        {
            if (node != null)
            {
                _nodes.Remove(node);
            }
        }

        /// <summary>
        /// Bind the expression engine to all existing nodes
        /// </summary>
        public void BindEngineToNodes()
        {
            foreach (var node in _nodes)
                node.BindExpressionEngine(_engine);
        }

        /// <summary>
        /// Run a single frame with audio features
        /// </summary>
        public void RunFrame(AudioFeatures audioFeatures, double deltaTime = 1.0/60.0)
        {
            _frameCounter++;
            _timeSeconds += deltaTime;

            // Inject AVS-compat vars
            _engine.SetVar("rms", audioFeatures.Rms);
            _engine.SetVar("beat", audioFeatures.IsBeat ? 1.0 : 0.0);
            
            // Calculate frequency bands from spectrum data
            if (audioFeatures.SpectrumData.Length > 0)
            {
                // Bass: 0-20% of spectrum
                int bassEnd = Math.Max(1, audioFeatures.SpectrumData.Length / 5);
                float bass = CalculateFrequencyBand(audioFeatures.SpectrumData, 0, bassEnd);
                _engine.SetVar("bass", bass);
                
                // Mid: 20-80% of spectrum
                int midStart = bassEnd;
                int midEnd = audioFeatures.SpectrumData.Length * 4 / 5;
                float mid = CalculateFrequencyBand(audioFeatures.SpectrumData, midStart, midEnd);
                _engine.SetVar("mid", mid);
                
                // Treble: 80-100% of spectrum
                float treb = CalculateFrequencyBand(audioFeatures.SpectrumData, midEnd, audioFeatures.SpectrumData.Length);
                _engine.SetVar("treb", treb);
            }

            for (int i = 0; i < audioFeatures.SpectrumData.Length; i++)
                _engine.SetVar($"spec{i}", audioFeatures.SpectrumData[i]);

            for (int i = 0; i < audioFeatures.WaveformData.Length; i++)
                _engine.SetVar($"wave{i}", audioFeatures.WaveformData[i]);

            // Inject Phoenix-native vars
            _engine.SetVar("pel_frame", _frameCounter);
            _engine.SetVar("pel_time", _timeSeconds);
            _engine.SetVar("pel_dt", deltaTime);

            // Run all nodes
            foreach (var node in _nodes)
                node.Process(new Dictionary<string, object>(), audioFeatures);
        }

        /// <summary>
        /// Get the current frame counter
        /// </summary>
        public long FrameCounter => _frameCounter;

        /// <summary>
        /// Get the current time in seconds
        /// </summary>
        public double TimeSeconds => _timeSeconds;

        /// <summary>
        /// Get the expression engine instance
        /// </summary>
        public PhoenixExpressionEngine ExpressionEngine => _engine;

        /// <summary>
        /// Get the number of nodes
        /// </summary>
        public int NodeCount => _nodes.Count;

        /// <summary>
        /// Clear all nodes
        /// </summary>
        public void Clear()
        {
            _nodes.Clear();
            _frameCounter = 0;
            _timeSeconds = 0.0;
        }

        /// <summary>
        /// Reset the execution engine
        /// </summary>
        public void Reset()
        {
            foreach (var node in _nodes)
                node.Reset();
            
            _frameCounter = 0;
            _timeSeconds = 0.0;
        }

        /// <summary>
        /// Calculate the average value of a frequency band
        /// </summary>
        private float CalculateFrequencyBand(float[] spectrum, int start, int end)
        {
            if (start >= end || start < 0 || end > spectrum.Length)
                return 0.0f;

            float sum = 0.0f;
            int count = 0;
            
            for (int i = start; i < end; i++)
            {
                sum += spectrum[i];
                count++;
            }
            
            return count > 0 ? sum / count : 0.0f;
        }
    }
}
