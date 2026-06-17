namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 状态 Enter / Update / Exit 期间可用的上下文；由 <see cref="GameFlowService"/> 在每帧复用同一实例。
    /// </summary>
    public interface IGameFlowContext
    {
        /// <summary>IOC 容器；Enter 内 Get/TryGet 依赖并缓存到状态私有字段，勿在 Update 热路径反复 Get。</summary>
        IServiceRegistry Services { get; }

        /// <summary>流程服务；状态内切换阶段请用 Flow.ChangeState，避免绕路 IoC。</summary>
        IGameFlowService Flow { get; }

        /// <summary>最近一次 ChangeState 传入的 userData；切换后由 Service 覆盖。</summary>
        object UserData { get; }
    }
}
