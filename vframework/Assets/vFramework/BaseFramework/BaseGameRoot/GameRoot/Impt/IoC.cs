namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 静态访问门面，便于 Bootstrap 与旧代码过渡。
    /// 新代码应在 <see cref="IGameModule.Init"/> 中 Get 并缓存；避免在 Update 内反复 IoC.Get。
    /// </summary>
    public static class IoC
    {
        public static ServiceContainer Container { get; private set; }

        internal static void SetContainer(ServiceContainer container) => Container = container;

        public static T Get<T>() where T : class
        {
            if (Container == null)
                throw new System.InvalidOperationException("IoC not initialized. Ensure GameRoot is in the bootstrap scene.");

            return Container.Get<T>();
        }

        public static bool TryGet<T>(out T instance) where T : class
        {
            if (Container == null)
            {
                instance = null;
                return false;
            }

            return Container.TryGet(out instance);
        }
    }
}
