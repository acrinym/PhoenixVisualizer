using PhoenixVisualizer.Core.Config;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Core;

// 🤖 Decides when to switch presets
public sealed class PresetScheduler
{
    private DateTime _lastSwitch = DateTime.MinValue;
    private DateTime _lastBeat = DateTime.MinValue;
    private int _beatCount;
    private int _barCount;

    public bool ShouldSwitch(AudioFeatures features, VisualizerSettings s)
    {
        // skip when silent unless allowed
        if (!s.RandomWhenSilent && features.Rms < s.SilenceRmsGate)
        {
            if ((DateTime.UtcNow - _lastBeat).TotalSeconds > 2)
            {
                _beatCount = 0;
                _barCount = 0;
            }
            return false;
        }

        if (_lastSwitch != DateTime.MinValue &&
            (DateTime.UtcNow - _lastSwitch).TotalMilliseconds < Math.Max(0, s.RandomPresetCooldownMs))
            return false;

        switch (s.RandomPresetMode)
        {
            case RandomPresetMode.Off:
                return false;
            case RandomPresetMode.OnBeat:
                return features.Beat && ArmSwitch(s);
            case RandomPresetMode.Interval:
                return IntervalReady(s);
            case RandomPresetMode.Stanza:
                return StanzaReady(features, s);
            default:
                return false;
        }
    }

    public void NotifySwitched() => _lastSwitch = DateTime.UtcNow;

    private bool IntervalReady(VisualizerSettings s)
    {
        if (_lastSwitch == DateTime.MinValue) return ArmSwitch(s);
        var due = _lastSwitch.AddSeconds(Math.Clamp(s.RandomPresetIntervalSeconds, 5, 600));
        return DateTime.UtcNow >= due && ArmSwitch(s);
    }

    private bool StanzaReady(AudioFeatures f, VisualizerSettings s)
    {
        if (f.Beat)
        {
            _lastBeat = DateTime.UtcNow;
            _beatCount++;
            int beatsPerBar = Math.Clamp(s.BeatsPerBar, 2, 8);
            if (_beatCount % beatsPerBar == 0)
            {
                _barCount++;
                if (_barCount >= Math.Clamp(s.StanzaBars, 4, 128))
                {
                    _beatCount = 0;
                    _barCount = 0;
                    return ArmSwitch(s);
                }
            }
        }
        return false;
    }

    private bool ArmSwitch(VisualizerSettings s)
    {
        if (_lastSwitch != DateTime.MinValue &&
            (DateTime.UtcNow - _lastSwitch).TotalMilliseconds < Math.Max(0, s.RandomPresetCooldownMs))
            return false;
        _lastSwitch = DateTime.UtcNow;
        return true;
    }
}

