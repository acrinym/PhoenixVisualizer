using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class DotPlaneEffectsNode : BaseEffectNode
{
    #region Constants

    private const int NUM_WIDTH = 64;

    #endregion

    #region Properties

    public bool Enabled { get; set; } = true;
    public float RotationVelocity { get; set; } = 16.0f;
    public float Angle { get; set; } = -20.0f;
    public float BaseRadius { get; set; } = 1.0f;
    public float Intensity { get; set; } = 1.0f;
    public bool BeatResponse { get; set; } = true;
    public float AudioSensitivity { get; set; } = 1.0f;
    public float DampingFactor { get; set; } = 0.15f;
    public float VelocityUpdateRate { get; set; } = 90.0f;
    public float HeightOffset { get; set; } = -20.0f;
    public float Depth { get; set; } = 400.0f;
    public float PlaneWidth { get; set; } = 350.0f;

    #endregion

    #region Private Fields

    private float _currentRotation = 0.0f;
    private Matrix4x4 _transformationMatrix;
    private readonly float[,] _heightTable = new float[NUM_WIDTH, NUM_WIDTH];
    private readonly float[,] _velocityTable = new float[NUM_WIDTH, NUM_WIDTH];
    private readonly int[,] _colorTable = new int[NUM_WIDTH, NUM_WIDTH];
    private readonly int[] _colorInterpolationTable = new int[64];
    private int _currentWidth, _currentHeight;
    private bool _isInitialized = false;
    private int _frameCounter = 0;

    #endregion

    #region Constructor

    public DotPlaneEffectsNode()
    {
        Name = "Dot Plane Effects";
        Description = "3D plane of dots reacting to audio";
        Category = "AVS Effects";

        SetDefaultColors();
        InitializeTables();
    }

    #endregion

    #region Port Initialization

    protected override void InitializePorts()
    {
        _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for sizing"));
        _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Dot plane output"));
    }

    #endregion

    #region Processing

    protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
    {
        if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
            return GetDefaultOutput();

        var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
        ProcessFrame(output, audioFeatures);
        return output;
    }

    private void ProcessFrame(ImageBuffer imageBuffer, AudioFeatures audioFeatures)
    {
        if (!Enabled || imageBuffer == null) return;

        InitializeEffect(imageBuffer.Width, imageBuffer.Height);
        _frameCounter++;
        UpdateTransformationMatrix();
        UpdateDotPlanePhysics(audioFeatures);
        RenderDotPlane(imageBuffer);
        UpdateRotation();
    }

    #endregion

    #region Initialization Methods

    private void SetDefaultColors()
    {
        DefaultColors = new Color[]
        {
            Color.FromArgb(28, 107, 24),
            Color.FromArgb(255, 10, 35),
            Color.FromArgb(42, 29, 116),
            Color.FromArgb(144, 54, 217),
            Color.FromArgb(107, 136, 255)
        };
    }

    private void InitializeColorTable()
    {
        for (int t = 0; t < 4; t++)
        {
            Color currentColor = DefaultColors[t];
            Color nextColor = DefaultColors[t + 1];

            int deltaR = (nextColor.R - currentColor.R) / 16;
            int deltaG = (nextColor.G - currentColor.G) / 16;
            int deltaB = (nextColor.B - currentColor.B) / 16;

            for (int x = 0; x < 16; x++)
            {
                int r = Math.Clamp(currentColor.R + deltaR * x, 0, 255);
                int g = Math.Clamp(currentColor.G + deltaG * x, 0, 255);
                int b = Math.Clamp(currentColor.B + deltaB * x, 0, 255);
                _colorInterpolationTable[t * 16 + x] = Color.FromArgb(255, r, g, b).ToArgb();
            }
        }
    }

    private void InitializeTables()
    {
        for (int y = 0; y < NUM_WIDTH; y++)
        {
            for (int x = 0; x < NUM_WIDTH; x++)
            {
                _heightTable[y, x] = 0.0f;
                _velocityTable[y, x] = 0.0f;
                _colorTable[y, x] = 0;
            }
        }
        InitializeColorTable();
    }

    private void InitializeEffect(int width, int height)
    {
        if (_currentWidth == width && _currentHeight == height && _isInitialized)
            return;

        _currentWidth = width;
        _currentHeight = height;
        _isInitialized = true;
    }

    #endregion

    #region Processing Helpers

    private void UpdateTransformationMatrix()
    {
        Matrix4x4 rotationY = Matrix4x4.CreateRotationY(_currentRotation * (float)Math.PI / 180.0f);
        Matrix4x4 rotationX = Matrix4x4.CreateRotationX(Angle * (float)Math.PI / 180.0f);
        Matrix4x4 translation = Matrix4x4.CreateTranslation(0.0f, HeightOffset, Depth);
        _transformationMatrix = translation * rotationX * rotationY;
    }

    private void UpdateDotPlanePhysics(AudioFeatures audioFeatures)
    {
        float[,] backupHeightTable = new float[NUM_WIDTH, NUM_WIDTH];
        Array.Copy(_heightTable, backupHeightTable, _heightTable.Length);

        for (int fo = 0; fo < NUM_WIDTH; fo++)
        {
            int sourceIndex = NUM_WIDTH - (fo + 2);
            int targetIndex = NUM_WIDTH - (fo + 1);

            if (fo == NUM_WIDTH - 1)
            {
                GenerateNewDotsFromAudio(audioFeatures);
            }
            else
            {
                UpdateExistingDots(sourceIndex, targetIndex);
            }
        }
    }

    private void GenerateNewDotsFromAudio(AudioFeatures audioFeatures)
    {
        for (int p = 0; p < NUM_WIDTH; p++)
        {
            float audioValue = GetAudioValue(p, audioFeatures);
            _heightTable[0, p] = audioValue;
            int colorIndex = Math.Min(63, (int)(audioValue / 4));
            _colorTable[0, p] = _colorInterpolationTable[colorIndex];
            float velocity = (audioValue - _heightTable[1, p]) / VelocityUpdateRate;
            _velocityTable[0, p] = velocity;
        }
    }

    private void UpdateExistingDots(int sourceIndex, int targetIndex)
    {
        for (int p = 0; p < NUM_WIDTH; p++)
        {
            float newHeight = _heightTable[sourceIndex, p] + _velocityTable[sourceIndex, p];
            if (newHeight < 0.0f) newHeight = 0.0f;
            _heightTable[targetIndex, p] = newHeight;
            float damping = DampingFactor * (newHeight / 255.0f);
            _velocityTable[targetIndex, p] = _velocityTable[sourceIndex, p] - damping;
            _colorTable[targetIndex, p] = _colorTable[sourceIndex, p];
        }
    }

    private float GetAudioValue(int position, AudioFeatures audioFeatures)
    {
        float baseValue = 0.0f;
        if (audioFeatures?.SpectrumData != null && audioFeatures.SpectrumData.Length > 0)
        {
            int bandIndex = position % audioFeatures.SpectrumData.Length;
            baseValue = audioFeatures.SpectrumData[bandIndex];
        }
        float variation = (float)Math.Sin(position * 0.1f + _frameCounter * 0.05f) * 30.0f;
        baseValue += variation;
        baseValue *= AudioSensitivity;
        return Math.Clamp(baseValue, 0, 255);
    }

    private void RenderDotPlane(ImageBuffer imageBuffer)
    {
        int width = imageBuffer.Width;
        int height = imageBuffer.Height;
        float perspectiveAdjust = Math.Min(
            width * 440.0f / 640.0f,
            height * 440.0f / 480.0f
        );

        for (int fo = 0; fo < NUM_WIDTH; fo++)
        {
            int renderIndex = (_currentRotation < 90.0f || _currentRotation > 270.0f)
                ? NUM_WIDTH - fo - 1 : fo;

            float dotWidth = PlaneWidth / NUM_WIDTH;
            float startWidth = -(NUM_WIDTH * 0.5f) * dotWidth;

            int[] colorRow = GetColorRow(renderIndex);
            float[] heightRow = GetHeightRow(renderIndex);

            int direction = (_currentRotation < 180.0f) ? -1 : 1;
            float widthStep = (_currentRotation < 180.0f) ? -dotWidth : dotWidth;
            float currentWidth = (_currentRotation < 180.0f) ? -startWidth + dotWidth : startWidth;

            for (int p = 0; p < NUM_WIDTH; p++)
            {
                int dataIndex = (_currentRotation < 180.0f) ? NUM_WIDTH - 1 - p : p;

                Vector3 position = new(
                    currentWidth,
                    64.0f - heightRow[dataIndex],
                    (renderIndex - NUM_WIDTH * 0.5f) * dotWidth
                );

                Vector3 transformedPosition = TransformVector(position, _transformationMatrix);

                if (transformedPosition.Z > 0.0000001f)
                {
                    float perspective = perspectiveAdjust / transformedPosition.Z;
                    int screenX = (int)(transformedPosition.X * perspective) + width / 2;
                    int screenY = (int)(transformedPosition.Y * perspective) + height / 2;

                    if (screenX >= 0 && screenX < width && screenY >= 0 && screenY < height)
                    {
                        int colorArgb = colorRow[dataIndex];
                        Color color = Color.FromArgb(colorArgb);
                        color = ApplyIntensity(color, Intensity);
                        imageBuffer.SetPixel(screenX, screenY, color.ToArgb());
                    }
                }

                currentWidth += widthStep;
            }
        }
    }

    private int[] GetColorRow(int rowIndex)
    {
        int[] row = new int[NUM_WIDTH];
        for (int i = 0; i < NUM_WIDTH; i++)
            row[i] = _colorTable[rowIndex, i];
        return row;
    }

    private float[] GetHeightRow(int rowIndex)
    {
        float[] row = new float[NUM_WIDTH];
        for (int i = 0; i < NUM_WIDTH; i++)
            row[i] = _heightTable[rowIndex, i];
        return row;
    }

    private Color ApplyIntensity(Color color, float intensity)
    {
        if (intensity <= 1.0f) return color;
        int r = Math.Min(255, (int)(color.R * intensity));
        int g = Math.Min(255, (int)(color.G * intensity));
        int b = Math.Min(255, (int)(color.B * intensity));
        return Color.FromArgb(color.A, r, g, b);
    }

    private void UpdateRotation()
    {
        _currentRotation += RotationVelocity / 5.0f;
        while (_currentRotation >= 360.0f) _currentRotation -= 360.0f;
        while (_currentRotation < 0.0f) _currentRotation += 360.0f;
    }

    private Vector3 TransformVector(Vector3 vector, Matrix4x4 matrix)
    {
        return new Vector3(
            vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31 + matrix.M41,
            vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32 + matrix.M42,
            vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33 + matrix.M43
        );
    }

    #endregion

    #region Configuration Validation

    public override bool ValidateConfiguration()
    {
        if (RotationVelocity < -100.0f || RotationVelocity > 100.0f) return false;
        if (Angle < -90.0f || Angle > 90.0f) return false;
        if (BaseRadius < 0.1f || BaseRadius > 10.0f) return false;
        if (Intensity < 0.1f || Intensity > 10.0f) return false;
        if (AudioSensitivity < 0.1f || AudioSensitivity > 5.0f) return false;
        if (DampingFactor < 0.01f || DampingFactor > 1.0f) return false;
        if (VelocityUpdateRate < 10.0f || VelocityUpdateRate > 200.0f) return false;
        if (HeightOffset < -100.0f || HeightOffset > 100.0f) return false;
        if (Depth < 100.0f || Depth > 1000.0f) return false;
        if (PlaneWidth < 100.0f || PlaneWidth > 1000.0f) return false;
        return true;
    }

    #endregion

    #region Preset Methods

    public void LoadSlowRotatingPreset()
    {
        RotationVelocity = 8.0f;
        Angle = -15.0f;
        BaseRadius = 1.0f;
        Intensity = 1.0f;
        AudioSensitivity = 1.2f;
        DampingFactor = 0.12f;
        VelocityUpdateRate = 90.0f;
        HeightOffset = -20.0f;
        Depth = 400.0f;
        PlaneWidth = 350.0f;
    }

    public void LoadFastSpinningPreset()
    {
        RotationVelocity = 32.0f;
        Angle = -25.0f;
        BaseRadius = 1.5f;
        Intensity = 1.5f;
        AudioSensitivity = 1.8f;
        DampingFactor = 0.20f;
        VelocityUpdateRate = 70.0f;
        HeightOffset = -30.0f;
        Depth = 350.0f;
        PlaneWidth = 300.0f;
    }

    public void LoadGentleFlowingPreset()
    {
        RotationVelocity = 4.0f;
        Angle = -10.0f;
        BaseRadius = 0.8f;
        Intensity = 0.8f;
        AudioSensitivity = 0.7f;
        DampingFactor = 0.08f;
        VelocityUpdateRate = 120.0f;
        HeightOffset = -15.0f;
        Depth = 450.0f;
        PlaneWidth = 400.0f;
    }

    public void LoadBeatResponsivePreset()
    {
        RotationVelocity = 16.0f;
        Angle = -20.0f;
        BaseRadius = 1.2f;
        Intensity = 2.0f;
        AudioSensitivity = 2.5f;
        DampingFactor = 0.18f;
        VelocityUpdateRate = 60.0f;
        HeightOffset = -25.0f;
        Depth = 380.0f;
        PlaneWidth = 320.0f;
        BeatResponse = true;
    }

    #endregion

    #region Utility Methods

    public float GetCurrentRotation() => _currentRotation;

    public int GetActiveDotCount()
    {
        int count = 0;
        for (int y = 0; y < NUM_WIDTH; y++)
            for (int x = 0; x < NUM_WIDTH; x++)
                if (_heightTable[y, x] > 0.0f) count++;
        return count;
    }

    public float GetAverageDotHeight()
    {
        float total = 0.0f;
        int count = 0;
        for (int y = 0; y < NUM_WIDTH; y++)
            for (int x = 0; x < NUM_WIDTH; x++)
            {
                total += _heightTable[y, x];
                count++;
            }
        return count > 0 ? total / count : 0.0f;
    }

    public override void Reset()
    {
        _currentRotation = 0.0f;
        _frameCounter = 0;
        _isInitialized = false;
        InitializeTables();
    }

    public string GetExecutionStats()
    {
        return $"Frame: {_frameCounter}, Rotation: {_currentRotation:F1}°, Active Dots: {GetActiveDotCount()}, Avg Height: {GetAverageDotHeight():F1}, Matrix Valid: {_transformationMatrix != Matrix4x4.Identity}";
    }

    #endregion

    #region Default Colors Property

    public Color[] DefaultColors { get; set; } = new Color[5];

    #endregion

    public override object GetDefaultOutput()
    {
        return new ImageBuffer(800, 600);
    }
}
