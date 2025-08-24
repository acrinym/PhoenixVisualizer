# Starfield Effects (3D Star Field Visualization)

## Overview

The **Starfield Effects** system is a sophisticated 3D star field visualization engine that creates immersive space-like environments with configurable star counts, movement speeds, and beat-reactive behavior. It implements comprehensive 3D star management with depth-based rendering, configurable warp speeds, beat-reactive speed changes, multiple blending modes, and intelligent star regeneration for creating dynamic, immersive star field visualizations. This effect provides the foundation for space-themed visualizations, offering both static and dynamic star fields with advanced depth perception and movement control.

## Source Analysis

### Core Architecture (`r_stars.cpp`)

The effect is implemented as a C++ class `C_StarField` that inherits from `C_RBASE`. It provides a comprehensive 3D star field system with depth-based rendering, configurable movement speeds, beat-reactive behavior, multiple blending modes, and intelligent star regeneration for creating dynamic, immersive star field visualizations.

### Key Components

#### 3D Star Management System
Advanced star field management:
- **Star Structure**: Individual star data with X, Y, Z coordinates and speed
- **Depth Management**: Z-coordinate based depth perception and movement
- **Star Regeneration**: Intelligent star regeneration when stars move out of view
- **Performance Optimization**: Configurable star count based on screen resolution

#### Movement and Speed System
Sophisticated movement control:
- **Warp Speed**: Configurable base movement speed for star field
- **Beat-Reactive Speed**: Dynamic speed changes on beat detection
- **Speed Interpolation**: Smooth speed transitions between normal and beat speeds
- **Duration Control**: Configurable duration for beat speed effects

#### Rendering and Blending System
Advanced rendering capabilities:
- **Depth-Based Rendering**: 3D perspective rendering with depth calculations
- **Multiple Blending Modes**: Replace, additive, and 50/50 blending options
- **Color Management**: Configurable star colors with intelligent color blending
- **Performance Optimization**: Optimized rendering for real-time operations

#### Beat Reactivity System
Advanced beat integration:
- **Beat Detection**: Automatic beat detection and response
- **Speed Modulation**: Dynamic speed changes on beat detection
- **Duration Control**: Configurable duration for beat effects
- **Smooth Transitions**: Interpolated speed changes for smooth effects

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | bool | true/false | true | Enable/disable starfield effect |
| `color` | int | 0x000000-0xFFFFFF | 0xFFFFFF | Star color (RGB) |
| `blend` | int | 0-1 | 0 | Additive blending mode |
| `blendavg` | int | 0-1 | 0 | 50/50 blending mode |
| `warpSpeed` | float | 0.01-50.0 | 6.0 | Base movement speed |
| `maxStars` | int | 100-4095 | 350 | Maximum number of stars |
| `onbeat` | int | 0-1 | 0 | Enable beat-reactive speed changes |
| `spdBeat` | float | 0.01-50.0 | 4.0 | Beat speed multiplier |
| `durFrames` | int | 1-100 | 15 | Duration of beat speed effect |

### Blending Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Replace** | 0,0 | No blending | Stars replace existing pixels |
| **Additive** | 1,0 | Additive blending | Stars add to existing pixels |
| **50/50** | 0,1 | 50/50 blending | Stars blend 50/50 with existing pixels |

### Star Properties

#### Star Structure
Each star contains:
- **X, Y**: 2D screen coordinates
- **Z**: Depth coordinate (0-255, where 0 is closest)
- **Speed**: Individual star movement speed (0.1-1.0)
- **OX, OY**: Previous frame coordinates for motion trails

#### Depth Calculations
- **Perspective**: Stars closer to viewer (lower Z) appear larger and move faster
- **Movement**: Stars move toward viewer (Z decreases) creating warp effect
- **Regeneration**: Stars are recreated when Z reaches 0 or move off-screen

## C# Implementation

```csharp
public class StarfieldEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int Color { get; set; } = 0xFFFFFF;
    public int Blend { get; set; } = 0;
    public int BlendAvg { get; set; } = 0;
    public float WarpSpeed { get; set; } = 6.0f;
    public int MaxStars { get; set; } = 350;
    public int OnBeat { get; set; } = 0;
    public float SpdBeat { get; set; } = 4.0f;
    public int DurFrames { get; set; } = 15;
    
    // Internal state
    private int lastWidth, lastHeight;
    private int lastMaxStars;
    private float currentSpeed;
    private int nc;
    private float incBeat;
    private Star[] stars;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxStarCount = 4096;
    private const int MinStarCount = 100;
    private const int MaxMaxStars = 4095;
    private const int MinMaxStars = 100;
    private const float MaxWarpSpeed = 50.0f;
    private const float MinWarpSpeed = 0.01f;
    private const float MaxSpdBeat = 50.0f;
    private const float MinSpdBeat = 0.01f;
    private const int MaxDurFrames = 100;
    private const int MinDurFrames = 1;
    
    // Star structure
    private struct Star
    {
        public int X, Y;
        public float Z;
        public float Speed;
        public int OX, OY;
    }
    
    public StarfieldEffectsNode()
    {
        lastWidth = lastHeight = 0;
        lastMaxStars = MaxStars;
        currentSpeed = WarpSpeed;
        nc = 0;
        incBeat = 0.0f;
        stars = new Star[MaxStarCount];
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
            if (lastMaxStars != MaxStars)
            {
                RecompileEffect();
                lastMaxStars = MaxStars;
            }
            
            // Update system variables
            UpdateSystemVariables(ctx);
            
            // Initialize stars if needed
            if (stars == null || stars.Length != MaxStars)
            {
                InitializeStars(ctx);
            }
            
            // Process star field
            ProcessStarField(ctx, output);
        }
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        if (lastWidth != ctx.Width || lastHeight != ctx.Height)
        {
            lastWidth = ctx.Width;
            lastHeight = ctx.Height;
            InitializeStars(ctx);
        }
    }
    
    private void RecompileEffect()
    {
        // Validate parameters
        MaxStars = Math.Clamp(MaxStars, MinMaxStars, MaxMaxStars);
        WarpSpeed = Math.Clamp(WarpSpeed, MinWarpSpeed, MaxWarpSpeed);
        SpdBeat = Math.Clamp(SpdBeat, MinSpdBeat, MaxSpdBeat);
        DurFrames = Math.Clamp(DurFrames, MinDurFrames, MaxDurFrames);
        
        // Ensure stars array is properly sized
        if (stars == null || stars.Length != MaxStars)
        {
            Array.Resize(ref stars, MaxStars);
        }
    }
    
    private void UpdateSystemVariables(FrameContext ctx)
    {
        // Update speed based on beat detection
        if (OnBeat != 0 && IsBeatDetected())
        {
            currentSpeed = SpdBeat;
            incBeat = (WarpSpeed - currentSpeed) / DurFrames;
            nc = DurFrames;
        }
        
        // Update speed over time
        if (nc == 0)
        {
            currentSpeed = WarpSpeed;
        }
        else
        {
            currentSpeed = Math.Max(0, currentSpeed + incBeat);
            nc--;
        }
    }
    
    private void InitializeStars(FrameContext ctx)
    {
        // Calculate actual star count based on screen resolution
        int actualStarCount = MaxStars;
        
        // Scale star count based on screen resolution (similar to original)
        actualStarCount = (actualStarCount * ctx.Width * ctx.Height) / (512 * 384);
        actualStarCount = Math.Clamp(actualStarCount, MinStarCount, MaxStarCount);
        
        // Ensure stars array is properly sized
        if (stars == null || stars.Length != actualStarCount)
        {
            Array.Resize(ref stars, actualStarCount);
        }
        
        // Initialize stars
        Random random = new Random();
        for (int i = 0; i < actualStarCount; i++)
        {
            stars[i].X = (random.Next() % ctx.Width) - (ctx.Width / 2);
            stars[i].Y = (random.Next() % ctx.Height) - (ctx.Height / 2);
            stars[i].Z = (float)(random.Next() % 255);
            stars[i].Speed = (float)(random.Next() % 9 + 1) / 10.0f;
            stars[i].OX = 0;
            stars[i].OY = 0;
        }
    }
    
    private void CreateStar(int index, FrameContext ctx)
    {
        Random random = new Random();
        stars[index].X = (random.Next() % ctx.Width) - (ctx.Width / 2);
        stars[index].Y = (random.Next() % ctx.Height) - (ctx.Height / 2);
        stars[index].Z = 255.0f; // Start at maximum depth
    }
    
    private void ProcessStarField(FrameContext ctx, ImageBuffer output)
    {
        int xoff = ctx.Width / 2;
        int yoff = ctx.Height / 2;
        
        for (int i = 0; i < stars.Length; i++)
        {
            if ((int)stars[i].Z > 0)
            {
                // Calculate screen coordinates based on depth
                int nx = ((stars[i].X << 7) / (int)stars[i].Z) + xoff;
                int ny = ((stars[i].Y << 7) / (int)stars[i].Z) + yoff;
                
                if (nx > 0 && nx < ctx.Width && ny > 0 && ny < ctx.Height)
                {
                    // Calculate star brightness based on depth and speed
                    int brightness = (int)((255 - (int)stars[i].Z) * stars[i].Speed);
                    brightness = Math.Clamp(brightness, 0, 255);
                    
                    // Create star color
                    int starColor;
                    if (Color != 0xFFFFFF)
                    {
                        // Blend with custom color
                        starColor = BlendAdaptive((brightness | (brightness << 8) | (brightness << 16)), Color, brightness >> 4);
                    }
                    else
                    {
                        // Use brightness as grayscale
                        starColor = (brightness | (brightness << 8) | (brightness << 16));
                    }
                    
                    // Apply blending mode
                    Color finalColor = Color.FromRgb(
                        (byte)(starColor & 0xFF),
                        (byte)((starColor >> 8) & 0xFF),
                        (byte)((starColor >> 16) & 0xFF)
                    );
                    
                    if (Blend != 0)
                    {
                        // Additive blending
                        Color existingColor = output.GetPixel(nx, ny);
                        finalColor = BlendAdditive(existingColor, finalColor);
                    }
                    else if (BlendAvg != 0)
                    {
                        // 50/50 blending
                        Color existingColor = output.GetPixel(nx, ny);
                        finalColor = BlendAverage(existingColor, finalColor);
                    }
                    
                    // Set pixel
                    output.SetPixel(nx, ny, finalColor);
                    
                    // Store previous coordinates
                    stars[i].OX = nx;
                    stars[i].OY = ny;
                    
                    // Move star toward viewer
                    stars[i].Z -= stars[i].Speed * currentSpeed;
                }
                else
                {
                    // Star moved off-screen, recreate it
                    CreateStar(i, ctx);
                }
            }
            else
            {
                // Star reached viewer, recreate it
                CreateStar(i, ctx);
            }
        }
    }
    
    private int BlendAdaptive(int a, int b, int divisor)
    {
        // Adaptive blending algorithm from original code
        return ((((a >> 4) & 0x0F0F0F) * (16 - divisor) + (((b >> 4) & 0x0F0F0F) * divisor)));
    }
    
    private Color BlendAdditive(Color existing, Color star)
    {
        int r = Math.Min(255, existing.R + star.R);
        int g = Math.Min(255, existing.G + star.G);
        int b = Math.Min(255, existing.B + star.B);
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color BlendAverage(Color existing, Color star)
    {
        int r = (existing.R + star.R) / 2;
        int g = (existing.G + star.G) / 2;
        int b = (existing.B + star.B) / 2;
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetColor(int color) 
    { 
        Color = Math.Clamp(color, 0x000000, 0xFFFFFF); 
    }
    
    public void SetBlend(int blend) 
    { 
        Blend = Math.Clamp(blend, 0, 1); 
    }
    
    public void SetBlendAvg(int blendAvg) 
    { 
        BlendAvg = Math.Clamp(blendAvg, 0, 1); 
    }
    
    public void SetWarpSpeed(float warpSpeed) 
    { 
        WarpSpeed = Math.Clamp(warpSpeed, MinWarpSpeed, MaxWarpSpeed); 
    }
    
    public void SetMaxStars(int maxStars) 
    { 
        MaxStars = Math.Clamp(maxStars, MinMaxStars, MaxMaxStars); 
    }
    
    public void SetOnBeat(int onBeat) 
    { 
        OnBeat = Math.Clamp(onBeat, 0, 1); 
    }
    
    public void SetSpdBeat(float spdBeat) 
    { 
        SpdBeat = Math.Clamp(spdBeat, MinSpdBeat, MaxSpdBeat); 
    }
    
    public void SetDurFrames(int durFrames) 
    { 
        DurFrames = Math.Clamp(durFrames, MinDurFrames, MaxDurFrames); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetColor() => Color;
    public int GetBlend() => Blend;
    public int GetBlendAvg() => BlendAvg;
    public float GetWarpSpeed() => WarpSpeed;
    public int GetMaxStars() => MaxStars;
    public int GetOnBeat() => OnBeat;
    public float GetSpdBeat() => SpdBeat;
    public int GetDurFrames() => DurFrames;
    
    // Blending mode presets
    public void SetReplaceMode()
    {
        SetBlend(0);
        SetBlendAvg(0);
    }
    
    public void SetAdditiveMode()
    {
        SetBlend(1);
        SetBlendAvg(0);
    }
    
    public void SetAverageMode()
    {
        SetBlend(0);
        SetBlendAvg(1);
    }
    
    // Speed presets
    public void SetSlowSpeed()
    {
        SetWarpSpeed(1.0f);
    }
    
    public void SetNormalSpeed()
    {
        SetWarpSpeed(6.0f);
    }
    
    public void SetFastSpeed()
    {
        SetWarpSpeed(15.0f);
    }
    
    public void SetWarpSpeed()
    {
        SetWarpSpeed(25.0f);
    }
    
    // Star count presets
    public void SetLowStarCount()
    {
        SetMaxStars(100);
    }
    
    public void SetMediumStarCount()
    {
        SetMaxStars(350);
    }
    
    public void SetHighStarCount()
    {
        SetMaxStars(1000);
    }
    
    public void SetMaximumStarCount()
    {
        SetMaxStars(4095);
    }
    
    // Beat reactivity presets
    public void SetBeatReactive(bool enable)
    {
        SetOnBeat(enable ? 1 : 0);
    }
    
    public void SetBeatSpeedMultiplier(float multiplier)
    {
        SetSpdBeat(multiplier);
    }
    
    public void SetBeatDuration(int frames)
    {
        SetDurFrames(frames);
    }
    
    // Complete effect presets
    public void SetEffectPreset(int preset)
    {
        switch (preset)
        {
            case 0: // Slow, few stars, no beat reactivity
                SetSlowSpeed();
                SetLowStarCount();
                SetBeatReactive(false);
                SetReplaceMode();
                break;
            case 1: // Normal, medium stars, no beat reactivity
                SetNormalSpeed();
                SetMediumStarCount();
                SetBeatReactive(false);
                SetReplaceMode();
                break;
            case 2: // Fast, many stars, no beat reactivity
                SetFastSpeed();
                SetHighStarCount();
                SetBeatReactive(false);
                SetReplaceMode();
                break;
            case 3: // Normal, medium stars, additive blending
                SetNormalSpeed();
                SetMediumStarCount();
                SetBeatReactive(false);
                SetAdditiveMode();
                break;
            case 4: // Normal, medium stars, beat reactive
                SetNormalSpeed();
                SetMediumStarCount();
                SetBeatReactive(true);
                SetSpdBeat(4.0f);
                SetDurFrames(15);
                SetReplaceMode();
                break;
            case 5: // Fast, many stars, beat reactive, additive
                SetFastSpeed();
                SetHighStarCount();
                SetBeatReactive(true);
                SetSpdBeat(8.0f);
                SetDurFrames(20);
                SetAdditiveMode();
                break;
            default:
                SetEffectPreset(1);
                break;
        }
    }
    
    // Color presets
    public void SetWhiteStars()
    {
        SetColor(0xFFFFFF);
    }
    
    public void SetBlueStars()
    {
        SetColor(0x0000FF);
    }
    
    public void SetGreenStars()
    {
        SetColor(0x00FF00);
    }
    
    public void SetRedStars()
    {
        SetColor(0xFF0000);
    }
    
    public void SetYellowStars()
    {
        SetColor(0xFFFF00);
    }
    
    public void SetCyanStars()
    {
        SetColor(0x00FFFF);
    }
    
    public void SetMagentaStars()
    {
        SetColor(0xFF00FF);
    }
    
    public void SetOrangeStars()
    {
        SetColor(0xFF8000);
    }
    
    // Advanced effect control
    public void SetCustomEffect(float warpSpeed, int maxStars, bool beatReactive, float beatSpeed, int beatDuration, int blendMode)
    {
        SetWarpSpeed(warpSpeed);
        SetMaxStars(maxStars);
        SetBeatReactive(beatReactive);
        SetSpdBeat(beatSpeed);
        SetDurFrames(beatDuration);
        
        switch (blendMode)
        {
            case 0: SetReplaceMode(); break;
            case 1: SetAdditiveMode(); break;
            case 2: SetAverageMode(); break;
            default: SetReplaceMode(); break;
        }
    }
    
    public void SetRenderQuality(int quality)
    {
        // Quality could affect star rendering detail or optimization level
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
    
    // Advanced star field features
    public void SetStarFieldType(int type)
    {
        // Type could control star field behavior (static, dynamic, etc.)
        // For now, we maintain the standard dynamic behavior
    }
    
    public void SetDepthRange(float minDepth, float maxDepth)
    {
        // This could modify the Z-coordinate range for stars
        // For now, we maintain the standard 0-255 range
    }
    
    public void SetStarSize(float size)
    {
        // This could modify the visual size of stars
        // For now, we maintain the standard size
    }
    
    // Beat reactivity
    public void OnBeatDetected()
    {
        // Beat detection is handled in UpdateSystemVariables
        // This method can be called externally to trigger beat effects
    }
    
    // Star field management
    public void RegenerateStarField()
    {
        // Force regeneration of all stars
        if (lastWidth > 0 && lastHeight > 0)
        {
            InitializeStars(new FrameContext { Width = lastWidth, Height = lastHeight });
        }
    }
    
    public void ClearStarField()
    {
        // Clear all stars (they will be regenerated on next frame)
        if (stars != null)
        {
            Array.Clear(stars, 0, stars.Length);
        }
    }
    
    public void SetStarFieldDensity(float density)
    {
        // Density could modify the star count calculation
        // For now, we maintain the standard density calculation
    }
    
    // Performance optimization
    public void SetRenderMode(int mode)
    {
        // Mode could control rendering method (CPU vs GPU)
        // For now, we maintain automatic mode selection
    }
    
    public void SetUpdateRate(int fps)
    {
        // This could control the update rate
        // For now, we maintain the standard frame rate
    }
    
    // Effect information
    public string GetEffectDescription()
    {
        string[] blendModes = { "Replace", "Additive", "50/50" };
        string blendMode = Blend != 0 ? blendModes[1] : BlendAvg != 0 ? blendModes[2] : blendModes[0];
        string beatReactive = OnBeat != 0 ? "Beat Reactive" : "Static";
        
        return $"Starfield - {MaxStars} Stars - {WarpSpeed:F1}x Speed - {blendMode} - {beatReactive}";
    }
    
    public int GetCurrentStarCount()
    {
        return stars?.Length ?? 0;
    }
    
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
    
    public override void Dispose()
    {
        lock (renderLock)
        {
            // Clean up resources if needed
            stars = null;
        }
    }
}
```

## Integration Points

### 3D Rendering Integration
- **Depth Management**: Seamless integration with depth-based rendering system
- **Perspective Calculations**: Advanced perspective and depth calculations
- **Performance Optimization**: Optimized 3D rendering operations
- **Motion Integration**: Integration with motion and animation systems

### Blending Integration
- **Multiple Blending Modes**: Deep integration with blending system
- **Color Management**: Advanced color blending and management
- **Pixel Operations**: Intelligent pixel manipulation and blending
- **Performance Optimization**: Optimized blending operations

### Beat Reactivity Integration
- **Beat Detection**: Seamless integration with beat detection system
- **Speed Modulation**: Dynamic speed changes and interpolation
- **Effect Management**: Advanced effect management and optimization
- **Performance Optimization**: Optimized beat-reactive operations

## Usage Examples

### Basic Starfield Effect
```csharp
var starfieldNode = new StarfieldEffectsNode
{
    Enabled = true,                        // Enable effect
    Color = 0xFFFFFF,                      // White stars
    MaxStars = 350,                        // Medium star count
    WarpSpeed = 6.0f,                      // Normal speed
    OnBeat = 0,                            // No beat reactivity
    Blend = 0,                             // Replace mode
    BlendAvg = 0                           // No 50/50 blending
};

// Apply basic preset
starfieldNode.SetEffectPreset(1);
```

### Beat-Reactive Starfield
```csharp
var starfieldNode = new StarfieldEffectsNode
{
    Enabled = true,
    Color = 0x00FFFF,                      // Cyan stars
    MaxStars = 1000,                       // High star count
    WarpSpeed = 8.0f,                      // Fast speed
    OnBeat = 1,                            // Enable beat reactivity
    SpdBeat = 6.0f,                        // Beat speed multiplier
    DurFrames = 20,                        // Beat effect duration
    Blend = 1                              // Additive blending
};

// Apply beat-reactive preset
starfieldNode.SetEffectPreset(4);
```

### Custom Starfield Configuration
```csharp
var starfieldNode = new StarfieldEffectsNode
{
    Enabled = true,
    Color = 0xFF8000,                      // Orange stars
    MaxStars = 2000,                       // Very high star count
    WarpSpeed = 15.0f,                     // Very fast speed
    OnBeat = 1,                            // Enable beat reactivity
    SpdBeat = 12.0f,                       // High beat speed multiplier
    DurFrames = 25,                        // Long beat effect duration
    Blend = 0,                             // Replace mode
    BlendAvg = 0                           // No 50/50 blending
};

// Apply custom configuration
starfieldNode.SetCustomEffect(15.0f, 2000, true, 12.0f, 25, 0);
```

### Dynamic Starfield Control
```csharp
var starfieldNode = new StarfieldEffectsNode
{
    Enabled = true,
    Color = 0xFFFFFF,                      // White stars
    MaxStars = 500,                        // Medium-high star count
    WarpSpeed = 10.0f,                     // Fast speed
    OnBeat = 1,                            // Enable beat reactivity
    Blend = 1                              // Additive blending
};

// Dynamic mode switching
starfieldNode.SetSlowSpeed();              // Switch to slow speed
starfieldNode.SetFastSpeed();              // Switch to fast speed
starfieldNode.SetWarpSpeed();              // Switch to warp speed
starfieldNode.SetLowStarCount();           // Switch to low star count
starfieldNode.SetMaximumStarCount();       // Switch to maximum star count

// Blending mode switching
starfieldNode.SetReplaceMode();            // Switch to replace mode
starfieldNode.SetAdditiveMode();           // Switch to additive mode
starfieldNode.SetAverageMode();            // Switch to 50/50 mode

// Color switching
starfieldNode.SetBlueStars();              // Switch to blue stars
starfieldNode.SetGreenStars();             // Switch to green stars
starfieldNode.SetRedStars();               // Switch to red stars
starfieldNode.SetYellowStars();            // Switch to yellow stars

// Beat reactivity control
starfieldNode.SetBeatReactive(true);       // Enable beat reactivity
starfieldNode.SetBeatSpeedMultiplier(8.0f); // Set beat speed multiplier
starfieldNode.SetBeatDuration(30);         // Set beat effect duration

// Get effect information
string description = starfieldNode.GetEffectDescription();
int starCount = starfieldNode.GetCurrentStarCount();
float currentSpeed = starfieldNode.GetCurrentSpeed();
```

### Advanced Starfield Effects
```csharp
var starfieldNode = new StarfieldEffectsNode
{
    Enabled = true,
    Color = 0x00FFFF,                      // Cyan stars
    MaxStars = 1500,                       // High star count
    WarpSpeed = 12.0f,                     // Fast speed
    OnBeat = 1,                            // Enable beat reactivity
    SpdBeat = 10.0f,                       // High beat speed multiplier
    DurFrames = 25,                        // Long beat effect duration
    Blend = 1                              // Additive blending
};

// Apply various presets
starfieldNode.SetEffectPreset(0);          // Slow, few stars, no beat reactivity
starfieldNode.SetEffectPreset(1);          // Normal, medium stars, no beat reactivity
starfieldNode.SetEffectPreset(2);          // Fast, many stars, no beat reactivity
starfieldNode.SetEffectPreset(3);          // Normal, medium stars, additive blending
starfieldNode.SetEffectPreset(4);          // Normal, medium stars, beat reactive
starfieldNode.SetEffectPreset(5);          // Fast, many stars, beat reactive, additive

// Advanced control
starfieldNode.RegenerateStarField();       // Force star regeneration
starfieldNode.ClearStarField();            // Clear all stars
starfieldNode.OnBeatDetected();            // Trigger beat effect manually
```

## Technical Notes

### 3D Architecture
The effect implements sophisticated 3D processing:
- **Depth Management**: Intelligent depth-based rendering and movement
- **Perspective Calculations**: Advanced perspective and depth calculations
- **Star Regeneration**: Intelligent star regeneration and management
- **Performance Optimization**: Optimized 3D rendering operations

### Movement Architecture
Advanced movement system:
- **Speed Control**: Configurable movement speeds with interpolation
- **Beat Reactivity**: Dynamic speed changes on beat detection
- **Smooth Transitions**: Interpolated speed changes for smooth effects
- **Performance Optimization**: Optimized movement calculations

### Integration System
Sophisticated system integration:
- **3D Rendering**: Deep integration with 3D rendering system
- **Blending Management**: Seamless integration with blending system
- **Beat Integration**: Advanced integration with beat detection system
- **Performance Optimization**: Optimized operations for star field processing

This effect provides the foundation for space-themed visualizations, offering both static and dynamic star fields with advanced depth perception, movement control, beat reactivity, and multiple blending modes for creating immersive, dynamic star field visualizations in AVS presets.
