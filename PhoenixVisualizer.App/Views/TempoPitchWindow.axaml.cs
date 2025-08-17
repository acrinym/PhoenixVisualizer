using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PhoenixVisualizer.Audio;
using System;

namespace PhoenixVisualizer.Views
{
    public partial class TempoPitchWindow : Window
    {
        private readonly AudioService? _audio;

        public TempoPitchWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public TempoPitchWindow(AudioService audio)
        {
            AvaloniaXamlLoader.Load(this);
            _audio = audio;

            // Find controls after XAML is loaded
            var tempoSlider = this.FindControl<Slider>("TempoSlider");
            var pitchSlider = this.FindControl<Slider>("PitchSlider");
            var tempoLabel = this.FindControl<TextBlock>("TempoLabel");
            var pitchLabel = this.FindControl<TextBlock>("PitchLabel");
            var btn075 = this.FindControl<Button>("Btn075");
            var btn050 = this.FindControl<Button>("Btn050");
            var btn025 = this.FindControl<Button>("Btn025");
            var btn005 = this.FindControl<Button>("Btn005");
            var btnReset = this.FindControl<Button>("BtnReset");
            var btnClose = this.FindControl<Button>("BtnClose");

            // Initialize labels
            if (tempoLabel != null) tempoLabel.Text = "1.00×";
            if (pitchLabel != null) pitchLabel.Text = "0 st";

            if (tempoSlider != null)
            {
                tempoSlider.PropertyChanged += (_, e) =>
                {
                    if (e.Property.Name == "Value" && _audio != null)
                    {
                        var m = (double)tempoSlider.Value;
                        _audio.SetTempoMultiplier(m);
                        if (tempoLabel != null) tempoLabel.Text = $"{m:0.00}×";
                    }
                };
            }

            if (pitchSlider != null)
            {
                pitchSlider.PropertyChanged += (_, e) =>
                {
                    if (e.Property.Name == "Value" && _audio != null)
                    {
                        var semis = (float)pitchSlider.Value;
                        _audio.SetPitchSemitones(semis);
                        if (pitchLabel != null) pitchLabel.Text = $"{semis:+0;-0;0} st";
                    }
                };
            }

            if (btn075 != null) btn075.Click += (_, __) => { if (tempoSlider != null) tempoSlider.Value = 0.75; };
            if (btn050 != null) btn050.Click += (_, __) => { if (tempoSlider != null) tempoSlider.Value = 0.50; };
            if (btn025 != null) btn025.Click += (_, __) => { if (tempoSlider != null) tempoSlider.Value = 0.25; };
            if (btn005 != null) btn005.Click += (_, __) => { if (tempoSlider != null) tempoSlider.Value = 0.05; };
            if (btnReset != null) btnReset.Click += (_, __) =>
            {
                if (tempoSlider != null) tempoSlider.Value = 1.0;
                if (pitchSlider != null) pitchSlider.Value = 0.0;
            };

            if (btnClose != null) btnClose.Click += (_, __) => Close();
        }
    }
}
