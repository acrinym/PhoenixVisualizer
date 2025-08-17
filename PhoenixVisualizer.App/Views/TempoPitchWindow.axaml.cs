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

            // Initialize labels
            TempoLabel.Text = "1.00×";
            PitchLabel.Text = "0 st";

            TempoSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == "Value" && _audio != null)
                {
                    var m = (double)TempoSlider.Value;
                    _audio.SetTempoMultiplier(m);
                    TempoLabel.Text = $"{m:0.00}×";
                }
            };

            PitchSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == "Value" && _audio != null)
                {
                    var semis = (float)PitchSlider.Value;
                    _audio.SetPitchSemitones(semis);
                    PitchLabel.Text = $"{semis:+0;-0;0} st";
                }
            };

            Btn075.Click += (_, __) => TempoSlider.Value = 0.75;
            Btn050.Click += (_, __) => TempoSlider.Value = 0.50;
            Btn025.Click += (_, __) => TempoSlider.Value = 0.25;
            Btn005.Click += (_, __) => TempoSlider.Value = 0.05;
            BtnReset.Click += (_, __) =>
            {
                TempoSlider.Value = 1.0;
                PitchSlider.Value = 0.0;
            };

            BtnClose.Click += (_, __) => Close();
        }
    }
}
