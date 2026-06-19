namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 无 HybridCLR 热更时的 AOT 最小装配：仅注册框架级 Module，不含 GameFlow 与业务逻辑。
    /// <para>
    /// 用法：<see cref="GameRoot.TryStart"/> 或 <see cref="GameLaunchRunner"/> 的
    /// <see cref="GameLaunchMode.AotBootstrap"/> 模式。
    /// </para>
    /// <para>
    /// <see cref="GameRoot"/> 仍会执行 <c>EnsureAssetSystemReady</c> 预热资源系统；
    /// Asset 子系统可独立使用，GameRoot 作为集成入口负责在 Module Init 前完成 Catalog 就绪。
    /// </para>
    /// </summary>
    public sealed class AotMinimalBootstrap : IGameBootstrap
    {
        /// <inheritdoc />
        public void Configure(IServiceRegistry services, IModuleRegistry modules)
        {
            // [100] 可选但建议保留：提供 IGameUpdatePipeline / Timer，与 ModuleManager 共用 gameDelta
            modules.AddModule(new GameTimeModule(new GameTimeOptions
            {
                CalendarSettings = new GameCalendarSettings { SecondsPerDay = 120f },
                InitialTimeScale = 1f
            }));

            // 不注册 GameFlowModule — 宏观流程由热更 GameBootstrap 或项目自建 Bootstrap 按需添加
        }
    }
}
