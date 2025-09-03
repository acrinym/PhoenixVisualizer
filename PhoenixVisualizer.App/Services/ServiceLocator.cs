using System;
using System.Collections.Concurrent;

namespace PhoenixVisualizer.App.Services
{
    /// <summary>
    /// IF_NEEDED: Simple service locator for places where DI is not yet wired.
    /// Use grep 'IF_NEEDED' to find and remove when full DI is in place.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly ConcurrentDictionary<Type, object> _map = new();

        public static void Register<T>(T instance) where T : class => _map[typeof(T)] = instance;
        public static T Resolve<T>() where T : class
        {
            if (_map.TryGetValue(typeof(T), out var o) && o is T t) return t;
            // Auto-create known services if not registered explicitly
            if (typeof(T) == typeof(ModalPreviewService))
            {
                var svc = new ModalPreviewService();
                Register(svc);
                return (T)(object)svc;
            }
            throw new InvalidOperationException($"IF_NEEDED ServiceLocator: Service not registered for {typeof(T).Name}");
        }
    }
}
