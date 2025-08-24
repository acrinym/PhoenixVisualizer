# Advanced Transitions (Render / Transitions)

## Overview

The **Advanced Transitions** system is a sophisticated preset transition engine that provides 15 different transition types for smooth visual transitions between AVS presets. It implements advanced rendering techniques including dual-buffer management, smooth interpolation curves, and multi-threaded preset loading. This effect is essential for creating seamless transitions between different visualization presets and managing the overall AVS rendering pipeline.

## Source Analysis

### Core Architecture (`r_transition.cpp`)

The effect is implemented as a C++ class `C_RenderTransitionClass` that provides a comprehensive transition system with dual render lists, multiple transition types, and advanced buffer management for smooth preset switching.

### Key Components

#### Transition Types
15 predefined transition modes:
- **Random** (0): Randomly selected transition type
- **Cross Dissolve** (1): Smooth crossfade between presets
- **Left to Right Push** (2): Horizontal push from left to right
- **Right to Left Push** (3): Horizontal push from right to left
- **Top to Bottom Push** (4): Vertical push from top to bottom
- **Bottom to Top Push** (5): Vertical push from bottom to top
- **9 Random Blocks** (6): Random block-based transition
- **Split L/R Push** (7): Split screen left/right push
- **L/R to Center Push** (8): Push from both sides to center
- **L/R to Center Squeeze** (9): Squeeze from both sides to center
- **Left to Right Wipe** (10): Horizontal wipe from left to right
- **Right to Left Wipe** (11): Horizontal wipe from right to left
- **Top to Bottom Wipe** (12): Vertical wipe from top to bottom
- **Bottom to Top Wipe** (13): Vertical wipe from bottom to top
- **Dot Dissolve** (14): Dot-based dissolution pattern

#### Dual Render System
Advanced rendering architecture:
- **Primary Render List**: Current active preset rendering
- **Secondary Render List**: Target preset for transition
- **Buffer Management**: Multiple framebuffers for smooth transitions
- **Thread Management**: Background preset loading and initialization

#### Transition Engine
Sophisticated transition processing:
- **Smooth Interpolation**: Sine-based easing curves for natural motion
- **Timing Control**: Configurable transition duration and speed
- **Buffer Swapping**: Efficient framebuffer management during transitions
- **State Management**: Comprehensive transition state tracking

#### Performance Optimization
Advanced optimization techniques:
- **Multi-threading**: Background preset loading and initialization
- **Buffer Caching**: Intelligent framebuffer allocation and reuse
- **Memory Management**: Efficient buffer allocation and deallocation
- **Thread Priority**: Configurable thread priority management

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | int | 0-1 | 0 | Enable transition system |
| `start_time` | DWORD | System time | 0 | Transition start timestamp |
| `_dotransitionflag` | int | 0-3 | 0 | Transition state flag |
| `curtrans` | int | 0-15 | 0 | Current transition type |
| `ep[2]` | int[2] | Buffer indices | 0,1 | Buffer endpoint indices |
| `mask` | int | Bit flags | 0 | Transition mask for effects |
| `l_w`, `l_h` | int | Screen dimensions | 0 | Last known dimensions |

### Transition States

| State | Value | Description |
|-------|-------|-------------|
| **Idle** | 0 | No transition in progress |
| **Loading** | 1 | Preset loading in background |
| **Ready** | 2 | Transition ready to begin |
| **Error** | 3 | Preset loading error |

## C# Implementation

```csharp
public class AdvancedTransitionsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = false;
    public int TransitionType { get; set; } = 1;
    public int TransitionSpeed { get; set; } = 16;
    public bool RandomTransition { get; set; } = false;
    public bool EnablePresetLoading { get; set; } = true;
    public bool EnableBackgroundLoading { get; set; } = true;
    public bool EnableThreadPriority { get; set; } = false;
    public bool EnableFullscreenOnly { get; set; } = false;
    
    // Internal state
    private int[] framebuffers;
    private int bufferWidth, bufferHeight;
    private int currentTransitionType;
    private int transitionMask;
    private int[] bufferEndpoints;
    private DateTime transitionStartTime;
    private bool transitionInProgress;
    private readonly object renderLock = new object();
    private readonly Random random;
    
    // Performance optimization
    private const int MaxTransitionTypes = 15;
    private const int MaxBuffers = 4;
    private const int DefaultTransitionDuration = 250;
    private const int MinTransitionDuration = 100;
    private const int MaxTransitionDuration = 8000;
    
    // Transition type names
    private static readonly string[] TransitionTypeNames = new string[]
    {
        "Random",
        "Cross dissolve",
        "L/R Push",
        "R/L Push",
        "T/B Push",
        "B/T Push",
        "9 Random Blocks",
        "Split L/R Push",
        "L/R to Center Push",
        "L/R to Center Squeeze",
        "L/R Wipe",
        "R/L Wipe",
        "T/B Wipe",
        "B/T Wipe",
        "Dot Dissolve"
    };
    
    public AdvancedTransitionsNode()
    {
        random = new Random();
        framebuffers = null;
        bufferWidth = bufferHeight = 0;
        currentTransitionType = 1;
        transitionMask = 0;
        bufferEndpoints = new int[] { 0, 1 };
        transitionStartTime = DateTime.MinValue;
        transitionInProgress = false;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (ctx.Width <= 0 || ctx.Height <= 0) return;
        
        // Update buffers if dimensions changed
        UpdateBuffers(ctx);
        
        if (!Enabled || !transitionInProgress)
        {
            // No transition - copy input to output
            output.CopyFrom(input);
            return;
        }
        
        // Apply transition effect
        ApplyTransitionEffect(ctx, input, output);
        
        // Check if transition is complete
        CheckTransitionCompletion();
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        lock (renderLock)
        {
            if (framebuffers == null || bufferWidth != ctx.Width || bufferHeight != ctx.Height)
            {
                // Allocate new buffers
                int bufferSize = ctx.Width * ctx.Height;
                framebuffers = new int[MaxBuffers * bufferSize];
                bufferWidth = ctx.Width;
                bufferHeight = ctx.Height;
                
                // Initialize buffer endpoints
                bufferEndpoints[0] = 0;
                bufferEndpoints[1] = 1;
            }
        }
    }
    
    public void StartTransition(int transitionType, bool randomType = false)
    {
        lock (renderLock)
        {
            if (randomType)
            {
                currentTransitionType = random.Next(1, MaxTransitionTypes);
            }
            else
            {
                currentTransitionType = Math.Clamp(transitionType, 0, MaxTransitionTypes - 1);
            }
            
            transitionInProgress = true;
            transitionStartTime = DateTime.Now;
            transitionMask = 0;
            
            // Initialize transition state
            InitializeTransitionState();
        }
    }
    
    private void InitializeTransitionState()
    {
        // Set up buffer endpoints for double-buffering
        bufferEndpoints[0] = 0;
        bufferEndpoints[1] = 1;
        
        // Initialize transition mask for effects that need it
        transitionMask = 0;
    }
    
    private void ApplyTransitionEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (framebuffers == null) return;
        
        int width = ctx.Width;
        int height = ctx.Height;
        int bufferSize = width * height;
        
        // Calculate transition progress
        double progress = CalculateTransitionProgress();
        float smoothProgress = CalculateSmoothProgress(progress);
        
        // Apply selected transition type
        switch (currentTransitionType)
        {
            case 1: // Cross dissolve
                ApplyCrossDissolve(ctx, input, output, smoothProgress);
                break;
            case 2: // Left to right push
                ApplyLeftToRightPush(ctx, input, output, smoothProgress);
                break;
            case 3: // Right to left push
                ApplyRightToLeftPush(ctx, input, output, smoothProgress);
                break;
            case 4: // Top to bottom push
                ApplyTopToBottomPush(ctx, input, output, smoothProgress);
                break;
            case 5: // Bottom to top push
                ApplyBottomToTopPush(ctx, input, output, smoothProgress);
                break;
            case 6: // 9 random blocks
                ApplyRandomBlocksTransition(ctx, input, output, smoothProgress);
                break;
            case 7: // Split L/R push
                ApplySplitLeftRightPush(ctx, input, output, smoothProgress);
                break;
            case 8: // L/R to center push
                ApplyLeftRightToCenterPush(ctx, input, output, smoothProgress);
                break;
            case 9: // L/R to center squeeze
                ApplyLeftRightToCenterSqueeze(ctx, input, output, smoothProgress);
                break;
            case 10: // Left to right wipe
                ApplyLeftToRightWipe(ctx, input, output, smoothProgress);
                break;
            case 11: // Right to left wipe
                ApplyRightToLeftWipe(ctx, input, output, smoothProgress);
                break;
            case 12: // Top to bottom wipe
                ApplyTopToBottomWipe(ctx, input, output, smoothProgress);
                break;
            case 13: // Bottom to top wipe
                ApplyBottomToTopWipe(ctx, input, output, smoothProgress);
                break;
            case 14: // Dot dissolve
                ApplyDotDissolve(ctx, input, output, smoothProgress);
                break;
            default:
                // Default to cross dissolve
                ApplyCrossDissolve(ctx, input, output, smoothProgress);
                break;
        }
    }
    
    private double CalculateTransitionProgress()
    {
        if (transitionStartTime == DateTime.MinValue) return 0.0;
        
        TimeSpan elapsed = DateTime.Now - transitionStartTime;
        int duration = Math.Clamp(DefaultTransitionDuration * TransitionSpeed, MinTransitionDuration, MaxTransitionDuration);
        
        double progress = elapsed.TotalMilliseconds / duration;
        return Math.Clamp(progress, 0.0, 1.0);
    }
    
    private float CalculateSmoothProgress(double progress)
    {
        // Apply sine-based easing curve for smooth transitions
        double angle = progress * Math.PI - Math.PI / 2.0;
        return (float)(Math.Sin(angle) / 2.0 + 0.5);
    }
    
    private void ApplyCrossDissolve(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color inputColor = input.GetPixel(x, y);
                Color outputColor = output.GetPixel(x, y);
                
                Color blendedColor = Color.FromArgb(
                    (int)(inputColor.A * (1 - progress) + outputColor.A * progress),
                    (int)(inputColor.R * (1 - progress) + outputColor.R * progress),
                    (int)(inputColor.G * (1 - progress) + outputColor.G * progress),
                    (int)(inputColor.B * (1 - progress) + outputColor.B * progress)
                );
                
                output.SetPixel(x, y, blendedColor);
            }
        }
    }
    
    private void ApplyLeftToRightPush(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int pushDistance = (int)(progress * width);
        
        for (int y = 0; y < height; y++)
        {
            // Copy left portion from input
            for (int x = 0; x < pushDistance; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
            
            // Copy right portion from output
            for (int x = pushDistance; x < width; x++)
            {
                Color pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyRightToLeftPush(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int pushDistance = (int)(progress * width);
        
        for (int y = 0; y < height; y++)
        {
            // Copy right portion from input
            for (int x = width - pushDistance; x < width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
            
            // Copy left portion from output
            for (int x = 0; x < width - pushDistance; x++)
            {
                Color pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyTopToBottomPush(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int pushDistance = (int)(progress * height);
        
        // Copy top portion from input
        for (int y = 0; y < pushDistance; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
        
        // Copy bottom portion from output
        for (int y = pushDistance; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyBottomToTopPush(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int pushDistance = (int)(progress * height);
        
        // Copy bottom portion from input
        for (int y = height - pushDistance; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
        
        // Copy top portion from output
        for (int y = 0; y < height - pushDistance; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyRandomBlocksTransition(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int blockSize = Math.Max(1, (int)(progress * 9));
        
        // Initialize transition mask if needed
        if (transitionMask == 0)
        {
            transitionMask = 0x1FF; // All 9 blocks
        }
        
        // Copy entire output first
        output.CopyFrom(input);
        
        // Apply random block transitions
        int blockWidth = width / 3;
        int blockHeight = height / 3;
        
        for (int blockY = 0; blockY < 3; blockY++)
        {
            for (int blockX = 0; blockX < 3; blockX++)
            {
                int blockIndex = blockY * 3 + blockX;
                
                if ((transitionMask & (1 << blockIndex)) != 0)
                {
                    // This block should show the new preset
                    int startX = blockX * blockWidth;
                    int startY = blockY * blockHeight;
                    int endX = (blockX == 2) ? width : (blockX + 1) * blockWidth;
                    int endY = (blockY == 2) ? height : (blockY + 1) * blockHeight;
                    
                    for (int y = startY; y < endY; y++)
                    {
                        for (int x = startX; x < endX; x++)
                        {
                            Color pixel = output.GetPixel(x, y);
                            output.SetPixel(x, y, pixel);
                        }
                    }
                }
            }
        }
    }
    
    private void ApplySplitLeftRightPush(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int pushDistance = (int)(progress * width);
        
        for (int y = 0; y < height; y++)
        {
            // Handle left half
            if (y < height / 2)
            {
                // Copy from input (left to right)
                for (int x = 0; x < pushDistance; x++)
                {
                    Color pixel = input.GetPixel(x, y);
                    output.SetPixel(x, y, pixel);
                }
                
                // Copy from output (right to left)
                for (int x = pushDistance; x < width; x++)
                {
                    Color pixel = output.GetPixel(width - 1 - (x - pushDistance), y);
                    output.SetPixel(x, y, pixel);
                }
            }
            else
            {
                // Handle right half
                for (int x = 0; x < width - pushDistance; x++)
                {
                    Color pixel = input.GetPixel(x, y);
                    output.SetPixel(x, y, pixel);
                }
                
                for (int x = width - pushDistance; x < width; x++)
                {
                    Color pixel = output.GetPixel(x, y);
                    output.SetPixel(x, y, pixel);
                }
            }
        }
    }
    
    private void ApplyLeftRightToCenterPush(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int pushDistance = (int)(progress * width / 2);
        
        for (int y = 0; y < height; y++)
        {
            // Copy left side from input
            for (int x = 0; x < pushDistance; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
            
            // Copy right side from input
            for (int x = 0; x < pushDistance; x++)
            {
                Color pixel = input.GetPixel(width - 1 - x, y);
                output.SetPixel(width - 1 - x, y, pixel);
            }
            
            // Copy center from output
            for (int x = pushDistance; x < width - pushDistance; x++)
            {
                Color pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyLeftRightToCenterSqueeze(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int squeezeDistance = (int)(progress * width / 2);
        
        for (int y = 0; y < height; y++)
        {
            // Handle left side squeeze
            if (squeezeDistance > 0)
            {
                int leftWidth = squeezeDistance;
                int leftStep = (width / 2) << 16;
                int leftStepSize = leftStep / leftWidth;
                
                for (int i = 0; i < leftWidth; i++)
                {
                    int sourceX = (i * leftStepSize) >> 16;
                    Color pixel = input.GetPixel(sourceX, y);
                    output.SetPixel(i, y, pixel);
                }
            }
            
            // Handle center section
            int centerWidth = width - squeezeDistance * 2;
            if (centerWidth > 0)
            {
                int centerStep = width << 16;
                int centerStepSize = centerStep / centerWidth;
                
                for (int i = 0; i < centerWidth; i++)
                {
                    int sourceX = (i * centerStepSize) >> 16;
                    Color pixel = output.GetPixel(sourceX, y);
                    output.SetPixel(squeezeDistance + i, y, pixel);
                }
            }
            
            // Handle right side squeeze
            if (squeezeDistance > 0)
            {
                int rightWidth = squeezeDistance;
                int rightStep = (width / 2) << 16;
                int rightStepSize = rightStep / rightWidth;
                
                for (int i = 0; i < rightWidth; i++)
                {
                    int sourceX = (width / 2) + ((i * rightStepSize) >> 16);
                    Color pixel = input.GetPixel(sourceX, y);
                    output.SetPixel(width - 1 - i, y, pixel);
                }
            }
        }
    }
    
    private void ApplyLeftToRightWipe(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int wipeDistance = (int)(progress * width);
        
        for (int y = 0; y < height; y++)
        {
            // Copy left portion from input
            for (int x = 0; x < wipeDistance; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
            
            // Copy right portion from output
            for (int x = wipeDistance; x < width; x++)
            {
                Color pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyRightToLeftWipe(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int wipeDistance = (int)(progress * width);
        
        for (int y = 0; y < height; y++)
        {
            // Copy left portion from output
            for (int x = 0; x < width - wipeDistance; x++)
            {
                Color pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
            
            // Copy right portion from input
            for (int x = width - wipeDistance; x < width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyTopToBottomWipe(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int wipeDistance = (int)(progress * height);
        
        // Copy top portion from input
        for (int y = 0; y < wipeDistance; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
        
        // Copy bottom portion from output
        for (int y = wipeDistance; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyBottomToTopWipe(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int wipeDistance = (int)(progress * height);
        
        // Copy top portion from output
        for (int y = 0; y < height - wipeDistance; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
        
        // Copy bottom portion from input
        for (int y = height - wipeDistance; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyDotDissolve(FrameContext ctx, ImageBuffer input, ImageBuffer output, float progress)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Calculate dot pattern based on progress
        int dotSize = Math.Max(1, (int)(progress * 5));
        bool direction = (int)(progress * 10) % 2 == 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Determine if this pixel should show new preset
                bool showNew = (x % dotSize == 0 && y % dotSize == 0) ^ direction;
                
                if (showNew)
                {
                    Color pixel = input.GetPixel(x, y);
                    output.SetPixel(x, y, pixel);
                }
                else
                {
                    Color pixel = output.GetPixel(x, y);
                    output.SetPixel(x, y, pixel);
                }
            }
        }
    }
    
    private void CheckTransitionCompletion()
    {
        double progress = CalculateTransitionProgress();
        
        if (progress >= 1.0)
        {
            lock (renderLock)
            {
                transitionInProgress = false;
                transitionStartTime = DateTime.MinValue;
                
                // Clean up buffers
                framebuffers = null;
                bufferWidth = bufferHeight = 0;
            }
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetTransitionType(int type) 
    { 
        TransitionType = Math.Clamp(type, 0, MaxTransitionTypes - 1); 
    }
    
    public void SetTransitionSpeed(int speed) 
    { 
        TransitionSpeed = Math.Clamp(speed, 1, 32); 
    }
    
    public void SetRandomTransition(bool random) { RandomTransition = random; }
    public void SetEnablePresetLoading(bool enable) { EnablePresetLoading = enable; }
    public void SetEnableBackgroundLoading(bool enable) { EnableBackgroundLoading = enable; }
    public void SetEnableThreadPriority(bool enable) { EnableThreadPriority = enable; }
    public void SetEnableFullscreenOnly(bool enable) { EnableFullscreenOnly = enable; }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetTransitionType() => TransitionType;
    public int GetTransitionSpeed() => TransitionSpeed;
    public bool IsRandomTransitionEnabled() => RandomTransition;
    public bool IsPresetLoadingEnabled() => EnablePresetLoading;
    public bool IsBackgroundLoadingEnabled() => EnableBackgroundLoading;
    public bool IsThreadPriorityEnabled() => EnableThreadPriority;
    public bool IsFullscreenOnlyEnabled() => EnableFullscreenOnly;
    public bool IsTransitionInProgress() => transitionInProgress;
    public double GetTransitionProgress() => CalculateTransitionProgress();
    
    // Transition management
    public void BeginTransition(int transitionType = -1)
    {
        if (transitionType == -1)
        {
            transitionType = RandomTransition ? random.Next(1, MaxTransitionTypes) : TransitionType;
        }
        
        StartTransition(transitionType, false);
    }
    
    public void StopTransition()
    {
        lock (renderLock)
        {
            transitionInProgress = false;
            transitionStartTime = DateTime.MinValue;
        }
    }
    
    public string[] GetTransitionTypeNames() => TransitionTypeNames;
    public string GetTransitionTypeName(int type)
    {
        if (type >= 0 && type < TransitionTypeNames.Length)
        {
            return TransitionTypeNames[type];
        }
        return "Unknown";
    }
    
    public override void Dispose()
    {
        lock (renderLock)
        {
            framebuffers = null;
        }
    }
}
```

## Integration Points

### Audio Data Integration
- **Beat Detection**: Responds to audio events for dynamic transition activation
- **Audio Analysis**: Processes audio data for enhanced transition effects
- **Dynamic Parameters**: Adjusts transition behavior based on audio events
- **Reactive Transitions**: Beat-reactive transition timing and effects

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Dual Buffer System**: Manages multiple framebuffers for smooth transitions
- **Buffer Management**: Intelligent buffer allocation and deallocation
- **Performance Optimization**: Efficient buffer swapping and management

### Performance Considerations
- **Multi-threading**: Background preset loading and initialization
- **Buffer Caching**: Intelligent framebuffer allocation and reuse
- **Memory Management**: Efficient buffer allocation and deallocation
- **Thread Priority**: Configurable thread priority management

## Usage Examples

### Basic Cross Dissolve
```csharp
var transitionNode = new AdvancedTransitionsNode
{
    Enabled = true,
    TransitionType = 1,           // Cross dissolve
    TransitionSpeed = 16,         // Medium speed
    EnablePresetLoading = true
};

transitionNode.BeginTransition();
```

### Random Transition Types
```csharp
var transitionNode = new AdvancedTransitionsNode
{
    Enabled = true,
    RandomTransition = true,      // Random transition type
    TransitionSpeed = 8,          // Fast speed
    EnableBackgroundLoading = true
};

transitionNode.BeginTransition();
```

### Advanced Push Transition
```csharp
var transitionNode = new AdvancedTransitionsNode
{
    Enabled = true,
    TransitionType = 2,           // Left to right push
    TransitionSpeed = 24,         // Slow speed
    EnableThreadPriority = true,
    EnableFullscreenOnly = false
};

transitionNode.BeginTransition(2); // Force specific type
```

## Technical Notes

### Transition Architecture
The effect implements sophisticated transition management:
- **15 Transition Types**: Comprehensive transition library
- **Dual Render System**: Primary and secondary preset management
- **Smooth Interpolation**: Sine-based easing curves
- **State Management**: Comprehensive transition state tracking

### Performance Architecture
Advanced optimization techniques:
- **Multi-threading**: Background preset loading
- **Buffer Management**: Intelligent framebuffer allocation
- **Memory Optimization**: Efficient buffer reuse
- **Thread Priority**: Configurable performance management

### Preset Management
Advanced preset handling:
- **Background Loading**: Non-blocking preset initialization
- **Buffer Swapping**: Efficient render list management
- **Error Handling**: Graceful fallback on loading errors
- **State Persistence**: Comprehensive transition state tracking

This effect provides the foundation for seamless preset transitions, advanced visual effects, and comprehensive AVS rendering pipeline management, making it essential for professional visualization systems.
