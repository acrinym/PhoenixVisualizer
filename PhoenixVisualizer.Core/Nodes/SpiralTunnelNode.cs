using System;
using System.Collections.Generic;
using System.Numerics;

namespace PhoenixVisualizer.Core.Nodes;

public class SpiralTunnelNode : IEffectNode
{
    public string Name => "Spiral Tunnel";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["twist"] = new EffectParam{ Label="Twist", Type="slider", Min=0, Max=10, FloatValue=5 },
        ["zoomSpeed"] = new EffectParam{ Label="Zoom Speed", Type="slider", Min=0.1f, Max=5f, FloatValue=1f },
        ["color"] = new EffectParam{ Label="Color", Type="color", ColorValue="#FF33FF" },
        ["depth"] = new EffectParam{ Label="Depth", Type="slider", Min=1, Max=20, FloatValue=10 },
        ["spiralTurns"] = new EffectParam{ Label="Spiral Turns", Type="slider", Min=1, Max=8, FloatValue=4 },
        ["waveformReactivity"] = new EffectParam{ Label="Waveform Reactivity", Type="slider", Min=0, Max=2, FloatValue=1 },
        ["spectrumReactivity"] = new EffectParam{ Label="Spectrum Reactivity", Type="slider", Min=0, Max=2, FloatValue=1 },
        ["animationSpeed"] = new EffectParam{ Label="Animation Speed", Type="slider", Min=0.1f, Max=3f, FloatValue=1f }
    };

    private float _time = 0f;

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        if (ctx.Canvas == null) return;

        _time += 0.016f * Params["animationSpeed"].FloatValue;

        // Clear canvas
        ctx.Canvas.Clear(0xFF000000);

        // Get parameters
        float twist = Params["twist"].FloatValue;
        float zoomSpeed = Params["zoomSpeed"].FloatValue;
        float depth = Params["depth"].FloatValue;
        float spiralTurns = Params["spiralTurns"].FloatValue;
        float waveformReactivity = Params["waveformReactivity"].FloatValue;
        float spectrumReactivity = Params["spectrumReactivity"].FloatValue;

        // Parse color
        uint baseColor = ParseColor(Params["color"].ColorValue);

        // Calculate center
        float centerX = ctx.Width / 2f;
        float centerY = ctx.Height / 2f;
        float maxRadius = Math.Min(centerX, centerY);

        // Get audio reactivity
        float audioEnergy = GetAudioEnergy(waveform, spectrum);
        float twistModulation = twist + (audioEnergy * spectrumReactivity);
        float zoomModulation = zoomSpeed + (audioEnergy * waveformReactivity);

        // Render spiral tunnel
        RenderSpiralTunnel(ctx, centerX, centerY, maxRadius, twistModulation, zoomModulation, depth, spiralTurns, baseColor);
    }

    private void RenderSpiralTunnel(RenderContext ctx, float centerX, float centerY, float maxRadius, float twist, float zoomSpeed, float depth, float spiralTurns, uint baseColor)
    {
        // Render tunnel rings from back to front
        for (int ring = 0; ring < depth; ring++)
        {
            float ringProgress = ring / (float)depth;
            float ringRadius = maxRadius * (0.1f + ringProgress * 0.9f);
            
            // Calculate ring position in 3D space
            float z = ringProgress * 10f - _time * zoomSpeed;
            float zMod = 1f / (1f + z * 0.1f); // Perspective scaling
            
            // Apply spiral twist
            float twistAngle = twist * ringProgress * spiralTurns + _time * 2f;
            float twistRadius = ringRadius * zMod;
            
            // Calculate spiral offset
            float spiralOffsetX = MathF.Sin(twistAngle) * twistRadius * 0.3f;
            float spiralOffsetY = MathF.Cos(twistAngle) * twistRadius * 0.3f;
            
            // Apply perspective transformation
            float x = centerX + spiralOffsetX * zMod;
            float y = centerY + spiralOffsetY * zMod;
            float radius = twistRadius * zMod;
            
            // Calculate color based on depth and audio
            uint ringColor = CalculateRingColor(baseColor, ringProgress, zMod);
            
            // Draw ring
            if (radius > 2f && radius < maxRadius * 2f)
            {
                ctx.Canvas!.DrawCircle(x, y, radius, ringColor, false);
                
                // Add inner detail rings
                if (ringProgress > 0.3f)
                {
                    float innerRadius = radius * 0.7f;
                    uint innerColor = ApplyAlpha(ringColor, 0.6f);
                    ctx.Canvas.DrawCircle(x, y, innerRadius, innerColor, false);
                }
                
                // Add spiral detail lines
                if (ringProgress > 0.5f)
                {
                    RenderSpiralDetail(ctx, x, y, radius, twistAngle, ringColor);
                }
            }
        }
        
        // Render tunnel center
        RenderTunnelCenter(ctx, centerX, centerY, maxRadius, twist, zoomSpeed, baseColor);
    }

    private void RenderSpiralDetail(RenderContext ctx, float x, float y, float radius, float twistAngle, uint color)
    {
        int detailLines = 8;
        for (int i = 0; i < detailLines; i++)
        {
            float angle = (i / (float)detailLines) * MathF.PI * 2f + twistAngle;
            float startX = x + MathF.Cos(angle) * radius * 0.3f;
            float startY = y + MathF.Sin(angle) * radius * 0.3f;
            float endX = x + MathF.Cos(angle) * radius * 0.8f;
            float endY = y + MathF.Sin(angle) * radius * 0.8f;
            
            uint lineColor = ApplyAlpha(color, 0.4f);
            ctx.Canvas!.DrawLine(startX, startY, endX, endY, lineColor, 1f);
        }
    }

    private void RenderTunnelCenter(RenderContext ctx, float centerX, float centerY, float maxRadius, float twist, float zoomSpeed, uint baseColor)
    {
        // Calculate center intensity based on audio and time
        float centerIntensity = 0.5f + 0.3f * MathF.Sin(_time * 3f);
        float centerRadius = maxRadius * 0.05f * centerIntensity;
        
        // Draw pulsing center
        uint centerColor = ApplyAlpha(baseColor, centerIntensity);
        ctx.Canvas!.FillCircle(centerX, centerY, centerRadius, centerColor);
        
        // Draw center glow
        float glowRadius = centerRadius * 3f;
        uint glowColor = ApplyAlpha(baseColor, 0.2f);
        ctx.Canvas.DrawCircle(centerX, centerY, glowRadius, glowColor, false);
        
        // Draw spiral arms from center
        RenderCenterSpiralArms(ctx, centerX, centerY, maxRadius, twist, zoomSpeed, baseColor);
    }

    private void RenderCenterSpiralArms(RenderContext ctx, float centerX, float centerY, float maxRadius, float twist, float zoomSpeed, uint baseColor)
    {
        int armCount = 4;
        for (int arm = 0; arm < armCount; arm++)
        {
            float armAngle = (arm / (float)armCount) * MathF.PI * 2f + _time * zoomSpeed;
            float armLength = maxRadius * 0.3f;
            
            float endX = centerX + MathF.Cos(armAngle) * armLength;
            float endY = centerY + MathF.Sin(armAngle) * armLength;
            
            // Draw arm line
            uint armColor = ApplyAlpha(baseColor, 0.6f);
            ctx.Canvas!.DrawLine(centerX, centerY, endX, endY, armColor, 2f);
            
            // Draw arm endpoint
            float endpointRadius = 3f;
            ctx.Canvas.FillCircle(endX, endY, endpointRadius, armColor);
            
            // Add spiral detail to arm
            RenderArmSpiralDetail(ctx, centerX, centerY, endX, endY, armAngle, baseColor);
        }
    }

    private void RenderArmSpiralDetail(RenderContext ctx, float startX, float startY, float endX, float endY, float angle, uint baseColor)
    {
        int detailPoints = 5;
        for (int i = 1; i < detailPoints; i++)
        {
            float progress = i / (float)detailPoints;
            float x = startX + (endX - startX) * progress;
            float y = startY + (endY - startY) * progress;
            
            float detailRadius = 2f * (1f - progress);
            uint detailColor = ApplyAlpha(baseColor, 0.4f * (1f - progress));
            
            ctx.Canvas!.FillCircle(x, y, detailRadius, detailColor);
        }
    }

    private float GetAudioEnergy(float[] waveform, float[] spectrum)
    {
        if (waveform.Length == 0 && spectrum.Length == 0) return 0.5f;
        
        float waveEnergy = 0f;
        if (waveform.Length > 0)
        {
            for (int i = 0; i < Math.Min(waveform.Length, 100); i++)
            {
                waveEnergy += Math.Abs(waveform[i]);
            }
            waveEnergy /= Math.Min(waveform.Length, 100);
        }
        
        float spectrumEnergy = 0f;
        if (spectrum.Length > 0)
        {
            for (int i = 0; i < Math.Min(spectrum.Length, 50); i++)
            {
                spectrumEnergy += spectrum[i];
            }
            spectrumEnergy /= Math.Min(spectrum.Length, 50);
        }
        
        return Math.Max(waveEnergy, spectrumEnergy);
    }

    private uint CalculateRingColor(uint baseColor, float depth, float perspective)
    {
        // Modify color based on depth and perspective
        byte r = (byte)(baseColor >> 16);
        byte g = (byte)(baseColor >> 8);
        byte b = (byte)baseColor;
        
        // Darken with depth
        float depthFactor = 0.3f + depth * 0.7f;
        r = (byte)(r * depthFactor);
        g = (byte)(g * depthFactor);
        b = (byte)(b * depthFactor);
        
        // Add perspective-based alpha
        byte alpha = (byte)(255 * perspective);
        
        return (uint)((alpha << 24) | (r << 16) | (g << 8) | b);
    }

    private uint ApplyAlpha(uint color, float alpha)
    {
        byte r = (byte)(color >> 16);
        byte g = (byte)(color >> 8);
        byte b = (byte)color;
        byte a = (byte)(alpha * 255);
        
        return (uint)((a << 24) | (r << 16) | (g << 8) | b);
    }

    private uint ParseColor(string colorString)
    {
        // Simple hex color parser
        if (colorString.StartsWith("#") && colorString.Length == 7)
        {
            string hex = colorString.Substring(1);
            if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint color))
            {
                return 0xFF000000 | color; // Add full alpha
            }
        }
        return 0xFFFF33FF; // Default magenta
    }
}