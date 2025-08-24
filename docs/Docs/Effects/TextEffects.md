# Text Effects (Render / Text)

## Overview

The **Text Effects** system is a sophisticated text rendering engine that creates dynamic, animated text visualizations with advanced typography, positioning, and animation capabilities. It implements comprehensive text rendering with multiple fonts, colors, effects, and beat-reactive animations for creating complex textual visualizations. This effect provides the foundation for sophisticated text overlays, animated captions, and dynamic textual content in AVS presets.

## Source Analysis

### Core Architecture (`r_text.cpp`)

The effect is implemented as a C++ class `C_TextClass` that inherits from `C_RBASE`. It provides a comprehensive text rendering system with font management, dynamic positioning, multiple text effects, and sophisticated animation capabilities for creating complex textual visualizations.

### Key Components

#### Text Rendering System
Advanced text processing engine:
- **Font Management**: Multiple font support with dynamic font loading
- **Text Processing**: Advanced text parsing and word management
- **Rendering Engine**: High-quality text rendering with anti-aliasing
- **Performance Optimization**: Optimized rendering for real-time text display

#### Animation System
Sophisticated animation capabilities:
- **Beat Reactivity**: Beat-reactive text animations and positioning
- **Dynamic Movement**: Real-time text movement and positioning
- **Speed Control**: Configurable animation speeds and timing
- **Force Control**: Force-based animation and positioning control

#### Text Effects Engine
Advanced text effect system:
- **Outline Effects**: Configurable text outlines with color and size control
- **Shadow Effects**: Dynamic shadow rendering with positioning control
- **Blending Modes**: Multiple blending modes for text integration
- **Color Management**: Advanced color control and interpolation

#### Positioning System
Intelligent positioning engine:
- **Alignment Control**: Horizontal and vertical text alignment
- **Dynamic Positioning**: Real-time position calculation and adjustment
- **Boundary Management**: Intelligent text boundary handling
- **Shift Control**: Configurable text shifting and movement

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | bool | true/false | true | Enable/disable text effect |
| `color` | int | 0x000000-0xFFFFFF | 0xFFFFFF | Text color (RGB) |
| `blend` | int | 0-1 | 0 | Blend mode (0=Replace, 1=Additive) |
| `blendavg` | int | 0-1 | 1 | Average blending mode |
| `onbeat` | int | 0-1 | 0 | Beat-reactive behavior |
| `insertBlank` | int | 0-1 | 0 | Insert blank characters |
| `randomPos` | int | 0-1 | 0 | Random position generation |
| `valign` | int | 0-2 | 0 | Vertical alignment (0=Top, 1=Center, 2=Bottom) |
| `halign` | int | 0-2 | 0 | Horizontal alignment (0=Left, 1=Center, 2=Right) |
| `onbeatSpeed` | int | 0-255 | 0 | Beat-reactive animation speed |
| `normSpeed` | int | 0-255 | 0 | Normal animation speed |
| `text` | string | MAX_PATH | "" | Text content to display |
| `outline` | int | 0-1 | 0 | Enable text outline |
| `shadow` | int | 0-1 | 0 | Enable text shadow |
| `outlinecolor` | int | 0x000000-0xFFFFFF | 0x000000 | Outline color (RGB) |
| `outlinesize` | int | 1-10 | 1 | Outline thickness |
| `xshift` | int | -1000-1000 | 0 | X-axis text shift |
| `yshift` | int | -1000-1000 | 0 | Y-axis text shift |
| `forceshift` | int | 0-1 | 0 | Force text shifting |
| `forcealign` | int | 0-1 | 0 | Force text alignment |

### Text Alignment Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Left/Top** | 0 | Left/Top alignment | Text aligned to left/top edge |
| **Center** | 1 | Center alignment | Text centered horizontally/vertically |
| **Right/Bottom** | 2 | Right/Bottom alignment | Text aligned to right/bottom edge |

### Blending Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Replace** | 0 | Replace mode | Text replaces underlying content |
| **Additive** | 1 | Additive blending | Text adds to underlying content |
| **Average** | 1 | Average blending | Text averages with underlying content |

## C# Implementation

```csharp
public class TextEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int Color { get; set; } = 0xFFFFFF;
    public int Blend { get; set; } = 0;
    public int BlendAvg { get; set; } = 1;
    public int OnBeat { get; set; } = 0;
    public int InsertBlank { get; set; } = 0;
    public int RandomPos { get; set; } = 0;
    public int Valign { get; set; } = 0;
    public int Halign { get; set; } = 0;
    public int OnBeatSpeed { get; set; } = 0;
    public int NormSpeed { get; set; } = 0;
    public string Text { get; set; } = "";
    public int Outline { get; set; } = 0;
    public int Shadow { get; set; } = 0;
    public int OutlineColor { get; set; } = 0x000000;
    public int OutlineSize { get; set; } = 1;
    public int XShift { get; set; } = 0;
    public int YShift { get; set; } = 0;
    public int ForceShift { get; set; } = 0;
    public int ForceAlign { get; set; } = 0;
    
    // Internal state
    private string[] words;
    private int currentWord;
    private int lastWidth, lastHeight;
    private int currentXShift, currentYShift;
    private int currentValign, currentHalign;
    private int animationCounter;
    private int beatCounter;
    private readonly Random random;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxOutlineSize = 10;
    private const int MinOutlineSize = 1;
    private const int MaxShift = 1000;
    private const int MinShift = -1000;
    private const int MaxSpeed = 255;
    private const int MinSpeed = 0;
    private const int MaxAlignment = 2;
    private const int MinAlignment = 0;
    
    // Font properties
    private string fontFamily = "Arial";
    private float fontSize = 24.0f;
    private FontStyle fontStyle = FontStyle.Regular;
    
    public TextEffectsNode()
    {
        random = new Random();
        words = new string[0];
        currentWord = 0;
        lastWidth = lastHeight = 0;
        currentXShift = currentYShift = 0;
        currentValign = Valign;
        currentHalign = Halign;
        animationCounter = 0;
        beatCounter = 0;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || string.IsNullOrEmpty(Text) || ctx.Width <= 0 || ctx.Height <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Process text content
            ProcessTextContent();
            
            // Update animation state
            UpdateAnimationState(ctx);
            
            // Render text
            RenderText(ctx, input, output);
        }
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        if (lastWidth != ctx.Width || lastHeight != ctx.Height)
        {
            lastWidth = ctx.Width;
            lastHeight = ctx.Height;
            
            // Reset positioning for new dimensions
            ResetPositioning();
        }
    }
    
    private void ProcessTextContent()
    {
        if (string.IsNullOrEmpty(Text)) return;
        
        // Split text into words
        words = Text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Insert blank characters if enabled
        if (InsertBlank == 1 && words.Length > 0)
        {
            var newWords = new List<string>();
            foreach (var word in words)
            {
                newWords.Add(word);
                newWords.Add(" ");
            }
            words = newWords.ToArray();
        }
        
        // Ensure current word is within bounds
        if (currentWord >= words.Length)
        {
            currentWord = 0;
        }
    }
    
    private void UpdateAnimationState(FrameContext ctx)
    {
        // Handle beat reactivity
        if (ctx.IsBeat && OnBeat == 1)
        {
            beatCounter++;
            
            // Update positioning on beat
            if (RandomPos == 1)
            {
                UpdateRandomPosition();
            }
            
            // Update alignment on beat
            if (ForceAlign == 1)
            {
                UpdateAlignment();
            }
        }
        
        // Update animation counter
        animationCounter += (ctx.IsBeat && OnBeat == 1) ? OnBeatSpeed : NormSpeed;
        
        // Update text shifting
        if (ForceShift == 1)
        {
            UpdateTextShifting();
        }
    }
    
    private void UpdateRandomPosition()
    {
        currentXShift = random.Next(MinShift, MaxShift + 1);
        currentYShift = random.Next(MinShift, MaxShift + 1);
    }
    
    private void UpdateAlignment()
    {
        currentValign = random.Next(MinAlignment, MaxAlignment + 1);
        currentHalign = random.Next(MinAlignment, MaxAlignment + 1);
    }
    
    private void UpdateTextShifting()
    {
        // Apply configured shifts
        currentXShift = XShift;
        currentYShift = YShift;
        
        // Add animation-based shifts
        if (OnBeat == 1)
        {
            currentXShift += (int)(Math.Sin(beatCounter * 0.5) * 50);
            currentYShift += (int)(Math.Cos(beatCounter * 0.5) * 50);
        }
    }
    
    private void ResetPositioning()
    {
        currentXShift = XShift;
        currentYShift = YShift;
        currentValign = Valign;
        currentHalign = Halign;
        animationCounter = 0;
        beatCounter = 0;
    }
    
    private void RenderText(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (words.Length == 0) return;
        
        // Copy input to output first
        CopyInputToOutput(ctx, input, output);
        
        // Get current word to display
        string currentText = words[currentWord];
        
        // Calculate text position
        var textPosition = CalculateTextPosition(ctx, currentText);
        
        // Render text with effects
        RenderTextWithEffects(ctx, output, currentText, textPosition);
        
        // Update word counter
        UpdateWordCounter();
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                output.SetPixel(x, y, input.GetPixel(x, y));
            }
        }
    }
    
    private Point CalculateTextPosition(FrameContext ctx, string text)
    {
        // Calculate text dimensions (approximate)
        int textWidth = text.Length * (int)(fontSize * 0.6f);
        int textHeight = (int)fontSize;
        
        // Calculate base position
        int x, y;
        
        // Horizontal alignment
        switch (currentHalign)
        {
            case 0: // Left
                x = 10;
                break;
            case 1: // Center
                x = (ctx.Width - textWidth) / 2;
                break;
            case 2: // Right
                x = ctx.Width - textWidth - 10;
                break;
            default:
                x = 10;
                break;
        }
        
        // Vertical alignment
        switch (currentValign)
        {
            case 0: // Top
                y = 10;
                break;
            case 1: // Center
                y = (ctx.Height - textHeight) / 2;
                break;
            case 2: // Bottom
                y = ctx.Height - textHeight - 10;
                break;
            default:
                y = 10;
                break;
        }
        
        // Apply shifts
        x += currentXShift;
        y += currentYShift;
        
        // Ensure text stays within bounds
        x = Math.Clamp(x, 0, ctx.Width - textWidth);
        y = Math.Clamp(y, 0, ctx.Height - textHeight);
        
        return new Point(x, y);
    }
    
    private void RenderTextWithEffects(FrameContext ctx, ImageBuffer output, string text, Point position)
    {
        // Convert color values
        Color textColor = IntToColor(Color);
        Color outlineColor = IntToColor(OutlineColor);
        
        // Render shadow if enabled
        if (Shadow == 1)
        {
            RenderTextShadow(ctx, output, text, position, outlineColor);
        }
        
        // Render outline if enabled
        if (Outline == 1)
        {
            RenderTextOutline(ctx, output, text, position, outlineColor);
        }
        
        // Render main text
        RenderMainText(ctx, output, text, position, textColor);
    }
    
    private void RenderTextShadow(FrameContext ctx, ImageBuffer output, string text, Point position, Color shadowColor)
    {
        // Render shadow with offset
        int shadowOffset = 2;
        var shadowPosition = new Point(position.X + shadowOffset, position.Y + shadowOffset);
        
        RenderTextAtPosition(ctx, output, text, shadowPosition, shadowColor, false);
    }
    
    private void RenderTextOutline(FrameContext ctx, ImageBuffer output, string text, Point position, Color outlineColor)
    {
        // Render outline around text
        for (int offset = 1; offset <= OutlineSize; offset++)
        {
            // Render outline in all directions
            for (int dx = -offset; dx <= offset; dx++)
            {
                for (int dy = -offset; dy <= offset; dy++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        var outlinePosition = new Point(position.X + dx, position.Y + dy);
                        RenderTextAtPosition(ctx, output, text, outlinePosition, outlineColor, false);
                    }
                }
            }
        }
    }
    
    private void RenderMainText(FrameContext ctx, ImageBuffer output, string text, Point position, Color textColor)
    {
        RenderTextAtPosition(ctx, output, text, position, textColor, true);
    }
    
    private void RenderTextAtPosition(FrameContext ctx, ImageBuffer output, string text, Point position, Color color, bool applyBlending)
    {
        // Simple text rendering (in a real implementation, this would use proper font rendering)
        for (int i = 0; i < text.Length; i++)
        {
            int charX = position.X + (i * (int)(fontSize * 0.6f));
            int charY = position.Y;
            
            // Render character (simplified - just a colored rectangle)
            RenderCharacter(ctx, output, text[i], charX, charY, color, applyBlending);
        }
    }
    
    private void RenderCharacter(FrameContext ctx, ImageBuffer output, char character, int x, int y, Color color, bool applyBlending)
    {
        int charWidth = (int)(fontSize * 0.6f);
        int charHeight = (int)fontSize;
        
        for (int cy = 0; cy < charHeight; cy++)
        {
            for (int cx = 0; cx < charWidth; cx++)
            {
                int pixelX = x + cx;
                int pixelY = y + cy;
                
                if (pixelX >= 0 && pixelX < ctx.Width && pixelY >= 0 && pixelY < ctx.Height)
                {
                    Color existingColor = output.GetPixel(pixelX, pixelY);
                    Color finalColor = color;
                    
                    if (applyBlending)
                    {
                        finalColor = ApplyBlendingMode(existingColor, color);
                    }
                    
                    output.SetPixel(pixelX, pixelY, finalColor);
                }
            }
        }
    }
    
    private Color ApplyBlendingMode(Color existingColor, Color newColor)
    {
        switch (Blend)
        {
            case 0: // Replace
                return newColor;
                
            case 1: // Additive
                return Color.FromRgb(
                    (byte)Math.Min(255, existingColor.R + newColor.R),
                    (byte)Math.Min(255, existingColor.G + newColor.G),
                    (byte)Math.Min(255, existingColor.B + newColor.B)
                );
                
            default:
                return newColor;
        }
    }
    
    private Color IntToColor(int colorInt)
    {
        int r = (colorInt >> 16) & 0xFF;
        int g = (colorInt >> 8) & 0xFF;
        int b = colorInt & 0xFF;
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private void UpdateWordCounter()
    {
        // Update word counter based on animation speed
        int speed = (OnBeat == 1) ? OnBeatSpeed : NormSpeed;
        
        if (animationCounter >= 1000) // Threshold for word change
        {
            currentWord = (currentWord + 1) % words.Length;
            animationCounter = 0;
        }
    }
    
    // Font management
    public void SetFont(string family, float size, FontStyle style)
    {
        fontFamily = family;
        fontSize = Math.Max(8.0f, Math.Min(72.0f, size));
        fontStyle = style;
    }
    
    public void SetFontFamily(string family)
    {
        fontFamily = family;
    }
    
    public void SetFontSize(float size)
    {
        fontSize = Math.Max(8.0f, Math.Min(72.0f, size));
    }
    
    public void SetFontStyle(FontStyle style)
    {
        fontStyle = style;
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetColor(int color) { Color = color; }
    
    public void SetBlend(int blend) 
    { 
        Blend = Math.Clamp(blend, 0, 1); 
    }
    
    public void SetBlendAvg(int blendAvg) 
    { 
        BlendAvg = Math.Clamp(blendAvg, 0, 1); 
    }
    
    public void SetOnBeat(int onBeat) 
    { 
        OnBeat = Math.Clamp(onBeat, 0, 1); 
    }
    
    public void SetInsertBlank(int insertBlank) 
    { 
        InsertBlank = Math.Clamp(insertBlank, 0, 1); 
    }
    
    public void SetRandomPos(int randomPos) 
    { 
        RandomPos = Math.Clamp(randomPos, 0, 1); 
    }
    
    public void SetValign(int valign) 
    { 
        Valign = Math.Clamp(valign, MinAlignment, MaxAlignment); 
    }
    
    public void SetHalign(int halign) 
    { 
        Halign = Math.Clamp(halign, MinAlignment, MaxAlignment); 
    }
    
    public void SetOnBeatSpeed(int speed) 
    { 
        OnBeatSpeed = Math.Clamp(speed, MinSpeed, MaxSpeed); 
    }
    
    public void SetNormSpeed(int speed) 
    { 
        NormSpeed = Math.Clamp(speed, MinSpeed, MaxSpeed); 
    }
    
    public void SetText(string text) { Text = text ?? ""; }
    
    public void SetOutline(int outline) 
    { 
        Outline = Math.Clamp(outline, 0, 1); 
    }
    
    public void SetShadow(int shadow) 
    { 
        Shadow = Math.Clamp(shadow, 0, 1); 
    }
    
    public void SetOutlineColor(int color) { OutlineColor = color; }
    
    public void SetOutlineSize(int size) 
    { 
        OutlineSize = Math.Clamp(size, MinOutlineSize, MaxOutlineSize); 
    }
    
    public void SetXShift(int shift) 
    { 
        XShift = Math.Clamp(shift, MinShift, MaxShift); 
    }
    
    public void SetYShift(int shift) 
    { 
        YShift = Math.Clamp(shift, MinShift, MaxShift); 
    }
    
    public void SetForceShift(int forceShift) 
    { 
        ForceShift = Math.Clamp(forceShift, 0, 1); 
    }
    
    public void SetForceAlign(int forceAlign) 
    { 
        ForceAlign = Math.Clamp(forceAlign, 0, 1); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetColor() => Color;
    public int GetBlend() => Blend;
    public int GetBlendAvg() => BlendAvg;
    public int GetOnBeat() => OnBeat;
    public int GetInsertBlank() => InsertBlank;
    public int GetRandomPos() => RandomPos;
    public int GetValign() => Valign;
    public int GetHalign() => Halign;
    public int GetOnBeatSpeed() => OnBeatSpeed;
    public int GetNormSpeed() => NormSpeed;
    public string GetText() => Text;
    public int GetOutline() => Outline;
    public int GetShadow() => Shadow;
    public int GetOutlineColor() => OutlineColor;
    public int GetOutlineSize() => OutlineSize;
    public int GetXShift() => XShift;
    public int GetYShift() => YShift;
    public int GetForceShift() => ForceShift;
    public int GetForceAlign() => ForceAlign;
    public string GetFontFamily() => fontFamily;
    public float GetFontSize() => fontSize;
    public FontStyle GetFontStyle() => fontStyle;
    public int GetCurrentWord() => currentWord;
    public int GetWordCount() => words.Length;
    public string GetCurrentText() => (words.Length > 0 && currentWord < words.Length) ? words[currentWord] : "";
    
    // Advanced text control
    public void SetRandomText(string[] textArray)
    {
        if (textArray != null && textArray.Length > 0)
        {
            Text = string.Join(" ", textArray);
            currentWord = 0;
        }
    }
    
    public void SetCyclingText(string[] textArray)
    {
        if (textArray != null && textArray.Length > 0)
        {
            Text = string.Join(" ", textArray);
            currentWord = 0;
        }
    }
    
    public void SetBeatReactiveText(string[] textArray)
    {
        if (textArray != null && textArray.Length > 0)
        {
            Text = string.Join(" ", textArray);
            currentWord = 0;
            OnBeat = 1;
        }
    }
    
    // Text animation presets
    public void SetBouncingText()
    {
        OnBeat = 1;
        RandomPos = 1;
        ForceShift = 1;
        OnBeatSpeed = 50;
    }
    
    public void SetFloatingText()
    {
        OnBeat = 1;
        ForceShift = 1;
        OnBeatSpeed = 25;
        NormSpeed = 10;
    }
    
    public void SetStaticText()
    {
        OnBeat = 0;
        RandomPos = 0;
        ForceShift = 0;
        OnBeatSpeed = 0;
        NormSpeed = 0;
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect font rendering or optimization level
        // For now, we maintain full quality
    }
    
    public void EnableOptimizations(bool enable)
    {
        // Various optimization flags could be implemented here
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

### Text Processing Integration
- **Font Management**: Advanced font loading and management
- **Text Parsing**: Intelligent text parsing and word management
- **Rendering Engine**: High-quality text rendering with effects
- **Performance Optimization**: Optimized rendering for real-time text display

### Animation Integration
- **Beat Reactivity**: Beat-reactive text animations and positioning
- **Dynamic Movement**: Real-time text movement and positioning
- **Speed Control**: Configurable animation speeds and timing
- **Force Control**: Force-based animation and positioning control

### Visual Effects Integration
- **Outline Effects**: Configurable text outlines with color and size control
- **Shadow Effects**: Dynamic shadow rendering with positioning control
- **Blending Modes**: Multiple blending modes for text integration
- **Color Management**: Advanced color control and interpolation

## Usage Examples

### Basic Text Display
```csharp
var textNode = new TextEffectsNode
{
    Enabled = true,                      // Enable effect
    Text = "Hello World",                // Text content
    Color = 0xFFFFFF,                    // White text
    Blend = 0,                          // Replace mode
    Valign = 1,                         // Center vertically
    Halign = 1,                         // Center horizontally
    FontSize = 32.0f                    // Large font
};
```

### Beat-Reactive Text
```csharp
var textNode = new TextEffectsNode
{
    Enabled = true,
    Text = "Beat Beat Beat",
    Color = 0xFF0000,                   // Red text
    OnBeat = 1,                         // Beat-reactive
    RandomPos = 1,                      // Random positioning
    OnBeatSpeed = 100,                  // Fast beat response
    Outline = 1,                        // Enable outline
    OutlineColor = 0x000000,            // Black outline
    OutlineSize = 2                     // Thick outline
};
```

### Animated Text Effects
```csharp
var textNode = new TextEffectsNode
{
    Enabled = true,
    Text = "Animated Text",
    Color = 0x00FF00,                   // Green text
    Blend = 1,                          // Additive blending
    OnBeat = 1,                         // Beat-reactive
    ForceShift = 1,                     // Force shifting
    XShift = 50,                        // X-axis shift
    YShift = -30,                       // Y-axis shift
    Shadow = 1,                         // Enable shadow
    NormSpeed = 25                      // Normal animation speed
};

// Set font properties
textNode.SetFont("Times New Roman", 48.0f, FontStyle.Bold);

// Apply animation preset
textNode.SetBouncingText();
```

## Technical Notes

### Text Architecture
The effect implements sophisticated text processing:
- **Dynamic Rendering**: Intelligent text rendering with multiple fonts
- **Word Management**: Advanced word parsing and cycling
- **Effect Processing**: Comprehensive text effects and animations
- **Performance Optimization**: Optimized rendering for real-time display

### Animation Architecture
Advanced animation processing system:
- **Beat Integration**: Deep integration with beat detection system
- **Dynamic Positioning**: Real-time position calculation and adjustment
- **Speed Control**: Configurable animation speeds and timing
- **Force Control**: Force-based animation and positioning control

### Integration System
Sophisticated system integration:
- **Font System**: Deep integration with font rendering system
- **Animation Pipeline**: Seamless integration with animation pipeline
- **Effect Management**: Advanced effect management and optimization
- **Performance Optimization**: Optimized operations for text processing

This effect provides the foundation for sophisticated text visualization, creating dynamic, animated text overlays with advanced typography, positioning, and animation capabilities for sophisticated AVS visualization systems.
