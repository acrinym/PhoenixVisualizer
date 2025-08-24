# Particle Systems (Render / Moving Particle)

## Overview

The **Particle Systems** is a sophisticated physics-based particle simulation that creates dynamic, moving particles with realistic motion, beat reactivity, and multiple blending modes. It implements a spring-damper physics system where particles are attracted to beat-reactive targets, creating organic, flowing animations. This effect is essential for creating dynamic particle trails, organic motion, and beat-reactive visual elements in AVS presets.

## Source Analysis

### Core Architecture (`r_parts.cpp`)

The effect is implemented as a C++ class `C_BPartsClass` that inherits from `C_RBASE`. It provides a complete particle physics engine with spring-damper dynamics, beat-reactive target positioning, and sophisticated rendering with multiple blending algorithms.

### Key Components

#### Physics Simulation Engine
The effect implements a sophisticated physics system:
- **Spring-Damper Model**: Particles are attracted to targets with spring force and damping
- **Velocity Integration**: Uses Euler integration for smooth particle motion
- **Beat-Reactive Targets**: Dynamic target positions based on audio beat events
- **Momentum Conservation**: Realistic particle behavior with velocity decay

#### Particle Rendering System
Advanced rendering with multiple blending modes:
- **Circular Particles**: Renders particles as filled circles with configurable sizes
- **Multiple Blending Modes**: Replace, blend, average, and linear blending options
- **Size Animation**: Dynamic particle sizing with beat-reactive scaling
- **Boundary Handling**: Proper clipping and edge case management

#### Audio Integration
Deep integration with audio data:
- **Beat Detection**: Uses `isBeat` for target position randomization
- **Dynamic Parameters**: Adjusts particle behavior based on audio events
- **Size Modulation**: Beat-reactive particle size changes
- **Target Variation**: Random target positions on beat events

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | int | Bit flags | 1 | Enable flags (1=active, 2=beat size) |
| `colors` | int | RGB color | White | Particle color |
| `maxdist` | int | 1-32 | 16 | Maximum distance multiplier |
| `size` | int | 1-128 | 8 | Base particle size |
| `size2` | int | 1-128 | 8 | Beat-reactive particle size |
| `blend` | int | 0-3 | 1 | Blending mode (0=replace, 1=blend, 2=average, 3=linear) |

### Rendering Pipeline

1. **Physics Update**: Update particle position and velocity using spring-damper model
2. **Beat Processing**: Handle beat events and update target positions
3. **Position Calculation**: Convert normalized coordinates to screen coordinates
4. **Size Animation**: Apply beat-reactive size changes
5. **Particle Rendering**: Render circular particles with selected blending mode
6. **Boundary Clipping**: Ensure particles stay within screen bounds

## C# Implementation

```csharp
public class ParticleSystemsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public bool BeatSizeEnabled { get; set; } = false;
    public Color ParticleColor { get; set; } = Color.White;
    public int MaxDistance { get; set; } = 16;
    public int BaseSize { get; set; } = 8;
    public int BeatSize { get; set; } = 8;
    public ParticleBlendMode BlendMode { get; set; } = ParticleBlendMode.Blend;
    
    // Internal state
    private double[] currentPosition;
    private double[] currentVelocity;
    private double[] targetPosition;
    private int currentSize;
    private int targetSize;
    private bool wasBeat;
    
    // Audio data
    private float[] leftChannelData;
    private float[] rightChannelData;
    private float[] centerChannelData;
    
    // Physics constants
    private const double SpringForce = 0.004;
    private const double DampingFactor = 0.991;
    private const double BeatRandomRange = 16.0 / 48.0;
    
    // Performance optimization
    private int[] particleBuffer;
    private bool bufferInitialized;
    
    public ParticleSystemsNode()
    {
        currentPosition = new double[2] { -0.6, 0.3 };
        currentVelocity = new double[2] { -0.01551, 0.0 };
        targetPosition = new double[2] { 0.0, 0.0 };
        currentSize = BaseSize;
        targetSize = BaseSize;
        InitializeBuffer();
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) 
        {
            CopyInputToOutput(ctx, input, output);
            return;
        }
        
        UpdateAudioData(ctx);
        UpdatePhysics(ctx);
        RenderParticles(ctx, input, output);
    }
    
    private void UpdateAudioData(FrameContext ctx)
    {
        if (ctx.AudioData != null)
        {
            leftChannelData = ctx.AudioData.LeftChannel;
            rightChannelData = ctx.AudioData.RightChannel;
            centerChannelData = ctx.AudioData.CenterChannel;
        }
    }
    
    private void UpdatePhysics(FrameContext ctx)
    {
        // Handle beat events
        if (ctx.IsBeat && !wasBeat)
        {
            // Randomize target position on beat
            targetPosition[0] = (random.NextDouble() - 0.5) * BeatRandomRange;
            targetPosition[1] = (random.NextDouble() - 0.5) * BeatRandomRange;
            
            // Update size if beat size is enabled
            if (BeatSizeEnabled)
            {
                targetSize = BeatSize;
            }
            
            wasBeat = true;
        }
        else if (!ctx.IsBeat)
        {
            wasBeat = false;
        }
        
        // Apply spring-damper physics
        for (int i = 0; i < 2; i++)
        {
            double displacement = targetPosition[i] - currentPosition[i];
            double springAcceleration = displacement * SpringForce;
            
            currentVelocity[i] += springAcceleration;
            currentVelocity[i] *= DampingFactor;
            currentPosition[i] += currentVelocity[i];
        }
        
        // Update size with smooth transition
        if (currentSize != targetSize)
        {
            int sizeDiff = targetSize - currentSize;
            if (Math.Abs(sizeDiff) <= 1)
            {
                currentSize = targetSize;
            }
            else
            {
                currentSize += sizeDiff / 8;
            }
        }
    }
    
    private void RenderParticles(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!bufferInitialized)
        {
            InitializeBuffer();
        }
        
        // Copy input to output
        Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);
        
        // Convert normalized coordinates to screen coordinates
        int screenX = (int)((currentPosition[0] + 1.0) * ctx.Width / 2.0);
        int screenY = (int)((1.0 - currentPosition[1]) * ctx.Height / 2.0);
        
        // Ensure coordinates are within bounds
        screenX = Math.Max(0, Math.Min(ctx.Width - 1, screenX));
        screenY = Math.Max(0, Math.Min(ctx.Height - 1, screenY));
        
        // Render particle with selected blending mode
        RenderParticle(output, screenX, screenY, currentSize);
    }
    
    private void RenderParticle(ImageBuffer output, int x, int y, int size)
    {
        int radius = size / 2;
        int startX = Math.Max(0, x - radius);
        int endX = Math.Min(output.Width - 1, x + radius);
        int startY = Math.Max(0, y - radius);
        int endY = Math.Min(output.Height - 1, y + radius);
        
        for (int py = startY; py <= endY; py++)
        {
            for (int px = startX; px <= endX; px++)
            {
                int dx = px - x;
                int dy = py - y;
                int distanceSquared = dx * dx + dy * dy;
                
                if (distanceSquared <= radius * radius)
                {
                    int pixelIndex = py * output.Width + px;
                    int existingColor = output.Pixels[pixelIndex];
                    
                    int newColor = ApplyBlending(existingColor, ParticleColor, BlendMode);
                    output.Pixels[pixelIndex] = newColor;
                }
            }
        }
    }
    
    private int ApplyBlending(int existingColor, Color newColor, ParticleBlendMode blendMode)
    {
        switch (blendMode)
        {
            case ParticleBlendMode.Replace:
                return newColor.ToArgb();
                
            case ParticleBlendMode.Blend:
                return BlendColors(existingColor, newColor.ToArgb());
                
            case ParticleBlendMode.Average:
                return AverageColors(existingColor, newColor.ToArgb());
                
            case ParticleBlendMode.Linear:
                return LinearBlendColors(existingColor, newColor.ToArgb());
                
            default:
                return newColor.ToArgb();
        }
    }
    
    private int BlendColors(int colorA, int colorB)
    {
        int r = Math.Min(255, ((colorA >> 16) & 0xFF) + ((colorB >> 16) & 0xFF));
        int g = Math.Min(255, ((colorA >> 8) & 0xFF) + ((colorB >> 8) & 0xFF));
        int b = Math.Min(255, (colorA & 0xFF) + (colorB & 0xFF));
        return (r << 16) | (g << 8) | b;
    }
    
    private int AverageColors(int colorA, int colorB)
    {
        int r = (((colorA >> 16) & 0xFF) + ((colorB >> 16) & 0xFF)) / 2;
        int g = (((colorA >> 8) & 0xFF) + ((colorB >> 8) & 0xFF)) / 2;
        int b = ((colorA & 0xFF) + (colorB & 0xFF)) / 2;
        return (r << 16) | (g << 8) | b;
    }
    
    private int LinearBlendColors(int colorA, int colorB)
    {
        int r = (int)(((colorA >> 16) & 0xFF) * 0.7 + ((colorB >> 16) & 0xFF) * 0.3);
        int g = (int)(((colorA >> 8) & 0xFF) * 0.7 + ((colorB >> 8) & 0xFF) * 0.3);
        int b = (int)((colorA & 0xFF) * 0.7 + (colorB & 0xFF) * 0.3);
        return (r << 16) | (g << 8) | b;
    }
    
    private void InitializeBuffer()
    {
        particleBuffer = new int[1];
        bufferInitialized = true;
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);
    }
}

public enum ParticleBlendMode
{
    Replace = 0,
    Blend = 1,
    Average = 2,
    Linear = 3
}
```

## Usage Examples

### Basic Particle System
```csharp
var particleNode = new ParticleSystemsNode
{
    Enabled = true,
    ParticleColor = Color.White,
    BaseSize = 8,
    MaxDistance = 16,
    BlendMode = ParticleBlendMode.Blend
};
```

### Beat-Reactive Particles
```csharp
var particleNode = new ParticleSystemsNode
{
    Enabled = true,
    BeatSizeEnabled = true,
    BaseSize = 8,
    BeatSize = 16,
    ParticleColor = Color.Cyan,
    BlendMode = ParticleBlendMode.Average
};
```

### Large Organic Particles
```csharp
var particleNode = new ParticleSystemsNode
{
    Enabled = true,
    BaseSize = 32,
    BeatSize = 64,
    MaxDistance = 24,
    ParticleColor = Color.Magenta,
    BlendMode = ParticleBlendMode.Linear
};
```

## Technical Notes

### Physics Simulation
The effect implements a sophisticated physics system:
- **Spring-Damper Model**: Particles are attracted to targets with realistic force
- **Velocity Integration**: Uses Euler integration for smooth motion
- **Momentum Conservation**: Realistic particle behavior with velocity decay
- **Beat Reactivity**: Dynamic target positions based on audio events

### Rendering Algorithm
Advanced particle rendering with multiple options:
- **Circular Particles**: Filled circles with configurable radii
- **Multiple Blending**: Replace, blend, average, and linear modes
- **Size Animation**: Dynamic sizing with beat-reactive changes
- **Boundary Clipping**: Proper screen edge handling

### Performance Characteristics
- **CPU Moderate**: Physics calculations and circular rendering
- **Memory Access**: Efficient particle buffer management
- **Optimization**: Fast blending operations and boundary checking
- **Quality**: Smooth particle motion with realistic physics

This effect is essential for creating dynamic particle animations, organic motion, and beat-reactive visual elements in AVS presets, providing the foundation for many advanced particle-based visualization techniques.

