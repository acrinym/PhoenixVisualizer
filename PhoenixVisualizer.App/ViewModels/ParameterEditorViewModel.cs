using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;

namespace PhoenixVisualizer.App.ViewModels;

public sealed partial class ParameterEditorViewModel : ObservableObject
    {
        private object? _target;
        private string _targetTypeName = "—";

        public string TargetTypeName
        {
            get => _targetTypeName;
            private set => SetProperty(ref _targetTypeName, value);
        }

        public ObservableCollection<ParameterSection> Sections { get; } = new();

        public void SetTarget(object? obj)
        {
            _target = obj;
            TargetTypeName = obj?.GetType().Name ?? "—";
            RebuildFromTarget();
        }

        [RelayCommand]
        private void Refresh() => RebuildFromTarget();

        private void RebuildFromTarget()
        {
            Sections.Clear();
            if (_target == null) return;

            var t = _target.GetType();
            // Collect properties that are writable and look like parameters
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(p => p.CanRead && p.CanWrite)
                         .Select(p => new ReflectedParam(p, ReadParamMeta(p)))
                         .Where(rp => rp.Meta.IsParameterCandidate)
                         .ToList();

            // Build view-model items grouped by meta.Group (or default group)
            var groups = props.GroupBy(p => p.Meta.Group ?? "General")
                              .OrderBy(g => g.Min(x => x.Meta.Order));

            foreach (var group in groups)
            {
                var section = new ParameterSection(group.Key);
                foreach (var rp in group.OrderBy(x => x.Meta.Order))
                {
                    var item = CreateItemVM(_target, rp);
                    if (item != null) section.Items.Add(item);
                }
                Sections.Add(section);
            }
        }

        private static ParamVM? CreateItemVM(object target, ReflectedParam rp)
        {
            var p = rp.Property;
            var label = rp.Meta.DisplayName ?? p.Name;
            var desc  = rp.Meta.Description ?? string.Empty;

            var type = p.PropertyType;
            try
            {
                if (type == typeof(bool))
                {
                    var vm = new BoolParamVM(label, desc, rp) { Value = (bool)(p.GetValue(target) ?? false) };
                    vm.ValueChanged = v => p.SetValue(target, v);
                    return vm;
                }
                if (type.IsEnum)
                {
                    var values = Enum.GetValues(type).Cast<object>().ToArray();
                    var current = p.GetValue(target) ?? values.FirstOrDefault()!;
                    var vm = new EnumParamVM(label, desc, rp, values) { SelectedChoice = current };
                    vm.SelectionChanged = v => p.SetValue(target, v);
                    return vm;
                }
                if (type == typeof(string))
                {
                    var vm = new TextParamVM(label, desc, rp) { Value = (string?)p.GetValue(target) ?? string.Empty };
                    vm.ValueChanged = v => p.SetValue(target, v ?? string.Empty);
                    return vm;
                }
                if (type == typeof(Color))
                {
                    var c = (Color)(p.GetValue(target) ?? Colors.White);
                    var vm = new ColorParamVM(label, desc, rp) { Value = c };
                    vm.ColorChanged = v => p.SetValue(target, v);
                    return vm;
                }
                if (IsNumeric(type))
                {
                    // normalize to double in VM; convert back on set
                    var d = Convert.ToDouble(p.GetValue(target) ?? 0.0, CultureInfo.InvariantCulture);
                    var vm = new NumberParamVM(label, desc, rp,
                                               rp.Meta.Min ?? 0.0,
                                               rp.Meta.Max ?? 1.0,
                                               rp.Meta.Step ?? GuessStepFromRange(rp.Meta.Min, rp.Meta.Max),
                                               rp.Meta.Suffix)
                    { Value = d };
                    vm.NumberChanged = v => p.SetValue(target, ConvertBackNumber(v, type));
                    return vm;
                }
            }
            catch
            {
                // ignore a bad property rather than throwing
            }
            return null;
        }

        private static bool IsNumeric(Type t)
            => t == typeof(int) || t == typeof(float) || t == typeof(double)
            || t == typeof(long) || t == typeof(uint) || t == typeof(short) || t == typeof(byte);

        private static object ConvertBackNumber(double v, Type t)
        {
            if (t == typeof(int)) return (int)Math.Round(v);
            if (t == typeof(float)) return (float)v;
            if (t == typeof(long)) return (long)Math.Round(v);
            if (t == typeof(uint)) return (uint)Math.Max(0, Math.Round(v));
            if (t == typeof(short)) return (short)Math.Round(v);
            if (t == typeof(byte)) return (byte)Math.Clamp(Math.Round(v), 0, 255);
            return v; // double
        }

        private static double GuessStepFromRange(double? min, double? max)
        {
            var range = (max ?? 1.0) - (min ?? 0.0);
            if (range <= 0) return 0.01;
            if (range >= 1000) return 1;
            if (range >= 100) return 0.5;
            if (range >= 10) return 0.1;
            return 0.01;
        }

        // ——— metadata discovery ———
        private static ParamMeta ReadParamMeta(PropertyInfo p)
        {
            var meta = new ParamMeta();
            // Accept any attribute with "Parameter" in the name (e.g., VFXParameterAttribute)
            var attr = p.GetCustomAttributes(inherit: true)
                        .FirstOrDefault(a => a.GetType().Name.Contains("Parameter", StringComparison.OrdinalIgnoreCase));

            // If no attribute, still allow obvious types (bool/enum/number/color/string)
            meta.IsParameterCandidate = attr != null || IsObviousType(p.PropertyType);

            if (attr != null)
            {
                // reflect common metadata names if present
                meta.DisplayName = GetMaybe<string>(attr, "DisplayName") ?? GetMaybe<string>(attr, "Name");
                meta.Description = GetMaybe<string>(attr, "Description") ?? GetMaybe<string>(attr, "Help");
                meta.Group       = GetMaybe<string>(attr, "Group") ?? GetMaybe<string>(attr, "Category");
                meta.Order       = GetMaybe<int?>(attr, "Order") ?? 1000;
                meta.Min         = GetMaybe<double?>(attr, "Min") ?? GetMaybe<double?>(attr, "Minimum");
                meta.Max         = GetMaybe<double?>(attr, "Max") ?? GetMaybe<double?>(attr, "Maximum");
                meta.Step        = GetMaybe<double?>(attr, "Step") ?? GetMaybe<double?>(attr, "Increment");
                meta.Suffix      = GetMaybe<string>(attr, "Suffix");
            }

            // Defaults
            meta.DisplayName ??= p.Name;
            meta.Group ??= "General";

            return meta;
        }

        private static bool IsObviousType(Type t)
            => t == typeof(bool) || t.IsEnum || t == typeof(string) || t == typeof(Color) || IsNumeric(t);

        private static T? GetMaybe<T>(object attr, string name)
        {
            var pi = attr.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (pi == null) return default;
            var val = pi.GetValue(attr);
            if (val is null) return default;
            try { return (T?)Convert.ChangeType(val, typeof(T), CultureInfo.InvariantCulture); }
            catch { return (T?)val; }
        }

        private sealed record ReflectedParam(PropertyInfo Property, ParamMeta Meta);
        private sealed class ParamMeta
        {
            public bool IsParameterCandidate { get; set; }
            public string? DisplayName { get; set; }
            public string? Description { get; set; }
            public string? Group { get; set; }
            public int Order { get; set; } = 1000;
            public double? Min { get; set; }
            public double? Max { get; set; }
            public double? Step { get; set; }
            public string? Suffix { get; set; }
        }
    }

    // ——— View-model item hierarchy (one per editor row) ———
    public class ParameterSection : ObservableObject
    {
        public string Title { get; }
        public ObservableCollection<ParamVM> Items { get; } = new();
        public ParameterSection(string title) => Title = title;
    }

    public abstract class ParamVM : ObservableObject
    {
        protected ParamVM(string label, string description)
        {
            Label = label;
            Description = description;
        }
        public string Label { get; }
        public string Description { get; }
    }

    public sealed class BoolParamVM : ParamVM
    {
        public BoolParamVM(string label, string description, object tag) : base(label, description) { Tag = tag; }
        public object Tag { get; }
        private bool _value;
        public bool Value { get => _value; set { if (SetProperty(ref _value, value)) ValueChanged?.Invoke(value); } }
        public Action<bool>? ValueChanged { get; set; }
    }

    public sealed class EnumParamVM : ParamVM
    {
        public EnumParamVM(string label, string description, object tag, object[] choices) : base(label, description)
        { Tag = tag; Choices = choices; }
        public object Tag { get; }
        public object[] Choices { get; }
        private object? _selected;
        public object? SelectedChoice
        {
            get => _selected;
            set { if (SetProperty(ref _selected, value)) { if (value != null) SelectionChanged?.Invoke(value); } }
        }
        public Action<object>? SelectionChanged { get; set; }
    }

    public sealed class TextParamVM : ParamVM
    {
        public TextParamVM(string label, string description, object tag) : base(label, description) { Tag = tag; }
        public object Tag { get; }
        private string? _value;
        public string? Value { get => _value; set { if (SetProperty(ref _value, value)) ValueChanged?.Invoke(value); } }
        public Action<string?>? ValueChanged { get; set; }
    }

    public sealed class NumberParamVM : ParamVM
    {
        public NumberParamVM(string label, string description, object tag,
                             double min, double max, double step, string? suffix) : base(label, description)
        { Tag = tag; Min = min; Max = max; Step = step; Suffix = suffix ?? string.Empty; }
        public object Tag { get; }
        public double Min { get; }
        public double Max { get; }
        public double Step { get; }
        public string Suffix { get; }
        private double _value;
        public double Value { get => _value; set { if (SetProperty(ref _value, value)) { NumberChanged?.Invoke(value); OnPropertyChanged(nameof(ValueText)); } } }
        public string ValueText
        {
            get => _value.ToString("0.###", CultureInfo.InvariantCulture);
            set
            {
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    Value = Math.Clamp(v, Min, Max);
                else
                    OnPropertyChanged(); // keep text unchanged if parse fails
            }
        }
        public Action<double>? NumberChanged { get; set; }
    }

    public sealed class ColorParamVM : ParamVM
    {
        public ColorParamVM(string label, string description, object tag) : base(label, description) { Tag = tag; }
        public object Tag { get; }
        private Color _value = Colors.White;
        public Color Value
        {
            get => _value;
            set { if (SetProperty(ref _value, value)) { OnPropertyChanged(nameof(Hex)); ColorChanged?.Invoke(value); } }
        }
        public string Hex
        {
            get => Value.A == 255
                ? $"#{Value.R:X2}{Value.G:X2}{Value.B:X2}"
                : $"#{Value.A:X2}{Value.R:X2}{Value.G:X2}{Value.B:X2}";
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        var parsed = Color.Parse(value.Trim());
                        Value = parsed;
                    }
                    catch { /* ignore bad text */ }
                }
            }
        }
        public Action<Color>? ColorChanged { get; set; }
    }
