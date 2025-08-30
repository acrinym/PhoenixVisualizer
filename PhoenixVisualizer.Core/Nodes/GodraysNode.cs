using System;
using System.Collections.Generic;
using System.Numerics;

namespace PhoenixVisualizer.Core.Nodes;

public class GodraysNode : IEffectNode
{
    public string Name => "Godrays";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["density"] = new EffectParam{ Label="Density", Type="slider", Min=0.1f, Max=5f, FloatValue=1f },
        ["decay"] = new EffectParam{ Label="Decay", Type="slider", Min=0.8f, Max=1f, FloatValue=0.92f },
        ["weight"] = new EffectParam{ Label="Weight", Type="slider", Min=0.1f, Max=2f, FloatValue=0.5f },
        ["exposure"] = new EffectParam{ Label="Exposure", Type="slider", Min=0.1f, Max=3f, FloatValue=1f },
        ["lightX"] = new EffectParam{ Label="Light X", Type="slider", Min=0, Max=1f, FloatValue=0.5f },
        ["lightY"] = new EffectParam{ Label="Light Y", Type="slider", Min=0, Max=1f, FloatValue=0.5f },
        ["numSamples"] = new EffectParam{ Label="Samples", Type="slider", Min=16, Max=128, FloatValue=64 },
        ["radialBlur"] = new EffectParam{ Label="Radial Blur", Type="slider", Min=0, Max=1f, FloatValue=0.5f },
        ["color"] = new EffectParam{ Label="Color", Type="color", ColorValue="#FFD700" }
    };

    private float _time = 0f;

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        if (ctx.Canvas == null) return;

        _time += 0.016f;

        // Get parameters
        float density = Params["density"].FloatValue;
        float decay = Params["decay"].FloatValue;
        float weight = Params["weight"].FloatValue;
        float exposure = Params["exposure"].FloatValue;
        float lightX = Params["lightX"].FloatValue;
        float lightY = Params["lightY"].FloatValue;
        int numSamples = (int)Params["numSamples"].FloatValue;
        float radialBlur = Params["radialBlur"].FloatValue;

        // Calculate audio energy for dynamic intensity
        float audioEnergy = GetAudioEnergy(waveform, spectrum);
        float dynamicDensity = density * (1f + audioEnergy * 0.5f);

        // Light position (with some movement)
        Vector2 lightPos = new Vector2(
            lightX + MathF.Sin(_time * 0.5f) * 0.1f,
            lightY + MathF.Cos(_time * 0.3f) * 0.1f
        );

        // Render godrays effect
        RenderGodrays(ctx, lightPos, dynamicDensity, decay, weight, exposure, numSamples, radialBlur);
    }

    private void RenderGodrays(RenderContext ctx, Vector2 lightPos, float density, float decay, float weight, float exposure, int numSamples, float radialBlur)
    {
        // Convert light position to screen coordinates
        int lightScreenX = (int)(lightPos.X * ctx.Width);
        int lightScreenY = (int)(lightPos.Y * ctx.Height);

        // Create radial blur effect
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                // Calculate direction from light to current pixel
                Vector2 pixelPos = new Vector2(x, y);
                Vector2 lightToPixel = pixelPos - new Vector2(lightScreenX, lightScreenY);
                float distance = lightToPixel.Length();

                if (distance < 1f) continue; // Skip pixels too close to light

                Vector2 direction = Vector2.Normalize(lightToPixel);

                // Calculate illumination based on angle and distance
                float illumination = CalculateIllumination(pixelPos, new Vector2(lightScreenX, lightScreenY), density, decay, numSamples);

                // Apply radial blur
                if (radialBlur > 0)
                {
                    illumination = ApplyRadialBlur(ctx, pixelPos, direction, distance, illumination, radialBlur, numSamples);
                }

                // Apply exposure and weight
                illumination *= weight * exposure;

                // Clamp and convert to color
                illumination = Math.Clamp(illumination, 0f, 1f);

                // Create godray color (warm light)
                uint r = (byte)(illumination * 255 * 0.9f); // Red
                uint g = (byte)(illumination * 255 * 0.8f); // Green
                uint b = (byte)(illumination * 255 * 0.6f); // Blue

                uint color = (uint)((255 << 24) | (r << 16) | (g << 8) | b);

                // Draw the godray pixel
                ctx.Canvas!.FillCircle(x, y, 1f, color);
            }
        }
    }

    private float CalculateIllumination(Vector2 pixelPos, Vector2 lightPos, float density, float decay, int numSamples)
    {
        Vector2 delta = pixelPos - lightPos;
        float distance = delta.Length();

        if (distance < 1f) return 1f;

        // Sample along the ray from light to pixel
        float illumination = 0f;
        Vector2 step = delta / numSamples;

        for (int i = 1; i <= numSamples; i++)
        {
            Vector2 samplePos = lightPos + step * i;
            float sampleDistance = samplePos.Length();

            // Calculate density falloff
            float densityFalloff = 1f / (1f + sampleDistance * density);

            // Apply decay
            float decayFactor = (float)Math.Pow(decay, i);

            illumination += densityFalloff * decayFactor;
        }

        return illumination / numSamples;
    }

    private float ApplyRadialBlur(RenderContext ctx, Vector2 pixelPos, Vector2 direction, float distance, float illumination, float blurAmount, int numSamples)
    {
        float blurredIllumination = illumination;
        float blurRadius = distance * blurAmount;

        for (int i = 1; i <= numSamples / 4; i++)
        {
            // Sample neighboring pixels along the radial direction
            Vector2 offset = direction * (blurRadius * i / (numSamples / 4f));

            // This would sample from the original image buffer in a full implementation
            // For now, we apply a simple blur based on distance
            float sampleIllumination = illumination * (1f - (float)i / (numSamples / 4f) * 0.5f);
            blurredIllumination += sampleIllumination;
        }

        return blurredIllumination / (numSamples / 4f + 1f);
    }

    private float GetAudioEnergy(float[] waveform, float[] spectrum)
    {
        if (waveform.Length == 0 && spectrum.Length == 0) return 0.5f;

        float energy = 0f;

        // Use spectrum energy for godrays intensity
        if (spectrum.Length > 0)
        {
            for (int i = 0; i < Math.Min(spectrum.Length, 100); i++)
            {
                energy += spectrum[i] * spectrum[i];
            }
            energy = (float)Math.Sqrt(energy / Math.Min(spectrum.Length, 100));
        }

        // Add waveform contribution
        if (waveform.Length > 0)
        {
            float waveEnergy = 0f;
            for (int i = 0; i < Math.Min(waveform.Length, 100); i++)
            {
                waveEnergy += Math.Abs(waveform[i]);
            }
            waveEnergy /= Math.Min(waveform.Length, 100);
            energy += waveEnergy * 0.3f;
        }

        return Math.Clamp(energy, 0f, 1f);
    }
}