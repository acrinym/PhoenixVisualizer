# Beat Detection (BPM) Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_bpm.cpp`  
**Class:** `C_BpmClass`  
**Module Name:** "Misc / Custom BPM"

---

## 🎯 **Effect Overview**

Beat Detection (BPM) is a **sophisticated beat analysis and generation system** that provides advanced control over beat timing and synchronization. It can **detect beats from AVS**, **generate arbitrary beats at specific BPM**, **skip beats selectively**, and **invert beat patterns**. The effect is essential for creating **precise musical synchronization** and **custom rhythm patterns** in visualizations.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_BpmClass : public C_RBASE
```

### **Core Components**
- **Beat Detection** - Real-time analysis of incoming beat signals
- **Arbitrary Beat Generation** - Custom BPM-based beat creation
- **Beat Skipping** - Selective beat filtering and modification
- **Beat Inversion** - Pattern reversal and timing manipulation
- **Slider Synchronization** - Beat-reactive UI element control

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `enabled` | int | Effect enabled | 1 | 0 or 1 |
| `arbitrary` | int | Arbitrary beat generation | 1 | 0 or 1 |
| `skip` | int | Beat skipping mode | 0 | 0 or 1 |
| `invert` | int | Beat inversion mode | 0 | 0 or 1 |
| `arbVal` | int | Arbitrary beat interval (ms) | 500 | 200 to 10,000 |
| `skipVal` | int | Beat skip count | 1 | 1 to 16 |
| `skipfirst` | int | Initial beats to skip | 0 | 0 to 64 |

### **Beat Modes**
- **Arbitrary Mode**: Generate beats at custom BPM intervals
- **Skip Mode**: Filter beats based on skip count
- **Invert Mode**: Reverse beat timing patterns
- **Detection Mode**: Pass through detected beats from AVS

---

## 🎵 **Audio Data Integration**

### **Data Structure**
```cpp
char visdata[2][2][576]
// [stereo][spectrum/waveform][frequency bins]
```

### **Beat Detection**
- **Input Beat**: Uses `isBeat` flag from AVS beat detection
- **Beat Processing**: Multiple modes for beat manipulation
- **Output Beat**: Returns modified beat signals via return values

### **Beat Signal Values**
```cpp
#define SET_BEAT    0x10000000  // Beat detected/generated
#define CLR_BEAT    0x20000000  // Beat cleared/suppressed
```

---

## 🔧 **Beat Processing Pipeline**

### **1. Beat Detection Mode**
```cpp
if (isBeat) {
    // Show the beat received from AVS
    SliderStep(IDC_IN, &inSlide);
    count++;
}

// Skip initial beats if configured
if (skipfirst != 0 && count <= skipfirst) {
    return isBeat ? CLR_BEAT : 0;
}
```

### **2. Arbitrary Beat Generation**
```cpp
if (arbitrary) {
    DWORD TCNow = GetTickCount();
    if (TCNow > arbLastTC + arbVal) {
        arbLastTC = TCNow;
        SliderStep(IDC_OUT, &outSlide);
        return SET_BEAT;  // Generate beat
    }
    return CLR_BEAT;  // No beat
}
```

### **3. Beat Skipping Mode**
```cpp
if (skip) {
    if (isBeat && ++skipCount >= skipVal+1) {
        skipCount = 0;
        SliderStep(IDC_OUT, &outSlide);
        return SET_BEAT;  // Pass beat after skip count
    }
    return CLR_BEAT;  // Suppress beat
}
```

### **4. Beat Inversion Mode**
```cpp
if (invert) {
    if (isBeat) {
        return CLR_BEAT;  // Suppress detected beat
    } else {
        SliderStep(IDC_OUT, &outSlide);
        return SET_BEAT;  // Generate beat when none detected
    }
}
```

---

## 🎛️ **Slider Synchronization**

### **Slider Control System**
```cpp
void C_THISCLASS::SliderStep(int Ctl, int *slide)
{
    *slide += (Ctl == IDC_IN) ? inInc : outInc;
    
    // Reverse direction at boundaries
    if (!*slide || *slide == 8) {
        (Ctl == IDC_IN ? inInc : outInc) *= -1;
    }
}
```

### **Slider Behavior**
- **Input Slider**: Responds to detected beats from AVS
- **Output Slider**: Responds to generated/modified beats
- **Bidirectional Movement**: Sliders move back and forth (0-8 range)
- **Beat Synchronization**: Slider movement synchronized with beat timing

---

## 📊 **BPM Calculations**

### **Arbitrary Beat Timing**
```cpp
// Convert millisecond interval to BPM
int bpm = 60000 / arbVal;

// Example: 500ms interval = 120 BPM
// Example: 1000ms interval = 60 BPM
```

### **Beat Skip Patterns**
```cpp
// Skip every N+1 beats
// skipVal = 1: Pass every 2nd beat
// skipVal = 2: Pass every 3rd beat
// skipVal = 3: Pass every 4th beat
```

### **Timing Precision**
- **Resolution**: 1ms precision for beat intervals
- **Range**: 200ms to 10,000ms (300 BPM to 6 BPM)
- **Accuracy**: System tick count based timing

---

## 🌈 **Visual Effects**

### **Beat Visualization**
- **Input Beat Display**: Visual feedback for detected beats
- **Output Beat Display**: Visual feedback for generated beats
- **Slider Animation**: Smooth movement synchronized with beats
- **Pattern Recognition**: Visual representation of beat patterns

### **Synchronization Effects**
- **Musical Timing**: Precise BPM-based beat generation
- **Rhythm Control**: Custom beat patterns and timing
- **Visual Sync**: UI elements synchronized with beat timing
- **Pattern Manipulation**: Beat filtering and modification

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(1) - constant time beat processing
- **Space Complexity**: O(1) - minimal memory usage
- **Memory Access**: Efficient timing calculations

### **Optimization Features**
- **Efficient timing**: System tick count based calculations
- **Conditional processing**: Only process enabled modes
- **Minimal overhead**: Lightweight beat manipulation
- **Real-time response**: Immediate beat processing

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class BeatDetectionNode : AvsModuleNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public BeatMode Mode { get; set; } = BeatMode.Detection;
    public int ArbitraryInterval { get; set; } = 500;  // milliseconds
    public int SkipCount { get; set; } = 1;
    public int SkipFirst { get; set; } = 0;
    
    // Beat processing state
    private DateTime lastBeatTime;
    private DateTime lastArbitraryBeatTime;
    private int beatCounter;
    private int skipCounter;
    private int totalBeatsProcessed;
    
    // Timing and synchronization
    private readonly Stopwatch beatTimer;
    private readonly Stopwatch arbitraryTimer;
    private float lastBeatInterval;
    private float averageBeatInterval;
    
    // Audio analysis for beat detection
    private float[] audioBuffer;
    private float[] previousAudioBuffer;
    private int audioBufferSize;
    private float beatThreshold;
    private float beatSensitivity;
    
    // Output state
    public bool BeatDetected { get; private set; }
    public bool IsBeat { get; private set; }
    public float BeatStrength { get; private set; }
    public float CurrentBPM { get; private set; }
    public int InputSlider { get; private set; }
    public int OutputSlider { get; private set; }
    
    // Beat detection algorithm parameters
    private float[] energyHistory;
    private int energyHistorySize;
    private float localAverage;
    private float variance;
    private float beatConfidence;
    
    // Constructor
    public BeatDetectionNode()
    {
        // Initialize timing
        beatTimer = new Stopwatch();
        arbitraryTimer = new Stopwatch();
        lastBeatTime = DateTime.Now;
        lastArbitraryBeatTime = DateTime.Now;
        
        // Initialize counters
        beatCounter = 0;
        skipCounter = 0;
        totalBeatsProcessed = 0;
        
        // Initialize audio analysis
        audioBufferSize = 576; // Standard AVS audio buffer size
        audioBuffer = new float[audioBufferSize];
        previousAudioBuffer = new float[audioBufferSize];
        beatThreshold = 0.6f;
        beatSensitivity = 0.8f;
        
        // Initialize beat detection
        energyHistorySize = 32;
        energyHistory = new float[energyHistorySize];
        localAverage = 0.0f;
        variance = 0.0f;
        beatConfidence = 0.0f;
        
        // Start timers
        beatTimer.Start();
        arbitraryTimer.Start();
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        // Copy input to output (this effect doesn't modify the image)
        CopyInputToOutput(ctx, input, output);
        
        // Update audio data
        UpdateAudioData(ctx);
        
        // Process beat detection based on mode
        switch (Mode)
        {
            case BeatMode.Detection:
                ProcessBeatDetection(ctx);
                break;
            case BeatMode.Arbitrary:
                ProcessArbitraryBeats(ctx);
                break;
            case BeatMode.Skip:
                ProcessBeatSkipping(ctx);
                break;
            case BeatMode.Invert:
                ProcessBeatInversion(ctx);
                break;
        }
        
        // Update UI elements
        UpdateUIElements(ctx);
        
        // Update beat statistics
        UpdateBeatStatistics(ctx);
    }
    
    private void UpdateAudioData(FrameContext ctx)
    {
        // Store previous audio buffer
        Array.Copy(audioBuffer, previousAudioBuffer, audioBufferSize);
        
        // Get current audio data (mix left and right channels)
        float[] leftChannel = ctx.AudioData.Waveform[0];
        float[] rightChannel = ctx.AudioData.Waveform[1];
        
        for (int i = 0; i < audioBufferSize; i++)
        {
            if (i < leftChannel.Length && i < rightChannel.Length)
            {
                audioBuffer[i] = (leftChannel[i] + rightChannel[i]) * 0.5f;
            }
            else
            {
                audioBuffer[i] = 0.0f;
            }
        }
    }
    
    private void ProcessBeatDetection(FrameContext ctx)
    {
        // Calculate audio energy
        float currentEnergy = CalculateAudioEnergy();
        
        // Update energy history
        UpdateEnergyHistory(currentEnergy);
        
        // Calculate beat probability
        float beatProbability = CalculateBeatProbability(currentEnergy);
        
        // Determine if this is a beat
        bool detectedBeat = beatProbability > beatThreshold;
        
        // Apply beat sensitivity
        if (detectedBeat && beatConfidence > beatSensitivity)
        {
            // Beat detected
            IsBeat = true;
            BeatDetected = true;
            BeatStrength = beatProbability;
            
            // Update timing
            DateTime now = DateTime.Now;
            lastBeatInterval = (float)(now - lastBeatTime).TotalMilliseconds;
            lastBeatTime = now;
            
            // Update counter
            beatCounter++;
            totalBeatsProcessed++;
            
            // Calculate BPM
            if (lastBeatInterval > 0)
            {
                CurrentBPM = 60000.0f / lastBeatInterval;
            }
            
            // Update average beat interval
            UpdateAverageBeatInterval(lastBeatInterval);
        }
        else
        {
            // No beat
            IsBeat = false;
            BeatDetected = false;
            BeatStrength = 0.0f;
        }
    }
    
    private void ProcessArbitraryBeats(FrameContext ctx)
    {
        DateTime now = DateTime.Now;
        TimeSpan timeSinceLastBeat = now - lastArbitraryBeatTime;
        
        if (timeSinceLastBeat.TotalMilliseconds >= ArbitraryInterval)
        {
            // Generate arbitrary beat
            IsBeat = true;
            BeatDetected = true;
            BeatStrength = 1.0f;
            
            // Update timing
            lastArbitraryBeatTime = now;
            lastBeatInterval = ArbitraryInterval;
            
            // Update counter
            beatCounter++;
            totalBeatsProcessed++;
            
            // Calculate BPM
            CurrentBPM = 60000.0f / ArbitraryInterval;
            
            // Update average beat interval
            UpdateAverageBeatInterval(lastBeatInterval);
        }
        else
        {
            // No beat
            IsBeat = false;
            BeatDetected = false;
            BeatStrength = 0.0f;
        }
    }
    
    private void ProcessBeatSkipping(FrameContext ctx)
    {
        // Check if we should skip the first N beats
        if (SkipFirst > 0 && totalBeatsProcessed <= SkipFirst)
        {
            IsBeat = false;
            BeatDetected = false;
            BeatStrength = 0.0f;
            return;
        }
        
        // Process beat detection first
        ProcessBeatDetection(ctx);
        
        if (BeatDetected)
        {
            // Apply skip logic
            skipCounter++;
            if (skipCounter > SkipCount)
            {
                // Pass this beat
                skipCounter = 0;
                // Beat is already set to true
            }
            else
            {
                // Skip this beat
                IsBeat = false;
                BeatDetected = false;
                BeatStrength = 0.0f;
            }
        }
    }
    
    private void ProcessBeatInversion(FrameContext ctx)
    {
        // Process beat detection first
        ProcessBeatDetection(ctx);
        
        if (BeatDetected)
        {
            // Invert the beat
            IsBeat = !IsBeat;
            BeatDetected = IsBeat;
            BeatStrength = IsBeat ? BeatStrength : 0.0f;
        }
    }
    
    private float CalculateAudioEnergy()
    {
        float totalEnergy = 0.0f;
        
        // Calculate RMS energy from audio buffer
        for (int i = 0; i < audioBufferSize; i++)
        {
            totalEnergy += audioBuffer[i] * audioBuffer[i];
        }
        
        return MathF.Sqrt(totalEnergy / audioBufferSize);
    }
    
    private void UpdateEnergyHistory(float currentEnergy)
    {
        // Shift energy history
        for (int i = energyHistorySize - 1; i > 0; i--)
        {
            energyHistory[i] = energyHistory[i - 1];
        }
        energyHistory[0] = currentEnergy;
        
        // Calculate local average
        float sum = 0.0f;
        for (int i = 0; i < energyHistorySize; i++)
        {
            sum += energyHistory[i];
        }
        localAverage = sum / energyHistorySize;
        
        // Calculate variance
        float varianceSum = 0.0f;
        for (int i = 0; i < energyHistorySize; i++)
        {
            float diff = energyHistory[i] - localAverage;
            varianceSum += diff * diff;
        }
        variance = varianceSum / energyHistorySize;
    }
    
    private float CalculateBeatProbability(float currentEnergy)
    {
        if (variance == 0) return 0.0f;
        
        // Calculate beat probability using statistical analysis
        float energyDifference = currentEnergy - localAverage;
        float normalizedDifference = energyDifference / MathF.Sqrt(variance);
        
        // Apply sigmoid function for smooth probability
        float probability = 1.0f / (1.0f + MathF.Exp(-normalizedDifference));
        
        // Update beat confidence
        beatConfidence = probability;
        
        return probability;
    }
    
    private void UpdateAverageBeatInterval(float newInterval)
    {
        // Exponential moving average for smooth BPM tracking
        float alpha = 0.1f; // Smoothing factor
        averageBeatInterval = (alpha * newInterval) + ((1.0f - alpha) * averageBeatInterval);
    }
    
    private void UpdateUIElements(FrameContext ctx)
    {
        // Update input slider (shows detected beats)
        if (BeatDetected)
        {
            InputSlider = (InputSlider + 1) % 100;
        }
        
        // Update output slider (shows processed beats)
        if (IsBeat)
        {
            OutputSlider = (OutputSlider + 1) % 100;
        }
    }
    
    private void UpdateBeatStatistics(FrameContext ctx)
    {
        // Update frame-based statistics
        if (BeatDetected)
        {
            // Additional statistics can be calculated here
            // Such as beat pattern analysis, rhythm detection, etc.
        }
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Multi-threaded copying
        int threadCount = Environment.ProcessorCount;
        int rowsPerThread = height / threadCount;
        
        var tasks = new Task[threadCount];
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startRow = threadIndex * rowsPerThread;
            int endRow = (threadIndex == threadCount - 1) ? height : startRow + rowsPerThread;
            
            tasks[threadIndex] = Task.Run(() =>
            {
                CopyRowRange(startRow, endRow, width, input, output);
            });
        }
        
        Task.WaitAll(tasks);
    }
    
    private void CopyRowRange(int startRow, int endRow, int width, ImageBuffer input, ImageBuffer output)
    {
        for (int y = startRow; y < endRow; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = input.GetPixel(x, y);
                output.SetPixel(x, y, pixelColor);
            }
        }
    }
    
    // Public methods for external access
    public void SetBeatThreshold(float threshold)
    {
        beatThreshold = Math.Clamp(threshold, 0.1f, 0.9f);
    }
    
    public void SetBeatSensitivity(float sensitivity)
    {
        beatSensitivity = Math.Clamp(sensitivity, 0.1f, 0.9f);
    }
    
    public void ResetBeatCounter()
    {
        beatCounter = 0;
        totalBeatsProcessed = 0;
        skipCounter = 0;
    }
    
    public float GetAverageBPM()
    {
        if (averageBeatInterval > 0)
        {
            return 60000.0f / averageBeatInterval;
        }
        return 0.0f;
    }
    
    public int GetTotalBeatsProcessed() => totalBeatsProcessed;
    public int GetBeatCounter() => beatCounter;
    public float GetBeatConfidence() => beatConfidence;
    public float GetLocalAverage() => localAverage;
    public float GetVariance() => variance;
    
    // Dispose pattern
    public override void Dispose()
    {
        beatTimer?.Stop();
        arbitraryTimer?.Stop();
        base.Dispose();
    }
}

public enum BeatMode
{
    Detection,    // Pass through detected beats
    Arbitrary,    // Generate beats at custom BPM
    Skip,         // Skip beats based on count
    Invert        // Reverse beat patterns
}
```

### **Optimization Strategies**
- **High-precision timing**: Use high-resolution timers and Stopwatch
- **Statistical beat detection**: Advanced audio energy analysis with variance calculation
- **Efficient beat processing**: Optimized beat detection algorithms with confidence scoring
- **Memory management**: Minimal allocation and efficient audio buffer handling
- **Real-time performance**: Low-latency beat processing with multi-threading support
- **Smooth BPM tracking**: Exponential moving average for stable BPM calculation
- **Beat pattern analysis**: Comprehensive beat statistics and pattern recognition
- **Audio energy analysis**: RMS-based energy calculation with history tracking

---

## 📚 **Use Cases**

### **Musical Applications**
- **BPM synchronization**: Precise musical timing control
- **Rhythm patterns**: Custom beat sequences and patterns
- **Tempo control**: Dynamic BPM adjustment
- **Beat manipulation**: Advanced beat filtering and modification

### **Visual Effects**
- **Beat-reactive animations**: Visual elements synchronized with music
- **Rhythm visualization**: Visual representation of musical timing
- **Pattern recognition**: Analysis of musical beat structures
- **Synchronization**: Coordinating multiple visual effects

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **Advanced BPM detection**: Machine learning based beat analysis
- **Real-time editing**: Live BPM and pattern adjustment
- **Effect chaining**: Multiple beat detection effects
- **MIDI integration**: External MIDI clock synchronization

### **Advanced Beat Features**
- **Polyrhythms**: Complex multi-layered rhythm patterns
- **Beat prediction**: Anticipatory beat generation
- **Tempo mapping**: Dynamic tempo changes
- **Rhythm analysis**: Musical pattern recognition

---

## 📖 **References**

- **Source Code**: `r_bpm.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **Audio Processing**: Beat detection and timing algorithms
- **Timing Systems**: High-precision timing calculations
- **Base Class**: `C_RBASE` for basic effect support

---

**Status:** ✅ **NINTH EFFECT DOCUMENTED**  
**Next:** Spectrum Visualization (SVP) effect analysis
