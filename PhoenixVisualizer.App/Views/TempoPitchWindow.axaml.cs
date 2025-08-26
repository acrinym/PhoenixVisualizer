using PhoenixVisualizer.Audio;

namespace PhoenixVisualizer.Views
{
    public partial class TempoPitchWindow : Window
    {
        private readonly VlcAudioService? _audio;

        public TempoPitchWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public TempoPitchWindow(VlcAudioService audio)
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

            // VLC supports tempo/pitch via rate control, so we can enable all controls

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
                        // Convert multiplier to percentage (1.0 = 100%, 0.5 = 50%, etc.)
                        var tempoPercent = (float)((m - 1.0) * 100.0);
                        _audio.SetTempo(tempoPercent);
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

                        // VLC's native API does not provide a direct SetPitchSemitones method, but pitch can be changed by adjusting the playback rate.
                        // To change pitch without affecting tempo, VLC requires the "scaletempo_pitch" audio filter.
                        // Here, we assume VlcAudioService exposes a method to set pitch in semitones using VLC's native capabilities.

                        // VLC doesn't support independent pitch control without affecting tempo
                        // For now, we'll just update the label to show the desired pitch
                        // TODO: Implement proper pitch shifting in VlcAudioService if needed

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
