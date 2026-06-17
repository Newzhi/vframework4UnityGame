namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 单个宏观游戏流程状态（对标 Procedure / GameFramework Procedure）。
    /// 热更层新增状态时实现此接口并在 <see cref="IGameFlowRegistry.Register"/> 注册。
    /// </summary>
    public interface IGameFlowState
    {
        /// <summary>唯一标识，与 <see cref="GameFlowIds"/> 或业务常量一致。</summary>
        string Id { get; }

        /// <summary>进入状态：订阅事件、打开 UI、启动异步加载、Activate 业务 Module 等。</summary>
        void Enter(IGameFlowContext context);

        /// <summary>每帧驱动：等待加载完成、超时、子步骤推进；无逻辑可留空。</summary>
        void Update(float deltaTime, IGameFlowContext context);

        /// <summary>离开状态：退订、关闭 UI、Deactivate 业务 Module；与 Enter 对称。</summary>
        void Exit(IGameFlowContext context);
    }
}
