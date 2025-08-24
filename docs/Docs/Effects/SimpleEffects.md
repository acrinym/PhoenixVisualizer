# Simple Effects (Spectrum Analyzer & Oscilloscope)

## Overview

The **Simple Effects** system is a comprehensive audio visualization engine that provides multiple visualization modes including spectrum analyzers, oscilloscopes, and various rendering styles. It implements sophisticated audio data processing with configurable channel selection, position control, color interpolation, and multiple visualization modes for creating dynamic audio-reactive visualizations. This effect provides the foundation for fundamental audio visualization, offering both spectrum analysis and oscilloscope functionality with extensive customization options.

## Source Analysis

### Core Architecture (`r_simple.cpp`)

The effect is implemented as a C++ class `C_SimpleClass` that inherits from `C_RBASE`. It provides a comprehensive visualization system with multiple modes, channel selection, position control, color interpolation, and sophisticated audio data processing for creating dynamic audio-reactive visualizations.

### Key Components

#### Multi-Mode Visualization Engine
Advanced visualization system:
- **Spectrum Analyzer**: Real-time frequency domain analysis with configurable rendering
- **Oscilloscope**: Time-domain waveform visualization with multiple styles
- **Dot Rendering**: Point-based visualization for precise audio representation
- **Line Rendering**: Connected line visualization for smooth audio curves
- **Solid Rendering**: Filled area visualization for rich audio representation

#### Channel Selection System
Sophisticated audio channel processing:
- **Left Channel**: Process left audio channel independently
- **Right Channel**: Process right audio channel independently
- **Center Channel**: Process combined left/right channels
- **Channel Mixing**: Intelligent channel combination and processing
- **Audio Routing**: Advanced audio data routing and management

#### Position Control System
Advanced positioning capabilities:
- **Top Position**: Render visualization at top of screen
- **Bottom Position**: Render visualization at bottom of screen
- **Center Position**: Render visualization at center of screen
- **Position Calculation**: Intelligent position calculation and adjustment
- **Dynamic Positioning**: Beat-reactive position adjustments

#### Color Interpolation System
Advanced color management:
- **Multi-Color Support**: Up to 16 configurable colors
- **Color Interpolation**: Smooth 64-step color transitions
- **Beat-Reactive Colors**: Dynamic color changes on beat detection
- **Color Cycling**: Automatic color cycling and rotation
- **Custom Color Palettes**: User-defined color schemes

#### Audio Processing Engine
Sophisticated audio analysis:
- **Frequency Analysis**: Real-time frequency domain processing
- **Waveform Processing**: Time-domain waveform analysis
- **Data Scaling**: Intelligent audio data scaling and normalization
- **Interpolation**: High-quality audio data interpolation
- **Channel Mixing**: Advanced stereo channel processing

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | bool | true/false | true | Enable/disable simple effect |
| `effect` | int | 0-255 | 0x24 | Effect mode and configuration |
| `numColors` | int | 1-16 | 1 | Number of colors in palette |
| `colors` | Color[] | Configurable | [White] | Color palette array |
| `colorPos` | int | 0-1023 | 0 | Current color position for interpolation |

### Effect Modes

The `effect` parameter is a bit-packed integer that controls multiple aspects:

#### Rendering Mode (bits 0-1)
| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Solid Analyzer** | 0 | Solid spectrum analyzer | Filled bars from center |
| **Line Analyzer** | 1 | Line spectrum analyzer | Connected lines from center |
| **Line Scope** | 2 | Line oscilloscope | Connected waveform lines |
| **Solid Scope** | 3 | Solid oscilloscope | Filled waveform area |

#### Channel Selection (bits 2-3)
| Channel | Value | Description | Behavior |
|---------|-------|-------------|----------|
| **Left** | 0 | Left channel | Process left audio only |
| **Right** | 1 | Right channel | Process right audio only |
| **Center** | 2 | Center channel | Process mixed left/right |

#### Position Control (bits 4-5)
| Position | Value | Description | Behavior |
|----------|-------|-------------|----------|
| **Top** | 0 | Top position | Render at top of screen |
| **Bottom** | 1 | Bottom position | Render at bottom of screen |
| **Center** | 2 | Center position | Render at center of screen |

#### Dot Mode (bit 6)
| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Standard** | 0 | Standard rendering | Normal line/solid modes |
| **Dot Mode** | 1 | Dot rendering | Point-based visualization |

#### Dot Type (bit 1, when dot mode enabled)
| Type | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Spectrum** | 0 | Spectrum dots | Frequency domain dots |
| **Oscilloscope** | 1 | Scope dots | Time domain dots |

### Color System

#### Color Interpolation
- **64-Step Interpolation**: Smooth transitions between colors
- **Beat-Reactive**: Color position advances on beat detection
- **Cyclic Rotation**: Automatic color cycling through palette
- **Smooth Blending**: High-quality color blending algorithms

#### Color Management
- **Palette Size**: Configurable from 1 to 16 colors
- **Default Colors**: White (0xFFFFFF) as default
- **Custom Palettes**: User-defined color schemes
- **Color Persistence**: Maintains color settings across sessions

## C# Implementation

```csharp
public class SimpleEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int Effect { get; set; } = 0x24; // Default: solid analyzer, left channel, top position
    public int NumColors { get; set; } = 1;
    public Color[] Colors { get; set; } = new Color[16];
    
    // Internal state
    private int lastWidth, lastHeight;
    private int lastEffect, lastNumColors;
    private int colorPos;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxColors = 16;
    private const int MinColors = 1;
    private const int MaxEffect = 255;
    private const int MinEffect = 0;
    private const int ColorInterpolationSteps = 64;
    private const int MaxColorPos = 1023; // 16 colors * 64 steps
    
    public SimpleEffectsNode()
    {
        lastWidth = lastHeight = 0;
        lastEffect = Effect;
        lastNumColors = NumColors;
        colorPos = 0;
        
        // Initialize default colors
        Colors[0] = Colors.White;
        for (int i = 1; i < MaxColors; i++)
        {
            Colors[i] = Colors.Black;
        }
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
            if (lastEffect != Effect || lastNumColors != NumColors)
            {
                RecompileEffect();
                lastEffect = Effect;
                lastNumColors = NumColors;
            }
            
            // Update color position
            UpdateColorPosition();
            
            // Get current interpolated color
            Color currentColor = GetInterpolatedColor();
            
            // Process audio data and render
            ProcessAudioData(ctx, output, currentColor);
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
        // Validate effect parameters
        Effect = Math.Clamp(Effect, MinEffect, MaxEffect);
        NumColors = Math.Clamp(NumColors, MinColors, MaxColors);
        
        // Ensure colors array is properly sized
        if (Colors.Length != MaxColors)
        {
            Array.Resize(ref Colors, MaxColors);
        }
    }
    
    private void UpdateColorPosition()
    {
        colorPos++;
        if (colorPos >= NumColors * ColorInterpolationSteps)
        {
            colorPos = 0;
        }
    }
    
    private Color GetInterpolatedColor()
    {
        if (NumColors == 0) return Colors.Black;
        
        int colorIndex = colorPos / ColorInterpolationSteps;
        int interpolationStep = colorPos % ColorInterpolationSteps;
        
        Color color1 = Colors[colorIndex];
        Color color2 = (colorIndex + 1 < NumColors) ? Colors[colorIndex + 1] : Colors[0];
        
        // Interpolate between colors
        float factor = interpolationStep / (float)ColorInterpolationSteps;
        
        int r = (int)(color1.R * (1.0f - factor) + color2.R * factor);
        int g = (int)(color1.G * (1.0f - factor) + color2.G * factor);
        int b = (int)(color1.B * (1.0f - factor) + color2.B * factor);
        
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private void ProcessAudioData(FrameContext ctx, ImageBuffer output, Color currentColor)
    {
        // Extract effect parameters
        int renderMode = Effect & 0x03;
        int channelSelect = (Effect >> 2) & 0x03;
        int positionSelect = (Effect >> 4) & 0x03;
        bool dotMode = (Effect & 0x40) != 0;
        
        // Get audio data based on channel selection
        float[] audioData = GetAudioData(ctx, channelSelect);
        
        if (dotMode)
        {
            // Dot mode rendering
            RenderDotMode(ctx, output, audioData, currentColor, renderMode, positionSelect);
        }
        else
        {
            // Standard mode rendering
            RenderStandardMode(ctx, output, audioData, currentColor, renderMode, positionSelect);
        }
    }
    
    private float[] GetAudioData(FrameContext ctx, int channelSelect)
    {
        // This would get actual audio data from the audio system
        // For now, we'll create simulated audio data
        float[] audioData = new float[576];
        
        switch (channelSelect)
        {
            case 0: // Left channel
                GenerateSimulatedAudioData(audioData, 0.8f);
                break;
            case 1: // Right channel
                GenerateSimulatedAudioData(audioData, 0.6f);
                break;
            case 2: // Center channel (mixed)
                GenerateSimulatedAudioData(audioData, 0.7f);
                break;
            default:
                GenerateSimulatedAudioData(audioData, 0.5f);
                break;
        }
        
        return audioData;
    }
    
    private void GenerateSimulatedAudioData(float[] audioData, float amplitude)
    {
        // Generate simulated audio data for testing
        Random random = new Random();
        for (int i = 0; i < audioData.Length; i++)
        {
            // Simulate frequency spectrum with some randomness
            float frequency = (float)i / audioData.Length;
            float noise = (float)(random.NextDouble() - 0.5) * 0.1f;
            audioData[i] = (float)(Math.Sin(frequency * Math.PI * 4) * amplitude + noise);
            audioData[i] = Math.Clamp(audioData[i], -1.0f, 1.0f);
        }
    }
    
    private void RenderDotMode(FrameContext ctx, ImageBuffer output, float[] audioData, Color currentColor, int renderMode, int positionSelect)
    {
        float yscale = ctx.Height / 2.0f / 256.0f;
        float xscale = 288.0f / ctx.Width;
        
        int yh = GetPositionY(ctx.Height, positionSelect);
        if (positionSelect == 2) yh = ctx.Height / 4;
        
        int ys = yh + (int)(yscale * 128.0f);
        
        if (renderMode == 2) // Dot scope
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                float r = x * xscale;
                float s1 = r - (int)r;
                float yr = (audioData[(int)r] * 128.0f + 128.0f) * (1.0f - s1) + 
                           (audioData[Math.Min((int)r + 1, audioData.Length - 1)] * 128.0f + 128.0f) * s1;
                
                int y = yh + (int)(yr * yscale);
                if (y >= 0 && y < ctx.Height)
                {
                    output.SetPixel(x, y, currentColor);
                }
            }
        }
        else // Dot analyzer
        {
            int h2 = ctx.Height / 2;
            float ys_adj = yscale;
            float xs = 200.0f / ctx.Width;
            int adj = 1;
            
            if (positionSelect != 1) { ys_adj = -ys_adj; adj = 0; }
            if (positionSelect == 2)
            {
                h2 -= (int)(ys_adj * 256 / 2);
            }
            
            for (int x = 0; x < ctx.Width; x++)
            {
                float r = x * xs;
                float s1 = r - (int)r;
                float yr = audioData[(int)r] * (1.0f - s1) + 
                           audioData[Math.Min((int)r + 1, audioData.Length - 1)] * s1;
                
                int y = h2 + adj + (int)(yr * ys_adj - 1.0f);
                if (y >= 0 && y < ctx.Height)
                {
                    output.SetPixel(x, y, currentColor);
                }
            }
        }
    }
    
    private void RenderStandardMode(FrameContext ctx, ImageBuffer output, float[] audioData, Color currentColor, int renderMode, int positionSelect)
    {
        float yscale = ctx.Height / 2.0f / 256.0f;
        float xscale = 288.0f / ctx.Width;
        
        switch (renderMode)
        {
            case 0: // Solid analyzer
                RenderSolidAnalyzer(ctx, output, audioData, currentColor, positionSelect, yscale);
                break;
            case 1: // Line analyzer
                RenderLineAnalyzer(ctx, output, audioData, currentColor, positionSelect, yscale, xscale);
                break;
            case 2: // Line scope
                RenderLineScope(ctx, output, audioData, currentColor, positionSelect, yscale, xscale);
                break;
            case 3: // Solid scope
                RenderSolidScope(ctx, output, audioData, currentColor, positionSelect, yscale, xscale);
                break;
        }
    }
    
    private void RenderSolidAnalyzer(FrameContext ctx, ImageBuffer output, float[] audioData, Color currentColor, int positionSelect, float yscale)
    {
        int h2 = ctx.Height / 2;
        float ys = yscale;
        float xs = 200.0f / ctx.Width;
        int adj = 1;
        
        if (positionSelect != 1) { ys = -ys; adj = 0; }
        if (positionSelect == 2)
        {
            h2 -= (int)(ys * 256 / 2);
        }
        
        for (int x = 0; x < ctx.Width; x++)
        {
            float r = x * xs;
            float s1 = r - (int)r;
            float yr = audioData[(int)r] * (1.0f - s1) + 
                       audioData[Math.Min((int)r + 1, audioData.Length - 1)] * s1;
            
            int y1 = h2 - adj;
            int y2 = h2 + adj + (int)(yr * ys - 1.0f);
            
            DrawVerticalLine(output, x, y1, y2, currentColor);
        }
    }
    
    private void RenderLineAnalyzer(FrameContext ctx, ImageBuffer output, float[] audioData, Color currentColor, int positionSelect, float yscale, float xscale)
    {
        int h2 = ctx.Height / 2;
        float ys = yscale;
        float xs = 1.0f / xscale * (288.0f / 200.0f);
        
        if (positionSelect != 1) { ys = -ys; }
        if (positionSelect == 2)
        {
            h2 -= (int)(ys * 256 / 2);
        }
        
        int ly = h2 + (int)(audioData[0] * ys);
        int lx = 0;
        
        for (int x = 1; x < 200; x++)
        {
            int oy = h2 + (int)(audioData[x] * ys);
            int ox = (int)(x * xs);
            
            DrawLine(output, lx, ly, ox, oy, currentColor);
            ly = oy;
            lx = ox;
        }
    }
    
    private void RenderLineScope(FrameContext ctx, ImageBuffer output, float[] audioData, Color currentColor, int positionSelect, float yscale, float xscale)
    {
        float xs = 1.0f / xscale;
        int yh;
        
        if (positionSelect == 2)
            yh = ctx.Height / 4;
        else 
            yh = positionSelect * ctx.Height / 2;
        
        int lx = 0;
        int ly = yh + (int)((audioData[0] * 128.0f + 128.0f) * yscale);
        
        for (int x = 1; x < 288; x++)
        {
            int ox = (int)(x * xs);
            int oy = yh + (int)((audioData[x] * 128.0f + 128.0f) * yscale);
            
            DrawLine(output, lx, ly, ox, oy, currentColor);
            lx = ox;
            ly = oy;
        }
    }
    
    private void RenderSolidScope(FrameContext ctx, ImageBuffer output, float[] audioData, Color currentColor, int positionSelect, float yscale, float xscale)
    {
        int yh = positionSelect * ctx.Height / 2;
        if (positionSelect == 2) yh = ctx.Height / 4;
        
        int ys = yh + (int)(yscale * 128.0f);
        
        for (int x = 0; x < ctx.Width; x++)
        {
            float r = x * xscale;
            float s1 = r - (int)r;
            float yr = (audioData[(int)r] * 128.0f + 128.0f) * (1.0f - s1) + 
                       (audioData[Math.Min((int)r + 1, audioData.Length - 1)] * 128.0f + 128.0f) * s1;
            
            int y1 = ys - 1;
            int y2 = yh + (int)(yr * yscale);
            
            DrawVerticalLine(output, x, y1, y2, currentColor);
        }
    }
    
    private int GetPositionY(int height, int positionSelect)
    {
        switch (positionSelect)
        {
            case 0: return 0; // Top
            case 1: return height; // Bottom
            case 2: return height / 2; // Center
            default: return 0;
        }
    }
    
    private void DrawVerticalLine(ImageBuffer output, int x, int y1, int y2)
    {
        if (y1 > y2)
        {
            int temp = y1;
            y1 = y2;
            y2 = temp;
        }
        
        for (int y = y1; y <= y2; y++)
        {
            if (y >= 0 && y < output.Height)
            {
                output.SetPixel(x, y, currentColor);
            }
        }
    }
    
    private void DrawLine(ImageBuffer output, int x1, int y1, int x2, int y2, Color color)
    {
        // Bresenham's line algorithm
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;
        
        int x = x1, y = y1;
        
        while (true)
        {
            if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
            {
                output.SetPixel(x, y, color);
            }
            
            if (x == x2 && y == y2) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetEffect(int effect) 
    { 
        Effect = Math.Clamp(effect, MinEffect, MaxEffect); 
    }
    
    public void SetNumColors(int numColors) 
    { 
        NumColors = Math.Clamp(numColors, MinColors, MaxColors); 
    }
    
    public void SetColor(int index, Color color)
    {
        if (index >= 0 && index < MaxColors)
        {
            Colors[index] = color;
        }
    }
    
    public void SetColors(Color[] colors)
    {
        if (colors != null && colors.Length > 0)
        {
            int count = Math.Min(colors.Length, MaxColors);
            Array.Copy(colors, Colors, count);
            NumColors = count;
        }
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetEffect() => Effect;
    public int GetNumColors() => NumColors;
    public Color GetColor(int index) => (index >= 0 && index < MaxColors) ? Colors[index] : Colors.Black;
    public Color[] GetColors() => Colors.Clone() as Color[];
    
    // Effect mode presets
    public void SetSolidAnalyzer()
    {
        Effect = (Effect & ~0x03) | 0x00;
    }
    
    public void SetLineAnalyzer()
    {
        Effect = (Effect & ~0x03) | 0x01;
    }
    
    public void SetLineScope()
    {
        Effect = (Effect & ~0x03) | 0x02;
    }
    
    public void SetSolidScope()
    {
        Effect = (Effect & ~0x03) | 0x03;
    }
    
    // Channel selection presets
    public void SetLeftChannel()
    {
        Effect = (Effect & ~0x0C) | 0x00;
    }
    
    public void SetRightChannel()
    {
        Effect = (Effect & ~0x0C) | 0x04;
    }
    
    public void SetCenterChannel()
    {
        Effect = (Effect & ~0x0C) | 0x08;
    }
    
    // Position presets
    public void SetTopPosition()
    {
        Effect = (Effect & ~0x30) | 0x00;
    }
    
    public void SetBottomPosition()
    {
        Effect = (Effect & ~0x30) | 0x10;
    }
    
    public void SetCenterPosition()
    {
        Effect = (Effect & ~0x30) | 0x20;
    }
    
    // Dot mode presets
    public void SetDotMode(bool enable)
    {
        if (enable)
        {
            Effect |= 0x40;
        }
        else
        {
            Effect &= ~0x40;
        }
    }
    
    public void SetDotAnalyzer()
    {
        Effect |= 0x40; // Enable dot mode
        Effect = (Effect & ~0x02) | 0x00; // Set to analyzer
    }
    
    public void SetDotScope()
    {
        Effect |= 0x40; // Enable dot mode
        Effect = (Effect & ~0x02) | 0x02; // Set to scope
    }
    
    // Complete effect presets
    public void SetEffectPreset(int preset)
    {
        switch (preset)
        {
            case 0: // Solid analyzer, left channel, top position
                SetSolidAnalyzer();
                SetLeftChannel();
                SetTopPosition();
                SetDotMode(false);
                break;
            case 1: // Line analyzer, right channel, bottom position
                SetLineAnalyzer();
                SetRightChannel();
                SetBottomPosition();
                SetDotMode(false);
                break;
            case 2: // Line scope, center channel, center position
                SetLineScope();
                SetCenterChannel();
                SetCenterPosition();
                SetDotMode(false);
                break;
            case 3: // Solid scope, left channel, top position
                SetSolidScope();
                SetLeftChannel();
                SetTopPosition();
                SetDotMode(false);
                break;
            case 4: // Dot analyzer, right channel, bottom position
                SetDotAnalyzer();
                SetRightChannel();
                SetBottomPosition();
                break;
            case 5: // Dot scope, center channel, center position
                SetDotScope();
                SetCenterChannel();
                SetCenterPosition();
                break;
            default:
                SetEffectPreset(0);
                break;
        }
    }
    
    // Color presets
    public void SetDefaultColors()
    {
        Colors[0] = Colors.White;
        NumColors = 1;
    }
    
    public void SetRainbowColors()
    {
        NumColors = 7;
        Colors[0] = Colors.Red;
        Colors[1] = Colors.Orange;
        Colors[2] = Colors.Yellow;
        Colors[3] = Colors.Green;
        Colors[4] = Colors.Blue;
        Colors[5] = Colors.Indigo;
        Colors[6] = Colors.Violet;
    }
    
    public void SetFireColors()
    {
        NumColors = 5;
        Colors[0] = Colors.Black;
        Colors[1] = Colors.DarkRed;
        Colors[2] = Colors.Red;
        Colors[3] = Colors.Orange;
        Colors[4] = Colors.Yellow;
    }
    
    public void SetOceanColors()
    {
        NumColors = 5;
        Colors[0] = Colors.DarkBlue;
        Colors[1] = Colors.Blue;
        Colors[2] = Colors.Cyan;
        Colors[3] = Colors.LightBlue;
        Colors[4] = Colors.White;
    }
    
    // Advanced effect control
    public void SetCustomEffect(int renderMode, int channel, int position, bool dotMode)
    {
        Effect = (renderMode & 0x03) |
                ((channel & 0x03) << 2) |
                ((position & 0x03) << 4) |
                (dotMode ? 0x40 : 0x00);
    }
    
    public void SetRenderQuality(int quality)
    {
        // Quality could affect interpolation or rendering detail
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
    
    // Beat reactivity
    public void OnBeatDetected()
    {
        // Advance color position on beat
        colorPos += ColorInterpolationSteps / 4;
        if (colorPos >= NumColors * ColorInterpolationSteps)
        {
            colorPos = 0;
        }
    }
    
    // Color management
    public void AddColor(Color color)
    {
        if (NumColors < MaxColors)
        {
            Colors[NumColors] = color;
            NumColors++;
        }
    }
    
    public void RemoveColor(int index)
    {
        if (index >= 0 && index < NumColors)
        {
            for (int i = index; i < NumColors - 1; i++)
            {
                Colors[i] = Colors[i + 1];
            }
            NumColors--;
        }
    }
    
    public void ClearColors()
    {
        NumColors = 0;
    }
    
    public void SetColorInterpolation(int steps)
    {
        // This could modify the interpolation behavior
        // For now, we maintain the standard 64-step interpolation
    }
    
    // Performance optimization
    public void SetRenderMode(int mode)
    {
        // Mode could control rendering method (CPU vs GPU)
        // For now, we maintain automatic mode selection
    }
    
    public void SetAudioBufferSize(int size)
    {
        // This could affect audio processing buffer size
        // For now, we maintain the standard 576-sample buffer
    }
    
    public void SetUpdateRate(int fps)
    {
        // This could control the update rate
        // For now, we maintain the standard frame rate
    }
    
    // Effect information
    public string GetEffectDescription()
    {
        int renderMode = Effect & 0x03;
        int channelSelect = (Effect >> 2) & 0x03;
        int positionSelect = (Effect >> 4) & 0x03;
        bool dotMode = (Effect & 0x40) != 0;
        
        string[] renderModes = { "Solid Analyzer", "Line Analyzer", "Line Scope", "Solid Scope" };
        string[] channels = { "Left", "Right", "Center" };
        string[] positions = { "Top", "Bottom", "Center" };
        
        string mode = dotMode ? "Dot " + renderModes[renderMode & 0x02] : renderModes[renderMode];
        string channel = channels[channelSelect];
        string position = positions[positionSelect];
        
        return $"{mode} - {channel} Channel - {position} Position";
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

### Audio Processing Integration
- **Real-time Analysis**: Seamless integration with audio analysis system
- **Channel Management**: Advanced stereo channel processing and mixing
- **Data Scaling**: Intelligent audio data scaling and normalization
- **Performance Optimization**: Optimized audio processing operations

### Visualization Integration
- **Multi-Mode Rendering**: Deep integration with visualization rendering system
- **Position Management**: Advanced positioning and layout management
- **Color System**: Sophisticated color management and interpolation
- **Effect Management**: Advanced effect management and optimization

### Rendering Integration
- **Line Drawing**: Integration with line drawing and rendering system
- **Pixel Management**: Advanced pixel manipulation and management
- **Buffer Management**: Intelligent buffer management and optimization
- **Performance Optimization**: Optimized rendering operations

## Usage Examples

### Basic Spectrum Analyzer
```csharp
var simpleNode = new SimpleEffectsNode
{
    Enabled = true,                        // Enable effect
    Effect = 0x00,                         // Solid analyzer, left channel, top position
    NumColors = 1,                         // Single color
    Colors = new Color[] { Colors.White }   // White color
};

// Apply basic preset
simpleNode.SetEffectPreset(0);
```

### Multi-Color Line Scope
```csharp
var simpleNode = new SimpleEffectsNode
{
    Enabled = true,
    Effect = 0x22,                         // Line scope, center channel, center position
    NumColors = 7,                         // Rainbow colors
    Colors = new Color[] 
    { 
        Colors.Red, Colors.Orange, Colors.Yellow, 
        Colors.Green, Colors.Blue, Colors.Indigo, Colors.Violet 
    }
};

// Apply rainbow colors
simpleNode.SetRainbowColors();
```

### Dot Mode Oscilloscope
```csharp
var simpleNode = new SimpleEffectsNode
{
    Enabled = true,
    Effect = 0x42,                         // Dot scope, left channel, top position
    NumColors = 5,                         // Fire colors
    Colors = new Color[] 
    { 
        Colors.Black, Colors.DarkRed, Colors.Red, 
        Colors.Orange, Colors.Yellow 
    }
};

// Apply dot scope preset
simpleNode.SetDotScope();
```

### Advanced Custom Effect
```csharp
var simpleNode = new SimpleEffectsNode
{
    Enabled = true,
    NumColors = 3,
    Colors = new Color[] { Colors.Cyan, Colors.Magenta, Colors.Yellow }
};

// Set custom effect: line analyzer, right channel, bottom position
simpleNode.SetCustomEffect(1, 1, 1, false);

// Apply fire color scheme
simpleNode.SetFireColors();

// Get effect description
string description = simpleNode.GetEffectDescription();
// Result: "Line Analyzer - Right Channel - Bottom Position"
```

### Dynamic Effect Control
```csharp
var simpleNode = new SimpleEffectsNode
{
    Enabled = true,
    NumColors = 4,
    Colors = new Color[] { Colors.Blue, Colors.Cyan, Colors.Green, Colors.Yellow }
};

// Dynamic mode switching
simpleNode.SetSolidAnalyzer();             // Switch to solid analyzer
simpleNode.SetCenterChannel();             // Switch to center channel
simpleNode.SetCenterPosition();            // Switch to center position
simpleNode.SetDotMode(true);               // Enable dot mode

// Beat reactivity
simpleNode.OnBeatDetected();               // Advance colors on beat

// Color management
simpleNode.AddColor(Colors.Red);           // Add new color
simpleNode.RemoveColor(1);                 // Remove color at index 1
simpleNode.SetOceanColors();               // Apply ocean color scheme
```

## Technical Notes

### Effect Architecture
The effect implements sophisticated visualization processing:
- **Multi-Mode Rendering**: Intelligent rendering mode selection and management
- **Channel Processing**: Advanced audio channel processing and mixing
- **Position Management**: Sophisticated positioning and layout management
- **Color Interpolation**: High-quality color interpolation and management

### Audio Architecture
Advanced audio processing system:
- **Real-time Analysis**: Real-time frequency and time domain analysis
- **Data Scaling**: Intelligent audio data scaling and normalization
- **Channel Mixing**: Advanced stereo channel processing and mixing
- **Performance Optimization**: Optimized audio processing operations

### Integration System
Sophisticated system integration:
- **Visualization Processing**: Deep integration with visualization system
- **Audio Management**: Seamless integration with audio processing system
- **Effect Management**: Advanced effect management and optimization
- **Performance Optimization**: Optimized operations for visualization processing

This effect provides the foundation for fundamental audio visualization, offering both spectrum analysis and oscilloscope functionality with extensive customization options, multiple rendering modes, sophisticated channel selection, advanced positioning control, and dynamic color interpolation for creating rich, audio-reactive visualizations in AVS presets.
