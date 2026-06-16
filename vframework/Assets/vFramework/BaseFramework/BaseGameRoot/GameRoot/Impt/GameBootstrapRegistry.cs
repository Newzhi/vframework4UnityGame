using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    public static class GameBootstrapRegistry
    {
        private static IGameBootstrap _bootstrap;

        public static void Register(IGameBootstrap bootstrap)
        {
            if (bootstrap == null)
                throw new System.ArgumentNullException(nameof(bootstrap));

            if (_bootstrap != null && _bootstrap.GetType() != bootstrap.GetType())
                Debug.LogWarning(
                    $"{nameof(GameBootstrapRegistry)}: Bootstrap already registered as {_bootstrap.GetType().Name}, replacing with {bootstrap.GetType().Name}.");

            _bootstrap = bootstrap;
        }

        public static bool TryGet(out IGameBootstrap bootstrap)
        {
            bootstrap = _bootstrap;
            return bootstrap != null;
        }

        internal static void Clear() => _bootstrap = null;
    }
}
