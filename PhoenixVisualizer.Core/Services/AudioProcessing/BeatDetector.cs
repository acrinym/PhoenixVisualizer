using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PhoenixVisualizer.Core.Services.AudioProcessing
{
    /// <summary>
    /// Real-time beat detection and BPM calculation for audio analysis
    /// Uses energy-based detection with adaptive thresholding and inter-beat interval analysis
    /// </summary>
    public class BeatDetector : IDisposable
    {
        private readonly CircularBuffer<float> _energyHistory;
        private readonly CircularBuffer<DateTime> _beatTimestamps;
        private readonly List<float> _interBeatIntervals;
        
        // Configuration parameters
        private readonly float _beatThresholdMultiplier;
        private readonly int _energyHistorySize;
        private readonly int _beatHistorySize;
        private readonly float _minBPM;
        private readonly float _maxBPM;
        
        // Current state
        private volatile bool _isBeatDetected;
        private volatile float _currentBPM;
        private volatile float _currentEnergy;
        private volatile float _averageEnergy;
        private volatile float _beatThreshold;
        private DateTime _lastBeatTime;
        private volatile bool _isInitialized;
        
        // Performance metrics
        private readonly Stopwatch _processingTimer;
        private double _averageProcessingTime;
        private int _totalBeatsDetected;
        // removed unused field falsePositiveCount
        
        private bool _isDisposed;
        
        /// <summary>
        /// Gets whether a beat was detected in the current frame
        /// </summary>
        public bool IsBeatDetected => _isBeatDetected;
        
        /// <summary>
        /// Gets the current calculated BPM
        /// </summary>
        public float CurrentBPM => _currentBPM;
        
        /// <summary>
        /// Gets the current audio energy level
        /// </summary>
        public float CurrentEnergy => _currentEnergy;
        
        /// <summary>
        /// Gets the average energy level over the history window
        /// </summary>
        public float AverageEnergy => _averageEnergy;
        
        /// <summary>
        /// Gets the current beat detection threshold
        /// </summary>
        public float BeatThreshold => _beatThreshold;
        
        /// <summary>
        /// Gets the timestamp of the last detected beat
        /// </summary>
        public DateTime LastBeatTime => _lastBeatTime;
        
        /// <summary>
        /// Gets the total number of beats detected since initialization
        /// </summary>
        public int TotalBeatsDetected => _totalBeatsDetected;
        
        /// <summary>
        /// Gets the beat detection confidence (0.0 to 1.0)
        /// </summary>
        public float BeatConfidence { get; private set; }
        
        /// <summary>
        /// Event raised when a beat is detected
        /// </summary>
        public event EventHandler<BeatDetectedEventArgs>? BeatDetected;
        
        /// <summary>
        /// Initializes a new beat detector with default parameters
        /// </summary>
        public BeatDetector() : this(1.5f, 32, 16, 60.0f, 200.0f)
        {
        }
        
        /// <summary>
        /// Initializes a new beat detector with custom parameters
        /// </summary>
        /// <param name="beatThresholdMultiplier">Multiplier for adaptive threshold (default 1.5)</param>
        /// <param name="energyHistorySize">Size of energy history buffer (default 32)</param>
        /// <param name="beatHistorySize">Size of beat timestamp history (default 16)</param>
        /// <param name="minBPM">Minimum valid BPM (default 60.0)</param>
        /// <param name="maxBPM">Maximum valid BPM (default 200.0)</param>
        public BeatDetector(float beatThresholdMultiplier, int energyHistorySize, int beatHistorySize, float minBPM, float maxBPM)
        {
            _beatThresholdMultiplier = beatThresholdMultiplier;
            _energyHistorySize = energyHistorySize;
            _beatHistorySize = beatHistorySize;
            _minBPM = minBPM;
            _maxBPM = maxBPM;
            
            // Initialize buffers
            _energyHistory = new CircularBuffer<float>(energyHistorySize);
            _beatTimestamps = new CircularBuffer<DateTime>(beatHistorySize);
            _interBeatIntervals = new List<float>();
            
            // Initialize state
            _currentBPM = 120.0f; // Default BPM
            _beatThreshold = 0.1f; // Initial threshold
            _lastBeatTime = DateTime.MinValue;
            _isInitialized = false;
            
            // Initialize performance monitoring
            _processingTimer = new Stopwatch();
            
            // Initialize with some default values
            for (int i = 0; i < energyHistorySize; i++)
            {
                _energyHistory.Add(0.1f);
            }
        }
        
        /// <summary>
        /// Processes a frame of audio data for beat detection
        /// </summary>
        /// <param name="leftChannel">Left channel audio samples</param>
        /// <param name="rightChannel">Right channel audio samples</param>
        /// <returns>True if a beat was detected, false otherwise</returns>
        public async Task<bool> ProcessFrameAsync(float[] leftChannel, float[] rightChannel)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(BeatDetector));
            
            return await Task.Run(() => ProcessFrameSynchronous(leftChannel, rightChannel));
        }
        
        /// <summary>
        /// Processes a frame of audio data synchronously
        /// </summary>
        /// <param name="leftChannel">Left channel audio samples</param>
        /// <param name="rightChannel">Right channel audio samples</param>
        /// <returns>True if a beat was detected, false otherwise</returns>
        public bool ProcessFrameSynchronous(float[] leftChannel, float[] rightChannel)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(BeatDetector));
            
            _processingTimer.Restart();
            
            try
            {
                // Calculate frame energy
                _currentEnergy = CalculateFrameEnergy(leftChannel, rightChannel);
                
                // Update energy history
                _energyHistory.Add(_currentEnergy);
                
                // Calculate average energy
                _averageEnergy = _energyHistory.GetEnumerable().Average();
                
                // Update beat threshold adaptively
                UpdateBeatThreshold();
                
                // Detect beat
                bool wasBeatDetected = _isBeatDetected;
                _isBeatDetected = DetectBeat();
                
                // Process beat detection
                if (_isBeatDetected && !wasBeatDetected)
                {
                    ProcessBeatDetection();
                }
                
                // Update BPM calculation
                UpdateBPM();
                
                // Calculate beat confidence
                CalculateBeatConfidence();
                
                return _isBeatDetected;
            }
            finally
            {
                _processingTimer.Stop();
                UpdateProcessingMetrics(_processingTimer.Elapsed.TotalMilliseconds);
            }
        }
        
        /// <summary>
        /// Calculates the energy of a frame from left and right channel data
        /// </summary>
        private float CalculateFrameEnergy(float[] leftChannel, float[] rightChannel)
        {
            if (leftChannel == null || rightChannel == null || leftChannel.Length == 0)
                return 0.0f;
            
            float totalEnergy = 0.0f;
            int sampleCount = Math.Min(leftChannel.Length, rightChannel.Length);
            
            // Calculate RMS energy for both channels
            for (int i = 0; i < sampleCount; i++)
            {
                float leftSample = leftChannel[i];
                float rightSample = rightChannel[i];
                
                // Square the samples and accumulate
                totalEnergy += leftSample * leftSample + rightSample * rightSample;
            }
            
            // Calculate RMS energy
            float rmsEnergy = (float)Math.Sqrt(totalEnergy / (sampleCount * 2));
            
            return rmsEnergy;
        }
        
        /// <summary>
        /// Updates the beat detection threshold adaptively
        /// </summary>
        private void UpdateBeatThreshold()
        {
            // Adaptive threshold based on energy history
            float energyVariance = CalculateEnergyVariance();
            float adaptiveThreshold = _averageEnergy + (energyVariance * 0.5f);
            
            // Apply threshold multiplier
            _beatThreshold = adaptiveThreshold * _beatThresholdMultiplier;
            
            // Ensure minimum threshold
            _beatThreshold = Math.Max(_beatThreshold, 0.01f);
        }
        
        /// <summary>
        /// Calculates the variance of energy values in the history buffer
        /// </summary>
        private float CalculateEnergyVariance()
        {
            if (_energyHistory.Count < 2)
                return 0.0f;
            
            float mean = _averageEnergy;
            float variance = 0.0f;
            
            foreach (float energy in _energyHistory.GetEnumerable())
            {
                float diff = energy - mean;
                variance += diff * diff;
            }
            
            return variance / (_energyHistory.Count - 1);
        }
        
        /// <summary>
        /// Detects if a beat occurred in the current frame
        /// </summary>
        private bool DetectBeat()
        {
            // Check if current energy exceeds threshold
            if (_currentEnergy <= _beatThreshold)
                return false;
            
            // Check if enough time has passed since last beat (minimum beat interval)
            if (_lastBeatTime != DateTime.MinValue)
            {
                float timeSinceLastBeat = (float)(DateTime.Now - _lastBeatTime).TotalSeconds;
                float minBeatInterval = 60.0f / _maxBPM; // Minimum time between beats
                
                if (timeSinceLastBeat < minBeatInterval)
                    return false;
            }
            
            // Additional validation: check if energy is significantly higher than recent average
            float energyRatio = _currentEnergy / _averageEnergy;
            if (energyRatio < 1.2f) // Must be at least 20% higher than average
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Processes beat detection and updates internal state
        /// </summary>
        private void ProcessBeatDetection()
        {
            var now = DateTime.Now;
            
            // Update beat timestamp
            _lastBeatTime = now;
            _beatTimestamps.Add(now);
            
            // Calculate inter-beat interval if we have at least 2 beats
            if (_beatTimestamps.Count >= 2)
            {
                var previousBeat = _beatTimestamps[_beatTimestamps.Count - 2];
                float interval = (float)(now - previousBeat).TotalSeconds;
                
                // Validate interval is within reasonable BPM range
                if (interval >= 60.0f / _maxBPM && interval <= 60.0f / _minBPM)
                {
                    _interBeatIntervals.Add(interval);
                    
                    // Keep only recent intervals for BPM calculation
                    if (_interBeatIntervals.Count > _beatHistorySize)
                    {
                        _interBeatIntervals.RemoveAt(0);
                    }
                }
            }
            
            // Update statistics
            _totalBeatsDetected++;
            
            // Raise beat detected event
            OnBeatDetected(new BeatDetectedEventArgs(now, _currentEnergy, _currentBPM));
        }
        
        /// <summary>
        /// Updates BPM calculation based on inter-beat intervals
        /// </summary>
        private void UpdateBPM()
        {
            if (_interBeatIntervals.Count < 3)
                return; // Need at least 3 intervals for reliable BPM
            
            // Calculate median interval to filter out outliers
            var sortedIntervals = _interBeatIntervals.OrderBy(x => x).ToList();
            float medianInterval = sortedIntervals[sortedIntervals.Count / 2];
            
            // Convert to BPM
            float newBPM = 60.0f / medianInterval;
            
            // Validate BPM is within reasonable range
            if (newBPM >= _minBPM && newBPM <= _maxBPM)
            {
                // Smooth BPM changes to avoid sudden jumps
                if (_isInitialized)
                {
                    _currentBPM = _currentBPM * 0.7f + newBPM * 0.3f;
                }
                else
                {
                    _currentBPM = newBPM;
                    _isInitialized = true;
                }
            }
        }
        
        /// <summary>
        /// Calculates beat detection confidence based on recent performance
        /// </summary>
        private void CalculateBeatConfidence()
        {
            if (_totalBeatsDetected == 0)
            {
                BeatConfidence = 0.0f;
                return;
            }
            
            // Calculate confidence based on consistency (falsePositiveCount was removed)
            float falsePositiveRatio = 0.0f; // No false positives tracked
            float consistencyScore = CalculateConsistencyScore();
            
            BeatConfidence = Math.Max(0.0f, 1.0f - falsePositiveRatio) * consistencyScore;
        }
        
        /// <summary>
        /// Calculates consistency score based on BPM stability
        /// </summary>
        private float CalculateConsistencyScore()
        {
            if (_interBeatIntervals.Count < 3)
                return 0.5f;
            
            // Calculate coefficient of variation (lower is better)
            float mean = _interBeatIntervals.Average();
            float variance = _interBeatIntervals.Select(x => (x - mean) * (x - mean)).Average();
            float stdDev = (float)Math.Sqrt(variance);
            float cv = mean > 0 ? stdDev / mean : 1.0f;
            
            // Convert to consistency score (0.0 to 1.0)
            return Math.Max(0.0f, 1.0f - cv);
        }
        
        /// <summary>
        /// Updates processing performance metrics
        /// </summary>
        private void UpdateProcessingMetrics(double processingTime)
        {
            // Exponential moving average for processing time
            _averageProcessingTime = (_averageProcessingTime * 0.9) + (processingTime * 0.1);
            
            // Performance validation
            if (_averageProcessingTime > 16.0) // Target: <16ms
            {
                Debug.WriteLine($"Warning: Beat detection processing time ({_averageProcessingTime:F2}ms) exceeds target (16ms)");
            }
        }
        
        /// <summary>
        /// Raises the BeatDetected event
        /// </summary>
        protected virtual void OnBeatDetected(BeatDetectedEventArgs e)
        {
            BeatDetected?.Invoke(this, e);
        }
        
        /// <summary>
        /// Resets the beat detector to initial state
        /// </summary>
        public void Reset()
        {
            _energyHistory.Clear();
            _beatTimestamps.Clear();
            _interBeatIntervals.Clear();
            
            _isBeatDetected = false;
            _currentBPM = 120.0f;
            _beatThreshold = 0.1f;
            _lastBeatTime = DateTime.MinValue;
            _isInitialized = false;
            
            // Re-initialize energy history
            for (int i = 0; i < _energyHistorySize; i++)
            {
                _energyHistory.Add(0.1f);
            }
        }
        
        /// <summary>
        /// Gets performance statistics for monitoring
        /// </summary>
        public BeatDetectorStats GetStats()
        {
            return new BeatDetectorStats
            {
                TotalBeatsDetected = _totalBeatsDetected,
                                 FalsePositiveCount = 0, // No false positives tracked
                BeatConfidence = BeatConfidence,
                AverageProcessingTime = _averageProcessingTime,
                CurrentBPM = _currentBPM,
                BeatThreshold = _beatThreshold,
                CurrentEnergy = _currentEnergy,
                AverageEnergy = _averageEnergy
            };
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                // Stopwatch doesn't need disposal
            }
        }
    }
    
    /// <summary>
    /// Event arguments for beat detection events
    /// </summary>
    public class BeatDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Timestamp when the beat was detected
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Energy level when the beat was detected
        /// </summary>
        public float Energy { get; }
        
        /// <summary>
        /// Current BPM at the time of beat detection
        /// </summary>
        public float BPM { get; }
        
        public BeatDetectedEventArgs(DateTime timestamp, float energy, float bpm)
        {
            Timestamp = timestamp;
            Energy = energy;
            BPM = bpm;
        }
    }
    
    /// <summary>
    /// Statistics for the beat detector
    /// </summary>
    public class BeatDetectorStats
    {
        public int TotalBeatsDetected { get; set; }
        public int FalsePositiveCount { get; set; }
        public float BeatConfidence { get; set; }
        public double AverageProcessingTime { get; set; }
        public float CurrentBPM { get; set; }
        public float BeatThreshold { get; set; }
        public float CurrentEnergy { get; set; }
        public float AverageEnergy { get; set; }
    }
    
    /// <summary>
    /// Circular buffer implementation for efficient data storage
    /// </summary>
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _tail;
        private int _count;
        
        public int Count => _count;
        public int Capacity => _buffer.Length;
        
        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }
        
        public void Add(T item)
        {
            _buffer[_tail] = item;
            _tail = (_tail + 1) % _buffer.Length;
            
            if (_count < _buffer.Length)
                _count++;
            else
                _head = (_head + 1) % _buffer.Length;
        }
        
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                int actualIndex = (_head + index) % _buffer.Length;
                return _buffer[actualIndex];
            }
        }
        
        public void Clear()
        {
            _head = 0;
            _tail = 0;
            _count = 0;
        }
        
        public IEnumerable<T> GetEnumerable()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return this[i];
            }
        }
    }
}
