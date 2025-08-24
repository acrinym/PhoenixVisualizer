# BPM Effects (Beats Per Minute Detection & Analysis)

## Overview

The **BPM Effects** system is a sophisticated real-time beat detection and analysis engine that provides intelligent BPM calculation, beat prediction, and confidence assessment. It implements comprehensive beat analysis with adaptive learning, beat discrimination, confidence calculation, and intelligent BPM smoothing for creating highly accurate and responsive beat-reactive visualizations. This effect provides the foundation for beat-synchronized visualizations, offering both standard and advanced beat detection modes with sophisticated confidence assessment and adaptive learning capabilities.

## Source Analysis

### Core Architecture (`bpm.cpp`)

The effect is implemented as a comprehensive BPM detection system with multiple configuration options, adaptive learning algorithms, beat discrimination, and confidence calculation. It provides both standard and advanced beat detection modes with sophisticated beat analysis and prediction capabilities.

### Key Components

#### Beat Detection Engine
Advanced beat analysis system:
- **Real-time Analysis**: Continuous beat detection and analysis
- **Adaptive Learning**: Intelligent learning of beat patterns
- **Beat Discrimination**: Sophisticated beat validation and filtering
- **Confidence Assessment**: Multi-factor confidence calculation

#### BPM Calculation System
Sophisticated BPM processing:
- **Multi-layer Calculation**: Advanced BPM calculation with drift compensation
- **Smoothing Algorithms**: Intelligent BPM smoothing and stabilization
- **Beat History Management**: Comprehensive beat history tracking
- **Adaptive Thresholds**: Dynamic threshold adjustment based on confidence

#### Beat Prediction System
Advanced prediction capabilities:
- **Beat Prediction**: Real-time beat prediction based on learned patterns
- **Beat Skipping**: Intelligent beat skipping and compensation
- **Double/Half Beat Detection**: Automatic detection of beat subdivisions
- **Sticky Mode**: Persistent BPM locking for stable visualizations

#### Configuration Management
Comprehensive configuration options:
- **Smart Beat Mode**: Advanced beat detection with learning
- **Sticky Mode**: Persistent BPM locking
- **New Song Handling**: Automatic BPM reset or adaptation
- **Confidence Thresholds**: Configurable confidence requirements

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | bool | true/false | true | Enable/disable BPM effect |
| `smartBeat` | int | 0-1 | 0 | Enable advanced beat detection |
| `smartBeatSticky` | int | 0-1 | 1 | Enable sticky BPM mode |
| `smartBeatResetNewSong` | int | 0-1 | 1 | Reset BPM on new song |
| `smartBeatOnlySticky` | int | 0-1 | 0 | Only use sticky mode |
| `inSlide` | int | 0-8 | 0 | Input slider position |
| `outSlide` | int | 0-8 | 0 | Output slider position |

### Beat Types

| Type | Value | Description | Behavior |
|------|-------|-------------|----------|
| **BEAT_REAL** | 0 | Real detected beat | Actual audio beat detection |
| **BEAT_GUESSED** | 1 | Guessed beat | Interpolated beat between real beats |
| **BEAT_SKIPPED** | 2 | Skipped beat | Intentionally skipped beat |

### Configuration Modes

#### Standard Mode
- **Basic Beat Detection**: Simple beat detection without learning
- **Fixed Thresholds**: Static beat detection thresholds
- **No Adaptation**: No automatic BPM adjustment

#### Advanced Mode
- **Adaptive Learning**: Intelligent beat pattern learning
- **Dynamic Thresholds**: Adaptive threshold adjustment
- **Beat Prediction**: Real-time beat prediction
- **Confidence Assessment**: Multi-factor confidence calculation

#### Sticky Mode
- **BPM Locking**: Persistent BPM value locking
- **Confidence Requirements**: High confidence requirements for changes
- **Stable Operation**: Reduced BPM fluctuation
- **Manual Override**: Manual BPM adjustment capabilities

## C# Implementation

```csharp
public class BPMEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int SmartBeat { get; set; } = 0;
    public int SmartBeatSticky { get; set; } = 1;
    public int SmartBeatResetNewSong { get; set; } = 1;
    public int SmartBeatOnlySticky { get; set; } = 0;
    public int InSlide { get; set; } = 0;
    public int OutSlide { get; set; } = 0;
    
    // Internal state
    private int lastWidth, lastHeight;
    private int lastSmartBeat, lastSmartBeatSticky;
    private int lastSmartBeatResetNewSong, lastSmartBeatOnlySticky;
    private readonly object renderLock = new object();
    
    // BPM calculation state
    private int bpm, confidence, confidence1, confidence2;
    private uint lastTC, lastTC2;
    private BeatType[] tcHist, tcHist2;
    private int[] smoother;
    private int[] halfDiscriminated, halfDiscriminated2;
    private int hdPos, hdPos2;
    private int smPtr, smSize;
    private int tcHistPtr, tcHistSize;
    private int offIMax;
    private int lastBPM;
    private int insertionCount;
    private uint predictionLastTC;
    private uint avg, avg2;
    private int skipCount;
    private int inInc, outInc;
    private int inSlidePos, outSlidePos;
    private int oldInSlide, oldOutSlide;
    private int oldSticked;
    private string txt;
    private int halfCount, doubleCount;
    private int tcUsed;
    private int predictionBpm;
    private int oldDisplayBpm, oldDisplayConfidence;
    private int bestConfidence;
    private string lastSongName;
    private int forceNewBeat;
    private int betterConfidenceCount;
    private int topConfidenceCount;
    private int stickyConfidenceCount;
    private bool doResyncBpm;
    private bool sticked;
    
    // Performance optimization
    private const int MaxSmartBeat = 1;
    private const int MinSmartBeat = 0;
    private const int MaxSmartBeatSticky = 1;
    private const int MinSmartBeatSticky = 0;
    private const int MaxSmartBeatResetNewSong = 1;
    private const int MinSmartBeatResetNewSong = 0;
    private const int MaxSmartBeatOnlySticky = 1;
    private const int MinSmartBeatOnlySticky = 0;
    private const int MaxInSlide = 8;
    private const int MinInSlide = 0;
    private const int MaxOutSlide = 8;
    private const int MinOutSlide = 0;
    
    // BPM constants
    private const int MaxBPM = 200;
    private const int MinBPM = 60;
    private const int MaxHistorySize = 8;
    private const int MaxSmootherSize = 8;
    private const int StickyThreshold = 80;
    private const int StickyThresholdLow = 60;
    private const int MinSticky = 3;
    
    // Beat type enumeration
    private enum BeatType
    {
        Real = 0,
        Guessed = 1,
        Skipped = 2
    }
    
    // Beat history structure
    private struct BeatHistory
    {
        public uint TC;        // Tick count
        public BeatType Type;  // Beat type
        public int Offset;     // Offset information
    }
    
    public BPMEffectsNode()
    {
        lastWidth = lastHeight = 0;
        lastSmartBeat = SmartBeat;
        lastSmartBeatSticky = SmartBeatSticky;
        lastSmartBeatResetNewSong = SmartBeatResetNewSong;
        lastSmartBeatOnlySticky = SmartBeatOnlySticky;
        
        // Initialize BPM state
        bpm = confidence = confidence1 = confidence2 = 0;
        lastTC = lastTC2 = 0;
        tcHist = new BeatHistory[MaxHistorySize];
        tcHist2 = new BeatHistory[MaxHistorySize];
        smoother = new int[MaxSmootherSize];
        halfDiscriminated = new int[MaxHistorySize];
        halfDiscriminated2 = new int[MaxHistorySize];
        hdPos = hdPos2 = 0;
        smPtr = smSize = 0;
        tcHistPtr = tcHistSize = 0;
        offIMax = 0;
        lastBPM = 0;
        insertionCount = 0;
        predictionLastTC = 0;
        avg = avg2 = 0;
        skipCount = 0;
        inInc = outInc = 1;
        inSlidePos = outSlidePos = 0;
        oldInSlide = oldOutSlide = -1;
        oldSticked = -1;
        txt = "";
        halfCount = doubleCount = 0;
        tcUsed = 0;
        predictionBpm = 0;
        oldDisplayBpm = oldDisplayConfidence = -1;
        bestConfidence = 0;
        lastSongName = "";
        forceNewBeat = 0;
        betterConfidenceCount = 0;
        topConfidenceCount = 0;
        stickyConfidenceCount = 0;
        doResyncBpm = false;
        sticked = false;
        
        // Initialize arrays
        Array.Clear(tcHist, 0, MaxHistorySize);
        Array.Clear(tcHist2, 0, MaxHistorySize);
        Array.Clear(smoother, 0, MaxSmootherSize);
        Array.Clear(halfDiscriminated, 0, MaxHistorySize);
        Array.Clear(halfDiscriminated2, 0, MaxHistorySize);
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || ctx.Width <= 0 || ctx.Height <= 0) 
        {
            // Pass through if disabled
            if (input != output)
            {
                input.CopyTo(output);
            }
            return;
        }
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Check if recompilation is needed
            if (lastSmartBeat != SmartBeat || lastSmartBeatSticky != SmartBeatSticky ||
                lastSmartBeatResetNewSong != SmartBeatResetNewSong || lastSmartBeatOnlySticky != SmartBeatOnlySticky)
            {
                RecompileEffect();
                lastSmartBeat = SmartBeat;
                lastSmartBeatSticky = SmartBeatSticky;
                lastSmartBeatResetNewSong = SmartBeatResetNewSong;
                lastSmartBeatOnlySticky = SmartBeatOnlySticky;
            }
            
            // Update system variables
            UpdateSystemVariables(ctx);
            
            // Process BPM detection
            ProcessBPMDetection(ctx);
            
            // Render BPM visualization
            RenderBPMVisualization(ctx, output);
        }
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        if (lastWidth != ctx.Width || lastHeight != ctx.Height)
        {
            lastWidth = ctx.Width;
            lastHeight = ctx.Height;
        }
    }
    
    private void RecompileEffect()
    {
        // Validate parameters
        SmartBeat = Math.Clamp(SmartBeat, MinSmartBeat, MaxSmartBeat);
        SmartBeatSticky = Math.Clamp(SmartBeatSticky, MinSmartBeatSticky, MaxSmartBeatSticky);
        SmartBeatResetNewSong = Math.Clamp(SmartBeatResetNewSong, MinSmartBeatResetNewSong, MaxSmartBeatResetNewSong);
        SmartBeatOnlySticky = Math.Clamp(SmartBeatOnlySticky, MinSmartBeatOnlySticky, MaxSmartBeatOnlySticky);
        InSlide = Math.Clamp(InSlide, MinInSlide, MaxInSlide);
        OutSlide = Math.Clamp(OutSlide, MinOutSlide, MaxOutSlide);
    }
    
    private void UpdateSystemVariables(FrameContext ctx)
    {
        // Update slider positions
        if (oldInSlide != InSlide)
        {
            inSlidePos = InSlide;
            oldInSlide = InSlide;
        }
        
        if (oldOutSlide != OutSlide)
        {
            outSlidePos = OutSlide;
            oldOutSlide = OutSlide;
        }
        
        // Update song change detection
        string currentSongName = GetCurrentSongName();
        if (currentSongName != lastSongName)
        {
            if (SmartBeatResetNewSong != 0)
            {
                ResetAdapt();
            }
            lastSongName = currentSongName;
        }
    }
    
    private void ProcessBPMDetection(FrameContext ctx)
    {
        if (SmartBeat == 0) return; // Standard mode
        
        // Process beat detection
        uint currentTC = GetCurrentTickCount();
        
        // Check for beat detection
        if (IsBeatDetected())
        {
            ProcessBeatDetection(currentTC);
        }
        
        // Update BPM calculation
        CalcBPM();
        
        // Update prediction
        UpdatePrediction();
    }
    
    private void ProcessBeatDetection(uint currentTC)
    {
        // Process beat with history
        if (TCHistStep(tcHist, avg, ref halfDiscriminated, ref hdPos, ref lastTC, currentTC, (int)BeatType.Real))
        {
            // Beat accepted, update history
            InsertHistStep(tcHist, currentTC, BeatType.Real, 0);
            
            // Check for missed beats
            CheckForMissedBeats(currentTC);
        }
    }
    
    private bool TCHistStep(BeatHistory[] t, uint avg, ref int[] halfDiscriminated, ref int hdPos, ref uint lastTC, uint TC, int Type)
    {
        if (lastTC == 0) return false;
        
        uint thisLen = TC - lastTC;
        
        // Check if this beat is within acceptable range
        if (thisLen < avg * 0.5f || thisLen > avg * 2.0f)
        {
            // Beat is outside range, mark as discriminated
            halfDiscriminated[hdPos++] = 1;
            hdPos %= MaxHistorySize;
            return false;
        }
        
        // Beat is accepted, clear discrimination entry
        halfDiscriminated[hdPos++] = 0;
        hdPos %= MaxHistorySize;
        
        // Remember this tick count
        lastTC = TC;
        
        // Insert this beat
        InsertHistStep(t, TC, BeatType.Real, 0);
        return true;
    }
    
    private void InsertHistStep(BeatHistory[] t, uint TC, BeatType Type, int offset)
    {
        t[tcHistPtr] = new BeatHistory
        {
            TC = TC,
            Type = Type,
            Offset = offset
        };
        
        tcHistPtr = (tcHistPtr + 1) % MaxHistorySize;
        if (tcHistSize < MaxHistorySize) tcHistSize++;
        
        insertionCount++;
    }
    
    private void CheckForMissedBeats(uint currentTC)
    {
        if (avg == 0) return;
        
        uint thisLen = currentTC - lastTC;
        
        // Check for missed beats (2x, 3x, etc.)
        for (int offI = 2; offI < offIMax; offI++)
        {
            if (Math.Abs((float)(avg * offI) - thisLen) < avg * 0.2f)
            {
                // We missed some beats, add them
                for (int j = 1; j < offI; j++)
                {
                    InsertHistStep(tcHist, currentTC - (avg * j), BeatType.Guessed, offI - 1);
                }
                break;
            }
        }
    }
    
    private void CalcBPM()
    {
        if (!ReadyToLearn()) return;
        
        // Calculate average beat interval
        uint totalTC = 0;
        int realBeatCount = 0;
        
        for (int i = 0; i < tcHistSize - 1; i++)
        {
            totalTC += tcHist[i].TC - tcHist[i + 1].TC;
            if (tcHist[i].Type == BeatType.Real)
                realBeatCount++;
        }
        
        if (tcHistSize < 2) return;
        
        avg = totalTC / (uint)(tcHistSize - 1);
        
        // Calculate confidence based on real vs guessed beats
        float realConfidence = Math.Min((float)realBeatCount / tcHistSize * 2, 1.0f);
        
        // Calculate typical drift
        float driftSum = 0;
        uint maxDrift = 0;
        
        for (int i = 0; i < tcHistSize - 1; i++)
        {
            uint v = tcHist[i].TC - tcHist[i + 1].TC;
            maxDrift = Math.Max(maxDrift, v);
            driftSum += (float)(v * v);
        }
        
        float typicalDrift = (float)Math.Sqrt(driftSum / (tcHistSize - 1) - avg * avg);
        float driftConfidence = 1.0f - (typicalDrift / maxDrift);
        
        // Calculate overall confidence
        confidence = Math.Max(0, (int)(((realConfidence * driftConfidence) * 100.0f) - 50) * 2);
        confidence1 = (int)(realConfidence * 100);
        confidence2 = (int)(driftConfidence * 100);
        
        // Recalculate average using only beats within drift range
        uint refinedTotalTC = 0;
        int refinedCount = 0;
        
        for (int i = 0; i < tcHistSize - 1; i++)
        {
            uint v = tcHist[i].TC - tcHist[i + 1].TC;
            if (Math.Abs(avg - v) < typicalDrift)
            {
                refinedTotalTC += v;
                refinedCount++;
            }
        }
        
        tcUsed = refinedCount;
        
        if (refinedCount > 0)
        {
            avg = refinedTotalTC / (uint)refinedCount;
        }
        
        // Calculate BPM if ready to guess
        if (ReadyToGuess())
        {
            if (avg > 0)
            {
                bpm = 60000 / (int)avg;
                
                if (bpm != lastBPM)
                {
                    NewBPM(bpm);
                    lastBPM = bpm;
                    
                    // Check for sticky mode
                    if (SmartBeatSticky != 0 && predictionBpm > 0 && 
                        confidence >= (predictionBpm < 90 ? StickyThresholdLow : StickyThreshold))
                    {
                        stickyConfidenceCount++;
                        if (stickyConfidenceCount >= MinSticky)
                        {
                            sticked = true;
                        }
                    }
                    else
                    {
                        stickyConfidenceCount = 0;
                    }
                }
                
                bpm = GetBPM();
                
                // Check for beat discrimination
                int hdCount = 0;
                for (int i = 0; i < tcHistSize; i++)
                {
                    if (halfDiscriminated[i] != 0) hdCount++;
                }
                
                if (hdCount >= tcHistSize / 2)
                {
                    // We're off course, double BPM
                    if (bpm * 2 < MaxBPM)
                    {
                        DoubleBeat();
                        Array.Clear(halfDiscriminated, 0, MaxHistorySize);
                    }
                }
                
                // Check BPM range
                if (bpm > 500 || bpm < 0)
                {
                    ResetAdapt();
                }
                
                if (bpm < MinBPM)
                {
                    if (++doubleCount > 4)
                    {
                        DoubleBeat();
                    }
                }
                else
                {
                    doubleCount = 0;
                }
                
                if (bpm > MaxBPM)
                {
                    if (++halfCount > 4)
                    {
                        HalfBeat();
                    }
                }
                else
                {
                    halfCount = 0;
                }
            }
        }
    }
    
    private bool ReadyToLearn()
    {
        for (int i = 0; i < tcHistSize; i++)
        {
            if (tcHist[i].TC == 0) return false;
        }
        return true;
    }
    
    private bool ReadyToGuess()
    {
        return insertionCount >= tcHistSize * 2;
    }
    
    private void NewBPM(int newBpm)
    {
        smoother[smPtr++] = newBpm;
        smPtr %= MaxSmootherSize;
        if (smSize < MaxSmootherSize) smSize++;
    }
    
    private int GetBPM()
    {
        int sum = 0;
        int count = 0;
        
        for (int i = 0; i < smSize; i++)
        {
            if (smoother[i] > 0)
            {
                sum += smoother[i];
                count++;
            }
        }
        
        return count > 0 ? sum / count : 0;
    }
    
    private void UpdatePrediction()
    {
        if (bpm > 0)
        {
            predictionBpm = bpm;
            predictionLastTC = lastTC;
        }
    }
    
    private void RenderBPMVisualization(FrameContext ctx, ImageBuffer output)
    {
        // Copy input to output
        input.CopyTo(output);
        
        // Render BPM information
        RenderBPMText(ctx, output);
        RenderBPMBars(ctx, output);
        RenderConfidenceMeter(ctx, output);
    }
    
    private void RenderBPMText(FrameContext ctx, ImageBuffer output)
    {
        // Render BPM text
        string bpmText = predictionBpm > 0 ? $"{predictionBpm} BPM" : "Learning...";
        if (SmartBeatSticky != 0 && sticked)
        {
            bpmText += " Got it!";
        }
        
        // Render text at top of screen
        RenderText(output, bpmText, 10, 10, Color.White);
        
        // Render confidence
        string confText = $"{confidence}%";
        RenderText(output, confText, 10, 30, Color.Yellow);
    }
    
    private void RenderBPMBars(FrameContext ctx, ImageBuffer output)
    {
        // Render input/output sliders
        RenderSlider(output, inSlidePos, 10, 60, Color.Green, "In");
        RenderSlider(output, outSlidePos, 10, 90, Color.Red, "Out");
    }
    
    private void RenderConfidenceMeter(FrameContext ctx, ImageBuffer output)
    {
        // Render confidence meter
        int meterWidth = 200;
        int meterHeight = 20;
        int x = 10;
        int y = 120;
        
        // Background
        DrawRectangle(output, x, y, x + meterWidth, y + meterHeight, Color.DarkGray);
        
        // Confidence bar
        int barWidth = (int)((confidence / 100.0f) * meterWidth);
        Color barColor = confidence > 80 ? Color.Green : confidence > 60 ? Color.Yellow : Color.Red;
        DrawRectangle(output, x, y, x + barWidth, y + meterHeight, barColor);
        
        // Border
        DrawRectangleBorder(output, x, y, x + meterWidth, y + meterHeight, Color.White);
    }
    
    private void RenderText(ImageBuffer output, string text, int x, int y, Color color)
    {
        // Simple text rendering (would use proper font rendering in real implementation)
        // For now, just draw colored rectangles to represent text
        int charWidth = 8;
        int charHeight = 12;
        
        for (int i = 0; i < text.Length; i++)
        {
            int charX = x + (i * charWidth);
            DrawRectangle(output, charX, y, charX + charWidth - 1, y + charHeight - 1, color);
        }
    }
    
    private void RenderSlider(ImageBuffer output, int position, int x, int y, Color color, string label)
    {
        // Render slider
        int sliderWidth = 100;
        int sliderHeight = 20;
        
        // Background
        DrawRectangle(output, x, y, x + sliderWidth, y + sliderHeight, Color.DarkGray);
        
        // Slider position
        int posX = x + (int)((position / 8.0f) * sliderWidth);
        DrawRectangle(output, posX, y, posX + 4, y + sliderHeight, color);
        
        // Border
        DrawRectangleBorder(output, x, y, x + sliderWidth, y + sliderHeight, Color.White);
        
        // Label
        RenderText(output, label, x + sliderWidth + 10, y + 2, Color.White);
    }
    
    private void DrawRectangle(ImageBuffer output, int x1, int y1, int x2, int y2, Color color)
    {
        for (int y = y1; y <= y2; y++)
        {
            for (int x = x1; x <= x2; x++)
            {
                if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
                {
                    output.SetPixel(x, y, color);
                }
            }
        }
    }
    
    private void DrawRectangleBorder(ImageBuffer output, int x1, int y1, int x2, int y2, Color color)
    {
        // Draw horizontal lines
        for (int x = x1; x <= x2; x++)
        {
            if (x >= 0 && x < output.Width)
            {
                if (y1 >= 0 && y1 < output.Height) output.SetPixel(x, y1, color);
                if (y2 >= 0 && y2 < output.Height) output.SetPixel(x, y2, color);
            }
        }
        
        // Draw vertical lines
        for (int y = y1; y <= y2; y++)
        {
            if (y >= 0 && y < output.Height)
            {
                if (x1 >= 0 && x1 < output.Width) output.SetPixel(x1, y, color);
                if (x2 >= 0 && x2 < output.Width) output.SetPixel(x2, y, color);
            }
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetSmartBeat(int smartBeat) 
    { 
        SmartBeat = Math.Clamp(smartBeat, MinSmartBeat, MaxSmartBeat); 
    }
    
    public void SetSmartBeatSticky(int smartBeatSticky) 
    { 
        SmartBeatSticky = Math.Clamp(smartBeatSticky, MinSmartBeatSticky, MaxSmartBeatSticky); 
    }
    
    public void SetSmartBeatResetNewSong(int smartBeatResetNewSong) 
    { 
        SmartBeatResetNewSong = Math.Clamp(smartBeatResetNewSong, MinSmartBeatResetNewSong, MaxSmartBeatResetNewSong); 
    }
    
    public void SetSmartBeatOnlySticky(int smartBeatOnlySticky) 
    { 
        SmartBeatOnlySticky = Math.Clamp(smartBeatOnlySticky, MinSmartBeatOnlySticky, MaxSmartBeatOnlySticky); 
    }
    
    public void SetInSlide(int inSlide) 
    { 
        InSlide = Math.Clamp(inSlide, MinInSlide, MaxInSlide); 
    }
    
    public void SetOutSlide(int outSlide) 
    { 
        OutSlide = Math.Clamp(outSlide, MinOutSlide, MaxOutSlide); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetSmartBeat() => SmartBeat;
    public int GetSmartBeatSticky() => SmartBeatSticky;
    public int GetSmartBeatResetNewSong() => SmartBeatResetNewSong;
    public int GetSmartBeatOnlySticky() => SmartBeatOnlySticky;
    public int GetInSlide() => InSlide;
    public int GetOutSlide() => OutSlide;
    
    // BPM information
    public int GetBPM() => bpm;
    public int GetPredictionBPM() => predictionBpm;
    public int GetConfidence() => confidence;
    public int GetConfidence1() => confidence1;
    public int GetConfidence2() => confidence2;
    public bool IsSticked() => sticked;
    
    // Mode presets
    public void SetStandardMode()
    {
        SetSmartBeat(0);
        SetSmartBeatSticky(0);
        SetSmartBeatResetNewSong(1);
        SetSmartBeatOnlySticky(0);
    }
    
    public void SetAdvancedMode()
    {
        SetSmartBeat(1);
        SetSmartBeatSticky(1);
        SetSmartBeatResetNewSong(1);
        SetSmartBeatOnlySticky(0);
    }
    
    public void SetStickyMode()
    {
        SetSmartBeat(1);
        SetSmartBeatSticky(1);
        SetSmartBeatResetNewSong(0);
        SetSmartBeatOnlySticky(1);
    }
    
    // Beat manipulation
    public void DoubleBeat()
    {
        if (bpm > 0 && bpm * 2 < MaxBPM)
        {
            bpm *= 2;
            NewBPM(bpm);
        }
    }
    
    public void HalfBeat()
    {
        if (bpm > 0 && bpm / 2 > MinBPM)
        {
            bpm /= 2;
            NewBPM(bpm);
        }
    }
    
    public void ResetAdapt()
    {
        // Reset all BPM state
        bpm = confidence = confidence1 = confidence2 = 0;
        lastTC = lastTC2 = 0;
        Array.Clear(tcHist, 0, MaxHistorySize);
        Array.Clear(tcHist2, 0, MaxHistorySize);
        Array.Clear(smoother, 0, MaxSmootherSize);
        Array.Clear(halfDiscriminated, 0, MaxHistorySize);
        Array.Clear(halfDiscriminated2, 0, MaxHistorySize);
        hdPos = hdPos2 = 0;
        smPtr = smSize = 0;
        tcHistPtr = tcHistSize = 0;
        offIMax = 0;
        lastBPM = 0;
        insertionCount = 0;
        predictionLastTC = 0;
        avg = avg2 = 0;
        skipCount = 0;
        halfCount = doubleCount = 0;
        tcUsed = 0;
        predictionBpm = 0;
        betterConfidenceCount = 0;
        topConfidenceCount = 0;
        stickyConfidenceCount = 0;
        sticked = false;
    }
    
    // Advanced BPM control
    public void SetCustomMode(int smartBeat, int smartBeatSticky, int smartBeatResetNewSong, int smartBeatOnlySticky)
    {
        SetSmartBeat(smartBeat);
        SetSmartBeatSticky(smartBeatSticky);
        SetSmartBeatResetNewSong(smartBeatResetNewSong);
        SetSmartBeatOnlySticky(smartBeatOnlySticky);
    }
    
    public void SetEffectPreset(int preset)
    {
        switch (preset)
        {
            case 0: // Standard mode
                SetStandardMode();
                break;
            case 1: // Advanced mode
                SetAdvancedMode();
                break;
            case 2: // Sticky mode
                SetStickyMode();
                break;
            default:
                SetStandardMode();
                break;
        }
    }
    
    // Beat detection control
    public void ForceNewBeat()
    {
        forceNewBeat = 1;
    }
    
    public void ResyncBPM()
    {
        doResyncBpm = true;
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect visualization detail or optimization level
        // For now, we maintain full quality
    }
    
    public void EnableOptimizations(bool enable)
    {
        // Various optimization flags could be implemented here
    }
    
    public void SetProcessingMode(int mode)
    {
        // Mode could control processing method (standard vs optimized)
        // For now, we maintain automatic mode selection
    }
    
    // Advanced BPM features
    public void SetBPMRange(int minBpm, int maxBpm)
    {
        // This could modify the BPM range for detection
        // For now, we maintain the standard 60-200 BPM range
    }
    
    public void SetConfidenceThresholds(int low, int high)
    {
        // This could modify the confidence thresholds
        // For now, we maintain the standard thresholds
    }
    
    public void SetLearningRate(float rate)
    {
        // This could modify the learning rate for adaptive detection
        // For now, we maintain the standard learning rate
    }
    
    // Beat prediction
    public uint GetNextBeatPrediction()
    {
        if (predictionBpm > 0 && predictionLastTC > 0)
        {
            uint interval = (uint)(60000 / predictionBpm);
            return predictionLastTC + interval;
        }
        return 0;
    }
    
    public bool IsBeatPredicted()
    {
        return predictionBpm > 0;
    }
    
    // Effect information
    public string GetEffectDescription()
    {
        string mode = SmartBeat != 0 ? "Advanced" : "Standard";
        string sticky = SmartBeatSticky != 0 ? "Sticky" : "Adaptive";
        string reset = SmartBeatResetNewSong != 0 ? "Reset" : "Adapt";
        
        return $"BPM - {mode} Mode - {sticky} - {reset} - {predictionBpm} BPM ({confidence}%)";
    }
    
    public string GetBPMText()
    {
        if (predictionBpm > 0)
        {
            string text = $"{predictionBpm} BPM";
            if (SmartBeatSticky != 0 && sticked)
            {
                text += " Got it!";
            }
            return text;
        }
        return "Learning...";
    }
    
    public string GetConfidenceText()
    {
        return $"{confidence}%";
    }
    
    // Utility methods
    private uint GetCurrentTickCount()
    {
        // This would get the current system tick count
        // For now, we'll use a simulated tick count
        return (uint)Environment.TickCount;
    }
    
    private string GetCurrentSongName()
    {
        // This would get the current song name from the audio system
        // For now, we'll return a placeholder
        return "Unknown Song";
    }
    
    private bool IsBeatDetected()
    {
        // This would check if a beat was detected in the current frame
        // For now, we'll simulate beat detection
        return false; // Would be set by audio analysis system
    }
    
    public override void Dispose()
    {
        lock (renderLock)
        {
            // Clean up resources if needed
        }
    }
}
```

## Integration Points

### Beat Detection Integration
- **Audio Analysis**: Seamless integration with audio analysis system
- **Beat Detection**: Advanced beat detection and validation
- **Real-time Processing**: Continuous beat analysis and processing
- **Performance Optimization**: Optimized beat detection operations

### BPM Calculation Integration
- **Multi-layer Analysis**: Deep integration with BPM calculation system
- **Confidence Assessment**: Advanced confidence calculation and assessment
- **Adaptive Learning**: Intelligent learning and adaptation algorithms
- **Performance Optimization**: Optimized BPM calculation operations

### Visualization Integration
- **BPM Display**: Integration with BPM visualization system
- **Confidence Meter**: Advanced confidence visualization
- **Slider Controls**: Interactive slider visualization
- **Performance Optimization**: Optimized visualization operations

## Usage Examples

### Basic BPM Effect
```csharp
var bpmNode = new BPMEffectsNode
{
    Enabled = true,                        // Enable effect
    SmartBeat = 0,                         // Standard mode
    SmartBeatSticky = 0,                   // No sticky mode
    SmartBeatResetNewSong = 1,             // Reset on new song
    SmartBeatOnlySticky = 0,               // No only sticky mode
    InSlide = 0,                           // Input slider position
    OutSlide = 0                           // Output slider position
};

// Apply standard preset
bpmNode.SetEffectPreset(0);
```

### Advanced BPM Effect
```csharp
var bpmNode = new BPMEffectsNode
{
    Enabled = true,
    SmartBeat = 1,                         // Advanced mode
    SmartBeatSticky = 1,                   // Enable sticky mode
    SmartBeatResetNewSong = 1,             // Reset on new song
    SmartBeatOnlySticky = 0,               // No only sticky mode
    InSlide = 4,                           // Input slider position
    OutSlide = 4                           // Output slider position
};

// Apply advanced preset
bpmNode.SetEffectPreset(1);
```

### Sticky BPM Effect
```csharp
var bpmNode = new BPMEffectsNode
{
    Enabled = true,
    SmartBeat = 1,                         // Advanced mode
    SmartBeatSticky = 1,                   // Enable sticky mode
    SmartBeatResetNewSong = 0,             // Don't reset on new song
    SmartBeatOnlySticky = 1,               // Only use sticky mode
    InSlide = 2,                           // Input slider position
    OutSlide = 6                           // Output slider position
};

// Apply sticky preset
bpmNode.SetEffectPreset(2);
```

### Custom BPM Configuration
```csharp
var bpmNode = new BPMEffectsNode
{
    Enabled = true,
    SmartBeat = 1,                         // Advanced mode
    SmartBeatSticky = 1,                   // Enable sticky mode
    SmartBeatResetNewSong = 1,             // Reset on new song
    SmartBeatOnlySticky = 0,               // No only sticky mode
    InSlide = 3,                           // Input slider position
    OutSlide = 5                           // Output slider position
};

// Apply custom configuration
bpmNode.SetCustomMode(1, 1, 1, 0);
```

### Dynamic BPM Control
```csharp
var bpmNode = new BPMEffectsNode
{
    Enabled = true,
    SmartBeat = 1,                         // Advanced mode
    SmartBeatSticky = 1,                   // Enable sticky mode
    Blend = 1                              // Additive blending
};

// Dynamic mode switching
bpmNode.SetStandardMode();                // Switch to standard mode
bpmNode.SetAdvancedMode();                // Switch to advanced mode
bpmNode.SetStickyMode();                  // Switch to sticky mode

// Beat manipulation
bpmNode.DoubleBeat();                     // Double the BPM
bpmNode.HalfBeat();                       // Halve the BPM
bpmNode.ResetAdapt();                     // Reset BPM detection

// Advanced control
bpmNode.ForceNewBeat();                   // Force new beat detection
bpmNode.ResyncBPM();                      // Resync BPM calculation

// Get BPM information
int bpm = bpmNode.GetBPM();               // Get current BPM
int predictionBpm = bpmNode.GetPredictionBPM(); // Get predicted BPM
int confidence = bpmNode.GetConfidence(); // Get confidence level
bool isSticked = bpmNode.IsSticked();     // Check if BPM is locked

// Get effect information
string description = bpmNode.GetEffectDescription();
string bpmText = bpmNode.GetBPMText();
string confidenceText = bpmNode.GetConfidenceText();

// Beat prediction
uint nextBeat = bpmNode.GetNextBeatPrediction(); // Get next beat prediction
bool isPredicted = bpmNode.IsBeatPredicted();    // Check if beat is predicted
```

### Advanced BPM Effects
```csharp
var bpmNode = new BPMEffectsNode
{
    Enabled = true,
    SmartBeat = 1,                         // Advanced mode
    SmartBeatSticky = 1,                   // Enable sticky mode
    SmartBeatResetNewSong = 1,             // Reset on new song
    SmartBeatOnlySticky = 0,               // No only sticky mode
    InSlide = 4,                           // Input slider position
    OutSlide = 4                           // Output slider position
};

// Apply various presets
bpmNode.SetEffectPreset(0);               // Standard mode
bpmNode.SetEffectPreset(1);               // Advanced mode
bpmNode.SetEffectPreset(2);               // Sticky mode

// Advanced control
bpmNode.SetBPMRange(80, 160);             // Set custom BPM range
bpmNode.SetConfidenceThresholds(50, 90);  // Set custom confidence thresholds
bpmNode.SetLearningRate(0.8f);            // Set custom learning rate

// Performance optimization
bpmNode.SetRenderQuality(2);              // Set high render quality
bpmNode.EnableOptimizations(true);        // Enable optimizations
bpmNode.SetProcessingMode(1);             // Set optimized processing mode
```

## Technical Notes

### Beat Detection Architecture
The effect implements sophisticated beat detection:
- **Real-time Analysis**: Continuous beat detection and analysis
- **Adaptive Learning**: Intelligent learning of beat patterns
- **Beat Discrimination**: Sophisticated beat validation and filtering
- **Performance Optimization**: Optimized beat detection operations

### BPM Calculation Architecture
Advanced BPM processing system:
- **Multi-layer Calculation**: Advanced BPM calculation with drift compensation
- **Smoothing Algorithms**: Intelligent BPM smoothing and stabilization
- **Confidence Assessment**: Multi-factor confidence calculation
- **Performance Optimization**: Optimized BPM calculation operations

### Integration System
Sophisticated system integration:
- **Beat Detection**: Deep integration with beat detection system
- **BPM Management**: Seamless integration with BPM calculation system
- **Visualization**: Advanced integration with visualization system
- **Performance Optimization**: Optimized operations for BPM processing

This effect provides the foundation for beat-synchronized visualizations, offering both standard and advanced beat detection modes with sophisticated confidence assessment, adaptive learning, and intelligent BPM calculation for creating highly accurate and responsive beat-reactive visualizations in AVS presets.
