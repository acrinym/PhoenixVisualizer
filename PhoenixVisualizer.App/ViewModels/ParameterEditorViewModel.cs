using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Media;

// We read your Core attribute via reflection to avoid tight compile coupling.
using PVCoreVfx = PhoenixVisualizer.Core.VFX;

namespace PhoenixVisualizer.App.ViewModels
{
    public sealed class ParameterEditorViewModel : INotifyPropertyChanged
    {
        private object? _target;
        public object? Target
        {
            get => _target;
            private set
            {
                if (!ReferenceEquals(_target, value))
                {
                    _target = value;
                    OnPropertyChanged(nameof(Target));
                    OnPropertyChanged(nameof(TargetName));
                }
            }
        }

        public string TargetName => Target == null ? "No target" : Target.GetType().Name;

        public ObservableCollection<ParameterItemVM> Items { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void SetTarget(object? target)
        {
            Target = target;
            Items.Clear();
            if (target == null) return;

            var t = target.GetType();
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(p => p.CanRead && p.CanWrite);

            foreach (var p in props)
            {
                var attr = GetVfxParamAttribute(p);
                if (attr == null) continue; // only expose properties explicitly marked as VFX parameters

                var (displayName, description, min, max, step, order) = ReadCommonMetadata(attr, p);
                var pt = p.PropertyType;

                if (pt == typeof(bool))
                {
                    Items.Add(new BoolParameterItem(target, p, displayName, description, order));
                }
                else if (pt == typeof(string))
                {
                    Items.Add(new StringParameterItem(target, p, displayName, description, order));
                }
                else if (pt.IsEnum)
                {
                    Items.Add(new EnumParameterItem(target, p, displayName, description, order));
                }
                else if (pt == typeof(int) || pt == typeof(float) || pt == typeof(double))
                {
                    Items.Add(new NumberParameterItem(target, p, displayName, description, order, min, max, step));
                }
                else if (pt == typeof(Color))
                {
                    Items.Add(new ColorParameterItem(target, p, displayName, description, order));
                }
                // If you want to support vectors, points, etc., add branches here.
            }

            foreach (var sorted in Items.OrderBy(i => i.Order).ToArray()) { /* already added in order */ }
        }

        private static object? GetVfxParamAttribute(PropertyInfo p)
        {
            // Accept either VFXParameterAttribute or any attribute whose name matches to avoid versioning pain.
            var attrs = p.GetCustomAttributes(inherit: true);
            return attrs.FirstOrDefault(a =>
            {
                var n = a.GetType().Name;
                return n == "VFXParameterAttribute" || n == "VFXParameter";
            });
        }

        private static (string display, string desc, double? min, double? max, double? step, int order)
            ReadCommonMetadata(object attr, PropertyInfo p)
        {
            string display = p.Name;
            string desc = "";
            double? min = null, max = null, step = null;
            int order = 0;
            var at = attr.GetType();

            display = TryGet<string>(at, attr, "DisplayName") ?? TryGet<string>(at, attr, "Name") ?? p.Name;
            desc    = TryGet<string>(at, attr, "Description") ?? "";

            min  = TryGetDouble(at, attr, "Min");
            max  = TryGetDouble(at, attr, "Max");
            step = TryGetDouble(at, attr, "Step");
            order = TryGet<int>(at, attr, "Order") ?? 0;

            return (display, desc, min, max, step, order);
        }

        private static TOut? TryGet<TOut>(Type t, object instance, string propName)
        {
            var p = t.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p == null) return default;
            if (!typeof(TOut).IsAssignableFrom(p.PropertyType)) return default;
            return (TOut?)p.GetValue(instance);
        }

        private static double? TryGetDouble(Type t, object instance, string propName)
        {
            var p = t.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p == null) return null;
            var v = p.GetValue(instance);
            if (v == null) return null;
            if (v is double d) return d;
            if (v is float f) return f;
            if (v is int i) return i;
            if (double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return null;
        }
    }

    // -----------------------
    // Parameter item base + types
    // -----------------------
    public abstract class ParameterItemVM : INotifyPropertyChanged
    {
        protected readonly object Target;
        protected readonly PropertyInfo Prop;
        public string Name { get; }
        public string Description { get; }
        public int Order { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected ParameterItemVM(object target, PropertyInfo prop, string name, string description, int order)
        {
            Target = target;
            Prop = prop;
            Name = name;
            Description = description;
            Order = order;
        }
    }

    public sealed class BoolParameterItem : ParameterItemVM
    {
        public BoolParameterItem(object t, PropertyInfo p, string n, string d, int o) : base(t, p, n, d, o) { }
        public bool Value
        {
            get => (bool)(Prop.GetValue(Target) ?? false);
            set { Prop.SetValue(Target, value); Raise(nameof(Value)); }
        }
    }

    public sealed class StringParameterItem : ParameterItemVM
    {
        public StringParameterItem(object t, PropertyInfo p, string n, string d, int o) : base(t, p, n, d, o) { }
        public string Value
        {
            get => (string?)(Prop.GetValue(Target)) ?? string.Empty;
            set { Prop.SetValue(Target, value); Raise(nameof(Value)); }
        }
    }

    public sealed class EnumParameterItem : ParameterItemVM
    {
        public Array Choices { get; }
        public EnumParameterItem(object t, PropertyInfo p, string n, string d, int o) : base(t, p, n, d, o)
        {
            Choices = Enum.GetValues(p.PropertyType);
        }
        public object Value
        {
            get => Prop.GetValue(Target)!;
            set
            {
                if (value != null && value.GetType() == Prop.PropertyType)
                {
                    Prop.SetValue(Target, value);
                    Raise(nameof(Value));
                }
            }
        }
    }

    public sealed class NumberParameterItem : ParameterItemVM
    {
        public double? Min { get; }
        public double? Max { get; }
        public double? Step { get; }
        public bool IsInteger { get; }

        public NumberParameterItem(object t, PropertyInfo p, string n, string d, int o, double? min, double? max, double? step)
            : base(t, p, n, d, o)
        {
            Min = min; Max = max; Step = step;
            IsInteger = p.PropertyType == typeof(int);
        }

        public double Value
        {
            get
            {
                var v = Prop.GetValue(Target);
                if (v == null) return 0;
                if (v is int i) return i;
                if (v is float f) return f;
                if (v is double d) return d;
                double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed);
                return parsed;
            }
            set
            {
                var clamped = value;
                if (Min.HasValue && clamped < Min.Value) clamped = Min.Value;
                if (Max.HasValue && clamped > Max.Value) clamped = Max.Value;

                if (IsInteger)
                {
                    Prop.SetValue(Target, (int)Math.Round(clamped));
                }
                else if (Prop.PropertyType == typeof(float))
                {
                    Prop.SetValue(Target, (float)clamped);
                }
                else
                {
                    Prop.SetValue(Target, clamped);
                }
                Raise(nameof(Value));
            }
        }
    }

    public sealed class ColorParameterItem : ParameterItemVM
    {
        public ColorParameterItem(object t, PropertyInfo p, string n, string d, int o) : base(t, p, n, d, o) { }

        private Color Get() => (Color)(Prop.GetValue(Target) ?? Colors.White);
        private void Set(Color c) { Prop.SetValue(Target, c); Raise(nameof(A)); Raise(nameof(R)); Raise(nameof(G)); Raise(nameof(B)); Raise(nameof(Preview)); }

        public byte A { get => Get().A; set => Set(Color.FromArgb(value, R, G, B)); }
        public byte R { get => Get().R; set => Set(Color.FromArgb(A, value, G, B)); }
        public byte G { get => Get().G; set => Set(Color.FromArgb(A, R, value, B)); }
        public byte B { get => Get().B; set => Set(Color.FromArgb(A, R, G, value)); }

        public Color Preview => Get();
    }
}
