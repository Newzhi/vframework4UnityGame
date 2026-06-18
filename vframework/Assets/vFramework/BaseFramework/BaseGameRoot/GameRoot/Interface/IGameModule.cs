namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏模块：由 <see cref="ModuleManager"/> 统一 Init / Update / FixedUpdate / LateUpdate / Dispose。
    /// 数值越小越先执行（与 ECS SystemUpdateOrder 概念对齐，便于后续迁移）。
    /// 可选相位：<see cref="IFixedUpdateModule"/>、<see cref="ILateUpdateModule"/>、
    /// <see cref="IGizmoDrawModule"/>（Editor Scene Gizmo）。
    /// </summary>
    public interface IGameModule
    {
        /// <summary>执行顺序，默认 <see cref="ModulePriority.Normal"/>。</summary>
        int Priority => ModulePriority.Normal;

        /// <summary>启动阶段：从 <paramref name="services"/> Get 依赖并缓存字段，禁止在此 Update。</summary>
        void Init(IServiceRegistry services);

        /// <summary>Update 相位逻辑（由 <see cref="GameRoot"/> 驱动）。</summary>
        void Update(float deltaTime);

        /// <summary>逆序释放，与 Init 对称。</summary>
        void Dispose();
    }
}
