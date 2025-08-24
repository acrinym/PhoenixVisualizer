# AVS Water Simulation Effect

## Overview
The Water Simulation effect creates realistic water rippling and wave effects by simulating fluid dynamics through pixel averaging and frame subtraction.

## C++ Source Analysis
Based on `r_water.cpp`, this effect inherits from `C_RBASE2` and implements multi-threaded water simulation with MMX optimization.

## C# Implementation

### Class Definition
```csharp
public class WaterSimulationEffectsNode : BaseEffectNode
{
    public bool Enabled { get; set; } = true;
    public float WaterIntensity { get; set; } = 1.0f;
    public float RippleSpeed { get; set; } = 1.0f;
    public float WaveAmplitude { get; set; } = 0.5f;
    public int WaterQuality { get; set; } = 1;
    public bool BeatReactive { get; set; } = false;
    public float BeatWaterIntensity { get; set; } = 2.0f;
    
    private ImageBuffer LastFrame { get; set; } = null;
    private float WaterTime { get; set; } = 0.0f;
}
```

### Key Features
- Realistic water simulation through neighbor pixel averaging
- Frame subtraction for ripple effects
- Multi-threaded processing support
- Beat-reactive water intensity
- Multiple water quality algorithms

## Technical Implementation
The effect creates water ripples by averaging neighboring pixels and subtracting the previous frame, creating realistic fluid movement effects.
