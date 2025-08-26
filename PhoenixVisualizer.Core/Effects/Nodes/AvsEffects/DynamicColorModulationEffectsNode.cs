using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class DynamicColorModulationEffectsNode : BaseEffectNode
    {
        #region Properties

        public bool Enabled { get; set; } = true;
        public string InitScript { get; set; } = "";
        public string LevelScript { get; set; } = "";
        public string FrameScript { get; set; } = "";
        public string BeatScript { get; set; } = "";
        public bool RecomputeTables { get; set; } = false;
        public float Intensity { get; set; } = 1.0f;
        public bool BeatResponseEnabled { get; set; } = true;
        public int MaxExecutionTime { get; set; } = 100;
        public ColorBlendMode BlendMode { get; set; } = ColorBlendMode.Replace;

        #endregion

        #region Private Fields

        private readonly byte[] _colorTable;
        private bool _tableValid = false;
        private EELScriptEngine _scriptEngine;
        private readonly object?[] _compiledScripts;
        private bool _scriptsNeedRecompilation = true;
        private bool _isInitialized = false;
        private bool _currentBeat = false;
        private int _frameCounter = 0;

        #endregion

        #region Constructor

        public DynamicColorModulationEffectsNode()
        {
            Name = "Dynamic Color Modulation";
            Description = "EEL-scripted color modulation with audio reactivity";
            Category = "Color Effects";

            _colorTable = new byte[768];
            _compiledScripts = new object[4];
            _scriptEngine = new EELScriptEngine();

            SetDefaultScripts();
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Modulated output image"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            if (!Enabled)
                return imageBuffer;

            if (!_isInitialized)
                InitializeScriptEngine();

            _currentBeat = audioFeatures?.IsBeat ?? false;
            _frameCounter++;

            if (_scriptsNeedRecompilation)
                RecompileScripts();

            if (audioFeatures != null)
        {
            SetAudioVariables(audioFeatures);
        }

            ExecuteInitScript();
            ExecuteFrameScript();
            if (_currentBeat && BeatResponseEnabled)
                ExecuteBeatScript();

            if (RecomputeTables || !_tableValid)
                UpdateLookupTable();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            ApplyColorModulation(imageBuffer, output);
            return output;
        }

        private void SetAudioVariables(AudioFeatures audioFeatures)
        {
            var spectrum = audioFeatures?.SpectrumData ?? Array.Empty<float>();
            var waveform = audioFeatures?.WaveformData ?? Array.Empty<float>();

            double bass = CalculateFrequencyBand(spectrum, 0, 170);
            double mid = CalculateFrequencyBand(spectrum, 171, 341);
            double treble = CalculateFrequencyBand(spectrum, 342, 511);
            double wave = CalculateWaveformAmplitude(waveform);

            _scriptEngine.SetVariable("bass", bass);
            _scriptEngine.SetVariable("mid", mid);
            _scriptEngine.SetVariable("treble", treble);
            _scriptEngine.SetVariable("wave", wave);
        }

        #endregion

        #region Initialization Methods

        private void SetDefaultScripts()
        {
            LevelScript = "red=red; green=green; blue=blue;";
            FrameScript = "";
            BeatScript = "";
            InitScript = "";
        }

        private void InitializeScriptEngine()
        {
            _scriptEngine ??= new EELScriptEngine();

            _scriptEngine.RegisterVariable("red", 0.0);
            _scriptEngine.RegisterVariable("green", 0.0);
            _scriptEngine.RegisterVariable("blue", 0.0);
            _scriptEngine.RegisterVariable("beat", 0.0);
            _scriptEngine.RegisterVariable("frame", 0.0);
            _scriptEngine.RegisterVariable("time", 0.0);
            _scriptEngine.RegisterVariable("bass", 0.0);
            _scriptEngine.RegisterVariable("mid", 0.0);
            _scriptEngine.RegisterVariable("treble", 0.0);
            _scriptEngine.RegisterVariable("wave", 0.0);

            _isInitialized = true;
        }

        #endregion

        #region Script Execution

        private void RecompileScripts()
        {
            try
            {
                _compiledScripts[0] = _scriptEngine.CompileScript(LevelScript);
                _compiledScripts[1] = _scriptEngine.CompileScript(FrameScript);
                _compiledScripts[2] = _scriptEngine.CompileScript(BeatScript);
                _compiledScripts[3] = _scriptEngine.CompileScript(InitScript);
                _scriptsNeedRecompilation = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EEL script compilation error: {ex.Message}");
            }
        }

        private void ExecuteInitScript()
        {
            if (_compiledScripts[3] != null && !_isInitialized)
            {
                try
                {
                    _scriptEngine.ExecuteScript(_compiledScripts[3]);
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Init script execution error: {ex.Message}");
                }
            }
        }

        private void ExecuteFrameScript()
        {
            if (_compiledScripts[1] != null)
            {
                try
                {
                    _scriptEngine.SetVariable("frame", _frameCounter);
                    _scriptEngine.SetVariable("time", _frameCounter / 60.0);
                    _scriptEngine.ExecuteScript(_compiledScripts[1]);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Frame script execution error: {ex.Message}");
                }
            }
        }

        private void ExecuteBeatScript()
        {
            if (_compiledScripts[2] != null)
            {
                try
                {
                    _scriptEngine.SetVariable("beat", 1.0);
                    _scriptEngine.ExecuteScript(_compiledScripts[2]);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Beat script execution error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Lookup Table

        private void UpdateLookupTable()
        {
            for (int i = 0; i < 256; i++)
            {
                double inputValue = i / 255.0;
                _scriptEngine.SetVariable("red", inputValue);
                _scriptEngine.SetVariable("green", inputValue);
                _scriptEngine.SetVariable("blue", inputValue);

                if (_compiledScripts[0] != null)
                {
                    try
                    {
                        _scriptEngine.ExecuteScript(_compiledScripts[0]);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Level script execution error: {ex.Message}");
                        break;
                    }
                }

                double outR = Math.Max(0.0, Math.Min(1.0, _scriptEngine.GetVariable("red")));
                double outG = Math.Max(0.0, Math.Min(1.0, _scriptEngine.GetVariable("green")));
                double outB = Math.Max(0.0, Math.Min(1.0, _scriptEngine.GetVariable("blue")));

                int index = i * 3;
                _colorTable[index] = (byte)(outB * 255.0 + 0.5);
                _colorTable[index + 1] = (byte)(outG * 255.0 + 0.5);
                _colorTable[index + 2] = (byte)(outR * 255.0 + 0.5);
            }

            _tableValid = true;
        }

        #endregion

        #region Color Processing

        private void ApplyColorModulation(ImageBuffer source, ImageBuffer dest)
        {
            int width = source.Width;
            int height = source.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color original = Color.FromArgb(source.GetPixel(x, y));
                    Color modulated = ModulatePixelColor(original);
                    Color blended = BlendColors(original, modulated);
                    dest.SetPixel(x, y, (blended.B << 16) | (blended.G << 8) | blended.R);
                }
            }
        }

        private Color ModulatePixelColor(Color pixel)
        {
            int blueIndex = pixel.B * 3;
            int greenIndex = pixel.G * 3 + 1;
            int redIndex = pixel.R * 3 + 2;

            int newR = _colorTable[redIndex];
            int newG = _colorTable[greenIndex];
            int newB = _colorTable[blueIndex];

            if (Intensity != 1.0f)
            {
                newR = (int)(newR * Intensity);
                newG = (int)(newG * Intensity);
                newB = (int)(newB * Intensity);
                newR = Math.Max(0, Math.Min(255, newR));
                newG = Math.Max(0, Math.Min(255, newG));
                newB = Math.Max(0, Math.Min(255, newB));
            }

            return Color.FromArgb(pixel.A, newR, newG, newB);
        }

        private Color BlendColors(Color original, Color modulated)
        {
            return BlendMode switch
            {
                ColorBlendMode.Additive => Color.FromArgb(
                    ClampByte(original.R + modulated.R),
                    ClampByte(original.G + modulated.G),
                    ClampByte(original.B + modulated.B)),
                ColorBlendMode.Multiply => Color.FromArgb(
                    (original.R * modulated.R) / 255,
                    (original.G * modulated.G) / 255,
                    (original.B * modulated.B) / 255),
                _ => modulated
            };
        }

        private int ClampByte(int value) => Math.Max(0, Math.Min(255, value));

        #endregion

        #region Audio Helpers

        private static double CalculateFrequencyBand(float[] spectrum, int start, int end)
        {
            if (spectrum == null || spectrum.Length == 0)
                return 0.0;

            start = Math.Max(0, Math.Min(start, spectrum.Length - 1));
            end = Math.Max(start, Math.Min(end, spectrum.Length - 1));
            double sum = 0.0;
            for (int i = start; i <= end; i++)
                sum += spectrum[i];
            double avg = sum / (end - start + 1);
            return Math.Max(0.0, Math.Min(1.0, avg));
        }

        private static double CalculateWaveformAmplitude(float[] waveform)
        {
            if (waveform == null || waveform.Length == 0)
                return 0.0;

            double sum = 0.0;
            foreach (var v in waveform)
                sum += Math.Abs(v);
            double avg = sum / waveform.Length;
            return Math.Max(0.0, Math.Min(1.0, avg));
        }

        #endregion

        #region Configuration Validation

        public override bool ValidateConfiguration()
        {
            if (MaxExecutionTime < 1 || MaxExecutionTime > 1000) return false;
            if (Intensity < 0.0f || Intensity > 10.0f) return false;
            if (!Enum.IsDefined(typeof(ColorBlendMode), BlendMode)) return false;
            if (InitScript?.Length > 10000) return false;
            if (LevelScript?.Length > 10000) return false;
            if (FrameScript?.Length > 10000) return false;
            if (BeatScript?.Length > 10000) return false;
            return true;
        }

        #endregion

        #region Preset Methods

        public void LoadBrightnessPreset()
        {
            LevelScript = "red=4*red; green=2*green; blue=blue;";
            FrameScript = "";
            BeatScript = "";
            InitScript = "";
            RecomputeTables = false;
            _scriptsNeedRecompilation = true;
        }

        public void LoadSolarizationPreset()
        {
            LevelScript = "red=(min(1,red*2)-red)*2;\ngreen=red; blue=red;";
            FrameScript = "";
            BeatScript = "";
            InitScript = "";
            RecomputeTables = false;
            _scriptsNeedRecompilation = true;
        }

        #endregion

        #region Utility Methods

        public byte[] GetLookupTable()
        {
            if (!_tableValid)
                UpdateLookupTable();
            return _colorTable?.Clone() as byte[] ?? Array.Empty<byte>();
        }

        public bool IsLookupTableValid() => _tableValid;

        public void ForceRecompilation()
        {
            _scriptsNeedRecompilation = true;
            _tableValid = false;
        }

        public override void Reset()
        {
            _isInitialized = false;
            _frameCounter = 0;
            _currentBeat = false;
            _tableValid = false;
            _scriptsNeedRecompilation = true;
        }

        #endregion

        #region Nested Types

        public enum ColorBlendMode
        {
            Replace,
            Additive,
            Multiply
        }

        private class EELScriptEngine
        {
            private readonly Dictionary<string, double> _variables = new();

            public void RegisterVariable(string name, double value) => _variables[name] = value;
            public void SetVariable(string name, double value) => _variables[name] = value;
            public double GetVariable(string name) => _variables.TryGetValue(name, out var v) ? v : 0.0;

            public object? CompileScript(string script)
            {
                if (string.IsNullOrEmpty(script))
                    return null;
                return script;
            }

            public void ExecuteScript(object? compiledScript)
            {
                if (compiledScript == null) return;
                string script = compiledScript.ToString() ?? string.Empty;
                ExecuteBasicScript(script);
            }

            private void ExecuteBasicScript(string script)
            {
                string[] lines = script.Split('\n');
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Contains('='))
                    {
                        string[] parts = trimmed.Split('=');
                        if (parts.Length == 2)
                        {
                            string varName = parts[0].Trim();
                            string expr = parts[1].Trim();
                            double result = EvaluateExpression(expr);
                            _variables[varName] = result;
                        }
                    }
                }
            }

            private double EvaluateExpression(string expression)
            {
                foreach (var kv in _variables)
                    expression = expression.Replace(kv.Key, kv.Value.ToString());

                try
                {
                    return Convert.ToDouble(new System.Data.DataTable().Compute(expression, ""));
                }
                catch
                {
                    return 0.0;
                }
            }
        }

        #endregion
    }
}
