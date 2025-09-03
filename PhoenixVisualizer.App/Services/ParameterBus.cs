using System;
using Avalonia.Threading;

namespace PhoenixVisualizer.App.Services
{
    /// <summary>
    /// Tiny global dispatcher for "current parameter editing target".
    /// Any editor can call PublishTarget(target); the ParameterPanel will reflect it.
    /// </summary>
    public static class ParameterBus
    {
        private static object? _current;
        public static event Action<object?>? TargetChanged;

        public static object? CurrentTarget => _current;

        public static void PublishTarget(object? target)
        {
            if (!ReferenceEquals(_current, target))
            {
                _current = target;
                if (Dispatcher.UIThread.CheckAccess())
                {
                    TargetChanged?.Invoke(_current);
                }
                else
                {
                    Dispatcher.UIThread.Post(() => TargetChanged?.Invoke(_current));
                }
            }
        }
    }
}
