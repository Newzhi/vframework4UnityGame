namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// Bootstrap / Launcher 场景启动方式。默认 AOT 直启；热更（HybridCLR）为可选附加能力。
    /// </summary>
    public enum GameLaunchMode
    {
        /// <summary>
        /// 直接 <see cref="GameRoot.TryStart"/> 传入 AOT Bootstrap，无热更 DLL、无反射（默认）。
        /// </summary>
        AotBootstrap = 0,

        /// <summary>
        /// 经 <see cref="HotfixLaunchCoordinator"/> 反射调用热更入口（启用 HybridCLR 时使用）。
        /// 首次解析后会缓存 MethodInfo，避免重复反射开销。
        /// </summary>
        HotfixReflection = 1,
    }
}
